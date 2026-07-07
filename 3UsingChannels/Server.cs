using System;
using Ers;

namespace UsingChannels
{
    internal struct ServerProcessEvent : ILocalEvent<ServerProcessEvent>
    {
        public Entity ServerEntity;

        public void OnEvent()
        {
            ServerBehavior server = ServerEntity.GetComponent<ServerBehavior>();

            Entity output = ServerEntity.GetComponent<ResourceComponent>().Value.GetOutputChannel(0);
            if (ChannelComponent.IsReady(output))
            {
                // Product can be moved, process it and move it
                Entity toSend = server.CurrentlyProcessing;
                server.CurrentlyProcessing = CEntity.InvalidEntity();

                toSend.GetComponent<Product>().Value.Filled = true;
                ChannelComponent.Send(output, toSend);

                Entity input = ServerEntity.GetComponent<ResourceComponent>().Value.GetInputChannel(0);
                ChannelComponent.Open(input);
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
        public Entity CurrentlyProcessing = CEntity.InvalidEntity();

        public override void OnStart()
        {
            Ref<ResourceComponent> resource = ConnectedEntity.GetComponent<ResourceComponent>();
            Entity input = resource.Value.GetInputChannel(0);
            Entity output = resource.Value.GetOutputChannel(0);

            ChannelComponent.Open(input);
            ChannelComponent.Open(output);
        }

        public override void OnReceive(Entity inputChannel, Entity child)
        {
            Logger.Debug("Server received {0}", child.GetName());

            CurrentlyProcessing = child;
            Entity input = ConnectedEntity.GetComponent<ResourceComponent>().Value.GetInputChannel(0);
            ChannelComponent.Close(input);
            Process();
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
