using FrooxEngine;
using FrooxEngine.CommonAvatar;
using FrooxEngine.UIX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NeosSimpleUtilities.AvatarStrippingTool
{

    [Category("Add-Ons/Optimization")]
    class ColliderCleanupTool : Component, ICustomInspector
    {
        public readonly SyncRef<Slot> TargetSlot;
        public readonly Sync<bool> OnlyRemoveDisabled;
        public void BuildInspectorUI(UIBuilder ui)
        {
            WorkerInspector.BuildInspectorUI(this, ui);
            ui.Button("Remove Components", RunCleanup_Pressed);
            ui.Button("Remove Haptics Slots", RunSlotCleanup_Pressed);
        }

        private void RunCleanup_Pressed(IButton button, ButtonEventData eventData)
        {
            if (TargetSlot.Target != null)
            {
                int totalRemovedComponents = RemoveColliders(TargetSlot.Target);
                button.LabelText = $"Removed {totalRemovedComponents} components.";
            }
        }
        private void RunSlotCleanup_Pressed(IButton button, ButtonEventData eventData)
        {
            if (TargetSlot.Target != null)
            {
                int totalRemovedSlots = RemoveSlots(TargetSlot.Target);
                button.LabelText = $"Removed {totalRemovedSlots} Haptic Slots.";
            }
        }

        private int RemoveColliders(Slot targetSlot)
        {
            int removedComponentCount = targetSlot.RemoveAllComponents((Component targetComponent) => {
                return (targetComponent is ICollider ||
                targetComponent is HandPoser ||
                targetComponent is AvatarHandDataAssigner ||
                targetComponent is AvatarToolAnchor ||
                targetComponent is TipTouchSource || 
                targetComponent is VibrationDeviceRelay || 
                targetComponent is AvatarUserReferenceAssigner ||
                targetComponent is EyeManager ||
                targetComponent is EyeLinearDriver ||
                targetComponent is EyeRotationDriver || 
                targetComponent is AvatarUserPositioner) && (!targetComponent.Enabled || !OnlyRemoveDisabled.Value);
            });

            foreach (Slot childSlot in targetSlot.Children)
            {
                removedComponentCount += RemoveColliders(childSlot);
            }
            return removedComponentCount;
        }

        private int RemoveSlots(Slot targetSlot)
        {
            int removedSlots = 0;
            targetSlot.DestroyChildren(false, false, false, (Slot childSlot) =>
            {
                if(childSlot.Name.EndsWith("Haptics"))
                {
                    removedSlots++;
                    return true;
                }
                return false;
            });

            foreach (Slot childSlot in targetSlot.Children)
            {
                removedSlots += RemoveSlots(childSlot);
            }
            return removedSlots;
        }
    }
}
