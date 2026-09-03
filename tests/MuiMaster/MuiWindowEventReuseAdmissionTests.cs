using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiWindowEventReuseAdmissionTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);

	[Fact]
	public void WindowEventReuseAdmissionRequiresCanonicalContextAndOwner()
	{
		var platform = CreateWindow(out var window);
		var eventMessage = APTR.FromPointer(0x1200);
		Assert.Equal(0u, MuiApplicationWindowCore.DispatchWindowEvent(
			ref platform, State, window, eventMessage, 4));
		var block = MuiStoreCore.DataspaceFind(ref platform, State, window,
			MuiApplicationWindowCore.WindowEventReuseStateKey);
		Assert.True(MuiWindowEventReuseStateRecordCodec.TryReadStructural(
			ref platform, block, out var valid));
		Assert.True(MuiWindowEventReuseStateAdmission.Validate(ref platform,
			valid));
		Assert.True(MuiWindowEventReuseStateAdmission.ValidateLive(ref platform,
			State, window, valid));
		valid.ContextActive = 2;
		Assert.False(MuiWindowEventReuseStateAdmission.Validate(ref platform,
			valid));
		Assert.False(MuiWindowEventReuseStateAdmission.ValidateLive(ref platform,
			State, window, valid));
		valid.ContextActive = 0;
		Assert.False(MuiWindowEventReuseStateAdmission.ValidateLive(ref platform,
			State, APTR.FromPointer(0xDEAD), valid));
	}

	[Fact]
	public void MalformedWindowEventReuseMagicFailsClosedButRemainsStructural()
	{
		var platform = CreateWindow(out var window);
		var eventMessage = APTR.FromPointer(0x1200);
		Assert.Equal(0u, MuiApplicationWindowCore.DispatchWindowEvent(
			ref platform, State, window, eventMessage, 4));
		var block = MuiStoreCore.DataspaceFind(ref platform, State, window,
			MuiApplicationWindowCore.WindowEventReuseStateKey);
		Assert.True(MuiWindowEventReuseStateFieldCursorCodec.TryWriteUInt32(
			ref platform, block, MuiWindowEventReuseStateField.Magic, 0));
		Assert.True(MuiWindowEventReuseStateRecordCodec.TryReadStructural(
			ref platform, block, out var structural));
		Assert.Equal(0u, structural.Magic);
		Assert.False(MuiWindowEventReuseStateRecordCodec.TryRead(ref platform,
			block, out _));
		Assert.Equal(0u, MuiApplicationWindowCore.DispatchWindowEvent(
			ref platform, State, window, eventMessage, 4));
		Assert.Equal(block, MuiStoreCore.DataspaceFind(ref platform, State, window,
			MuiApplicationWindowCore.WindowEventReuseStateKey));
	}

	[Fact]
	public void WindowEventReuseRecordUsesDedicatedStructMemoryAdapter()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var address = APTR.FromPointer(0x1600);
		var value = new MuiWindowEventReuseStateRecord
		{
			Magic = MuiWindowEventReuseStateRecord.Cookie,
			ContextActive = 0,
			Pending = 0,
			EventMessage = APTR.Null,
			InputEvent = APTR.Null,
			EventClass = 0,
			MuiKey = -7,
		};
		Assert.True(MuiWindowEventReuseStateRecordCodec.Write(ref platform, address,
			value));
		Assert.True(MuiWindowEventReuseStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiWindowEventReuseStateField.MuiKey,
			out var keyField) && keyField.Raw == 0x1618u);
		Assert.True(MuiWindowEventReuseStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, MuiWindowEventReuseStateField.MuiKey,
			out var key) && key == unchecked((uint)-7));
		Assert.True(MuiWindowEventReuseStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiWindowEventReuseStateField.MuiKey,
			unchecked((uint)-8)));
		Assert.True(MuiWindowEventReuseStateRecordCodec.TryReadStructural(ref platform,
			address, out var decoded) && decoded.Magic == value.Magic &&
			decoded.ContextActive == value.ContextActive && decoded.Pending == value.Pending &&
			decoded.EventMessage == value.EventMessage && decoded.InputEvent == value.InputEvent &&
			decoded.EventClass == value.EventClass && decoded.MuiKey == -8);
		Assert.True(MuiWindowEventReuseStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, MuiWindowEventReuseStateField.Magic,
			out var magic) && magic == value.Magic);
		Assert.False(MuiWindowEventReuseStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, (MuiWindowEventReuseStateField)255, out _));
		Assert.False(MuiWindowEventReuseStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, (MuiWindowEventReuseStateField)255, out _));
		Assert.False(MuiWindowEventReuseStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiWindowEventReuseStateField.Magic, out _));
	}

	private static MuiHeadlessTestPlatform CreateWindow(out APTR window)
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var name = APTR.FromPointer(0x1100);
		platform.WriteCString(name, "Window.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		var windowClass = MuiHeadlessObjectCore.RegisterClass(ref platform, State,
			name, APTR.Null, 0, APTR.FromPointer(1), false);
		window = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			windowClass, APTR.Null);
		return platform;
	}
}
