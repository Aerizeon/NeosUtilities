using FrooxEngine;
using FrooxEngine.LogiX;
using FrooxEngine.UIX;

namespace NeosSimpleUtilities.LogixCleanupTool
{
    [Category("Epsilion/Utilities")]
    public class LogixCleanupTool : Component, ICustomInspector
    {
        public readonly SyncRef<Slot> TargetSlot;
        public void BuildInspectorUI(UIBuilder ui)
        {
            WorkerInspector.BuildInspectorUI(this, ui);
            ui.Button("Run Cleanup", RunCleanup_Pressed);
        }

        private void RunCleanup_Pressed(IButton button, ButtonEventData eventData)
        {
            if(TargetSlot.Target != null)
            {
                int totalRemovedComponents = RemoveUnusedLogixComponents(TargetSlot.Target);
                button.LabelText = $"Removed {totalRemovedComponents} components.";
            }
        }

        private int RemoveUnusedLogixComponents(Slot targetSlot)
        {
            int removedComponentCount = targetSlot.RemoveAllComponents((Component targetComponent) => {
                if (targetComponent is LogixReference targetLogixReference)
                    return targetLogixReference.RefTarget.Target is null || targetLogixReference.RefNode.Target is null;
                return targetComponent is LogixInterfaceProxy;
            });

            foreach (Slot childSlot in targetSlot.Children)
            {
                removedComponentCount += RemoveUnusedLogixComponents(childSlot);
            }
            return removedComponentCount;
        }
    }
}
