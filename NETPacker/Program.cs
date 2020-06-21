using System;
using static NETPacker.Context;
using static NETPacker.Logger;

namespace NETPacker
{
    internal static class Program
    {
        static void Main( string[] args )
        {
            switch ( args.Length )
            {
                case 0:
                    Welcome();
                    Write( $"Please drag and drop your file\n\n", TypeMessage.Debug );
                    LoadModule( Console.ReadLine() );
                    PackerPhase();
                    SaveModule();
                    break;
                case 1:
                    Welcome();
                    LoadModule( args[0] );
                    PackerPhase();
                    SaveModule();
                    break;
                default:
                    Write( $"Too many arguments, only one argument allowed", TypeMessage.Error );
                    break;
            }
        }
    }
}