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
    /*
     * The actual MonoPack logic, which is a disaster.
     */

    public static class MonoPack
    {
        /// <summary>
        /// Packs all of the child LogiX nodes directly into their respective
        /// LogiX root slots, reducing the overall number of slots, at the expense
        /// of maintainability.
        /// </summary>
        /// <param name="targetSlot">The Object Root or LogiX root to pack.
        /// Object roots will be traversed to find all relevant LogiX roots.</param>
        /// <returns></returns>
        public static int MonoPackSlot(Slot targetSlot)
        {
            if (targetSlot == null)
                return 0;
            int movedNodes = 0;
            Dictionary<Slot, LogixSlotTarget> ComponentMap = new Dictionary<Slot, LogixSlotTarget>();

            List<Component> allComponents = targetSlot.GetComponentsInChildren<Component>();
            allComponents.AddRange(targetSlot.Components);
            List<ISyncRef> retargetableSyncRefs = new List<ISyncRef>();
            List<IDriverNode> retargetableDriveNodes = new List<IDriverNode>();
            List<IField> retargetableFields = new List<IField>();

            foreach (Component childComponent in allComponents)
            {
                //Check if the current component is a LogiX node.
                if (childComponent is LogixNode childNode)
                {
                    foreach (ISyncMember syncMember in childComponent.SyncMembers)
                    {
                        /* Something is driving the content of a LogiX node,
                         * which generally isn't a good idea.
                         */
                        if (syncMember.IsDriven)
                        {
                            UniLog.Log(childNode.Name + ":" + syncMember.Name + " is being driven - we should probably rebind this");
                        }
                    }

                    /* Here we find the LogiX root of each item,
                     * and add it to a list so we can traverse
                     * them individually later.
                     */

                    if (!ComponentMap.TryGetValue(childNode.Slot.Parent, out LogixSlotTarget target))
                    {
                        //Check if this slot has already been packed.
                        if (childNode.Slot.Parent.Tag != "MonoPack")
                        {
                            target = new LogixSlotTarget();
                            ComponentMap.Add(childNode.Slot.Parent, target);
                        }
                    }

                    /*
                     * LogiX drive nodes are special, and have to be 
                     * destroyed before a new one can be connected,
                     * unlike most other components (at the moment).
                     * So, we keep track of all drive nodes to
                     * retarget them later
                     */

                    if (childNode is IDriverNode driverNode)
                    {
                        retargetableDriveNodes.Add(driverNode);
                    }

                    //Add this LogiX node to list of nodes in this LogiX root.
                    target.Nodes.Add(childNode);

                }
                //Otherwise, it's some other component
                else
                {
                    /*
                     * Check each SyncMember in every component,
                     * and see if any are targeting fields inside
                     * a LogiX node. If so, we need to rebind it
                     * to the new LogiX node component when we 
                     * move it.
                     */

                    foreach (ISyncMember syncMember in childComponent.SyncMembers)
                    {
                        if (syncMember is ISyncRef syncRef)
                        {
                            //Check if this SyncRef is pointing at a Logix Node
                            if (syncRef.Target.FindNearestParent<Component>() is LogixNode Lx)
                            {
                                //In theory, both of these calls should do the same thing, but I haven't tested it yet.

                                if (syncRef.Target is ISyncMember)
                                {
                                    retargetableSyncRefs.Add(syncRef);
                                    UniLog.Log("Found binding to" + Lx.Name + " - we should retarget it once duplication is done");
                                }
                                if (syncRef.TargetType.IsGenericType && syncRef.TargetType.GetGenericTypeDefinition() == typeof(IField<>))
                                {
                                    UniLog.Log("Found Field Binding, it should be retargeted");
                                }
                            }
                        }
                    }
                }

            }

            /*
             * For each component with a reference that needs
             * to be retargeted, get the index of the logix node
             * that it is currently bound to, and store it along 
             * with the name of the SyncMember,so we can set it 
             * back up later.
             */

            foreach (ISyncRef syncRef in retargetableSyncRefs)
            {
                if (syncRef.Target.FindNearestParent<Component>() is LogixNode childNode)
                {
                    if (syncRef.Target is ISyncMember targetMember)
                    {
                        //Check if this LogiX Node was added to the list for its intended LogiX root.
                        if (!ComponentMap.TryGetValue(childNode.Slot.Parent, out LogixSlotTarget target))
                        {
                            //LogiX Node didn't exist in root, ignore it.
                            continue;
                        }
                        //Find the index of this LogiX Node within the parent
                        int targetIndex = target.Nodes.FindIndex(C => C.ReferenceID == childNode.ReferenceID);
                        UniLog.Log("Queue SyncRef rebind: " + childNode.GetSyncMemberName(targetMember));
                        //Add to the rebind queue, with the SyncMember name, and the LogiX Node Index.
                        target.Rebinds.Add(new SyncRefRebind()
                        {
                            ComponentIndex = targetIndex,
                            ComponentMemberName = childNode.GetSyncMemberName(targetMember),
                            ReferenceSource = syncRef
                        });
                    }
                }
            }

            foreach(IDriverNode driveNode in retargetableDriveNodes)
            {
                if (!ComponentMap.TryGetValue(driveNode.Slot.Parent, out LogixSlotTarget target))
                {
                    continue;
                }
                int targetIndex = target.Nodes.FindIndex(C => C.ReferenceID == driveNode.ReferenceID);
                UniLog.Log("Queue Drive rebind");
                target.Rebinds.Add(new DriveNodeRebind()
                {
                    ComponentIndex = targetIndex,
                    ComponentMemberName = "",
                    FieldTarget = driveNode.DriveField
                });
            }

            foreach (KeyValuePair<Slot, LogixSlotTarget> slotEntry in ComponentMap)
            {
                List<Component> duplicates = new List<Component>();
                slotEntry.Key.DuplicateComponents(slotEntry.Value.Nodes, false, duplicates);
                movedNodes += duplicates.Count;
                foreach (Component t in slotEntry.Value.Nodes)
                    t.Slot.Destroy(false);

                foreach (IRebindSource rebind in slotEntry.Value.Rebinds)
                {
                    if (rebind is SyncRefRebind syncRebind)
                    {
                        UniLog.Log("Attempt SyncRef rebind: " + rebind.ComponentMemberName);
                        ISyncMember target = duplicates[rebind.ComponentIndex].GetSyncMember(rebind.ComponentMemberName);
                        if (target != null)
                        {
                            syncRebind.ReferenceSource.Target = target;
                        }
                        else
                        {
                            UniLog.Log("Unable to resolve target sync member: " + rebind.ComponentMemberName + " in component" + duplicates[rebind.ComponentIndex].Name);
                        }
                    }
                    else if(rebind is DriveNodeRebind driveRebind)
                    {
                        UniLog.Log("Attempt Drive Rebind");
                        IDriverNode target = duplicates[rebind.ComponentIndex] as IDriverNode;
                        target.TrySetTarget(driveRebind.FieldTarget);
                    }
                }
                //Tag slots that we've packed.
                slotEntry.Key.Tag = "MonoPack";
            }
            return movedNodes;
        }

        /// <summary>
        /// Removes extra components and LogiX nodes that aren't needed for the LogiX to function.
        /// </summary>
        /// <param name="targetSlot">The Object Root or LogiX root to Optimize.
        /// Object Roots will be traversed to find all relevant LogiX roots, and will have extra components removed</param>
        /// <param name="RemoveReferences">If True, removes LogixReference components.</param>
        /// <param name="RemoveInterfaceProxies">If True, remove LogixInterfaceProxy components</param>
        /// <param name="RemoveRelays">If True, unpacks LogiX and removes all Relay and ImpulseRelay nodes</param>
        /// <param name="DestroyParent">If True, will destroy the parent slot of optimized components, if empty.</param>
        /// <returns></returns>
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

                /*
                 * If DestroyParent is enabled, and the target isn't a component that gets embedded on a normal slot,
                 * we should destroy the component's parent slot if it is empty.
                 */

                if (DestroyParent && !(targetComponent is LogixReference || targetComponent is LogixInterfaceProxy) && parent != targetSlot && parent.ChildrenCount == 0)
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

        public List<IRebindSource> Rebinds { get; set; } = new List<IRebindSource>();
    }

    public interface IRebindSource
    {
        int ComponentIndex { get; set; }
        string ComponentMemberName { get; set; }
    }
    public class SyncRefRebind : IRebindSource
    {
        public int ComponentIndex { get; set; }
        public string ComponentMemberName { get; set; }
        public ISyncRef ReferenceSource { get; set; }
    }

    public class DriveNodeRebind : IRebindSource
    {
        public int ComponentIndex { get; set; }
        public string ComponentMemberName { get; set; }
        public IWorldElement FieldTarget { get; set; }
    }
}
