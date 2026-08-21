using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiGroupChildrenTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);
	private const uint Child = 0x804226E6;
	private const uint ChildCount = 0x80420322;
	private const uint ChildList = 0x80424748;
	private const uint Forward = 0x80421422;
	private const uint ForwardDepth = 0x80428488;
	private const uint Probe = 0x8042F8DC;

	[Fact]
	public void ChildTagsAdoptChildrenAndPublishCount()
	{
		var platform = CreateClasses(out var groupClass, out var areaClass);
		var first = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			areaClass, APTR.Null);
		var second = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			areaClass, APTR.Null);
		var tags = APTR.FromPointer(0x1200);
		platform.WriteUInt32(tags, 0, Child);
		platform.WriteUInt32(tags, 4, first.Raw);
		platform.WriteUInt32(tags, 8, Child);
		platform.WriteUInt32(tags, 12, second.Raw);
		platform.WriteUInt32(tags, 16, 0);
		platform.WriteUInt32(tags, 20, 0);

		var group = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			groupClass, tags);
		Assert.True(group.IsNotNull);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, group,
			ChildCount, out var count));
		Assert.Equal(2u, count);
		Assert.Equal(first, MuiFamilyCore.GetChild(ref platform, State, group,
			0, APTR.Null));
		Assert.Equal(second, MuiFamilyCore.GetChild(ref platform, State, group,
			1, APTR.Null));
	}

	[Fact]
	public void GroupChildGettersUseNamedStateThroughCommonOmGet()
	{
		var platform = CreateClasses(out var groupClass, out var areaClass);
		var group = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			groupClass, APTR.Null);
		var child = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			areaClass, APTR.Null);
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, group, child));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, group,
			ChildCount, out var count));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, group,
			ChildList, out var list));

		// Compatibility writes to the public slots cannot replace the live
		// family-derived count or the named ChildList projection.
		var record = MuiHeadlessObjectCore.FindObject(ref platform, State, group);
		Assert.True(MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, State,
			record, ChildCount, 99, false));
		Assert.True(MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, State,
			record, ChildList, 0x1234, false));
		Assert.Equal(1u, Get(ref platform, group, ChildCount));
		var rebuiltList = Get(ref platform, group, ChildList);
		Assert.NotEqual(0x1234u, rebuiltList);
		Assert.True(platform.IsMapped(APTR.FromPointer(rebuiltList), Amiga.List.Size));

		var message = APTR.FromPointer(0x1800);
		var storage = APTR.FromPointer(0x1900);
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform, message,
			MuiCommonPacketKind.Get, MuiCommonField.MethodId,
			MuiCommonControlPacketCore.OmGet));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform, message,
			MuiCommonPacketKind.Get, MuiCommonField.Attribute, ChildCount));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform, message,
			MuiCommonPacketKind.Get, MuiCommonField.Storage, storage.Raw));
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			group, message));
		Assert.True(MuiGuestUlongStorageCodec.TryRead(ref platform, storage,
			out var countStorage));
		Assert.Equal(count, countStorage.Value);

		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform, message,
			MuiCommonPacketKind.Get, MuiCommonField.Attribute, ChildList));
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			group, message));
		Assert.True(MuiGuestUlongStorageCodec.TryRead(ref platform, storage,
			out var listStorage));
		Assert.Equal(rebuiltList, listStorage.Value);
	}

	[Fact]
	public void NullChildTagFailsAndDisposesPreviouslyAdoptedChildren()
	{
		var platform = CreateClasses(out var groupClass, out var areaClass);
		var child = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			areaClass, APTR.Null);
		var tags = APTR.FromPointer(0x1200);
		platform.WriteUInt32(tags, 0, Child);
		platform.WriteUInt32(tags, 4, child.Raw);
		platform.WriteUInt32(tags, 8, Child);
		platform.WriteUInt32(tags, 12, 0);
		platform.WriteUInt32(tags, 16, 0);
		platform.WriteUInt32(tags, 20, 0);

		var group = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			groupClass, tags);
		Assert.True(group.IsNull);
		Assert.True(MuiHeadlessObjectCore.FindObject(ref platform, State, child)
			.IsNull);
	}

	[Fact]
	public void ForwardTargetsChildrenAndForwardDepthTargetsDescendants()
	{
		var platform = CreateClasses(out var groupClass, out var areaClass);
		var group = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			groupClass, APTR.Null);
		var childGroup = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			groupClass, APTR.Null);
		var child = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			areaClass, APTR.Null);
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, group, childGroup));
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, childGroup, child));

		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, group,
			Forward, 1, false));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, group,
			Probe, 0x1111, false));
		Assert.Equal(0x1111u, Get(ref platform, childGroup, Probe));
		Assert.False(MuiHeadlessObjectCore.GetAttribute(ref platform, State, child,
			Probe, out _));

		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, group,
			ForwardDepth, 1, false));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, group,
			Probe, 0x2222, false));
		Assert.Equal(0x2222u, Get(ref platform, childGroup, Probe));
		Assert.Equal(0x2222u, Get(ref platform, child, Probe));
		Assert.False(MuiHeadlessObjectCore.GetAttribute(ref platform, State, group,
			Probe, out _));
	}

	[Fact]
	public void ForwardStateUsesTypedGuestRecordAndIsReleased()
	{
		var platform = CreateClasses(out var groupClass, out _);
		var group = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			groupClass, APTR.Null);
		var before = platform.AllocationCount;
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, group,
			Forward, 1, false));
		Assert.True(platform.AllocationCount > before);
		Assert.Equal(1u, Get(ref platform, group, Forward));
		var freesBefore = platform.FreeCount;
		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State, group));
		Assert.True(platform.FreeCount > freesBefore);
	}

	[Fact]
	public void GroupExecListCodecUsesNamedListFields()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var address = APTR.FromPointer(0x1500);
		var value = default(MuiGroupExecListRecord);
		value.Head = APTR.FromPointer(0x1800);
		value.Tail = APTR.FromPointer(0x1900);
		value.TailPred = APTR.FromPointer(0x1A00);
		value.Type = NodeType.Unknown;
		value.Padding = 0xA5;
		Assert.True(MuiGroupExecListCodec.Write(ref platform, address, value));
		Assert.True(MuiGroupExecListCodec.TryRead(ref platform, address,
			out var decoded));
		Assert.Equal(value.Head, decoded.Head);
		Assert.Equal(value.Tail, decoded.Tail);
		Assert.Equal(value.TailPred, decoded.TailPred);
		Assert.Equal(value.Type, decoded.Type);
		Assert.Equal(value.Padding, decoded.Padding);
		Assert.False(MuiGroupExecListCodec.TryRead(ref platform,
			APTR.FromPointer(0x21000), out _));
	}

	[Fact]
	public void GroupRecordFieldCursorUsesSemanticKindsAndWidths()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var cursor = default(MuiGroupRecordFieldCursor);
		cursor.Address = APTR.FromPointer(0x2200);
		cursor.Record = MuiGroupRecordKind.ChildList;
		cursor.Field = MuiGroupRecordField.Generation;
		Assert.True(MuiGroupRecordFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out var fieldAddress, out var fieldSize));
		Assert.Equal(0x221Cu, fieldAddress.Raw);
		Assert.Equal(4u, fieldSize);
		Assert.True(MuiGroupRecordFieldCursorCodec.TryWriteUInt32(ref platform,
			cursor.Address, MuiGroupRecordKind.ChildList,
			MuiGroupRecordField.Generation, 0xA5A5u));
		Assert.True(MuiGroupRecordFieldCursorCodec.TryReadUInt32(ref platform,
			cursor.Address, MuiGroupRecordKind.ChildList,
			MuiGroupRecordField.Generation, out var generation));
		Assert.Equal(0xA5A5u, generation);
		cursor.Record = MuiGroupRecordKind.ExecList;
		cursor.Field = MuiGroupRecordField.Type;
		Assert.True(MuiGroupRecordFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out fieldAddress, out fieldSize));
		Assert.Equal(0x220Cu, fieldAddress.Raw);
		Assert.Equal(1u, fieldSize);
		Assert.True(MuiGroupRecordFieldCursorCodec.TryWriteUInt8(ref platform,
			cursor.Address, MuiGroupRecordKind.ExecList,
			MuiGroupRecordField.Padding, (byte)0x7F));
		Assert.True(MuiGroupRecordFieldCursorCodec.TryReadUInt8(ref platform,
			cursor.Address, MuiGroupRecordKind.ExecList,
			MuiGroupRecordField.Padding, out var padding));
		Assert.Equal(0x7Fu, padding);
		cursor.Record = MuiGroupRecordKind.ExecList;
		cursor.Field = MuiGroupRecordField.Generation;
		Assert.False(MuiGroupRecordFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out _, out _));
		cursor.Address = APTR.FromPointer(0xFFFFFFF0u);
		cursor.Field = MuiGroupRecordField.Head;
		Assert.False(MuiGroupRecordFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out _, out _));
	}

	[Fact]
	public void GroupChildListEntryUsesNamedCursorBoundary()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var cursor = new MuiGroupChildListEntryCursor
		{
			Base = APTR.FromPointer(0x1800),
			Index = 2,
		};

		Assert.True(MuiGroupChildListEntryVectorCodec.TryGetEntry(ref platform,
			cursor, out var address));
		Assert.Equal(APTR.FromPointer(0x1820), address);
		cursor.Base = APTR.FromPointer(0x20FFC);
		cursor.Index = 0;
		Assert.False(MuiGroupChildListEntryVectorCodec.TryGetEntry(ref platform,
			cursor, out _));
	}

	[Fact]
	public void ChildListPublishesTypedExecListAndNextObjectView()
	{
		var platform = CreateClasses(out var groupClass, out var areaClass);
		var group = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			groupClass, APTR.Null);
		var first = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			areaClass, APTR.Null);
		var second = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			areaClass, APTR.Null);
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, group, first));
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, group, second));

		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, group,
			ChildList, out var listValue));
		var list = APTR.FromPointer(listValue);
		Assert.True(platform.IsMapped(list, Amiga.List.Size));
		var cursor = platform.ReadUInt32(list, ExecLayout.List.Head);
		Assert.Equal(first, MuiGroupChildrenCore.NextObject(ref platform, list,
			ref cursor));
		Assert.Equal(second, MuiGroupChildrenCore.NextObject(ref platform, list,
			ref cursor));
		Assert.Equal(0u, cursor);
		Assert.Equal(APTR.Null, MuiGroupChildrenCore.NextObject(ref platform, list,
			ref cursor));

		var freeBefore = platform.FreeCount;
		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State, group));
		Assert.True(platform.FreeCount >= freeBefore + 2);
	}

	[Fact]
	public void ChildListIsReadOnlyAndRebuiltAfterFamilyMutation()
	{
		var platform = CreateClasses(out var groupClass, out var areaClass);
		var group = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			groupClass, APTR.Null);
		var first = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			areaClass, APTR.Null);
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, group, first));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, group,
			ChildList, out var before));
		Assert.False(MuiHeadlessObjectCore.SetAttribute(ref platform, State, group,
			ChildList, 0x1234, false));

		var second = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			areaClass, APTR.Null);
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, group, second));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, group,
			ChildList, out var after));
		Assert.NotEqual(before, after);
		var list = APTR.FromPointer(after);
		var cursor = platform.ReadUInt32(list, ExecLayout.List.Head);
		Assert.Equal(first, MuiGroupChildrenCore.NextObject(ref platform, list,
			ref cursor));
		Assert.Equal(second, MuiGroupChildrenCore.NextObject(ref platform, list,
			ref cursor));
	}

	private static MuiHeadlessTestPlatform CreateClasses(out APTR groupClass,
		out APTR areaClass)
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var groupName = APTR.FromPointer(0x1100);
		var areaName = APTR.FromPointer(0x1140);
		platform.WriteCString(groupName, "Group.mui");
		platform.WriteCString(areaName, "Area.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		groupClass = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			State, groupName, APTR.Null, 0, APTR.FromPointer(1));
		areaClass = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			State, areaName, APTR.Null, 0, APTR.FromPointer(1));
		return platform;
	}

	private static uint Get(ref MuiHeadlessTestPlatform platform, APTR obj,
		uint attribute)
	{
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, obj,
			attribute, out var value));
		return value;
	}
}
