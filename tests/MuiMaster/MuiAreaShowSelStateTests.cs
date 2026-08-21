using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiAreaShowSelStateTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);

	[Fact]
	public void GadgetSelectedVisualHonorsInitializeOnlyShowSelState()
	{
		var platform = NewPlatform();
		var gadgetClass = Register(ref platform, 0x1100, "Gadget.mui");
		var gadget = MuiCommonControlCore.CreateControl(ref platform, State,
			gadgetClass, BuildTags(ref platform, 0x1200, new[] {
				(MuiCommonControlCore.Selected, 1u),
				(MuiCommonControlCore.ShowSelState, 0u) }));

		Assert.True(MuiCommonControlCore.TryGetGadgetInteractionStateRecord(
			ref platform, State, gadget, out var record));
		Assert.Equal(0u, record.ShowSelState);
		Assert.Equal(0u, Get(ref platform, gadget,
			MuiCommonControlCore.ShowSelState));
		Assert.False(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			gadget, MuiCommonControlCore.ShowSelState, 1, false));

		var renderInfo = APTR.FromPointer(0x1300);
		WriteRenderInfo(ref platform, renderInfo, APTR.FromPointer(0x1400));
		Assert.True(MuiAreaLayoutCore.Setup(ref platform, State, gadget, renderInfo));
		Assert.True(MuiAreaLayoutCore.Layout(ref platform, State, gadget, 0, 0,
			20, 10));
		Assert.True(MuiCommonControlCore.DrawControl(ref platform, State, gadget,
			0));
		Assert.Equal(0u, platform.LineCount);

		// A raw write models the guest initializer/compatibility seam; runtime
		// SetControlAttribute remains correctly rejected for this [I] attribute.
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, gadget,
			MuiCommonControlCore.ShowSelState, 1, false));
		Assert.True(MuiCommonControlCore.DrawControl(ref platform, State, gadget,
			0));
		Assert.Equal(4u, platform.LineCount);
	}

	[Fact]
	public void ImageSelectedPenHonorsInitializeOnlyShowSelState()
	{
		var platform = NewPlatform();
		var imageClass = Register(ref platform, 0x1100, "Image.mui");
		var image = MuiCommonControlCore.CreateControl(ref platform, State,
			imageClass, BuildTags(ref platform, 0x1200, new[] {
				(MuiCommonControlCore.ImageBuiltinSpec, 0x0Bu),
				(MuiCommonControlCore.Selected, 1u),
				(MuiCommonControlCore.ShowSelState, 0u) }));

		Assert.True(MuiCommonControlCore.TryGetImageRenderStateRecord(
			ref platform, State, image, out var record));
		Assert.Equal(0u, record.ShowSelState);
		Assert.Equal(0u, Get(ref platform, image,
			MuiCommonControlCore.ShowSelState));
		Assert.False(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			image, MuiCommonControlCore.ShowSelState, 1, false));

		var renderInfo = APTR.FromPointer(0x1300);
		WriteRenderInfo(ref platform, renderInfo, APTR.FromPointer(0x1400));
		Assert.True(MuiAreaLayoutCore.Setup(ref platform, State, image, renderInfo));
		Assert.True(MuiAreaLayoutCore.Layout(ref platform, State, image, 0, 0,
			20, 10));
		Assert.True(MuiCommonControlCore.DrawControl(ref platform, State, image, 0));
		Assert.Equal(2u, platform.LastPen);

		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, image,
			MuiCommonControlCore.ShowSelState, 1, false));
		Assert.True(MuiCommonControlCore.DrawControl(ref platform, State, image, 0));
		Assert.Equal(3u, platform.LastPen);
	}

	private static MuiHeadlessTestPlatform NewPlatform() =>
		new(0x1000, 0x40000, 0x8000, State);

	private static APTR Register(ref MuiHeadlessTestPlatform platform,
		uint nameAddress, string name)
	{
		platform.WriteCString(APTR.FromPointer(nameAddress), name);
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		return MuiHeadlessObjectCore.RegisterClass(ref platform, State,
			APTR.FromPointer(nameAddress), APTR.Null, 1, APTR.FromPointer(1), false);
	}

	private static APTR BuildTags(ref MuiHeadlessTestPlatform platform, uint address,
		(uint tag, uint data)[] pairs)
	{
		var offset = 0;
		foreach (var pair in pairs)
		{
			platform.WriteUInt32(APTR.FromPointer(address), offset, pair.tag);
			platform.WriteUInt32(APTR.FromPointer(address), offset + 4, pair.data);
			offset += 8;
		}
		platform.WriteUInt32(APTR.FromPointer(address), offset, 0);
		platform.WriteUInt32(APTR.FromPointer(address), offset + 4, 0);
		return APTR.FromPointer(address);
	}

	private static uint Get(ref MuiHeadlessTestPlatform platform, APTR obj,
		uint attribute)
	{
		Assert.True(MuiCommonControlCore.TryGet(ref platform, State, obj,
			attribute, out var value, out var handled));
		Assert.True(handled);
		return value;
	}

	private static void WriteRenderInfo(ref MuiHeadlessTestPlatform platform,
		APTR address, APTR rastPort)
	{
		var record = default(MuiDrawingRenderInfoRecord);
		record.RastPort = rastPort;
		Assert.True(MuiDrawingRenderInfoCodec.Write(ref platform, address, record));
	}
}
