/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.CompilerServices;
using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiFamilyMessageStructCodecTests
{
	[Fact]
	public void FamilyPacketsRoundTripThroughSequentialRecords()
	{
		Assert.Equal(4, Unsafe.SizeOf<MuiFamilyDoChildMethodsMessage>());
		Assert.Equal(12, Unsafe.SizeOf<MuiFamilyGetChildMessage>());
		Assert.Equal(4, Unsafe.SizeOf<MuiFamilyMethodMessage>());
		Assert.Equal(8, Unsafe.SizeOf<MuiFamilyChildMessage>());
		Assert.Equal(12, Unsafe.SizeOf<MuiFamilyInsertMessage>());
		Assert.Equal(8, Unsafe.SizeOf<MuiFamilyTransferMessage>());
		Assert.Equal(8, Unsafe.SizeOf<MuiFamilyReorderMessage>());
		Assert.Equal(4, Unsafe.SizeOf<MuiFamilySortMessage>());
		Assert.Equal(8, Unsafe.SizeOf<MuiFamilyMutationListRecord>());
		Assert.Equal(4, Unsafe.SizeOf<MuiFamilyMutationVectorEntry>());

		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x2400);
		var objectAddress = APTR.FromPointer(0x3000);
		var predecessor = APTR.FromPointer(0x3100);

		Assert.True(MuiFamilyDoChildMethodsMessageStructCodec.TryWrite(ref platform,
			address, MuiFamilyDoChildMethodsMessageCodec.Method));
		Assert.True(MuiFamilyDoChildMethodsMessageStructCodec.TryRead(ref platform,
			address, out var doChild));
		Assert.Equal(MuiFamilyDoChildMethodsMessageCodec.Method, doChild.MethodId);

		Assert.True(MuiFamilyGetChildMessageStructCodec.TryWrite(ref platform,
			address, MuiFamilyGetChildMessageCodec.Method, -2, objectAddress));
		Assert.True(MuiFamilyGetChildMessageStructCodec.TryRead(ref platform,
			address, out var getChild));
		Assert.Equal(MuiFamilyGetChildMessageCodec.Method, getChild.MethodId);
		Assert.Equal(-2, getChild.Number);
		Assert.Equal(objectAddress, getChild.Reference);

		Assert.True(MuiFamilyMutationMessageStructCodec.TryWriteMethod(ref platform,
			address, MuiFamilyMutationCore.AddHeadMethod));
		Assert.True(MuiFamilyMutationMessageStructCodec.TryReadMethod(ref platform,
			address, out var method));
		Assert.Equal(MuiFamilyMutationCore.AddHeadMethod, method.MethodId);

		Assert.True(MuiFamilyMutationMessageStructCodec.TryWriteChild(ref platform,
			address, MuiFamilyMutationCore.AddTailMethod, objectAddress));
		Assert.True(MuiFamilyMutationMessageStructCodec.TryReadChild(ref platform,
			address, out var child));
		Assert.Equal(MuiFamilyMutationCore.AddTailMethod, child.MethodId);
		Assert.Equal(objectAddress, child.Object);

		Assert.True(MuiFamilyMutationMessageStructCodec.TryWriteInsert(ref platform,
			address, MuiFamilyMutationCore.InsertMethod, objectAddress, predecessor));
		Assert.True(MuiFamilyMutationMessageStructCodec.TryReadInsert(ref platform,
			address, out var insert));
		Assert.Equal(MuiFamilyMutationCore.InsertMethod, insert.MethodId);
		Assert.Equal(objectAddress, insert.Object);
		Assert.Equal(predecessor, insert.Predecessor);

		Assert.True(MuiFamilyMutationMessageStructCodec.TryWriteTransfer(ref platform,
			address, MuiFamilyMutationCore.TransferMethod, objectAddress));
		Assert.True(MuiFamilyMutationMessageStructCodec.TryReadTransfer(ref platform,
			address, out var transfer));
		Assert.Equal(MuiFamilyMutationCore.TransferMethod, transfer.MethodId);
		Assert.Equal(objectAddress, transfer.Family);

		Assert.True(MuiFamilyMutationMessageStructCodec.TryWriteReorder(ref platform,
			address, MuiFamilyMutationCore.ReorderMethod, predecessor));
		Assert.True(MuiFamilyMutationMessageStructCodec.TryReadReorder(ref platform,
			address, out var reorder));
		Assert.Equal(MuiFamilyMutationCore.ReorderMethod, reorder.MethodId);
		Assert.Equal(predecessor, reorder.After);
		Assert.True(MuiFamilyMutationMessageStructCodec.TryWriteSort(ref platform,
			address, MuiFamilyMutationCore.SortMethod));
		Assert.True(MuiFamilyMutationMessageStructCodec.TryReadMethodIdValue(
			ref platform, address, out var sortMethod));
		Assert.Equal(MuiFamilyMutationCore.SortMethod, sortMethod);

		var list = default(MuiFamilyMutationListRecord);
		list.Head = objectAddress;
		list.Tail = predecessor;
		Assert.True(MuiFamilyMutationListStructCodec.TryWrite(ref platform, address,
			list));
		Assert.True(MuiFamilyMutationListStructCodec.TryRead(ref platform, address,
			out var listRead));
		Assert.Equal(objectAddress, listRead.Head);
		Assert.Equal(predecessor, listRead.Tail);

		var entry = default(MuiFamilyMutationVectorEntry);
		entry.Object = objectAddress;
		Assert.True(MuiFamilyMutationVectorEntryStructCodec.TryWrite(ref platform,
			address, entry));
		Assert.True(MuiFamilyMutationVectorEntryStructCodec.TryRead(ref platform,
			address, out var entryRead));
		Assert.Equal(objectAddress, entryRead.Object);
	}

	[Fact]
	public void FamilyPacketsRejectIncompleteSequentialRecords()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var childEnd = APTR.FromPointer(0x20FFC);
		var insertEnd = APTR.FromPointer(0x20FF8);
		Assert.False(MuiFamilyDoChildMethodsMessageStructCodec.TryRead(ref platform,
			APTR.Null, out _));
		Assert.False(MuiFamilyGetChildMessageStructCodec.TryRead(ref platform,
			insertEnd, out _));
		Assert.False(MuiFamilyMutationMessageStructCodec.TryWriteChild(ref platform,
			childEnd, MuiFamilyMutationCore.AddHeadMethod, APTR.FromPointer(0x3000)));
		Assert.False(MuiFamilyMutationMessageStructCodec.TryReadInsert(ref platform,
			insertEnd, out _));
		Assert.False(MuiFamilyMutationListStructCodec.TryRead(ref platform,
			childEnd, out _));
		Assert.False(MuiFamilyMutationVectorEntryStructCodec.TryRead(ref platform,
			APTR.FromPointer(0x20FFD), out _));
	}

	[Fact]
	public void FamilyMutationVectorCursorExchangesNamedObjectValues()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var cursor = new MuiFamilyMutationVectorCursor
		{
			Base = APTR.FromPointer(0x5000),
			Index = 2,
		};
		var value = APTR.FromPointer(0xFEDCBA98u);
		Assert.True(MuiFamilyMutationVectorCodec.TryWriteObject(ref platform,
			cursor, value));
		Assert.True(MuiFamilyMutationVectorCodec.TryReadObjectValue(ref platform,
			cursor, out var raw));
		Assert.Equal(value.Raw, raw);
		cursor.Index = MuiFamilyMutationVectorCursor.MaximumEntries;
		Assert.False(MuiFamilyMutationVectorCodec.TryReadObjectValue(ref platform,
			cursor, out _));
		Assert.False(MuiFamilyMutationVectorCodec.TryWriteObject(ref platform,
			cursor, value));
	}
}
