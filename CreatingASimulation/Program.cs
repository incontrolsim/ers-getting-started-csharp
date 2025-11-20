using Ers;

namespace GettingStarted
{
    class SourceBehavior : ScriptBehaviorComponent
    {
        // The entity to which products are sent.
        public Entity Target;
        public ulong GenerationTime = 5;
        private ulong produced = 0;

        public override void OnStart()
        {
            // Start infinite product generating event loop
            ulong delay = SubModel.GetSubModel().ApplyModelPrecision(GenerationTime);
            EventScheduler.ScheduleLocalEvent(0, delay, GenerateProduct);
        }

        private void GenerateProduct()
        {
            // Create the product
            SubModel subModel = SubModel.GetSubModel();
            Entity entity = subModel.CreateEntity(ConnectedEntity, $"Product{produced + 1}");
            produced++;

            // Move the product to the next simulation object
            subModel.UpdateParentOnEntity(entity, Target);

            // Re-schedule this function, creating a loop
            ulong delay = subModel.ApplyModelPrecision(GenerationTime);
            EventScheduler.ScheduleLocalEvent(0, delay, GenerateProduct);
        }
    }

    class SinkBehavior : ScriptBehaviorComponent
    {
        public ulong Received = 0;

        public override void OnEntered(Entity newChild)
        {
            Received++;
            SubModel.GetSubModel().DestroyEntity(newChild);
        }
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            ERS.Initialize();

            ModelContainer modelContainer = ModelContainer.CreateModelContainer();
            modelContainer.SetPrecision(1_000_000);
            Simulator sim = modelContainer.AddSimulator("Sim1", SimulatorType.DiscreteEvent);
            sim.EnterSubModel();

            SubModel subModel = SubModel.GetSubModel();
            subModel.AddComponentType<SourceBehavior>();
            subModel.AddComponentType<SinkBehavior>();

            Entity sourceEntity = subModel.CreateEntity("Source");
            SourceBehavior source = sourceEntity.AddComponent<SourceBehavior>();

            Entity sinkEntity = subModel.CreateEntity("Sink");
            SinkBehavior sink = sinkEntity.AddComponent<SinkBehavior>();

            source.Target = sinkEntity;

            sim.ExitSubModel();

            ulong endTime = 60 * modelContainer.GetPrecision();
            while (modelContainer.CurrentTime < endTime)
            {
                modelContainer.Update(modelContainer.GetPrecision());
            }
            Logger.Info($"Sink received {sink.Received} products");

            ERS.Uninitialize();
        }
    }
}
