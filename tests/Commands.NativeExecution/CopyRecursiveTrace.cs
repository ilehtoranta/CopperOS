using Amiga;

namespace CopperOS.Commands.NativeExecution;

/// <summary>
/// Reference-derived matcher input for normal-command recursive COPY coverage.
/// Completion counts refer to payload files, never to directory records.
/// </summary>
internal readonly record struct CopyRecursiveMatchRecord(
    string Path, string Name, bool Directory, bool Exited,
    int CompletedBeforeNext, string Destination);

internal static class CopyRecursiveTrace
{
    public static readonly CopyRecursiveMatchRecord[] Dangling =
    [
        new("SYS:first","first",true,false,0,"RAM:first"),
        new("SYS:second","second",true,false,0,"RAM:second"),
        new("SYS:second/child","child",false,false,0,"RAM:second/child"),
        new("SYS:second","second",true,true,1,"RAM:second"),
    ];
    // Two siblings force ascent to the root between copies. The pending file
    // executes on the DIDDIR iteration, before MatchNext observes completion.
    public static readonly CopyRecursiveMatchRecord[] Siblings =
    [
        new("SYS:first", "first", true, false, 0, "RAM:first"),
        new("SYS:first/child", "child", false, false, 0, "RAM:first/child"),
        new("SYS:first", "first", true, true, 1, "RAM:first"),
        new("SYS:second", "second", true, false, 1, "RAM:second"),
        new("SYS:second/child", "child", false, false, 1, "RAM:second/child"),
        new("SYS:second", "second", true, true, 2, "RAM:second"),
    ];

    public static readonly CopyRecursiveMatchRecord[] Nested =
    [
        new("SYS:first","first",true,false,0,"RAM:first"),
        new("SYS:first/deep","deep",true,false,0,"RAM:first/deep"),
        new("SYS:first/deep/child","child",false,false,0,"RAM:first/deep/child"),
        new("SYS:first/deep","deep",true,true,1,"RAM:first/deep"),
        new("SYS:first","first",true,true,1,"RAM:first"),
        new("SYS:second","second",true,false,1,"RAM:second"),
        new("SYS:second/child","child",false,false,1,"RAM:second/child"),
        new("SYS:second","second",true,true,2,"RAM:second"),
    ];

    public static readonly CopyRecursiveMatchRecord[] Literal =
    [
        new("SYS:","",true,false,0,"RAM:"),
        ..Siblings,
        new("SYS:","",true,true,2,"RAM:"),
    ];

    public static void Put(CommandTestBus bus, uint anchor, int index, CopyRecursiveMatchRecord[]? trace=null)
    {
        var record = (trace??Siblings)[index];
        var fib = anchor + (uint)DosLayout.AnchorPath.Info;
        // Poisoning/replacing the complete live FIB makes stale matcher aliases
        // observable when deferred work should consume its private snapshot.
        bus.Memory.AsSpan((int)fib, FileInfoBlock.SizeInBytes).Clear();
        bus.Long(fib + FileInfoBlock.DirEntryTypeOffset,
            record.Directory ? 2u : unchecked((uint)-3));
        bus.Long(fib+FileInfoBlock.ProtectionOffset,(uint)(FileProtection.Archive|FileProtection.Pure|FileProtection.Script)|(uint)(index&15));
        bus.Long(fib+FileInfoBlock.DateDaysOffset,(uint)(100+index));
        PutString(bus,fib+FileInfoBlock.CommentOffset,"record-"+index);
        PutString(bus, fib + FileInfoBlock.FileNameOffset, record.Name);
        PutString(bus, anchor + (uint)DosLayout.AnchorPath.PathBuffer, record.Path);
        bus.Memory[anchor + (uint)DosLayout.AnchorPath.Flags] =
            (byte)(record.Exited ? AnchorPathFlags.DidDirectory : AnchorPathFlags.IsWild);
    }

    private static void PutString(CommandTestBus bus, uint address, string value) =>
        System.Text.Encoding.Latin1.GetBytes(value + "\0").CopyTo(bus.Memory.AsSpan((int)address));
}
