using Amiga;
using CopperSharp.Compiler;
using CopperSharp.Sdk.Amiga;
using CopperStart.Dos;

namespace CopperOS.Shell.Dos.NativeRoot;

/// <summary>Separates public DOS Delay scheduling from Shell continuation polling.</summary>
public static class PublicDelayPriorityNativeProbe
{
    private const uint MessageBytes = 24, TagBytes = 48;
#if COPPEROS_DELAY_HEAP_PRESSURE
    private const uint ObservationBytes = 112;
#else
    private const uint ObservationBytes = 96;
#endif

    [M68kEntryPoint]
    [M68kExport("copperos.shell.delay-priority-probe")]
    public static int Main()
    {
        var dos = global::Amiga.Exec.OpenLibraryRaw("dos.library", 37);
        if (dos.IsNull) return 20;
        var observations = Allocate(ObservationBytes);
        var passed = 0u;
        if (observations.IsNotNull)
        {
            APTR.WriteUInt32(observations, 0, 0x444c4159); // DLAY
            APTR.WriteUInt32(observations, 80, 0x7fffffff);
#if COPPEROS_DELAY_HEAP_PRESSURE
            var before = global::Amiga.Exec.AvailMem(global::Amiga.Exec.MemoryFlags.Public);
            APTR.WriteUInt32(observations, 96, ExhaustAndReleasePublicMemory());
            APTR.WriteUInt32(observations, 100, before);
            APTR.WriteUInt32(observations, 104, global::Amiga.Exec.AvailMem(global::Amiga.Exec.MemoryFlags.Public));
            APTR.WriteUInt32(observations, 108, 0x48454150); // HEAP variant
#endif
            for (var test = 0; test < 4; test++)
            {
                APTR.WriteUInt32(observations, 8, unchecked((uint)test + 1));
                var ticks = test == 0 ? 1 : test == 1 ? 25 : 50;
                var row = APTR.FromPointer(observations.Raw + 16 + unchecked((uint)test * 16));
                if (Exercise(dos, ticks, row, test == 3, observations)) passed |= 1u << test;
                APTR.WriteUInt32(observations, 4, passed);
                APTR.WriteUInt32(observations, 12, unchecked((uint)test + 1));
            }
            var file = Open(dos, "SYS:T/delay-priority.bin");
            if (file.IsNotNull)
            {
                _ = PublicShellFileServices.Write(dos, file, observations, ObservationBytes);
                _ = PublicShellFileServices.Close(dos, file);
            }
            global::Amiga.Exec.FreeMem(observations.Raw, ObservationBytes);
        }
        var marker = Open(dos, passed == 15 ? (CString)"SYS:runner-ok" : (CString)"SYS:runner-fail");
        if (marker.IsNotNull) _ = PublicShellFileServices.Close(dos, marker);
        global::Amiga.Exec.CloseLibrary(dos);
        return passed == 15 ? 0 : 10;
    }

#if COPPEROS_DELAY_HEAP_PRESSURE
    // Isolates the owner probe's pressure precondition from Shell polling.
    // No task can observe the exhausted heap; release every block before Permit.
    private static uint ExhaustAndReleasePublicMemory()
    {
        var head = APTR.Null;
        var request = 65_536u;
        var count = 0u;
        global::Amiga.Exec.Forbid();
        while (request >= 64 && count < 65_536)
        {
            var block = APTR.FromPointer(global::Amiga.Exec.AllocMem(request, global::Amiga.Exec.MemoryFlags.Public));
            if (block.IsNull) { request >>= 1; continue; }
            APTR.WriteUInt32(block, 0, head.Raw);
            APTR.WriteUInt32(block, 4, request);
            head = block;
            count++;
        }
        while (head.IsNotNull)
        {
            var next = APTR.FromPointer(APTR.ReadUInt32(head, 0));
            var bytes = APTR.ReadUInt32(head, 4);
            global::Amiga.Exec.FreeMem(head.Raw, bytes);
            head = next;
        }
        global::Amiga.Exec.Permit();
        return count;
    }
#endif

    private static bool Exercise(APTR dos, int ticks, APTR row, bool directTimer, APTR observations)
    {
        APTR.WriteUInt32(row, 0, unchecked((uint)ticks) | (directTimer ? 0x80000000u : 0));
        APTR.WriteUInt32(row, 12, 0x7fffffff);
        var reply = global::Amiga.Exec.CreateMsgPort();
        var message = Allocate(MessageBytes);
        var tags = Allocate(TagBytes);
        var ok = reply.IsNotNull && message.IsNotNull && tags.IsNotNull;
        var timerPort = APTR.Null;
        var timerRequest = APTR.Null;
        var timerOpened = false;
        var timerSubmitted = false;
        if (directTimer)
        {
            timerPort = global::Amiga.Exec.CreateMsgPort();
            if (timerPort.IsNotNull) timerRequest = global::Amiga.Exec.CreateIORequest(timerPort, TimerRequest.Size);
            if (timerRequest.IsNotNull)
            {
                var error = global::Amiga.Exec.OpenDevice((CString)"timer.device", (uint)TimerUnit.MicroHz, timerRequest, 0);
                APTR.WriteUInt32(observations, 80, unchecked((uint)error));
                timerOpened = error == 0;
            }
            ok = timerOpened && ok;
        }
        if (ok)
        {
            APTR.WriteUInt32(message, ExecLayout.Message.ReplyPort, reply.Raw);
            APTR.WriteUInt16(message, ExecLayout.Message.Length, (ushort)MessageBytes);
            Tag(tags, 0, DosNewProcessTag.Entry, APTR.ExportAddress("copperos.shell.delay-priority-worker").Raw);
            Tag(tags, 1, DosNewProcessTag.Cli, 1);
            Tag(tags, 2, DosNewProcessTag.StackSize, 16_384);
            Tag(tags, 3, DosNewProcessTag.Name, Text("CopperOS Delay Child").Raw);
            Tag(tags, 4, DosNewProcessTag.Priority, unchecked((uint)-3));
            var child = ChildProbeDosVectors.CreateNewProc(dos, tags);
            if (child.IsNull) ok = false;
            else
            {
                var priority = APTR.ReadUInt8(child, ExecLayout.Node.Priority);
                APTR.WriteUInt32(row, 4, priority);
                global::Amiga.Exec.PutMsg(DosProcessCodec.MessagePortAddress(child), message);
                if (directTimer)
                {
                    APTR.WriteUInt16(timerRequest, ExecLayout.IORequest.Command, (ushort)TimerCommand.AddRequest);
                    APTR.WriteUInt8(timerRequest, ExecLayout.IORequest.Flags, 0);
                    APTR.WriteUInt8(timerRequest, ExecLayout.IORequest.Error, 0);
                    APTR.WriteUInt32(timerRequest, 32, 1);
                    APTR.WriteUInt32(timerRequest, 36, 0);
                    timerSubmitted = true;
                    global::Amiga.Exec.DoIO(timerRequest);
                    APTR.WriteUInt32(observations, 84, APTR.ReadUInt8(timerRequest, ExecLayout.IORequest.Error));
                    APTR.WriteUInt32(observations, 88, APTR.ReadUInt8(timerRequest, ExecLayout.IORequest.Flags));
                    APTR.WriteUInt32(observations, 92, APTR.ReadUInt8(timerRequest, ExecLayout.Node.Type));
                }
                else ChildProbeDosVectors.Delay(dos, ticks);
                var afterDelay = APTR.ReadUInt32(message, 20);
                APTR.WriteUInt32(row, 8, afterDelay);
                // An unconditional Exec wait distinguishes a DOS Delay that
                // did not yield from an Exec dispatcher that cannot run this
                // lower-priority child even when its parent is blocked.
                _ = global::Amiga.Exec.WaitPort(reply);
                ok = global::Amiga.Exec.GetMsg(reply) == message &&
                    APTR.ReadUInt32(message, 20) == 1 && priority == unchecked((byte)-3) &&
                    (ticks == 1 || afterDelay == 1);
                // ReplyMsg can wake this parent before the child retires.
                // Make retirement independent of the Delay under test.
                global::Amiga.Exec.Forbid();
                var live = global::Amiga.Exec.FindTask((CString)"CopperOS Delay Child");
                if (live.IsNotNull)
                    APTR.WriteUInt32(row, 12, unchecked((uint)global::Amiga.Exec.SetTaskPri(live, 0)));
                global::Amiga.Exec.Permit();
                while (global::Amiga.Exec.FindTask((CString)"CopperOS Delay Child").IsNotNull)
                    ChildProbeDosVectors.Delay(dos, 1);
            }
        }
        if (timerOpened)
        {
            if (timerSubmitted)
            {
                if (global::Amiga.Exec.CheckIO(timerRequest).IsNull) global::Amiga.Exec.AbortIO(timerRequest);
                _ = global::Amiga.Exec.WaitIO(timerRequest);
            }
            global::Amiga.Exec.CloseDevice(timerRequest);
        }
        if (timerRequest.IsNotNull) global::Amiga.Exec.DeleteIORequest(timerRequest);
        if (timerPort.IsNotNull) global::Amiga.Exec.DeleteMsgPort(timerPort);
        if (tags.IsNotNull) global::Amiga.Exec.FreeMem(tags.Raw, TagBytes);
        if (message.IsNotNull) global::Amiga.Exec.FreeMem(message.Raw, MessageBytes);
        if (reply.IsNotNull) global::Amiga.Exec.DeleteMsgPort(reply);
        return ok;
    }

    [M68kExport("copperos.shell.delay-priority-worker")]
    public static uint Worker()
    {
        var task = global::Amiga.Exec.FindTask(CString.FromPointer(0));
        var port = DosProcessCodec.MessagePortAddress(task);
        _ = global::Amiga.Exec.WaitPort(port);
        var message = global::Amiga.Exec.GetMsg(port);
        if (message.IsNull) return 20;
        APTR.WriteUInt32(message, 20, 1);
        global::Amiga.Exec.ReplyMsg(message);
        return 0;
    }

    private static APTR Allocate(uint bytes) => APTR.FromPointer(global::Amiga.Exec.AllocMem(bytes,
        global::Amiga.Exec.MemoryFlags.Public | global::Amiga.Exec.MemoryFlags.Clear));
    private static APTR Text(CString value) => APTR.FromPointer(CString.ToUInt32(value));
    private static BPTR Open(APTR dos, CString value)
    {
        var path = Text(value);
        var length = 0u;
        while (APTR.ReadUInt8(path, (int)length) != 0) length++;
        return PublicShellFileServices.OpenOutput(dos, path, length, 0);
    }
    private static void Tag(APTR tags, int index, DosNewProcessTag tag, uint value)
    { APTR.WriteUInt32(tags, index * 8, (uint)tag); APTR.WriteUInt32(tags, index * 8 + 4, value); }
}
