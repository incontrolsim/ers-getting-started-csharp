using Ers;

namespace ScalingUp
{
    internal class SinkContext
    {
        public Entity SinkEntity;
    }

    internal class Program
    {
        static Simulator CreateSinkSubModel(ModelContainer modelContainer)
        {
            Simulator sim = modelContainer.AddSimulator("SinkSim", SimulatorType.DiscreteEvent);
            sim.EnterSubModel();

            SubModel subModel = SubModel.GetSubModel();
            // Register the custom components
            subModel.AddComponentType<SinkBehavior>();
            subModel.AddComponentType<Channel>();

            Entity sinkEntity = subModel.CreateEntity("Sink");
            sinkEntity.AddComponent<SinkBehavior>();
            sinkEntity.AddComponent<Channel>();

            var context = subModel.AddSubModelContext<SinkContext>();
            context.SinkEntity = sinkEntity;

            return sim;
        }

        static Simulator AddSubModel(ModelContainer modelContainer, Simulator sinkSim, string name)
        {
            Simulator sim = modelContainer.AddSimulator(name, SimulatorType.DiscreteEvent);
            sim.EnterSubModel();

            SubModel subModel = SubModel.GetSubModel();
            // Register the custom components
            subModel.AddComponentType<SourceBehavior>();
            subModel.AddComponentType<QueueBehavior>();
            subModel.AddComponentType<ServerBehavior>();
            subModel.AddComponentType<SenderBehavior>();
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

            Entity senderEntity = subModel.CreateEntity("Sender");
            SenderBehavior sender = senderEntity.AddComponent<SenderBehavior>();
            senderEntity.AddComponent<Channel>();

            // Important to get the components after adding them all
            var sourceChannel = sourceEntity.GetComponent<Channel>();
            var queueChannel = queueEntity.GetComponent<Channel>();
            var serverChannel = serverEntity.GetComponent<Channel>();
            var senderChannel = senderEntity.GetComponent<Channel>();

            // Connect the simulation objects
            sourceChannel.Value.ToEntity = queueEntity;
            queueChannel.Value.FromEntity = sourceEntity;
            queueChannel.Value.ToEntity = serverEntity;
            serverChannel.Value.FromEntity = queueEntity;
            serverChannel.Value.ToEntity = senderEntity;
            senderChannel.Value.FromEntity = serverEntity;
            server.PullNext = queue.MoveNext;
            sender.TargetSimId = sinkSim.ID;

            sim.ExitSubModel();

            return sim;
        }

        static void LogResult(in Simulator sim)
        {
            Logger.Info($"----------[{sim.Name}]----------");

            // Log how many products each of the simulation objects saw
            sim.EnterSubModel();
            SubModel subModel = SubModel.GetSubModel();

            var seenView = subModel.GetView<Channel>([]);
            while (seenView.Next())
            {
                Entity entity = seenView.GetEntity();
                var channel = seenView.GetComponent<Channel>();
                Logger.Info($"{entity.GetName()} saw {channel.Value.Seen} products");
            }
            seenView.Dispose();

            sim.ExitSubModel();
        }

        static void Main(string[] args)
        {
            ERS.Initialize();

            Logger.SetLogLevel(LogLevel.Info);

            ModelContainer modelContainer = ModelContainer.CreateModelContainer();
            modelContainer.SetPrecision(1_000_000);
            modelContainer.SetSeed(1);

            Simulator sinkSim = CreateSinkSubModel(modelContainer);
            Simulator sim1 = AddSubModel(modelContainer, sinkSim, "Sim1");
            Simulator sim2 = AddSubModel(modelContainer, sinkSim, "Sim2");

            // Set the dependencies between simulators
            modelContainer.AddSimulatorDependency(sim1, sinkSim);
            modelContainer.AddSimulatorDependency(sim2, sinkSim);

            // Set the promises between simulators
            sim1.EnterSubModel();
            EventScheduler.SetPromise(sinkSim.ID, 5 * modelContainer.GetPrecision());
            sim1.ExitSubModel();
            sim2.EnterSubModel();
            EventScheduler.SetPromise(sinkSim.ID, 5 * modelContainer.GetPrecision());
            sim2.ExitSubModel();

            ulong endTime = 3600 * modelContainer.GetPrecision();
            while (modelContainer.CurrentTime < endTime)
            {
                modelContainer.Update(modelContainer.GetPrecision());
            }

            LogResult(sim1);
            LogResult(sim2);
            LogResult(sinkSim);

            ERS.Uninitialize();
        }
    }
}
