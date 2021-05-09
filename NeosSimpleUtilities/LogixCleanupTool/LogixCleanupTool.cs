using FrooxEngine;
using FrooxEngine.LogiX;
using FrooxEngine.UIX;
using System;
using System.Collections.Generic;

namespace NeosSimpleUtilities.LogixCleanupTool
{
    [Category("Add-Ons/Optimization")]
    public class LogixCleanupTool : Component, ICustomInspector
    {
        public readonly SyncRef<Slot> TargetSlot;
        public readonly Sync<bool> RemoveLogixReferences;
        public readonly Sync<bool> RemoveLogixInterfaceProxies;
        public readonly Sync<bool> RemoveRelays;
        public void BuildInspectorUI(UIBuilder ui)
        {
            WorkerInspector.BuildInspectorUI(this, ui);
            ui.Button("Run Cleanup", RunCleanup_Pressed);
        }

        private void RunCleanup_Pressed(IButton button, ButtonEventData eventData)
        {
            if(TargetSlot.Target != null)
            {
                int totalRemovedComponents = OptimizeLogiX(TargetSlot.Target);
                button.LabelText = $"Removed {totalRemovedComponents} components.";
            }
        }

        private int OptimizeLogiX(Slot targetSlot)
        {
            List<Component> componentsForRemoval = targetSlot.GetComponentsInChildren((Component targetComponent) =>
            {
                if (RemoveLogixReferences.Value && targetComponent is LogixReference targetLogixReference)
                    return targetLogixReference.RefTarget.Target is null || targetLogixReference.RefNode.Target is null;
                else if (RemoveLogixInterfaceProxies.Value && targetComponent is LogixInterfaceProxy)
                    return true;
                else if (RemoveRelays.Value)
                {
                    Type componentType = targetComponent.GetType();
                    return (componentType.IsGenericType && componentType.GetGenericTypeDefinition() == typeof(RelayNode<>)) || targetComponent is ImpulseRelay;
                }
                return false;
            });

            foreach (Component c in componentsForRemoval)
                c.Destroy();

            return componentsForRemoval.Count;
        }
    }
}
