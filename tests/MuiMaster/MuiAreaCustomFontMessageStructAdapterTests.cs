using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiAreaCustomFontMessageStructAdapterTests
{
	[Fact]
	public void CustomFontMessageRecordsUseNamedFieldsAndBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var openAddress = APTR.FromPointer(0x3000);
		var open = default(MuiAreaOpenCustomFontMessage);
		open.MethodId = MuiAreaCustomFontMessageCodec.OpenCustomFont;
		open.Spec = APTR.FromPointer(0x3400);
		Assert.True(MuiAreaCustomFontMessageCodec.WriteOpen(ref platform,
			openAddress, open.Spec));
		Assert.True(MuiAreaCustomFontMessageMemoryCodec.TryReadUInt32(ref platform,
			openAddress, MuiAreaCustomFontMessageKind.Open,
			MuiAreaCustomFontMessageField.MethodId, out var openMethod));
		Assert.Equal(open.MethodId, openMethod);
		Assert.True(MuiAreaCustomFontMessageMemoryCodec.TryGetAddress(ref platform,
			openAddress, MuiAreaCustomFontMessageKind.Open,
			MuiAreaCustomFontMessageField.Pointer, out var specAddress));
		Assert.Equal(APTR.FromPointer(0x3004), specAddress);
		var cursor = new MuiAreaCustomFontMessageFieldCursor
		{
			Message = openAddress,
			Kind = MuiAreaCustomFontMessageKind.Open,
			Field = MuiAreaCustomFontMessageField.Pointer,
		};
		Assert.True(MuiAreaCustomFontMessageFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out var typedSpecAddress, out var typedSpecSize));
		Assert.Equal(specAddress, typedSpecAddress);
		Assert.Equal(MuiAreaOpenCustomFontMessage.FieldSize, typedSpecSize);
		Assert.True(MuiAreaCustomFontMessageMemoryCodec.TryGetAddress(ref platform,
			cursor, out var memorySpecAddress, out var memorySpecSize));
		Assert.Equal(typedSpecAddress, memorySpecAddress);
		Assert.Equal(typedSpecSize, memorySpecSize);
		Assert.True(MuiAreaCustomFontMessageCodec.TryReadOpen(ref platform,
			openAddress, out var decodedOpen));
		Assert.Equal(open.Spec, decodedOpen.Spec);

		var closeAddress = APTR.FromPointer(0x3040);
		var close = default(MuiAreaCloseCustomFontMessage);
		close.MethodId = MuiAreaCustomFontMessageCodec.CloseCustomFont;
		close.Font = APTR.FromPointer(0x3500);
		Assert.True(MuiAreaCustomFontMessageCodec.WriteClose(ref platform,
			closeAddress, close.Font));
		Assert.True(MuiAreaCustomFontMessageCodec.TryReadClose(ref platform,
			closeAddress, out var decodedClose));
		Assert.Equal(close.Font, decodedClose.Font);
		Assert.True(MuiAreaCustomFontMessageMemoryCodec.TryReadUInt32(ref platform,
			closeAddress, MuiAreaCustomFontMessageKind.Close,
			MuiAreaCustomFontMessageField.Pointer, out var font));
		Assert.Equal(close.Font.Raw, font);

		Assert.False(MuiAreaCustomFontMessageMemoryCodec.TryGetAddress(ref platform,
			APTR.FromPointer(0x20FFC), MuiAreaCustomFontMessageKind.Open,
			MuiAreaCustomFontMessageField.Pointer, out _));
		Assert.False(MuiAreaCustomFontMessageMemoryCodec.TryGetAddress(ref platform,
			openAddress, MuiAreaCustomFontMessageKind.Open,
			(MuiAreaCustomFontMessageField)255, out _));
		Assert.False(MuiAreaCustomFontMessageMemoryCodec.TryGetAddress(ref platform,
			APTR.Null, MuiAreaCustomFontMessageKind.Close,
			MuiAreaCustomFontMessageField.Pointer, out _));
		cursor.Field = (MuiAreaCustomFontMessageField)255;
		Assert.False(MuiAreaCustomFontMessageFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out _, out _));
		cursor.Message = APTR.Null;
		cursor.Field = MuiAreaCustomFontMessageField.Pointer;
		Assert.False(MuiAreaCustomFontMessageFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out _, out _));
	}

	[Fact]
	public void CustomFontMessageFieldAccessUsesCompleteNamedRecords()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var openAddress = APTR.FromPointer(0x3600);
		var open = new MuiAreaOpenCustomFontMessage
		{
			MethodId = MuiAreaCustomFontMessageCodec.OpenCustomFont,
			Spec = APTR.FromPointer(0x3700),
		};
		Assert.True(MuiAreaOpenCustomFontMessageCodec.WriteStructural(ref platform,
			openAddress, open));
		Assert.True(MuiAreaCustomFontMessageMemoryCodec.TryReadUInt32(ref platform,
			openAddress, MuiAreaCustomFontMessageKind.Open,
			MuiAreaCustomFontMessageField.Pointer, out var spec));
		Assert.Equal(open.Spec.Raw, spec);
		Assert.True(MuiAreaCustomFontMessageMemoryCodec.TryWriteUInt32(ref platform,
			openAddress, MuiAreaCustomFontMessageKind.Open,
			MuiAreaCustomFontMessageField.Pointer, 0x3800u));
		Assert.True(MuiAreaOpenCustomFontMessageCodec.TryReadStructural(ref platform,
			openAddress, out var updatedOpen));
		Assert.Equal(open.MethodId, updatedOpen.MethodId);
		Assert.Equal(APTR.FromPointer(0x3800), updatedOpen.Spec);

		var closeAddress = APTR.FromPointer(0x3640);
		var close = new MuiAreaCloseCustomFontMessage
		{
			MethodId = MuiAreaCustomFontMessageCodec.CloseCustomFont,
			Font = APTR.FromPointer(0x3900),
		};
		Assert.True(MuiAreaCloseCustomFontMessageCodec.WriteStructural(ref platform,
			closeAddress, close));
		Assert.True(MuiAreaCustomFontMessageMemoryCodec.TryWriteUInt32(ref platform,
			closeAddress, MuiAreaCustomFontMessageKind.Close,
			MuiAreaCustomFontMessageField.Pointer, 0x3A00u));
		Assert.True(MuiAreaCloseCustomFontMessageCodec.TryReadStructural(ref platform,
			closeAddress, out var updatedClose));
		Assert.Equal(close.MethodId, updatedClose.MethodId);
		Assert.Equal(APTR.FromPointer(0x3A00), updatedClose.Font);

		Assert.False(MuiAreaCustomFontMessageMemoryCodec.TryReadUInt32(ref platform,
			openAddress, MuiAreaCustomFontMessageKind.Open,
			(MuiAreaCustomFontMessageField)255, out _));
		Assert.False(MuiAreaCustomFontMessageMemoryCodec.TryWriteUInt32(ref platform,
			APTR.FromPointer(0x20FFCu), MuiAreaCustomFontMessageKind.Close,
			MuiAreaCustomFontMessageField.Pointer, 1));
		Assert.False(MuiAreaCustomFontMessageMemoryCodec.TryReadUInt32(ref platform,
			APTR.Null, MuiAreaCustomFontMessageKind.Open,
			MuiAreaCustomFontMessageField.Pointer, out _));
	}

	[Fact]
	public void CustomFontDispatchAdmitsOnlyCompleteNamedPackets()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var openAddress = APTR.FromPointer(0x3000);
		Assert.True(MuiAreaCustomFontMessageCodec.WriteOpen(ref platform,
			openAddress, APTR.Null));
		Assert.Equal(0u, MuiAreaCustomFontMessageCore.Dispatch(ref platform,
			APTR.FromPointer(0x1000), APTR.FromPointer(0x3800), openAddress));

		var truncated = APTR.FromPointer(0x20FFC);
		Assert.Equal(0u, MuiAreaCustomFontMessageCore.Dispatch(ref platform,
			APTR.FromPointer(0x1000), APTR.FromPointer(0x3800), truncated));
	}
}
