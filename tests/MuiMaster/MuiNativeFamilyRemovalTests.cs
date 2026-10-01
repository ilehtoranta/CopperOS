/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.CompilerServices;
using Amiga;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiNativeFamilyRemovalTests
{
	private static readonly APTR MemoryBase = APTR.FromPointer(0x1000);
	private static readonly APTR MessageAddress = APTR.FromPointer(0x1200);
	private static readonly APTR FamilyListAddress = APTR.FromPointer(0x1300);

	[Fact]
	public void FamilyRemoveMessageUsesCompleteNamedRecord()
	{
		Assert.Equal((int)MuiNativeFamilyRemoveMessageRecord.Size,
			Unsafe.SizeOf<MuiNativeFamilyRemoveMessageRecord>());
		var memory = new MuiHeadlessTestPlatform(MemoryBase.Raw, 0x1000, 0,
			MemoryBase);
		var expected = new MuiNativeFamilyRemoveMessageRecord
		{
			MethodId = MuiFamilyMutationCore.RemoveMethod,
			Object = APTR.FromPointer(0x4567),
		};
		Assert.True(MuiNativeFamilyRemoveMessageCodec.Write(ref memory,
			MessageAddress, expected));
		Assert.True(MuiNativeFamilyRemoveMessageCodec.TryRead(ref memory,
			MessageAddress, out var actual));
		Assert.Equal(expected.MethodId, actual.MethodId);
		Assert.Equal(expected.Object, actual.Object);
		Assert.False(MuiNativeFamilyRemoveMessageCodec.TryRead(ref memory,
			APTR.FromPointer(0x1FFC), out _));
	}

	[Fact]
	public void FamilyListUsesMorphOsMinListShape()
	{
		Assert.Equal(12, Unsafe.SizeOf<MuiNativeFamilyListRecord>());
		var memory = new MuiHeadlessTestPlatform(MemoryBase.Raw, 0x1000, 0,
			MemoryBase);
		var expected = new MuiNativeFamilyListRecord
		{
			Head = APTR.FromPointer(0x1800),
			Tail = APTR.Null,
			TailPred = APTR.FromPointer(0x1900),
		};
		Assert.True(MuiNativeFamilyListCodec.Write(ref memory,
			FamilyListAddress, expected));
		Assert.True(MuiNativeFamilyListCodec.TryRead(ref memory,
			FamilyListAddress, out var actual));
		Assert.Equal(expected.Head, actual.Head);
		Assert.Equal(expected.Tail, actual.Tail);
		Assert.Equal(expected.TailPred, actual.TailPred);
		Assert.True(MuiNativeFamilyRemovalCore.TryReadListHead(ref memory,
			FamilyListAddress, MuiNativeGuiMode.FamilyList, out var head));
		Assert.Equal(expected.Head, head);
		Assert.False(MuiNativeFamilyListCodec.TryRead(ref memory,
			APTR.FromPointer(0x1FF5), out _));
	}

	[Fact]
	public void FamilyRemovalListReaderUsesGroupExecListShapeForGroupAttribute()
	{
		var memory = new MuiHeadlessTestPlatform(MemoryBase.Raw, 0x1000, 0,
			MemoryBase);
		var expected = new MuiGroupExecListRecord
		{
			Head = APTR.FromPointer(0x1A00),
			Tail = APTR.Null,
			TailPred = APTR.FromPointer(0x1A10),
			Type = NodeType.Unknown,
		};
		Assert.True(MuiGroupExecListCodec.Write(ref memory, FamilyListAddress,
			expected));
		Assert.True(MuiNativeFamilyRemovalCore.TryReadListHead(ref memory,
			FamilyListAddress, MuiNativeGuiMode.GroupChildList, out var head));
		Assert.Equal(expected.Head, head);
		Assert.False(MuiNativeFamilyRemovalCore.TryReadListHead(ref memory,
			FamilyListAddress, 0xFFFFu, out _));
	}

	[Theory]
	[InlineData(0x1300, 0x1300, 1, 0, true)]
	[InlineData(0x1300, 0x1300, 1, 1, false)]
	[InlineData(0x1300, 0x1400, 1, 0, false)]
	[InlineData(0x1300, 0x1300, 0, 0, false)]
	[InlineData(0, 0, 1, 0, false)]
	public void RemovalRequiresSameCollectionAndObservedPriorMembership(
		uint beforeList, uint afterList, uint beforeContainsChild,
		uint afterContainsChild, bool expected)
	{
		var before = new MuiNativeFamilyChildMembershipRecord
		{
			List = APTR.FromPointer(beforeList),
			ContainsChild = beforeContainsChild,
		};
		var after = new MuiNativeFamilyChildMembershipRecord
		{
			List = APTR.FromPointer(afterList),
			ContainsChild = afterContainsChild,
		};

		Assert.Equal(expected, MuiNativeFamilyRemovalCore.ConfirmsRemovalFromSameList(
			before, after));
	}
}
