using Ers;

namespace ExpandingTheModel
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

            Entity queueEntity = subModel.CreateEntity("Queue");
            QueueBehavior queue = queueEntity.AddComponent<QueueBehavior>();

            Entity serverEntity = subModel.CreateEntity("Server");
            ServerBehavior server = serverEntity.AddComponent<ServerBehavior>();

            Entity sinkEntity = subModel.CreateEntity("Sink");
            SinkBehavior sink = sinkEntity.AddComponent<SinkBehavior>();

            // Connect the simulation objects
            source.Target = queueEntity;
            queue.Target = serverEntity;
            server.From = queueEntity;
            server.Target = sinkEntity;

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
