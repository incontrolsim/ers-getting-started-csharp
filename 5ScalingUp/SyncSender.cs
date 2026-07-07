using System;
using System.Runtime.InteropServices;
using Ers;

namespace ScalingUp
{
    internal class SyncSenderBehavior : ScriptBehaviorComponent
    {
        public ulong SendTime = 10;
        public int TargetSimId;

        public static (SyncSenderBehavior, Entity) Create(string name = "Sender", bool addTracker = true)
        {
            SubModel subModel = SubModel.Get();

            Entity senderEntity = subModel.CreateEntity(name);
            SyncSenderBehavior sender = senderEntity.AddComponent<SyncSenderBehavior>();

            // Channels
            senderEntity.AddComponent<ResourceComponent>();
            Entity senderInputEntity = subModel.CreateEntity(senderEntity, $"{name}-Input");
            ChannelComponent.AddChannelComponent(senderInputEntity, ChannelType.Input, senderEntity);

            if (addTracker)
                senderEntity.AddComponent<ProductTracker>();

            return (sender, senderInputEntity);
        }

        public override void OnStart()
        {
            Entity input = ConnectedEntity.GetComponent<ResourceComponent>().Value.GetInputChannel(0);
            ChannelComponent.Open(input);
        }

        public override void OnReceive(Entity inputChannel, Entity child)
        {
            SubModel subModel = SubModel.Get();

            // If the entity also has a ProductTracker component, track the products
            if (ConnectedEntity.HasComponent<ProductTracker>())
            {
                ConnectedEntity.GetComponent<ProductTracker>().Value.Seen++;
            }

            Entity input = ConnectedEntity.GetComponent<ResourceComponent>().Value.GetInputChannel(0);
            ChannelComponent.Close(input);

            // Schedule sync-event
            ulong delay = subModel.ApplyModelPrecision(SendTime);
            SendEntitySyncEvent syncEventData = new();
            syncEventData.MoveEntity = child;
            syncEventData.SenderEntity = ConnectedEntity;
            EventScheduler.ScheduleSyncEvent<SendEntitySyncEvent>(delay, TargetSimId, syncEventData);
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

            // Open the input-channel of the sender again so it can accept more products
            Entity input = SenderEntity.GetComponent<ResourceComponent>().Value.GetInputChannel(0);
            ChannelComponent.Open(input);
        }

        public void OnTargetSide()
        {
            moveEvent.OnTargetSide();
            Logger.Debug($"Receiving {moveEvent.EntityInFlight.GetName()} through a sync-event");

            // Move the received entity to the sink
            SubModel subModel = SubModel.Get();
            var context = subModel.GetSubModelContext<SinkContext>();
            subModel.UpdateParentOnEntity(moveEvent.EntityInFlight, context.ReceiverEntity);
        }
    }
}
