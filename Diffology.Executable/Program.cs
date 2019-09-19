using System;

namespace Diffology.Executable
{
    sealed class Program
    {
        static void Main(string[] args)
        {
            var merger = new Merger();
            merger.Sync(args[0]).Wait();
        }
    }
}
