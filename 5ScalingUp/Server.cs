using System;
using Ers;

namespace ScalingUp
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

        public static (ServerBehavior, Entity, Entity) Create(string name = "Server", bool addTracker = true)
        {
            SubModel subModel = SubModel.Get();

            Entity serverEntity = subModel.CreateEntity(name);
            ServerBehavior server = serverEntity.AddComponent<ServerBehavior>();

            // Channels
            serverEntity.AddComponent<ResourceComponent>();
            Entity serverInputEntity = subModel.CreateEntity(serverEntity, $"{name}-Input");
            Entity serverOutputEntity = subModel.CreateEntity(serverEntity, $"${name}-Output");
            ChannelComponent.AddChannelComponent(serverInputEntity, ChannelType.Input, serverEntity);
            ChannelComponent.AddChannelComponent(serverOutputEntity, ChannelType.Output, serverEntity);

            if (addTracker)
                serverEntity.AddComponent<ProductTracker>();

            return (server, serverInputEntity, serverOutputEntity);
        }

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

            // If the entity also has a ProductTracker component, track the products
            if (ConnectedEntity.HasComponent<ProductTracker>())
            {
                ConnectedEntity.GetComponent<ProductTracker>().Value.Seen++;
            }

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
