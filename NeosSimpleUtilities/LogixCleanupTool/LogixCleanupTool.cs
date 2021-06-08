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
                World.Coroutines.StartTask(async () =>
                {
                    int totalRemovedComponents = await MonoPackTool.MonoPack.OptimizeLogiX(TargetSlot.Target, RemoveLogixReferences.Value, RemoveLogixInterfaceProxies.Value, RemoveRelays.Value, true);
                    button.LabelText = $"Removed {totalRemovedComponents} components.";
                });
            }
        }
    }
}
