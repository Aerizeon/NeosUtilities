using BaseX;
using FrooxEngine;
using FrooxEngine.LogiX;
using FrooxEngine.UIX;
using System.Collections.Generic;

namespace NeosSimpleUtilities.MonoPackTool
{
    [Category("Add-Ons/Optimization")]
    public class MonoPackTool : Component, ICustomInspector
    {
        public readonly SyncRef<Slot> PackingRoot;

        public void BuildInspectorUI(UIBuilder ui)
        {
            WorkerInspector.BuildInspectorUI(this, ui);
            ui.Button("MonoPack Slot", MonoPack_Pressed);
        }

        private void MonoPack_Pressed(IButton button, ButtonEventData eventData)
        {
            button.LabelText = $"MonoPacked {MonoPack.MonoPackSlot(PackingRoot.Target)} Slot(s)";
        }

        [ImpulseTarget]
        public void MonoPackSlot()
        {
            MonoPack.MonoPackSlot(PackingRoot.Target);
        }
    }
}
