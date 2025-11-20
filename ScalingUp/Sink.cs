using System;
using Ers;

namespace ScalingUp
{
    internal class SinkBehavior : ScriptBehaviorComponent
    {
        public override void OnEntered(Entity newChild)
        {
            ConnectedEntity.GetComponent<Channel>().Value.Seen++;
            SubModel.GetSubModel().DestroyEntity(newChild);
        }
    }
}
