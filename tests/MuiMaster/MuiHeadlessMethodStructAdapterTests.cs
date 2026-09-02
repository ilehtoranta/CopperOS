/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.CompilerServices;
using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiHeadlessMethodStructAdapterTests
{
	[Fact]
	public void HeadlessMethodHeaderUsesNamedRecordAndBounds()
	{
		var state = APTR.FromPointer(0x1000);
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			state);
		var address = APTR.FromPointer(0x2400);
		var value = new MuiHeadlessMethodMessage { MethodId = 0x80420001 };

		Assert.Equal(4, Unsafe.SizeOf<MuiHeadlessMethodMessage>());
		Assert.True(MuiHeadlessMethodMessageRecordCodec.Write(ref platform,
			address, value));
		Assert.True(MuiHeadlessMethodMessageRecordCodec.TryRead(ref platform,
			address, out var decoded));
		Assert.Equal(value.MethodId, decoded.MethodId);
		Assert.True(MuiHeadlessMessageCodec.TryReadMethodIdValue(ref platform,
			address, out var methodId));
		Assert.Equal(value.MethodId, methodId);

		Assert.True(MuiHeadlessMethodMessageRecordMemoryCodec.TryGetAddress(ref platform,
			address, MuiHeadlessMethodMessageField.MethodId, out var directFieldAddress));
		Assert.Equal(address.Raw + MuiHeadlessMethodMessage.MethodIdOffset,
			directFieldAddress.Raw);
		var cursor = default(MuiHeadlessMethodMessageFieldCursor);
		cursor.Record = address;
		cursor.Field = MuiHeadlessMethodMessageField.MethodId;
		Assert.True(MuiHeadlessMethodMessageFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out var fieldAddress));
		Assert.Equal(address.Raw + MuiHeadlessMethodMessage.MethodIdOffset,
			fieldAddress.Raw);
		cursor.Field = (MuiHeadlessMethodMessageField)255;
		Assert.False(MuiHeadlessMethodMessageFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out _));
		Assert.False(MuiHeadlessMethodMessageRecordCodec.TryRead(ref platform,
			APTR.FromPointer(0x20FFE), out _));
	}

	[Fact]
	public void HeadlessMethodHeaderUsesSharedUlongStorageBoundary()
	{
		var state = APTR.FromPointer(0x1000);
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			state);
		var address = APTR.FromPointer(0x2400);
		const uint methodId = 0xC2468ACEu;
		Assert.True(MuiHeadlessMethodHeaderCodec.WriteValue(ref platform,
			address, methodId));
		Assert.True(MuiHeadlessMethodHeaderCodec.TryReadValue(ref platform,
			address, out var readMethodId));
		Assert.Equal(methodId, readMethodId);
		Assert.False(MuiHeadlessMethodHeaderCodec.TryReadValue(ref platform,
			APTR.FromPointer(0x20FFF), out _));
		Assert.False(MuiHeadlessMethodHeaderCodec.WriteValue(ref platform,
			APTR.Null, methodId));
	}
}
