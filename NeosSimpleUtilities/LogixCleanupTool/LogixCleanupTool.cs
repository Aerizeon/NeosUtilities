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
                TargetSlot.Target.RemoveAllComponents((Component targetComponent) => {
                    if (targetComponent is LogixReference targetLogixReference)
                        return targetLogixReference.RefTarget.Target == null;
                    return targetComponent is LogixInterfaceProxy;
                });
            }
        }
    }
}
