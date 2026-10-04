using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// Workbench 3.1 Assign mutation path through the public DOS assign vectors.
/// The observed classic template is retained verbatim.  Listing, filtering,
/// and DISMOUNT are deliberately left for the source-bound follow-up because
/// their original output and handler protocol are not established yet.
/// </summary>
public static class NativeWorkbench31AssignCommand
{
    public const string Template =
        "NAME,TARGET/M,LIST/S,EXISTS/S,DISMOUNT/S,DEFER/S,PATH/S,ADD/S,REMOVE/S,VOLS/S,DIRS/S,DEVICES/S";
    public const uint ResultCount = 12;

    private const uint TargetGuard = 1024;

    public static int Run(out int ioError)
    {
        ioError = 0;
        if (!NativeCommandArguments.TryRead(Template, ResultCount,
                out var arguments))
        {
            ioError = arguments.IoError;
            DOS.PrintFault((DOS.Error)ioError, CString.FromPointer(0));
            // The classic Assign cleanup still calls FreeArgs with a null
            // result after a failed parse. Preserve that observable vector
            // sequence and its ambient IoErr effect.
            DOS.FreeArgs(APTR.Null);
            return DOS.RETURN_FAIL;
        }

        var result = DOS.RETURN_FAIL;
        var error = 0;
        do
        {
            if (!TryRead(ref arguments, 0, out var name) || name.IsNull)
            {
                error = (int)DOS.Error.BadTemplate;
                break;
            }

            var list = ReadSwitch(ref arguments, 2);
            var exists = ReadSwitch(ref arguments, 3);
            var dismount = ReadSwitch(ref arguments, 4);
            var defer = ReadSwitch(ref arguments, 5);
            var path = ReadSwitch(ref arguments, 6);
            var add = ReadSwitch(ref arguments, 7);
            var remove = ReadSwitch(ref arguments, 8);
            var volumes = ReadSwitch(ref arguments, 9);
            var dirs = ReadSwitch(ref arguments, 10);
            var devices = ReadSwitch(ref arguments, 11);

            // These modes need a separately captured reference contract. Do
            // not silently reinterpret them as a mutation operation.
            if (list != 0 || dismount != 0 || volumes != 0 || dirs != 0 ||
                devices != 0 || exists != 0 || (defer != 0 && path != 0) ||
                (add != 0 && remove != 0))
            {
                error = (int)DOS.Error.BadTemplate;
                break;
            }

            if (!TryRead(ref arguments, 1, out var targetVector) ||
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
                // compares without consuming it. Unlock only when ownership
                // remains with this invocation.
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

    private static bool TryRead(ref NativeCommandArguments arguments, uint index,
        out APTR value)
    {
        value = APTR.Null;
        if (!arguments.TryGetResult(index, out var raw)) return false;
        value = APTR.FromPointer(raw);
        return true;
    }

    private static uint ReadSwitch(ref NativeCommandArguments arguments, uint index) =>
        arguments.TryGetResult(index, out var value) ? value : 0;
}
