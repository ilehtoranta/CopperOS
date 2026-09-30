using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiColorFieldStructAdapterTests
{
	private static MuiHeadlessTestPlatform NewPlatform()
	{
		var state = APTR.FromPointer(0x1000);
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000,
			0x8000, state);
		Assert.True(MuiDrawingServiceCore.Initialize(ref platform, state));
		return platform;
	}

	[Fact]
	public void PenSpecFieldAdapterPreservesReservedSiblings()
	{
		var platform = NewPlatform();
		var address = APTR.FromPointer(0x2180);
		var expected = new MuiColorPenSpecRecord
		{
			Kind = MuiColorSpecialistLayout.SpecKindRgb,
			Scalar = 0x10203040,
			Red = 0x11223344,
			Green = 0x55667788,
			Blue = 0x99AABBCC,
			Reserved0 = 0xDEADBEEF,
			Reserved1 = 0x0BADF00D,
			Reserved2 = 0xFEEDFACE,
		};
		Assert.True(MuiColorPenSpecCodec.WriteRecord(ref platform, address,
			expected));
		var cursor = new MuiColorRecordFieldCursor
		{
			Address = address,
			Record = MuiColorRecordKind.PenSpec,
			Field = MuiColorRecordField.Reserved2,
		};
		Assert.True(MuiColorRecordFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out var cursorReserved2Address));
		Assert.Equal(address.Raw + MuiColorPenSpecRecord.Reserved2Offset,
			cursorReserved2Address.Raw);
		Assert.True(MuiColorRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, MuiColorRecordKind.PenSpec, MuiColorRecordField.Green,
			0xF00DCAFE));
		Assert.True(MuiColorRecordMemoryCodec.TryReadUInt32(ref platform,
			address, MuiColorRecordKind.PenSpec, MuiColorRecordField.Green,
			out var green));
		Assert.Equal(0xF00DCAFEu, green);
		Assert.True(MuiColorPenSpecCodec.TryReadRecord(ref platform, address,
			out var actual));
		Assert.Equal(expected.Kind, actual.Kind);
		Assert.Equal(expected.Scalar, actual.Scalar);
		Assert.Equal(expected.Red, actual.Red);
		Assert.Equal(expected.Blue, actual.Blue);
		Assert.Equal(expected.Reserved0, actual.Reserved0);
		Assert.Equal(expected.Reserved1, actual.Reserved1);
		Assert.Equal(expected.Reserved2, actual.Reserved2);
		Assert.False(MuiColorRecordMemoryCodec.TryReadUInt32(ref platform,
			APTR.FromPointer(0x20FFE), MuiColorRecordKind.PenSpec,
			MuiColorRecordField.Green, out _));
	}

	[Fact]
	public void RgbFieldAdapterUsesNamedComponents()
	{
		var platform = NewPlatform();
		var address = APTR.FromPointer(0x2300);
		Assert.True(MuiColorRgbCodec.WriteRecord(ref platform, address,
			new MuiColorRgbRecord
			{
				Red = 1,
				Green = 2,
				Blue = 3,
			}));
		Assert.True(MuiColorRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, MuiColorRecordKind.Rgb, MuiColorRecordField.Blue,
			0xAABBCCDD));
		Assert.True(MuiColorRgbCodec.TryReadRecord(ref platform, address,
			out var rgb));
		Assert.Equal(1u, rgb.Red);
		Assert.Equal(2u, rgb.Green);
		Assert.Equal(0xAABBCCDDu, rgb.Blue);
		Assert.False(MuiColorRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, MuiColorRecordKind.Rgb, MuiColorRecordField.Flags, 1));
	}
}
