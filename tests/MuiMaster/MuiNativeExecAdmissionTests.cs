/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiNativeExecAdmissionTests
{
	private static readonly APTR Address = APTR.FromPointer(0x1000);

	[Fact]
	public void NamedAdmissionSnapshotUsesTheSdkExecBaseFieldLayout()
	{
		Assert.Equal(6, Unsafe.SizeOf<MuiNativeExecAdmissionRecord>());
		Assert.Equal(6u, MuiNativeExecAdmissionRecord.Size);
		Assert.Equal(2, typeof(MuiNativeExecAdmissionRecord).StructLayoutAttribute!.Pack);
		Assert.Equal(ExecLayout.ExecBase.ThisTask,
			Marshal.OffsetOf<ExecBase>(nameof(ExecBase.ThisTask)).ToInt32());
		Assert.Equal(ExecLayout.ExecBase.IDNestCount,
			Marshal.OffsetOf<ExecBase>(nameof(ExecBase.IDNestCount)).ToInt32());
		Assert.Equal(ExecLayout.ExecBase.TaskDisableNestCount,
			Marshal.OffsetOf<ExecBase>(nameof(ExecBase.TaskDisableNestCount)).ToInt32());
	}

	[Theory]
	[InlineData(-1, -1)]
	[InlineData(-128, -2)]
	[InlineData(0, 0)]
	[InlineData(127, 1)]
	public void CodecReturnsNamedSignedNestingAndTaskWithoutChangingGuestState(int interrupts, int tasks)
	{
		var memory = new MuiHeadlessTestPlatform(Address.Raw, (int)ExecBase.Size + 4, 0, Address);
		memory.WriteUInt32(Address, ExecLayout.ExecBase.ThisTask, 0x12345678);
		memory.WriteUInt8(Address, ExecLayout.ExecBase.IDNestCount, unchecked((byte)interrupts));
		memory.WriteUInt8(Address, ExecLayout.ExecBase.TaskDisableNestCount, unchecked((byte)tasks));
		memory.WriteUInt32(Address, (int)ExecBase.Size, 0xAABBCCDD);
		Assert.True(MuiNativeExecAdmissionCodec.TryRead(ref memory, Address, out var state));
		Assert.Equal(APTR.FromPointer(0x12345678), state.ThisTask);
		Assert.Equal((sbyte)interrupts, state.InterruptDisableNesting);
		Assert.Equal((sbyte)tasks, state.TaskDisableNesting);
		Assert.Equal(0x12345678u, memory.ReadUInt32(Address, ExecLayout.ExecBase.ThisTask));
		Assert.Equal(unchecked((byte)interrupts), memory.ReadUInt8(Address, ExecLayout.ExecBase.IDNestCount));
		Assert.Equal(unchecked((byte)tasks), memory.ReadUInt8(Address, ExecLayout.ExecBase.TaskDisableNestCount));
		Assert.Equal(0xAABBCCDDu, memory.ReadUInt32(Address, (int)ExecBase.Size));
	}

	[Theory]
	[InlineData(0)]
	[InlineData(1)]
	[InlineData(296)]
	[InlineData((int)ExecBase.Size - 1)]
	public void TruncatedExecBaseIsNotAdmittedEvenIfTheRequestedFieldsFit(int mappedBytes)
	{
		var memory = new MuiHeadlessTestPlatform(Address.Raw, mappedBytes, 0, Address);
		Assert.False(MuiNativeExecAdmissionCodec.TryRead(ref memory, Address, out var state));
		Assert.Equal(default(MuiNativeExecAdmissionRecord), state);
	}

	[Theory]
	[InlineData(0u)]
	[InlineData(uint.MaxValue - 1)]
	public void NullAndWrappingExecBasesAreRejected(uint address)
	{
		var memory = new MuiHeadlessTestPlatform(Address.Raw, (int)ExecBase.Size, 0, Address);
		Assert.False(MuiNativeExecAdmissionCodec.TryRead(ref memory, APTR.FromPointer(address), out var state));
		Assert.Equal(default(MuiNativeExecAdmissionRecord), state);
	}
}
