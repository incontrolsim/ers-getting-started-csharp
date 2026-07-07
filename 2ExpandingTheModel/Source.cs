using System;
using Ers;

namespace ExpandingTheModel
{
    internal struct GenerateProductEvent : ILocalEvent<GenerateProductEvent>
    {
        public Entity SourceEntity;

        public void OnEvent()
        {
            SubModel subModel = SubModel.Get();
            SourceBehavior source = SourceEntity.GetComponent<SourceBehavior>();

            // Only create a product when it can be moved to the next entity
            if (source.Target.GetComponent<QueueBehavior>().InputOpen)
            {
                // Create the product
                Entity entity = subModel.CreateEntity(SourceEntity, $"Product{source.Produced + 1}");
                entity.AddComponent<Product>();
                source.Produced++;

                // Move the product to the next simulation object
                subModel.UpdateParentOnEntity(entity, source.Target);
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
        // The entity to which products are sent.
        public Entity Target;
        public ulong GenerationTime = 5;
        public ulong Produced = 0;

        public override void OnStart()
        {
            // Start infinite product generating event loop
            ulong delay = SubModel.Get().ApplyModelPrecision(GenerationTime);
            EventScheduler.ScheduleLocalEvent(0, delay, new GenerateProductEvent()
            {
                SourceEntity = ConnectedEntity
            });
        }
    }
}
