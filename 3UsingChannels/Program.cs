using Ers;

namespace UsingChannels
{
    internal class Program
    {
        static void Main(string[] args)
        {
            ERS.Initialize();

            Logger.SetLogLevel(LogLevel.Info);

            // Register the custom components
            ComponentRegistry<SourceBehavior>.Register();
            ComponentRegistry<QueueBehavior>.Register();
            ComponentRegistry<ServerBehavior>.Register();
            ComponentRegistry<SinkBehavior>.Register();
            ComponentRegistry<Product>.Register();

            // Register the local events
            LocalEventRegistry<GenerateProductEvent>.Register();
            LocalEventRegistry<ServerProcessEvent>.Register();

            ModelContainer modelContainer = ModelContainer.Create();
            modelContainer.Precision = 1_000_000;
            modelContainer.Seed = 1;
            Simulator sim = modelContainer.AddSimulator("Sim1", SimulatorType.DiscreteEvent);
            sim.EnterSubModel();

            SubModel subModel = SubModel.Get();

            // Create the simulation objects
            Entity sourceEntity = subModel.CreateEntity("Source");
            SourceBehavior source = sourceEntity.AddComponent<SourceBehavior>();
            sourceEntity.AddComponent<ResourceComponent>();
            Entity sourceOutputEntity = subModel.CreateEntity(sourceEntity, "SourceOutput");
            ChannelComponent.AddChannelComponent(sourceOutputEntity, ChannelType.Output, sourceEntity);

            Entity queueEntity = subModel.CreateEntity("Queue");
            queueEntity.AddComponent<QueueBehavior>();
            queueEntity.AddComponent<ResourceComponent>();
            Entity queueInputEntity = subModel.CreateEntity(queueEntity, "QueueInput");
            Entity queueOutputEntity = subModel.CreateEntity(queueEntity, "QueueOutput");
            ChannelComponent.AddChannelComponent(queueInputEntity, ChannelType.Input, queueEntity);
            ChannelComponent.AddChannelComponent(queueOutputEntity, ChannelType.Output, queueEntity);

            Entity serverEntity = subModel.CreateEntity("Server");
            serverEntity.AddComponent<ServerBehavior>();
            serverEntity.AddComponent<ResourceComponent>();
            Entity serverInputEntity = subModel.CreateEntity(serverEntity, "ServerInput");
            Entity serverOutputEntity = subModel.CreateEntity(serverEntity, "ServerOutput");
            ChannelComponent.AddChannelComponent(serverInputEntity, ChannelType.Input, serverEntity);
            ChannelComponent.AddChannelComponent(serverOutputEntity, ChannelType.Output, serverEntity);

            Entity sinkEntity = subModel.CreateEntity("Sink");
            SinkBehavior sink = sinkEntity.AddComponent<SinkBehavior>();
            sinkEntity.AddComponent<ResourceComponent>();
            Entity sinkInputEntity = subModel.CreateEntity(sinkEntity, "SinkInput");
            ChannelComponent.AddChannelComponent(sinkInputEntity, ChannelType.Input, sinkEntity);

            // Connect the simulation objects
            ChannelComponent.Connect(sourceOutputEntity, queueInputEntity);
            ChannelComponent.Connect(queueOutputEntity, serverInputEntity);
            ChannelComponent.Connect(serverOutputEntity, sinkInputEntity);

            sim.ExitSubModel();

            ulong endTime = 3600 * modelContainer.Precision;
            while (modelContainer.CurrentTime < endTime)
            {
                modelContainer.Update(modelContainer.Precision);
            }
            Logger.Info($"Sink received {sink.Received} products");

            ERS.Uninitialize();
        }
    }
}
