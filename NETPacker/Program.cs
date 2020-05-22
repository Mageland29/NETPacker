using System;
using static Context;
using static Logger;

namespace NETPacker
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Welcome();
                Write($"Please drag and drop your file\n\n", TypeMessage.Debug);
                LoadModule(Console.ReadLine());
                PackerPhase();
                SaveModule();
            }
            else if (args.Length == 1)
            {
                Welcome();
                LoadModule(args[0]);
                PackerPhase();
                SaveModule();
            }
        }
    }
}
