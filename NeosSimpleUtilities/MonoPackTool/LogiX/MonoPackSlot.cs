using FrooxEngine;
using FrooxEngine.LogiX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NeosSimpleUtilities.MonoPackTool.LogiX
{
    [Category("LogiX/Epsilion/Utility")]
    class MonoPackSlot : LogixNode
    {
        public readonly Input<Slot> TargetSlot;
        public readonly Output<int> AffectedNodes;
        public readonly Impulse OnDone;

        [ImpulseTarget]
        public void Pack()
        {
            Slot targetSlot = TargetSlot.Evaluate();
            if (targetSlot != null)
            {
                AffectedNodes.Value = MonoPack.MonoPackSlot(targetSlot);
                OnDone.Trigger();
            }
        }
    }
}
