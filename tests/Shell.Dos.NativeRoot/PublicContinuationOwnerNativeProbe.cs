using Amiga;
using CopperOS.Shell;
using CopperSharp.Compiler;
using CopperSharp.Sdk.Amiga;
using CopperStart.Dos;

namespace CopperOS.Shell.Dos.NativeRoot;

/// <summary>Exercises Shell-owned continuation retirement and parent exit.</summary>
public static class PublicContinuationOwnerNativeProbe
{
    private const uint OuterBytes = 64;
    private const uint GateBytes = 32;
    private const uint TagBytes = 40;
#if COPPEROS_CONTINUATION_NO_OOM_CONTROL
    // Diagnostic control only. Its distinct mask cannot qualify allocation
    // failure handling or replace the complete owner probe.
    private const uint Expected = 0x3bff;
#else
    private const uint Expected = 0x3fff;
#endif
    private const uint OwnerExpected = Expected & ~(64u | 128u | 256u);

    [M68kEntryPoint]
    [M68kExport("copperos.shell.continuation-owner-probe")]
    public static int Main()
    {
        var dos = global::Amiga.Exec.OpenLibraryRaw("dos.library", 37);
        if (dos.IsNull) return 20;
        var message = Allocate(OuterBytes);
        var tags = Allocate(TagBytes);
        var reply = global::Amiga.Exec.CreateMsgPort();
        var stages = 0u;
        if (message.IsNotNull && tags.IsNotNull && reply.IsNotNull)
        {
            APTR.WriteUInt32(message, ExecLayout.Message.ReplyPort, reply.Raw);
            APTR.WriteUInt16(message, ExecLayout.Message.Length, (ushort)OuterBytes);
            Tag(tags, 0, DosNewProcessTag.Entry, APTR.ExportAddress("copperos.shell.continuation-owner-worker").Raw);
            Tag(tags, 1, DosNewProcessTag.Cli, 1);
            Tag(tags, 2, DosNewProcessTag.StackSize, 65_536);
            Tag(tags, 3, DosNewProcessTag.Name, Text("CopperOS Continuation Owner Probe").Raw);
            var parent = ChildProbeDosVectors.CreateNewProc(dos, tags);
            if (parent.IsNotNull)
            {
                global::Amiga.Exec.PutMsg(DosProcessCodec.MessagePortAddress(parent), message);
                _ = global::Amiga.Exec.WaitPort(reply);
                if (global::Amiga.Exec.GetMsg(reply) == message) stages = APTR.ReadUInt32(message, 20);
                WaitGone(dos, Text("CopperOS Continuation Owner Probe"));
                stages |= 64;
                var gate = APTR.FromPointer(APTR.ReadUInt32(message, 24));
                var mask = APTR.ReadUInt32(message, 28);
                var name = APTR.FromPointer(message.Raw + 32);
                if (gate.IsNotNull && mask != 0)
                {
                    global::Amiga.Exec.Forbid();
                    var found = global::Amiga.Exec.FindTask(CString.FromPointer(name.Raw));
                    if (found == gate) { stages |= 128; global::Amiga.Exec.Signal(gate, mask); }
                    global::Amiga.Exec.Permit();
                    WaitGone(dos, name);
                    stages |= 256;
                }
            }
        }
        if (message.IsNotNull)
        {
            APTR.WriteUInt32(message, 0, stages);
            var path = Text("SYS:T/continuation.bin");
            var file = PublicShellFileServices.OpenOutput(dos, path, Length(path), 0);
            if (file.IsNotNull)
            {
                _ = PublicShellFileServices.Write(dos, file, message, 4);
                _ = PublicShellFileServices.Close(dos, file);
            }
        }
        if (tags.IsNotNull) global::Amiga.Exec.FreeMem(tags.Raw, TagBytes);
        if (reply.IsNotNull) global::Amiga.Exec.DeleteMsgPort(reply);
        if (message.IsNotNull) global::Amiga.Exec.FreeMem(message.Raw, OuterBytes);
        var marker = Text(stages == Expected ? (CString)"SYS:runner-ok" : (CString)"SYS:runner-fail");
        var markerFile = PublicShellFileServices.OpenOutput(dos, marker, Length(marker), 0);
        if (markerFile.IsNotNull) _ = PublicShellFileServices.Close(dos, markerFile);
        global::Amiga.Exec.CloseLibrary(dos);
        return stages == Expected ? 0 : 10;
    }

    [M68kExport("copperos.shell.continuation-owner-worker")]
    public static uint OwnerWorker()
    {
        var task = global::Amiga.Exec.FindTask(CString.FromPointer(0));
        var port = DosProcessCodec.MessagePortAddress(task);
        _ = global::Amiga.Exec.WaitPort(port);
        var outer = global::Amiga.Exec.GetMsg(port);
        if (outer.IsNull) return 20;
        var shell = new DosShellNativePlatform(APTR.Null);
        var cli = DosProcessCodec.ReadCommandLineInterface(ref shell, task).Address;
        var dos = global::Amiga.Exec.OpenLibraryRaw("dos.library", 37);
        var stages = 0u;
        var state = APTR.Null;
        var installed = dos.IsNotNull && ShellProcessState.TryInstall(ref shell, cli, out state);
        if (installed)
        {
            stages |= 1;
            if (!PublicShellContinuationCore.Prepare(ref shell, cli, APTR.Null,
                    out var invalidOwner, out var invalidReservation, out var invalidError) &&
                invalidOwner.IsNull && invalidReservation.IsNull && invalidError == DOS.Error.ObjectWrongType)
                stages |= 512;
            var record = NewRecord(ref shell, cli);
#if !COPPEROS_CONTINUATION_NO_OOM_CONTROL
            if (record.IsNotNull && CheckAllocationFailure(ref shell, dos, cli, record)) stages |= 1024;
#endif
            if (record.IsNotNull && PublicShellContinuationCore.Prepare(ref shell, cli, record,
                    out var owner, out var reservation, out _))
            {
                var gate = StartGate(dos, owner, reservation, 0, out var mask);
                if (gate.IsNotNull && mask != 0)
                {
                    stages |= 2;
                    var waitReady = PrepareWait(ref shell, cli, record, out var frame, out var runner, out var scratch);
                    var mixedWait = new DosShellNativePlatform(APTR.FromPointer(4));
                    PublicShellFileServices.SetError(dos, (DOS.Error)205);
                    var exec = APTR.FromPointer(APTR.ReadUInt32(APTR.FromPointer(4), 0));
                    var exportedPending = waitReady && CheckExportedPending(cli, exec);
                    PublicShellFileServices.SetError(dos, (DOS.Error)205);
                    if (exportedPending && shell.TryPrepareScriptWait(cli) && mixedWait.TryPrepareScriptWait(cli) &&
                        ContinuationProbeCalls.Park(APTR.ExportAddress("copperos.shell.execute-park"),
                            cli.Raw, 1, APTR.Null, exec) == 1 &&
                        ContinuationProbeCalls.Park(APTR.ExportAddress("copperos.shell.execute-park"),
                            cli.Raw, 1, APTR.FromPointer(4), exec) == 1 &&
                        mixedWait.TryParkScriptWait(cli, 1) &&
                        PublicShellFileServices.GetError(dos) == 205 &&
                        shell.TryPollShellContinuation(cli, record, out var pending, out _) &&
                        pending == ShellProcessContinuationState.Running &&
                        !shell.TryReadContinuationDiagnostics(cli, record, out _) &&
                        !ShellProcessContinuationTeardown.TryRelease(ref shell, cli, record)) stages |= 4;
                    var lowerPriority = waitReady && global::Amiga.Exec.SetTaskPri(gate, -3) == 0 &&
                        shell.ReadUInt8(gate, ExecLayout.Node.Priority) == unchecked((byte)-3);
                    global::Amiga.Exec.Signal(gate, mask);
                    if (lowerPriority && shell.TryParkScriptWait(cli, uint.MaxValue)) stages |= 8192;
                    WaitGone(dos, PublicShellContinuationCore.TaskName(owner));
                    if (waitReady && shell.TryParkScriptWait(cli, 0) && mixedWait.TryParkScriptWait(cli, 0) &&
                        ContinuationProbeCalls.Park(APTR.ExportAddress("copperos.shell.execute-park"),
                            cli.Raw, 0, APTR.FromPointer(4), exec) == 1 &&
                        ShellProcessContinuationPolling.TryPoll(ref shell, cli, record, out var terminal, out var result) &&
                        terminal == ShellProcessContinuationState.Completed && result == 7 &&
                        shell.TryReadContinuationDiagnostics(cli, record, out var diagnostics) &&
                        diagnostics.ReturnCode == 7 && diagnostics.IoError == 205 &&
                        ShellProcessContinuationTeardown.TryRelease(ref shell, cli, record) &&
                        !shell.TryReleaseShellContinuation(cli, record, 0)) stages |= 8;
                    if (waitReady)
                    {
                        if (CheckOwned(ref shell, dos, cli, frame)) stages |= 2048;
                        _ = ShellScriptFrameCodec.TrySetPendingCommand(ref shell, frame, APTR.Null, 0, 0);
                        _ = ShellRunnerBinding.TryRemove(ref shell, cli, frame, runner);
                    }
                    if (scratch.IsNotNull) global::Amiga.Exec.FreeMem(scratch.Raw, 4);
                    if (runner.IsNotNull) global::Amiga.Exec.FreeMem(runner.Raw, ShellScriptRunnerCodec.Size);
                    if (frame.IsNotNull) global::Amiga.Exec.FreeMem(frame.Raw, ShellScriptFrameCodec.Size);
                }
                else if (gate.IsNull) _ = PublicShellContinuationCore.AbortLaunch(ref shell, cli, owner, reservation);
            }
            if (record.IsNotNull) global::Amiga.Exec.FreeMem(record.Raw, ShellProcessContinuationCodec.Size);

            if (CheckMixedCleanup(ref shell, dos, cli)) stages |= 4096;

            var second = NewRecord(ref shell, cli);
            if (second.IsNotNull && PublicShellContinuationCore.Prepare(ref shell, cli, second,
                    out var owner2, out var reservation2, out _))
            {
                var gate = StartGate(dos, owner2, reservation2, 1, out var mask);
                if (gate.IsNotNull && mask != 0)
                {
                    stages |= 16;
                    var name = PublicShellContinuationCore.TaskName(owner2);
                    for (var i = 0; i < (int)PublicShellTaskIdentity.NameBytes; i++)
                        APTR.WriteUInt8(outer, 32 + i, APTR.ReadUInt8(name, i));
                    APTR.WriteUInt32(outer, 24, gate.Raw);
                    APTR.WriteUInt32(outer, 28, mask);
                    if (PublicShellContinuationCore.DetachAll(ref shell, cli) &&
                        ShellProcessState.TryRemove(ref shell, cli, state)) stages |= 32;
                    installed = false;
                }
                else if (gate.IsNull) _ = PublicShellContinuationCore.AbortLaunch(ref shell, cli, owner2, reservation2);
            }
            if (second.IsNotNull) global::Amiga.Exec.FreeMem(second.Raw, ShellProcessContinuationCodec.Size);
            if (installed)
            {
                _ = PublicShellContinuationCore.DetachAll(ref shell, cli);
                _ = ShellProcessState.TryRemove(ref shell, cli, state);
            }
        }
        if (dos.IsNotNull) global::Amiga.Exec.CloseLibrary(dos);
        APTR.WriteUInt32(outer, 20, stages);
        global::Amiga.Exec.ReplyMsg(outer);
        return stages == OwnerExpected ? 0u : 10u;
    }

    private static bool CheckOwned(ref DosShellNativePlatform shell, APTR dos, APTR cli, APTR frame)
    {
        var record = ShellFrameResources.AllocateContinuation(ref shell, frame, 4, out _);
        if (record.IsNull) return false;
        var initial = new ShellProcessContinuation { ParentCli = cli, State = ShellProcessContinuationState.Running,
            Flags = (uint)ShellProcessContinuationFlags.RecordOwned };
        if (!ShellProcessContinuationCodec.Initialize(ref shell, record, in initial) ||
            !PublicShellContinuationCore.Prepare(ref shell, cli, record, out var owner, out var reservation, out _))
        {
            _ = ShellFrameResources.FreeContinuation(ref shell, frame, record);
            return false;
        }
        var gate = StartGate(dos, owner, reservation, 0, out var mask);
        if (gate.IsNull)
        {
            _ = PublicShellContinuationCore.AbortLaunch(ref shell, cli, owner, reservation);
            _ = ShellFrameResources.FreeContinuation(ref shell, frame, record);
            return false;
        }
        if (mask == 0) return false;
        global::Amiga.Exec.Signal(gate, mask);
        WaitGone(dos, PublicShellContinuationCore.TaskName(owner));
        // The public owner must remain discoverable even for an entry caller
        // carrying a non-null legacy context pointer.
        var mixed = new DosShellNativePlatform(APTR.FromPointer(4));
        if (!ShellProcessContinuationPolling.TryPoll(ref mixed, cli, record, out var state, out var result) ||
            state != ShellProcessContinuationState.Completed || result != 7 ||
            !mixed.TryReadContinuationDiagnostics(cli, record, out var diagnostics) ||
            diagnostics.ReturnCode != 7 || diagnostics.IoError != 205 ||
            !ShellProcessContinuationTeardown.TryRelease(ref mixed, cli, record) ||
            ShellFrameResources.IsContinuationOwned(ref shell, frame, record)) return false;

        record = ShellFrameResources.AllocateContinuation(ref shell, frame, 4, out _);
        if (record.IsNull) return false;
        if (!ShellProcessContinuationCodec.Initialize(ref shell, record, in initial) ||
            !PublicShellContinuationCore.Prepare(ref shell, cli, record, out owner, out reservation, out _))
        {
            _ = ShellFrameResources.FreeContinuation(ref shell, frame, record);
            return false;
        }
        var name = Allocate(PublicShellTaskIdentity.NameBytes);
        if (name.IsNull)
        {
            _ = PublicShellContinuationCore.AbortLaunch(ref shell, cli, owner, reservation);
            _ = ShellFrameResources.FreeContinuation(ref shell, frame, record);
            return false;
        }
        for (var i = 0; i < (int)PublicShellTaskIdentity.NameBytes; i++)
            APTR.WriteUInt8(name, i, APTR.ReadUInt8(PublicShellContinuationCore.TaskName(owner), i));
        gate = StartGate(dos, owner, reservation, 1, out mask);
        var ok = false;
        if (gate.IsNotNull && mask != 0)
        {
            // The frame's pending pointer still names a different record. This
            // exercises cleanup after publication but before pending assignment.
            ok = PublicShellContinuationCore.DetachFrame(ref shell, cli, frame) &&
                !ShellFrameResources.IsContinuationOwned(ref shell, frame, record);
            global::Amiga.Exec.Signal(gate, mask);
            WaitGone(dos, name);
        }
        else if (gate.IsNull)
        {
            _ = PublicShellContinuationCore.AbortLaunch(ref shell, cli, owner, reservation);
            _ = ShellFrameResources.FreeContinuation(ref shell, frame, record);
        }
        global::Amiga.Exec.FreeMem(name.Raw, PublicShellTaskIdentity.NameBytes);
        return ok;
    }

    private static bool CheckMixedCleanup(ref DosShellNativePlatform shell, APTR dos, APTR cli)
    {
        var frame = Allocate(ShellScriptFrameCodec.Size);
        var context = Allocate(DosCore.StateSize);
        var name = Allocate(PublicShellTaskIdentity.NameBytes);
        if (frame.IsNull || context.IsNull || name.IsNull) return false;
        // Minimal legacy record-owner fixture. It is never installed as DOS:
        // only the magic, version, generation and owned allocation chains are
        // used by AllocateScriptRecord/ReleaseScriptRecords.
        APTR.WriteUInt32(context, 0, DosCore.StateMagic);
        APTR.WriteUInt32(context, 4, DosCore.StateVersion);
        APTR.WriteUInt32(context, 24, 1);
        var legacy = new CopperSharpNativeDosPlatform(context.Raw);
        var mixed = new DosShellNativePlatform(context);
        var initialFrame = new ShellScriptFrameState { Cli = cli, Parent = cli,
            Flags = ShellScriptFrameFlags.Active, FailureLimit = 10 };
        var ok = ShellScriptFrameCodec.Initialize(ref shell, frame, in initialFrame) &&
            shell.TryBindScriptFrame(cli, frame);
        var record = ok ? ShellFrameResources.AllocateContinuation(ref shell, frame, 4, out _) : APTR.Null;
        var initial = new ShellProcessContinuation { ParentCli = cli,
            State = ShellProcessContinuationState.Running,
            Flags = (uint)ShellProcessContinuationFlags.RecordOwned };
        var owner = APTR.Null;
        var reservation = APTR.Null;
        ok = ok && record.IsNotNull && ShellProcessContinuationCodec.Initialize(ref shell, record, in initial) &&
            PublicShellContinuationCore.Prepare(ref shell, cli, record, out owner, out reservation, out _);
        var gate = APTR.Null;
        var mask = 0u;
        if (ok)
        {
            for (var i = 0; i < (int)PublicShellTaskIdentity.NameBytes; i++)
                APTR.WriteUInt8(name, i, APTR.ReadUInt8(PublicShellContinuationCore.TaskName(owner), i));
            gate = StartGate(dos, owner, reservation, 1, out mask);
            var legacyRecord = DosShellStateCore.AllocateScriptRecord(ref legacy, context, frame, 16, 1);
            var legacyHead = APTR.ReadUInt32(context, 88);
            ok = gate.IsNotNull && mask != 0 && legacyRecord.IsNotNull &&
                ShellScriptFrameCodec.TrySetPendingCommand(ref shell, frame, record, 2, 0) &&
                mixed.TryUnbindScriptFrame(cli, frame) &&
                !ShellFrameResources.IsContinuationOwned(ref shell, frame, record) &&
                APTR.ReadUInt32(context, 88) == legacyHead &&
                ShellProcessState.TryGetContinuationHead(ref shell, cli, out var present, out var head) &&
                present && head.IsNull;
        }
        // Detach on a failing assertion too, before letting the child publish.
        if (owner.IsNotNull && gate.IsNull)
            _ = PublicShellContinuationCore.AbortLaunch(ref shell, cli, owner, reservation);
        else if (!ok) _ = PublicShellContinuationCore.DetachFrame(ref shell, cli, frame);
        if (!ok)
        {
            _ = ShellScriptFrameCodec.TrySetPendingCommand(ref shell, frame, APTR.Null, 0, 0);
            _ = mixed.TryUnbindScriptFrame(cli, frame);
        }
        if (gate.IsNotNull && mask != 0) { global::Amiga.Exec.Signal(gate, mask); WaitGone(dos, name); }
        // The shared platform ignores the supplied context. The fixture,
        // rather than the native Shell, still owns these legacy allocations.
        ok = DosShellStateCore.ReleaseScriptRecords(ref legacy, context, frame) && ok;
        // SetIoErr in the legacy fixture lazily creates one owned Process
        // record. Validate its header and retire only that fixture allocation.
        var process = APTR.FromPointer(APTR.ReadUInt32(context, 8));
        if (process.IsNotNull && APTR.ReadUInt32(process, 0) == 0x50524f43 &&
            APTR.ReadUInt32(process, 4) == context.Raw && APTR.ReadUInt32(process, 16) == 0)
            global::Amiga.Exec.FreeMem(process.Raw, APTR.ReadUInt32(process, 8));
        else ok = false;
        global::Amiga.Exec.FreeMem(name.Raw, PublicShellTaskIdentity.NameBytes);
        global::Amiga.Exec.FreeMem(context.Raw, DosCore.StateSize);
        global::Amiga.Exec.FreeMem(frame.Raw, ShellScriptFrameCodec.Size);
        return ok;
    }

    private static bool CheckAllocationFailure(ref DosShellNativePlatform shell, APTR dos, APTR cli, APTR record)
    {
        // Prevent another task from observing the deliberately exhausted
        // public heap. Each reserved block holds its own address and size so
        // all memory can be returned before scheduling resumes.
        var head = APTR.Null;
        var count = 0u;
        var request = 65_536u;
        var allocationFailed = false;
        global::Amiga.Exec.Forbid();
        while (count < 65_536 && request >= 64)
        {
            var block = APTR.FromPointer(global::Amiga.Exec.AllocMem(request, global::Amiga.Exec.MemoryFlags.Public));
            if (block.IsNull)
            {
                allocationFailed = true;
                request >>= 1;
                continue;
            }
            APTR.WriteUInt32(block, 0, head.Raw);
            APTR.WriteUInt32(block, 4, request);
            head = block;
            count++;
        }
        var prepared = PublicShellContinuationCore.Prepare(ref shell, cli, record,
            out var owner, out var reservation, out var failure);
        if (prepared) _ = PublicShellContinuationCore.AbortLaunch(ref shell, cli, owner, reservation);
        var ok = count != 0 && !prepared && owner.IsNull && reservation.IsNull && failure == DOS.Error.NoFreeStore;
        while (head.IsNotNull)
        {
            var next = APTR.FromPointer(APTR.ReadUInt32(head, 0));
            var bytes = APTR.ReadUInt32(head, 4);
            global::Amiga.Exec.FreeMem(head.Raw, bytes);
            head = next;
        }
        global::Amiga.Exec.Permit();
        var diagnostic = Allocate(20);
        if (diagnostic.IsNotNull)
        {
            APTR.WriteUInt32(diagnostic, 0, count);
            APTR.WriteUInt32(diagnostic, 4, request);
            APTR.WriteUInt32(diagnostic, 8, allocationFailed ? 1u : 0u);
            APTR.WriteUInt32(diagnostic, 12, prepared ? 1u : 0u);
            APTR.WriteUInt32(diagnostic, 16, unchecked((uint)failure));
            var path = Text("SYS:T/continuation-oom.bin");
            var file = PublicShellFileServices.OpenOutput(dos, path, Length(path), 0);
            if (file.IsNotNull)
            {
                _ = PublicShellFileServices.Write(dos, file, diagnostic, 20);
                _ = PublicShellFileServices.Close(dos, file);
            }
            global::Amiga.Exec.FreeMem(diagnostic.Raw, 20);
        }
        return ok;
    }

    [M68kExport("copperos.shell.continuation-gate-worker")]
    public static uint GateWorker()
    {
        var task = global::Amiga.Exec.FindTask(CString.FromPointer(0));
        var port = DosProcessCodec.MessagePortAddress(task);
        _ = global::Amiga.Exec.WaitPort(port);
        var message = global::Amiga.Exec.GetMsg(port);
        if (message.IsNull) return 20;
        var owner = APTR.FromPointer(APTR.ReadUInt32(message, 20));
        var phase = APTR.ReadUInt32(message, 24);
        var signal = global::Amiga.Exec.AllocSignal(-1);
        var mask = signal < 0 ? 0u : 1u << signal;
        if (phase == 0) PublicShellContinuationCore.Complete(owner, 7, 205);
        APTR.WriteUInt32(message, 28, mask);
        global::Amiga.Exec.ReplyMsg(message);
        if (mask != 0) _ = global::Amiga.Exec.Wait(mask);
        if (phase != 0) PublicShellContinuationCore.Complete(owner, 7, 205);
        if (mask != 0) global::Amiga.Exec.FreeSignal(signal);
        return 7;
    }

    private static APTR StartGate(APTR dos, APTR owner, APTR reservation, uint phase, out uint mask)
    {
        mask = 0;
        var message = Allocate(GateBytes);
        var tags = Allocate(TagBytes);
        var reply = global::Amiga.Exec.CreateMsgPort();
        var child = APTR.Null;
        if (message.IsNotNull && tags.IsNotNull && reply.IsNotNull)
        {
            APTR.WriteUInt32(message, ExecLayout.Message.ReplyPort, reply.Raw);
            APTR.WriteUInt16(message, ExecLayout.Message.Length, (ushort)GateBytes);
            APTR.WriteUInt32(message, 20, owner.Raw);
            APTR.WriteUInt32(message, 24, phase);
            Tag(tags, 0, DosNewProcessTag.Entry, APTR.ExportAddress("copperos.shell.continuation-gate-worker").Raw);
            Tag(tags, 1, DosNewProcessTag.Cli, 1);
            Tag(tags, 2, DosNewProcessTag.StackSize, 16_384);
            Tag(tags, 3, DosNewProcessTag.Name, PublicShellContinuationCore.TaskName(owner).Raw);
            child = ChildProbeDosVectors.CreateNewProc(dos, tags);
            if (child.IsNotNull)
            {
                PublicShellTaskIdentity.Attach(child, reservation);
                global::Amiga.Exec.PutMsg(DosProcessCodec.MessagePortAddress(child), message);
                _ = global::Amiga.Exec.WaitPort(reply);
                if (global::Amiga.Exec.GetMsg(reply) == message) mask = APTR.ReadUInt32(message, 28);
            }
        }
        if (tags.IsNotNull) global::Amiga.Exec.FreeMem(tags.Raw, TagBytes);
        if (reply.IsNotNull) global::Amiga.Exec.DeleteMsgPort(reply);
        if (message.IsNotNull) global::Amiga.Exec.FreeMem(message.Raw, GateBytes);
        return child;
    }

    private static APTR NewRecord(ref DosShellNativePlatform shell, APTR cli)
    {
        var record = Allocate(ShellProcessContinuationCodec.Size);
        var value = new ShellProcessContinuation { ParentCli = cli, State = ShellProcessContinuationState.Running };
        if (record.IsNotNull && ShellProcessContinuationCodec.Initialize(ref shell, record, in value)) return record;
        if (record.IsNotNull) global::Amiga.Exec.FreeMem(record.Raw, ShellProcessContinuationCodec.Size);
        return APTR.Null;
    }

    private static bool PrepareWait(ref DosShellNativePlatform shell, APTR cli, APTR record,
        out APTR frame, out APTR runner, out APTR scratch)
    {
        frame = Allocate(ShellScriptFrameCodec.Size);
        runner = Allocate(ShellScriptRunnerCodec.Size);
        scratch = Allocate(4);
        if (frame.IsNull || runner.IsNull || scratch.IsNull) return false;
        var frameValue = new ShellScriptFrameState
        {
            Parent = cli, Cli = cli, Input = BPTR.FromRaw(1), CurrentLine = 1,
            Flags = ShellScriptFrameFlags.Active,
        };
        var runnerValue = new ShellScriptRunnerRecord
        {
            Cli = cli, Frame = frame, Input = BPTR.FromRaw(1), Line = scratch,
            CommandName = scratch, Token = scratch, First = scratch, Second = scratch,
            Third = scratch, Fourth = scratch, RedirectionCommand = scratch,
            RedirectionInput = scratch, RedirectionOutput = scratch, RedirectionError = scratch,
            AliasLine = scratch, LookupPath = scratch, ScriptTemporaryPath = scratch,
            State = ShellScriptRunnerState.Pending,
        };
        return ShellScriptFrameCodec.Initialize(ref shell, frame, in frameValue) &&
            ShellScriptFrameCodec.TrySetPendingCommand(ref shell, frame, record, 2, 0) &&
            ShellScriptRunnerCodec.Initialize(ref shell, runner, in runnerValue) &&
            ShellRunnerBinding.TryPublish(ref shell, cli, frame, runner);
    }

    private static bool CheckExportedPending(APTR cli, APTR exec)
    {
        var before = APTR.ReadUInt32(APTR.FromPointer(4), 0);
        var unused = Text("unused");
        for (var context = 0; context < 2; context++)
        {
            var state = context == 0 ? APTR.Null : APTR.FromPointer(4);
            var pair = ExportedRunnerCalls.Begin(APTR.ExportAddress("copperos.shell.execute-begin"),
                cli.Raw, unused.Raw, Length(unused), state, exec);
            var result = M68kRuntime.SplitUInt64(pair, out var status);
            if (status != (uint)ShellScriptExecutionStatus.Pending || result != 0) return false;
            pair = ExportedRunnerCalls.Poll(APTR.ExportAddress("copperos.shell.execute-poll"), cli.Raw, state, exec);
            result = M68kRuntime.SplitUInt64(pair, out status);
            if (status != (uint)ShellScriptExecutionStatus.Pending || result != 0 ||
                APTR.ReadUInt32(APTR.FromPointer(4), 0) != before) return false;
        }
        return true;
    }

    private static void WaitGone(APTR dos, APTR name)
    { while (global::Amiga.Exec.FindTask(CString.FromPointer(name.Raw)).IsNotNull) ChildProbeDosVectors.Delay(dos, 1); }
    private static APTR Allocate(uint bytes) => APTR.FromPointer(global::Amiga.Exec.AllocMem(bytes,
        global::Amiga.Exec.MemoryFlags.Public | global::Amiga.Exec.MemoryFlags.Clear));
    private static APTR Text(CString text) => APTR.FromPointer(CString.ToUInt32(text));
    private static uint Length(APTR text)
    { uint length = 0; while (APTR.ReadUInt8(text, (int)length) != 0) length++; return length; }
    private static void Tag(APTR tags, int index, DosNewProcessTag tag, uint value)
    { APTR.WriteUInt32(tags, index * 8, (uint)tag); APTR.WriteUInt32(tags, index * 8 + 4, value); }
}

internal static class ContinuationProbeCalls
{
    [AmigaIndirectCall(M68kRegister.A3)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern uint Park([M68kRegister(M68kRegister.A3)] APTR entry,
        [M68kRegister(M68kRegister.D0)] uint cli, [M68kRegister(M68kRegister.D1)] uint ticks,
        [M68kRegister(M68kRegister.A0)] APTR state, [M68kRegister(M68kRegister.A6)] APTR exec);
}
