using CopperMod.Amiga.Lightweight;

if (args.Length is not (3 or 4 or 5))
    throw new ArgumentException("Expected <rom> <system-adf> <result-adf> [helper-adf or empty] [extension-rom].");
if (File.Exists(args[2])) throw new IOException("Choose a new result image path.");
var hasHelper = args.Length >= 4 && args[3].Length != 0;

// Run the original disk bytes with no replacement startup sequence or helper
// volume. Changes made by the guest are exported separately from the input.
using var machine = new LightweightA500Machine(new LightweightA500Configuration
{
    AgnusModel = LightweightAgnusModel.Mos8375Pal2M,
    ChipRamBytes = 2 * 1024 * 1024,
    SlowRamBytes = 0,
    FloppyDriveCount = hasHelper ? 2 : 1,
});
machine.LoadKickstart(File.ReadAllBytes(args[0]));
if (args.Length == 5) machine.LoadExtensionRom(File.ReadAllBytes(args[4]));
machine.MountAdf(File.ReadAllBytes(args[1]));
machine.SetDriveWriteProtected(0, false);
if (hasHelper)
{
    machine.MountAdf(1, File.ReadAllBytes(args[3]));
    machine.SetDriveWriteProtected(1, true);
}

byte Read(uint address) => address < machine.ChipRam.Length
    ? machine.ChipRam.Span[(int)address] : (byte)0;
uint Long(uint address) => ((uint)Read(address) << 24) |
    ((uint)Read(address + 1) << 16) | ((uint)Read(address + 2) << 8) | Read(address + 3);

uint FindShell()
{
    var exec = Long(4);
    if (exec == 0) return 0;
    // Exec's public ready and waiting task lists; task names are public Nodes.
    foreach (var offset in new uint[] { 406, 420 })
    {
        var list = exec + offset;
        var task = Long(list);
        for (var guard = 0; guard < 256 && task != 0 && task != list + 4;
             guard++, task = Long(task))
        {
            var name = Long(task + 10);
            const string expected = "CopperOS Shell";
            var matches = Read(task + 8) == 13 && name != 0;
            for (var i = 0; matches && i < expected.Length; i++)
                matches = Read(name + (uint)i) == expected[i];
            if (matches && Read(name + (uint)expected.Length) == 0) return task;
        }
    }
    return 0;
}

uint shell = 0;
for (var frame = 0; frame < 30000; frame++)
{
    machine.ExecuteFrame();
    if (frame % 500 != 0) continue;
    shell = FindShell();
    Console.WriteLine($"BOOT frame={frame} shell={shell:X8}");
    if (shell != 0) break;
}
if (shell == 0) throw new InvalidOperationException("CopperOS Shell task did not start.");

void Run(int frames)
{
    for (var i = 0; i < frames; i++) machine.ExecuteFrame();
}
Run(500);
var keys = new Dictionary<char, byte>
{
    ['e'] = 0x12, ['c'] = 0x33, ['h'] = 0x25, ['o'] = 0x18,
    ['s'] = 0x21, ['y'] = 0x15, ['t'] = 0x14, ['x'] = 0x32,
    ['a'] = 0x20, ['k'] = 0x27, [' '] = 0x40, [':'] = 0x29,
    ['>'] = 0x39, ['/'] = 0x3a, ['d'] = 0x22, ['f'] = 0x23,
    ['1'] = 0x01, ['p'] = 0x19, ['r'] = 0x13, ['b'] = 0x35,
};
void Type(string text)
{
    foreach (var character in text)
    {
        var shifted = character is ':' or '>';
        if (shifted) { machine.SetKeyState(0x60, true); Run(4); }
        var key = keys[character];
        machine.SetKeyState(key, true); Run(4);
        machine.SetKeyState(key, false); Run(4);
        if (shifted) { machine.SetKeyState(0x60, false); Run(4); }
    }
    machine.SetKeyState(0x44, true); Run(4);
    machine.SetKeyState(0x44, false);
}
var diagnosticDirectory = Environment.GetEnvironmentVariable("COPPEROS_BOOT_PROBE_DIAGNOSTICS");
var commandFrames = int.TryParse(Environment.GetEnvironmentVariable("COPPEROS_BOOT_PROBE_COMMAND_FRAMES"), out var requestedFrames) ? requestedFrames : 12000;
if (commandFrames < 100 || commandFrames > 12000) throw new ArgumentOutOfRangeException("commandFrames");
void WaitForCommand(string label)
{
    for (var frame = 0; frame < commandFrames; frame += 100)
    {
        Run(100);
        if (frame % 1000 == 0)
        {
            Console.WriteLine($"{label} frame={frame} shell={FindShell():X8} pc={machine.Cpu.ProgramCounter:X8} d2={machine.Cpu.D[2]:X8}");
            if (!string.IsNullOrEmpty(diagnosticDirectory))
            {
                Directory.CreateDirectory(diagnosticDirectory);
                File.WriteAllBytes(Path.Combine(diagnosticDirectory, $"{label}-{frame}.ram"), machine.ChipRam.ToArray());
            }
        }
    }
}
Type("echo >sys:t/exact ok");
WaitForCommand("COMMAND");
if (hasHelper)
{
    Type("df1:p");
    WaitForCommand("MATHTRANS");
}
File.WriteAllBytes(args[2], machine.ExportAdf());
Console.WriteLine($"DONE shell={FindShell():X8}");
