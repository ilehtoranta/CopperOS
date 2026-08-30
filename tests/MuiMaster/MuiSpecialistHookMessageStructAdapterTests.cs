/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.CompilerServices;
using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiSpecialistHookMessageStructAdapterTests
{
	[Fact]
	public void SpecialistHookMessageUsesCompleteNamedRecordAndBounds()
	{
		var state = APTR.FromPointer(0x1000);
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			state);
		var address = APTR.FromPointer(0x2400);
		var value = new MuiSpecialistHookMessage
		{
			MethodId = 0x80420001,
			Param1 = 0x00005100,
			Param2 = 0x0000CAFE,
			Reserved = 0x000055AA,
		};

		Assert.Equal(16, Unsafe.SizeOf<MuiSpecialistHookMessage>());
		Assert.True(MuiSpecialistHookMessageCodec.Write(ref platform, address,
			value));
		Assert.True(MuiSpecialistHookMessageCodec.TryRead(ref platform, address,
			out var decoded));
		Assert.Equal(value.MethodId, decoded.MethodId);
		Assert.Equal(value.Param1, decoded.Param1);
		Assert.Equal(value.Param2, decoded.Param2);
		Assert.Equal(value.Reserved, decoded.Reserved);

		Assert.True(MuiSpecialistHookMessageRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiSpecialistHookMessageField.Param1,
			out var fieldAddress));
		Assert.Equal(address.Raw + MuiSpecialistHookMessage.Param1Offset,
			fieldAddress.Raw);
		var cursor = default(MuiSpecialistHookMessageFieldCursor);
		cursor.Record = address;
		cursor.Field = MuiSpecialistHookMessageField.Param1;
		Assert.True(MuiSpecialistHookMessageFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out var compatibilityFieldAddress));
		Assert.Equal(fieldAddress.Raw, compatibilityFieldAddress.Raw);
		cursor.Field = (MuiSpecialistHookMessageField)255;
		Assert.False(MuiSpecialistHookMessageFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out _));
		Assert.False(MuiSpecialistHookMessageCodec.TryRead(ref platform,
			APTR.FromPointer(0x20FF5), out _));
	}
}
