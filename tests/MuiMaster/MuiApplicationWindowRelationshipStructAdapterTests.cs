using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiApplicationWindowRelationshipStructAdapterTests
{
	[Fact]
	public void ApplicationWindowRelationshipStructAdapterUsesNamedFieldsAndBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiApplicationWindowRelationshipStateRecord
		{
			Magic = MuiApplicationWindowRelationshipStateRecord.Cookie,
			LastWindow = APTR.Null,
			AddedCount = uint.MaxValue,
		};

		Assert.True(MuiApplicationWindowRelationshipStateRecordCodec.Write(
			ref platform, address, value));
		Assert.True(MuiApplicationWindowRelationshipStateRecordMemoryCodec
			.TryGetAddress(ref platform, address,
				MuiApplicationWindowRelationshipStateField.LastWindow,
				out var windowAddress));
		Assert.Equal(0x3504u, windowAddress.Raw);
		Assert.True(MuiApplicationWindowRelationshipStateRecordMemoryCodec
			.TryReadUInt32(ref platform, address,
				MuiApplicationWindowRelationshipStateField.AddedCount,
				out var addedCount));
		Assert.Equal(uint.MaxValue, addedCount);
		Assert.True(MuiApplicationWindowRelationshipStateRecordMemoryCodec
			.TryWriteUInt32(ref platform, address,
				MuiApplicationWindowRelationshipStateField.AddedCount, 7));
		Assert.True(MuiApplicationWindowRelationshipStateRecordCodec
			.TryReadStructural(ref platform, address, out var decoded));
		Assert.Equal(7u, decoded.AddedCount);
		Assert.False(MuiApplicationWindowRelationshipStateRecordMemoryCodec
			.TryGetAddress(ref platform, APTR.FromPointer(0x30FF5),
				MuiApplicationWindowRelationshipStateField.Magic, out _));
		Assert.False(MuiApplicationWindowRelationshipStateRecordMemoryCodec
			.TryGetAddress(ref platform, APTR.Null,
				MuiApplicationWindowRelationshipStateField.Magic, out _));
	}

	[Fact]
	public void ApplicationWindowRelationshipSequentialRecordPreservesPointersAndBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x35C0);
		var value = new MuiApplicationWindowRelationshipStateRecord
		{
			Magic = MuiApplicationWindowRelationshipStateRecord.Cookie,
			LastWindow = APTR.FromPointer(uint.MaxValue),
			AddedCount = uint.MaxValue,
		};

		Assert.True(MuiApplicationWindowRelationshipStateRecordCodec.WriteRecord(
			ref platform, address, value));
		Assert.True(MuiApplicationWindowRelationshipStateRecordCodec.TryReadRecord(
			ref platform, address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.LastWindow, decoded.LastWindow);
		Assert.Equal(value.AddedCount, decoded.AddedCount);

		var crossingEnd = APTR.FromPointer(0x30FF5);
		Assert.False(MuiApplicationWindowRelationshipStateRecordCodec.WriteRecord(
			ref platform, crossingEnd, value));
		Assert.False(MuiApplicationWindowRelationshipStateRecordCodec.TryReadRecord(
			ref platform, crossingEnd, out _));
	}

	[Fact]
	public void ApplicationWindowRelationshipFieldPathPreservesNamedRecord()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3640);
		var initial = new MuiApplicationWindowRelationshipStateRecord
		{
			Magic = 0x10203040u,
			LastWindow = APTR.FromPointer(0x50607080u),
			AddedCount = 0x90A0B0C0u,
		};

		Assert.True(MuiApplicationWindowRelationshipStateRecordCodec.WriteRecord(
			ref platform, address, initial));
		Assert.True(MuiApplicationWindowRelationshipStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address,
			MuiApplicationWindowRelationshipStateField.AddedCount, 0xF1020304u));
		Assert.True(MuiApplicationWindowRelationshipStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address,
			MuiApplicationWindowRelationshipStateField.LastWindow, out var window));
		Assert.Equal(initial.LastWindow.Raw, window);
		Assert.True(MuiApplicationWindowRelationshipStateRecordCodec.TryReadStructural(
			ref platform, address, out var updated));
		Assert.Equal(initial.Magic, updated.Magic);
		Assert.Equal(initial.LastWindow, updated.LastWindow);
		Assert.Equal(0xF1020304u, updated.AddedCount);
		Assert.False(MuiApplicationWindowRelationshipStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address,
			unchecked((MuiApplicationWindowRelationshipStateField)255), 1));
	}
}
