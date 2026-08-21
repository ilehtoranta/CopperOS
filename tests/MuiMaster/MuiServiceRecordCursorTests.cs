using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiServiceRecordCursorTests
{
	[Fact]
	public void ErrorServiceFieldsUseNamedRecordBoundary()
	{
		var platform = CreatePlatform();
		var record = APTR.FromPointer(0x3000);
		var cursor = new MuiErrorServiceStateFieldCursor
		{
			Record = record,
			Field = MuiErrorServiceStateField.Error,
		};
		Assert.True(MuiErrorServiceStateFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out var address));
		Assert.Equal(APTR.FromPointer(0x3008), address);
		Assert.True(MuiErrorServiceStateFieldCursorCodec.TryWriteUInt32(
			ref platform, record, MuiErrorServiceStateField.Error, 9));
		Assert.True(MuiErrorServiceStateFieldCursorCodec.TryReadUInt32(
			ref platform, record, MuiErrorServiceStateField.Error, out var error));
		Assert.Equal(9u, error);
		Assert.False(MuiErrorServiceStateFieldCursorCodec.TryReadUInt32(
			ref platform, record, unchecked((MuiErrorServiceStateField)255), out _));
	}

	[Fact]
	public void GroupPageFieldsUseNamedRecordBoundary()
	{
		var platform = CreatePlatform();
		var record = APTR.FromPointer(0x3100);
		var cursor = new MuiGroupPageStateFieldCursor
		{
			Record = record,
			Field = MuiGroupPageStateField.LastSelector,
		};
		Assert.True(MuiGroupPageStateFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out var address));
		Assert.Equal(APTR.FromPointer(0x310C), address);
		Assert.True(MuiGroupPageStateFieldCursorCodec.TryWriteUInt32(ref platform,
			record, MuiGroupPageStateField.Active, 2));
		Assert.True(MuiGroupPageStateFieldCursorCodec.TryReadUInt32(ref platform,
			record, MuiGroupPageStateField.Active, out var active));
		Assert.Equal(2u, active);
		Assert.False(MuiGroupPageStateFieldCursorCodec.TryReadUInt32(ref platform,
			APTR.FromPointer(0xFFFFFFF0u), MuiGroupPageStateField.Changes, out _));
	}

	[Fact]
	public void StringInteger64FieldsUseNamedQuadBoundary()
	{
		var platform = CreatePlatform();
		var record = APTR.FromPointer(0x3200);
		var cursor = new MuiStringInteger64FieldCursor
		{
			Record = record,
			Field = MuiStringInteger64Field.Low,
		};
		Assert.True(MuiStringInteger64FieldCursorCodec.TryGetAddress(ref platform,
			cursor, out var address));
		Assert.Equal(APTR.FromPointer(0x3204), address);
		Assert.True(MuiStringInteger64FieldCursorCodec.TryWriteUInt32(ref platform,
			record, MuiStringInteger64Field.High, 0x7FFFFFFFu));
		Assert.True(MuiStringInteger64FieldCursorCodec.TryReadUInt32(ref platform,
			record, MuiStringInteger64Field.High, out var high));
		Assert.Equal(0x7FFFFFFFu, high);
		Assert.False(MuiStringInteger64FieldCursorCodec.TryReadUInt32(ref platform,
			record, unchecked((MuiStringInteger64Field)255), out _));
	}

	[Fact]
	public void RequesterServiceFieldsUseNamedRecordBoundary()
	{
		var platform = CreatePlatform();
		var record = APTR.FromPointer(0x3300);
		var cursor = new MuiRequesterServiceStateFieldCursor
		{
			Record = record,
			Field = MuiRequesterServiceStateField.Generation,
		};
		Assert.True(MuiRequesterServiceStateFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out var address));
		Assert.Equal(APTR.FromPointer(0x3304), address);
		Assert.True(MuiRequesterServiceStateFieldCursorCodec.TryWriteUInt32(
			ref platform, record, MuiRequesterServiceStateField.Generation, 3));
		Assert.True(MuiRequesterServiceStateFieldCursorCodec.TryReadUInt32(
			ref platform, record, MuiRequesterServiceStateField.Generation,
			out var generation));
		Assert.Equal(3u, generation);
		Assert.False(MuiRequesterServiceStateFieldCursorCodec.TryReadUInt32(
			ref platform, APTR.FromPointer(0xFFFFFFF0u),
			MuiRequesterServiceStateField.Magic, out _));
	}

	private static MuiHeadlessTestPlatform CreatePlatform() =>
		new(0x1000, 0x40000, 0x4000, APTR.FromPointer(0x1000));
}
