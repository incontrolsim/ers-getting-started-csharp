using System;
using Ers;

namespace ScalingUp
{
    internal class SinkBehavior : ScriptBehaviorComponent
    {
        public static (SinkBehavior, Entity) Create(string name = "Sink", bool addTracker = true)
        {
            SubModel subModel = SubModel.Get();

            Entity sinkEntity = subModel.CreateEntity(name);
            SinkBehavior sink = sinkEntity.AddComponent<SinkBehavior>();

            // Channels
            sinkEntity.AddComponent<ResourceComponent>();
            Entity sinkInputEntity = subModel.CreateEntity(sinkEntity, $"{name}-Input");
            ChannelComponent.AddChannelComponent(sinkInputEntity, ChannelType.Input, sinkEntity);

            if (addTracker)
                sinkEntity.AddComponent<ProductTracker>();

            return (sink, sinkInputEntity);
        }

        public override void OnStart()
        {
            Ref<ResourceComponent> resource = ConnectedEntity.GetComponent<ResourceComponent>();
            Entity input = resource.Value.GetInputChannel(0);
            ChannelComponent.Open(input);
        }

        public override void OnReceive(Entity inputChannel, Entity child)
        {
            // If the entity also has a ProductTracker component, track the products
            if (ConnectedEntity.HasComponent<ProductTracker>())
            {
                ConnectedEntity.GetComponent<ProductTracker>().Value.Seen++;
            }
            SubModel.Get().DestroyEntity(child);
        }
    }
}
