using System;
using Ers;

namespace ScalingUp
{
    internal class SyncReceiver : ScriptBehaviorComponent
    {
        private Entity OutputChannel = CEntity.InvalidEntity();

        public static (SyncReceiver, Entity) Create(string name = "Receiver", bool addTracker = true)
        {
            SubModel subModel = SubModel.Get();

            Entity receiverEntity = subModel.CreateEntity(name);
            SyncReceiver receiver = receiverEntity.AddComponent<SyncReceiver>();

            // Channels
            receiverEntity.AddComponent<ResourceComponent>();
            Entity receiverOutputEntity = subModel.CreateEntity(receiverEntity, $"{name}-Output");
            ChannelComponent.AddChannelComponent(receiverOutputEntity, ChannelType.Output, receiverEntity);

            if (addTracker)
                receiverEntity.AddComponent<ProductTracker>();

            return (receiver, receiverOutputEntity);
        }

        public override void OnStart()
        {
            OutputChannel = ConnectedEntity.GetComponent<ResourceComponent>().Value.GetOutputChannel(0);
            ChannelComponent.Open(OutputChannel);
        }

        public override void OnEntered(Entity newChild)
        {
            // When output-channel is created, it also triggers this function. Ignore it
            if (newChild == OutputChannel || OutputChannel == CEntity.InvalidEntity())
                return;

            // If the entity also has a ProductTracker component, track the products
            if (ConnectedEntity.HasComponent<ProductTracker>())
            {
                ConnectedEntity.GetComponent<ProductTracker>().Value.Seen++;
            }

            Entity output = ConnectedEntity.GetComponent<ResourceComponent>().Value.GetOutputChannel(0);
            if (ChannelComponent.IsReady(output))
            {
                ChannelComponent.Send(output, newChild);
            }
        }
    }
}
