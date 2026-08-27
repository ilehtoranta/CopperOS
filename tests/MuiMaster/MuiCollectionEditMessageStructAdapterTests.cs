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

		var doneAddress = APTR.FromPointer(0x3060);
		Assert.True(MuiCollectionEditMessageCodec.WriteEditDone(ref platform,
			doneAddress, 6, -7, 0x3600, 0x3700));
		Assert.True(MuiCollectionEditMessageCodec.TryReadEditDone(ref platform,
			doneAddress, out var done));
		Assert.Equal(6, done.Row);
		Assert.Equal(-7, done.Column);
		Assert.Equal(0x3700u, done.EditObject);

		var endAddress = APTR.FromPointer(0x3090);
		Assert.True(MuiCollectionEditMessageCodec.WriteEndEdit(ref platform,
			endAddress, 2));
		Assert.True(MuiCollectionEditMessageCodec.TryReadEndEdit(ref platform,
			endAddress, out var end));
		Assert.Equal(2u, end.Mode);

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
}
