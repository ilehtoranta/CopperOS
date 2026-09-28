using System.Text;
using Amiga;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

/// <summary>
/// Supplied-vector receipt for Delete's final DeleteFile stage. It deliberately
/// excludes matcher, protection, retry, diagnostic, and recursive ownership.
/// </summary>
internal sealed record DeleteObjectProbeCase(string Name, bool Succeeds,
    int Error = Invocation.InitialIoError, bool NullName = false);

internal sealed partial class ProbeFixture
{
    public const string DeleteObjectProbeSuite = "delete-object-native-entry-vector-fixture";

    private List<object> RunDeleteObjectProbeCases()
    {
        ProbeCase[] cases =
        [
            Case("file-deleted", "Work:File", true),
            Case("directory-deleted", "Work:EmptyDir", true),
            Case("handler-failure", "Work:Denied", false, (int)DOS.Error.DeleteProtected),
            Case("null-name", "", false, (int)DOS.Error.BadTemplate, true),
        ];
        var reports = new List<object>();
        foreach (var test in cases) reports.AddRange(Execute([test], false));
        reports.AddRange(Execute(
        [
            cases[0] with { Name = "interleaved-file" },
            cases[2] with { Name = "interleaved-failure" }
        ], true));
        Bus.AssertImageUnchanged();
        return reports;
    }

    private static ProbeCase Case(string label, string name, bool succeeds,
        int error = Invocation.InitialIoError, bool nullName = false) =>
        new(label, "", succeeds ? DOS.RETURN_OK : DOS.RETURN_FAIL,
            succeeds ? 0 : error, "")
        {
            EntryLength = 4,
            DeleteObject = new(name, succeeds, error, nullName)
        };

    private void PrepareDeleteObjectProbe(Invocation invocation)
    {
        var definition = invocation.Definition.DeleteObject ??
            throw new InvalidOperationException("Missing Delete object definition.");
        var name = definition.NullName ? 0u : invocation.Arguments + 16;
        Bus.Long(invocation.Arguments, name);
        if (!definition.NullName)
        {
            Encoding.Latin1.GetBytes(definition.Name).CopyTo(
                Bus.Memory.AsSpan((int)name));
            Bus.Memory[name + (uint)definition.Name.Length] = 0;
        }
    }

    private void VerifyDeleteObjectProbe(Invocation invocation)
    {
        var definition = invocation.Definition.DeleteObject ??
            throw new InvalidOperationException("Missing Delete object definition.");
        Require(invocation.Events.Count(item => item == "DeleteFile") ==
            (definition.NullName ? 0 : 1), "Delete object call count differs.");
        Require(invocation.Events.Count(item => item == "IoErr") ==
            (!definition.Succeeds && !definition.NullName ? 1 : 0),
            "Delete object error observation differs.");
    }

    private void RegisterDeleteObjectProbeDos(uint baseAddress)
    {
        Register(baseAddress, DosLvo.DeleteFile, "DeleteFile", (state, invocation) =>
        {
            var definition = invocation.Definition.DeleteObject!;
            Require(!definition.NullName && Bus.CString(state.D[1]) == definition.Name,
                "Delete object name ABI differs.");
            invocation.IoError = definition.Error;
            return definition.Succeeds ? 1u : 0;
        });
        Register(baseAddress, DosLvo.IoErr, "IoErr", (_, invocation) =>
            unchecked((uint)invocation.IoError));
        Register(baseAddress, DosLvo.SetIoErr, "SetIoErr", (state, invocation) =>
        {
            invocation.IoError = unchecked((int)state.D[1]);
            return 0;
        });
    }
}
