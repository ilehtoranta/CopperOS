using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiWindowRelationshipAdmissionTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);

	[Fact]
	public void WindowRelationshipAdmissionRequiresMappedRelationshipsAndOwner()
	{
		var platform = CreateWindow(out var window);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			window, MuiWindowPublicCore.RootObject, out _));
		Assert.True(MuiWindowPublicCore.TryGetWindowRelationshipState(
			ref platform, State, window, out var valid));
		Assert.True(MuiWindowRelationshipStateAdmission.Validate(ref platform,
			valid));
		Assert.True(MuiWindowRelationshipStateAdmission.ValidateLive(ref platform,
			State, window, valid));
		valid.RefWindow = APTR.FromPointer(0x1200);
		Assert.True(MuiWindowRelationshipStateAdmission.Validate(ref platform,
			valid));
		Assert.False(MuiWindowRelationshipStateAdmission.ValidateLive(ref platform,
			State, window, valid));
		Assert.False(MuiWindowRelationshipStateAdmission.ValidateLive(ref platform,
			State, APTR.FromPointer(0xDEAD), valid));
	}

	[Fact]
	public void MalformedWindowRelationshipMagicFailsClosedButRemainsStructural()
	{
		var platform = CreateWindow(out var window);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			window, MuiWindowPublicCore.RootObject, out _));
		Assert.True(MuiWindowPublicCore.TryGetWindowRelationshipState(
			ref platform, State, window, out _));
		var block = MuiStoreCore.DataspaceFind(ref platform, State, window,
			MuiWindowPublicCore.WindowRelationshipStateKey);
		Assert.True(MuiWindowRelationshipStateFieldCursorCodec.TryWriteUInt32(
			ref platform, block, MuiWindowRelationshipStateField.Magic, 0));
		Assert.True(MuiWindowRelationshipStateRecordCodec.TryReadStructural(
			ref platform, block, out var structural));
		Assert.Equal(0u, structural.Magic);
		Assert.False(MuiWindowRelationshipStateRecordCodec.TryRead(ref platform,
			block, out _));
		Assert.False(MuiWindowPublicCore.TryGetWindowRelationshipState(
			ref platform, State, window, out _));
		Assert.False(MuiHeadlessObjectCore.GetAttribute(ref platform, State, window,
			MuiWindowPublicCore.RootObject, out _));
		Assert.Equal(block, MuiStoreCore.DataspaceFind(ref platform, State, window,
			MuiWindowPublicCore.WindowRelationshipStateKey));
	}

	[Fact]
	public void WindowRelationshipRecordUsesDedicatedStructMemoryAdapter()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var address = APTR.FromPointer(0x16C0);
		var value = new MuiWindowRelationshipStateRecord
		{
			Magic = MuiWindowRelationshipStateRecord.Cookie,
			RootObject = APTR.Null,
			Menustrip = APTR.Null,
			RefWindow = APTR.Null,
		};
		Assert.True(MuiWindowRelationshipStateRecordCodec.Write(ref platform,
			address, value));
		Assert.True(MuiWindowRelationshipStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, 12, out var refWindow) && refWindow.Raw == 0x16CCu);
		Assert.True(MuiWindowRelationshipStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, 4, out var root) && root == 0);
		Assert.True(MuiWindowRelationshipStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, 0, MuiWindowRelationshipStateRecord.Cookie));
		Assert.True(MuiWindowRelationshipStateRecordCodec.TryReadStructural(ref platform,
			address, out var decoded) && decoded.Menustrip.IsNull);
		Assert.False(MuiWindowRelationshipStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiWindowRelationshipStateRecord.Size, out _));
		Assert.False(MuiWindowRelationshipStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, 0, out _));
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
