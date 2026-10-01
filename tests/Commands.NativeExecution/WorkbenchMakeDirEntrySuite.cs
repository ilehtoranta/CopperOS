using Amiga;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

internal sealed partial class ProbeFixture
{
    private List<object> RunWorkbench31MakeDirEntryCases()
    {
        static MakeDirDirectoryStep Create(string name, uint result,
            int error = 0) => new(MakeDirDirectoryOperation.CreateDir, name,
            0, result, error);
        static MakeDirDirectoryStep Lock(string name, uint result,
            int error = 205) => new(MakeDirDirectoryOperation.Lock, name,
            unchecked((uint)DOS.LockMode.Read), result, error);
        static MakeDirDirectoryStep Unlock(uint argument) =>
            new(MakeDirDirectoryOperation.UnLock, "", argument, 0);

        static ProbeCase Case(string name, MakeDirEntryCase definition,
            bool workbench = false, bool missingDos = false,
            int? entryLength = null, bool nullArgument = false) =>
            new(name, "ignored\n", definition.Result, definition.Error,
                definition.Output)
            {
                MakeDir = definition,
                Workbench = workbench,
                MissingDos = missingDos,
                WritesOwnProcessError = missingDos,
                EntryLength = entryLength,
                NullArgumentPointer = nullArgument
            };

        var cases = new List<ProbeCase>
        {
            Case("no-name", new(null, false, 20, 0,
                "No name given\n", [])),
            Case("empty-vector", new([], false, 0, 0, "", [])),
            Case("create-success", new(["new"], false, 0, 0, "",
                [Lock("new", 0), Create("new", 0x151), Unlock(0x151)])),
            Case("existing", new(["old"], false, 10, 0,
                "old already exists\n",
                [Lock("old", 0x151, 0), Unlock(0x151)])),
            Case("create-failure", new(["bad"], false, 10, 221,
                "Can't create directory bad\n",
                [Lock("bad", 0, 205), Create("bad", 0, 221)])
                { FaultCodes = [221] }),
            Case("failure-then-success", new(["bad", "good"], false, 10,
                221, "Can't create directory bad\n",
                [Lock("bad", 0, 205), Create("bad", 0, 221),
                 Lock("good", 0, 205), Create("good", 0x152),
                 Unlock(0x152)]) { FaultCodes = [221] }),
            Case("parser-failure", new(null, false, 20, 119, "", [])
                { ParserError = 119, FaultCodes = [119] }),
            Case("result-allocation-failure", new(null, false, 20, 103,
                "", []) { AllocationFailure = true, FaultCodes = [103] }),
            Case("missing-dos", new(null, false, 20,
                (int)DOS.Error.InvalidResidentLibrary, "", []), missingDos: true),
            Case("workbench-startup", new(null, false, 10,
                (int)DOS.Error.ObjectWrongType, "", []), workbench: true),
            Case("negative-entry-length", new(null, false, 10,
                (int)DOS.Error.LineTooLong, "", []), entryLength: -1),
            Case("null-entry-buffer", new(null, false, 10,
                (int)DOS.Error.LineTooLong, "", []), entryLength: 4,
                nullArgument: true)
        };

        var reports = new List<object>();
        foreach (var test in cases)
            reports.AddRange(Execute([test], false));
        reports.AddRange(Execute([
            Case("interleaved-left", new(["left"], false, 0, 0, "",
                [Lock("left", 0), Create("left", 0x161),
                 Unlock(0x161)])),
            Case("interleaved-right", new(["right"], false, 10, 222,
                "Can't create directory right\n",
                [Lock("right", 0, 205), Create("right", 0, 222)])
                { FaultCodes = [222] })
        ], true));
        Bus.AssertImageUnchanged();
        return reports;
    }
}
