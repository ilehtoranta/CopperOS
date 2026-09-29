using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiSelectgroupActiveStructAdapterTests
{
	[Fact]
	public void SelectgroupActiveStructAdapterUsesNamedFieldsAndBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var address = APTR.FromPointer(0x3500);
		var value = new MuiSelectgroupActiveStateRecord
		{
			Magic = MuiSelectgroupActiveStateRecord.Cookie,
			Active = 7,
		};

		Assert.True(MuiSelectgroupActiveStateRecordCodec.Write(ref platform,
			address, value));
		Assert.True(MuiSelectgroupActiveStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiSelectgroupActiveStateField.Active,
			out var activeAddress));
		Assert.Equal(0x3504u, activeAddress.Raw);
		var activeCursor = new MuiSelectgroupActiveStateFieldCursor
		{
			Record = address,
			Field = MuiSelectgroupActiveStateField.Active,
		};
		Assert.True(MuiSelectgroupActiveStateFieldCursorCodec.TryGetAddress(ref platform,
			activeCursor, out var cursorActive, out var cursorFieldSize));
		Assert.Equal(activeAddress, cursorActive);
		Assert.Equal(MuiSelectgroupActiveStateRecord.FieldSize, cursorFieldSize);
		Assert.True(MuiSelectgroupActiveStateRecordMemoryCodec.TryGetAddress(ref platform,
			activeCursor, out var memoryActive, out var memoryFieldSize));
		Assert.Equal(cursorActive, memoryActive);
		Assert.Equal(cursorFieldSize, memoryFieldSize);
		Assert.True(MuiSelectgroupActiveStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiSelectgroupActiveStateField.Active, 2));
		Assert.True(MuiSelectgroupActiveStateRecordCodec.TryReadStructural(
			ref platform, address, out var decoded));
		Assert.Equal(2u, decoded.Active);
		Assert.False(MuiSelectgroupActiveStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.FromPointer(0x30FF9),
			MuiSelectgroupActiveStateField.Magic, out _));
		Assert.False(MuiSelectgroupActiveStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiSelectgroupActiveStateField.Magic, out _));
		activeCursor.Field = (MuiSelectgroupActiveStateField)255;
		Assert.False(MuiSelectgroupActiveStateFieldCursorCodec.TryGetAddress(ref platform,
			activeCursor, out _, out _));
		activeCursor.Record = APTR.Null;
		activeCursor.Field = MuiSelectgroupActiveStateField.Active;
		Assert.False(MuiSelectgroupActiveStateFieldCursorCodec.TryGetAddress(ref platform,
			activeCursor, out _, out _));
	}
}
