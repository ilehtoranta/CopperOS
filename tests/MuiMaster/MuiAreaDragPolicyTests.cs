using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiAreaDragPolicyTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);

	[Fact]
	public void DragPolicyUsesNamedRecordAndMorphosDefaults()
	{
		var platform = CreatePlatform(out var rectangleClass);
		var rectangle = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			rectangleClass, APTR.Null);

		Assert.True(MuiCommonControlCore.TryGet(ref platform, State, rectangle,
			MuiCommonControlCore.Draggable, out var draggable, out var handled));
		Assert.True(handled);
		Assert.Equal(0u, draggable);
		Assert.Equal(1u, Get(ref platform, rectangle,
			MuiCommonControlCore.Dropable));
		Assert.True(MuiAreaDragCore.TryGetPolicyStateRecord(ref platform, State,
			rectangle, out var record));
		Assert.Equal(MuiAreaDragPolicyStateRecord.Cookie, record.Magic);
		Assert.Equal(0u, record.Draggable);
		Assert.Equal(1u, record.Dropable);
	}

	[Fact]
	public void DragPolicySetAndOmGetNormalizeBooleanValues()
	{
		var platform = CreatePlatform(out var rectangleClass);
		var rectangle = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			rectangleClass, APTR.Null);
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			rectangle, MuiCommonControlCore.Draggable, 9, false));
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			rectangle, MuiCommonControlCore.Dropable, 0, false));
		Assert.Equal(1u, Get(ref platform, rectangle,
			MuiCommonControlCore.Draggable));
		Assert.Equal(0u, Get(ref platform, rectangle,
			MuiCommonControlCore.Dropable));

		var message = APTR.FromPointer(0x1800);
		var storage = APTR.FromPointer(0x1840);
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			message, MuiCommonPacketKind.Get, MuiCommonField.MethodId,
			MuiCommonControlPacketCore.OmGet));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			message, MuiCommonPacketKind.Get, MuiCommonField.Attribute,
			MuiCommonControlCore.Draggable));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			message, MuiCommonPacketKind.Get, MuiCommonField.Storage,
			storage.Raw));
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			rectangle, message));
		Assert.Equal(1u, platform.ReadUInt32(storage, 0));
	}

	[Fact]
	public void DragPolicyAdmissionRequiresCanonicalBooleansAndLiveOwner()
	{
		var platform = CreatePlatform(out var rectangleClass);
		var rectangle = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			rectangleClass, APTR.Null);
		var valid = new MuiAreaDragPolicyStateRecord
		{
			Magic = MuiAreaDragPolicyStateRecord.Cookie,
			Draggable = 1,
			Dropable = 0,
		};
		Assert.True(MuiAreaDragPolicyStateAdmission.Validate(valid));
		Assert.True(MuiAreaDragPolicyStateAdmission.ValidateLive(ref platform,
			State, rectangle, valid));
		var malformed = valid;
		malformed.Draggable = 2;
		Assert.False(MuiAreaDragPolicyStateAdmission.Validate(malformed));
		malformed = valid;
		malformed.Dropable = 2;
		Assert.False(MuiAreaDragPolicyStateAdmission.Validate(malformed));
		Assert.False(MuiAreaDragPolicyStateAdmission.ValidateLive(ref platform,
			State, APTR.FromPointer(0xDEAD), valid));
	}

	[Fact]
	public void DragPolicyUsesDedicatedStructCodec()
	{
		var platform = CreatePlatform(out _);
		var address = APTR.FromPointer(0x1A80);
		var value = default(MuiAreaDragPolicyStateRecord);
		value.Magic = MuiAreaDragPolicyStateRecord.Cookie;
		value.Draggable = 1;
		value.Dropable = 0;

		Assert.True(MuiAreaDragPolicyStateRecordCodec.Write(ref platform, address,
			value));
		Assert.True(MuiAreaDragPolicyStateRecordCodec.TryReadStructural(
			ref platform, address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.Draggable, decoded.Draggable);
		Assert.Equal(value.Dropable, decoded.Dropable);
		Assert.True(MuiAreaDragPolicyStateRecordCodec.TryRead(ref platform,
			address, out decoded));
		Assert.False(MuiAreaDragPolicyStateRecordCodec.TryReadStructural(
			ref platform, APTR.Null, out _));
	}

	[Fact]
	public void DragPolicyMemoryAdapterBoundsNamedRecord()
	{
		var platform = CreatePlatform(out _);
		var address = APTR.FromPointer(0x1A80);
		Assert.True(MuiAreaDragPolicyStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiAreaDragPolicyStateField.Draggable,
			out var draggableAddress));
		Assert.Equal(address.Raw + 4, draggableAddress.Raw);
		Assert.False(MuiAreaDragPolicyStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, (MuiAreaDragPolicyStateField)255, out _));
		Assert.False(MuiAreaDragPolicyStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiAreaDragPolicyStateField.Magic, out _));
	}

	[Fact]
	public void MalformedDragPolicyFailsClosedBeforeRawRepairOrDragRouting()
	{
		var platform = CreatePlatform(out var rectangleClass);
		var rectangle = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			rectangleClass, APTR.Null);
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State,
			rectangle, MuiAreaDragCore.Draggable, 1, false));
		Assert.True(MuiAreaDragCore.TryGetPolicyStateRecord(ref platform, State,
			rectangle, out _));
		var block = MuiStoreCore.DataspaceFind(ref platform, State, rectangle,
			MuiAreaDragCore.PolicyStateKey);
		Assert.True(block.IsNotNull);
		Assert.True(MuiAreaDragPolicyStateFieldCursorCodec.TryWriteUInt32(
			ref platform, block, MuiAreaDragPolicyStateField.Draggable, 2));
		Assert.True(MuiAreaDragPolicyStateRecordCodec.TryReadStructural(ref platform,
			block, out var structural));
		Assert.Equal(2u, structural.Draggable);
		Assert.False(MuiAreaDragPolicyStateAdmission.Validate(structural));
		Assert.False(MuiAreaDragPolicyStateRecordCodec.TryRead(ref platform, block,
			out _));
		var allocationsBefore = platform.AllocationCount;
		Assert.False(MuiAreaDragCore.TryGetPolicyStateRecord(ref platform, State,
			rectangle, out _));
		Assert.Equal(allocationsBefore, platform.AllocationCount);
		Assert.False(MuiCommonControlCore.TryGet(ref platform, State, rectangle,
			MuiCommonControlCore.Draggable, out _, out _));
		Assert.False(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			rectangle, MuiCommonControlCore.Draggable, 0, false));
		Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State,
			rectangle, MuiCommonControlCore.Draggable, out var raw));
		Assert.Equal(1u, raw);
		platform.DragRouteSampleAvailable = true;
		var packet = APTR.FromPointer(0x1A00);
		Assert.True(MuiAreaDragMessageCodec.WriteBegin(ref platform, packet,
			rectangle.Raw));
		Assert.Equal(0u, MuiLayoutDispatcher.Dispatch(ref platform, State,
			rectangle, packet));
		Assert.Equal(0u, platform.DragRouteCount);
		Assert.Equal(block, MuiStoreCore.DataspaceFind(ref platform, State,
			rectangle, MuiAreaDragCore.PolicyStateKey));
	}

	private static uint Get(ref MuiHeadlessTestPlatform platform, APTR obj,
		uint attribute)
	{
		Assert.True(MuiCommonControlCore.TryGet(ref platform, State, obj,
			attribute, out var value, out var handled));
		Assert.True(handled);
		return value;
	}

	private static MuiHeadlessTestPlatform CreatePlatform(out APTR rectangleClass)
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var name = APTR.FromPointer(0x1100);
		platform.WriteCString(name, "Rectangle.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		rectangleClass = MuiHeadlessObjectCore.RegisterClass(ref platform, State,
			name, APTR.Null, 0, APTR.FromPointer(1), false);
		return platform;
	}
}
