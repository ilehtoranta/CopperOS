using System.Buffers.Binary;
using Copper68k;

namespace CopperOS.MuiMaster.NativeExecution;

internal static class Program
{
	// Large full-closure HUNKs can exceed the historical low load window and
	// overlap the fixture's guest data records. Keep code above the stack and
	// leave the 0x35F00-0x51000 typed fixture arena untouched.
	private const uint LoadAddress = 0x000A0000;
	private const uint ReturnAddress = 0x00020000;
	private const uint StackPointer = 0x00090000;

	public static int Main(string[] args)
	{
		var maximumInstructions = 20_000_000;
		if (args.Length is < 1 or > 2 ||
			(args.Length == 2 && (!int.TryParse(args[1], out maximumInstructions) ||
				maximumInstructions is < 1 or > 1_000_000_000)))
		{
			Console.Error.WriteLine("usage: NativeExecution <single-code-hunk> [maximum-instructions:1..1000000000]");
			return 2;
		}
		if (args.Length == 2)
			Console.Error.WriteLine($"diagnostic instruction budget={maximumInstructions} (default=20000000)");

		var image = File.ReadAllBytes(args[0]);
		var code = LoadSingleCodeHunk(image);
		var methodTrace = NativeMethodTrace.Create(args[0], LoadAddress, code.Length);
		var bus = new FlatBus(0x00200000);
		code.CopyTo(bus.Memory.AsSpan(checked((int)LoadAddress)));
		using var cpu = M68kCoreFactory.Default.Create(M68kCpuModel.M68000, bus);
		cpu.BeginSubroutine(LoadAddress, StackPointer, ReturnAddress);

		// Event-handler registration now reconciles named active/default state
		// on each bounded queue walk. Keep the native smoke-run guard generous
		// enough for that typed traversal while still bounding malformed code.
		// An explicit diagnostic budget changes only the host stopping condition,
		// not the guest artifact, success predicate, or default qualification limit.
		var executed = 0;
		var recentPc = new uint[16];
		var traceDispatcher = Environment.GetEnvironmentVariable("COPPEROS_TRACE_DISPATCHER") == "1";
		while (cpu.State.ProgramCounter != ReturnAddress &&
			executed < maximumInstructions && !cpu.State.Halted)
		{
			recentPc[executed & 15] = cpu.State.ProgramCounter;
			methodTrace?.Observe(cpu.State.ProgramCounter, cpu.State.A[7], cpu.State.D[0], cpu.State.A[0], bus.Memory);
			if (traceDispatcher && cpu.State.ProgramCounter >= LoadAddress + 0x8CE &&
				cpu.State.ProgramCounter <= LoadAddress + 0x900)
			{
				var pc = cpu.State.ProgramCounter;
				var opcode = BinaryPrimitives.ReadUInt16BigEndian(bus.Memory.AsSpan((int)pc, 2));
				var stackTop = BinaryPrimitives.ReadUInt32BigEndian(
					bus.Memory.AsSpan(checked((int)cpu.State.A[7]), 4));
				Console.Error.WriteLine($"trace PC=${pc:X8} op=${opcode:X4} D0=${cpu.State.D[0]:X8} D1=${cpu.State.D[1]:X8} A0=${cpu.State.A[0]:X8} A1=${cpu.State.A[1]:X8} A2=${cpu.State.A[2]:X8} SP=${cpu.State.A[7]:X8} [SP]=${stackTop:X8} SR=${cpu.State.StatusRegister:X4} cycles={cpu.State.Cycles}");
			}
			try
			{
				cpu.ExecuteInstruction();
			}
			catch (InvalidOperationException exception)
			{
				methodTrace?.Dump();
				Console.Error.WriteLine($"native closure fault: PC=${cpu.State.ProgramCounter:X8}, D0=${cpu.State.D[0]:X8}, D1=${cpu.State.D[1]:X8}, A0=${cpu.State.A[0]:X8}, A1=${cpu.State.A[1]:X8}");
				Console.Error.WriteLine(exception.Message);
				Console.Error.WriteLine("recent PCs=" + string.Join(",", recentPc.Select(pc => $"${pc:X8}")));
				Console.Error.WriteLine($"instructions={executed}, SP=${cpu.State.A[7]:X8}, load=${LoadAddress:X8}, codeBytes={code.Length}");
				return 5;
			}
			executed++;
		}

		if (cpu.State.ProgramCounter != ReturnAddress)
		{
			methodTrace?.Dump();
			Console.Error.WriteLine($"native closure did not return: PC=${cpu.State.ProgramCounter:X8}, D0={cpu.State.D[0]}, instructions={executed}, halted={cpu.State.Halted}");
			return 3;
		}
		if (cpu.State.D[0] != 42)
		{
			methodTrace?.Dump();
			Console.Error.WriteLine($"native closure returned {cpu.State.D[0]}, expected 42 after {executed} instructions; recent PCs=" +
				string.Join(",", recentPc));
			return 4;
		}

		Console.WriteLine($"PASS M68000 return=42 instructions={executed} cycles={cpu.State.Cycles}");
		return 0;
	}

	private static byte[] LoadSingleCodeHunk(byte[] image)
	{
		var offset = 0;
		uint ReadLong()
		{
			if (offset > image.Length - 4) throw new InvalidDataException("truncated HUNK");
			var value = BinaryPrimitives.ReadUInt32BigEndian(image.AsSpan(offset, 4));
			offset += 4;
			return value;
		}

		if (ReadLong() != 0x000003F3 || ReadLong() != 0 || ReadLong() != 1 ||
			ReadLong() != 0 || ReadLong() != 0)
			throw new InvalidDataException("expected one unnamed HUNK");
		_ = ReadLong();
		if (ReadLong() != 0x000003E9)
			throw new InvalidDataException("expected HUNK_CODE");
		var byteCount = checked((int)ReadLong() * 4);
		if (offset > image.Length - byteCount) throw new InvalidDataException("truncated code");
		var code = image.AsSpan(offset, byteCount).ToArray();
		offset += byteCount;
		var record = ReadLong();
		if (record == 0x000003EC)
		{
			// CopperSharp emits a single code hunk, so every HUNK_RELOC32
			// group must target hunk zero.  Relocations are internal absolute
			// addresses; add the address at which this hunk is loaded before
			// copying it to the bus.  This keeps the native harness faithful
			// for larger freestanding closures such as live MUIM_MultiSet.
			while (true)
			{
				var relocationCount = ReadLong();
				if (relocationCount == 0) break;
				if (ReadLong() != 0)
					throw new InvalidDataException("relocation targets an unexpected hunk");
				for (var index = 0u; index < relocationCount; index++)
				{
					var relocationOffset = ReadLong();
					if (relocationOffset > (uint)code.Length - 4)
						throw new InvalidDataException("relocation falls outside code hunk");
					var codeOffset = checked((int)relocationOffset);
					var value = BinaryPrimitives.ReadUInt32BigEndian(
						code.AsSpan(codeOffset, 4));
					BinaryPrimitives.WriteUInt32BigEndian(code.AsSpan(codeOffset, 4),
						checked(value + LoadAddress));
				}
			}
			record = ReadLong();
		}
		if (record == 0x000003F0)
		{
			while (true)
			{
				var nameLongs = ReadLong();
				if (nameLongs == 0) break;
				var nameBytes = checked((int)nameLongs * 4);
				if (offset > image.Length - nameBytes)
					throw new InvalidDataException("truncated symbol name");
				offset += nameBytes;
				_ = ReadLong();
			}
			record = ReadLong();
		}
		if (record != 0x000003F2 || offset != image.Length)
			throw new InvalidDataException("unexpected trailing HUNK records");
		return code;
	}
}

internal sealed class FlatBus : IM68kBus
{
	public FlatBus(int size) => Memory = new byte[size];
	public byte[] Memory { get; }

	public byte ReadByte(uint address, ref long cycle, M68kBusAccessKind accessKind) =>
		Memory[Offset(address, 1)];
	public ushort ReadWord(uint address, ref long cycle, M68kBusAccessKind accessKind)
	{
		var offset = Offset(address, 2);
		return (ushort)((Memory[offset] << 8) | Memory[offset + 1]);
	}
	public uint ReadLong(uint address, ref long cycle, M68kBusAccessKind accessKind)
	{
		var offset = Offset(address, 4);
		return ((uint)Memory[offset] << 24) | ((uint)Memory[offset + 1] << 16) |
			((uint)Memory[offset + 2] << 8) | Memory[offset + 3];
	}
	public void WriteByte(uint address, byte value, ref long cycle,
		M68kBusAccessKind accessKind) => Memory[Offset(address, 1)] = value;
	public void WriteWord(uint address, ushort value, ref long cycle,
		M68kBusAccessKind accessKind)
	{
		var offset = Offset(address, 2);
		Memory[offset] = (byte)(value >> 8);
		Memory[offset + 1] = (byte)value;
	}
	public void WriteLong(uint address, uint value, ref long cycle,
		M68kBusAccessKind accessKind)
	{
		var offset = Offset(address, 4);
		Memory[offset] = (byte)(value >> 24);
		Memory[offset + 1] = (byte)(value >> 16);
		Memory[offset + 2] = (byte)(value >> 8);
		Memory[offset + 3] = (byte)value;
	}
	public bool HasHostTrapStub(uint address) => false;
	public bool TryInvokeHostTrap(uint instructionProgramCounter, ushort trapId,
		M68kCpuState state) => false;
	public void ResetExternalDevices(long cycle) { }

	private int Offset(uint address, int count)
	{
		if (address > int.MaxValue || address > Memory.Length - count)
			throw new InvalidOperationException($"unmapped bus address ${address:X8}");
		return (int)address;
	}
}
