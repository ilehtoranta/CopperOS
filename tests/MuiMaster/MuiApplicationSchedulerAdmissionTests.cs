using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiApplicationSchedulerAdmissionTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);

	[Fact]
	public void ApplicationSchedulerAdmissionAcceptsCanonicalEmptyQueues()
	{
		var platform = CreateApplication(out var application);
		Assert.True(MuiApplicationWindowCore.InitializeApplication(ref platform,
			State, application, 0x20));
		Assert.True(MuiApplicationWindowCore.TryGetApplicationSchedulerState(
			ref platform, State, application, out var valid));
		Assert.True(MuiApplicationSchedulerStateAdmission.Validate(ref platform,
			valid));
		Assert.True(MuiApplicationSchedulerStateAdmission.ValidateLive(ref platform,
			State, application, valid));

		valid.SignalMask = 0xFFFFFFFF;
		Assert.True(MuiApplicationSchedulerStateAdmission.Validate(ref platform,
			valid));
		valid.ReturnHead = APTR.FromPointer(0x1200);
		Assert.False(MuiApplicationSchedulerStateAdmission.Validate(ref platform,
			valid));
	}

	[Fact]
	public void MalformedApplicationSchedulerMagicRemainsStructuralButFailsClosed()
	{
		var platform = CreateApplication(out var application);
		Assert.True(MuiApplicationWindowCore.InitializeApplication(ref platform,
			State, application, 0));
		var block = MuiStoreCore.DataspaceFind(ref platform, State, application,
			MuiApplicationWindowCore.ApplicationSchedulerStateKey);
		Assert.True(MuiApplicationSchedulerStateFieldCursorCodec.TryWriteUInt32(
			ref platform, block, MuiApplicationSchedulerStateField.Magic, 0));

		Assert.True(MuiApplicationSchedulerStateRecordCodec.TryReadStructural(
			ref platform, block, out var structural));
		Assert.Equal(0u, structural.Magic);
		Assert.False(MuiApplicationSchedulerStateRecordCodec.TryRead(ref platform,
			block, out _));
		Assert.False(MuiApplicationWindowCore.TryGetApplicationSchedulerState(
			ref platform, State, application, out _));
		Assert.False(MuiApplicationWindowCore.ReturnId(ref platform, State,
			application, 7));
		Assert.Equal(block, MuiStoreCore.DataspaceFind(ref platform, State,
			application, MuiApplicationWindowCore.ApplicationSchedulerStateKey));
	}

	[Fact]
	public void ApplicationSchedulerRecordUsesDedicatedStructMemoryAdapter()
	{
		var platform = CreateApplication(out _);
		var address = APTR.FromPointer(0x1D00);
		var record = new MuiApplicationSchedulerStateRecord
		{
			Magic = MuiApplicationSchedulerStateRecord.Cookie,
			ReturnHead = APTR.Null,
			ReturnTail = APTR.Null,
			InputHandlers = APTR.Null,
			SignalMask = 0x20,
			PushHead = APTR.Null,
			PushTail = APTR.Null,
		};
		Assert.True(MuiApplicationSchedulerStateRecordCodec.Write(ref platform,
			address, record));
		Assert.True(MuiApplicationSchedulerStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiApplicationSchedulerStateField.SignalMask,
			out var signalAddress));
		Assert.Equal(0x1D10u, signalAddress.Raw);
		Assert.True(MuiApplicationSchedulerStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, MuiApplicationSchedulerStateField.SignalMask,
			out var signalMask));
		Assert.Equal(0x20u, signalMask);
		Assert.True(MuiApplicationSchedulerStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiApplicationSchedulerStateField.SignalMask,
			0x40));
		Assert.True(MuiApplicationSchedulerStateRecordCodec.TryReadStructural(
			ref platform, address, out var updated));
		Assert.Equal(0x40u, updated.SignalMask);
		Assert.False(MuiApplicationSchedulerStateRecordMemoryCodec.TryGetAddress(
			ref platform, address,
			(MuiApplicationSchedulerStateField)255, out _));
		Assert.False(MuiApplicationSchedulerStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiApplicationSchedulerStateField.Magic,
			out _));
		Assert.False(MuiApplicationSchedulerStateRecordCodec.TryReadStructural(
			ref platform, APTR.Null, out _));
	}

	private static MuiHeadlessTestPlatform CreateApplication(out APTR application)
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var name = APTR.FromPointer(0x1100);
		platform.WriteCString(name, "Application.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		var applicationClass = MuiHeadlessObjectCore.RegisterClass(ref platform,
			State, name, APTR.Null, 0, APTR.FromPointer(1), false);
		application = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			applicationClass, APTR.Null);
		return platform;
	}
}
