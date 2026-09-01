namespace CopperOS.Commands.MakeDirNativeExecution;

// All strings and directory outcomes are supplied test vectors. They do not
// implement ReadArgs grammar or a filesystem. Expected command outcomes are
// explicit contract cases, shared by the original and generated executions.
internal sealed record DirectoryStep(string Name, uint ExistingLock, uint CreatedLock,
    int CreateError = 0, int LockError = 205);

internal sealed record MakeDirCase(string Id, string[]? Names, DirectoryStep[] Steps,
    int ExpectedResult, int ExpectedError, string ExpectedVPrintfBytes)
{
    public int[] ExpectedFaultCodes { get; init; } = [];
    public int ParserError { get; init; }
    public bool MissingDos { get; init; }
    public bool AllocationFailure { get; init; }
    public int? VPrintfResult { get; init; }
    public bool PrintFaultFailure { get; init; }
    public uint StackBytes { get; init; } = 16384;
}

internal sealed record CaseBatch(bool Interleaved, MakeDirCase[] Cases);

internal static class MakeDirCases
{
    private static DirectoryStep New(string name, uint raw = 0x12341, int lockError = 205) =>
        new(name, 0, raw, LockError: lockError);
    private static DirectoryStep Exists(string name, uint raw = 0x23451) => new(name, raw, 0);
    private static DirectoryStep Fails(string name, int error, int lockError = 205) =>
        new(name, 0, 0, error, lockError);

    public static List<CaseBatch> Comparable()
    {
        MakeDirCase[] ordinary =
        [
            new("MK31-A01.absent-list", null, [], 20, 0, "No name given\n"),
            new("MK31-A02.present-empty-vector", [], [], 0, 0, ""),
            new("MK31-A03.supplied-parser-error119", null, [], 20, 119, "")
                { ParserError = 119, ExpectedFaultCodes = [119] },
            new("MK31-A03.supplied-parser-error103", null, [], 20, 103, "")
                { ParserError = 103, ExpectedFaultCodes = [103] },
            new("MK31-A04.supplied-vector-order", ["third", "first", "second"],
                [New("third", 0x12011), New("first", 0x12021), New("second", 0x12031)], 0, 0, ""),
            new("MK31-A05.supplied-space-latin1-percent", ["café dir", "100% ready"],
                [Exists("café dir"), Fails("100% ready", 221)], 10, 0,
                "café dir already exists\nCan't create directory 100% ready\n"),
            new("MK31-A05.supplied-empty-string", [""], [Exists("")], 10, 0, " already exists\n"),
            new("MK31-A07.supplied-option-like-names", ["ALL", "-p", "#?"],
                [New("ALL", 0x12011), New("-p", 0x12021), New("#?", 0x12031)], 0, 0, ""),
            new("MK31-B01.single-new", ["new"], [New("new")], 0, 0, ""),
            new("MK31-B01.several-new", ["one", "two"], [New("one"), New("two", 0x12349)], 0, 0, ""),
            new("MK31-B02.existing-directory", ["dir"], [Exists("dir")], 10, 0, "dir already exists\n"),
            new("MK31-B02.existing-file", ["file"], [Exists("file")], 10, 0, "file already exists\n"),
            new("MK31-B03.duplicate-new-existing", ["same", "same"],
                [New("same"), Exists("same", 0x23459)], 10, 0, "same already exists\n"),
            new("MK31-B04.parent-then-child", ["parent", "parent/child"],
                [New("parent"), New("parent/child", 0x12349)], 0, 0, ""),
            new("MK31-B04.child-fails-parent-succeeds", ["parent/child", "parent"],
                [Fails("parent/child", 204), New("parent")], 10, 204, "Can't create directory parent/child\n")
                { ExpectedFaultCodes = [204] },
            new("MK31-E01.failure-then-success", ["bad", "good"],
                [Fails("bad", 221), New("good")], 10, 221, "Can't create directory bad\n")
                { ExpectedFaultCodes = [221] },
            new("MK31-E02.first-error-retained", ["bad1", "bad2"],
                [Fails("bad1", 214), Fails("bad2", 221)], 10, 214,
                "Can't create directory bad1\nCan't create directory bad2\n") { ExpectedFaultCodes = [214] },
            new("MK31-E03.existing-clears-error", ["bad", "present"],
                [Fails("bad", 205), Exists("present")], 10, 0,
                "Can't create directory bad\npresent already exists\n"),
            new("MK31-E04.existing-suppresses-later-error", ["present", "bad"],
                [Exists("present"), Fails("bad", 222)], 10, 0,
                "present already exists\nCan't create directory bad\n"),
            new("MK31-E05.error-existing-error-success", ["a", "b", "c", "d"],
                [Fails("a", 103), Exists("b"), Fails("c", 221), New("d")], 10, 0,
                "Can't create directory a\nb already exists\nCan't create directory c\n"),
            new("MK31-E06.lock-in-use-create-success", ["busy"], [New("busy", lockError: 202)], 0, 0, ""),
            new("MK31-E06.lock-error-is-not-selected", ["denied"], [Fails("denied", 214, 224)], 10, 214,
                "Can't create directory denied\n") { ExpectedFaultCodes = [214] },
            new("MK31-E07.supplied-race-object-exists", ["race"], [Fails("race", 203)], 10, 203,
                "Can't create directory race\n") { ExpectedFaultCodes = [203] },
            new("MK31-E08.vprintf-failure-existing", ["present"], [Exists("present")], 10, 0, "")
                { VPrintfResult = -1 },
            new("MK31-E08.vprintf-short-no-name", null, [], 20, 0, "No n") { VPrintfResult = 4 },
            new("MK31-E08.printfault-failure", ["bad"], [Fails("bad", 221)], 10, 221,
                "Can't create directory bad\n") { ExpectedFaultCodes = [221], PrintFaultFailure = true },
            new("MK31-E01.supplied-create-failure-zero-ioerr", ["bad"], [Fails("bad", 0)], 10, 0,
                "Can't create directory bad\n"),
            new("MK31-L01.missing-dos", null, [], 20, 122, "") { MissingDos = true }
        ];
        var batches = ordinary.Select(c => new CaseBatch(false, [c])).ToList();
        for (var cycle = 0; cycle < 2; cycle++)
        {
            batches.Add(new(false, [new($"MK31-R01.repeat{cycle}-failure", ["bad"], [Fails("bad", 221)],
                10, 221, "Can't create directory bad\n") { ExpectedFaultCodes = [221] }]));
            batches.Add(new(false, [new($"MK31-R01.repeat{cycle}-success", ["good"], [New("good")], 0, 0, "")]));
        }
        batches.Add(new(true,
        [
            new("MK31-R02.interleaved-left", ["left1", "left2"], [New("left1"), New("left2", 0x12349)],
                0, 0, "") { StackBytes = 4096 },
            new("MK31-R02.interleaved-right", ["right"], [Exists("right")], 10, 0, "right already exists\n")
        ]));
        batches.Add(new(true,
        [
            new("MK31-R02.interleaved-error-reset", ["bad", "present"], [Fails("bad", 214), Exists("present")],
                10, 0, "Can't create directory bad\npresent already exists\n"),
            new("MK31-R02.interleaved-error-retained", ["other"], [Fails("other", 221)], 10, 221,
                "Can't create directory other\n") { ExpectedFaultCodes = [221], StackBytes = 4096 }
        ]));
        batches.Add(new(true,
        [
            new("MK31-R02.interleaved-parser-error", null, [], 20, 119, "")
                { ParserError = 119, ExpectedFaultCodes = [119] },
            new("MK31-R02.interleaved-no-name", null, [], 20, 0, "No name given\n")
        ]));
        return batches;
    }

    public static CaseBatch GeneratedOnlyAllocationFailure() => new(false,
    [
        new("MK31-E09.generated-only-slot-allocation-failure", null, [], 20, 103, "")
            { AllocationFailure = true, ExpectedFaultCodes = [103] }
    ]);
}
