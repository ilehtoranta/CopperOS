using Amiga;
using CopperOS.Shell;
using CopperSharp.Compiler;

namespace CopperOS.Shell.Dos.NativeRoot;

/// <summary>Checks explicit-handle Fault output through the public DOS catalogue.</summary>
public static class PublicFaultNativeProbe
{
    [M68kEntryPoint]
    [M68kExport("copperos.shell.public-fault-probe")]
    public static int Main()
    {
        var ok = Run();
        var dos = global::Amiga.Exec.OpenLibraryRaw("dos.library", 37);
        if (dos.IsNull) return 20;
        #if COPPEROS_PROBE_SYS
        var marker = ok ? Text("SYS:T/runner-ok") : Text("SYS:T/runner-fail");
        var file = PublicShellFileServices.OpenOutput(dos, marker, ok ? 15u : 17u, 0);
        #else
        var marker = ok ? Text("DH0:runner-ok") : Text("DH0:runner-fail");
        var file = PublicShellFileServices.OpenOutput(dos, marker, ok ? 13u : 15u, 0);
        #endif
        if (file.IsNotNull) _ = PublicShellFileServices.Close(dos, file);
        global::Amiga.Exec.CloseLibrary(dos);
        return ok ? 0 : 10;
    }

    private static bool Run()
    {
        var platform = new DosShellNativePlatform(APTR.Null);
        var dos = global::Amiga.Exec.OpenLibraryRaw("dos.library", 37);
        var memory = platform.AllocateRunnerMemory(512);
        if (dos.IsNull || memory.IsNull)
        {
            if (memory.IsNotNull) platform.FreeRunnerMemory(memory, 512);
            if (dos.IsNotNull) global::Amiga.Exec.CloseLibrary(dos);
            return false;
        }
        var previousError = PublicShellFileServices.GetError(dos);
        platform.WriteUInt32(memory, 0, 205);
        platform.WriteUInt32(memory, 4, 0);
        platform.WriteUInt32(memory, 8, 0x80000000);
        #if COPPEROS_PROBE_SYS
        var path = Text("SYS:T/fault-probe"); const uint pathLength = 17;
        #else
        var path = Text("DH0:fault-probe"); const uint pathLength = 15;
        #endif
        var file = PublicShellFileServices.OpenOutput(dos, path, pathLength, 0);
        var ok = file.IsNotNull && platform.TryWriteFault(file, memory, 3) &&
            PublicShellFileServices.GetError(dos) == 0;
        if (file.IsNotNull)
        {
            ok = !platform.TryWriteFault(file, memory, 0) &&
                PublicShellFileServices.GetError(dos) == (int)DOS.Error.ObjectWrongType && ok;
            ok = !platform.TryWriteFault(file, APTR.FromPointer(memory.Raw + 1), 1) &&
                PublicShellFileServices.GetError(dos) == (int)DOS.Error.ObjectWrongType && ok;
            ok = PublicShellFileServices.Close(dos, file) != 0 && ok;
        }
        file = PublicShellFileServices.OpenInput(dos, path, pathLength);
        if (file.IsNull) ok = false;
        else
        {
            var actual = APTR.FromPointer(memory.Raw + 32);
            var length = PublicShellFileServices.Read(dos, file, actual, 224);
            var expected = Text("Fault 205: object not found\nFault 0: \n");
            for (var i = 0; i < 38; i++)
                if (platform.ReadUInt8(actual, i) != platform.ReadUInt8(expected, i)) ok = false;
            var prefix = Text("Fault -2147483648: ");
            for (var i = 0; i < 19; i++)
                if (platform.ReadUInt8(actual, i + 38) != platform.ReadUInt8(prefix, i)) ok = false;
            var message = APTR.FromPointer(memory.Raw + 256);
            platform.WriteUInt8(message, 0, 0);
            _ = PublicShellFaultVectors.Fault(dos, int.MinValue, APTR.Null, message, 256);
            var first = platform.ReadUInt8(message);
            if (first >= (byte)'A' && first <= (byte)'Z') platform.WriteUInt8(message, 0, (byte)(first + 32));
            var messageLength = 0;
            while (messageLength < 160 && platform.ReadUInt8(message, messageLength) != 0)
            {
                if (platform.ReadUInt8(actual, 57 + messageLength) != platform.ReadUInt8(message, messageLength)) ok = false;
                messageLength++;
            }
            ok = length == 58 + messageLength &&
                platform.ReadUInt8(actual, 57 + messageLength) == (byte)'\n' && ok;
            ok = PublicShellFileServices.Close(dos, file) != 0 && ok;
        }
        _ = PublicShellFileServices.Delete(dos, path);
        platform.FreeRunnerMemory(memory, 512);
        PublicShellFileServices.SetError(dos, (DOS.Error)previousError);
        global::Amiga.Exec.CloseLibrary(dos);
        return ok;
    }

    private static APTR Text(CString value) => APTR.FromPointer(CString.ToUInt32(value));
}
