using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using static Logger;

public static class Context
{
    private static Random random = new Random();
    public static ModuleDefMD module = null;
    public static string FileName = null;
    public static void LoadModule(string filename)
    {
        try
        {
            FileName = filename;
            byte[] data = File.ReadAllBytes(filename);
            ModuleContext modCtx = ModuleDef.CreateModuleContext();
            module = ModuleDefMD.Load(data, modCtx);
            Write("Module Loaded : " + module.Name, TypeMessage.Info);
            foreach (AssemblyRef dependance in module.GetAssemblyRefs())
            {
                Write($"Dependance : {dependance.Name}", TypeMessage.Info);
            }
        }
        catch
        {
            Write("Error for Loade Module", TypeMessage.Error);
        }
    }
    public static void SaveModule()
    {
        try
        {
            string filename = string.Concat(new string[] { Path.GetDirectoryName(FileName), "\\", Path.GetFileNameWithoutExtension(FileName), "_Packed", Path.GetExtension(FileName) });
            if (module.IsILOnly)
            {
                ModuleWriterOptions writer = new ModuleWriterOptions(module);
                writer.MetaDataOptions.Flags = MetaDataFlags.PreserveAll;
                writer.MetaDataLogger = DummyLogger.NoThrowInstance;
                module.Write(filename, writer);
            }
            else
            {
                NativeModuleWriterOptions writer = new NativeModuleWriterOptions(module);
                writer.MetaDataOptions.Flags = MetaDataFlags.PreserveAll;
                writer.MetaDataLogger = DummyLogger.NoThrowInstance;
                module.NativeWrite(filename, writer);
            }
            Write("File Paked and Saved : " + filename, TypeMessage.Done);
        }
        catch (ModuleWriterException ex)
        {
            Write("Fail to save current module\n" + ex.ToString(), TypeMessage.Error);
        }
        Console.ReadLine();
    }
    public static byte[] Compress(byte[] data)
    {
        MemoryStream output = new MemoryStream();
        using (DeflateStream dstream = new DeflateStream(output, CompressionLevel.Optimal))
        {
            dstream.Write(data, 0, data.Length);
        }
        return output.ToArray();
    }
    public static void PackerPhase()
    {
        Write("Packing in run...", TypeMessage.Debug);
        ModuleDefMD stub = ModuleDefMD.Load(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\Stub.exe");
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
        MethodDef init = stub.EntryPoint;
        string random_resource_name = RandomString(20);
        Instruction Key_Method = (from op in init.Body.Instructions where op.OpCode == OpCodes.Ldc_I4 && op.GetLdcI4Value() == 123456789 select op).First();
        Instruction Resources_Name = (from op in init.Body.Instructions where op.OpCode == OpCodes.Ldstr && op.Operand.ToString().Equals("Name_Resource") select op).First();
        Key_Method.Operand = init.MDToken.ToInt32();
        Resources_Name.Operand = random_resource_name;
        byte[] Key = Assembly.Load(GetCurrentModule(stub)).ManifestModule.ResolveMethod(init.MDToken.ToInt32()).GetMethodBody().GetILAsByteArray();
        stub.Resources.Add(new EmbeddedResource(random_resource_name, Encrypt(GetCurrentModule(module), Key)));
        module = stub;
    }
    public static byte[] GetCurrentModule(ModuleDefMD module)
    {
        MemoryStream memorystream = new MemoryStream();
        if (module.IsILOnly)
        {
            ModuleWriterOptions writer = new ModuleWriterOptions(module);
            writer.MetaDataOptions.Flags = MetaDataFlags.PreserveAll;
            writer.MetaDataLogger = DummyLogger.NoThrowInstance;
            module.Write(memorystream, writer);
        }
        else
        {
            NativeModuleWriterOptions writer = new NativeModuleWriterOptions(module);
            writer.MetaDataOptions.Flags = MetaDataFlags.PreserveAll;
            writer.MetaDataLogger = DummyLogger.NoThrowInstance;
            module.NativeWrite(memorystream, writer);
        }
        byte[] ByteArray = new byte[memorystream.Length];
        memorystream.Position = 0;
        memorystream.Read(ByteArray, 0, (int)memorystream.Length);
        return ByteArray;
    }
    public static byte[] Encrypt(byte[] plain, byte[] Key)
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
    public static string RandomString(int length)
    {
        return new string((from s in Enumerable.Repeat<string>("ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789", length) select s[Context.random.Next(s.Length)]).ToArray<char>());
    }
    public static void Welcome()
    {
        Console.Title = "NETPacker Console 1.0";
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(@"Made By Sir-_-MaGeLanD#7358");
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(@"                         __          __  _                                  _______    ");
        Console.WriteLine(@"                         \ \        / / | |                                |__   __|   ");
        Console.WriteLine(@"                          \ \  /\  / /__| | ___ ___  _ __ ___   ___           | | ___  ");
        Console.WriteLine(@"                           \ \/  \/ / _ \ |/ __/ _ \| '_ ` _ \ / _ \          | |/ _ \ ");
        Console.WriteLine(@"                            \  /\  /  __/ | (_| (_) | | | | | |  __/          | | (_) |");
        Console.WriteLine(@"                             \/  \/ \___|_|\___\___/|_| |_| |_|\___|          |_|\___/ ");
        Console.WriteLine(@"███╗   ██╗███████╗████████╗██████╗  █████╗  ██████╗██╗  ██╗███████╗██████╗ ");
        Console.WriteLine(@"████╗  ██║██╔════╝╚══██╔══╝██╔══██╗██╔══██╗██╔════╝██║ ██╔╝██╔════╝██╔══██╗");
        Console.WriteLine(@"██╔██╗ ██║█████╗     ██║   ██████╔╝███████║██║     █████╔╝ █████╗  ██████╔╝");
        Console.WriteLine(@"██║╚██╗██║██╔══╝     ██║   ██╔═══╝ ██╔══██║██║     ██╔═██╗ ██╔══╝  ██╔══██╗");
        Console.WriteLine(@"██║ ╚████║███████╗   ██║   ██║     ██║  ██║╚██████╗██║  ██╗███████╗██║  ██║"); 
        Console.WriteLine(@"╚═╝  ╚═══╝╚══════╝   ╚═╝   ╚═╝     ╚═╝  ╚═╝ ╚═════╝╚═╝  ╚═╝╚══════╝╚═╝  ╚═╝");
        Console.WriteLine(Environment.NewLine);
        Console.WriteLine(Environment.NewLine);
        Console.WriteLine(Environment.NewLine);
    }
}
