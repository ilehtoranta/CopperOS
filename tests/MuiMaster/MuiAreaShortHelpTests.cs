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
