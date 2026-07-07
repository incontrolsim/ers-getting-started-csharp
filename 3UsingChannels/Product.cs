using System;
using System.Runtime.InteropServices;
using Ers;

namespace UsingChannels
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
