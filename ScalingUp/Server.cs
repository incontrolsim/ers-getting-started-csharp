using System;
using Ers;

namespace ScalingUp
{
    internal class ServerBehavior : ScriptBehaviorComponent
    {
        public double BaseProcessTime = 10.0;
        public Action? PullNext;

        public override void OnEntered(ulong newChild)
        {
            Logger.Debug("Server received {0}", newChild.GetName());
            var channel = ConnectedEntity.GetComponent<Channel>();
            channel.Value.InputOpen = false;
            channel.Value.Seen++;
            Process();
        }

        public override void OnExited(ulong newChild)
        {
            var channel = ConnectedEntity.GetComponent<Channel>();
            channel.Value.InputOpen = true;
            PullNext?.Invoke(); // Invoke when not null
        }

        private void Process()
        {
            SubModel subModel = SubModel.GetSubModel();

            // Create random process time values between 9 and 11
            double processTime = SubModel.GetSubModel().SampleRandomGenerator() * 2.0 - 1.0 + BaseProcessTime;
            ulong unitProcessTime = (ulong)(processTime * subModel.ModelPrecision);

            Logger.Debug("Server process delay: {0:F2}s", processTime);
            EventScheduler.ScheduleLocalEvent(0, unitProcessTime, () =>
            {
                Entity target = ConnectedEntity.GetComponent<Channel>().Value.ToEntity;
                if (target.GetComponent<Channel>().Value.InputOpen)
                {
                    // Product can be moved, process it and move it
                    Entity child = ConnectedEntity.GetComponent<RelationComponent>().Value.First();
                    child.GetComponent<Product>().Value.Filled = true;
                    SubModel.GetSubModel().UpdateParentOnEntity(child, target);
                }
                else
                {
                    // Could not move the product, retry
                    Process();
                }
            });
        }
    }
}
