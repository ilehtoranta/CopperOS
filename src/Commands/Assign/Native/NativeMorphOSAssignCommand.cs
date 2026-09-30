using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// Bounded MorphOS 3.20 Assign mutation path through the public DOS assign
/// vectors.  MorphOS documents the classic NAME/TARGET multi-value grammar,
/// but the listing, dismount and filtering output contracts still need a
/// packed-binary or guest capture.  Those modes therefore fail closed rather
/// than being silently mapped to a different operation.
/// </summary>
public static class NativeMorphOSAssignCommand
{
    public const string Template =
        "NAME,TARGET/M,LIST/S,DISMOUNT/S,DEFER/S,PATH/S,ADD/S,REMOVE/S,VOLS/S,DIRS/S,DEVICES/S";
    public const uint ResultCount = 11;

    private const uint TargetGuard = 1024;

    public static int Run(out int ioError)
    {
        ioError = 0;
        if (!NativeCommandArguments.TryRead(Template, ResultCount,
                out var arguments))
        {
            ioError = arguments.IoError;
            DOS.PrintFault((DOS.Error)ioError, CString.FromPointer(0));
            return DOS.RETURN_FAIL;
        }

        var result = DOS.RETURN_FAIL;
        var error = 0;
        do
        {
            if (!TryRead(arguments, 0, out var name) || name.IsNull)
            {
                error = (int)DOS.Error.BadTemplate;
                break;
            }

            var list = ReadSwitch(arguments, 2);
            var dismount = ReadSwitch(arguments, 3);
            var defer = ReadSwitch(arguments, 4);
            var path = ReadSwitch(arguments, 5);
            var add = ReadSwitch(arguments, 6);
            var remove = ReadSwitch(arguments, 7);
            var volumes = ReadSwitch(arguments, 8);
            var dirs = ReadSwitch(arguments, 9);
            var devices = ReadSwitch(arguments, 10);

            // These modes require the original packed command's output and
            // handler protocol.  Do not reinterpret them as a mutation.
            if (list != 0 || dismount != 0 || volumes != 0 || dirs != 0 ||
                devices != 0 || (defer != 0 && path != 0) ||
                (add != 0 && remove != 0))
            {
                error = (int)DOS.Error.BadTemplate;
                break;
            }

            if (!TryRead(arguments, 1, out var targetVector) ||
                targetVector.IsNull)
            {
                error = (int)DOS.Error.BadTemplate;
                break;
            }

            var targetSlot = targetVector;
            var first = true;
            var targets = 0u;
            while (targets < TargetGuard)
            {
                var targetRaw = APTR.ReadUInt32(targetSlot, 0);
                if (targetRaw == 0) break;
                targets++;
                targetSlot = APTR.FromPointer(targetSlot.Raw + sizeof(uint));
            }
            if (targets == 0 || targets == TargetGuard)
            {
                error = (int)DOS.Error.BadTemplate;
                break;
            }

            if (defer != 0 || path != 0)
            {
                if (targets != 1)
                {
                    error = (int)DOS.Error.BadTemplate;
                    break;
                }
                var target = APTR.ReadUInt32(targetVector, 0);
                var assigned = defer != 0
                    ? DOS.AssignLate(CString.FromPointer(name.Raw),
                        CString.FromPointer(target))
                    : DOS.AssignPath(CString.FromPointer(name.Raw),
                        CString.FromPointer(target));
                error = (int)DOS.IoErr();
                if (assigned == 0) break;
                result = DOS.RETURN_OK;
                error = 0;
                break;
            }

            targetSlot = targetVector;
            while (true)
            {
                var targetRaw = APTR.ReadUInt32(targetSlot, 0);
                if (targetRaw == 0) break;
                var target = DOS.LockRaw(CString.FromPointer(targetRaw),
                    DOS.LockMode.Shared);
                if (target.IsNull)
                {
                    error = (int)DOS.IoErr();
                    break;
                }

                var operationResult = first && remove == 0 && add == 0
                    ? DOS.AssignLock(CString.FromPointer(name.Raw), target)
                    : remove != 0
                        ? DOS.RemAssignList(CString.FromPointer(name.Raw), target)
                        : DOS.AssignAdd(CString.FromPointer(name.Raw), target);
                var operationError = (int)DOS.IoErr();

                // AssignLock and AssignAdd consume a successful lock. Remove
                // compares without consuming it, so only release locks that
                // remain owned by this invocation.
                var consumed = operationResult != 0 && remove == 0;
                if (!consumed) DOS.UnLock(target);
                if (operationResult == 0)
                {
                    error = operationError;
                    break;
                }

                first = false;
                targetSlot = APTR.FromPointer(targetSlot.Raw + sizeof(uint));
            }

            if (error == 0) result = DOS.RETURN_OK;
        }
        while (false);

        arguments.Release();
        if (error != 0)
        {
            ioError = error;
            DOS.SetIoErr((DOS.Error)error);
            DOS.PrintFault((DOS.Error)error, CString.FromPointer(0));
            return DOS.RETURN_FAIL;
        }

        ioError = (int)DOS.IoErr();
        DOS.SetIoErr((DOS.Error)ioError);
        return result;
    }

    private static bool TryRead(NativeCommandArguments arguments, uint index,
        out APTR value)
    {
        value = APTR.Null;
        if (!arguments.TryGetResult(index, out var raw)) return false;
        value = APTR.FromPointer(raw);
        return true;
    }

    private static uint ReadSwitch(NativeCommandArguments arguments, uint index) =>
        arguments.TryGetResult(index, out var value) ? value : 0;
}
