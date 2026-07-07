using System;
using Ers;

namespace ScalingUp
{
    internal struct GenerateProductEvent : ILocalEvent<GenerateProductEvent>
    {
        public Entity SourceEntity;

        public void OnEvent()
        {
            SubModel subModel = SubModel.Get();
            SourceBehavior source = SourceEntity.GetComponent<SourceBehavior>();

            // Only create a product when it can be moved to the next entity
            if (source.CurrentProduced == CEntity.InvalidEntity())
            {
                // Create the product
                Entity entity = subModel.CreateEntity(SourceEntity, $"Product{source.Produced + 1}");
                entity.AddComponent<Product>();
                source.Produced++;
                source.CurrentProduced = entity;

                // If the Source has a tracker, perform tracking
                if (SourceEntity.HasComponent<ProductTracker>())
                {
                    SourceEntity.GetComponent<ProductTracker>().Value.Seen = source.Produced;
                }

                // Move the product to the next simulation object
                Entity output = SourceEntity.GetComponent<ResourceComponent>().Value.GetOutputChannel(0);
                ChannelComponent.Open(output);
            }

            // Re-schedule this function, creating a loop
            ulong delay = subModel.ApplyModelPrecision(source.GenerationTime);
            EventScheduler.ScheduleLocalEvent(0, delay, new GenerateProductEvent()
            {
                SourceEntity = SourceEntity
            });
        }
    }

    internal class SourceBehavior : ScriptBehaviorComponent
    {
        public ulong GenerationTime = 5;
        public ulong Produced = 0;
        public Entity CurrentProduced = CEntity.InvalidEntity();

        public static (SourceBehavior, Entity) Create(string name = "Source", bool addTracker = true)
        {
            SubModel subModel = SubModel.Get();

            Entity sourceEntity = subModel.CreateEntity(name);
            SourceBehavior source = sourceEntity.AddComponent<SourceBehavior>();
            sourceEntity.AddComponent<ResourceComponent>();
            Entity sourceOutputEntity = subModel.CreateEntity(sourceEntity, $"{name}-Output");
            ChannelComponent.AddChannelComponent(sourceOutputEntity, ChannelType.Output, sourceEntity);

            if (addTracker)
                sourceEntity.AddComponent<ProductTracker>();

            return (source, sourceOutputEntity);
        }

        public override void OnStart()
        {
            // Start infinite product generating event loop
            ulong delay = SubModel.Get().ApplyModelPrecision(GenerationTime);
            EventScheduler.ScheduleLocalEvent(0, delay, new GenerateProductEvent()
            {
                SourceEntity = ConnectedEntity
            });
        }

        public override void OnOutputChannelReady(Entity outputChannel)
        {
            if (CurrentProduced == CEntity.InvalidEntity())
                return;

            /*
             * Store toSend locally first, because CurrentProduced needs to be invalid before Send is called.
             * Otherwise, the receiving side of the channel may trigger this callback again
             * and CurrentProduced will still be valid, thus it would try to send the same entity again.
             */
            Entity toSend = CurrentProduced;
            CurrentProduced = CEntity.InvalidEntity();
            ChannelComponent.Send(outputChannel, toSend);
            ChannelComponent.Close(outputChannel);
        }
    }
}
