using System;
using System.Runtime.InteropServices;
using Ers;

namespace ScalingUp
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct Product : IDataComponent
    {
        public bool Filled = false;

        public Product()
        {
        }
    }
}
