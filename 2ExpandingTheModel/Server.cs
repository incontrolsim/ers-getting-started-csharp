using System;
using Ers;

namespace ExpandingTheModel
{
    internal struct ServerProcessEvent : ILocalEvent<ServerProcessEvent>
    {
        public Entity ServerEntity;

        public void OnEvent()
        {
            SubModel subModel = SubModel.Get();
            ServerBehavior server = ServerEntity.GetComponent<ServerBehavior>();

            if (server.Target.GetComponent<SinkBehavior>().InputOpen)
            {
                // Product can be moved, process it and move it
                Entity child = ServerEntity.GetComponent<RelationComponent>().Value.First;
                child.GetComponent<Product>().Value.Filled = true;
                subModel.UpdateParentOnEntity(child, server.Target);
            }
            else
            {
                // Could not move the product, retry
                server.Process();
            }
        }
    }

    internal class ServerBehavior : ScriptBehaviorComponent
    {
        public double BaseProcessTime = 10.0;

        public Entity From;
        public Entity Target;

        public bool InputOpen = true;

        public override void OnEntered(Entity newChild)
        {
            Logger.Debug("Server received {0}", newChild.GetName());
            InputOpen = false;
            Process();
        }

        public override void OnExited(Entity newChild)
        {
            QueueBehavior queue = From.GetComponent<QueueBehavior>();
            InputOpen = true;
            queue.MoveNext();
        }

        public void Process()
        {
            SubModel subModel = SubModel.Get();

            // Create random process time values
            double processTime = subModel.SampleRandomGenerator() * 2.0 - 1.0 + BaseProcessTime;
            ulong unitProcessTime = (ulong)(processTime * subModel.ModelPrecision);

            Logger.Debug("Server process delay: {0:F2}s", processTime);
            EventScheduler.ScheduleLocalEvent(0, unitProcessTime, new ServerProcessEvent()
            {
                ServerEntity = ConnectedEntity
            });
        }
    }
}
