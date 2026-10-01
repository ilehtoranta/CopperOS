using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// Workbench 3.1 <c>C:Execute</c> CLI body for the ROM Shell (template
/// <c>FILE/A</c>, original <c>execute 37.11</c>).
///
/// Execute does not run commands itself. It turns the script into a work file
/// and makes that file the calling CLI's current input; the Shell then reads
/// the script's lines after Execute returns:
/// <list type="number">
/// <item>The first item of the command tail is the script; everything after
/// it is the script's own argument line, bound to the <c>.KEY</c> template
/// with DOS ReadArgs.</item>
/// <item>Each source line is processed in order. Dot directives (<c>.KEY</c>,
/// <c>.K</c>, <c>.DEFAULT</c>, <c>.DEF</c>, <c>.BRA</c>, <c>.KET</c>,
/// <c>.DOLLAR</c>, <c>.DOT</c>, the binary's own keyword list) take effect
/// from the following line and are not copied; dot comments are dropped;
/// every other line is written with its <c>&lt;key&gt;</c> references
/// substituted.</item>
/// <item>When the CLI is already reading a script (current input is not its
/// standard input), the unread rest of that script is appended, so the
/// caller's remaining lines run after this script. The old input is closed,
/// and a previous Execute work file is deleted.</item>
/// <item>The work file (<c>T:Command-nn-Tmm</c>, else <c>:T/</c>) is
/// reopened as <c>cli_CurrentInput</c>, recorded in <c>cli_CommandFile</c>,
/// and the CLI is marked non-interactive.</item>
/// </list>
///
/// Behaviour fixed by Workbench 3.1 captures (docs/Commands/Workbench31MorphOS320/
/// reference-captures/execute-wb31-*.json): positional substitution, .DEF with
/// whitespace or '=', first .DEF wins, undeclared and bare .DEF are ignored,
/// only the first value token is kept, a line using an empty .DEF value is
/// dropped, <c>.DEF =x</c> fails with RC 10 / Result2 0 before any script line
/// runs, and .BRA/.KET only affect later lines. Not established by any capture
/// and chosen here: diagnostic wording and stream, /S /N /M substitution
/// forms, the work-file name, and the lack of a .DOL alias (absent from the
/// binary's keyword list).
/// </summary>
public static class Workbench31ExecuteCommand
{
    public const string Template = "FILE/A";

    // ---- Invocation work area (one AllocMem, cleared) --------------------
    private const int LineOffset = 0, LineCapacity = 1024;
    private const int OutOffset = 1024, OutCapacity = 2048;
    private const int KeyTemplateOffset = 3072, KeyTemplateCapacity = 256;
    private const int ResultsOffset = 3328, MaxKeys = 32;
    private const int DefaultsOffset = 3456;          // MaxKeys x 8 bytes
    private const int PoolOffset = 3712, PoolCapacity = 512;
    private const int TempNameOffset = 4224, NameCapacity = 64;
    private const int OldNameOffset = 4288;
    private const int FileOffset = 4352, FileCapacity = 256;
    private const int CSourceOffset = 4608;           // 12-byte CSource
    private const int FormatArgsOffset = 4624;        // VPrintf argument LONGs
    private const int EmptyLineOffset = 4640;         // "\n" for an empty tail
    private const int NumberOffset = 4644;            // 12 bytes
    private const uint WorkBytes = 4672;

    // dos/rdargs.h: DOS_RDARGS for AllocDosObject; ReadItem results.
    private const uint DosRdArgs = 5;
    private const int ItemNothing = 0, ItemUnquoted = 1;

    // Template modifier flags recorded for a .KEY item.
    private const uint ModSwitch = 1, ModNumber = 2, ModMulti = 4;

    // Default table flags.
    private const byte DefaultPresent = 1, DefaultEmpty = 2;

    private enum LineAction { Emit, Drop, Fail }

    private struct State
    {
        internal APTR Work;
        internal APTR Tail;
        internal uint TailLength;
        internal APTR RdArgs;
        internal BPTR Temp;
        internal bool KeyDefined;
        internal byte Bra, Ket, Dollar, Dot;
        internal uint PoolUsed;
        internal uint OutLength;
    }

    /// <summary>
    /// Runs Execute over the raw command tail the entry received. DOS must be
    /// open; the caller keeps startup ownership and publishes the result.
    /// </summary>
    public static int Run(int argumentLength, APTR argumentText, out int ioError)
    {
        ioError = 0;
        var cli = DOS.Cli();
        if (cli.IsNull)
        {
            ioError = (int)DOS.Error.ObjectWrongType;
            return DOS.RETURN_FAIL;
        }

        var work = Exec.AllocMem(WorkBytes,
            Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear);
        if (work.IsNull)
        {
            ioError = (int)DOS.Error.NoFreeStore;
            DOS.PrintFault(DOS.Error.NoFreeStore, CString.FromPointer(0));
            return DOS.RETURN_FAIL;
        }

        var state = new State
        {
            Work = work, Bra = (byte)'<', Ket = (byte)'>',
            Dollar = (byte)'$', Dot = (byte)'.',
        };
        var result = RunWithWork(ref state, cli, argumentLength, argumentText,
            out ioError);
        if (state.RdArgs.IsNotNull)
        {
            DOS.FreeArgs(state.RdArgs);
            DOS.FreeDosObject(DosRdArgs, state.RdArgs);
        }
        Exec.FreeMem(work, WorkBytes);
        DOS.SetIoErr((DOS.Error)ioError);
        return result;
    }

    private static int RunWithWork(ref State st, APTR cli, int argumentLength,
        APTR argumentText, out int ioError)
    {
        ioError = 0;
        var file = At(st.Work, FileOffset);

        // FILE is the first item only; the rest of the line belongs to the
        // script's .KEY template, so FILE/A must not see it.
        var source = At(st.Work, CSourceOffset);
        APTR.WriteUInt32(source, 0, argumentText.Raw);
        APTR.WriteUInt32(source, 4, (uint)argumentLength);
        APTR.WriteUInt32(source, 8, 0);
        var item = argumentLength > 0
            ? DOS.ReadItem(file, FileCapacity - 1, source)
            : ItemNothing;
        if (item == ItemUnquoted && APTR.ReadUInt8(file, 0) == (byte)'?' &&
            APTR.ReadUInt8(file, 1) == 0)
            return RunTemplatePrompt(ref st, out ioError);
        if (item <= ItemNothing)
        {
            ioError = (int)(item == ItemNothing
                ? DOS.Error.RequiredArgumentMissing : DOS.Error.BadTemplate);
            DOS.PrintFault((DOS.Error)ioError, CString.FromPointer(0));
            return DOS.RETURN_FAIL;
        }

        var consumed = APTR.ReadUInt32(source, 8);
        if (consumed > (uint)argumentLength) consumed = (uint)argumentLength;
        st.Tail = APTR.FromPointer(argumentText.Raw + consumed);
        st.TailLength = (uint)argumentLength - consumed;
        return RunScript(ref st, cli, file, out ioError);
    }

    /// <summary>
    /// "Execute ?" (and a "?" item): let ReadArgs prompt for FILE/A as the
    /// ROM commands do; the reply carries no script arguments.
    /// </summary>
    private static int RunTemplatePrompt(ref State st, out int ioError)
    {
        ioError = 0;
        if (!NativeCommandArguments.TryRead(Template, 1, out var arguments))
        {
            ioError = arguments.IoError;
            DOS.PrintFault((DOS.Error)ioError, CString.FromPointer(0));
            return DOS.RETURN_FAIL;
        }
        var cli = DOS.Cli();
        var file = At(st.Work, FileOffset);
        arguments.TryGetResult(0, out var name);
        CopyCString(APTR.FromPointer(name), file, FileCapacity);
        arguments.Release();
        st.Tail = APTR.Null;
        st.TailLength = 0;
        return RunScript(ref st, cli, file, out ioError);
    }

    private static int RunScript(ref State st, APTR cli, APTR file, out int ioError)
    {
        ioError = 0;
        var script = DOS.OpenRaw(CString.FromPointer(file.Raw), DOS.FileMode.OldFile);
        if (script.IsNull)
        {
            ioError = (int)DOS.IoErr();
            DOS.PrintFault((DOS.Error)ioError, CString.FromPointer(file.Raw));
            return DOS.RETURN_FAIL;
        }

        var tempName = At(st.Work, TempNameOffset);
        st.Temp = CreateWorkFile(cli, tempName);
        if (st.Temp.IsNull)
        {
            ioError = (int)DOS.IoErr();
            DOS.Close(script);
            DOS.PrintFault((DOS.Error)ioError, "Execute: can't create work file");
            return DOS.RETURN_FAIL;
        }

        var line = At(st.Work, LineOffset);
        var failed = false;
        while (DOS.FGets(script, line, LineCapacity - 1).IsNotNull)
        {
            var length = CStringLength(line, LineCapacity);
            var action = ProcessLine(ref st, line, length);
            if (action == LineAction.Fail) { failed = true; break; }
            if (action == LineAction.Emit && st.OutLength > 0)
                DOS.FWrite(st.Temp, At(st.Work, OutOffset), 1, st.OutLength);
        }
        DOS.Close(script);

        if (failed)
        {
            DOS.Close(st.Temp);
            DOS.DeleteFile(CString.FromPointer(tempName.Raw));
            return DOS.RETURN_ERROR;
        }

        // Inside a script: the caller's unread lines follow this script.
        var current = BPTR.FromRaw(APTR.ReadUInt32(cli, DosLayout.CommandLineInterface.CurrentInput));
        var standard = BPTR.FromRaw(APTR.ReadUInt32(cli, DosLayout.CommandLineInterface.StandardInput));
        var oldName = At(st.Work, OldNameOffset);
        APTR.WriteUInt8(oldName, 0, 0);
        if (current.IsNotNull && current.Raw != standard.Raw)
        {
            int count;
            while ((count = DOS.FRead(current, line, 1, LineCapacity)) > 0)
                DOS.FWrite(st.Temp, line, 1, (uint)count);
            ReadCommandFile(cli, oldName);
            DOS.Close(current);
        }
        DOS.Close(st.Temp);
        st.Temp = BPTR.Null;
        if (IsWorkFileName(oldName))
            DOS.DeleteFile(CString.FromPointer(oldName.Raw));

        var input = DOS.OpenRaw(CString.FromPointer(tempName.Raw), DOS.FileMode.OldFile);
        if (input.IsNull)
        {
            ioError = (int)DOS.IoErr();
            // The old input is already gone if we were inside a script; fall
            // back to standard input so the Shell does not read a closed handle.
            if (current.IsNotNull && current.Raw != standard.Raw)
                APTR.WriteUInt32(cli, DosLayout.CommandLineInterface.CurrentInput, standard.Raw);
            DOS.DeleteFile(CString.FromPointer(tempName.Raw));
            DOS.PrintFault((DOS.Error)ioError, "Execute: can't open work file");
            return DOS.RETURN_FAIL;
        }

        APTR.WriteUInt32(cli, DosLayout.CommandLineInterface.CurrentInput, input.Raw);
        APTR.WriteUInt32(cli, DosLayout.CommandLineInterface.Interactive, 0);
        WriteCommandFile(cli, tempName);
        return DOS.RETURN_OK;
    }

    // ---- Line processing ---------------------------------------------------

    private static LineAction ProcessLine(ref State st, APTR line, uint length)
    {
        st.OutLength = 0;
        if (length > 0 && APTR.ReadUInt8(line, 0) == st.Dot)
            return ProcessDirective(ref st, line, length);
        if (!st.KeyDefined)
        {
            for (var i = 0u; i < length; i++) Append(ref st, APTR.ReadUInt8(line, (int)i));
            return LineAction.Emit;
        }
        return Substitute(ref st, line, length);
    }

    private static LineAction ProcessDirective(ref State st, APTR line, uint length)
    {
        // ". text", a lone "." and ".<newline>" are comments.
        if (length == 1) return LineAction.Drop;
        var next = APTR.ReadUInt8(line, 1);
        if (IsBlank(next) || next == (byte)'\n' || next == (byte)'\r')
            return LineAction.Drop;

        var wordEnd = 1u;
        while (wordEnd < length && IsLetter(APTR.ReadUInt8(line, (int)wordEnd))) wordEnd++;
        if (wordEnd == 1)
            return Fail("Invalid directive\n");

        // FindArg against the original binary's keyword list.
        var saved = APTR.ReadUInt8(line, (int)wordEnd);
        APTR.WriteUInt8(line, (int)wordEnd, 0);
        var keyword = DOS.FindArg("KEY,K,DEFAULT,DEF,BRA,KET,DOLLAR,DOT",
            CString.FromPointer(line.Raw + 1));
        APTR.WriteUInt8(line, (int)wordEnd, saved);
        if (keyword < 0)
            return Fail("Invalid directive\n");

        var argStart = wordEnd;
        while (argStart < length && IsBlank(APTR.ReadUInt8(line, (int)argStart))) argStart++;
        var argEnd = length;
        while (argEnd > argStart && IsTrailing(APTR.ReadUInt8(line, (int)argEnd - 1))) argEnd--;
        var arg = APTR.FromPointer(line.Raw + argStart);
        var argLength = argEnd - argStart;

        switch (keyword)
        {
            case 0: case 1: return DefineKey(ref st, arg, argLength);
            case 2: case 3: return DefineDefault(ref st, arg, argLength);
        }
        if (argLength == 0)
            return Fail("Invalid directive argument\n");
        var value = APTR.ReadUInt8(arg, 0);
        switch (keyword)
        {
            case 4: st.Bra = value; break;
            case 5: st.Ket = value; break;
            case 6: st.Dollar = value; break;
            default: st.Dot = value; break;
        }
        return LineAction.Drop;
    }

    private static LineAction DefineKey(ref State st, APTR arg, uint argLength)
    {
        if (st.KeyDefined)
            return Fail("More than one .KEY directive\n");
        if (argLength == 0 || argLength >= KeyTemplateCapacity)
            return Fail("Illegal KEY directive\n");

        var template = At(st.Work, KeyTemplateOffset);
        var items = 1u;
        for (var i = 0u; i < argLength; i++)
        {
            var c = APTR.ReadUInt8(arg, (int)i);
            if (c == (byte)',') items++;
            APTR.WriteUInt8(template, (int)i, c);
        }
        APTR.WriteUInt8(template, (int)argLength, 0);
        if (items > MaxKeys)
            return Fail("Illegal KEY directive\n");

        var rdArgs = DOS.AllocDosObject(DosRdArgs, APTR.Null);
        if (rdArgs.IsNull)
        {
            DOS.PrintFault(DOS.Error.NoFreeStore, CString.FromPointer(0));
            return LineAction.Fail;
        }
        // RDA_Source (CSource) is the script's argument line. ReadArgs needs a
        // newline-terminated source; the Shell's tail already ends with one.
        var tail = st.Tail;
        var tailLength = st.TailLength;
        if (tailLength == 0)
        {
            tail = At(st.Work, EmptyLineOffset);
            APTR.WriteUInt8(tail, 0, (byte)'\n');
            tailLength = 1;
        }
        APTR.WriteUInt32(rdArgs, 0, tail.Raw);
        APTR.WriteUInt32(rdArgs, 4, tailLength);
        APTR.WriteUInt32(rdArgs, 8, 0);

        if (DOS.ReadArgs(CString.FromPointer(template.Raw),
                At(st.Work, ResultsOffset), rdArgs).IsNull)
        {
            DOS.FreeDosObject(DosRdArgs, rdArgs);
            var args = At(st.Work, FormatArgsOffset);
            APTR.WriteUInt32(args, 0, template.Raw);
            DOS.VPrintf("Parameters unsuitable for key \"%s\"\n", args);
            return LineAction.Fail;
        }
        st.RdArgs = rdArgs;
        st.KeyDefined = true;
        return LineAction.Drop;
    }

    private static LineAction DefineDefault(ref State st, APTR arg, uint argLength)
    {
        var nameLength = 0u;
        while (nameLength < argLength)
        {
            var c = APTR.ReadUInt8(arg, (int)nameLength);
            if (IsBlank(c) || c == (byte)'=') break;
            nameLength++;
        }
        if (nameLength == 0)
            return argLength == 0 ? LineAction.Drop : Fail("Invalid directive argument\n");
        if (!FindKey(ref st, arg, nameLength, out var index, out _, out _, out _))
            return LineAction.Drop;           // undeclared: ignored
        var entry = At(st.Work, DefaultsOffset + (int)index * 8);
        if (APTR.ReadUInt8(entry, 0) != 0)
            return LineAction.Drop;           // first .DEF wins

        var p = nameLength;
        if (p < argLength && APTR.ReadUInt8(arg, (int)p) == (byte)'=') p++;
        else while (p < argLength && IsBlank(APTR.ReadUInt8(arg, (int)p))) p++;

        uint start, end;
        if (p < argLength && APTR.ReadUInt8(arg, (int)p) == (byte)'"')
        {
            start = p + 1;
            end = start;
            while (end < argLength && APTR.ReadUInt8(arg, (int)end) != (byte)'"') end++;
        }
        else
        {
            start = p;
            end = p;
            while (end < argLength && !IsBlank(APTR.ReadUInt8(arg, (int)end))) end++;
        }

        var valueLength = end - start;
        if (valueLength == 0)
        {
            APTR.WriteUInt8(entry, 0, DefaultPresent | DefaultEmpty);
            return LineAction.Drop;
        }
        if (st.PoolUsed + valueLength > PoolCapacity)
            return Fail("Key too long\n");
        var pool = At(st.Work, PoolOffset);
        for (var i = 0u; i < valueLength; i++)
            APTR.WriteUInt8(pool, (int)(st.PoolUsed + i), APTR.ReadUInt8(arg, (int)(start + i)));
        APTR.WriteUInt8(entry, 0, DefaultPresent);
        APTR.WriteUInt16(entry, 2, (ushort)st.PoolUsed);
        APTR.WriteUInt16(entry, 4, (ushort)valueLength);
        st.PoolUsed += valueLength;
        return LineAction.Drop;
    }

    private static LineAction Substitute(ref State st, APTR line, uint length)
    {
        var i = 0u;
        while (i < length)
        {
            var c = APTR.ReadUInt8(line, (int)i);
            if (c == st.Bra)
            {
                var close = i + 1;
                while (close < length)
                {
                    var k = APTR.ReadUInt8(line, (int)close);
                    if (k == st.Ket || k == (byte)'\n') break;
                    close++;
                }
                if (close < length && APTR.ReadUInt8(line, (int)close) == st.Ket)
                {
                    var nameStart = i + 1;
                    var split = nameStart;
                    while (split < close && APTR.ReadUInt8(line, (int)split) != st.Dollar) split++;
                    var name = APTR.FromPointer(line.Raw + nameStart);
                    if (split > nameStart &&
                        FindKey(ref st, name, split - nameStart, out var index,
                            out var modifiers, out var keyName, out var keyNameLength))
                    {
                        var hasInline = split < close;
                        if (!AppendValue(ref st, index, modifiers, keyName, keyNameLength))
                        {
                            if (hasInline)
                            {
                                for (var d = split + 1; d < close; d++)
                                    Append(ref st, APTR.ReadUInt8(line, (int)d));
                            }
                            else
                            {
                                var entry = At(st.Work, DefaultsOffset + (int)index * 8);
                                var flags = APTR.ReadUInt8(entry, 0);
                                // An empty .DEF value drops the whole line (WB 3.1 capture).
                                if ((flags & DefaultEmpty) != 0) return LineAction.Drop;
                                if ((flags & DefaultPresent) != 0)
                                {
                                    var pool = At(st.Work, PoolOffset);
                                    var offset = APTR.ReadUInt16(entry, 2);
                                    var count = APTR.ReadUInt16(entry, 4);
                                    for (var d = 0; d < count; d++)
                                        Append(ref st, APTR.ReadUInt8(pool, offset + d));
                                }
                            }
                        }
                        i = close + 1;
                        continue;
                    }
                }
            }
            Append(ref st, c);
            i++;
        }
        return LineAction.Emit;
    }

    /// <summary>Appends the ReadArgs value of a key; false when it was not given.</summary>
    private static bool AppendValue(ref State st, uint index, uint modifiers,
        APTR keyName, uint keyNameLength)
    {
        var value = APTR.ReadUInt32(At(st.Work, ResultsOffset), (int)index * 4);
        if (value == 0) return false;
        if ((modifiers & ModSwitch) != 0)
        {
            for (var i = 0u; i < keyNameLength; i++) Append(ref st, APTR.ReadUInt8(keyName, (int)i));
            return true;
        }
        if ((modifiers & ModNumber) != 0)
        {
            AppendNumber(ref st, (int)APTR.ReadUInt32(APTR.FromPointer(value), 0));
            return true;
        }
        if ((modifiers & ModMulti) != 0)
        {
            var vector = APTR.FromPointer(value);
            var first = true;
            for (var slot = 0; ; slot += 4)
            {
                var text = APTR.ReadUInt32(vector, slot);
                if (text == 0) break;
                if (!first) Append(ref st, (byte)' ');
                first = false;
                AppendCString(ref st, APTR.FromPointer(text));
            }
            return true;
        }
        AppendCString(ref st, APTR.FromPointer(value));
        return true;
    }

    /// <summary>
    /// Finds a .KEY item by any of its '='-separated names, case-insensitively.
    /// Reports its index, its /S /N /M modifiers and its first name.
    /// </summary>
    private static bool FindKey(ref State st, APTR name, uint nameLength,
        out uint index, out uint modifiers, out APTR firstName, out uint firstNameLength)
    {
        index = 0; modifiers = 0; firstName = APTR.Null; firstNameLength = 0;
        if (!st.KeyDefined) return false;
        var template = At(st.Work, KeyTemplateOffset);
        var pos = 0;
        var item = 0u;
        while (true)
        {
            var matched = false;
            var itemStart = pos;
            var firstLength = 0u;
            while (true)
            {
                var aliasStart = pos;
                byte c;
                while ((c = APTR.ReadUInt8(template, pos)) != 0 &&
                       c != (byte)'=' && c != (byte)'/' && c != (byte)',') pos++;
                var aliasLength = (uint)(pos - aliasStart);
                if (aliasStart == itemStart) firstLength = aliasLength;
                if (aliasLength == nameLength &&
                    EqualsIgnoreCase(APTR.FromPointer(template.Raw + (uint)aliasStart), name, nameLength))
                    matched = true;
                if (c != (byte)'=') break;
                pos++;
            }
            var mods = 0u;
            byte m;
            while ((m = APTR.ReadUInt8(template, pos)) != 0 && m != (byte)',')
            {
                var upper = ToUpper(m);
                if (upper == (byte)'S' || upper == (byte)'T') mods |= ModSwitch;
                else if (upper == (byte)'N') mods |= ModNumber;
                else if (upper == (byte)'M') mods |= ModMulti;
                pos++;
            }
            if (matched)
            {
                index = item;
                modifiers = mods;
                firstName = APTR.FromPointer(template.Raw + (uint)itemStart);
                firstNameLength = firstLength;
                return item < MaxKeys;
            }
            if (m != (byte)',') return false;
            pos++;
            item++;
        }
    }

    // ---- Work file and CLI state -------------------------------------------

    /// <summary>
    /// Creates T:Command-&lt;cli&gt;-T&lt;n&gt;, or the same name under :T/ when
    /// T: is not assigned. Existing names are skipped. Returns a write handle.
    /// </summary>
    private static BPTR CreateWorkFile(APTR cli, APTR name)
    {
        var process = Exec.FindTask(CString.FromPointer(0));
        var cliNumber = APTR.ReadUInt32(process, DosLayout.Process.TaskNumber) % 100;
        // An unassigned T: must fail quietly, not raise "Please insert volume T".
        var window = APTR.ReadUInt32(process, DosLayout.Process.WindowPointer);
        APTR.WriteUInt32(process, DosLayout.Process.WindowPointer, 0xFFFFFFFFu);
        var handle = BPTR.Null;
        for (var prefix = 0; prefix < 2 && handle.IsNull; prefix++)
        {
            for (var n = 1u; n < 100; n++)
            {
                var length = 0;
                if (prefix == 0) { Put(name, ref length, (byte)'T'); Put(name, ref length, (byte)':'); }
                else { Put(name, ref length, (byte)':'); Put(name, ref length, (byte)'T'); Put(name, ref length, (byte)'/'); }
                PutText(name, ref length, "Command-");
                PutTwoDigits(name, ref length, cliNumber);
                Put(name, ref length, (byte)'-');
                Put(name, ref length, (byte)'T');
                PutTwoDigits(name, ref length, n);
                APTR.WriteUInt8(name, length, 0);

                var existing = DOS.LockRaw(CString.FromPointer(name.Raw), DOS.LockMode.Read);
                if (existing.IsNotNull) { DOS.UnLock(existing); continue; }
                handle = DOS.OpenRaw(CString.FromPointer(name.Raw), DOS.FileMode.NewFile);
                break;                        // opened, or directory missing: next prefix
            }
        }
        var error = DOS.IoErr();
        APTR.WriteUInt32(process, DosLayout.Process.WindowPointer, window);
        DOS.SetIoErr(error);
        return handle;
    }

    private static bool IsWorkFileName(APTR name)
    {
        // Only ever delete a previous Execute work file, never a user script.
        var pos = 0;
        if (APTR.ReadUInt8(name, 0) == (byte)'T' && APTR.ReadUInt8(name, 1) == (byte)':') pos = 2;
        else if (APTR.ReadUInt8(name, 0) == (byte)':' && APTR.ReadUInt8(name, 1) == (byte)'T' &&
                 APTR.ReadUInt8(name, 2) == (byte)'/') pos = 3;
        else return false;
        return Matches(name, pos, "Command-");
    }

    private static void ReadCommandFile(APTR cli, APTR name)
    {
        APTR.WriteUInt8(name, 0, 0);
        var bstr = BPTR.FromRaw(APTR.ReadUInt32(cli, DosLayout.CommandLineInterface.CommandFile));
        if (bstr.IsNull) return;
        var address = bstr.Address;
        var length = (int)APTR.ReadUInt8(address, 0);
        if (length >= NameCapacity) length = NameCapacity - 1;
        for (var i = 0; i < length; i++)
            APTR.WriteUInt8(name, i, APTR.ReadUInt8(address, i + 1));
        APTR.WriteUInt8(name, length, 0);
    }

    private static void WriteCommandFile(APTR cli, APTR name)
    {
        var bstr = BPTR.FromRaw(APTR.ReadUInt32(cli, DosLayout.CommandLineInterface.CommandFile));
        if (bstr.IsNull) return;
        var address = bstr.Address;
        // The CLI's command-file buffer holds 40 bytes (length byte included).
        var length = (int)CStringLength(name, NameCapacity);
        if (length > 39) length = 39;
        APTR.WriteUInt8(address, 0, (byte)length);
        for (var i = 0; i < length; i++)
            APTR.WriteUInt8(address, i + 1, APTR.ReadUInt8(name, i));
    }

    // ---- Small helpers -------------------------------------------------------

    private static LineAction Fail(CString message)
    {
        DOS.VPrintf(message, APTR.Null);
        return LineAction.Fail;
    }

    private static APTR At(APTR work, int offset) => APTR.FromPointer(work.Raw + (uint)offset);

    private static void Append(ref State st, byte value)
    {
        if (st.OutLength >= OutCapacity) return;
        APTR.WriteUInt8(At(st.Work, OutOffset), (int)st.OutLength, value);
        st.OutLength++;
    }

    private static void AppendCString(ref State st, APTR text)
    {
        for (var i = 0; ; i++)
        {
            var c = APTR.ReadUInt8(text, i);
            if (c == 0) return;
            Append(ref st, c);
        }
    }

    private static void AppendNumber(ref State st, int value)
    {
        var digits = At(st.Work, NumberOffset);
        var magnitude = value < 0 ? 0u - (uint)value : (uint)value;
        var count = 0;
        do
        {
            APTR.WriteUInt8(digits, count++, (byte)('0' + magnitude % 10));
            magnitude /= 10;
        } while (magnitude != 0);
        if (value < 0) Append(ref st, (byte)'-');
        while (count > 0) Append(ref st, APTR.ReadUInt8(digits, --count));
    }

    private static void Put(APTR buffer, ref int length, byte value)
    {
        APTR.WriteUInt8(buffer, length, value);
        length++;
    }

    private static void PutText(APTR buffer, ref int length, CString text)
    {
        for (var i = 0; ; i++)
        {
            var c = APTR.ReadUInt8(APTR.FromPointer(CString.ToUInt32(text)), i);
            if (c == 0) return;
            Put(buffer, ref length, c);
        }
    }

    private static void PutTwoDigits(APTR buffer, ref int length, uint value)
    {
        Put(buffer, ref length, (byte)('0' + value / 10 % 10));
        Put(buffer, ref length, (byte)('0' + value % 10));
    }

    private static bool Matches(APTR buffer, int offset, CString text)
    {
        var t = APTR.FromPointer(CString.ToUInt32(text));
        for (var i = 0; ; i++)
        {
            var c = APTR.ReadUInt8(t, i);
            if (c == 0) return true;
            if (APTR.ReadUInt8(buffer, offset + i) != c) return false;
        }
    }

    private static void CopyCString(APTR source, APTR destination, int capacity)
    {
        var i = 0;
        if (source.IsNotNull)
        {
            for (; i < capacity - 1; i++)
            {
                var c = APTR.ReadUInt8(source, i);
                if (c == 0) break;
                APTR.WriteUInt8(destination, i, c);
            }
        }
        APTR.WriteUInt8(destination, i, 0);
    }

    private static uint CStringLength(APTR text, int capacity)
    {
        var i = 0;
        while (i < capacity && APTR.ReadUInt8(text, i) != 0) i++;
        return (uint)i;
    }

    private static bool EqualsIgnoreCase(APTR left, APTR right, uint length)
    {
        for (var i = 0u; i < length; i++)
            if (ToUpper(APTR.ReadUInt8(left, (int)i)) != ToUpper(APTR.ReadUInt8(right, (int)i)))
                return false;
        return true;
    }

    private static byte ToUpper(byte c) =>
        c >= (byte)'a' && c <= (byte)'z' ? (byte)(c - 32) : c;

    private static bool IsLetter(byte c) =>
        (c >= (byte)'a' && c <= (byte)'z') || (c >= (byte)'A' && c <= (byte)'Z');

    private static bool IsBlank(byte c) => c == (byte)' ' || c == (byte)'\t';

    private static bool IsTrailing(byte c) =>
        c == (byte)' ' || c == (byte)'\t' || c == (byte)'\n' || c == (byte)'\r';
}
