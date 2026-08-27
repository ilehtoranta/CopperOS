using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiHeadlessStateAdmissionTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);

	[Fact]
	public void HeadlessStateAdmissionRequiresCanonicalHeaderAndDepth()
	{
		var valid = new MuiHeadlessStateRecord
		{
			Magic = MuiHeadlessLayout.Magic,
			Version = MuiHeadlessLayout.Version,
			NextSequence = 1,
			NotifyDepth = MuiHeadlessLayout.MaximumNotificationDepth,
		};
		Assert.True(MuiHeadlessStateAdmission.Validate(valid));
		valid.Magic = 0;
		Assert.False(MuiHeadlessStateAdmission.Validate(valid));
		valid.Magic = MuiHeadlessLayout.Magic;
		valid.NotifyDepth = MuiHeadlessLayout.MaximumNotificationDepth + 1;
		Assert.False(MuiHeadlessStateAdmission.Validate(valid));
		valid.NotifyDepth = 0;
		valid.NextSequence = 0;
		Assert.True(MuiHeadlessStateAdmission.Validate(valid));
	}

	[Fact]
	public void MalformedHeadlessStateFailsClosedBeforeConsumers()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		Assert.True(MuiHeadlessMemory.Initialize(ref platform, State));
		Assert.True(MuiHeadlessStateFieldCursorCodec.TryWrite(ref platform, State,
			MuiHeadlessStateField.NotifyDepth,
			MuiHeadlessLayout.MaximumNotificationDepth + 1));
		Assert.True(MuiHeadlessStateCodec.TryReadStructural(ref platform, State,
			out var structural));
		Assert.False(MuiHeadlessStateAdmission.Validate(structural));
		Assert.False(MuiHeadlessStateCodec.TryRead(ref platform, State, out _));
		Assert.Equal(0u, MuiHeadlessMemory.NextSequence(ref platform, State));
		MuiHeadlessMemory.Mutated(ref platform, State);
		Assert.True(MuiHeadlessStateFieldCursorCodec.TryRead(ref platform, State,
			MuiHeadlessStateField.NotifyDepth, out var preserved));
		Assert.Equal(MuiHeadlessLayout.MaximumNotificationDepth + 1, preserved);
	}
}
