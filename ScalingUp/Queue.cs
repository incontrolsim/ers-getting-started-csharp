using System;
using Ers;

namespace ScalingUp
{
    internal class QueueBehavior : ScriptBehaviorComponent
    {
        public ulong Capacity = 5;

        public override void OnEntered(ulong newChild)
        {
            Logger.Debug("Queue received {0}", newChild.GetName());

            var channel = ConnectedEntity.GetComponent<Channel>();
            channel.Value.Seen++;
            if (ConnectedEntity.GetComponent<RelationComponent>().Value.ChildCount() >= Capacity)
                channel.Value.InputOpen = false;

            MoveNext();
        }

        public override void OnExited(ulong oldChild)
        {
            ConnectedEntity.GetComponent<Channel>().Value.InputOpen = true;
        }

        public void MoveNext()
        {
            // If the queue is empty, do nothing and return
            var relation = ConnectedEntity.GetComponent<RelationComponent>();
            if (relation.Value.ChildCount() == 0)
                return;

            // Only move an entity out of the queue if the target is ready to receive it
            var channel = ConnectedEntity.GetComponent<Channel>();
            if (channel.Value.ToEntity.GetComponent<Channel>().Value.InputOpen)
            {
                Entity first = relation.Value.First();
                SubModel.GetSubModel().UpdateParentOnEntity(first, channel.Value.ToEntity);
            }
        }
    }
}
