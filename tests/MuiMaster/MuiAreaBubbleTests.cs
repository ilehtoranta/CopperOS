using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiAreaBubbleTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);

	[Fact]
	public void BubblePacketsUseNamedCreateAndDeleteFields()
	{
		var platform = CreatePlatform(out _);
		var text = APTR.FromPointer(0x1800);
		platform.WriteCString(text, "Bubble");
		var create = APTR.FromPointer(0x1900);
		Assert.True(MuiAreaBubbleMessageCodec.WriteCreate(ref platform, create,
			-11, 23, text, 7));
		Assert.True(MuiAreaBubbleMessageCodec.TryReadCreate(ref platform, create,
			out var createPacket));
		Assert.Equal(-11, createPacket.X);
		Assert.Equal(23, createPacket.Y);
		Assert.Equal(text, createPacket.Text);
		Assert.Equal(7u, createPacket.Flags);

		var delete = APTR.FromPointer(0x1A00);
		Assert.True(MuiAreaBubbleMessageCodec.WriteDelete(ref platform, delete,
			APTR.FromPointer(0x1B00)));
		Assert.True(MuiAreaBubbleMessageCodec.TryReadDelete(ref platform, delete,
			out var deletePacket));
		Assert.Equal(APTR.FromPointer(0x1B00), deletePacket.Bubble);
		Assert.False(MuiAreaBubbleMessageCodec.TryReadCreate(ref platform,
			APTR.FromPointer(0x20FFFu), out _));
	}

	[Fact]
	public void BubbleRecordAdapterOwnsPacketBounds()
	{
		var platform = CreatePlatform(out _);
		var create = APTR.FromPointer(0x2400);
		Assert.True(MuiAreaBubbleMessageMemoryCodec.TryWriteUInt32(ref platform,
			create, MuiAreaBubblePacketKind.Create,
			MuiAreaBubbleMessageField.MethodId,
			MuiAreaBubbleMessageCodec.CreateBubble));
		Assert.True(MuiAreaBubbleMessageMemoryCodec.TryWriteUInt32(ref platform,
			create, MuiAreaBubblePacketKind.Create,
			MuiAreaBubbleMessageField.X, unchecked((uint)-3)));
		Assert.True(MuiAreaBubbleMessageMemoryCodec.TryWriteUInt32(ref platform,
			create, MuiAreaBubblePacketKind.Create,
			MuiAreaBubbleMessageField.Flags, 9));
		Assert.True(MuiAreaBubbleMessageMemoryCodec.TryReadUInt32(ref platform,
			create, MuiAreaBubblePacketKind.Create,
			MuiAreaBubbleMessageField.X, out var x));
		Assert.Equal(unchecked((uint)-3), x);
		Assert.True(MuiAreaBubbleMessageMemoryCodec.TryGetAddress(ref platform,
			create, MuiAreaBubblePacketKind.Create,
			MuiAreaBubbleMessageField.Text, out var textAddress));
		Assert.Equal(create.Raw + MuiAreaCreateBubbleMessage.TextOffset,
			textAddress.Raw);
		var cursor = new MuiAreaBubbleMessageFieldCursor
		{
			Message = create,
			Packet = MuiAreaBubblePacketKind.Create,
			Field = MuiAreaBubbleMessageField.Text,
		};
		Assert.True(MuiAreaBubbleMessageFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out var typedTextAddress, out var typedTextSize));
		Assert.Equal(textAddress, typedTextAddress);
		Assert.Equal(MuiAreaCreateBubbleMessage.FieldSize, typedTextSize);
		Assert.True(MuiAreaBubbleMessageMemoryCodec.TryGetAddress(ref platform,
			cursor, out var memoryTextAddress, out var memoryTextSize));
		Assert.Equal(typedTextAddress, memoryTextAddress);
		Assert.Equal(typedTextSize, memoryTextSize);

		Assert.False(MuiAreaBubbleMessageMemoryCodec.TryGetAddress(ref platform,
			APTR.FromPointer(0x20FF0), MuiAreaBubblePacketKind.Create,
			MuiAreaBubbleMessageField.Flags, out _));
		Assert.False(MuiAreaBubbleMessageMemoryCodec.TryGetAddress(ref platform,
			create, MuiAreaBubblePacketKind.Delete,
			MuiAreaBubbleMessageField.Flags, out _));
		Assert.False(MuiAreaBubbleMessageMemoryCodec.TryGetAddress(ref platform,
			APTR.Null, MuiAreaBubblePacketKind.Delete,
			MuiAreaBubbleMessageField.Bubble, out _));
		cursor.Field = (MuiAreaBubbleMessageField)255;
		Assert.False(MuiAreaBubbleMessageFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out _, out _));
		cursor.Message = APTR.Null;
		cursor.Field = MuiAreaBubbleMessageField.Text;
		Assert.False(MuiAreaBubbleMessageFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out _, out _));
	}

	[Fact]
	public void BubbleFieldAccessUsesCompleteNamedRecords()
	{
		var platform = CreatePlatform(out _);
		var createAddress = APTR.FromPointer(0x2500);
		var create = new MuiAreaCreateBubbleMessage
		{
			MethodId = MuiAreaBubbleMessageCodec.CreateBubble,
			X = -11,
			Y = 23,
			Text = APTR.FromPointer(0x1800),
			Flags = 7,
		};
		Assert.True(MuiAreaCreateBubbleMessageCodec.WriteStructural(ref platform,
			createAddress, create));
		Assert.True(MuiAreaBubbleMessageMemoryCodec.TryReadUInt32(ref platform,
			createAddress, MuiAreaBubblePacketKind.Create,
			MuiAreaBubbleMessageField.X, out var x));
		Assert.Equal(unchecked((uint)create.X), x);
		Assert.True(MuiAreaBubbleMessageMemoryCodec.TryWriteUInt32(ref platform,
			createAddress, MuiAreaBubblePacketKind.Create,
			MuiAreaBubbleMessageField.Y, unchecked((uint)-17)));
		Assert.True(MuiAreaBubbleMessageMemoryCodec.TryWriteUInt32(ref platform,
			createAddress, MuiAreaBubblePacketKind.Create,
			MuiAreaBubbleMessageField.Text, 0x1900u));
		Assert.True(MuiAreaCreateBubbleMessageCodec.TryReadStructural(ref platform,
			createAddress, out var updatedCreate));
		Assert.Equal(create.MethodId, updatedCreate.MethodId);
		Assert.Equal(create.X, updatedCreate.X);
		Assert.Equal(-17, updatedCreate.Y);
		Assert.Equal(APTR.FromPointer(0x1900), updatedCreate.Text);
		Assert.Equal(create.Flags, updatedCreate.Flags);

		var deleteAddress = APTR.FromPointer(0x2540);
		var delete = new MuiAreaDeleteBubbleMessage
		{
			MethodId = MuiAreaBubbleMessageCodec.DeleteBubble,
			Bubble = APTR.FromPointer(0x1A00),
		};
		Assert.True(MuiAreaDeleteBubbleMessageCodec.WriteStructural(ref platform,
			deleteAddress, delete));
		Assert.True(MuiAreaBubbleMessageMemoryCodec.TryWriteUInt32(ref platform,
			deleteAddress, MuiAreaBubblePacketKind.Delete,
			MuiAreaBubbleMessageField.Bubble, 0x1B00u));
		Assert.True(MuiAreaDeleteBubbleMessageCodec.TryReadStructural(ref platform,
			deleteAddress, out var updatedDelete));
		Assert.Equal(delete.MethodId, updatedDelete.MethodId);
		Assert.Equal(APTR.FromPointer(0x1B00), updatedDelete.Bubble);

		Assert.False(MuiAreaBubbleMessageMemoryCodec.TryReadUInt32(ref platform,
			createAddress, MuiAreaBubblePacketKind.Create,
			MuiAreaBubbleMessageField.Bubble, out _));
		Assert.False(MuiAreaBubbleMessageMemoryCodec.TryWriteUInt32(ref platform,
			APTR.FromPointer(0x20FF0), MuiAreaBubblePacketKind.Create,
			MuiAreaBubbleMessageField.Flags, 1));
		Assert.False(MuiAreaBubbleMessageMemoryCodec.TryReadUInt32(ref platform,
			APTR.Null, MuiAreaBubblePacketKind.Delete,
			MuiAreaBubbleMessageField.Bubble, out _));
	}

	[Fact]
	public void DispatcherCreatesAndDeletesProviderOwnedBubble()
	{
		var platform = CreatePlatform(out var areaClass);
		var obj = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			areaClass, APTR.Null);
		var text = APTR.FromPointer(0x1C00);
		platform.WriteCString(text, "Provider bubble");
		var bubble = APTR.FromPointer(0x1D00);
		platform.BubbleCreateSampleAvailable = true;
		platform.BubbleCreateResult = bubble;

		var create = APTR.FromPointer(0x1E00);
		Assert.True(MuiAreaBubbleMessageCodec.WriteCreate(ref platform, create,
			-4, 19, text, 3));
		Assert.Equal(bubble.Raw, MuiCommonControlDispatcher.Dispatch(ref platform,
			State, obj, create));
		Assert.Equal(obj, platform.LastBubbleCreateObject);
		Assert.Equal(-4, platform.LastBubbleCreateX);
		Assert.Equal(19, platform.LastBubbleCreateY);
		Assert.Equal(text, platform.LastBubbleCreateText);
		Assert.Equal(3u, platform.LastBubbleCreateFlags);

		platform.BubbleDeleteSampleAvailable = true;
		var delete = APTR.FromPointer(0x1F00);
		Assert.True(MuiAreaBubbleMessageCodec.WriteDelete(ref platform, delete,
			bubble));
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			obj, delete));
		Assert.Equal(bubble, platform.LastBubbleDeleted);
	}

	[Fact]
	public void BubbleCreationDeclinesUnmappedTextBeforeProviderCall()
	{
		var platform = CreatePlatform(out var areaClass);
		var obj = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			areaClass, APTR.Null);
		platform.BubbleCreateSampleAvailable = true;
		platform.BubbleCreateResult = APTR.FromPointer(0x1D00);
		var result = MuiAreaBubblePacketCore.Create(ref platform, State, obj, 1, 2,
			APTR.FromPointer(0x21000u), 0);
		Assert.Equal(0u, result.Raw);
		Assert.Equal(APTR.Null, platform.LastBubbleCreateObject);
	}

	private static MuiHeadlessTestPlatform CreatePlatform(out APTR areaClass)
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var name = APTR.FromPointer(0x1100);
		platform.WriteCString(name, "Text.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		areaClass = MuiHeadlessObjectCore.RegisterClass(ref platform, State, name,
			APTR.Null, 0, APTR.FromPointer(1), false);
		return platform;
	}
}
