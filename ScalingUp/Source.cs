using System;
using Ers;

namespace ScalingUp
{
    internal class SourceBehavior : ScriptBehaviorComponent
    {
        public ulong GenerationTime = 5;

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
            // Only create a product when it can be moved to the next entity
            var channel = ConnectedEntity.GetComponent<Channel>();
            if (channel.Value.ToEntity.GetComponent<Channel>().Value.InputOpen)
            {
                // Create the product
                SubModel subModel = SubModel.GetSubModel();
                Entity entity = subModel.CreateEntity(ConnectedEntity, $"Product{channel.Value.Seen + 1}");
                entity.AddComponent<Product>();
                channel.Value.Seen++;

                // Move the product to the next simulation object
                subModel.UpdateParentOnEntity(entity, channel.Value.ToEntity);
            }

            // Re-schedule this function, creating a loop
            ulong delay = SubModel.GetSubModel().ApplyModelPrecision(GenerationTime);
            EventScheduler.ScheduleLocalEvent(0, delay, GenerateProduct);
        }
    }
}
