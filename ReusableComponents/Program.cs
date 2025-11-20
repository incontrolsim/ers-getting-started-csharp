using Ers;

namespace ReusableComponents
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
            subModel.AddComponentType<Channel>();

            // Create the simulation objects
            Entity sourceEntity = subModel.CreateEntity("Source");
            sourceEntity.AddComponent<SourceBehavior>();
            sourceEntity.AddComponent<Channel>();

            Entity queueEntity = subModel.CreateEntity("Queue");
            QueueBehavior queue = queueEntity.AddComponent<QueueBehavior>();
            queueEntity.AddComponent<Channel>();

            Entity serverEntity = subModel.CreateEntity("Server");
            ServerBehavior server = serverEntity.AddComponent<ServerBehavior>();
            serverEntity.AddComponent<Channel>();

            Entity sinkEntity = subModel.CreateEntity("Sink");
            sinkEntity.AddComponent<SinkBehavior>();
            sinkEntity.AddComponent<Channel>();

            // Important to get the components after adding them all
            var sourceChannel = sourceEntity.GetComponent<Channel>();
            var queueChannel = queueEntity.GetComponent<Channel>();
            var serverChannel = serverEntity.GetComponent<Channel>();
            var sinkChannel = sinkEntity.GetComponent<Channel>();

            // Connect the simulation objects
            sourceChannel.Value.ToEntity = queueEntity;
            queueChannel.Value.FromEntity = sourceEntity;
            queueChannel.Value.ToEntity = serverEntity;
            serverChannel.Value.FromEntity = queueEntity;
            serverChannel.Value.ToEntity = sinkEntity;
            sinkChannel.Value.FromEntity = serverEntity;
            server.PullNext = queue.MoveNext;

            sim.ExitSubModel();

            ulong endTime = 3600 * modelContainer.GetPrecision();
            while (modelContainer.CurrentTime < endTime)
            {
                modelContainer.Update(modelContainer.GetPrecision());
            }

            // Log how many products each of the simulation objects saw
            sim.EnterSubModel();
            var seenView = subModel.GetView<Channel>([]);
            while (seenView.Next())
            {
                Entity entity = seenView.GetEntity();
                var channel = seenView.GetComponent<Channel>();
                Logger.Info($"{entity.GetName()} saw {channel.Value.Seen} products");
            }
            seenView.Dispose();
            sim.ExitSubModel();

            ERS.Uninitialize();
        }
    }
}
