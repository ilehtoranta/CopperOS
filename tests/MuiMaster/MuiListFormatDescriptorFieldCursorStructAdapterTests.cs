using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiListFormatDescriptorFieldCursorStructAdapterTests
{
	[Fact]
	public void CursorWalksNamedFormatDescriptorRecord()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiListCore.MuiListFormatDescriptor
		{
			Delta = 1,
			Weight = 2,
			MinWidth = 3,
			MaxWidth = 4,
			Column = 5,
			Flags = 6,
			Preparse = APTR.FromPointer(0x1234),
			PreparseLength = 7,
			PreparseStorage = APTR.FromPointer(0x5678),
			PreparseStorageLength = 8,
		};
		Assert.True(MuiListCore.MuiListFormatDescriptorCodec.WriteRecord(
			ref platform, address, value));
		var cursor = new MuiListCore.MuiListFormatDescriptorFieldCursor
		{
			Address = address,
			Field = MuiListCore.MuiListFormatDescriptorField.PreparseStorageLength,
		};
		Assert.True(MuiListCore.MuiListFormatDescriptorFieldCursorCodec
			.TryGetAddress(ref platform, cursor, out var fieldAddress, out var fieldSize));
		Assert.Equal(0x3524u, fieldAddress.Raw);
		Assert.Equal(4u, fieldSize);
		Assert.True(MuiListCore.MuiListFormatDescriptorFieldCursorCodec
			.TryWriteUInt32(ref platform, address,
				MuiListCore.MuiListFormatDescriptorField.Flags, 66));
		Assert.True(MuiListCore.MuiListFormatDescriptorFieldCursorCodec
			.TryReadUInt32(ref platform, address,
				MuiListCore.MuiListFormatDescriptorField.Preparse, out var preparse));
		Assert.Equal(value.Preparse.Raw, preparse);
		Assert.True(MuiListCore.MuiListFormatDescriptorCodec.TryReadRecord(
			ref platform, address, out var decoded));
		Assert.Equal(value.Delta, decoded.Delta);
		Assert.Equal(value.Weight, decoded.Weight);
		Assert.Equal(value.MinWidth, decoded.MinWidth);
		Assert.Equal(value.MaxWidth, decoded.MaxWidth);
		Assert.Equal(value.Column, decoded.Column);
		Assert.Equal(66u, decoded.Flags);
		Assert.Equal(value.Preparse.Raw, decoded.Preparse.Raw);
		Assert.Equal(value.PreparseLength, decoded.PreparseLength);
		Assert.Equal(value.PreparseStorage.Raw, decoded.PreparseStorage.Raw);
		Assert.Equal(value.PreparseStorageLength, decoded.PreparseStorageLength);
	}

	[Fact]
	public void CursorRejectsUnknownAndIncompleteRecords()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		Assert.False(MuiListCore.MuiListFormatDescriptorFieldCursorCodec
			.TryGetAddress(ref platform,
				new MuiListCore.MuiListFormatDescriptorFieldCursor
				{
					Address = address,
					Field = (MuiListCore.MuiListFormatDescriptorField)255,
				}, out _, out _));
		Assert.False(MuiListCore.MuiListFormatDescriptorFieldCursorCodec
			.TryGetAddress(ref platform,
				new MuiListCore.MuiListFormatDescriptorFieldCursor
				{
					Address = APTR.FromPointer(0x30FF9),
					Field = MuiListCore.MuiListFormatDescriptorField.Delta,
				}, out _, out _));
		Assert.False(MuiListCore.MuiListFormatDescriptorMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiListCore.MuiListFormatDescriptorField.Delta,
			out _, out _));
	}
}
