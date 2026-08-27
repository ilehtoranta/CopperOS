using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiAreaPresentationTimerAdmissionTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);

	[Fact]
	public void AreaPresentationAndTimerRecordsRoundTripThroughNamedStructs()
	{
		var platform = CreatePlatform();
		var presentationAddress = APTR.FromPointer(0x1500);
		var timerAddress = APTR.FromPointer(0x1540);
		var eventAddress = APTR.FromPointer(0x1580);
		var presentation = new MuiAreaPresentationStateRecord
		{
			Magic = MuiAreaPresentationStateRecord.Cookie,
			Disabled = 1,
			ShowMe = 0,
			Background = uint.MaxValue,
			Frame = 7,
			CustomBackfill = 1,
		};
		var timer = new MuiAreaTimerStateRecord
		{
			Magic = MuiAreaTimerStateRecord.Cookie,
			Value = -9,
			Generation = 3,
		};
		var timerEvent = new MuiAreaTimerEventStateRecord
		{
			Magic = MuiAreaTimerEventStateRecord.Cookie,
			Armed = 1,
			MouseOver = 1,
			DelayElapsed = 0,
			LastTick = 42,
			Generation = 5,
		};

		Assert.True(MuiAreaPresentationStateRecordCodec.Write(ref platform,
			presentationAddress, presentation));
		Assert.True(MuiAreaTimerStateRecordCodec.Write(ref platform, timerAddress,
			timer));
		Assert.True(MuiAreaTimerEventStateCodec.Write(ref platform, eventAddress,
			timerEvent));
		Assert.True(MuiAreaPresentationStateRecordCodec.TryRead(ref platform,
			presentationAddress, out var presentationRead));
		Assert.True(MuiAreaTimerStateRecordCodec.TryRead(ref platform, timerAddress,
			out var timerRead));
		Assert.True(MuiAreaTimerEventStateCodec.TryRead(ref platform, eventAddress,
			out var eventRead));
		Assert.Equal(presentation.Background, presentationRead.Background);
		Assert.Equal(presentation.CustomBackfill, presentationRead.CustomBackfill);
		Assert.Equal(timer.Value, timerRead.Value);
		Assert.Equal(timer.Generation, timerRead.Generation);
		Assert.Equal(timerEvent.LastTick, eventRead.LastTick);
		Assert.Equal(timerEvent.Generation, eventRead.Generation);
	}

	[Fact]
	public void AreaPresentationRecordUsesDedicatedStructMemoryAdapter()
	{
		var platform = CreatePlatform();
		var address = APTR.FromPointer(0x1A00);
		var value = new MuiAreaPresentationStateRecord
		{
			Magic = MuiAreaPresentationStateRecord.Cookie,
			Disabled = 1,
			ShowMe = 0,
			Background = uint.MaxValue,
			Frame = 7,
			CustomBackfill = 1,
		};

		Assert.True(MuiAreaPresentationStateRecordCodec.Write(ref platform,
			address, value));
		Assert.True(MuiAreaPresentationStateRecordCodec.TryReadStructural(
			ref platform, address, out var structural));
		Assert.Equal(value.Magic, structural.Magic);
		Assert.Equal(value.Disabled, structural.Disabled);
		Assert.Equal(value.ShowMe, structural.ShowMe);
		Assert.Equal(value.Background, structural.Background);
		Assert.Equal(value.Frame, structural.Frame);
		Assert.Equal(value.CustomBackfill, structural.CustomBackfill);
		Assert.True(MuiAreaPresentationStateRecordCodec.TryRead(ref platform,
			address, out _));
		Assert.True(MuiAreaPresentationStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiAreaPresentationStateField.CustomBackfill,
			out var lastField));
		Assert.Equal(address.Raw + 20, lastField.Raw);
		Assert.False(MuiAreaPresentationStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, (MuiAreaPresentationStateField)255, out _));
		Assert.False(MuiAreaPresentationStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiAreaPresentationStateField.Magic, out _));
		Assert.False(MuiAreaPresentationStateRecordCodec.TryReadStructural(
			ref platform, APTR.Null, out _));
	}

	[Fact]
	public void MalformedPresentationAndTimerMagicRemainStructuralButFailClosed()
	{
		var platform = CreatePlatform();
		var presentationAddress = APTR.FromPointer(0x1600);
		var timerAddress = APTR.FromPointer(0x1640);
		var eventAddress = APTR.FromPointer(0x1680);
		Assert.True(MuiAreaPresentationStateRecordCodec.Write(ref platform,
			presentationAddress, new MuiAreaPresentationStateRecord
			{
				Magic = MuiAreaPresentationStateRecord.Cookie,
				Disabled = 1,
				ShowMe = 1,
				CustomBackfill = 0,
			}));
		Assert.True(MuiAreaTimerStateRecordCodec.Write(ref platform, timerAddress,
			new MuiAreaTimerStateRecord
			{
				Magic = MuiAreaTimerStateRecord.Cookie,
				Value = -1,
				Generation = 1,
			}));
		Assert.True(MuiAreaTimerEventStateCodec.Write(ref platform, eventAddress,
			new MuiAreaTimerEventStateRecord
			{
				Magic = MuiAreaTimerEventStateRecord.Cookie,
				Armed = 0,
				MouseOver = 1,
				DelayElapsed = 1,
				LastTick = 9,
				Generation = 1,
			}));

		Assert.True(MuiAreaPresentationStateFieldCursorCodec.TryWriteUInt32(
			ref platform, presentationAddress,
			MuiAreaPresentationStateField.Magic, 0));
		Assert.True(MuiAreaTimerStateFieldCursorCodec.TryWriteUInt32(ref platform,
			timerAddress, MuiAreaTimerStateField.Magic, 0));
		Assert.True(MuiAreaTimerEventStateCodec.TryWriteUInt32(ref platform,
			eventAddress, MuiAreaTimerEventStateField.Magic, 0));

		Assert.True(MuiAreaPresentationStateRecordCodec.TryReadStructural(
			ref platform, presentationAddress, out var presentation));
		Assert.True(MuiAreaTimerStateRecordCodec.TryReadStructural(ref platform,
			timerAddress, out var timer));
		Assert.True(MuiAreaTimerEventStateCodec.TryReadStructural(ref platform,
			eventAddress, out var timerEvent));
		Assert.Equal(0u, presentation.Magic);
		Assert.Equal(0u, timer.Magic);
		Assert.Equal(0u, timerEvent.Magic);
		Assert.False(MuiAreaPresentationStateRecordCodec.TryRead(ref platform,
			presentationAddress, out _));
		Assert.False(MuiAreaTimerStateRecordCodec.TryRead(ref platform, timerAddress,
			out _));
		Assert.False(MuiAreaTimerEventStateCodec.TryRead(ref platform, eventAddress,
			out _));
		Assert.False(MuiAreaPresentationStateAdmission.Validate(presentation));
		Assert.False(MuiAreaTimerStateAdmission.Validate(timer));
		Assert.False(MuiAreaTimerEventStateAdmission.Validate(timerEvent));
	}

	private static MuiHeadlessTestPlatform CreatePlatform() =>
		new(0x1000, 0x20000, 0x4000, State);
}
