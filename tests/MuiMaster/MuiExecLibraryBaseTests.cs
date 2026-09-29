/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.CompilerServices;
using Amiga;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiExecLibraryBaseTests
{
	private static readonly APTR Address = APTR.FromPointer(0x1000);

	private static MuiExecLibraryBaseRecord Sample()
	{
		var value = default(MuiExecLibraryBaseRecord);
		value.Header.Node.Successor = APTR.FromPointer(0x1200);
		value.Header.Node.Predecessor = APTR.FromPointer(0x1300);
		value.Header.Node.Type = (byte)NodeType.Library;
		value.Header.Node.Priority = -7;
		value.Header.Node.Name = STRPTR.FromPointer(0x1400);
		value.Header.Flags = LibraryFlags.Changed | LibraryFlags.DelayedExpunge;
		value.Header.Padding = 0x5A;
		value.Header.NegativeSize = MuiExecLibraryBaseRecord.NegativeBytes;
		value.Header.PositiveSize = MuiExecLibraryBaseRecord.PositiveBytes;
		value.Header.Version = 0;
		value.Header.Revision = 1;
		value.Header.IdString = APTR.FromPointer(0x1500);
		value.Header.Checksum = 0xFEDCBA98;
		value.Header.OpenCount = 3;
		value.ExecBase = APTR.FromPointer(0x10000);
		value.SegmentList = BPTR.FromRaw(0x81234567);
		value.PrivateRoot = APTR.FromPointer(0x1600);
		return value;
	}

	[Fact]
	public void PackedBaseRoundTripsSdkHeaderAndTypedPrivateFields()
	{
		Assert.Equal((int)Library.Size, Unsafe.SizeOf<Library>());
		Assert.Equal((int)MuiExecLibraryBaseRecord.Size,
			Unsafe.SizeOf<MuiExecLibraryBaseRecord>());
		Assert.Equal(46u, MuiExecLibraryBaseRecord.Size);
		Assert.Equal((ushort)48, MuiExecLibraryBaseRecord.PositiveBytes);
		var memory = new MuiHeadlessTestPlatform(Address.Raw, 48, 0, Address);
		memory.WriteUInt16(Address, (int)MuiExecLibraryBaseRecord.Size, 0xA55A);
		var expected = Sample();
		Assert.True(MuiExecLibraryBaseCodec.Write(ref memory, Address, expected));
		Assert.True(MuiExecLibraryBaseCodec.TryRead(ref memory, Address, out var actual));
		Assert.Equal(expected, actual);
		Assert.Equal(0xA55A, memory.ReadUInt16(Address, (int)MuiExecLibraryBaseRecord.Size));
	}

	[Fact]
	public void NamedHeaderMutationsPreserveListLinksAndPrivateOwnership()
	{
		var memory = new MuiHeadlessTestPlatform(Address.Raw, 48, 0, Address);
		var expected = Sample();
		Assert.True(MuiExecLibraryBaseCodec.Write(ref memory, Address, expected));
		expected.Header.OpenCount = 4;
		expected.Header.Flags &= ~LibraryFlags.DelayedExpunge;
		ExecLibraryCodec.WriteOpenCount(ref memory, Address, expected.Header.OpenCount);
		ExecLibraryCodec.WriteFlags(ref memory, Address, expected.Header.Flags);
		Assert.True(MuiExecLibraryBaseCodec.TryRead(ref memory, Address, out var actual));
		Assert.Equal(expected, actual);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(33)]
	[InlineData(34)]
	[InlineData(45)]
	public void TruncatedBaseIsRejectedBeforeAnyWrite(int mappedBytes)
	{
		var memory = new MuiHeadlessTestPlatform(Address.Raw, mappedBytes, 0, Address);
		for (var index = 0; index < mappedBytes; index++)
			memory.WriteUInt8(Address, index, 0xA5);
		Assert.False(MuiExecLibraryBaseCodec.Write(ref memory, Address, Sample()));
		Assert.False(MuiExecLibraryBaseCodec.TryRead(ref memory, Address, out var rejected));
		Assert.Equal(default(MuiExecLibraryBaseRecord), rejected);
		for (var index = 0; index < mappedBytes; index++)
			Assert.Equal(0xA5, memory.ReadUInt8(Address, index));
	}

	[Fact]
	public void BorrowedPositiveAreaClaimRejectsOutsideAndWrappingRanges()
	{
		var memory = default(MuiExecLibraryMemory);
		Assert.False(memory.IsMapped(Address, 1));
		memory.Base = Address;
		Assert.True(memory.IsMapped(Address, MuiExecLibraryBaseRecord.PositiveBytes));
		Assert.True(memory.IsMapped(APTR.FromPointer(Address.Raw + 47), 1));
		Assert.False(memory.IsMapped(Address, 0));
		Assert.False(memory.IsMapped(Address, 49));
		Assert.False(memory.IsMapped(APTR.FromPointer(Address.Raw - 1), 1));
		Assert.False(memory.IsMapped(APTR.FromPointer(Address.Raw + 48), 1));
		Assert.False(memory.IsMapped(APTR.FromPointer(uint.MaxValue), 2));
		memory.Base = APTR.FromPointer(uint.MaxValue - 47);
		Assert.False(memory.IsMapped(memory.Base, 48));
	}
}
