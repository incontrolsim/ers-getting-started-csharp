using System;
using System.Runtime.InteropServices;
using Ers;

namespace ScalingUp
{
    internal class SenderBehavior : ScriptBehaviorComponent
    {
        public ulong SendTime = 10;
        public int TargetSimId;

        public override void OnEntered(ulong newChild)
        {
            SubModel subModel = SubModel.GetSubModel();

            // Update channel
            var channel = ConnectedEntity.GetComponent<Channel>();
            channel.Value.InputOpen = false;
            channel.Value.Seen++;

            // Schedule sync-event
            ulong delay = subModel.ApplyModelPrecision(SendTime);
            var syncEvent = EventScheduler.ScheduleSyncEvent<SendEntitySyncEvent>(delay, TargetSimId);
            syncEvent.Value.MoveEntity = newChild;
            syncEvent.Value.SenderEntity = ConnectedEntity;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public partial struct SendEntitySyncEvent : ISyncEvent<SendEntitySyncEvent>
    {
        public static string Name => "Send entity to other simulator";

        private MoveEntitySyncEvent moveEvent;

        public Entity MoveEntity
        {
            set
            {
                // Pass along the entity to the internal sync-event
                moveEvent.EntityInFlight = value;
            }
        }
        // This entity only exists on the sender submodel!
        public Entity SenderEntity;

        public void OnSenderSide()
        {
            Logger.Debug($"Sending {moveEvent.EntityInFlight.GetName()} through a sync-event");
            moveEvent.OnSenderSide();
            // Open the input channel of the sender again so it can accept more products
            SenderEntity.GetComponent<Channel>().Value.InputOpen = true;
        }

        public void OnTargetSide()
        {
            Logger.Debug($"Receiving {moveEvent.EntityInFlight.GetName()} through a sync-event");
            moveEvent.OnTargetSide();

            // Move the received entity to the sink
            SubModel subModel = SubModel.GetSubModel();
            var context = subModel.GetSubModelContext<SinkContext>();
            subModel.UpdateParentOnEntity(moveEvent.EntityInFlight, context.SinkEntity);
        }
    }
}
