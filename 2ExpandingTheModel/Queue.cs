using System;
using Ers;

namespace ExpandingTheModel
{
    internal class QueueBehavior : ScriptBehaviorComponent
    {
        public Entity Target;
        public ulong Capacity = 5;
        public bool InputOpen = true;

        public override void OnEntered(Entity newChild)
        {
            Logger.Debug("Queue received {0}", newChild.GetName());

            if (ConnectedEntity.GetComponent<RelationComponent>().Value.ChildCount >= Capacity)
                InputOpen = false;

            MoveNext();
        }

        public override void OnExited(Entity oldChild)
        {
            InputOpen = true;
        }

        public void MoveNext()
        {
            // If the queue is empty, do nothing and return
            var relation = ConnectedEntity.GetComponent<RelationComponent>();
            if (relation.Value.ChildCount == 0)
                return;

            // Only move an entity out of the queue if the target is ready to receive it
            if (Target.GetComponent<ServerBehavior>().InputOpen)
            {
                Entity first = relation.Value.First;
                SubModel.Get().UpdateParentOnEntity(first, Target);
            }
        }
    }
}
