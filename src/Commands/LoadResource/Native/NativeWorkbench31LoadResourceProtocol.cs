using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// Workbench 3.1 LoadResource's synchronous client/server message ABI, decoded
/// from the selected HUNK0 client and HUNK2 worker. The request stays on the
/// caller's stack while it waits for the reply, so its NAME/M pointers remain
/// protected by the live DOS ReadArgs lease for the whole transaction.
/// </summary>
public static class NativeWorkbench31LoadResourceProtocol
{
    public const ushort RequestBytes = 54;
    public const uint NamesResultSlot = 0;
    public const uint LockResultSlot = 1;
    public const uint UnlockResultSlot = 2;

    // Native aggregate lowering does not honor CLR Pack=2 field widths: a
    // Node/Message C# struct produced ReplyPort at 20 instead of 14. Reserve
    // fourteen LONGs on this caller's stack and write the public 54-byte Amiga
    // message explicitly. The final two reserved bytes are not in mn_Length.
    private struct RequestStorage
    {
#pragma warning disable CS0649 // Cleared by default(RequestStorage), then written through APTR.
        public uint A, B, C, D, E, F, G, H, I, J, K, L, M, N;
#pragma warning restore CS0649

        public static APTR AddressOf(ref RequestStorage message) =>
            throw new System.NotSupportedException(
                "RequestStorage.AddressOf is lowered by CopperSharp.");
    }

    /// <summary>
    /// Sends one parsed request and waits for its reply on the current process
    /// message port, matching the original synchronous protocol. The caller
    /// retains and releases the ReadArgs lease after this method returns.
    /// </summary>
    public static int Send(APTR servicePort,
        ref NativeCommandArguments arguments, out int ioError)
    {
        ioError = arguments.IoError;
        if (servicePort.IsNull || !arguments.IsSuccess ||
            !arguments.TryGetResult(NamesResultSlot, out var names) ||
            !arguments.TryGetResult(LockResultSlot, out var lockSwitch) ||
            !arguments.TryGetResult(UnlockResultSlot, out var unlockSwitch))
            return DOS.RETURN_FAIL;

        var task = Exec.FindTask(CString.FromPointer(0));
        if (task.IsNull)
            return DOS.RETURN_FAIL;

        var replyPort = APTR.FromPointer(task.Raw + DosLayout.Process.MessagePort);
        var storage = default(RequestStorage);
        var request = RequestStorage.AddressOf(ref storage);
        APTR.WriteUInt8(request, 8, (byte)NodeType.Message);
        APTR.WriteUInt32(request, 14, replyPort.Raw);
        APTR.WriteUInt16(request, 18, RequestBytes);
        APTR.WriteUInt16(request, 20, 0);
        APTR.WriteUInt32(request, 30, APTR.ReadUInt32(task,
            DosLayout.Process.CurrentDirectory));
        APTR.WriteUInt32(request, 34, DOS.Input().Raw);
        APTR.WriteUInt32(request, 38, DOS.Output().Raw);
        APTR.WriteUInt32(request, 42, names);
        APTR.WriteUInt32(request, 46, lockSwitch);
        APTR.WriteUInt32(request, 50, unlockSwitch);

        Exec.PutMsg(servicePort, request);
        Exec.WaitPort(replyPort);
        Exec.GetMsg(replyPort);

        ioError = unchecked((int)APTR.ReadUInt32(request, 26));
        return unchecked((int)APTR.ReadUInt32(request, 22));
    }
}
