using System;
using System.Runtime.InteropServices;
using Ers;

namespace ReusableComponents
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct ProductTracker : IDataComponent
    {
        public ulong Seen = 0;

        public ProductTracker()
        {
            // C# requires a constructor here
        }
    }
}
