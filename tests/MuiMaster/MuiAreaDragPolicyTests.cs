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
