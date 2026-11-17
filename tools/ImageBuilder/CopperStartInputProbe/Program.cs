using System.Text;
using System.Text.Json;
using CopperMod.Amiga.Lightweight;

// Test-only interactive probe. All commands and marker writes are performed by
// the guest; the host only supplies keyboard events and observes guest memory.
if (args.Length != 5 || (args[4] != "race" && args[4] != "control" && args[4] != "run-script" && args[4] != "nonresident-script" && args[4] != "recovery" && args[4] != "nonresident-recovery" && args[4] != "script-recovery" && args[4] != "nonresident-script-recovery"))
    throw new ArgumentException("Usage: CopperStartInputProbe <rom> <adf> <extension-rom> <output-base> <race|control|run-script|nonresident-script>");

var helperDisk = Environment.GetEnvironmentVariable("COPPEROS_PROBE_DF1");
using var machine = new LightweightA500Machine(new LightweightA500Configuration
{
    AgnusModel = LightweightAgnusModel.Mos8375Pal2M,
    ChipRamBytes = 2 * 1024 * 1024,
    SlowRamBytes = 0,
    FloppyDriveCount = string.IsNullOrEmpty(helperDisk) ? 1 : 2,
});
machine.LoadKickstart(File.ReadAllBytes(args[0]));
machine.LoadExtensionRom(File.ReadAllBytes(args[2]));
machine.MountAdf(File.ReadAllBytes(args[1]));
machine.SetDriveWriteProtected(0, false);
if (!string.IsNullOrEmpty(helperDisk))
    machine.MountAdf(1, File.ReadAllBytes(helperDisk));

byte Read(uint address) => address < machine.ChipRam.Length ? machine.ChipRam.Span[(int)address] : (byte)0;
uint Long(uint address) => ((uint)Read(address) << 24) | ((uint)Read(address + 1) << 16) |
                           ((uint)Read(address + 2) << 8) | Read(address + 3);
var glyphs = new Dictionary<ulong, char>
{
    [0x1c22262a32221c00UL]='0',[0x0818080808081c00UL]='1',[0x1c22020408103e00UL]='2',
    [0x3c02021c02023c00UL]='3',[0x040c14243e040400UL]='4',[0x3e20203c02023c00UL]='5',
    [0x1c20203c22221c00UL]='6',[0x3e02040810101000UL]='7',[0x1c22221c22221c00UL]='8',
    [0x1c22221e02021c00UL]='9',[0x0000000000000000UL]=' ',[0x1c22223e22222200UL]='A',
    [0x3c22223c22223c00UL]='B',[0x1e20202020201e00UL]='C',[0x3c22222222223c00UL]='D',
    [0x3e20203c20203e00UL]='E',[0x3e20203c20202000UL]='F',[0x1e20202e22221e00UL]='G',
    [0x2222223e22222200UL]='H',[0x3e08080808083e00UL]='I',[0x0e04040424241800UL]='J',
    [0x2224283028242200UL]='K',[0x2020202020203e00UL]='L',[0x22362a2a22222200UL]='M',
    [0x22322a2622222200UL]='N',[0x1c22222222221c00UL]='O',[0x3c22223c20202000UL]='P',
    [0x1c2222222a241a00UL]='Q',[0x3c22223c28242200UL]='R',[0x1e20201c02023c00UL]='S',
    [0x3e08080808080800UL]='T',[0x2222222222221c00UL]='U',[0x2222222222140800UL]='V',
    [0x2222222a2a2a1400UL]='W',[0x2222140814222200UL]='X',[0x2222140808080800UL]='Y',
    [0x3e02040810203e00UL]='Z',[0x00000000000c0c00UL]='.',[0x000c0c000c0c0000UL]=':',
    [0x0000003e00000000UL]='-',[0x0202040810202000UL]='/',[0x2010080408102000UL]='>'
};
string Screen()
{
    var control = Long(0x880); var console = Long(control + 32); var bitmap = Long(console + 128);
    if (control == 0 || console == 0 || bitmap == 0 || bitmap + 16000 > machine.ChipRam.Length) return "";
    var text = new StringBuilder();
    for (var row = 0; row < 25; row++)
    {
        for (var col = 0; col < 80; col++)
        {
            ulong glyph = 0;
            for (var y = 0; y < 8; y++) glyph = (glyph << 8) | Read(bitmap + (uint)((row * 8 + y) * 80 + col));
            text.Append(glyphs.GetValueOrDefault(glyph, '?'));
        }
        text.Append('\n');
    }
    return text.ToString();
}
void Run(int frames) { for (var i = 0; i < frames; i++) machine.ExecuteFrame(); }
uint FindTask(string wanted)
{
    var exec = Long(4);
    bool Matches(uint task)
    {
        if (task == 0) return false;
        var name = Long(task + 10);
        var match = Read(task + 8) == 13 && name != 0;
        for (var i = 0; match && i < wanted.Length; i++) match = Read(name + (uint)i) == wanted[i];
        return match && Read(name + (uint)wanted.Length) == 0;
    }
    // The currently running task is in neither the ready nor waiting list.
    // Include it before reporting that EndCLI removed the Shell.
    var current = Long(exec + 276);
    if (Matches(current)) return current;
    foreach (var offset in new uint[] { 406, 420 })
    {
        var list = exec + offset;
        for (var task = Long(list); task != 0 && task != list + 4; task = Long(task))
        {
            if (Matches(task)) return task;
        }
    }
    return 0;
}
uint FindShell() => FindTask("CopperOS Shell");
bool Ready()
{
    var last = Screen().Split('\n').LastOrDefault(row => !string.IsNullOrWhiteSpace(row))?.Trim();
    return last != null && last.StartsWith("2.") && last.EndsWith(">");
}
var keys = new Dictionary<char, byte>
{
    ['1']=0x01,['2']=0x02,['3']=0x03,['4']=0x04,['5']=0x05,
    ['6']=0x06,['7']=0x07,['8']=0x08,['9']=0x09,['0']=0x0a,
    ['q']=0x10,['w']=0x11,['e']=0x12,['r']=0x13,['t']=0x14,['y']=0x15,['u']=0x16,['i']=0x17,['o']=0x18,['p']=0x19,
    ['a']=0x20,['s']=0x21,['d']=0x22,['f']=0x23,['g']=0x24,['h']=0x25,['j']=0x26,['k']=0x27,['l']=0x28,
    ['z']=0x31,['x']=0x32,['c']=0x33,['v']=0x34,['b']=0x35,['n']=0x36,['m']=0x37,[' ']=0x40,
    [':']=0x29,['>']=0x39,['<']=0x38,['-']=0x0b,['"']=0x2a,
};
void ReturnKey() { machine.SetKeyState(0x44, true); Run(4); machine.SetKeyState(0x44, false); Run(20); }
void Type(string line)
{
    for (var n = 0; n < 20000 && !Ready(); n += 20) Run(20);
    if (!Ready()) throw new InvalidOperationException("Shell prompt missing before " + line + "\n" + Screen());
    foreach (var c in line)
    {
        var shifted = c is ':' or '>' or '<' or '"';
        if (shifted) { machine.SetKeyState(0x60, true); Run(4); }
        var key = keys[c]; machine.SetKeyState(key, true); Run(4);
        machine.SetKeyState(key, false); Run(4);
        if (shifted) { machine.SetKeyState(0x60, false); Run(4); }
    }
    ReturnKey();
}

var booted = false;
var bootFrameLimit = int.TryParse(Environment.GetEnvironmentVariable("COPPEROS_PROBE_BOOT_FRAMES"), out var requestedFrames)
    && requestedFrames > 0 ? requestedFrames : 30000;
for (var frame = 0; frame < bootFrameLimit; frame++)
{
    machine.ExecuteFrame();
    if (frame % 250 != 0) continue;
    var screen = Screen();
    if (Environment.GetEnvironmentVariable("COPPEROS_PROBE_BOOT") == "1" && frame % 2500 == 0)
    {
        var exec = Long(4); var current = Long(exec + 276); var name = Long(current + 10);
        var taskName = new StringBuilder();
        for (var i = 0; name != 0 && i < 80 && Read(name + (uint)i) != 0; i++) taskName.Append((char)Read(name + (uint)i));
        var chip = machine.ChipRam.Span;
        byte[] signature = [0x60, 0, 0, 0x2a, 0x43, 0x53, 0x48, 0x46, 0, 0, 0, 1];
        var images = new StringBuilder();
        for (var from = 0; from < chip.Length;)
        {
            var relative = chip[from..].IndexOf(signature);
            if (relative < 0) break;
            var at = from + relative;
            var bytes = at >= 8 ? Long((uint)at - 8) : 0;
            if (bytes >= 52 && bytes <= chip.Length - at + 8 && Long((uint)at - 4) == 0)
            {
                var last = at + (int)bytes - 9;
                while (last > at && chip[last] == 0) last--;
                images.Append($" {at:X}:{bytes}:{last - at}");
            }
            from = at + signature.Length;
        }
        Console.WriteLine($"BOOT frame={frame} pc={machine.Cpu.ProgramCounter:X8} exec={exec:X8} current={current:X8} name={taskName} shell={FindShell():X8} control={Long(0x880):X8} drive={machine.DriveState} images={images}\n{screen}");
    }
    var banner = args[4].StartsWith("nonresident-", StringComparison.Ordinal) ? "COPPEROS NONRESIDENT SHELL" : "COPPEROS SHELL REGISTERED";
    if (screen.Contains(banner) && screen.Contains('>')) { booted = true; break; }
}
if (!booted)
{
    File.WriteAllBytes(args[3] + ".boot-failed.adf", machine.ExportAdf());
    throw new InvalidOperationException($"Shell did not reach its boot prompt; task={FindShell():X8}\n{Screen()}");
}
var shell = FindShell();
for (var n = 0; shell == 0 && n < 1000; n++) { Run(1); shell = FindShell(); }
if (shell == 0) throw new InvalidOperationException("Shell task not found");
var segmentList = Long(shell + 128) << 2;
Console.WriteLine($"SHELL task={shell:X8} seglist={segmentList:X8} words={Long(segmentList):X8},{Long(segmentList + 4):X8},{Long(segmentList + 8):X8},{Long(segmentList + 12):X8},{Long(segmentList + 16):X8}");
var binding = Long(shell + 220); var runner = Long(binding + 20); var originalConsole = Long(shell + 164);
if (args[4] is "recovery" or "nonresident-recovery" or "script-recovery" or "nonresident-script-recovery")
{
    var complete = false;
    try
    {
        if (args[4].EndsWith("script-recovery", StringComparison.Ordinal))
        {
            Type("execute s:badsyntax");
            Type("echo >sys:recovery-script survived");
            Type("execute s:badfailat");
            Type("if error");
            Type("echo >sys:recovery-if-error yes");
            Type("endif");
            Type("echo >sys:recovery-failat survived");
        }
        else
        {
            Type("echo >");
            Type("echo >sys:recovery-redir survived");
            Type("echo \"unterminated");
            Type("echo >sys:recovery-quote survived");
        }
        Type("endcli");
        Run(2500);
        complete = FindShell() == 0;
    }
    finally
    {
        File.WriteAllBytes(args[3] + ".result.adf", machine.ExportAdf());
        File.WriteAllText(args[3] + ".trace.json", JsonSerializer.Serialize(new
        {
            shell, binding, runner, complete, remaining = FindShell(),
            childRemaining = FindTask("CopperOS child Shell"),
            backgroundRemaining = FindTask("CopperOS background Shell"),
            bindingAfter = Long(binding), runnerAfter = Long(runner), screen = Screen(),
        }, new JsonSerializerOptions { WriteIndented = true }) + "\n");
    }
    if (!complete) throw new InvalidOperationException("Recovered Shell did not exit");
    return;
}
if (args[4] == "run-script" || args[4] == "nonresident-script")
{
    Type("execute s:regress");
    var complete = false;
    var preparationError = "";
    var preparation = Environment.GetEnvironmentVariable("COPPEROS_PROBE_BEFORE_SCRIPT_WAIT");
    if (!string.IsNullOrEmpty(preparation))
    {
        try
        {
            foreach (var line in preparation.Split('\n', StringSplitOptions.RemoveEmptyEntries)) Type(line);
        }
        catch (Exception error) { preparationError = error.ToString(); }
    }
    var configuredLimit = Environment.GetEnvironmentVariable("COPPEROS_PROBE_SCRIPT_FRAMES");
    var scriptFrames = int.TryParse(configuredLimit, out var requestedScriptFrames) &&
        requestedScriptFrames > 0 && requestedScriptFrames <= 150000 ? requestedScriptFrames : 150000;
    for (var n = 0; n < scriptFrames && preparationError.Length == 0; n += 20)
    {
        Run(20);
        if (n % 10000 == 0)
        {
            Console.WriteLine($"SCRIPT frame={n}");
            if (Environment.GetEnvironmentVariable("COPPEROS_PROBE_SCREEN") == "1")
            {
                Console.WriteLine(Screen());
                try
                {
                    File.WriteAllBytes(args[3] + $".frame-{n}.adf", machine.ExportAdf());
                }
                catch (InvalidOperationException error) when (error.Message.Contains(
                    "valid AmigaDOS sectors; it cannot be saved as a standard ADF.", StringComparison.Ordinal))
                {
                    // An intermediate track update need not be representable
                    // as standard ADF. Preserve the observation and continue;
                    // the final, quiescent export below remains strict.
                    File.WriteAllText(args[3] + $".frame-{n}.snapshot-unavailable.json",
                        JsonSerializer.Serialize(new { frame = n, cycle = machine.Cycle,
                            pc = machine.Cpu.ProgramCounter, drive = machine.DriveState,
                            error = error.Message }, new JsonSerializerOptions { WriteIndented = true }) + "\n");
                    Console.WriteLine($"SNAPSHOT unavailable: {error.Message}; drive={machine.DriveState}");
                }
            }
            if (Environment.GetEnvironmentVariable("COPPEROS_PROBE_RAM") == "1")
            {
                Console.WriteLine($"SCRIPT pc={machine.Cpu.ProgramCounter:X8}");
                File.WriteAllBytes(args[3] + $".frame-{n}.chip", machine.ChipRam.ToArray());
            }
        }
        if (Screen().Split('\n').Any(line => line.Trim() == "SCRIPT DONE"))
        { complete = true; break; }
    }
    var scriptScreen = Screen();
    var followupError = preparationError;
    var followup = Environment.GetEnvironmentVariable("COPPEROS_PROBE_AFTER_SCRIPT");
    if (complete && !string.IsNullOrEmpty(followup))
    {
        try
        {
            foreach (var line in followup.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                Type(line);
        }
        catch (Exception error) { complete = false; followupError = error.ToString(); }
    }
    if (complete)
    {
        // The commands are asynchronous; let their workers retire before
        // testing the interactive Shell's own EndCLI and exporting the disk.
        Console.WriteLine("SCRIPT complete; allowing workers to retire");
        Run(10000);
        Console.WriteLine("SCRIPT checking child processes");
        for (var n = 0; n < 30000 && FindTask("CopperOS child Shell") != 0; n += 20)
            Run(20);
        for (var n = 0; n < 30000 && FindTask("CopperOS background Shell") != 0; n += 20)
            Run(20);
        ReturnKey();
        Type("endcli");
        Run(2500);
        Console.WriteLine("SCRIPT exit check complete");
    }
    if (!complete)
    {
        // Preserve the stopped guest before disk export, which can itself
        // reject an in-progress track. This is observation only: no guest
        // memory, scheduling or timeout is changed to make the test pass.
        var exec = Long(4);
        File.WriteAllBytes(args[3] + ".incomplete.chip", machine.ChipRam.ToArray());
        File.WriteAllText(args[3] + ".incomplete.json", JsonSerializer.Serialize(new
        {
            cycle = machine.Cycle, pc = machine.Cpu.ProgramCounter, exec,
            current = Long(exec + 276), ready = Long(exec + 406), waiting = Long(exec + 420),
            drive = machine.DriveState, screen = Screen(), followupError,
        }, new JsonSerializerOptions { WriteIndented = true }) + "\n");
    }
    File.WriteAllBytes(args[3] + ".result.adf", machine.ExportAdf());
    File.WriteAllText(args[3] + ".trace.json", JsonSerializer.Serialize(new
    {
        shell, binding, runner, complete, followupError, scriptScreen, remaining = FindShell(),
        childRemaining = FindTask("CopperOS child Shell"),
        backgroundRemaining = FindTask("CopperOS background Shell"),
        // Freed memory can be reused, so these values are diagnostic only.
        bindingAfter = Long(binding), runnerAfter = Long(runner), screen = Screen(),
    }, new JsonSerializerOptions { WriteIndented = true }) + "\n");
    if (!complete) throw new InvalidOperationException("Run script did not finish");
    return;
}
Type(args[4] == "control" ? "run <nil: c:delayprobe >sys:delayresult" : "run c:delayprobe >sys:delayresult");
byte[] sentinel = { 0x55, 0xc0, 0xff, 0xee, 0x0b, 0xad, 0xbe, 0xef, 0x12, 0xca, 0xfe, 0x34, 0x5e, 0xed, 0xfa, 0xce };
var seen = false;
for (var n = 0; n < 60000; n += 20)
{
    Run(20);
    if (machine.ChipRam.Span.IndexOf(sentinel) < 0) continue;
    seen = true;
    if (Long(shell + 164) != originalConsole) throw new InvalidOperationException("Shell console task was not restored");
    break;
}
if (!seen)
{
    File.WriteAllBytes(args[3] + ".timeout.adf", machine.ExportAdf());
    File.WriteAllText(args[3] + ".timeout.txt", Screen());
    throw new InvalidOperationException("Child never published its in-command marker");
}
var worker = FindTask("CopperOS background Shell");
for (var n = 0; worker == 0 && n < 1000; n++) { Run(1); worker = FindTask("CopperOS background Shell"); }
if (worker == 0) throw new InvalidOperationException("Background worker was not found at the overlap marker");
var shellInput = Long(shell + 156); var workerInput = Long(worker + 156);
var shellHandler = shellInput == 0 ? 0 : Long((shellInput << 2) + 8);
var workerHandler = workerInput == 0 ? 0 : Long((workerInput << 2) + 8);
if (args[4] == "race" && (shellHandler == 0 || shellHandler != workerHandler))
    throw new InvalidOperationException($"Worker input is not attached to the Shell console: shell={shellHandler:X8} worker={workerHandler:X8}");
Type("echo >sys:afterrun done");
Run(10000);
ReturnKey();
Type("endcli"); Run(2500);
var remaining = FindShell();
File.WriteAllBytes(args[3] + ".result.adf", machine.ExportAdf());
File.WriteAllText(args[3] + ".trace.json", JsonSerializer.Serialize(new
{
    shell, binding, runner, remaining,
    bindingAfter = Long(binding), runnerAfter = Long(runner),
    originalConsole, worker, shellInput, workerInput, shellHandler, workerHandler, screen = Screen(),
}, new JsonSerializerOptions { WriteIndented = true }) + "\n");
