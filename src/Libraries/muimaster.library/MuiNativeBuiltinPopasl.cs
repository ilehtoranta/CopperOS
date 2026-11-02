/* SPDX-License-Identifier: MIT */
using Amiga;
using CopperSharp.Compiler;
using CopperSharp.Sdk.Amiga;
using System.Runtime.InteropServices;

namespace CopperOS.MuiMaster;

// The worker borrows only ASL and its owned message. It never accesses the
// registry, calls MUI, runs application hooks or updates GUI object state.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiNativePopaslWork
{
    internal const uint Size = 64, Cookie = 0x4D504131;
    internal Message Message;
    internal uint Magic;
    internal APTR Asl, Requester, ReplyPort;
    internal uint Result;
    internal APTR Button, String, StopHook;
    internal uint OldDisabled;
    internal APTR RunTags;
    internal uint Bytes;
}

internal static class MuiNativePopaslWorkCodec
{
    internal static bool Read<T>(ref T p, APTR address, out MuiNativePopaslWork value)
        where T : struct, IMuiGuestMemory
    {
        value = default;
        if (!p.IsMapped(address, MuiNativePopaslWork.Size)) return false;
        value.Message = ExecMessageCodec.Read(ref p, address);
        value.Magic = p.ReadUInt32(address, 20);
        value.Asl = APTR.FromPointer(p.ReadUInt32(address, 24));
        value.Requester = APTR.FromPointer(p.ReadUInt32(address, 28));
        value.ReplyPort = APTR.FromPointer(p.ReadUInt32(address, 32));
        value.Result = p.ReadUInt32(address, 36);
        value.Button = APTR.FromPointer(p.ReadUInt32(address, 40));
        value.String = APTR.FromPointer(p.ReadUInt32(address, 44));
        value.StopHook = APTR.FromPointer(p.ReadUInt32(address, 48));
        value.OldDisabled = p.ReadUInt32(address, 52);
        value.RunTags = APTR.FromPointer(p.ReadUInt32(address, 56));
        value.Bytes = p.ReadUInt32(address, 60);
        return value.Magic == MuiNativePopaslWork.Cookie && value.Bytes >= MuiNativePopaslWork.Size;
    }
    internal static void Write<T>(ref T p, APTR address, in MuiNativePopaslWork value)
        where T : struct, IMuiGuestMemory
    {
        ExecMessageCodec.Write(ref p, address, value.Message);
        p.WriteUInt32(address, 20, value.Magic); p.WriteUInt32(address, 24, value.Asl.Raw);
        p.WriteUInt32(address, 28, value.Requester.Raw); p.WriteUInt32(address, 32, value.ReplyPort.Raw);
        p.WriteUInt32(address, 36, value.Result); p.WriteUInt32(address, 40, value.Button.Raw);
        p.WriteUInt32(address, 44, value.String.Raw); p.WriteUInt32(address, 48, value.StopHook.Raw);
        p.WriteUInt32(address, 52, value.OldDisabled); p.WriteUInt32(address, 56, value.RunTags.Raw);
        p.WriteUInt32(address, 60, value.Bytes);
    }
}

internal static class MuiNativeBuiltinPopasl
{
    internal const string WorkerExport = MuiNativeBuiltinClasses.PopaslWorkerExport;
    private const uint Work = 0x7FF10092, AslTagBase = 0x80080000;
    private const uint FileBufferSize = 512, RuntimeTagsSize = 128;
    private static APTR Offset(APTR a, uint n) => APTR.FromPointer(a.Raw + n);
    private static uint Raw(ref MuiNativeBuiltinPlatform p, APTR obj, uint key)
    { MuiHeadlessObjectCore.GetRawAttribute(ref p, p.Registry, obj, key, out var value); return value; }
    private static bool IsPopasl(ref MuiNativeBuiltinPlatform p, APTR obj) =>
        MuiHeadlessObjectCore.ObjectClassRecord(ref p, p.Registry, obj) ==
        MuiHeadlessObjectCore.FindClassByName(ref p, p.Registry, MuiNativeBuiltinClasses.Name(18));
    private static bool Live(ref MuiNativeBuiltinPlatform p, APTR obj, uint generation) =>
        MuiHeadlessObjectCodec.TryRead(ref p, MuiHeadlessObjectCore.FindObject(ref p, p.Registry, obj), out var value) &&
        value.Generation == generation && (value.Flags & MuiHeadlessObjectCore.ObjectDisposing) == 0;
    private static void Publish(ref MuiNativeBuiltinPlatform p, APTR obj, uint key, uint value)
    {
        MuiHeadlessObjectCore.SetAttribute(ref p, p.Registry, obj, key, value, false);
        var native = p.Native;
        if (MuiNativeClassOwnerCodec.TryGetPublicObjectStateAddress(ref p, p.Context, out var objects))
            MuiNativePublicObjectCore.SetAttribute(ref native, objects, native.OwnerRoot, obj, key, value, true);
    }
    internal static void ReleasedButton(ref MuiNativeBuiltinPlatform p, APTR button)
    {
        var pop = MuiHeadlessObjectCore.ParentObject(ref p, p.Registry, button);
        if (pop.IsNotNull && IsPopasl(ref p, pop) && Raw(ref p, pop, MuiPopAttributes.Popstring_Button) == button.Raw)
            Open(ref p, pop);
    }
    internal static uint Open(ref MuiNativeBuiltinPlatform p, APTR obj)
    {
        if (!IsPopasl(ref p, obj) || Raw(ref p, obj, Work) != 0 ||
            Raw(ref p, obj, MuiCommonControlCore.Disabled) != 0 ||
            Raw(ref p, obj, MuiPopAttributes.Popasl_Type) != (uint)AslRequestType.File ||
            !MuiHeadlessObjectCodec.TryRead(ref p, MuiHeadlessObjectCore.FindObject(ref p, p.Registry, obj), out var original)) return 0;
        var text = APTR.FromPointer(Raw(ref p, obj, MuiPopAttributes.Popstring_String));
        var button = APTR.FromPointer(Raw(ref p, obj, MuiPopAttributes.Popstring_Button));
        var window = MuiApplicationWindowCore.FindContainingWindow(ref p, p.Registry, obj);
        var nativeWindow = APTR.FromPointer(Raw(ref p, window, MuiNativeBuiltinWindow.NativeWindow));
        if (text.IsNull || button.IsNull || nativeWindow.IsNull ||
            !MuiNativeClassOwnerCodec.TryRead(ref p, p.Context, out var owner) || owner.DosBase.IsNull) return 0;
        uint count = 0; var attribute = original.Attributes;
        for (uint n = 0; attribute.IsNotNull && n < MuiHeadlessLayout.MaximumTraversal; n++)
        {
            if (!MuiHeadlessAttributeCodec.TryReadRecord(ref p, attribute, out var item)) return 0;
            if (item.Id >= AslTagBase && item.Id < AslTagBase + 0x10000) count++;
            attribute = item.Next;
        }
        if (attribute.IsNotNull || count > 256) return 0;
        var bytes = MuiNativePopaslWork.Size + (count + 1) * 8 + RuntimeTagsSize + FileBufferSize * 2;
        var address = p.Allocate(bytes, MuiHeadlessLayout.AllocationFlags);
        if (address.IsNull) return 0;
        var value = new MuiNativePopaslWork { Magic = MuiNativePopaslWork.Cookie, Bytes = bytes,
            Button = button, String = text, StopHook = APTR.FromPointer(Raw(ref p, obj, MuiPopAttributes.Popasl_StopHook)),
            OldDisabled = Raw(ref p, button, MuiCommonControlCore.Disabled) };
        var constructorTags = Offset(address, MuiNativePopaslWork.Size);
        uint index = 0; attribute = original.Attributes;
        while (attribute.IsNotNull)
        {
            if (!MuiHeadlessAttributeCodec.TryReadRecord(ref p, attribute, out var item))
            { p.Free(address, bytes); return 0; }
            if (item.Id >= AslTagBase && item.Id < AslTagBase + 0x10000)
            { Tag(ref p, constructorTags, index++, item.Id, item.Value); }
            attribute = item.Next;
        }
        value.RunTags = Offset(constructorTags, (count + 1) * 8);
        var path = Offset(value.RunTags, RuntimeTagsSize);
        var drawer = Offset(path, FileBufferSize);
        var actualWindow = IntuitionScreenWindowGuestCodec.ReadWindow(ref p, nativeWindow);
        Tag(ref p, value.RunTags, 0, AslTagBase + 40, actualWindow.Screen.Raw);
        Tag(ref p, value.RunTags, 1, AslTagBase + 42, 1); // Private IDCMP: worker owns requester input.
        uint runtimeCount = 2;
        if (MuiAreaLayoutCore.TryReadGeometryState(ref p, p.Registry, obj, out var geometry))
        {
            Tag(ref p, value.RunTags, 2, AslTagBase + 3, unchecked((uint)(actualWindow.LeftEdge + geometry.Left)));
            Tag(ref p, value.RunTags, 3, AslTagBase + 4, unchecked((uint)(actualWindow.TopEdge + geometry.Top + geometry.Height)));
            Tag(ref p, value.RunTags, 4, AslTagBase + 5, unchecked((uint)geometry.Width));
            runtimeCount = 5;
        }
        var startHook = APTR.FromPointer(Raw(ref p, obj, MuiPopAttributes.Popasl_StartHook));
        if (startHook.IsNotNull)
        {
            if (p.InvokeHook(startHook, obj, value.RunTags) == 0 || !Live(ref p, obj, original.Generation))
            { p.Free(address, bytes); return 0; }
        }
        else
        {
            MuiHeadlessObjectCore.GetAttribute(ref p, p.Registry, text, MuiCommonControlCore.StringContents, out var contents);
            if (!MuiNativePopaslFiles.Split(ref p, APTR.FromPointer(contents), path, drawer, FileBufferSize, out var file))
            { p.Free(address, bytes); return 0; }
            Tag(ref p, value.RunTags, runtimeCount++, AslTagBase + 8, file.Raw);
            Tag(ref p, value.RunTags, runtimeCount, AslTagBase + 9, drawer.Raw);
        }
        value.Asl = Exec.OpenLibraryRaw("asl.library", 37);
        if (value.Asl.IsNotNull) value.Requester = MuiNativePopaslCalls.Allocate(value.Asl, 0, constructorTags);
        if (value.Requester.IsNotNull) value.ReplyPort = Exec.CreateMsgPort();
        if (value.ReplyPort.IsNull) { Free(ref p, address, value); return 0; }
        value.Message = new Message { Node = new Node { Type = (byte)NodeType.Message }, ReplyPort = value.ReplyPort, Length = (ushort)bytes };
        MuiNativePopaslWorkCodec.Write(ref p, address, value);
        if (!MuiHeadlessObjectCore.SetAttribute(ref p, p.Registry, obj, Work, address.Raw, false))
        { Free(ref p, address, value); return 0; }
        // Scratch process tags no longer needed once DOS has copied them.
        var processTags = path;
        p.Clear(processTags, 40);
        Tag(ref p, processTags, 0, (uint)DosNewProcessTag.Entry, APTR.ExportAddress(WorkerExport).Raw);
        Tag(ref p, processTags, 1, (uint)DosNewProcessTag.StackSize, 131072);
        Tag(ref p, processTags, 2, (uint)DosNewProcessTag.Name, CString.ToUInt32(CString.FromLiteral("CopperOS MUI ASL")));
        var process = MuiNativePopaslDosCalls.CreateProcess(owner.DosBase, processTags);
        if (process.IsNull)
        {
            MuiHeadlessObjectCore.SetAttribute(ref p, p.Registry, obj, Work, 0, false);
            Free(ref p, address, value); return 0;
        }
        // Restore defaults overwritten by temporary process tags before worker delivery.
        if (startHook.IsNull)
        {
            MuiHeadlessObjectCore.GetAttribute(ref p, p.Registry, text, MuiCommonControlCore.StringContents, out var contents);
            MuiNativePopaslFiles.Split(ref p, APTR.FromPointer(contents), path, drawer, FileBufferSize, out _);
        }
        Exec.PutMsg(DosProcessCodec.MessagePortAddress(process), address);
        Publish(ref p, button, MuiCommonControlCore.Disabled, 1);
        if (Live(ref p, obj, original.Generation)) Publish(ref p, obj, MuiPopAttributes.Popasl_Active, 1);
        return 1;
    }
    private static void Tag(ref MuiNativeBuiltinPlatform p, APTR tags, uint index, uint key, uint value)
    { var tag = Offset(tags, index * 8); p.WriteUInt32(tag, 0, key); p.WriteUInt32(tag, 4, value); }

    [M68kExport(WorkerExport)]
    [return: M68kRegister(M68kRegister.D0)]
    public static uint Worker()
    {
        var memory = default(MuiNativeClassMemory);
        var task = Exec.FindTask(CString.FromPointer(0));
        var port = DosProcessCodec.MessagePortAddress(task);
        Exec.WaitPort(port);
        var address = Exec.GetMsg(port);
        if (!MuiNativePopaslWorkCodec.Read(ref memory, address, out var value)) return 20;
        var result = MuiNativePopaslCalls.Request(value.Asl, value.Requester, value.RunTags);
        memory.WriteUInt32(address, 36, unchecked((uint)result));
        // Reply only when no library code may run after rescheduling the owner.
        // DOS's process-exit path removes this task and unwinds its forbid count.
        Exec.Forbid();
        Exec.ReplyMsg(address);
        return 0;
    }
    internal static uint Pump(ref MuiNativeBuiltinPlatform p, APTR application, bool complete = true)
    {
        uint signals = 0;
        if (!MuiHeadlessStateCodec.TryRead(ref p, p.Registry, out var state)) return 0;
        var popClass = MuiHeadlessObjectCore.FindClassByName(ref p, p.Registry, MuiNativeBuiltinClasses.Name(18));
        if (popClass.IsNull) return 0;
        var record = state.Objects;
        for (uint n = 0; record.IsNotNull && n < MuiHeadlessLayout.MaximumTraversal; n++)
        {
            if (!MuiHeadlessObjectCodec.TryRead(ref p, record, out var obj)) break;
            if (obj.Class != popClass) { record = obj.Next; continue; }
            var address = APTR.FromPointer(Raw(ref p, obj.Boopsi, Work));
            if (address.IsNotNull && OwnsPopup(ref p, p.Registry, application, obj.Boopsi) &&
                MuiNativePopaslWorkCodec.Read(ref p, address, out var value))
            {
                if (complete && Exec.GetMsg(value.ReplyPort).IsNotNull)
                {
                    Complete(ref p, obj.Boopsi, address, false);
                    // Hooks can dispose arbitrary objects. Restart from a fresh registry.
                    return Pump(ref p, application);
                }
                var port = ExecMsgPortCodec.Read(ref p, value.ReplyPort);
                if (port.SignalBit < 32) signals |= 1u << port.SignalBit;
            }
            record = obj.Next;
        }
        return signals;
    }
    // The caller is the native Application input dispatcher. Its actual Family
    // ownership is authoritative; native applications do not run the portable
    // Application message-core initializer used by ApplicationObject's getter.
    internal static bool OwnsPopup<T>(ref T p, APTR state, APTR application, APTR obj)
        where T : struct, IMuiHeadlessPlatform
    {
        var current = obj;
        for (uint depth = 0; current.IsNotNull && depth < MuiHeadlessLayout.MaximumTraversal; depth++)
        {
            if (current == application) return true;
            current = MuiHeadlessObjectCore.ParentObject(ref p, state, current);
        }
        return false;
    }
    internal static void Dispose(ref MuiNativeBuiltinPlatform p, APTR obj)
    {
        var address = APTR.FromPointer(Raw(ref p, obj, Work));
        if (!MuiNativePopaslWorkCodec.Read(ref p, address, out var value)) return;
        Exec.WaitPort(value.ReplyPort);
        Exec.GetMsg(value.ReplyPort);
        Complete(ref p, obj, address, true);
    }
    private static void Complete(ref MuiNativeBuiltinPlatform p, APTR obj, APTR address, bool disposing)
    {
        if (!MuiNativePopaslWorkCodec.Read(ref p, address, out var value)) return;
        MuiHeadlessObjectCore.SetAttribute(ref p, p.Registry, obj, Work, 0, false);
        var record = MuiHeadlessObjectCore.FindObject(ref p, p.Registry, obj);
        MuiHeadlessObjectCodec.TryRead(ref p, record, out var original);
        if (!disposing && value.Result != 0)
        {
            if (value.StopHook.IsNotNull) p.InvokeHook(value.StopHook, obj, value.Requester);
            else
            {
                var output = Offset(value.RunTags, RuntimeTagsSize);
                // Public FileRequester begins with reserved[4], File*, Drawer*.
                var file = APTR.FromPointer(p.ReadUInt32(value.Requester, 4));
                var drawer = APTR.FromPointer(p.ReadUInt32(value.Requester, 8));
                if (MuiNativePopaslFiles.Join(ref p, drawer, file, output, FileBufferSize) && Live(ref p, obj, original.Generation))
                {
                    MuiHeadlessObjectCore.SetAttribute(ref p, p.Registry, value.String, MuiCommonControlCore.StringContents, output.Raw, false);
                    MuiNativeBuiltinPresentation.PublishInputChanges(ref p, value.String, true, true);
                    MuiNativeBuiltinPresentation.DrawObject(ref p, value.String, 0);
                }
            }
        }
        if (!disposing && Live(ref p, obj, original.Generation))
        {
            Publish(ref p, value.Button, MuiCommonControlCore.Disabled, value.OldDisabled);
            if (Live(ref p, obj, original.Generation)) Publish(ref p, obj, MuiPopAttributes.Popasl_Active, 0);
        }
        Free(ref p, address, value);
    }
    private static void Free(ref MuiNativeBuiltinPlatform p, APTR address, MuiNativePopaslWork value)
    {
        if (value.Requester.IsNotNull) MuiNativePopaslCalls.Free(value.Asl, value.Requester);
        if (value.Asl.IsNotNull) Exec.CloseLibrary(value.Asl);
        if (value.ReplyPort.IsNotNull) Exec.DeleteMsgPort(value.ReplyPort);
        p.Free(address, value.Bytes);
    }
}

[AmigaLibrary("asl.library", AmigaLibraryBasePolicy.CallerProvided)]
internal static class MuiNativePopaslCalls
{
    [AmigaLvo(-48)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern APTR Allocate([M68kRegister(M68kRegister.A6)] APTR library,
        [M68kRegister(M68kRegister.D0)] uint type, [M68kRegister(M68kRegister.A0)] APTR tags);
    [AmigaLvo(-60)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern int Request([M68kRegister(M68kRegister.A6)] APTR library,
        [M68kRegister(M68kRegister.A0)] APTR requester, [M68kRegister(M68kRegister.A1)] APTR tags);
    [AmigaLvo(-54)]
    internal static extern void Free([M68kRegister(M68kRegister.A6)] APTR library,
        [M68kRegister(M68kRegister.A0)] APTR requester);
}

[AmigaLibrary("dos.library", AmigaLibraryBasePolicy.CallerProvided)]
internal static class MuiNativePopaslDosCalls
{
    [AmigaLvo(-498)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern APTR CreateProcess([M68kRegister(M68kRegister.A6)] APTR library,
        [M68kRegister(M68kRegister.D1)] APTR tags);
}

internal static class MuiNativePopaslFiles
{
    internal static bool Split<T>(ref T p, APTR source, APTR path, APTR drawer, uint capacity, out APTR file)
        where T : struct, IMuiGuestMemory
    {
        file = path; uint length = 0, part = 0;
        while (source.IsNotNull && length + 1 < capacity)
        {
            var c = p.ReadUInt8(source, (int)length);
            if (c == 0) break;
            p.WriteUInt8(path, (int)length, c);
            if (c == ':' || c == '/') part = length + 1;
            length++;
        }
        if (source.IsNotNull && p.ReadUInt8(source, (int)length) != 0) return false;
        p.WriteUInt8(path, (int)length, 0); file = APTR.FromPointer(path.Raw + part);
        var drawerLength = part;
        if (part != 0 && p.ReadUInt8(path, (int)part - 1) == '/') drawerLength--;
        for (uint i = 0; i < drawerLength; i++) p.WriteUInt8(drawer, (int)i, p.ReadUInt8(path, (int)i));
        p.WriteUInt8(drawer, (int)drawerLength, 0);
        return true;
    }
    internal static bool Join<T>(ref T p, APTR drawer, APTR file, APTR output, uint capacity)
        where T : struct, IMuiGuestMemory
    {
        uint length = 0;
        while (drawer.IsNotNull && p.ReadUInt8(drawer, (int)length) != 0)
        {
            if (length + 1 >= capacity) return false;
            p.WriteUInt8(output, (int)length, p.ReadUInt8(drawer, (int)length)); length++;
        }
        if (length != 0 && file.IsNotNull && p.ReadUInt8(file) != 0)
        {
            var last = p.ReadUInt8(output, (int)length - 1);
            if (last != ':' && last != '/')
            { if (length + 1 >= capacity) return false; p.WriteUInt8(output, (int)length++, (byte)'/'); }
        }
        for (uint i = 0; file.IsNotNull && p.ReadUInt8(file, (int)i) != 0; i++)
        {
            if (length + 1 >= capacity) return false;
            p.WriteUInt8(output, (int)length++, p.ReadUInt8(file, (int)i));
        }
        p.WriteUInt8(output, (int)length, 0); return true;
    }
}
