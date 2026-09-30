/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiDataspaceMessageStructAdapterTests
{
	[Fact]
	public void DataspaceFieldAdapterUsesNamedRecordsForEveryPacket()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var packet = APTR.FromPointer(0x2400);
		Assert.True(MuiDataspaceMessageStructCodec.TryWriteAdd(ref platform,
			packet, new MuiDataspaceAddMessage
			{
				Data = APTR.FromPointer(0x3000), Length = -4, Id = 7,
			}));
		Assert.True(MuiDataspaceMessageMemoryCodec.TryWriteUInt32(ref platform,
			packet, MuiDataspacePacketKind.Add, MuiDataspaceField.Data, 0x3100));
		Assert.True(MuiDataspaceMessageMemoryCodec.TryWriteUInt32(ref platform,
			packet, MuiDataspacePacketKind.Add, MuiDataspaceField.Length,
			unchecked((uint)-12)));
		Assert.True(MuiDataspaceMessageMemoryCodec.TryWriteUInt32(ref platform,
			packet, MuiDataspacePacketKind.Add, MuiDataspaceField.Id, 19));
		Assert.True(MuiDataspaceMessageMemoryCodec.TryReadUInt32(ref platform,
			packet, MuiDataspacePacketKind.Add, MuiDataspaceField.Data,
			out var data));
		Assert.Equal(0x3100u, data);
		Assert.True(MuiDataspaceMessageMemoryCodec.TryReadUInt32(ref platform,
			packet, MuiDataspacePacketKind.Add, MuiDataspaceField.Length,
			out var length));
		Assert.Equal(unchecked((uint)-12), length);
		Assert.True(MuiDataspaceMessageMemoryCodec.TryReadUInt32(ref platform,
			packet, MuiDataspacePacketKind.Add, MuiDataspaceField.Id,
			out var id));
		Assert.Equal(19u, id);

		Assert.True(MuiDataspaceMessageStructCodec.TryWriteGet(ref platform,
			packet, new MuiDataspaceGetMessage
			{
				Id = 19, SizeStorage = APTR.FromPointer(0x3200),
			}));
		Assert.True(MuiDataspaceMessageMemoryCodec.TryWriteUInt32(ref platform,
			packet, MuiDataspacePacketKind.Get, MuiDataspaceField.SizeStorage,
			0x3300));
		Assert.True(MuiDataspaceMessageMemoryCodec.TryReadUInt32(ref platform,
			packet, MuiDataspacePacketKind.Get, MuiDataspaceField.SizeStorage,
			out var sizeStorage));
		Assert.Equal(0x3300u, sizeStorage);

		Assert.True(MuiDataspaceMessageStructCodec.TryWriteMerge(ref platform,
			packet, new MuiDataspaceMergeMessage
			{
				Dataspace = APTR.FromPointer(0x3400),
			}));
		Assert.True(MuiDataspaceMessageMemoryCodec.TryWriteUInt32(ref platform,
			packet, MuiDataspacePacketKind.Merge, MuiDataspaceField.Dataspace,
			0x3500));
		Assert.True(MuiDataspaceMessageMemoryCodec.TryReadUInt32(ref platform,
			packet, MuiDataspacePacketKind.Merge, MuiDataspaceField.Dataspace,
			out var dataspace));
		Assert.Equal(0x3500u, dataspace);
	}

	[Fact]
	public void DataspaceFieldAdapterPreservesMethodFieldsAndRejectsBadRanges()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var packet = APTR.FromPointer(0x2400);
		Assert.True(MuiDataspaceMessageStructCodec.TryWriteFind(ref platform,
			packet, new MuiDataspaceFindMessage { Id = 9 }));
		const uint replacement = 0xF1234567u;
		Assert.True(MuiDataspaceMessageMemoryCodec.TryWriteUInt32(ref platform,
			packet, MuiDataspacePacketKind.Find, MuiDataspaceField.MethodId,
			replacement));
		Assert.True(MuiDataspaceMessageMemoryCodec.TryReadUInt32(ref platform,
			packet, MuiDataspacePacketKind.Find, MuiDataspaceField.MethodId,
			out var method));
		Assert.Equal(replacement, method);
		Assert.False(MuiDataspaceMessageCodec.TryReadFind(ref platform, packet,
			out _));
		Assert.False(MuiDataspaceMessageMemoryCodec.TryReadUInt32(ref platform,
			APTR.FromPointer(0x20FFC), MuiDataspacePacketKind.Get,
			MuiDataspaceField.SizeStorage, out _));
		Assert.False(MuiDataspaceMessageMemoryCodec.TryWriteUInt32(ref platform,
			APTR.Null, MuiDataspacePacketKind.Remove, MuiDataspaceField.Id, 1));
		Assert.False(MuiDataspaceMessageMemoryCodec.TryReadUInt32(ref platform,
			packet, MuiDataspacePacketKind.Clear, MuiDataspaceField.Id, out _));
		Assert.False(MuiDataspaceMessageMemoryCodec.TryReadUInt32(ref platform,
			packet, (MuiDataspacePacketKind)255, MuiDataspaceField.MethodId,
			out _));
	}
}
