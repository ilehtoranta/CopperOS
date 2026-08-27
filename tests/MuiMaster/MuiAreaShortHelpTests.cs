using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiAreaShortHelpTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);

	[Fact]
	public void StateRecordUsesNamedPointerAndGenerationFields()
	{
		var platform = CreatePlatform(out _);
		var address = APTR.FromPointer(0x1500);
		var expected = default(MuiAreaShortHelpStateRecord);
		expected.Magic = MuiAreaShortHelpStateRecord.Cookie;
		expected.Text = APTR.FromPointer(0x1800);
		expected.Generation = 5;

		Assert.True(MuiAreaShortHelpStateRecordCodec.Write(ref platform, address,
			expected));
		Assert.True(MuiAreaShortHelpStateRecordCodec.TryRead(ref platform, address,
			out var actual));
		Assert.Equal(expected.Magic, actual.Magic);
		Assert.Equal(expected.Text, actual.Text);
		Assert.Equal(expected.Generation, actual.Generation);

		var cursor = default(MuiAreaShortHelpStateFieldCursor);
		cursor.Record = address;
		cursor.Field = MuiAreaShortHelpStateField.Text;
		Assert.True(MuiAreaShortHelpStateFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out var textAddress));
		Assert.Equal(address.Raw + 4, textAddress.Raw);
		Assert.False(MuiAreaShortHelpStateRecordCodec.TryRead(ref platform,
			APTR.FromPointer(0x20FFFu), out _));
	}

	[Fact]
	public void StateRecordUsesDedicatedStructCodec()
	{
		var platform = CreatePlatform(out _);
		var address = APTR.FromPointer(0x1540);
		var value = default(MuiAreaShortHelpStateRecord);
		value.Magic = MuiAreaShortHelpStateRecord.Cookie;
		value.Text = APTR.FromPointer(0x1A00);
		value.Generation = 7;

		Assert.True(MuiAreaShortHelpStateRecordCodec.Write(ref platform, address,
			value));
		Assert.True(MuiAreaShortHelpStateRecordCodec.TryReadStructural(
			ref platform, address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.Text, decoded.Text);
		Assert.Equal(value.Generation, decoded.Generation);
		Assert.True(MuiAreaShortHelpStateRecordCodec.TryRead(ref platform, address,
			out decoded));
		Assert.False(MuiAreaShortHelpStateRecordCodec.TryReadStructural(
			ref platform, APTR.Null, out _));
	}

	[Fact]
	public void ShortHelpAdmissionRequiresGenerationAndLiveOwner()
	{
		var platform = CreatePlatform(out var areaClass);
		var obj = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			areaClass, APTR.Null);
		var valid = new MuiAreaShortHelpStateRecord
		{
			Magic = MuiAreaShortHelpStateRecord.Cookie,
			Text = APTR.FromPointer(0x1800),
			Generation = 1,
		};
		Assert.True(MuiAreaShortHelpStateAdmission.Validate(valid));
		Assert.True(MuiAreaShortHelpStateAdmission.ValidateLive(ref platform,
			State, obj, valid));
		var malformed = valid;
		malformed.Generation = 0;
		Assert.False(MuiAreaShortHelpStateAdmission.Validate(malformed));
		Assert.False(MuiAreaShortHelpStateAdmission.ValidateLive(ref platform,
			State, obj, malformed));
		Assert.False(MuiAreaShortHelpStateAdmission.ValidateLive(ref platform,
			State, APTR.FromPointer(0xDEAD), valid));
	}

	[Fact]
	public void MalformedShortHelpFailsClosedBeforeRawRepairOrProviderCalls()
	{
		var platform = CreatePlatform(out var areaClass);
		var obj = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			areaClass, APTR.Null);
		var help = APTR.FromPointer(0x1800);
		Assert.True(MuiAreaShortHelpPacketCore.Set(ref platform, State, obj, help));
		var block = MuiStoreCore.DataspaceFind(ref platform, State, obj,
			MuiAreaShortHelpCore.StateKey);
		Assert.True(block.IsNotNull);
		Assert.True(MuiAreaShortHelpStateFieldCursorCodec.TryWriteUInt32(
			ref platform, block, MuiAreaShortHelpStateField.Generation, 0));
		Assert.True(MuiAreaShortHelpStateRecordCodec.TryReadStructural(ref platform,
			block, out var structural));
		Assert.Equal(0u, structural.Generation);
		Assert.False(MuiAreaShortHelpStateAdmission.Validate(structural));
		Assert.False(MuiAreaShortHelpStateRecordCodec.TryRead(ref platform, block,
			out _));
		var allocationsBefore = platform.AllocationCount;
		Assert.False(MuiAreaShortHelpPacketCore.TryGet(ref platform, State, obj,
			out _));
		Assert.Equal(allocationsBefore, platform.AllocationCount);
		platform.ShortHelpCheckSampleAvailable = true;
		Assert.Equal(APTR.Null, MuiAreaShortHelpPacketCore.Check(ref platform,
			State, obj, help, 1, 2));
		Assert.Equal(APTR.Null, platform.LastShortHelpCheckObject);
		Assert.False(MuiAreaShortHelpPacketCore.Set(ref platform, State, obj,
			APTR.Null));
		Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State, obj,
			MuiCommonControlCore.ShortHelp, out var raw));
		Assert.Equal(help.Raw, raw);
		Assert.Equal(block, MuiStoreCore.DataspaceFind(ref platform, State, obj,
			MuiAreaShortHelpCore.StateKey));
	}

	[Fact]
	public void TypedShortHelpPointerRoundTripsAndClears()
	{
		var platform = CreatePlatform(out var areaClass);
		var obj = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			areaClass, APTR.Null);
		var help = APTR.FromPointer(0x1800);

		Assert.True(MuiAreaShortHelpPacketCore.Set(ref platform, State, obj, help));
		Assert.True(MuiAreaShortHelpPacketCore.TryGet(ref platform, State, obj,
			out var value));
		Assert.Equal(help, value.Text);
		Assert.True(MuiAreaShortHelpPacketCore.Set(ref platform, State, obj,
			APTR.Null));
		Assert.True(MuiAreaShortHelpPacketCore.TryGet(ref platform, State, obj,
			out value));
		Assert.Equal(APTR.Null, value.Text);
	}

	[Fact]
	public void GenericSetAndGetUseShortHelpState()
	{
		var platform = CreatePlatform(out var areaClass);
		var obj = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			areaClass, APTR.Null);
		var help = APTR.FromPointer(0x1900);

		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, obj,
			MuiCommonControlCore.ShortHelp, help.Raw, false));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, obj,
			MuiCommonControlCore.ShortHelp, out var raw));
		Assert.Equal(help.Raw, raw);
		Assert.True(MuiCommonControlCore.TryGet(ref platform, State, obj,
			MuiCommonControlCore.ShortHelp, out raw, out var handled));
		Assert.True(handled);
		Assert.Equal(help.Raw, raw);
	}

	[Fact]
	public void DispatcherSetAndOmGetUseShortHelpPointer()
	{
		var platform = CreatePlatform(out var areaClass);
		var obj = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			areaClass, APTR.Null);
		var packet = APTR.FromPointer(0x1300);
		var help = APTR.FromPointer(0x1A00);
		platform.WriteUInt32(packet, 0, MuiCommonControlPacketCore.Set);
		platform.WriteUInt32(packet, 4, MuiCommonControlCore.ShortHelp);
		platform.WriteUInt32(packet, 8, help.Raw);
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			obj, packet));

		var storage = APTR.FromPointer(0x1400);
		platform.WriteUInt32(packet, 0, MuiCommonControlPacketCore.OmGet);
		platform.WriteUInt32(packet, 4, MuiCommonControlCore.ShortHelp);
		platform.WriteUInt32(packet, 8, storage.Raw);
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			obj, packet));
		Assert.Equal(help.Raw, platform.ReadUInt32(storage, 0));
	}

	[Fact]
	public void ShortHelpMethodPacketsUseNamedFields()
	{
		var platform = CreatePlatform(out _);
		var create = APTR.FromPointer(0x1B00);
		platform.WriteUInt32(create, 0, MuiAreaShortHelpMessageCodec.CreateShortHelp);
		platform.WriteUInt32(create, 4, unchecked((uint)-12));
		platform.WriteUInt32(create, 8, 34);
		Assert.True(MuiAreaShortHelpMessageCodec.TryReadCreate(ref platform, create,
			out var createPacket));
		Assert.Equal(-12, createPacket.MouseX);
		Assert.Equal(34, createPacket.MouseY);

		var delete = APTR.FromPointer(0x1C00);
		platform.WriteUInt32(delete, 0, MuiAreaShortHelpMessageCodec.DeleteShortHelp);
		platform.WriteUInt32(delete, 4, 0x1D00);
		Assert.True(MuiAreaShortHelpMessageCodec.TryReadDelete(ref platform, delete,
			out var deletePacket));
		Assert.Equal(APTR.FromPointer(0x1D00), deletePacket.Help);
		Assert.False(MuiAreaShortHelpMessageCodec.TryReadCreate(ref platform,
			APTR.FromPointer(0x20FFFu), out _));
	}

	[Fact]
	public void ShortHelpMethodHeaderUsesNamedField()
	{
		var platform = CreatePlatform(out _);
		var packet = APTR.FromPointer(0x1B80);
		platform.WriteUInt32(packet, 0, MuiAreaShortHelpMessageCodec.CreateShortHelp);
		Assert.True(MuiAreaShortHelpMessageCodec.TryReadMethodIdValue(ref platform,
			packet, MuiAreaShortHelpPacketKind.Create, out var methodId));
		Assert.Equal(MuiAreaShortHelpMessageCodec.CreateShortHelp, methodId);

		platform.WriteUInt32(packet, 0, MuiAreaShortHelpMessageCodec.DeleteShortHelp);
		Assert.True(MuiAreaShortHelpMessageCodec.TryReadMethodId(ref platform,
			packet, MuiAreaShortHelpPacketKind.Delete, out var named));
		Assert.Equal(MuiAreaShortHelpMessageCodec.DeleteShortHelp, named.MethodId);
		Assert.False(MuiAreaShortHelpMessageCodec.TryReadMethodIdValue(ref platform,
			APTR.FromPointer(0x20FFFu), MuiAreaShortHelpPacketKind.Check, out _));
	}

	[Fact]
	public void CheckShortHelpPacketUsesNamedHandleAndCoordinates()
	{
		var platform = CreatePlatform(out _);
		var check = APTR.FromPointer(0x1F00);
		platform.WriteUInt32(check, 0, MuiAreaShortHelpMessageCodec.CheckShortHelp);
		platform.WriteUInt32(check, 4, 0x1F80);
		platform.WriteUInt32(check, 8, unchecked((uint)-23));
		platform.WriteUInt32(check, 12, 47);

		Assert.True(MuiAreaShortHelpMessageCodec.TryReadCheck(ref platform, check,
			out var packet));
		Assert.Equal(APTR.FromPointer(0x1F80), packet.Help);
		Assert.Equal(-23, packet.MouseX);
		Assert.Equal(47, packet.MouseY);
		Assert.False(MuiAreaShortHelpMessageCodec.TryReadCheck(ref platform,
			APTR.FromPointer(0x20FFFu), out _));
	}

	[Fact]
	public void DispatcherCreateAndDeleteShortHelpRemainCallerOwned()
	{
		var platform = CreatePlatform(out var areaClass);
		var obj = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			areaClass, APTR.Null);
		var help = APTR.FromPointer(0x1E00);
		Assert.True(MuiAreaShortHelpPacketCore.Set(ref platform, State, obj, help));

		var create = APTR.FromPointer(0x1300);
		platform.WriteUInt32(create, 0, MuiAreaShortHelpMessageCodec.CreateShortHelp);
		platform.WriteUInt32(create, 4, 10);
		platform.WriteUInt32(create, 8, 20);
		Assert.Equal(help.Raw, MuiCommonControlDispatcher.Dispatch(ref platform,
			State, obj, create));

		var delete = APTR.FromPointer(0x1400);
		platform.WriteUInt32(delete, 0, MuiAreaShortHelpMessageCodec.DeleteShortHelp);
		platform.WriteUInt32(delete, 4, help.Raw);
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			obj, delete));
		Assert.True(MuiAreaShortHelpPacketCore.TryGet(ref platform, State, obj,
			out var value));
		Assert.Equal(help, value.Text);
	}

	[Fact]
	public void DispatcherCreateAndDeleteShortHelpUseNamedPlatformCapability()
	{
		var platform = CreatePlatform(out var areaClass);
		var obj = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			areaClass, APTR.Null);
		var staticHelp = APTR.FromPointer(0x1E40);
		var dynamicHelp = APTR.FromPointer(0x1E60);
		Assert.True(MuiAreaShortHelpPacketCore.Set(ref platform, State, obj,
			staticHelp));
		platform.ShortHelpCreateSampleAvailable = true;
		platform.ShortHelpCreateResult = dynamicHelp;

		var create = APTR.FromPointer(0x1300);
		platform.WriteUInt32(create, 0,
			MuiAreaShortHelpMessageCodec.CreateShortHelp);
		platform.WriteUInt32(create, 4, unchecked((uint)-12));
		platform.WriteUInt32(create, 8, 27);
		Assert.Equal(dynamicHelp.Raw, MuiCommonControlDispatcher.Dispatch(ref
			platform, State, obj, create));
		Assert.Equal(obj, platform.LastShortHelpCreateObject);
		Assert.Equal(staticHelp, platform.LastShortHelpCreateCurrent);
		Assert.Equal(-12, platform.LastShortHelpCreateMouseX);
		Assert.Equal(27, platform.LastShortHelpCreateMouseY);

		platform.ShortHelpDeleteSampleAvailable = true;
		var delete = APTR.FromPointer(0x1400);
		platform.WriteUInt32(delete, 0,
			MuiAreaShortHelpMessageCodec.DeleteShortHelp);
		platform.WriteUInt32(delete, 4, dynamicHelp.Raw);
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			obj, delete));
		Assert.Equal(dynamicHelp, platform.LastShortHelpDeleted);
	}

	[Fact]
	public void DispatcherCheckShortHelpUsesNamedPlatformCapability()
	{
		var platform = CreatePlatform(out var areaClass);
		var obj = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			areaClass, APTR.Null);
		var staticHelp = APTR.FromPointer(0x1EA0);
		var previousHelp = APTR.FromPointer(0x1EC0);
		var dynamicHelp = APTR.FromPointer(0x1EE0);
		Assert.True(MuiAreaShortHelpPacketCore.Set(ref platform, State, obj,
			staticHelp));
		platform.ShortHelpCheckSampleAvailable = true;
		platform.ShortHelpCheckResult = dynamicHelp;

		var check = APTR.FromPointer(0x1300);
		Assert.True(MuiAreaShortHelpMessageCodec.WriteCheck(ref platform, check,
			previousHelp, -18, 29));
		Assert.Equal(dynamicHelp.Raw, MuiCommonControlDispatcher.Dispatch(ref
			platform, State, obj, check));
		Assert.Equal(obj, platform.LastShortHelpCheckObject);
		Assert.Equal(previousHelp, platform.LastShortHelpCheckCurrent);
		Assert.Equal(-18, platform.LastShortHelpCheckMouseX);
		Assert.Equal(29, platform.LastShortHelpCheckMouseY);
	}

	[Fact]
	public void DispatcherCheckShortHelpRejectsProviderIdentityMutation()
	{
		var platform = CreatePlatform(out var areaClass);
		var obj = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			areaClass, APTR.Null);
		Assert.True(MuiAreaShortHelpPacketCore.Set(ref platform, State, obj,
			APTR.FromPointer(0x1F00)));
		platform.ShortHelpCheckSampleAvailable = true;
		platform.ShortHelpCheckSampleMutatesIdentity = true;

		var check = APTR.FromPointer(0x1300);
		Assert.True(MuiAreaShortHelpMessageCodec.WriteCheck(ref platform, check,
			APTR.FromPointer(0x1F20), 4, 5));
		Assert.Equal(0u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			obj, check));
	}

	[Fact]
	public void DispatcherCheckShortHelpReturnsCallerOwnedPointer()
	{
		var platform = CreatePlatform(out var areaClass);
		var obj = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			areaClass, APTR.Null);
		var help = APTR.FromPointer(0x1E80);
		Assert.True(MuiAreaShortHelpPacketCore.Set(ref platform, State, obj, help));

		var check = APTR.FromPointer(0x1300);
		platform.WriteUInt32(check, 0, MuiAreaShortHelpMessageCodec.CheckShortHelp);
		platform.WriteUInt32(check, 4, 0x1E80);
		platform.WriteUInt32(check, 8, unchecked((uint)-7));
		platform.WriteUInt32(check, 12, 19);
		Assert.Equal(help.Raw, MuiCommonControlDispatcher.Dispatch(ref platform,
			State, obj, check));

		Assert.True(MuiAreaShortHelpPacketCore.Set(ref platform, State, obj,
			APTR.Null));
		Assert.Equal(0u, MuiCommonControlDispatcher.Dispatch(ref platform,
			State, obj, check));
	}

	private static MuiHeadlessTestPlatform CreatePlatform(out APTR areaClass)
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var name = APTR.FromPointer(0x1100);
		platform.WriteCString(name, "Text.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		areaClass = MuiHeadlessObjectCore.RegisterClass(ref platform, State, name,
			APTR.Null, 0, APTR.FromPointer(1), false);
		return platform;
	}
}
