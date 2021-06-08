using BaseX;
using FrooxEngine;
using FrooxEngine.LogiX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NeosSimpleUtilities.MonoPackTool
{
    public static class MonoPack
    {
        public static int MonoPackSlot(Slot targetSlot)
        {
            if (targetSlot == null)
                return 0;
            int movedNodes = 0;
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
                movedNodes += duplicates.Count;
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
            return movedNodes;
        }
        public static async Task<int> OptimizeLogiX(Slot targetSlot, bool RemoveReferences, bool RemoveInterfaceProxies, bool RemoveRelays, bool DestroyParent = false)
        {
            if (targetSlot == null)
                return 0;
            //In order for relay removal to work properly, all of the LogiX must first be unpacked.
            //that way, Neos will regenerate the wire links upon removal.
            if (RemoveRelays)
                targetSlot.GetComponentsInChildren<LogixNode>().ForEach((n => n.GenerateVisual()));
            await new Updates(10);
            List<Component> componentsForRemoval = targetSlot.GetComponentsInChildren((Component targetComponent) =>
            {
                //Collect all LogiXReference and LogixInterfaceProxies for deletion
                if ((RemoveReferences && targetComponent is LogixReference) ||
                (RemoveInterfaceProxies && targetComponent is LogixInterfaceProxy))
                {
                    return true;
                }
                //If we have opted to remove relays, collect them as well.
                else if (RemoveRelays)
                {
                    Type componentType = targetComponent.GetType();
                    return (componentType.IsGenericType && componentType.GetGenericTypeDefinition() == typeof(RelayNode<>)) || targetComponent is ImpulseRelay;
                }
                return false;
            });


            foreach (Component targetComponent in componentsForRemoval)
            {
                //Get parent of the component to be removed
                Slot parent = targetComponent.Slot;
                //Destroy the component. If the logix is unpacked, this will regenerate any necessary connections.
                targetComponent.Destroy();
                //If DestroyParent is enabled, and the target isn't a component that gets embedded on a normal slot,
                //we should destroy the parent slot, which normally contains the component.
                if (DestroyParent && !(targetComponent is LogixReference || targetComponent is LogixInterfaceProxy) && parent != targetSlot)
                    parent.Destroy();
            }

            //If we were removing relays, we must pack everything back up so it's not hanging out all over the place (and presumably, so we can monopack)
            if (RemoveRelays)
                targetSlot.GetComponentInChildren<LogixNode>().RemoveAllLogixBoxes();
            await new Updates(10);
            //Return the total number of affected components.
            return componentsForRemoval.Count;
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
