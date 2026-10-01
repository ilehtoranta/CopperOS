using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// Workbench 3.1 LoadResource's serialized request dispatcher. The process
/// coordinator owns the service port, registry, library leases, installed
/// LoadSeg cache and code image. This class does not start or stop that process.
/// </summary>
public static class NativeWorkbench31LoadResourceWorker
{
    private const int AnchorBytes = 538;
    private const ushort PathBytes = 256;

#pragma warning disable CS0649 // Native stack storage initialized with default, accessed through APTR.
    private struct LongBlock
    {
        public uint A, B, C, D, E, F, G, H, I, J, K, L, M, N, O, P;
    }

    private struct AnchorStorage
    {
        public LongBlock A, B, C, D, E, F, G, H;
        public uint I, J, K, L, M, N, O;

        public static APTR AddressOf(ref AnchorStorage value) =>
            throw new System.NotSupportedException("AnchorStorage.AddressOf is lowered by CopperSharp.");
    }

    private struct RowArguments
    {
        public uint Type, Name;

        public static APTR AddressOf(ref RowArguments value) =>
            throw new System.NotSupportedException("RowArguments.AddressOf is lowered by CopperSharp.");
    }
#pragma warning restore CS0649

    /// <summary>Receives and dispatches one request on the worker-owned port.</summary>
    public static void ReceiveAndDispatch(APTR servicePort, APTR registry, APTR messageCatalogState)
    {
        Exec.WaitPort(servicePort);
        var message = Exec.GetMsg(servicePort);
        Dispatch(message, registry, messageCatalogState);
    }

    /// <summary>
    /// Handles a valid received 54-byte request, restores the worker's borrowed
    /// directory and streams, then replies. Caller owns all library leases,
    /// the cleared four-byte message catalog state and serialized registry.
    /// </summary>
    public static void Dispatch(APTR message, APTR registry, APTR messageCatalogState)
    {
        if (APTR.ReadUInt16(message, 20) != 0)
        {
            SetFailure(message, (int)DOS.Error.ObjectWrongType);
            Exec.ReplyMsg(message);
            return;
        }

        APTR.WriteUInt32(message, 22, 0);
        APTR.WriteUInt32(message, 26, 0);
        var directory = DOS.CurrentDirRaw(BPTR.FromRaw(APTR.ReadUInt32(message, 30)));
        var input = DOS.SelectInput(BPTR.FromRaw(APTR.ReadUInt32(message, 34)));
        var output = DOS.SelectOutput(BPTR.FromRaw(APTR.ReadUInt32(message, 38)));

        var names = APTR.FromPointer(APTR.ReadUInt32(message, 42));
        if (names.IsNull)
            List(registry, messageCatalogState);
        else
            ProcessNames(message, registry, messageCatalogState, names);

        var error = unchecked((int)APTR.ReadUInt32(message, 26));
        if (error != 0)
            DOS.PrintFault((DOS.Error)error, CString.FromPointer(0));

        DOS.CurrentDirRaw(directory);
        DOS.SelectInput(input);
        DOS.SelectOutput(output);
        Exec.ReplyMsg(message);
    }

    private static void ProcessNames(APTR message, APTR registry, APTR catalogState, APTR names)
    {
        var storage = default(AnchorStorage);
        var anchor = AnchorStorage.AddressOf(ref storage);
        var expandedName = CString.FromPointer(anchor.Raw + DosLayout.AnchorPath.PathBuffer);
        var cursor = names;
        while (APTR.ReadUInt32(cursor, 0) != 0 && APTR.ReadUInt32(message, 22) == 0)
        {
            // The original clears all 538 bytes for each NAME/M pattern.
            for (var offset = 0; offset < AnchorBytes - 2; offset += 4)
                APTR.WriteUInt32(anchor, offset, 0);
            APTR.WriteUInt16(anchor, AnchorBytes - 2, 0);
            APTR.WriteUInt32(anchor, DosLayout.AnchorPath.BreakBits, 0x1000);
            APTR.WriteUInt8(anchor, DosLayout.AnchorPath.Flags, 1);
            APTR.WriteUInt16(anchor, DosLayout.AnchorPath.StringLength, PathBytes);

            var pattern = CString.FromPointer(APTR.ReadUInt32(cursor, 0));
            var prefixNeeded = true;
            var error = DOS.MatchFirst(pattern, anchor);
            while (error == 0)
            {
                if (APTR.ReadUInt32(message, 50) != 0)
                {
                    var record = NativeWorkbench31LoadResourceRegistry.Find(registry, expandedName);
                    if (record.IsNotNull)
                        NativeWorkbench31LoadResourceRegistry.CloseAndRemove(record);
                    else
                        NativeWorkbench31LoadResourceMessages.PrintName(catalogState, 0xc35c, expandedName);
                }
                else
                {
                    // Source MOVE.L/EXT.L uses the switch's low word here.
                    var keepLocked = (APTR.ReadUInt32(message, 46) & 0xffff) != 0;
                    error = NativeWorkbench31LoadResourceActions.Load(registry, expandedName, keepLocked, catalogState);
                    if (error != 0)
                    {
                        prefixNeeded = false;
                        break;
                    }
                }
                error = DOS.MatchNext(anchor);
            }

            if (error != (int)DOS.Error.NoMoreEntries)
            {
                SetFailure(message, error);
                if (prefixNeeded)
                    NativeWorkbench31LoadResourceMessages.PrintName(catalogState, 0xc35d, pattern);
            }
            DOS.MatchEnd(anchor);
            cursor = APTR.FromPointer(cursor.Raw + 4);
        }
    }

    private static void List(APTR registry, APTR catalogState)
    {
        NativeWorkbench31LoadResourceMessages.Begin(catalogState);
        if (NativeWorkbench31LoadResourceRegistry.IsEmpty(registry))
        {
            NativeWorkbench31LoadResourceMessages.PrintName(catalogState, 0xc357, CString.FromLiteral(""));
            NativeWorkbench31LoadResourceMessages.End(catalogState);
            return;
        }

        var row = new RowArguments
        {
            Type = CString.ToUInt32(NativeWorkbench31LoadResourceMessages.Get(catalogState, 0xc354)),
            Name = CString.ToUInt32(NativeWorkbench31LoadResourceMessages.Get(catalogState, 0xc356))
        };
        // C356 already ends in LF; C355 adds its own LF, as in the source.
        NativeWorkbench31LoadResourceMessages.Print(catalogState, 0xc355, RowArguments.AddressOf(ref row));
        var record = APTR.FromPointer(APTR.ReadUInt32(registry, 0));
        while (APTR.ReadUInt32(record, 0) != 0)
        {
            row.Type = CString.ToUInt32(NativeWorkbench31LoadResourceMessages.Get(catalogState,
                0xc350u + APTR.ReadUInt8(record, NativeWorkbench31LoadResourceRegistry.TypeOffset)));
            row.Name = APTR.ReadUInt32(record, NativeWorkbench31LoadResourceRegistry.NameOffset);
            NativeWorkbench31LoadResourceMessages.Print(catalogState, 0xc355, RowArguments.AddressOf(ref row));
            record = APTR.FromPointer(APTR.ReadUInt32(record, 0));
        }
        NativeWorkbench31LoadResourceMessages.End(catalogState);
    }

    private static void SetFailure(APTR message, int error)
    {
        APTR.WriteUInt32(message, 22, DOS.RETURN_FAIL);
        APTR.WriteUInt32(message, 26, unchecked((uint)error));
    }
}
