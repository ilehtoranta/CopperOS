using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiListColumnLayoutAdmissionTests
{
	[Fact]
	public void ListColumnLayoutAndGeometryRoundTripThroughNamedStructs()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var layoutAddress = APTR.FromPointer(0x2E00);
		var valuesAddress = APTR.FromPointer(0x2F00);
		var layout = new MuiListCore.MuiListColumnLayoutState
		{
			Magic = MuiListCore.MuiListColumnLayoutState.Cookie,
			Width = 512,
			Columns = 2,
			Values = valuesAddress,
		};
		Assert.True(MuiListCore.MuiListColumnGeometryCodec.Write(ref platform,
			valuesAddress, new MuiListCore.MuiListColumnGeometry
			{
				Offset = 4,
				Width = 120,
			}));
		Assert.True(MuiListCore.MuiListColumnGeometryCodec.Write(ref platform,
			APTR.FromPointer(valuesAddress.Raw + 8),
			new MuiListCore.MuiListColumnGeometry
			{
				Offset = 124,
				Width = 388,
			}));
		Assert.True(MuiListCore.MuiListColumnLayoutStateCodec.Write(ref platform,
			layoutAddress, layout));
		Assert.True(MuiListCore.MuiListColumnLayoutStateCodec.TryRead(ref platform,
			layoutAddress, out var readLayout));
		Assert.Equal(layout.Width, readLayout.Width);
		Assert.Equal(layout.Columns, readLayout.Columns);
		Assert.Equal(layout.Values, readLayout.Values);
		Assert.True(MuiListCore.MuiListColumnGeometryCodec.TryRead(ref platform,
			valuesAddress, out var first));
		Assert.Equal(4u, first.Offset);
		Assert.True(MuiListCore.MuiListColumnGeometryCodec.TryRead(ref platform,
			APTR.FromPointer(valuesAddress.Raw + 8), out var second));
		Assert.Equal(388u, second.Width);
	}

	[Fact]
	public void MalformedListColumnLayoutMagicRemainsStructuralButFailsClosed()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			APTR.FromPointer(0x1000));
		var layoutAddress = APTR.FromPointer(0x2F40);
		var valuesAddress = APTR.FromPointer(0x3000);
		Assert.True(MuiListCore.MuiListColumnLayoutStateCodec.Write(ref platform,
			layoutAddress, new MuiListCore.MuiListColumnLayoutState
			{
				Magic = MuiListCore.MuiListColumnLayoutState.Cookie,
				Width = 320,
				Columns = 1,
				Values = valuesAddress,
			}));
		Assert.True(MuiListCore.MuiListColumnLayoutFieldCursorCodec
			.TryWriteUInt32(ref platform, layoutAddress,
				MuiListCore.MuiListColumnLayoutField.Magic, 0));
		Assert.True(MuiListCore.MuiListColumnLayoutStateCodec.TryReadStructural(
			ref platform, layoutAddress, out var structural));
		Assert.Equal(0u, structural.Magic);
		Assert.Equal(320u, structural.Width);
		Assert.Equal(valuesAddress, structural.Values);
		Assert.False(MuiListCore.MuiListColumnLayoutStateCodec.TryRead(
			ref platform, layoutAddress, out _));
		Assert.True(MuiListCore.MuiListColumnLayoutStateCodec.TryReadStorage(
			ref platform, layoutAddress, out var storage));
		Assert.Equal(0u, storage.Magic);
	}
}
