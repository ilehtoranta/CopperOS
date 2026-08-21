using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiErrorServiceTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);

	[Fact]
	public void ErrorStartsNeutralAndSetErrorReturnsPreviousValue()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		Assert.Equal(0, MuiErrorServiceCore.Error(ref platform, State));
		Assert.Equal(0, MuiErrorServiceCore.SetError(ref platform, State, 7));
		Assert.True(MuiErrorServiceCore.Initialize(ref platform, State));
		Assert.Equal(0, MuiErrorServiceCore.Error(ref platform, State));
		Assert.Equal(0, MuiErrorServiceCore.SetError(ref platform, State, 7));
		Assert.Equal(7, MuiErrorServiceCore.Error(ref platform, State));
		Assert.Equal(7, MuiErrorServiceCore.SetError(ref platform, State, 3));
		Assert.Equal(3, MuiErrorServiceCore.Error(ref platform, State));
	}

	[Fact]
	public void ReinitializationPreservesTheCurrentError()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		Assert.True(MuiErrorServiceCore.Initialize(ref platform, State));
		Assert.Equal(0, MuiErrorServiceCore.SetError(ref platform, State, -2));
		Assert.True(MuiErrorServiceCore.Initialize(ref platform, State));
		Assert.Equal(-2, MuiErrorServiceCore.Error(ref platform, State));
		Assert.Equal(-2, MuiErrorServiceCore.SetError(ref platform, State, 0));
	}
}
