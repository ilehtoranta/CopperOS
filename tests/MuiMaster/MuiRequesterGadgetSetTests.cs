using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiRequesterGadgetSetTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);
	private static readonly APTR Gadgets = APTR.FromPointer(0x1400);

	[Fact]
	public void GadgetSetProducesNamedButtonLabelsShortcutsAndReturnIds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		WriteCString(ref platform, Gadgets, "_Save|*_Use|_Cancel");

		Assert.True(MuiRequesterGadgetSetCore.TryParse(ref platform, Gadgets,
			out var set));
		Assert.Equal(Gadgets, set.Source);
		Assert.Equal(19u, set.StringLength);
		Assert.Equal(3u, set.GadgetCount);
		Assert.Equal(1u, set.HasActiveGadget);
		Assert.Equal(1u, set.ActiveGadgetOrdinal);

		Assert.True(MuiRequesterGadgetSetCore.TryGetButton(ref platform, set, 0,
			out var save));
		Assert.Equal(1u, save.ReturnId);
		Assert.Equal(0u, save.IsActive);
		Assert.Equal((uint)'S', save.ControlChar);
		Assert.Equal(5u, save.LabelLength);
		Assert.True(MuiRequesterGadgetSetCore.TryReadLabelByte(ref platform,
			save, 0, out var saveFirst));
		Assert.Equal((byte)'_', saveFirst);
		Assert.True(MuiRequesterGadgetSetCore.TryReadLabelByte(ref platform,
			save, 4, out var saveLast));
		Assert.Equal((byte)'e', saveLast);

		Assert.True(MuiRequesterGadgetSetCore.TryGetButton(ref platform, set, 1,
			out var use));
		Assert.Equal(2u, use.ReturnId);
		Assert.Equal(1u, use.IsActive);
		Assert.Equal((uint)'U', use.ControlChar);
		Assert.Equal(4u, use.LabelLength);
		Assert.True(MuiRequesterGadgetSetCore.TryReadLabelByte(ref platform,
			use, 0, out var useFirst));
		Assert.Equal((byte)'_', useFirst);

		Assert.True(MuiRequesterGadgetSetCore.TryGetButton(ref platform, set, 2,
			out var cancel));
		Assert.Equal(0u, cancel.ReturnId);
		Assert.Equal(0u, cancel.IsActive);
		Assert.Equal((uint)'C', cancel.ControlChar);
		Assert.Equal(7u, cancel.LabelLength);
	}

	[Fact]
	public void GadgetSetRejectsDuplicateActiveMarkersAndChangedSource()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		WriteCString(ref platform, Gadgets, "*One|*Two");
		Assert.False(MuiRequesterGadgetSetCore.TryParse(ref platform, Gadgets,
			out _));

		WriteCString(ref platform, Gadgets, "One|Two");
		Assert.True(MuiRequesterGadgetSetCore.TryParse(ref platform, Gadgets,
			out var set));
		WriteCString(ref platform, Gadgets, "*One|*Two");
		Assert.False(MuiRequesterGadgetSetCore.TryGetButton(ref platform, set, 0,
			out _));
	}

	[Fact]
	public void NullAndEmptyGadgetSetsHaveNoSelectableButtons()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		Assert.True(MuiRequesterGadgetSetCore.TryParse(ref platform, APTR.Null,
			out var nullSet));
		Assert.Equal(0u, nullSet.GadgetCount);
		Assert.False(MuiRequesterGadgetSetCore.TryGetButton(ref platform,
			nullSet, 0, out _));

		WriteCString(ref platform, Gadgets, "");
		Assert.True(MuiRequesterGadgetSetCore.TryParse(ref platform, Gadgets,
			out var emptySet));
		Assert.Equal(0u, emptySet.GadgetCount);
		Assert.False(MuiRequesterGadgetSetCore.TryGetButton(ref platform,
			emptySet, 0, out _));
	}

	[Fact]
	public void GadgetSetPreservesUppercaseControlChar()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		WriteCString(ref platform, Gadgets, "_Xray");

		Assert.True(MuiRequesterGadgetSetCore.TryParse(ref platform, Gadgets,
			out var set));
		Assert.True(MuiRequesterGadgetSetCore.TryGetButton(ref platform, set, 0,
			out var button));
		Assert.Equal((uint)'X', button.ControlChar);
	}

	private static void WriteCString(ref MuiHeadlessTestPlatform platform,
		APTR address, string value)
	{
		for (var index = 0; index < value.Length; index++)
			platform.WriteUInt8(address, index, (byte)value[index]);
		platform.WriteUInt8(address, value.Length, 0);
	}
}
