/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiObjectPersistenceStructAdapterTests
{
	[Fact]
	public void ObjectPersistenceFieldAdapterUsesNamedRecordAndPreservesMethod()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var packet = APTR.FromPointer(0x2400);
		Assert.True(MuiObjectPersistenceMessageStructCodec.TryWrite(ref platform,
			packet, MuiObjectPersistenceMessageCodec.ExportMethod,
			APTR.FromPointer(0x3000)));
		Assert.True(MuiObjectPersistenceMessageMemoryCodec.TryWriteUInt32(
			ref platform, packet, MuiObjectPersistencePacketField.Dataspace,
			0x3100));
		Assert.True(MuiObjectPersistenceMessageMemoryCodec.TryReadUInt32(
			ref platform, packet, MuiObjectPersistencePacketField.MethodId,
			out var method));
		Assert.Equal(MuiObjectPersistenceMessageCodec.ExportMethod, method);
		Assert.True(MuiObjectPersistenceMessageMemoryCodec.TryReadUInt32(
			ref platform, packet, MuiObjectPersistencePacketField.Dataspace,
			out var dataspace));
		Assert.Equal(0x3100u, dataspace);

		Assert.True(MuiObjectPersistenceMessageStructCodec.TryWrite(ref platform,
			packet, MuiObjectPersistenceMessageCodec.ImportMethod,
			APTR.FromPointer(0x3200)));
		Assert.True(MuiObjectPersistenceMessageMemoryCodec.TryWriteUInt32(
			ref platform, packet, MuiObjectPersistencePacketField.Dataspace,
			0x3300));
		Assert.True(MuiObjectPersistenceMessageMemoryCodec.TryReadUInt32(
			ref platform, packet, MuiObjectPersistencePacketField.MethodId,
			out method));
		Assert.Equal(MuiObjectPersistenceMessageCodec.ImportMethod, method);
		Assert.True(MuiObjectPersistenceMessageMemoryCodec.TryReadUInt32(
			ref platform, packet, MuiObjectPersistencePacketField.Dataspace,
			out dataspace));
		Assert.Equal(0x3300u, dataspace);
	}

	[Fact]
	public void ObjectPersistenceFieldAdapterRejectsBadRangesAndFields()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var packet = APTR.FromPointer(0x2400);
		Assert.True(MuiObjectPersistenceMessageStructCodec.TryWrite(ref platform,
			packet, MuiObjectPersistenceMessageCodec.ExportMethod,
			APTR.FromPointer(0x3000)));
		Assert.False(MuiObjectPersistenceMessageMemoryCodec.TryReadUInt32(
			ref platform, APTR.FromPointer(0x20FFC),
			MuiObjectPersistencePacketField.Dataspace, out _));
		Assert.False(MuiObjectPersistenceMessageMemoryCodec.TryWriteUInt32(
			ref platform, APTR.Null,
			MuiObjectPersistencePacketField.Dataspace, 1));
		Assert.False(MuiObjectPersistenceMessageMemoryCodec.TryReadUInt32(
			ref platform, packet, (MuiObjectPersistencePacketField)255, out _));
		Assert.False(MuiObjectPersistenceMessageMemoryCodec.TryWriteUInt32(
			ref platform, packet, (MuiObjectPersistencePacketField)255, 1));
		Assert.False(MuiObjectPersistenceMessageMemoryCodec.TryReadUInt32(
			ref platform, APTR.FromPointer(0x20FFE),
			MuiObjectPersistencePacketField.MethodId, out _));
	}
}
