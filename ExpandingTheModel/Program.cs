using Ers;

namespace ExpandingTheModel
{
    internal class Program
    {
        static void Main(string[] args)
        {
            ERS.Initialize();

            Logger.SetLogLevel(LogLevel.Info);

            ModelContainer modelContainer = ModelContainer.CreateModelContainer();
            modelContainer.SetPrecision(1_000_000);
            modelContainer.SetSeed(1);
            Simulator sim = modelContainer.AddSimulator("Sim1", SimulatorType.DiscreteEvent);
            sim.EnterSubModel();

            SubModel subModel = SubModel.GetSubModel();
            // Register the custom components
            subModel.AddComponentType<SourceBehavior>();
            subModel.AddComponentType<QueueBehavior>();
            subModel.AddComponentType<ServerBehavior>();
            subModel.AddComponentType<SinkBehavior>();
            subModel.AddComponentType<Product>();

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

            ulong endTime = 3600 * modelContainer.GetPrecision();
            while (modelContainer.CurrentTime < endTime)
            {
                modelContainer.Update(modelContainer.GetPrecision());
            }
            Logger.Info($"Sink received {sink.Received} products");

            ERS.Uninitialize();
        }
    }
}
