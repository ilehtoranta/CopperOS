using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiAreaCustomBackfillTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);

	[Fact]
	public void CustomBackfillUsesNamedAreaPresentationState()
	{
		var platform = CreatePlatform(out var rectangleClass);
		var rectangle = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			rectangleClass, APTR.Null);

		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			rectangle, MuiCommonControlCore.CustomBackfill, 1, false));
		Assert.True(MuiCommonControlCore.TryReadAreaPresentationState(
			ref platform, State, rectangle, out var state));
		Assert.Equal(1u, state.CustomBackfill);
		Assert.True(MuiCommonControlCore.TryGetAreaPresentationStateRecord(
			ref platform, State, rectangle, out var record));
		Assert.Equal(MuiAreaPresentationStateRecord.Cookie, record.Magic);
		Assert.Equal(1u, record.CustomBackfill);
		Assert.True(MuiCommonControlCore.TryGet(ref platform, State, rectangle,
			MuiCommonControlCore.CustomBackfill, out var value, out var handled));
		Assert.True(handled);
		Assert.Equal(1u, value);
	}

	[Fact]
	public void CustomBackfillNormalizesAndPublishesOmGet()
	{
		var platform = CreatePlatform(out var rectangleClass);
		var rectangle = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			rectangleClass, APTR.Null);
		var attribute = MuiCommonControlCore.CustomBackfill;
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			rectangle, attribute, 7, false));
		Assert.Equal(1u, Get(ref platform, rectangle, attribute));

		var message = APTR.FromPointer(0x1800);
		var storage = APTR.FromPointer(0x1840);
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			message, MuiCommonPacketKind.Get, MuiCommonField.MethodId,
			MuiCommonControlPacketCore.OmGet));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			message, MuiCommonPacketKind.Get, MuiCommonField.Attribute,
			attribute));
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
