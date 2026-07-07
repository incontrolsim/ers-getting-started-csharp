using System;
using Ers;

namespace UsingChannels
{
    internal class SinkBehavior : ScriptBehaviorComponent
    {
        public ulong Received = 0;

        public override void OnStart()
        {
            Ref<ResourceComponent> resource = ConnectedEntity.GetComponent<ResourceComponent>();
            Entity input = resource.Value.GetInputChannel(0);
            ChannelComponent.Open(input);
        }

        public override void OnReceive(Entity inputChannel, Entity child)
        {
            Received++;
            SubModel.Get().DestroyEntity(child);
        }
    }
}
