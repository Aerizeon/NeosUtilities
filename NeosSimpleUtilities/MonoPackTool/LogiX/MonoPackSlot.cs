using FrooxEngine;
using FrooxEngine.LogiX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NeosSimpleUtilities.MonoPackTool.LogiX
{
    /*
     * LogiX node for executing the MonoPack
     * operation automatically.
     * This doesn't work with an object root
     * yet, because I'm lazy, so you have to
     * pass in each LogiX root manually.
     */

    [Category("LogiX/Add-Ons/Optimization")]
    class MonoPackSlot : LogixNode
    {
        public readonly Input<Slot> TargetSlot;
        public readonly Input<bool> OptimizeLogiX;
        public readonly Output<int> PackedNodes;
        public readonly Output<int> OptimizedNodes;
        public readonly Impulse OnDone;

        [ImpulseTarget]
        public void Pack()
        {
            Slot targetSlot = TargetSlot.Evaluate();
            if (targetSlot != null)
            {
                World.Coroutines.StartTask(async () =>
                {
                    if (OptimizeLogiX.Evaluate())
                        OptimizedNodes.Value = await MonoPack.OptimizeLogiX(targetSlot, false, false, true);
                    PackedNodes.Value = MonoPack.MonoPackSlot(targetSlot);
                    OnDone.Trigger();
                });
            }
        }
    }
}
