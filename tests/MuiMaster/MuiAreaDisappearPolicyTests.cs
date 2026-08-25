using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiAreaDisappearPolicyTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);

	[Fact]
	public void CommonControlProjectsSignedDisappearPrioritiesThroughNamedState()
	{
		var platform = CreatePlatform(out var rectangleClass);
		var rectangle = MuiCommonControlCore.CreateControl(ref platform, State,
			rectangleClass, APTR.Null);

		Assert.True(MuiAreaDisappearPacketCore.TryGet(ref platform, State,
			rectangle, out var defaults));
		Assert.Equal(0, defaults.HorizDisappear);
		Assert.Equal(0, defaults.VertDisappear);

		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			rectangle, MuiCommonControlCore.HorizDisappear,
			unchecked((uint)-7), false));
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			rectangle, MuiCommonControlCore.VertDisappear,
			unchecked((uint)13), false));

		Assert.True(MuiAreaDisappearPacketCore.TryGet(ref platform, State,
			rectangle, out var value));
		Assert.Equal(-7, value.HorizDisappear);
		Assert.Equal(13, value.VertDisappear);
		Assert.True(MuiAreaDisappearCore.TryReadStateRecord(ref platform, State,
			rectangle, out var record));
		Assert.Equal(MuiAreaDisappearPolicyStateRecord.Cookie, record.Magic);
		Assert.Equal(-7, record.HorizDisappear);
		Assert.Equal(13, record.VertDisappear);
	}

	[Fact]
	public void TypedDisappearPacketUpdatesBothSignedFields()
	{
		var platform = CreatePlatform(out var rectangleClass);
		var rectangle = MuiCommonControlCore.CreateControl(ref platform, State,
			rectangleClass, APTR.Null);
		var input = new MuiAreaDisappearPolicyStateInput
		{
			HorizDisappear = 42,
			VertDisappear = -19,
		};

		Assert.True(MuiAreaDisappearPacketCore.Set(ref platform, State, rectangle,
			input));
		Assert.True(MuiAreaDisappearPacketCore.TryGet(ref platform, State,
			rectangle, out var value));
		Assert.Equal(input.HorizDisappear, value.HorizDisappear);
		Assert.Equal(input.VertDisappear, value.VertDisappear);
		Assert.True(MuiCommonControlCore.TryGet(ref platform, State, rectangle,
			MuiCommonControlCore.HorizDisappear, out var horizontal, out var handled));
		Assert.True(handled);
		Assert.Equal(unchecked((uint)input.HorizDisappear), horizontal);
	}

	private static MuiHeadlessTestPlatform CreatePlatform(out APTR rectangleClass)
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var name = APTR.FromPointer(0x1100);
		platform.WriteCString(name, "Rectangle.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		rectangleClass = MuiHeadlessObjectCore.RegisterClass(ref platform, State,
			name, APTR.Null, 0, APTR.FromPointer(1), false);
		return platform;
	}
}
