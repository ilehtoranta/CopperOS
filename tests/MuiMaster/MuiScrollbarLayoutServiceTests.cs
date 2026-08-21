using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiScrollbarLayoutServiceTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);
	private static readonly APTR Name = APTR.FromPointer(0x1100);
	private const uint Left = 0x8042BEC6;
	private const uint Top = 0x8042509B;
	private const uint Width = 0x8042B59C;
	private const uint Height = 0x80423237;

	[Fact]
	public void PublicLayoutServiceRoutesScrollbarCompositeGeometry()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		platform.WriteCString(Name, "Scrollbar.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		var classRecord = MuiHeadlessObjectCore.RegisterClass(ref platform, State,
			Name, APTR.Null, 0, APTR.FromPointer(1), false);
		var scrollbar = MuiCommonControlCore.CreateControl(ref platform, State,
			classRecord, APTR.Null);
		Assert.True(scrollbar.IsNotNull);
		Assert.True(MuiFamilyCore.GetChild(ref platform, State, scrollbar, 0,
			APTR.Null).IsNotNull);
		Assert.True(MuiFamilyCore.GetChild(ref platform, State, scrollbar, 1,
			APTR.Null).IsNotNull);
		Assert.True(MuiFamilyCore.GetChild(ref platform, State, scrollbar, 2,
			APTR.Null).IsNotNull);

		Assert.True(MuiLayoutServiceCore.Layout(ref platform, State, scrollbar,
			10, 20, 100, 80, 0));
		var first = MuiFamilyCore.GetChild(ref platform, State, scrollbar, 0,
			APTR.Null);
		var prop = MuiFamilyCore.GetChild(ref platform, State, scrollbar, 1,
			APTR.Null);
		var second = MuiFamilyCore.GetChild(ref platform, State, scrollbar, 2,
			APTR.Null);
		Assert.Equal(10u, Get(ref platform, first, Left));
		Assert.Equal(20u, Get(ref platform, first, Top));
		Assert.Equal(100u, Get(ref platform, first, Width));
		Assert.Equal(16u, Get(ref platform, first, Height));
		Assert.Equal(10u, Get(ref platform, prop, Left));
		Assert.Equal(36u, Get(ref platform, prop, Top));
		Assert.Equal(100u, Get(ref platform, prop, Width));
		Assert.Equal(48u, Get(ref platform, prop, Height));
		Assert.Equal(10u, Get(ref platform, second, Left));
		Assert.Equal(84u, Get(ref platform, second, Top));
		Assert.Equal(100u, Get(ref platform, second, Width));
		Assert.Equal(16u, Get(ref platform, second, Height));
	}

	private static uint Get(ref MuiHeadlessTestPlatform platform, APTR obj,
		uint attribute)
	{
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, obj,
			attribute, out var value));
		return value;
	}
}
