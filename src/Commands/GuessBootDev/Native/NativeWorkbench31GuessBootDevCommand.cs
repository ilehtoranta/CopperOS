using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// Bounded Workbench 3.1 GuessBootDev implementation.  The original
/// installation helper selects the highest-priority bootable device matching
/// the requested boot volume and writes its device name to DOS output.  The
/// parser, temporary name buffer, locks, process-window sentinel, and all
/// library leases remain invocation-local.
/// </summary>
public static class NativeWorkbench31GuessBootDevCommand
{
    public const string Template = "BOOTDISKNAME";
    public const uint ResultCount = 1;
    public const uint NameBufferBytes = 300;

    private const uint ExpansionBootNodeOffset = 0x4a;
    private const int BootNodeTypeOffset = 8;
    private const int BootNodePriorityOffset = 9;
    private const int BootNodeDeviceOffset = 0x10;
    private const int DeviceNodeFlagsOffset = 0x10;
    private const int DeviceNodeStartupOffset = 0x1c;
    private const int DeviceNodeNameOffset = 0x28;
    private const uint BootNodeType = 0x10;
    private const byte DeviceNodeDisabled = 0x80;
    private const uint DosEnvironmentMinimum = 0x13;
    private const int DosEnvironmentRequiredOffset = 0x4c;
    private const sbyte InitialPriority = sbyte.MinValue;

    public static int Run(out int ioError)
    {
        ioError = 0;
        APTR utilityLibrary = APTR.Null;
        APTR expansionLibrary = APTR.Null;
        APTR nameBuffer = APTR.Null;
        BPTR systemLock = BPTR.Null;
        BPTR requestedLock = BPTR.Null;
        APTR process = APTR.Null;
        var oldWindowPointer = 0u;
        var windowPointerChanged = false;
        var result = DOS.RETURN_FAIL;
        var error = 0;
        var arguments = default(NativeCommandArguments);

        do
        {
            // The replacement does not call a utility.library vector; retain
            // the original lease with the lowest existence floor.
            utilityLibrary = Exec.OpenLibraryRaw(Utility.Name, 0);
            if (utilityLibrary.IsNull) break;
            // The boot-node base is present in the V33 expansion.library ABI.
            expansionLibrary = Exec.OpenLibraryRaw(Expansion.Name, 33);
            if (expansionLibrary.IsNull) break;

            if (!NativeCommandArguments.TryRead(Template, ResultCount,
                    out arguments))
            {
                error = arguments.IoError;
                DOS.PrintFault((DOS.Error)error, CString.FromPointer(0));
                break;
            }

            nameBuffer = Exec.AllocMem(NameBufferBytes,
                Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear);
            if (nameBuffer.IsNull) break;

            process = Exec.FindTask(CString.FromPointer(0));
            if (process.IsNull) break;
            oldWindowPointer = APTR.ReadUInt32(process,
                DosLayout.Process.WindowPointer);
            APTR.WriteUInt32(process, DosLayout.Process.WindowPointer,
                uint.MaxValue);
            windowPointerChanged = true;

            systemLock = DOS.LockRaw("SYS:", DOS.LockMode.Shared);
            if (systemLock.IsNull) break;

            if (arguments.TryGetResult(0, out var requested) && requested != 0)
                requestedLock = DOS.LockRaw(CString.FromPointer(requested),
                    DOS.LockMode.Shared);

            if (systemLock.IsNotNull && requestedLock.IsNotNull &&
                DOS.SameLock(systemLock, requestedLock) == 0)
            {
                var bootNode = APTR.FromPointer(APTR.ReadUInt32(
                    expansionLibrary, unchecked((int)ExpansionBootNodeOffset)));
                var priority = InitialPriority;
                while (bootNode.IsNotNull)
                {
                    var deviceNode = APTR.FromPointer(APTR.ReadUInt32(
                        bootNode, BootNodeDeviceOffset));
                    if (deviceNode.IsNotNull &&
                        APTR.ReadUInt8(bootNode, BootNodeTypeOffset) ==
                        BootNodeType &&
                        (APTR.ReadUInt8(deviceNode, DeviceNodeFlagsOffset) &
                            DeviceNodeDisabled) == 0)
                    {
                        var candidatePriority = unchecked((sbyte)
                            APTR.ReadUInt8(bootNode, BootNodePriorityOffset));
                        if (candidatePriority > priority &&
                            IsUsableFileSystem(deviceNode))
                        {
                            if (CopyDeviceName(deviceNode, nameBuffer))
                            {
                                priority = candidatePriority;
                                var length = CStringLength(nameBuffer);
                                APTR.WriteUInt8(nameBuffer,
                                    unchecked((int)length), (byte)':');
                                APTR.WriteUInt8(nameBuffer,
                                    unchecked((int)length + 1), (byte)'\n');
                                APTR.WriteUInt8(nameBuffer,
                                    unchecked((int)length + 2), 0);
                            }
                        }
                    }
                    bootNode = APTR.FromPointer(APTR.ReadUInt32(bootNode, 0));
                }
            }

            if (APTR.ReadUInt8(nameBuffer, 0) == 0)
            {
                if (systemLock.IsNotNull && DOS.NameFromLock(systemLock,
                        nameBuffer, (int)NameBufferBytes) != 0)
                {
                    var length = CStringLength(nameBuffer);
                    APTR.WriteUInt8(nameBuffer, unchecked((int)length), (byte)':');
                    APTR.WriteUInt8(nameBuffer, unchecked((int)length + 1), (byte)'\n');
                    APTR.WriteUInt8(nameBuffer, unchecked((int)length + 2), 0);
                }
            }

            if (APTR.ReadUInt8(nameBuffer, 0) != 0)
                DOS.PutStr(CString.FromPointer(nameBuffer));
        }
        while (false);

        if (requestedLock.IsNotNull) DOS.UnLock(requestedLock);
        if (systemLock.IsNotNull) DOS.UnLock(systemLock);
        if (windowPointerChanged)
            APTR.WriteUInt32(process, DosLayout.Process.WindowPointer,
                oldWindowPointer);
        if (arguments.IsSuccess) arguments.Release();
        if (nameBuffer.IsNotNull) Exec.FreeMem(nameBuffer, NameBufferBytes);
        if (expansionLibrary.IsNotNull) Exec.CloseLibrary(expansionLibrary);
        if (utilityLibrary.IsNotNull) Exec.CloseLibrary(utilityLibrary);

        ioError = error;
        DOS.SetIoErr((DOS.Error)error);
        return result;
    }

    private static bool IsUsableFileSystem(APTR deviceNode)
    {
        var startup = BPTR.FromRaw(APTR.ReadUInt32(deviceNode,
            DeviceNodeStartupOffset)).Address;
        if (startup.IsNull) return false;
        var environment = BPTR.FromRaw(APTR.ReadUInt32(startup,
            DosLayout.FileSysStartupMsg.Environment)).Address;
        if (environment.IsNull || APTR.ReadUInt32(environment,
                DosLayout.DosEnvec.TableSize) < DosEnvironmentMinimum)
            return false;
        return APTR.ReadUInt32(environment, DosEnvironmentRequiredOffset) != 0;
    }

    private static bool CopyDeviceName(APTR deviceNode, APTR destination)
    {
        var bstr = BPTR.FromRaw(APTR.ReadUInt32(deviceNode,
            DeviceNodeNameOffset)).Address;
        if (bstr.IsNull) return false;
        var length = APTR.ReadUInt8(bstr, 0);
        if (length == 0 || length + 2u >= NameBufferBytes) return false;
        for (var index = 0u; index < length; index++)
            APTR.WriteUInt8(destination, unchecked((int)index),
                APTR.ReadUInt8(bstr, unchecked((int)index + 1)));
        APTR.WriteUInt8(destination, length, 0);
        return true;
    }

    private static uint CStringLength(APTR value)
    {
        var length = 0u;
        while (APTR.ReadUInt8(value, unchecked((int)length)) != 0)
            length++;
        return length;
    }
}
