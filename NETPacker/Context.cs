using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;
using static NETPacker.Logger;

namespace NETPacker
{
    public static class Context
    {
        private static readonly Random random = new Random();
        private static ModuleDefMD module = null;
        private static string FileName = null;

        public static void LoadModule( string filename )
        {
            try
            {
                FileName = filename;
                var data = File.ReadAllBytes( filename );
                var modCtx = ModuleDef.CreateModuleContext();
                module = ModuleDefMD.Load( data, modCtx );
                Write( "Module Loaded : " + module.Name, TypeMessage.Info );
                foreach ( var dependency in module.GetAssemblyRefs() )
                {
                    Write( $"Dependency : {dependency.Name}", TypeMessage.Info );
                }
            }
            catch
            {
                Write( "Error while loading Module", TypeMessage.Error );
            }
        }

        public static void SaveModule()
        {
            try
            {
                var filename = string.Concat( new string[]
                {
                    Path.GetDirectoryName( FileName ), "\\", Path.GetFileNameWithoutExtension( FileName ), "_Packed",
                    Path.GetExtension( FileName )
                } );
                if ( module.IsILOnly )
                {
                    var writer = new ModuleWriterOptions( module )
                    {
                        MetadataOptions = {Flags = MetadataFlags.PreserveAll},
                        MetadataLogger = DummyLogger.NoThrowInstance
                    };
                    module.Write( filename, writer );
                }
                else
                {
                    var writer = new NativeModuleWriterOptions( module, true )
                    {
                        MetadataOptions = {Flags = MetadataFlags.PreserveAll},
                        MetadataLogger = DummyLogger.NoThrowInstance
                    };
                    module.NativeWrite( filename, writer );
                }

                Write( "File Packed and Saved : " + filename, TypeMessage.Done );
            }
            catch ( ModuleWriterException ex )
            {
                Write( "Failed to save current module\n" + ex.ToString(), TypeMessage.Error );
            }
            Console.ReadLine();
        }
        
        //Unused method however dont know if it will be used in the future
        public static byte[] Compress( byte[] data )
        {
            using ( var destination = new MemoryStream() )
            using ( var deflateStream = new DeflateStream( destination, CompressionLevel.Optimal ) )
            {
                deflateStream.Write( data, 0, data.Length );
                return destination.ToArray();
            }
        }

        public static void PackerPhase()
        {
            Write( "Packing in run...", TypeMessage.Debug );
            var stub = GetStubModule( Path.GetDirectoryName( Assembly.GetExecutingAssembly().Location ) + "\\Stub.exe" );
            var init = stub.EntryPoint;
            var randomResourceName = RandomString( 20 );
            var keyMethod = ( from op in init.Body.Instructions
                where op.OpCode == OpCodes.Ldc_I4 && op.GetLdcI4Value() == 123456789
                select op ).First();
            var resourcesName = ( from op in init.Body.Instructions
                where op.OpCode == OpCodes.Ldstr && op.Operand.ToString().Equals( "Name_Resource" )
                select op ).First();
            keyMethod.Operand = init.MDToken.ToInt32();
            resourcesName.Operand = randomResourceName;
            var key = Assembly.Load( GetCurrentModule( stub ) ).ManifestModule.ResolveMethod( init.MDToken.ToInt32() )
                .GetMethodBody()
                ?.GetILAsByteArray();
            stub.Resources.Add( new EmbeddedResource( randomResourceName, Encrypt( GetCurrentModule( module ), key ) ) );
            module = stub;
        }

        private static ModuleDefMD GetStubModule( string path )
        {
            var stub = ModuleDefMD.Load( path );
            stub.Characteristics = module.Characteristics;
            stub.Cor20HeaderFlags = module.Cor20HeaderFlags;
            stub.Cor20HeaderRuntimeVersion = module.Cor20HeaderRuntimeVersion;
            stub.DllCharacteristics = module.DllCharacteristics;
            stub.EncBaseId = module.EncBaseId;
            stub.EncId = module.EncId;
            stub.Generation = module.Generation;
            stub.Kind = module.Kind;
            stub.Machine = module.Machine;
            stub.RuntimeVersion = module.RuntimeVersion;
            stub.TablesHeaderVersion = module.TablesHeaderVersion;
            stub.Win32Resources = module.Win32Resources;

            return stub;
        }

        private static byte[] GetCurrentModule( ModuleDefMD module )
        {
            using ( var memoryStream = new MemoryStream() )
            {
                if ( module.IsILOnly )
                {
                    var writer = new ModuleWriterOptions( module )
                    {
                        MetadataOptions = {Flags = MetadataFlags.PreserveAll}, MetadataLogger = DummyLogger.NoThrowInstance
                    };
                    module.Write( memoryStream, writer );
                }
                else
                {
                    var writer = new NativeModuleWriterOptions( module, false )
                    {
                        MetadataOptions = {Flags = MetadataFlags.PreserveAll}, MetadataLogger = DummyLogger.NoThrowInstance
                    };
                    module.NativeWrite( memoryStream, writer );
                }

                var byteArray = new byte[memoryStream.Length];
                memoryStream.Position = 0;
                memoryStream.Read( byteArray, 0, (int) memoryStream.Length );
                return byteArray;
            }
        }

        private static byte[] Encrypt( byte[] plain, byte[] key )
        {
            for ( var round = 0; round < 5; round++ )
            {
                for ( var i = 0; i < plain.Length; i++ )
                {
                    plain[i] = (byte) ( plain[i] ^ key[i % key.Length] );
                    for ( var k = 0; k < key.Length; k++ )
                        plain[i] = (byte) ( plain[i] ^ ( ( ( ( key[k] << round ) ^ k ) + i ) ) );
                }
            }

            return plain;
        }

        private static string RandomString( int length )
        {
            return new string( ( from s in Enumerable.Repeat<string>( "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789", length )
                select s[Context.random.Next( s.Length )] ).ToArray<char>() );
        }

        public static void Welcome()
        {
            Console.Title = "NETPacker Console 1.0";
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine( @"Made By Sir-_-MaGeLanD#7358" );
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine( @"                         __          __  _                                  _______    " );
            Console.WriteLine( @"                         \ \        / / | |                                |__   __|   " );
            Console.WriteLine( @"                          \ \  /\  / /__| | ___ ___  _ __ ___   ___           | | ___  " );
            Console.WriteLine( @"                           \ \/  \/ / _ \ |/ __/ _ \| '_ ` _ \ / _ \          | |/ _ \ " );
            Console.WriteLine( @"                            \  /\  /  __/ | (_| (_) | | | | | |  __/          | | (_) |" );
            Console.WriteLine( @"                             \/  \/ \___|_|\___\___/|_| |_| |_|\___|          |_|\___/ " );
            Console.WriteLine( @"███╗   ██╗███████╗████████╗██████╗  █████╗  ██████╗██╗  ██╗███████╗██████╗ " );
            Console.WriteLine( @"████╗  ██║██╔════╝╚══██╔══╝██╔══██╗██╔══██╗██╔════╝██║ ██╔╝██╔════╝██╔══██╗" );
            Console.WriteLine( @"██╔██╗ ██║█████╗     ██║   ██████╔╝███████║██║     █████╔╝ █████╗  ██████╔╝" );
            Console.WriteLine( @"██║╚██╗██║██╔══╝     ██║   ██╔═══╝ ██╔══██║██║     ██╔═██╗ ██╔══╝  ██╔══██╗" );
            Console.WriteLine( @"██║ ╚████║███████╗   ██║   ██║     ██║  ██║╚██████╗██║  ██╗███████╗██║  ██║" );
            Console.WriteLine( @"╚═╝  ╚═══╝╚══════╝   ╚═╝   ╚═╝     ╚═╝  ╚═╝ ╚═════╝╚═╝  ╚═╝╚══════╝╚═╝  ╚═╝" );
            Console.Write( Environment.NewLine, Environment.NewLine, Environment.NewLine );
        }
    }
}