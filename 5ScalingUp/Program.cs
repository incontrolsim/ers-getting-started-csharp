using Ers;

namespace ScalingUp
{
    internal class SinkContext
    {
        public Entity ReceiverEntity;
    }

    internal class Program
    {
        static Simulator CreateSinkSubModel(ModelContainer modelContainer)
        {
            Simulator sim = modelContainer.AddSimulator("SinkSim", SimulatorType.DiscreteEvent);
            sim.EnterSubModel();

            SubModel subModel = SubModel.Get();

            // Create simulation objects
            (SyncReceiver receiver, Entity receiverOutputEntity) = SyncReceiver.Create();
            (_, Entity sinkInputEntity) = SinkBehavior.Create();

            // Connect simulation objects
            ChannelComponent.Connect(receiverOutputEntity, sinkInputEntity);

            var context = subModel.AddSubModelContext<SinkContext>();
            context.ReceiverEntity = receiver.ConnectedEntity;

            sim.ExitSubModel();
            return sim;
        }

        static Simulator AddSubModel(ModelContainer modelContainer, Simulator sinkSim, string name)
        {
            Simulator sim = modelContainer.AddSimulator(name, SimulatorType.DiscreteEvent);
            sim.EnterSubModel();

            // Create the simulation objects
            (_, Entity sourceOutputEntity) = SourceBehavior.Create();
            (_, Entity queueInputEntity, Entity queueOutputEntity) = QueueBehavior.Create();
            (_, Entity serverInputEntity, Entity serverOutputEntity) = ServerBehavior.Create();
            (SyncSenderBehavior sender, Entity senderInputEntity) = SyncSenderBehavior.Create();

            // Configure simulation objects
            sender.TargetSimId = sinkSim.ID;

            // Connect the simulation objects
            ChannelComponent.Connect(sourceOutputEntity, queueInputEntity);
            ChannelComponent.Connect(queueOutputEntity, serverInputEntity);
            ChannelComponent.Connect(serverOutputEntity, senderInputEntity);

            sim.ExitSubModel();

            return sim;
        }

        static void LogResult(in Simulator sim)
        {
            Logger.Info($"----------[{sim.Name}]----------");

            // Log how many products each of the simulation objects saw
            sim.EnterSubModel();
            SubModel subModel = SubModel.Get();

            var trackerView = subModel.GetView<ProductTracker>([]);
            while (trackerView.Next())
            {
                Entity entity = trackerView.GetEntity();
                var tracker = trackerView.GetComponent<ProductTracker>();
                Logger.Info($"{entity.GetName()} saw {tracker.Value.Seen} products");
            }
            trackerView.Dispose();

            sim.ExitSubModel();
        }

        static void Main(string[] args)
        {
            ERS.Initialize();

            Logger.SetLogLevel(LogLevel.Info);

            // Register the custom components
            ComponentRegistry<SourceBehavior>.Register();
            ComponentRegistry<QueueBehavior>.Register();
            ComponentRegistry<ServerBehavior>.Register();
            ComponentRegistry<SinkBehavior>.Register();
            ComponentRegistry<SyncSenderBehavior>.Register();
            ComponentRegistry<SyncReceiver>.Register();
            ComponentRegistry<Product>.Register();
            ComponentRegistry<ProductTracker>.Register();

            // Register the local events
            LocalEventRegistry<GenerateProductEvent>.Register();
            LocalEventRegistry<ServerProcessEvent>.Register();

            // Register the sync events
            SyncEventRegistry<SendEntitySyncEvent>.Register();

            ModelContainer modelContainer = ModelContainer.Create();
            modelContainer.Precision = 1_000_000;
            modelContainer.Seed = 1;

            Simulator sinkSim = CreateSinkSubModel(modelContainer);
            Simulator sim1 = AddSubModel(modelContainer, sinkSim, "Sim1");
            Simulator sim2 = AddSubModel(modelContainer, sinkSim, "Sim2");

            // Set the dependencies between simulators
            modelContainer.AddSimulatorDependency(sim1, sinkSim);
            modelContainer.AddSimulatorDependency(sim2, sinkSim);

            // Set the promises between simulators
            sim1.EnterSubModel();
            EventScheduler.SetPromise(sinkSim.ID, 5 * modelContainer.Precision);
            sim1.ExitSubModel();
            sim2.EnterSubModel();
            EventScheduler.SetPromise(sinkSim.ID, 5 * modelContainer.Precision);
            sim2.ExitSubModel();

            ulong endTime = 3600 * modelContainer.Precision;
            while (modelContainer.CurrentTime < endTime)
            {
                modelContainer.Update(modelContainer.Precision);
            }

            LogResult(sim1);
            LogResult(sim2);
            LogResult(sinkSim);

            ERS.Uninitialize();
        }
    }
}
