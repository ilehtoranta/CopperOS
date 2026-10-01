using Amiga;
using CopperOS.Shell.Dos;

namespace CopperOS.Commands.Tests;

public sealed class DosShellNativeBootstrapCodecTests
{
	[Theory]
	[InlineData(0x400u)]
	[InlineData(0x402u)]
	[InlineData(4096u - ExecBase.Size)]
	public void RecoversCanonicalExecBaseWithoutAnyRegisterInput(uint address)
	{
		var memory = new TestMemory(4096);
		var expected = APTR.FromPointer(address);
		memory.WriteUInt32(APTR.FromPointer(
			DosShellNativeBootstrapCodec.ExecBasePointerAddress), 0, expected.Raw);

		Assert.True(DosShellNativeBootstrapCodec.TryReadExecBase(ref memory,
			out var execBase));
		Assert.Equal(expected, execBase);
		Assert.Equal(1, memory.LongReads);
	}

	[Theory]
	[InlineData(0u)]
	[InlineData(0x401u)]
	[InlineData(4096u - ExecBase.Size + 2u)]
	[InlineData(4096u)]
	[InlineData(uint.MaxValue - 1u)]
	public void RejectsMissingOddUnmappedOrOverflowingExecBase(uint candidate)
	{
		var memory = new TestMemory(4096);
		memory.WriteUInt32(APTR.FromPointer(
			DosShellNativeBootstrapCodec.ExecBasePointerAddress), 0, candidate);

		Assert.False(DosShellNativeBootstrapCodec.TryReadExecBase(ref memory,
			out var execBase));
		Assert.True(execBase.IsNull);
		Assert.Equal(1, memory.LongReads);
	}

	[Fact]
	public void RejectsUnavailablePointerSlotBeforeReadingIt()
	{
		var memory = new TestMemory(7);

		Assert.False(DosShellNativeBootstrapCodec.TryReadExecBase(ref memory,
			out var execBase));
		Assert.True(execBase.IsNull);
		Assert.Equal(0, memory.LongReads);
	}

	private struct TestMemory(int size) : IAmigaGuestMemory
	{
		public readonly byte[] Bytes = new byte[size];
		public int LongReads;
		public bool IsMapped(APTR address, uint byteSize) =>
			address.Raw <= Bytes.Length && byteSize <= (uint)Bytes.Length - address.Raw;
		public byte ReadUInt8(APTR address, int offset = 0) =>
			Bytes[checked((int)address.Raw + offset)];
		public ushort ReadUInt16(APTR address, int offset = 0) =>
			(ushort)((ReadUInt8(address, offset) << 8) | ReadUInt8(address, offset + 1));
		public uint ReadUInt32(APTR address, int offset = 0)
		{
			LongReads++;
			return ((uint)ReadUInt16(address, offset) << 16) | ReadUInt16(address, offset + 2);
		}
		public void WriteUInt8(APTR address, int offset, byte value) =>
			Bytes[checked((int)address.Raw + offset)] = value;
		public void WriteUInt16(APTR address, int offset, ushort value)
		{
			WriteUInt8(address, offset, (byte)(value >> 8));
			WriteUInt8(address, offset + 1, (byte)value);
		}
		public void WriteUInt32(APTR address, int offset, uint value)
		{
			WriteUInt16(address, offset, (ushort)(value >> 16));
			WriteUInt16(address, offset + 2, (ushort)value);
		}
		public void Clear(APTR address, uint byteCount) =>
			Array.Clear(Bytes, checked((int)address.Raw), checked((int)byteCount));
		public void Copy(APTR source, APTR destination, uint byteCount) =>
			Array.Copy(Bytes, checked((int)source.Raw), Bytes,
				checked((int)destination.Raw), checked((int)byteCount));
	}
}
