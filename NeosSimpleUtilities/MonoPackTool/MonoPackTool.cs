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
        public readonly SyncRef<Slot> TargetSlot;
        public readonly Sync<bool> OptimizeLogiX;

        public void BuildInspectorUI(UIBuilder ui)
        {
            WorkerInspector.BuildInspectorUI(this, ui);
            ui.PushStyle();
            ui.Style.MinHeight = 80f;
            ui.Text("Keep a separate backup of your project, both before and after using this tool - the process is <b>NOT</b> reversable at this time.\n" +
                "<b>I am not responsible for any work lost as a result of using this tool<b>.\n" +
                "Please test any LogiX packed using this tool <b>Extensively</b>, as it may cause issues\n" +
                "with some logic flows, especially when LogiX is modifying values inside of itself in weird ways.\n" +
                "If you encounter any issues, please contact Epsilion, and send a copy of the relevant Neos logfile.", true, Alignment.TopCenter);
            ui.PopStyle();
            ui.Button("MonoPack Slot", MonoPack_Pressed);
        }

        private void MonoPack_Pressed(IButton button, ButtonEventData eventData)
        {
            World.Coroutines.StartTask(async () =>
            {
                //Remove all LogixReferences and such, since that doesn't require unpacking.
                await MonoPack.OptimizeLogiX(TargetSlot.Target, true, true, false);
                //Get a list of all logix nodes under the target slot
                List<LogixNode> nodesInTargetSlot = TargetSlot.Target.GetComponentsInChildren<LogixNode>();
                HashSet<Slot> logixRoots = new HashSet<Slot>();
                //Find all unique logix root slots under the target slot, and add them to the logixRoots HashSet
                foreach (LogixNode node in nodesInTargetSlot)
                {
                    logixRoots.Add(node.Slot.Parent);
                }

                //Go through each logix root and monopack all of the logix nodes under them.
                int totalPackedNodes = 0;
                foreach (Slot logixRoot in logixRoots)
                {
                    if (OptimizeLogiX.Value)
                    {
                        //Remove all relays, since they're not essential to a program's flow.
                        await MonoPack.OptimizeLogiX(logixRoot, false, false, true, true);
                        //Wait a bit for the operation to complete (hopefully)
                        await new Updates(10);
                    }

                    //Run monopack
                    totalPackedNodes += MonoPack.MonoPackSlot(logixRoot);
                    await new Updates(1);
                }
                await new ToWorld();
                button.LabelText = $"MonoPacked {totalPackedNodes} Node(s)";
            });
        }

        [ImpulseTarget]
        public void MonoPackSlot()
        {
            MonoPack.MonoPackSlot(TargetSlot.Target);
        }
    }
}
