using System.Buffers.Binary;
using System.Globalization;
using System.Text.RegularExpressions;

namespace CopperOS.MuiMaster.NativeExecution;

// Optional host-only observer. Addresses come from the selected artifact's map,
// never from another build's offsets. No guest instructions or memory are changed.
internal sealed class NativeMethodTrace
{
	private readonly Dictionary<uint, string> _entries = new();
	private readonly Stack<CallFrame> _calls = new();
	private readonly Queue<string> _recent = new();
	private readonly record struct CallFrame(string Name, uint ReturnAddress, uint StackPointer);

	internal static NativeMethodTrace? Create(string artifact, uint loadAddress, int codeBytes)
	{
		var selection = Environment.GetEnvironmentVariable("COPPEROS_TRACE_METHODS");
		if (string.IsNullOrWhiteSpace(selection)) return null;
		var filters = selection.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		var trace = new NativeMethodTrace();
		foreach (var line in File.ReadLines(artifact + ".map"))
		{
			var match = Regex.Match(line, @"^([0-9A-Fa-f]{8})\s+(\d+)\s+(\S.*)$");
			if (!match.Success) continue;
			var name = match.Groups[3].Value;
			if (!filters.Any(filter => name.Contains(filter, StringComparison.Ordinal))) continue;
			var offset = uint.Parse(match.Groups[1].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
			var size = uint.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
			if (offset >= codeBytes || size > codeBytes - offset) continue;
			trace._entries.TryAdd(checked(loadAddress + offset), name);
		}
		return trace;
	}

	internal void Observe(uint pc, uint stackPointer, uint d0, uint a0, ReadOnlySpan<byte> memory)
	{
		while (_calls.TryPeek(out var call) && pc == call.ReturnAddress &&
			stackPointer == call.StackPointer + sizeof(uint))
		{
			_calls.Pop();
			Remember($"return D0=${d0:X8} A0=${a0:X8} {call.Name}");
		}
		if (!_entries.TryGetValue(pc, out var name) || stackPointer > memory.Length - sizeof(uint)) return;
		var returnAddress = BinaryPrimitives.ReadUInt32BigEndian(memory.Slice((int)stackPointer, sizeof(uint)));
		_calls.Push(new CallFrame(name, returnAddress, stackPointer));
		var arguments = memory[(checked((int)stackPointer) + sizeof(uint))..];
		Remember($"enter {name} stack-bytes={Convert.ToHexString(arguments[..Math.Min(32, arguments.Length)])}");
	}

	private void Remember(string value)
	{
		if (_recent.Count == 128) _recent.Dequeue();
		_recent.Enqueue(value);
	}

	internal void Dump()
	{
		Console.Error.WriteLine("Recent map-selected native method calls:");
		foreach (var line in _recent) Console.Error.WriteLine(line);
	}
}
