using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiAreaDoubleBufferTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);

	[Fact]
	public void StateRecordUsesNamedFields()
	{
		var platform = CreatePlatform(out _);
		var address = APTR.FromPointer(0x1500);
		var expected = default(MuiAreaDoubleBufferStateRecord);
		expected.Magic = MuiAreaDoubleBufferStateRecord.Cookie;
		expected.Enabled = 1;
		expected.Generation = 7;

		Assert.True(MuiAreaDoubleBufferStateRecordCodec.Write(ref platform,
			address, expected));
		Assert.True(MuiAreaDoubleBufferStateRecordCodec.TryRead(ref platform,
			address, out var actual));
		Assert.Equal(expected.Magic, actual.Magic);
		Assert.Equal(expected.Enabled, actual.Enabled);
		Assert.Equal(expected.Generation, actual.Generation);

		var cursor = default(MuiAreaDoubleBufferStateFieldCursor);
		cursor.Record = address;
		cursor.Field = MuiAreaDoubleBufferStateField.Generation;
		Assert.True(MuiAreaDoubleBufferStateFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out var generationAddress));
		Assert.Equal(address.Raw + 8, generationAddress.Raw);
		Assert.False(MuiAreaDoubleBufferStateRecordCodec.TryRead(ref platform,
			APTR.FromPointer(0x20FFFu), out _));
	}

	[Fact]
	public void TypedDoubleBufferStateNormalizesAndRoundTrips()
	{
		var platform = CreatePlatform(out var areaClass);
		var obj = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			areaClass, APTR.Null);

		Assert.True(MuiAreaDoubleBufferPacketCore.TryGet(ref platform, State, obj,
			out var initial));
		Assert.Equal(0u, initial.Enabled);
		Assert.True(MuiAreaDoubleBufferPacketCore.Set(ref platform, State, obj, 9));
		Assert.True(MuiAreaDoubleBufferPacketCore.TryGet(ref platform, State, obj,
			out var enabled));
		Assert.Equal(1u, enabled.Enabled);
		Assert.True(MuiAreaDoubleBufferStateRecordCodec.TryRead(ref platform,
			MuiStoreCore.DataspaceFind(ref platform, State, obj,
				MuiAreaDoubleBufferCore.StateKey), out var record));
		Assert.Equal(1u, record.Enabled);
		Assert.True(record.Generation >= 1u);
	}

	[Fact]
	public void GenericSetAndGetUseDoubleBufferState()
	{
		var platform = CreatePlatform(out var areaClass);
		var obj = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			areaClass, APTR.Null);

		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, obj,
			MuiCommonControlCore.DoubleBuffer, 2, false));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, obj,
			MuiCommonControlCore.DoubleBuffer, out var value));
		Assert.Equal(1u, value);
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			obj, MuiCommonControlCore.DoubleBuffer, 0, false));
		Assert.True(MuiCommonControlCore.TryGet(ref platform, State, obj,
			MuiCommonControlCore.DoubleBuffer, out value, out var handled));
		Assert.True(handled);
		Assert.Equal(0u, value);
	}

	[Fact]
	public void DispatcherSetAndOmGetProjectDoubleBuffer()
	{
		var platform = CreatePlatform(out var areaClass);
		var obj = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			areaClass, APTR.Null);
		var packet = APTR.FromPointer(0x1300);
		platform.WriteUInt32(packet, 0, MuiCommonControlPacketCore.Set);
		platform.WriteUInt32(packet, 4, MuiCommonControlCore.DoubleBuffer);
		platform.WriteUInt32(packet, 8, 3);
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			obj, packet));

		var storage = APTR.FromPointer(0x1400);
		platform.WriteUInt32(packet, 0, MuiCommonControlPacketCore.OmGet);
		platform.WriteUInt32(packet, 4, MuiCommonControlCore.DoubleBuffer);
		platform.WriteUInt32(packet, 8, storage.Raw);
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			obj, packet));
		Assert.Equal(1u, platform.ReadUInt32(storage, 0));
	}

	[Fact]
	public void CommonControlDrawUsesProviderTargetAndRestoresRenderInfo()
	{
		var platform = CreatePlatform(out var rectangleClass);
		var rectangle = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			rectangleClass, APTR.Null);
		var renderInfo = APTR.FromPointer(0x1300);
		var sourceRastPort = APTR.FromPointer(0x1400);
		var targetRenderInfo = APTR.FromPointer(0x1500);
		var targetRastPort = APTR.FromPointer(0x1600);
		platform.WriteUInt32(renderInfo, 20, sourceRastPort.Raw);
		platform.WriteUInt32(targetRenderInfo, 20, targetRastPort.Raw);
		platform.DoubleBufferCapabilityAvailable = true;
		platform.DoubleBufferTargetRenderInfo = targetRenderInfo;
		platform.DoubleBufferTargetRastPort = targetRastPort;
		platform.DoubleBufferOverrideTargetGeometry = true;
		platform.DoubleBufferTargetLeft = 7;
		platform.DoubleBufferTargetTop = 8;
		platform.DoubleBufferTargetWidth = 11;
		platform.DoubleBufferTargetHeight = 6;
		Assert.True(MuiAreaLayoutCore.Setup(ref platform, State, rectangle,
			renderInfo));
		Assert.True(MuiAreaLayoutCore.Layout(ref platform, State, rectangle, 4, 5,
			24, 12));
		Assert.True(MuiAreaDoubleBufferPacketCore.Set(ref platform, State,
			rectangle, 1));

		Assert.True(MuiCommonControlCore.DrawControl(ref platform, State,
			rectangle, 0x12));
		Assert.Equal(1u, platform.DoubleBufferBeginCount);
		Assert.Equal(1u, platform.DoubleBufferEndCount);
		Assert.True(platform.LastDoubleBufferEndCompleted);
		Assert.Equal(sourceRastPort, platform.LastDoubleBufferRequest.SourceRastPort);
		Assert.Equal(7, platform.LastDoubleBufferRequest.TargetLeft);
		Assert.Equal(8, platform.LastDoubleBufferRequest.TargetTop);
		Assert.Equal(11, platform.LastDoubleBufferRequest.TargetWidth);
		Assert.Equal(6, platform.LastDoubleBufferRequest.TargetHeight);
		Assert.Equal(targetRastPort, platform.LastBeginUpdateLayer);
		Assert.Equal(1u, platform.FillCount);
		Assert.Equal(7, platform.LastLeft);
		Assert.Equal(8, platform.LastTop);
		Assert.Equal(17, platform.LastRight);
		Assert.Equal(13, platform.LastBottom);
		Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State,
			rectangle, 0x7FFF0001u, out var restored));
		Assert.Equal(renderInfo.Raw, restored);
	}

	[Fact]
	public void AreaDrawEndsFailedDoubleBufferWhenUpdateCannotBegin()
	{
		var platform = CreatePlatform(out var areaClass);
		var area = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			areaClass, APTR.Null);
		var renderInfo = APTR.FromPointer(0x1300);
		var sourceRastPort = APTR.FromPointer(0x1400);
		var targetRenderInfo = APTR.FromPointer(0x1500);
		var targetRastPort = APTR.FromPointer(0x1600);
		platform.WriteUInt32(renderInfo, 20, sourceRastPort.Raw);
		platform.WriteUInt32(targetRenderInfo, 20, targetRastPort.Raw);
		platform.DoubleBufferCapabilityAvailable = true;
		platform.DoubleBufferTargetRenderInfo = targetRenderInfo;
		platform.DoubleBufferTargetRastPort = targetRastPort;
		platform.BeginUpdateFails = true;
		Assert.True(MuiAreaLayoutCore.Setup(ref platform, State, area, renderInfo));
		Assert.True(MuiAreaLayoutCore.Layout(ref platform, State, area, 1, 2, 16,
			8));
		Assert.True(MuiAreaDoubleBufferPacketCore.Set(ref platform, State, area, 1));

		Assert.False(MuiAreaLayoutCore.Draw(ref platform, State, area, 0x44));
		Assert.Equal(1u, platform.DoubleBufferBeginCount);
		Assert.Equal(1u, platform.DoubleBufferEndCount);
		Assert.False(platform.LastDoubleBufferEndCompleted);
		Assert.Equal(0u, platform.LayerDepth);
		Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State, area,
			0x7FFF0001u, out var restored));
		Assert.Equal(renderInfo.Raw, restored);
	}

	[Fact]
	public void EnabledDoubleBufferFallsBackWhenProviderDeclines()
	{
		var platform = CreatePlatform(out var rectangleClass);
		var rectangle = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			rectangleClass, APTR.Null);
		var renderInfo = APTR.FromPointer(0x1300);
		var sourceRastPort = APTR.FromPointer(0x1400);
		platform.WriteUInt32(renderInfo, 20, sourceRastPort.Raw);
		Assert.True(MuiAreaLayoutCore.Setup(ref platform, State, rectangle,
			renderInfo));
		Assert.True(MuiAreaLayoutCore.Layout(ref platform, State, rectangle, 0, 0,
			12, 8));
		Assert.True(MuiAreaDoubleBufferPacketCore.Set(ref platform, State,
			rectangle, 1));

		Assert.True(MuiCommonControlCore.DrawControl(ref platform, State,
			rectangle, 0));
		Assert.Equal(0u, platform.DoubleBufferBeginCount);
		Assert.Equal(0u, platform.DoubleBufferEndCount);
		Assert.Equal(sourceRastPort, platform.LastBeginUpdateLayer);
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
