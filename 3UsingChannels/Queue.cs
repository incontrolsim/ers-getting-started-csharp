using System;
using Ers;

namespace UsingChannels
{
    internal class QueueBehavior : ScriptBehaviorComponent
    {
        public ulong Capacity = 5;
        private readonly Queue<Entity> queue = new();

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
