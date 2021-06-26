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
    /*
     * Tool for removing non-IK components from avatars
     * so that they can be placed in a world, and not have 
     * an excessive amount of unused components (and slots)
     * on them.
     * Requested by Enverex
     */

    [Category("Add-Ons/Optimization")]
    class AvatarStrippingTool : Component, ICustomInspector
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
                int totalRemovedComponents = RemoveComponents(TargetSlot.Target);
                button.LabelText = $"Removed {totalRemovedComponents} components.";
            }
        }
        private void RunSlotCleanup_Pressed(IButton button, ButtonEventData eventData)
        {
            if (TargetSlot.Target != null)
            {
                int totalRemovedSlots = RemoveHapticsSlots(TargetSlot.Target);
                button.LabelText = $"Removed {totalRemovedSlots} Haptic Slots.";
            }
        }

        private int RemoveComponents(Slot targetSlot)
        {
            /*
             * For Enverex's purposes, none of these are useful,
             * so we strip out all colliders and other normal
             * avatar components, since only the IK system
             * is needed
             */

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
                removedComponentCount += RemoveComponents(childSlot);
            }
            return removedComponentCount;
        }

        private int RemoveHapticsSlots(Slot targetSlot)
        {
            /*
             * Removes the Haptics slots. Since this object
             * will no longer be a usable avatar, haptics
             * aren't necessary.
             */
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
                removedSlots += RemoveHapticsSlots(childSlot);
            }
            return removedSlots;
        }
    }
}
