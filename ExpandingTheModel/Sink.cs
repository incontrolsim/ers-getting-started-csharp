using System;
using Ers;

namespace ExpandingTheModel
{
    internal class SinkBehavior : ScriptBehaviorComponent
    {
        public ulong Received = 0;
        public bool InputOpen = true;

        public override void OnEntered(Entity newChild)
        {
            Received++;
            SubModel.GetSubModel().DestroyEntity(newChild);
        }
    }
}
