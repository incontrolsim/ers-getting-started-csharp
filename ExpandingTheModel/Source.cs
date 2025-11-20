using System;
using Ers;

namespace ExpandingTheModel
{
    internal class SourceBehavior : ScriptBehaviorComponent
    {
        // The entity to which products are sent.
        public Entity Target;
        public ulong GenerationTime = 5;
        private ulong produced = 0;

        public override void OnStart()
        {
            StartGenerating();
        }

        public void StartGenerating()
        {
            // Start infinite product generating event loop
            ulong delay = SubModel.GetSubModel().ApplyModelPrecision(GenerationTime);
            EventScheduler.ScheduleLocalEvent(0, delay, GenerateProduct);
        }

        private void GenerateProduct()
        {
            // Only move it when it can be moved to the next entity
            if (Target.GetComponent<QueueBehavior>().InputOpen)
            {
                // Create the product
                SubModel subModel = SubModel.GetSubModel();
                Entity entity = subModel.CreateEntity(ConnectedEntity, $"Product{produced + 1}");
                entity.AddComponent<Product>();
                produced++;

                // Move the product to the next simulation object
                subModel.UpdateParentOnEntity(entity, Target);
            }

            // Re-schedule this function, creating a loop
            ulong delay = SubModel.GetSubModel().ApplyModelPrecision(GenerationTime);
            EventScheduler.ScheduleLocalEvent(0, delay, GenerateProduct);
        }
    }
}
