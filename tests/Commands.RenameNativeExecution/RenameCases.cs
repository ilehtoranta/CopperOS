namespace CopperOS.Commands.RenameNativeExecution;

internal static class RenameCases
{
    public const int ExpectedReturned = 40;
    public const int ExpectedGuardStops = 8;
    public const uint DestinationLock = 0x23451;
    public const uint SourceLock = 0x34561;

    public static IReadOnlyList<RenameBatch> All()
    {
        var cases = new List<RenameCase>
        {
            new("RN31-L01.missing-dos", ["old"], "new", [], 20)
                { MissingDos = true, ExpectedFinalIoError = 122 },
            new("RN31-L02.initial-ctrl-c", ["old"], "new", [Fault(304)], 20)
                { BreakPending = true }
        };
        for (var ordinal = 1; ordinal <= 4; ordinal++)
            cases.Add(new($"RN31-M01.allocation-{ordinal}", ["old"], "new", [Fault(103)], 20)
                { FailAllocation = ordinal });
        cases.Add(new("RN31-M02.synthetic-parser-failure-zero-ioerr", ["old"], "new", [Error(0)], 20)
            { ParserFails = true, ParserError = 0, SyntheticProviderCombination = true });
        cases.Add(new("RN31-D01.initial-205-prefix", ["raw"], "new",
            [FirstFailure("raw", 205, 205), Error(205), Failure("raw", "new"), End(), Fault(205)], 20));
        cases.Add(new("RN31-D01.synthetic-return103-ioerr214", ["raw"], "new",
            [FirstFailure("raw", 103, 214), Error(214), End(), Fault(214)], 20)
            { SyntheticProviderCombination = true });
        cases.Add(new("RN31-D02.direct-missing-target", ["old"], "new",
            [.. DirectHead("old", "parsed-old", "new"), Rename("parsed-old", "new")], 0));
        cases.Add(new("RN31-D02.supplied-examine-failure-direct", ["old"], "new",
            [First("old", "match/ignored", "ignored"), Lock("new", DestinationLock), Examine(0, 2), End(),
             Pattern("old", "parsed-old"), Rename("parsed-old", "new")], 0)
            { SyntheticProviderCombination = true });
        cases.Add(new("RN31-D04.samelock-zero-distinct-bptrs", ["old"], "target",
            [First("old", "match/ignored", "ignored"), Lock("target", DestinationLock), Examine(1, 2),
             Lock("old", SourceLock), Same(0), Unlock(SourceLock), End(), Pattern("old", "old"), Rename("old", "target")], 0));
        cases.Add(new("RN31-D04.synthetic-wild-samelock-zero", ["raw#?"], "target",
            [First("raw#?", "match/ignored", "ignored", wild: true), Lock("target", DestinationLock), Examine(1, 2),
             Lock("raw#?", SourceLock), Same(0), Unlock(SourceLock), End(), Pattern("raw#?", "supplied-literal"),
             Rename("supplied-literal", "target")], 0) { SyntheticProviderCombination = true });
        cases.Add(DirectorySameLock("RN31-D04.samelock-one-directory", 1));
        cases.Add(DirectorySameLock("RN31-D04.samelock-minus-one-directory", -1));
        cases.Add(new("RN31-N01.colon-prefix", ["raw"], "target",
            [.. DirectoryHead("raw", "target", "VOL:"), First("raw", "SRC:old", "old"), Next(),
             Progress("SRC:old", "VOL:old"), Rename("SRC:old", "VOL:old"), End()], 0));
        cases.Add(new("RN31-N01.component-prefix", ["raw"], "target",
            [.. DirectoryHead("raw", "target", "VOL:drawer"), First("raw", "SRC:old", "old"), Next(),
             Progress("SRC:old", "VOL:drawer/old"), Rename("SRC:old", "VOL:drawer/old"), End()], 0));
        cases.Add(TwoMatches("RN31-A04.nonzero-quiet", quiet: 0x80000000));
        cases.Add(TwoPatterns("RN31-W01.two-patterns", 232));
        cases.Add(TwoMatches("RN31-W02.next-overwrites-anchor"));
        cases.Add(TwoPatterns("RN31-W03.next304-then-another-pattern", 304));
        cases.Add(new("RN31-W03.next103-current-item-still-renamed", ["raw"], "target",
            [.. DirectoryHead("raw", "target", "OUT:"), First("raw", "SRC:old", "old"), Next(103, 103),
             Progress("SRC:old", "OUT:old"), Rename("SRC:old", "OUT:old"), End()], 0));
        cases.Add(DirectoryFailure("RN31-E01.directory-first-failure214", 214));
        cases.Add(TwoMatches("RN31-E01.directory-later-failure221", secondFailure: 221));
        cases.Add(new("RN31-W04.later-matchfirst205-no-prefix", ["one", "two"], "target",
            [First("one", "preflight", "preflight"), Lock("target", DestinationLock), Examine(1, 2), End(), Name("OUT:"),
             First("one", "SRC:first", "first"), Next(), Progress("SRC:first", "OUT:first"), Rename("SRC:first", "OUT:first"), End(),
             FirstFailure("two", 205, 205), Error(205), End(), Fault(205)], 20));
        cases.Add(new("RN31-E04.vprintf-failure-does-not-stop-rename", ["raw"], "target",
            [.. DirectoryHead("raw", "target", "OUT:"), First("raw", "SRC:old", "old"), Next(),
             Progress("SRC:old", "OUT:old", -1), Rename("SRC:old", "OUT:old"), End()], 0));
        cases.Add(DirectoryFailure("RN31-E04.printfault-failure-keeps-return20", 214, faultResult: 0));
        cases.Add(new("RN31-R03.success-does-not-clear-ambient-error", ["old"], "new",
            [.. DirectHead("old", "old", "new"), Rename("old", "new", error: 214)], 0)
            { PoisonCleanup = false, ExpectedFinalIoError = 214, SyntheticProviderCombination = true });
        cases.Add(DirectoryFailure("RN31-R03.cleanup-preserving-selected-error", 214) with
            { PoisonCleanup = false, ExpectedFinalIoError = 214 });
        cases.Add(new("RN31-E02.synthetic-zero-error-raw-diagnostic", ["raw'quoted"], "new",
            [.. DirectHead("raw'quoted", "parsed-name", "new"), Rename("parsed-name", "new", 0, 0), Error(0),
             Failure("raw'quoted", "new"), SetError(0)], 0) { SyntheticProviderCombination = true });
        var exactPrefix = "V:" + new string('x', 240);
        var exactLeaf = new string('n', 12); // 242 + slash + 12 + NUL = 256 bytes.
        cases.Add(new("RN31-N03.destination-exact-capacity", ["raw"], "target",
            [.. DirectoryHead("raw", "target", exactPrefix), First("raw", "SRC:old", exactLeaf), Next(),
             Progress("SRC:old", exactPrefix + "/" + exactLeaf), Rename("SRC:old", exactPrefix + "/" + exactLeaf), End()], 0));
        cases.Add(DirectoryFailure("RN31-A04.quiet-does-not-suppress-failure", 214, quiet: 1));

        if (cases.Count != 32) throw new InvalidOperationException("Ordinary Rename matrix changed without review.");
        var batches = cases.Select(c => new RenameBatch(false, [c])).ToList();
        for (var cycle = 0; cycle < 2; cycle++)
        {
            batches.Add(new(false, [cases[9] with { Id = $"RN31-R04.repeat{cycle}-success" }]));
            batches.Add(new(false, [DirectoryFailure($"RN31-R04.repeat{cycle}-failure", 214)]));
        }
        batches.Add(new(true,
        [
            TwoMatches("RN31-R04.interleaved-pattern-left") with { StackBytes = 4096 },
            DirectoryFailure("RN31-R04.interleaved-failure-right", 221)
        ]));
        batches.Add(new(true,
        [
            cases[9] with { Id = "RN31-R04.interleaved-direct-left" },
            TwoMatches("RN31-R04.interleaved-quiet-right", quiet: 0xffffffff) with { StackBytes = 4096 }
        ]));

        foreach (var guarded in GuardedCases()) batches.Add(new(false, [guarded]));
        var all = batches.SelectMany(b => b.Cases).ToArray();
        if (all.Count(c => c.ExpectedGuard is null) != ExpectedReturned ||
            all.Count(c => c.ExpectedGuard is not null) != ExpectedGuardStops ||
            all.Select(c => c.Id).Distinct(StringComparer.Ordinal).Count() != all.Length)
            throw new InvalidOperationException("Rename fixture IDs/counts changed without review.");
        return batches;
    }

    private static RenameCase[] GuardedCases()
    {
        var tooLongPrefix = "V:" + new string('x', 247);
        return
        [
            new("RN31-M02.parser119-unstarted-matchend", ["old"], "new", [Error(119), End()], 20)
            { ParserFails = true, ParserError = 119, ExpectedGuard = "match-end-before-match-first", ExpectedGuardOrigin = "supplied-vector" },
            new("RN31-D03.multiple-nondirectory-search-not-ended", ["one", "two"], "new",
                [First("one", "SRC:one", "one"), Lock("new", 0), DestinationDiagnostic("new")], 0)
            { ExpectedGuard = "search-not-ended-before-anchor-free", ExpectedGuardOrigin = "supplied-vector" },
            new("RN31-D03.wild-nondirectory-search-not-ended", ["#?"], "new",
                [First("#?", "SRC:one", "one", wild: true), Lock("new", 0), DestinationDiagnostic("new")], 0)
            { ExpectedGuard = "search-not-ended-before-anchor-free", ExpectedGuardOrigin = "supplied-vector" },
            new("RN31-E01.direct-failure-repeated-matchend", ["raw'quoted"], "new",
                [.. DirectHead("raw'quoted", "parsed-name", "new"), Rename("parsed-name", "new", 0, 214), Error(214),
                 Failure("raw'quoted", "new"), SetError(214), End()], 20)
            { ExpectedGuard = "repeated-match-end", ExpectedGuardOrigin = "supplied-vector" },
            new("RN31-P02.unchecked-parsepattern-output", ["raw"], "new",
                [First("raw", "SRC:raw", "raw"), Lock("new", 0), End(), Pattern("raw", null, -1), Rename("unused", "new")], 0)
            { ExpectedGuard = "read-outside-owned-storage", ExpectedGuardOrigin = "supplied-vector-read", SyntheticProviderCombination = true },
            new("RN31-N02.failed-namefromlock-prefix-underread", ["raw"], "target",
                [First("raw", "SRC:raw", "raw"), Lock("target", DestinationLock), Examine(1, 2), Lock("raw", 0), End(),
                 Name(null, 0)], 0)
            { ExpectedGuard = "read-outside-owned-storage", ExpectedGuardOrigin = "native-read", SyntheticProviderCombination = true },
            new("RN31-N03.destination-append-overflow", ["raw"], "target",
                [.. DirectoryHead("raw", "target", tooLongPrefix), First("raw", "SRC:old", "0123456789")], 0)
            { ExpectedGuard = "write-outside-owned-storage", ExpectedGuardOrigin = "native-write" },
            new("RN31-D01.synthetic-matchfirst-error-zero-ioerr", ["raw"], "new",
                [FirstFailure("raw", 205, 0), Error(0)], 0)
            { ExpectedGuard = "search-not-ended-before-anchor-free", ExpectedGuardOrigin = "supplied-vector", SyntheticProviderCombination = true }
        ];
    }

    private static RenameCase DirectorySameLock(string id, int result) => new(id, ["raw"], "target",
        [First("raw", "preflight", "preflight"), Lock("target", DestinationLock), Examine(1, 2),
         Lock("raw", SourceLock), Same(result), Unlock(SourceLock), End(), Name("OUT:"),
         First("raw", "SRC:old", "old"), Next(), Progress("SRC:old", "OUT:old"), Rename("SRC:old", "OUT:old"), End()], 0);

    private static RenameCase DirectoryFailure(string id, int error, int faultResult = 1, uint quiet = 0)
    {
        var steps = DirectoryHead("raw", "target", "OUT:").ToList();
        steps.AddRange([First("raw", "SRC:old", "old"), Next()]);
        if (quiet == 0) steps.Add(Progress("SRC:old", "OUT:old"));
        steps.AddRange([Rename("SRC:old", "OUT:old", 0, error), Error(error), Failure("SRC:old", "OUT:old"),
            SetError(error), End(), Fault(error, faultResult)]);
        return new(id, ["raw"], "target", steps.ToArray(), 20) { Quiet = quiet };
    }

    private static RenameCase TwoMatches(string id, uint quiet = 0, int secondFailure = 0)
    {
        var steps = DirectoryHead("#?", "target", "OUT:", wild: true).ToList();
        steps.Add(First("#?", "SRC:first", "first", wild: true));
        steps.Add(new(RenameOperation.MatchNext) { Result = 0, FullPath = "SRC:second", Leaf = "second" });
        if (quiet == 0) steps.Add(Progress("SRC:first", "OUT:first"));
        steps.Add(Rename("SRC:first", "OUT:first"));
        steps.Add(Next());
        if (quiet == 0) steps.Add(Progress("SRC:second", "OUT:second"));
        steps.Add(Rename("SRC:second", "OUT:second", secondFailure == 0 ? 1 : 0, secondFailure));
        if (secondFailure != 0)
            steps.AddRange([Error(secondFailure), Failure("SRC:second", "OUT:second"), SetError(secondFailure)]);
        steps.Add(End());
        if (secondFailure != 0) steps.Add(Fault(secondFailure));
        return new(id, ["#?"], "target", steps.ToArray(), secondFailure == 0 ? 0 : 20) { Quiet = quiet };
    }

    private static RenameCase TwoPatterns(string id, int firstNextResult) => new(id, ["one", "two"], "target",
        [First("one", "preflight", "preflight"), Lock("target", DestinationLock), Examine(1, 2), End(), Name("OUT:"),
         First("one", "SRC:first", "first"), Next(firstNextResult, firstNextResult),
         Progress("SRC:first", "OUT:first"), Rename("SRC:first", "OUT:first"), End(),
         First("two", "SRC:second", "second"), Next(), Progress("SRC:second", "OUT:second"),
         Rename("SRC:second", "OUT:second"), End()], 0);

    private static RenameStep[] DirectHead(string raw, string parsed, string target) =>
        [First(raw, "unrelated-preflight-path", "preflight"), Lock(target, 0), End(), Pattern(raw, parsed)];
    private static RenameStep[] DirectoryHead(string raw, string target, string prefix, bool wild = false) =>
        [First(raw, "preflight", "preflight", wild), Lock(target, DestinationLock), Examine(1, 2), Lock(raw, 0), End(), Name(prefix)];
    private static RenameStep First(string pattern, string path, string leaf, bool wild = false) =>
        new(RenameOperation.MatchFirst) { First = pattern, FullPath = path, Leaf = leaf, Wild = wild };
    private static RenameStep FirstFailure(string pattern, int result, int error) =>
        new(RenameOperation.MatchFirst) { First = pattern, Result = result, IoError = error };
    private static RenameStep Next(int result = 232, int error = 232) => new(RenameOperation.MatchNext)
        { Result = result, IoError = error, FullPath = "NEXT-CLOBBERED-PATH", Leaf = "NEXT-CLOBBERED-NAME" };
    private static RenameStep End() => new(RenameOperation.MatchEnd);
    private static RenameStep Lock(string name, uint raw) => new(RenameOperation.Lock) { First = name, RawBptr = raw };
    private static RenameStep Examine(int result, int type) => new(RenameOperation.Examine)
        { RawBptr = DestinationLock, Result = result, EntryType = type };
    private static RenameStep Same(int result) => new(RenameOperation.SameLock)
        { RawBptr = SourceLock, OtherBptr = DestinationLock, Result = result };
    private static RenameStep Unlock(uint raw) => new(RenameOperation.UnLock) { RawBptr = raw };
    private static RenameStep Pattern(string source, string? output, int result = 0) => new(RenameOperation.ParsePattern)
        { First = source, Second = output, Result = result, WriteOutput = output is not null };
    private static RenameStep Name(string? prefix, int result = 1) => new(RenameOperation.NameFromLock)
        { RawBptr = DestinationLock, First = prefix, Result = result, WriteOutput = prefix is not null };
    private static RenameStep Rename(string oldName, string newName, int result = 1, int error = 777) => new(RenameOperation.Rename)
        { First = oldName, Second = newName, Result = result, IoError = error };
    private static RenameStep Error(int value) => new(RenameOperation.IoErr) { Result = value };
    private static RenameStep SetError(int value) => new(RenameOperation.SetIoErr) { Result = value };
    private static RenameStep Fault(int code, int result = 1) => new(RenameOperation.PrintFault) { IoError = code, Result = result };
    private static RenameStep Progress(string first, string second, int? returned = null) => new(RenameOperation.VPrintf)
        { Format = "Renaming %s as %s\n", First = first, Second = second, Rendered = $"Renaming {first} as {second}\n", OutputCount = returned };
    private static RenameStep Failure(string first, string second) => new(RenameOperation.VPrintf)
        { Format = "Can't rename %s as %s because ", First = first, Second = second, Rendered = $"Can't rename {first} as {second} because " };
    private static RenameStep DestinationDiagnostic(string name) => new(RenameOperation.VPrintf)
        { Format = "Destination \"%s\" is not a directory.\n", First = name, Rendered = $"Destination \"{name}\" is not a directory.\n" };
}
