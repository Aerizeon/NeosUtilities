using BaseX;
using FrooxEngine;
using FrooxEngine.LogiX;
using FrooxEngine.UIX;
using System.Collections.Generic;

namespace NeosSimpleUtilities.MonoPack
{
    [Category("Epsilion/Utilities")]
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
            Slot targetSlot = PackingRoot.Target;
            if (targetSlot == null)
                return;
            Dictionary<Slot, LogixSlotTarget> ComponentMap = new Dictionary<Slot, LogixSlotTarget>();

            List<Component> allComponents = targetSlot.GetComponentsInChildren<Component>();
            List<ISyncRef> retargetableSyncRefs = new List<ISyncRef>();
            foreach (Component childComponent in allComponents)
            {
                if (childComponent is LogixNode childNode)
                {
                    foreach (ISyncMember syncMember in childComponent.SyncMembers)
                    {
                        if (syncMember.IsDriven)
                        {
                            UniLog.Log(childNode.Name + ":" + syncMember.Name + " is being driven - we should probably rebind this");
                        }
                    }
                    if (!ComponentMap.TryGetValue(childNode.Slot.Parent, out LogixSlotTarget target))
                    {
                        target = new LogixSlotTarget();
                        ComponentMap.Add(childNode.Slot.Parent, target);
                    }
                    target.Nodes.Add(childNode);
                }
                else
                {
                    foreach (ISyncMember syncMember in childComponent.SyncMembers)
                    {
                        if (syncMember is ISyncRef syncRef)
                        {
                            if (syncRef.Target.FindNearestParent<Component>() is LogixNode Lx)
                            {
                                if (syncRef.Target is ISyncMember)
                                {
                                    retargetableSyncRefs.Add(syncRef);
                                    UniLog.Log("Found binding to" + Lx.Name + " - we should retarget it once duplication is done");
                                }
                            }
                        }
                    }
                }

            }

            foreach (ISyncRef syncRef in retargetableSyncRefs)
            {
                if (syncRef.Target.FindNearestParent<Component>() is LogixNode childNode)
                {
                    if (syncRef.Target is ISyncMember targetMember)
                    {
                        if (!ComponentMap.TryGetValue(childNode.Slot.Parent, out LogixSlotTarget target))
                        {
                            continue;
                        }
                        int targetIndex = target.Nodes.FindIndex(C => C.ReferenceID == childNode.ReferenceID);
                        UniLog.Log("Try add rebind: " + childNode.GetSyncMemberName(targetMember));
                        target.Rebinds.Add(new SyncRefRebind()
                        {
                            ComponentIndex = targetIndex,
                            ComponentMemberName = childNode.GetSyncMemberName(targetMember),
                            ReferenceSource = syncRef
                        });
                    }
                }
            }

            foreach (KeyValuePair<Slot, LogixSlotTarget> slotEntry in ComponentMap)
            {
                List<Component> duplicates = new List<Component>();
                slotEntry.Key.DuplicateComponents(slotEntry.Value.Nodes, false, duplicates);
                foreach (SyncRefRebind rebind in slotEntry.Value.Rebinds)
                {
                    UniLog.Log("Try rebind: " + rebind.ComponentMemberName);
                    ISyncMember target = duplicates[rebind.ComponentIndex].GetSyncMember(rebind.ComponentMemberName);
                    if (target != null)
                    {
                        rebind.ReferenceSource.Target = target;
                    }
                    else
                    {
                        UniLog.Log("Unable to resolve target sync member: " + rebind.ComponentMemberName + " in component" + duplicates[rebind.ComponentIndex].Name);
                    }
                }
                foreach (Component t in slotEntry.Value.Nodes)
                    t.Slot.Destroy(false);
            }
        }
    }

    public class LogixSlotTarget
    {
        public List<Component> Nodes { get; set; } = new List<Component>();

        public List<SyncRefRebind> Rebinds { get; set; } = new List<SyncRefRebind>();
    }

    public class SyncRefRebind
    {
        public int ComponentIndex { get; set; }
        public string ComponentMemberName { get; set; }
        public ISyncRef ReferenceSource { get; set; }
    }
}
