using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiFamilyGetterTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);
	private const uint FamilyChildCount = 0x8042B25A;
	private const uint FamilyList = 0x80424B9E;

	[Fact]
	public void FamilyGettersUseNamedChildProjectionThroughCommonOmGet()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			State);
		var familyName = APTR.FromPointer(0x1100);
		var areaName = APTR.FromPointer(0x1140);
		platform.WriteCString(familyName, "Family.mui");
		platform.WriteCString(areaName, "Area.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		var familyClass = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			State, familyName, APTR.Null, 0, APTR.FromPointer(1));
		var areaClass = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			State, areaName, APTR.Null, 0, APTR.FromPointer(1));
		var family = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			familyClass, APTR.Null);
		var child = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			areaClass, APTR.Null);
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, family, child));

		Assert.Equal(1u, Get(ref platform, family, FamilyChildCount));
		var list = APTR.FromPointer(Get(ref platform, family, FamilyList));
		Assert.True(platform.IsMapped(list, Amiga.List.Size));
		var cursor = platform.ReadUInt32(list, ExecLayout.List.Head);
		Assert.Equal(child, MuiGroupChildrenCore.NextObject(ref platform, list,
			ref cursor));

		// The public tag slots remain compatibility/bootstrap storage. The live
		// count and list are derived from the named Family topology instead.
		var record = MuiHeadlessObjectCore.FindObject(ref platform, State, family);
		Assert.True(MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform,
			State, record, FamilyChildCount, 99, false));
		Assert.True(MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform,
			State, record, FamilyList, 0x1234, false));
		Assert.Equal(1u, Get(ref platform, family, FamilyChildCount));
		var rebuiltList = Get(ref platform, family, FamilyList);
		Assert.NotEqual(0x1234u, rebuiltList);

		var message = APTR.FromPointer(0x1800);
		var storage = APTR.FromPointer(0x1900);
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform, message,
			MuiCommonPacketKind.Get, MuiCommonField.MethodId,
			MuiCommonControlPacketCore.OmGet));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform, message,
			MuiCommonPacketKind.Get, MuiCommonField.Storage, storage.Raw));
		foreach (var attribute in new[] { FamilyChildCount, FamilyList })
		{
			Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
				message, MuiCommonPacketKind.Get, MuiCommonField.Attribute,
				attribute));
			Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform,
				State, family, message));
			Assert.True(MuiGuestUlongStorageCodec.TryRead(ref platform, storage,
				out var stored));
			Assert.Equal(attribute == FamilyChildCount ? 1u : rebuiltList,
				stored.Value);
		}
	}

	[Fact]
	public void FamilyChildTagAdoptsOnlyDuringInitialization()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			State);
		var familyName = APTR.FromPointer(0x1100);
		var areaName = APTR.FromPointer(0x1140);
		platform.WriteCString(familyName, "Family.mui");
		platform.WriteCString(areaName, "Area.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		var familyClass = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			State, familyName, APTR.Null, 0, APTR.FromPointer(1));
		var areaClass = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			State, areaName, APTR.Null, 0, APTR.FromPointer(1));
		var first = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			areaClass, APTR.Null);
		var tags = APTR.FromPointer(0x1200);
		platform.WriteUInt32(tags, 0, MuiGroupChildrenCore.FamilyChild);
		platform.WriteUInt32(tags, 4, first.Raw);
		platform.WriteUInt32(tags, 8, 0);
		platform.WriteUInt32(tags, 12, 0);
		var family = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			familyClass, tags);
		Assert.True(family.IsNotNull);
		Assert.Equal(1u, Get(ref platform, family,
			MuiGroupChildrenCore.FamilyChildCount));
		Assert.Equal(family.Raw, Get(ref platform, first,
			0x8042E35F));

		var second = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			areaClass, APTR.Null);
		Assert.False(MuiHeadlessObjectCore.SetAttribute(ref platform, State,
			family, MuiGroupChildrenCore.FamilyChild, second.Raw, false));
		Assert.Equal(1u, Get(ref platform, family,
			MuiGroupChildrenCore.FamilyChildCount));

		var badTags = APTR.FromPointer(0x1300);
		platform.WriteUInt32(badTags, 0, MuiGroupChildrenCore.FamilyChild);
		platform.WriteUInt32(badTags, 4, 0);
		platform.WriteUInt32(badTags, 8, 0);
		platform.WriteUInt32(badTags, 12, 0);
		var failed = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			familyClass, badTags);
		Assert.True(failed.IsNull);
		Assert.Equal(1u, Get(ref platform, family,
			MuiGroupChildrenCore.FamilyChildCount));
	}

	[Fact]
	public void GroupChildTagAliasesToFamilyChildForFamilyClasses()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			State);
		var familyName = APTR.FromPointer(0x1100);
		var areaName = APTR.FromPointer(0x1140);
		platform.WriteCString(familyName, "Family.mui");
		platform.WriteCString(areaName, "Area.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		var familyClass = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			State, familyName, APTR.Null, 0, APTR.FromPointer(1));
		var areaClass = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			State, areaName, APTR.Null, 0, APTR.FromPointer(1));
		var first = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			areaClass, APTR.Null);
		var tags = APTR.FromPointer(0x1200);
		platform.WriteUInt32(tags, 0, MuiGroupChildrenCore.Child);
		platform.WriteUInt32(tags, 4, first.Raw);
		platform.WriteUInt32(tags, 8, 0);
		platform.WriteUInt32(tags, 12, 0);
		var family = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			familyClass, tags);
		Assert.True(family.IsNotNull);
		Assert.Equal(1u, Get(ref platform, family,
			MuiGroupChildrenCore.FamilyChildCount));

		var second = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			areaClass, APTR.Null);
		Assert.False(MuiHeadlessObjectCore.SetAttribute(ref platform, State,
			family, MuiGroupChildrenCore.Child, second.Raw, false));
		Assert.Equal(1u, Get(ref platform, family,
			MuiGroupChildrenCore.FamilyChildCount));
	}

	private static uint Get(ref MuiHeadlessTestPlatform platform, APTR obj,
		uint attribute)
	{
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, obj,
			attribute, out var value));
		return value;
	}
}
