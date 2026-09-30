using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiCollectionEditMessageStructAdapterTests
{
	[Fact]
	public void CollectionEditPacketsUseNamedFieldsAndCompleteBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var createAddress = APTR.FromPointer(0x3000);
		Assert.True(MuiCollectionEditMessageCodec.WriteCreateEditObject(
			ref platform, createAddress, -2, 3, 0x3500));
		Assert.True(MuiCollectionEditMessageCodec.TryReadCreateEditObject(
			ref platform, createAddress, out var create));
		Assert.Equal(-2, create.Row);
		Assert.Equal(3, create.Column);
		Assert.Equal(0x3500u, create.Entry);
		Assert.True(MuiCollectionEditMessageMemoryCodec.TryWriteUInt32(ref platform,
			createAddress, MuiCollectionEditPacketKind.CreateEditObject,
			MuiCollectionEditField.Row, unchecked((uint)-5)));
		Assert.True(MuiCollectionEditMessageCodec.TryReadCreateEditObject(
			ref platform, createAddress, out create));
		Assert.Equal(MuiCollectionEditMessageCodec.CreateEditObject, create.MethodId);
		Assert.Equal(-5, create.Row);
		Assert.Equal(3, create.Column);
		Assert.Equal(0x3500u, create.Entry);
		Assert.True(MuiCollectionEditMessageMemoryCodec.TryGetAddress(
			ref platform, createAddress, MuiCollectionEditPacketKind.CreateEditObject,
			MuiCollectionEditField.Entry, out var entryAddress));
		Assert.Equal(APTR.FromPointer(0x300C), entryAddress);

		var editAddress = APTR.FromPointer(0x3040);
		Assert.True(MuiCollectionEditMessageCodec.WriteEdit(ref platform,
			editAddress, -4, 5));
		Assert.True(MuiCollectionEditMessageCodec.TryReadEdit(ref platform,
			editAddress, out var edit));
		Assert.Equal(-4, edit.Row);
		Assert.Equal(5, edit.Column);
		Assert.True(MuiCollectionEditMessageMemoryCodec.TryWriteUInt32(ref platform,
			editAddress, MuiCollectionEditPacketKind.Edit,
			MuiCollectionEditField.Column, unchecked((uint)-6)));
		Assert.True(MuiCollectionEditMessageCodec.TryReadEdit(ref platform,
			editAddress, out edit));
		Assert.Equal(MuiCollectionEditMessageCodec.Edit, edit.MethodId);
		Assert.Equal(-4, edit.Row);
		Assert.Equal(-6, edit.Column);

		var doneAddress = APTR.FromPointer(0x3060);
		Assert.True(MuiCollectionEditMessageCodec.WriteEditDone(ref platform,
			doneAddress, 6, -7, 0x3600, 0x3700));
		Assert.True(MuiCollectionEditMessageCodec.TryReadEditDone(ref platform,
			doneAddress, out var done));
		Assert.Equal(6, done.Row);
		Assert.Equal(-7, done.Column);
		Assert.Equal(0x3700u, done.EditObject);
		Assert.True(MuiCollectionEditMessageMemoryCodec.TryWriteUInt32(ref platform,
			doneAddress, MuiCollectionEditPacketKind.EditDone,
			MuiCollectionEditField.Entry, 0x3610));
		Assert.True(MuiCollectionEditMessageCodec.TryReadEditDone(ref platform,
			doneAddress, out done));
		Assert.Equal(MuiCollectionEditMessageCodec.EditDone, done.MethodId);
		Assert.Equal(6, done.Row);
		Assert.Equal(-7, done.Column);
		Assert.Equal(0x3610u, done.Entry);
		Assert.Equal(0x3700u, done.EditObject);

		var endAddress = APTR.FromPointer(0x3090);
		Assert.True(MuiCollectionEditMessageCodec.WriteEndEdit(ref platform,
			endAddress, 2));
		Assert.True(MuiCollectionEditMessageCodec.TryReadEndEdit(ref platform,
			endAddress, out var end));
		Assert.Equal(2u, end.Mode);
		Assert.True(MuiCollectionEditMessageMemoryCodec.TryWriteUInt32(ref platform,
			endAddress, MuiCollectionEditPacketKind.EndEdit,
			MuiCollectionEditField.Mode, 3));
		Assert.True(MuiCollectionEditMessageCodec.TryReadEndEdit(ref platform,
			endAddress, out end));
		Assert.Equal(MuiCollectionEditMessageCodec.EndEdit, end.MethodId);
		Assert.Equal(3u, end.Mode);
		Assert.False(MuiCollectionEditMessageMemoryCodec.TryReadUInt32(ref platform,
			endAddress, MuiCollectionEditPacketKind.Edit,
			MuiCollectionEditField.Mode, out _));
		Assert.False(MuiCollectionEditMessageMemoryCodec.TryWriteUInt32(ref platform,
			endAddress, MuiCollectionEditPacketKind.EndEdit,
			(MuiCollectionEditField)255, 1));

		Assert.False(MuiCollectionEditMessageMemoryCodec.TryGetAddress(
			ref platform, APTR.FromPointer(0x20FED),
			MuiCollectionEditPacketKind.EditDone,
			MuiCollectionEditField.EditObject, out _));
		Assert.False(MuiCollectionEditMessageMemoryCodec.TryGetAddress(
			ref platform, createAddress, MuiCollectionEditPacketKind.Edit,
			MuiCollectionEditField.Entry, out _));
		Assert.False(MuiCollectionEditMessageMemoryCodec.TryGetAddress(
			ref platform, createAddress, (MuiCollectionEditPacketKind)255,
			MuiCollectionEditField.MethodId, out _));
		Assert.False(MuiCollectionEditMessageMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiCollectionEditPacketKind.EndEdit,
			MuiCollectionEditField.Mode, out _));
	}

	[Fact]
	public void CollectionEditCompleteRecordsPreserveHighBitsWithoutOffsetFallback()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3100);
		Assert.True(MuiCollectionEditMessageCodec.WriteEditDone(ref platform,
			address, -0x4000, int.MinValue, 0xF00DCAFEu, 0x80032940u));
		Assert.True(MuiCollectionEditMessageCodec.TryReadEditDone(ref platform,
			address, out var packet));
		Assert.Equal(-0x4000, packet.Row);
		Assert.Equal(int.MinValue, packet.Column);
		Assert.Equal(0xF00DCAFEu, packet.Entry);
		Assert.Equal(0x80032940u, packet.EditObject);

		var truncated = APTR.FromPointer(0x20FFC);
		Assert.False(MuiCollectionEditMessageCodec.TryReadEditDone(ref platform,
			truncated, out _));
		Assert.False(MuiCollectionEditMessageCodec.WriteEditDone(ref platform,
			truncated, 1, 2, 3, 4));
		Assert.True(MuiCollectionEditMessageCodec.WriteEndEdit(ref platform,
			address, 2));
		Assert.True(MuiCollectionEditMessageCodec.TryReadEndEdit(ref platform,
			address, out var end));
		Assert.Equal(2u, end.Mode);
	}
}
