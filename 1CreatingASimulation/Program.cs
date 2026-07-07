using Ers;

namespace GettingStarted
{
    struct GenerateProductEvent : ILocalEvent<GenerateProductEvent>
    {
        public Entity SourceEntity;

        public void OnEvent()
        {
            SubModel subModel = SubModel.Get();
            SourceBehavior source = SourceEntity.GetComponent<SourceBehavior>();

            // Create the product
            Entity entity = subModel.CreateEntity(SourceEntity, $"Product{source.Produced + 1}");
            source.Produced++;

            // Move the product to the next simulation object
            subModel.UpdateParentOnEntity(entity, source.Target);

            // Re-schedule this function, creating a loop
            ulong delay = subModel.ApplyModelPrecision(source.GenerationTime);
            EventScheduler.ScheduleLocalEvent(0, delay, new GenerateProductEvent()
            {
                SourceEntity = SourceEntity
            });
        }
    }

    class SourceBehavior : ScriptBehaviorComponent
    {
        // The entity to which products are sent.
        public Entity Target;
        public ulong GenerationTime = 5;
        public ulong Produced = 0;

        public override void OnStart()
        {
            // Start infinite product generating event loop
            ulong delay = SubModel.Get().ApplyModelPrecision(GenerationTime);
            EventScheduler.ScheduleLocalEvent(0, delay, new GenerateProductEvent()
            {
                SourceEntity = ConnectedEntity
            });
        }
    }

    class SinkBehavior : ScriptBehaviorComponent
    {
        public ulong Received = 0;

        public override void OnEntered(Entity newChild)
        {
            Received++;
            SubModel.Get().DestroyEntity(newChild);
        }
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            ERS.Initialize();

            // Register the custom components
            ComponentRegistry<SourceBehavior>.Register();
            ComponentRegistry<SinkBehavior>.Register();

            // Register the local events
            LocalEventRegistry<GenerateProductEvent>.Register();

            ModelContainer modelContainer = ModelContainer.Create();
            modelContainer.Precision = 1_000_000;
            Simulator sim = modelContainer.AddSimulator("Sim1", SimulatorType.DiscreteEvent);
            sim.EnterSubModel();

            SubModel subModel = SubModel.Get();

            Entity sourceEntity = subModel.CreateEntity("Source");
            SourceBehavior source = sourceEntity.AddComponent<SourceBehavior>();

            Entity sinkEntity = subModel.CreateEntity("Sink");
            SinkBehavior sink = sinkEntity.AddComponent<SinkBehavior>();

            source.Target = sinkEntity;

            sim.ExitSubModel();

            ulong endTime = 60 * modelContainer.Precision;
            while (modelContainer.CurrentTime < endTime)
            {
                modelContainer.Update(modelContainer.Precision);
            }
            Logger.Info($"Sink received {sink.Received} products");

            ERS.Uninitialize();
        }
    }
}
