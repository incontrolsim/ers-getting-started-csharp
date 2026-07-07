using Ers;

namespace ReusableComponents
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
            ComponentRegistry<ProductTracker>.Register();

            // Register the local events
            LocalEventRegistry<GenerateProductEvent>.Register();
            LocalEventRegistry<ServerProcessEvent>.Register();

            ModelContainer modelContainer = ModelContainer.Create();
            modelContainer.Precision = 1_000_000;
            modelContainer.Seed = 1;
            Simulator sim = modelContainer.AddSimulator("Sim1", SimulatorType.DiscreteEvent);
            sim.EnterSubModel();

            // Create the simulation objects
            (_, Entity sourceOutputEntity) = SourceBehavior.Create();
            (_, Entity queueInputEntity, Entity queueOutputEntity) = QueueBehavior.Create();
            (_, Entity serverInputEntity, Entity serverOutputEntity) = ServerBehavior.Create();
            (_, Entity sinkInputEntity) = SinkBehavior.Create();

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

            sim.EnterSubModel();
            var trackerView = SubModel.Get().GetView<ProductTracker>([]);
            while (trackerView.Next())
            {
                Entity entity = trackerView.GetEntity();
                var tracker = trackerView.GetComponent<ProductTracker>();
                Logger.Info($"{entity.GetName()} saw {tracker.Value.Seen} products");
            }
            trackerView.Dispose();
            sim.ExitSubModel();

            ERS.Uninitialize();
        }
    }
}
