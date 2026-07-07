using System;
using Ers;

namespace ScalingUp
{
    internal class QueueBehavior : ScriptBehaviorComponent
    {
        public ulong Capacity = 5;
        private readonly Queue<Entity> queue = new();

        public static (QueueBehavior, Entity, Entity) Create(string name = "Queue", bool addTracker = true)
        {
            SubModel subModel = SubModel.Get();

            Entity queueEntity = subModel.CreateEntity(name);
            QueueBehavior queue = queueEntity.AddComponent<QueueBehavior>();

            // Channels
            queueEntity.AddComponent<ResourceComponent>();
            Entity queueInputEntity = subModel.CreateEntity(queueEntity, $"{name}-Input");
            Entity queueOutputEntity = subModel.CreateEntity(queueEntity, $"{name}-Output");
            ChannelComponent.AddChannelComponent(queueInputEntity, ChannelType.Input, queueEntity);
            ChannelComponent.AddChannelComponent(queueOutputEntity, ChannelType.Output, queueEntity);

            if (addTracker)
                queueEntity.AddComponent<ProductTracker>();

            return (queue, queueInputEntity, queueOutputEntity);
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
            Logger.Debug("Queue received {0}", child.GetName());

            // If the entity also has a ProductTracker component, track the products
            if (ConnectedEntity.HasComponent<ProductTracker>())
            {
                ConnectedEntity.GetComponent<ProductTracker>().Value.Seen++;
            }

            queue.Enqueue(child);
            TrySend();

            if ((ulong)queue.Count >= Capacity)
            {
                Entity input = ConnectedEntity.GetComponent<ResourceComponent>().Value.GetInputChannel(0);
                ChannelComponent.Close(input);
            }
        }

        public override void OnOutputChannelReady(Entity outputChannel)
        {
            // If the queue is empty, do nothing and return
            if (queue.Count == 0)
                return;

            ChannelComponent.Send(outputChannel, queue.Dequeue());

            Entity input = ConnectedEntity.GetComponent<ResourceComponent>().Value.GetInputChannel(0);
            ChannelComponent.Open(input);
        }

        public void TrySend()
        {
            Entity output = ConnectedEntity.GetComponent<ResourceComponent>().Value.GetOutputChannel(0);
            if (ChannelComponent.IsReady(output) && queue.Count > 0)
            {
                ChannelComponent.Send(output, queue.Dequeue());

                Entity input = ConnectedEntity.GetComponent<ResourceComponent>().Value.GetInputChannel(0);
                ChannelComponent.Open(input);
            }
        }
    }
}
