using System;
using Ers;

namespace ExpandingTheModel
{
    internal class ServerBehavior : ScriptBehaviorComponent
    {
        public double BaseProcessTime = 10.0;

        public Entity From;
        public Entity Target;

        public bool InputOpen = true;

        public override void OnEntered(ulong newChild)
        {
            Logger.Debug("Server received {0}", newChild.GetName());
            InputOpen = false;
            Process();
        }

        public override void OnExited(ulong newChild)
        {
            QueueBehavior queue = From.GetComponent<QueueBehavior>();
            InputOpen = true;
            queue.MoveNext();
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
                if (Target.GetComponent<SinkBehavior>().InputOpen)
                {
                    // Product can be moved, process it and move it
                    Entity child = ConnectedEntity.GetComponent<RelationComponent>().Value.First();
                    child.GetComponent<Product>().Value.Filled = true;
                    SubModel.GetSubModel().UpdateParentOnEntity(child, Target);
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
