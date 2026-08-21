using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiObjectMetadataTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);
	private const uint ObjectId = 0x8042D76E;
	private const uint Parent = 0x8042E35F;
	private const uint UserData = 0x80420313;
	private const uint Revision = 0x80427EAA;
	private const uint Version = 0x80422301;

	[Fact]
	public void GenericObjectMetadataUsesNamedRecordThroughCommonOmGet()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			State);
		var groupName = APTR.FromPointer(0x1100);
		var areaName = APTR.FromPointer(0x1140);
		platform.WriteCString(groupName, "Group.mui");
		platform.WriteCString(areaName, "Area.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		var groupClass = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			State, groupName, APTR.Null, 0, APTR.FromPointer(1), 20, 42);
		var areaClass = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			State, areaName, APTR.Null, 0, APTR.FromPointer(1), 6, 7);
		Assert.True(MuiHeadlessClassCodec.TryRead(ref platform, areaClass,
			out var areaClassRecord));
		Assert.NotEqual(0u, areaClassRecord.Flags & 8u);
		var group = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			groupClass, APTR.Null);
		var child = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			areaClass, APTR.Null);
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, group, child));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, child,
			ObjectId, 0xCAFE, false));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, child,
			UserData, 0xBEEF, false));

		Assert.Equal(group.Raw, Get(ref platform, child, Parent));
		Assert.Equal(0xCAFEu, Get(ref platform, child, ObjectId));
		Assert.Equal(0xBEEFu, Get(ref platform, child, UserData));
		Assert.Equal(6u, Get(ref platform, child, Version));
		Assert.Equal(7u, Get(ref platform, child, Revision));

		// Parent is a relationship in the named object record, not a replaceable
		// raw public attribute slot.
		var childRecord = MuiHeadlessObjectCore.FindObject(ref platform, State,
			child);
		Assert.True(MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, State,
			childRecord, Parent, 0x1234, false));
		Assert.True(MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, State,
			childRecord, Version, 99, false));
		Assert.True(MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, State,
			childRecord, Revision, 99, false));
		Assert.Equal(group.Raw, Get(ref platform, child, Parent));
		Assert.Equal(6u, Get(ref platform, child, Version));
		Assert.Equal(7u, Get(ref platform, child, Revision));

		var message = APTR.FromPointer(0x1800);
		var storage = APTR.FromPointer(0x1900);
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform, message,
			MuiCommonPacketKind.Get, MuiCommonField.MethodId,
			MuiCommonControlPacketCore.OmGet));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform, message,
			MuiCommonPacketKind.Get, MuiCommonField.Storage, storage.Raw));
		foreach (var attribute in new[] { Parent, ObjectId, UserData, Version,
			Revision })
		{
			Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
				message, MuiCommonPacketKind.Get, MuiCommonField.Attribute,
				attribute));
			Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform,
				State, child, message));
			Assert.True(MuiGuestUlongStorageCodec.TryRead(ref platform, storage,
				out var stored));
			var expected = attribute == Parent ? group.Raw :
				attribute == ObjectId ? 0xCAFEu :
				attribute == UserData ? 0xBEEFu :
				attribute == Version ? 6u : 7u;
			Assert.Equal(expected, stored.Value);
		}
	}

	private static uint Get(ref MuiHeadlessTestPlatform platform, APTR obj,
		uint attribute)
	{
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, obj,
			attribute, out var value));
		return value;
	}
}
