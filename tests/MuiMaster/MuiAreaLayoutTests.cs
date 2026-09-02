using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiAreaLayoutTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);
	private const uint FixWidth = 0x8042A3F1;
	private const uint FixHeight = 0x8042A92B;
	private const uint InnerLeft = 0x804228F8;
	private const uint InnerRight = 0x804297FF;
	private const uint InnerTop = 0x80421EB6;
	private const uint InnerBottom = 0x8042F2C0;
	private const uint Frame = 0x8042AC64;
	private const uint Background = 0x8042545B;
	private const uint FillArea = 0x804294A3;
	private const uint AreaGeometryStateKey = 0x7F070035;
	private const uint AreaRenderPolicyStateKey = 0x7F070037;
	private const uint AreaLayoutPolicyStateKey = 0x7F070073;
	private const uint ShowMe = 0x80429BA8;
	private const uint Horizontal = 0x8042536B;
	private const uint HorizontalSpacing = 0x8042C651;
	private const uint VerticalSpacing = 0x8042E1BF;
	private const uint HorizontalWeight = 0x80426DB9;
	private const uint Columns = 0x8042F416;
	private const uint Rows = 0x8042B68F;
	private const uint SameSize = 0x80420860;
	private const uint HorizontalCenter = 0x8042CC64;
	private const uint VerticalCenter = 0x8042C008;
	private const uint LayoutHook = 0x8042C3B2;
	private const uint LayoutHookStateAttribute = 0x7FFE0046;
	private const uint LeftEdge = 0x8042BEC6;
	private const uint TopEdge = 0x8042509B;
	private const uint Width = 0x8042B59C;
	private const uint Height = 0x80423237;
	private const uint Unicode = 0x8042E7D0;
	private const uint HookEntryGroupLayout = 0x00CA0005u;

	[Fact]
	public void AreaMinMaxLifecycleAndNeutralDrawingAreDeterministic()
	{
		var platform = CreatePlatform(out var cl);
		var area = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		Set(ref platform, area, FixWidth, 20);
		Set(ref platform, area, FixHeight, 10);
		Set(ref platform, area, InnerLeft, 2);
		Set(ref platform, area, InnerRight, 3);
		Set(ref platform, area, InnerTop, 1);
		Set(ref platform, area, InnerBottom, 1);
		Set(ref platform, area, Frame, 1);
		Set(ref platform, area, Background, 6);
		var minMax = APTR.FromPointer(0x1200);
		Assert.True(MuiAreaLayoutCore.AskMinMax(ref platform, State, area, minMax));
		Assert.Equal((ushort)25, platform.ReadUInt16(minMax, 0));
		Assert.Equal((ushort)12, platform.ReadUInt16(minMax, 2));
		Assert.Equal((ushort)25, platform.ReadUInt16(minMax, 4));
		Assert.Equal((ushort)12, platform.ReadUInt16(minMax, 6));

		var renderInfo = APTR.FromPointer(0x1300);
		var rastPort = APTR.FromPointer(0x1400);
		platform.WriteUInt32(renderInfo, 20, rastPort.Raw);
		Assert.True(MuiAreaLayoutCore.Setup(ref platform, State, area, renderInfo));
		Assert.True(MuiAreaLayoutCore.Show(ref platform, State, area));
		Assert.True(MuiAreaLayoutCore.Layout(ref platform, State, area, 10, 20,
			25, 12));
		Assert.True(MuiAreaLayoutCore.Draw(ref platform, State, area, 1));
		Assert.Equal(1u, platform.FillCount);
		Assert.Equal(4u, platform.LineCount);
		Assert.Equal(4u, platform.LastPen);
		Assert.Equal(0u, platform.LayerDepth);
		Assert.Equal(10, platform.LastLeft);
		Assert.Equal(20, platform.LastTop);
		Assert.Equal(34, platform.LastRight);
		Assert.Equal(31, platform.LastBottom);
		Assert.True(MuiAreaLayoutCore.Hide(ref platform, State, area));
		Assert.True(MuiAreaLayoutCore.Cleanup(ref platform, State, area));
	}

	[Fact]
	public void AreaDrawingPublishesNamedRenderPolicy()
	{
		var platform = CreatePlatform(out var cl);
		var area = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		Set(ref platform, area, FillArea, 0);
		Set(ref platform, area, Background, 9);
		Set(ref platform, area, Frame, 1);
		var renderInfo = APTR.FromPointer(0x1300);
		var rastPort = APTR.FromPointer(0x1400);
		platform.WriteUInt32(renderInfo, 20, rastPort.Raw);
		Assert.True(MuiAreaLayoutCore.Setup(ref platform, State, area, renderInfo));
		Assert.True(MuiAreaLayoutCore.Show(ref platform, State, area));
		Assert.True(MuiAreaLayoutCore.Layout(ref platform, State, area, 2, 3,
			20, 10));
		Assert.True(MuiAreaLayoutCore.Draw(ref platform, State, area, 0));
		Assert.True(MuiAreaLayoutCore.TryGetRenderPolicyState(ref platform, State,
			area, out var policy));
		Assert.Equal(MuiAreaRenderPolicyStateRecord.Cookie, policy.Magic);
		Assert.Equal(0u, policy.FillArea);
		Assert.Equal(9u, policy.Background);
		Assert.Equal(1u, policy.Frame);
		Assert.Equal(0u, policy.Font);
		Assert.Equal(0u, platform.FillCount);
		Assert.Equal(4u, platform.LineCount);
	}

	[Fact]
	public void AreaRenderPolicyCodecUsesNamedFields()
	{
		var platform = CreatePlatform(out _);
		var address = APTR.FromPointer(0x1c00);
		var value = default(MuiAreaRenderPolicyStateRecord);
		value.Magic = MuiAreaRenderPolicyStateRecord.Cookie;
		value.FillArea = 1;
		value.Background = 7;
		value.Frame = 2;
		value.Font = 0x2200;
		Assert.True(MuiAreaRenderPolicyStateRecordCodec.Write(ref platform,
			address, value));
		Assert.True(MuiAreaRenderPolicyStateRecordCodec.TryRead(ref platform,
			address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.FillArea, decoded.FillArea);
		Assert.Equal(value.Background, decoded.Background);
		Assert.Equal(value.Frame, decoded.Frame);
		Assert.Equal(value.Font, decoded.Font);
		var cursor = default(MuiAreaRenderPolicyStateFieldCursor);
		cursor.Record = address;
		cursor.Field = MuiAreaRenderPolicyStateField.Font;
		Assert.True(MuiAreaRenderPolicyStateFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out var fieldAddress));
		Assert.Equal(address.Raw + 16, fieldAddress.Raw);
		cursor.Field = (MuiAreaRenderPolicyStateField)255;
		Assert.False(MuiAreaRenderPolicyStateFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out _));
	}

	[Fact]
	public void AreaRenderPolicyRecordUsesDedicatedStructMemoryAdapter()
	{
		var platform = CreatePlatform(out _);
		var address = APTR.FromPointer(0x1C80);
		var value = new MuiAreaRenderPolicyStateRecord
		{
			Magic = MuiAreaRenderPolicyStateRecord.Cookie,
			FillArea = 1,
			Background = 7,
			Frame = 2,
			Font = 0x2200,
			FrameVisible = 1,
			FramePhantomHoriz = 0,
			FrameTitle = APTR.FromPointer(0x2300),
			FrameDynamic = 1,
		};

		Assert.True(MuiAreaRenderPolicyStateRecordCodec.Write(ref platform,
			address, value));
		Assert.True(MuiAreaRenderPolicyStateRecordCodec.TryReadStructural(
			ref platform, address, out var structural));
		Assert.Equal(value.Magic, structural.Magic);
		Assert.Equal(value.FillArea, structural.FillArea);
		Assert.Equal(value.Background, structural.Background);
		Assert.Equal(value.Frame, structural.Frame);
		Assert.Equal(value.Font, structural.Font);
		Assert.Equal(value.FrameVisible, structural.FrameVisible);
		Assert.Equal(value.FramePhantomHoriz, structural.FramePhantomHoriz);
		Assert.Equal(value.FrameTitle, structural.FrameTitle);
		Assert.Equal(value.FrameDynamic, structural.FrameDynamic);
		Assert.True(MuiAreaRenderPolicyStateRecordCodec.TryRead(ref platform,
			address, out _));
		Assert.True(MuiAreaRenderPolicyStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiAreaRenderPolicyStateField.FrameDynamic,
			out var lastField));
		Assert.Equal(address.Raw + 32, lastField.Raw);
		Assert.True(MuiAreaRenderPolicyStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, MuiAreaRenderPolicyStateField.FrameTitle,
			out var frameTitle));
		Assert.Equal(value.FrameTitle.Raw, frameTitle);
		Assert.False(MuiAreaRenderPolicyStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, (MuiAreaRenderPolicyStateField)255, out _));
		Assert.False(MuiAreaRenderPolicyStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiAreaRenderPolicyStateField.Magic, out _));
		Assert.False(MuiAreaRenderPolicyStateRecordCodec.TryReadStructural(
			ref platform, APTR.Null, out _));
	}

	[Fact]
	public void AreaLayoutPolicyRecordUsesDedicatedStructMemoryAdapter()
	{
		var platform = CreatePlatform(out _);
		var address = APTR.FromPointer(0x1D00);
		var value = new MuiAreaLayoutPolicyStateRecord
		{
			Magic = MuiAreaLayoutPolicyStateRecord.Cookie,
			ShowMe = 1,
			FixWidth = 20,
			FixHeight = 10,
			MaxWidth = 100,
			MaxHeight = 80,
			InnerLeft = 2,
			InnerRight = 3,
			InnerTop = 1,
			InnerBottom = 1,
			HorizontalWeight = 7,
			VerticalWeight = 9,
		};

		Assert.True(MuiAreaLayoutPolicyStateRecordCodec.Write(ref platform,
			address, value));
		Assert.True(MuiAreaLayoutPolicyStateRecordCodec.TryReadStructural(
			ref platform, address, out var structural));
		Assert.Equal(value.Magic, structural.Magic);
		Assert.Equal(value.ShowMe, structural.ShowMe);
		Assert.Equal(value.FixWidth, structural.FixWidth);
		Assert.Equal(value.FixHeight, structural.FixHeight);
		Assert.Equal(value.MaxWidth, structural.MaxWidth);
		Assert.Equal(value.MaxHeight, structural.MaxHeight);
		Assert.Equal(value.InnerLeft, structural.InnerLeft);
		Assert.Equal(value.InnerRight, structural.InnerRight);
		Assert.Equal(value.InnerTop, structural.InnerTop);
		Assert.Equal(value.InnerBottom, structural.InnerBottom);
		Assert.Equal(value.HorizontalWeight, structural.HorizontalWeight);
		Assert.Equal(value.VerticalWeight, structural.VerticalWeight);
		Assert.True(MuiAreaLayoutPolicyStateRecordCodec.TryRead(ref platform,
			address, out _));
		Assert.True(MuiAreaLayoutPolicyStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiAreaLayoutPolicyField.VerticalWeight,
			out var lastField));
		Assert.Equal(address.Raw + 44, lastField.Raw);
		Assert.True(MuiAreaLayoutPolicyStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, MuiAreaLayoutPolicyField.HorizontalWeight,
			out var horizontalWeight));
		Assert.Equal(value.HorizontalWeight, horizontalWeight);
		Assert.False(MuiAreaLayoutPolicyStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, (MuiAreaLayoutPolicyField)255, out _));
		Assert.False(MuiAreaLayoutPolicyStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiAreaLayoutPolicyField.Magic, out _));
		Assert.False(MuiAreaLayoutPolicyStateRecordCodec.TryReadStructural(
			ref platform, APTR.Null, out _));
	}

	[Fact]
	public void MalformedRenderPolicyFailsClosedBeforeGetterMutationAndDrawing()
	{
		var platform = CreatePlatform(out var cl);
		var rectangleName = APTR.FromPointer(0x1120);
		platform.WriteCString(rectangleName, "Rectangle.mui");
		var rectangleClass = MuiHeadlessObjectCore.RegisterClass(ref platform, State,
			rectangleName, APTR.Null, 0, APTR.FromPointer(2), false);
		var area = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			rectangleClass,
			APTR.Null);
		Assert.True(MuiAreaLayoutCore.TryReadRenderPolicyState(ref platform, State,
			area, out _));
		var block = MuiStoreCore.DataspaceFind(ref platform, State, area,
			AreaRenderPolicyStateKey);
		Assert.True(block.IsNotNull);
		Assert.True(MuiAreaRenderPolicyStateFieldCursorCodec.TryWriteUInt32(
			ref platform, block, MuiAreaRenderPolicyStateField.FillArea, 2));
		Assert.True(MuiAreaRenderPolicyStateFieldCursorCodec.TryReadUInt32(
			ref platform, block, MuiAreaRenderPolicyStateField.FillArea,
			out var malformedFillArea));
		Assert.Equal(2u, malformedFillArea);

		Assert.False(MuiAreaLayoutCore.TryGetRenderPolicyState(ref platform, State,
			area, out _));
		Assert.False(MuiAreaLayoutCore.TryReadRenderPolicyState(ref platform, State,
			area, out _));
		Assert.False(MuiCommonControlCore.TryGet(ref platform, State, area,
			FillArea, out _, out _));
		Assert.False(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			area, FillArea, 1, false));
		Assert.False(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State,
			area, FillArea, out _));

		var renderInfo = APTR.FromPointer(0x1300);
		var rastPort = APTR.FromPointer(0x1400);
		platform.WriteUInt32(renderInfo, 20, rastPort.Raw);
		Assert.True(MuiAreaLayoutCore.Setup(ref platform, State, area, renderInfo));
		Assert.True(MuiAreaLayoutCore.Show(ref platform, State, area));
		Assert.True(MuiAreaLayoutCore.Layout(ref platform, State, area, 2, 3,
			20, 10));
		Assert.False(MuiAreaLayoutCore.Draw(ref platform, State, area, 0));
		Assert.Equal(0u, platform.FillCount);
		Assert.Equal(0u, platform.LineCount);
		Assert.False(MuiAreaLayoutCore.DrawBackground(ref platform, State, area,
			0, 0, 8, 8));

		Assert.True(MuiAreaRenderPolicyStateFieldCursorCodec.TryReadUInt32(
			ref platform, block, MuiAreaRenderPolicyStateField.FillArea,
			out malformedFillArea));
		Assert.Equal(2u, malformedFillArea);
	}

	[Fact]
	public void MalformedLayoutPolicyFailsClosedBeforeGetterMutationAndConsumers()
	{
		var platform = CreatePlatform(out var cl);
		var rectangleName = APTR.FromPointer(0x1120);
		platform.WriteCString(rectangleName, "Rectangle.mui");
		var rectangleClass = MuiHeadlessObjectCore.RegisterClass(ref platform, State,
			rectangleName, APTR.Null, 0, APTR.FromPointer(2), false);
		var area = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			rectangleClass, APTR.Null);
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, area,
			ShowMe, 1, false));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, area,
			FixWidth, 20, false));
		Assert.True(MuiAreaLayoutCore.TryReadLayoutPolicyState(ref platform, State,
			area, out _));
		var block = MuiStoreCore.DataspaceFind(ref platform, State, area,
			AreaLayoutPolicyStateKey);
		Assert.True(block.IsNotNull);
		Assert.True(MuiAreaLayoutPolicyFieldCursorCodec.TryWriteUInt32(ref platform,
			block, MuiAreaLayoutPolicyField.ShowMe, 2));
		Assert.True(MuiAreaLayoutPolicyFieldCursorCodec.TryReadUInt32(ref platform,
			block, MuiAreaLayoutPolicyField.ShowMe, out var malformedShowMe));
		Assert.Equal(2u, malformedShowMe);
		Assert.True(MuiAreaLayoutPolicyStateRecordCodec.TryReadStructural(
			ref platform, block, out var structural));
		Assert.Equal(2u, structural.ShowMe);
		Assert.False(MuiAreaLayoutPolicyStateAdmission.Validate(structural));
		Assert.False(MuiAreaLayoutPolicyStateRecordCodec.TryRead(ref platform,
			block, out _));

		Assert.False(MuiAreaLayoutCore.TryGetLayoutPolicyState(ref platform, State,
			area, out _));
		Assert.False(MuiAreaLayoutCore.TryReadLayoutPolicyState(ref platform, State,
			area, out _));
		Assert.False(MuiCommonControlCore.TryGet(ref platform, State, area,
			FixWidth, out _, out _));
		Assert.False(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			area, FixWidth, 40, false));
		Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State, area,
			FixWidth, out var rawFixWidth));
		Assert.Equal(20u, rawFixWidth);

		var minMax = MuiAreaLayoutCore.ComputeMinMax(ref platform, State, area);
		Assert.Equal(0, minMax.MinWidth);
		Assert.Equal(0, minMax.MinHeight);
		var renderInfo = APTR.FromPointer(0x1300);
		var rastPort = APTR.FromPointer(0x1400);
		platform.WriteUInt32(renderInfo, 20, rastPort.Raw);
		Assert.True(MuiAreaLayoutCore.Setup(ref platform, State, area, renderInfo));
		Assert.True(MuiAreaLayoutCore.Show(ref platform, State, area));
		Assert.True(MuiAreaLayoutCore.Layout(ref platform, State, area, 2, 3,
			20, 10));
		Assert.False(MuiAreaLayoutCore.Draw(ref platform, State, area, 0));
		Assert.False(MuiAreaLayoutCore.DrawBackground(ref platform, State, area,
			0, 0, 8, 8));
		Assert.Equal(0u, platform.FillCount);
		Assert.Equal(0u, platform.LineCount);

		Assert.True(MuiAreaLayoutPolicyFieldCursorCodec.TryReadUInt32(ref platform,
			block, MuiAreaLayoutPolicyField.ShowMe, out malformedShowMe));
		Assert.Equal(2u, malformedShowMe);
	}

	[Fact]
	public void MalformedGeometryFailsClosedBeforeGetterMutationAndDrawing()
	{
		var platform = CreatePlatform(out var cl);
		var area = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		Assert.True(MuiAreaLayoutCore.Layout(ref platform, State, area, 4, 6,
			20, 10));
		var block = MuiStoreCore.DataspaceFind(ref platform, State, area,
			AreaGeometryStateKey);
		Assert.True(block.IsNotNull);
		Assert.True(MuiAreaGeometryStateFieldCursorCodec.TryWriteInt32(ref platform,
			block, MuiAreaGeometryStateField.Width, -1));
		Assert.True(MuiAreaGeometryStateRecordCodec.TryReadStructural(ref platform,
			block, out var structural));
		Assert.Equal(-1, structural.Width);
		Assert.False(MuiAreaGeometryStateAdmission.Validate(structural));
		Assert.False(MuiAreaGeometryStateRecordCodec.TryRead(ref platform, block,
			out _));
		Assert.True(MuiAreaGeometryStateFieldCursorCodec.TryReadInt32(ref platform,
			block, MuiAreaGeometryStateField.Width, out var malformedWidth));
		Assert.Equal(-1, malformedWidth);

		Assert.False(MuiAreaLayoutCore.TryGetGeometryStateRecord(ref platform, State,
			area, out _));
		Assert.False(MuiAreaLayoutCore.TryReadGeometryState(ref platform, State,
			area, out _));
		Assert.False(MuiCommonControlCore.TryGet(ref platform, State, area, Width,
			out _, out _));
		Assert.False(MuiAreaLayoutCore.Layout(ref platform, State, area, 4, 6,
			20, 10));
		Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State, area,
			Width, out var rawWidth));
		Assert.Equal(20u, rawWidth);

		var renderInfo = APTR.FromPointer(0x1300);
		var rastPort = APTR.FromPointer(0x1400);
		platform.WriteUInt32(renderInfo, 20, rastPort.Raw);
		Assert.True(MuiAreaLayoutCore.Setup(ref platform, State, area, renderInfo));
		Assert.True(MuiAreaLayoutCore.Show(ref platform, State, area));
		Assert.False(MuiAreaLayoutCore.Draw(ref platform, State, area, 0));
		Assert.Equal(0u, platform.FillCount);
		Assert.Equal(0u, platform.LineCount);

		Assert.True(MuiAreaGeometryStateFieldCursorCodec.TryReadInt32(ref platform,
			block, MuiAreaGeometryStateField.Width, out malformedWidth));
		Assert.Equal(-1, malformedWidth);
	}

	[Fact]
	public void GeometryAdmissionRequiresCanonicalEdgesAndLiveOwner()
	{
		var platform = CreatePlatform(out var cl);
		var area = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		var valid = new MuiAreaGeometryStateRecord
		{
			Magic = MuiAreaGeometryStateRecord.Cookie,
			Left = -4,
			Top = -2,
			Width = 25,
			Height = 12,
			Right = 20,
			Bottom = 9,
		};
		Assert.True(MuiAreaGeometryStateAdmission.Validate(valid));
		Assert.True(MuiAreaGeometryStateAdmission.ValidateLive(ref platform, State,
			area, valid));
		var malformed = valid;
		malformed.Width = -1;
		Assert.False(MuiAreaGeometryStateAdmission.Validate(malformed));
		malformed = valid;
		malformed.Right = 21;
		Assert.False(MuiAreaGeometryStateAdmission.Validate(malformed));
		malformed = valid;
		malformed.Left = int.MaxValue;
		malformed.Width = 2;
		Assert.False(MuiAreaGeometryStateAdmission.Validate(malformed));
		Assert.False(MuiAreaGeometryStateAdmission.ValidateLive(ref platform, State,
			APTR.FromPointer(0xDEAD), valid));
	}

	[Fact]
	public void AreaGeometryRecordUsesDedicatedStructMemoryAdapter()
	{
		var platform = CreatePlatform(out _);
		var address = APTR.FromPointer(0x1A80);
		var value = new MuiAreaGeometryStateRecord
		{
			Magic = MuiAreaGeometryStateRecord.Cookie,
			Left = -4,
			Top = -2,
			Width = 25,
			Height = 12,
			Right = 20,
			Bottom = 9,
		};

		Assert.True(MuiAreaGeometryStateRecordCodec.Write(ref platform, address,
			value));
		Assert.True(MuiAreaGeometryStateRecordCodec.TryReadStructural(ref platform,
			address, out var structural));
		Assert.Equal(value.Magic, structural.Magic);
		Assert.Equal(value.Left, structural.Left);
		Assert.Equal(value.Top, structural.Top);
		Assert.Equal(value.Width, structural.Width);
		Assert.Equal(value.Height, structural.Height);
		Assert.Equal(value.Right, structural.Right);
		Assert.Equal(value.Bottom, structural.Bottom);
		Assert.True(MuiAreaGeometryStateRecordCodec.TryRead(ref platform, address,
			out _));
		Assert.True(MuiAreaGeometryStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, 24, out var lastField));
		Assert.Equal(address.Raw + 24, lastField.Raw);
		Assert.True(MuiAreaGeometryStateRecordMemoryCodec.TryReadInt32(
			ref platform, address, 4, out var left));
		Assert.Equal(value.Left, left);
		Assert.False(MuiAreaGeometryStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiAreaGeometryStateRecord.Size, out _));
		Assert.False(MuiAreaGeometryStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, 0, out _));
		Assert.False(MuiAreaGeometryStateRecordCodec.TryReadStructural(ref platform,
			APTR.Null, out _));
	}

	[Fact]
	public void AreaGeometrySequentialRecordPreservesValuesAndBounds()
	{
		var platform = CreatePlatform(out _);
		var address = APTR.FromPointer(0x1B00);
		var value = new MuiAreaGeometryStateRecord
		{
			Magic = MuiAreaGeometryStateRecord.Cookie,
			Left = int.MinValue,
			Top = int.MaxValue,
			Width = -1,
			Height = 0x01020304,
			Right = int.MinValue,
			Bottom = int.MaxValue,
		};

		Assert.True(MuiAreaGeometryStateRecordCodec.WriteRecord(ref platform,
			address, value));
		Assert.True(MuiAreaGeometryStateRecordCodec.TryReadRecord(ref platform,
			address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.Left, decoded.Left);
		Assert.Equal(value.Top, decoded.Top);
		Assert.Equal(value.Width, decoded.Width);
		Assert.Equal(value.Height, decoded.Height);
		Assert.Equal(value.Right, decoded.Right);
		Assert.Equal(value.Bottom, decoded.Bottom);

		var crossingEnd = APTR.FromPointer(0x30FE5);
		Assert.False(MuiAreaGeometryStateRecordCodec.WriteRecord(ref platform,
			crossingEnd, value));
		Assert.False(MuiAreaGeometryStateRecordCodec.TryReadRecord(ref platform,
			crossingEnd, out _));
	}

	[Fact]
	public void AreaGeometryUsesNamedGuestRecordAndReconcilesPublicProjection()
	{
		var platform = CreatePlatform(out var cl);
		var area = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		Assert.True(MuiAreaLayoutCore.Layout(ref platform, State, area, -4, -2,
			25, 12));

		Assert.True(MuiAreaLayoutCore.TryGetGeometryStateRecord(ref platform, State,
			area, out var record));
		Assert.Equal(MuiAreaGeometryStateRecord.Cookie, record.Magic);
		Assert.Equal(-4, record.Left);
		Assert.Equal(-2, record.Top);
		Assert.Equal(25, record.Width);
		Assert.Equal(12, record.Height);
		Assert.Equal(20, record.Right);
		Assert.Equal(9, record.Bottom);

		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, area,
			LeftEdge, unchecked((uint)-8), false));
		Assert.True(MuiAreaLayoutCore.TryReadGeometryState(ref platform, State, area,
			out var state));
		Assert.Equal(-8, state.Left);
		Assert.True(MuiAreaLayoutCore.TryGetGeometryStateRecord(ref platform, State,
			area, out record));
		Assert.Equal(-8, record.Left);
	}

	[Fact]
	public void MinMaxCodecUsesNamedFields()
	{
		Assert.Equal(12, System.Runtime.InteropServices.Marshal.SizeOf<
			MuiMinMaxValues>());
		Assert.Equal(0, System.Runtime.InteropServices.Marshal.OffsetOf<
			MuiMinMaxValues>(nameof(MuiMinMaxValues.MinWidth)).ToInt32());
		Assert.Equal(2, System.Runtime.InteropServices.Marshal.OffsetOf<
			MuiMinMaxValues>(nameof(MuiMinMaxValues.MinHeight)).ToInt32());
		Assert.Equal(4, System.Runtime.InteropServices.Marshal.OffsetOf<
			MuiMinMaxValues>(nameof(MuiMinMaxValues.MaxWidth)).ToInt32());
		Assert.Equal(6, System.Runtime.InteropServices.Marshal.OffsetOf<
			MuiMinMaxValues>(nameof(MuiMinMaxValues.MaxHeight)).ToInt32());
		Assert.Equal(8, System.Runtime.InteropServices.Marshal.OffsetOf<
			MuiMinMaxValues>(nameof(MuiMinMaxValues.DefWidth)).ToInt32());
		Assert.Equal(10, System.Runtime.InteropServices.Marshal.OffsetOf<
			MuiMinMaxValues>(nameof(MuiMinMaxValues.DefHeight)).ToInt32());
		var platform = CreatePlatform(out _);
		var address = APTR.FromPointer(0x1800);
		var value = default(MuiMinMaxValues);
		value.MinWidth = -2;
		value.MinHeight = 3;
		value.MaxWidth = 10000;
		value.MaxHeight = 20000;
		value.DefWidth = 640;
		value.DefHeight = 480;
		Assert.True(MuiMinMaxRecordCodec.WriteRecord(ref platform, address, value));
		Assert.True(MuiMinMaxRecordCodec.TryReadRecord(ref platform, address,
			out var decoded));
		Assert.Equal(value.MinWidth, decoded.MinWidth);
		Assert.Equal(value.MinHeight, decoded.MinHeight);
		Assert.Equal(value.MaxWidth, decoded.MaxWidth);
		Assert.Equal(value.MaxHeight, decoded.MaxHeight);
		Assert.Equal(value.DefWidth, decoded.DefWidth);
		Assert.Equal(value.DefHeight, decoded.DefHeight);
	}

	[Fact]
	public void MinMaxMemoryAdapterUsesNamedSignedBoundary()
	{
		var platform = CreatePlatform(out _);
		var record = APTR.FromPointer(0x1a00);
		var fields = new[]
		{
			MuiMinMaxField.MinWidth,
			MuiMinMaxField.MinHeight,
			MuiMinMaxField.MaxWidth,
			MuiMinMaxField.MaxHeight,
			MuiMinMaxField.DefWidth,
			MuiMinMaxField.DefHeight,
		};
		var offsets = new[]
		{
			MuiMinMaxValues.MinWidthOffset,
			MuiMinMaxValues.MinHeightOffset,
			MuiMinMaxValues.MaxWidthOffset,
			MuiMinMaxValues.MaxHeightOffset,
			MuiMinMaxValues.DefWidthOffset,
			MuiMinMaxValues.DefHeightOffset,
		};
		for (var i = 0; i < fields.Length; i++)
		{
			Assert.True(MuiMinMaxMemoryCodec.TryGetAddress(ref platform, record,
				fields[i], out var address));
			Assert.Equal(record.Raw + offsets[i], address.Raw);
			Assert.True(MuiMinMaxMemoryCodec.TryWrite(ref platform, record,
				fields[i], (short)(-10 + i)));
		}
		Assert.True(MuiMinMaxMemoryCodec.TryRead(ref platform, record,
			MuiMinMaxField.MaxHeight, out var maxHeight));
		Assert.Equal((short)-7, maxHeight);
		Assert.True(MuiMinMaxRecordCodec.TryReadRecord(ref platform, record,
			out var decoded));
		Assert.Equal((short)-10, decoded.MinWidth);
		Assert.Equal((short)-5, decoded.DefHeight);
		var cursor = new MuiMinMaxFieldCursor
		{
			Record = record,
			Field = MuiMinMaxField.DefHeight,
		};
		Assert.True(MuiMinMaxFieldCursorCodec.TryGetAddress(ref platform, cursor,
			out var compatibilityAddress));
		Assert.Equal(record.Raw + MuiMinMaxValues.DefHeightOffset,
			compatibilityAddress.Raw);
		Assert.False(MuiMinMaxMemoryCodec.TryGetAddress(ref platform, record,
			(MuiMinMaxField)255, out _));
		Assert.False(MuiMinMaxMemoryCodec.TryGetAddress(ref platform,
			APTR.FromPointer(0xfffffff0u), MuiMinMaxField.DefHeight, out _));
	}

	[Fact]
	public void HorizontalGroupDistributesSpaceByWeightAndSpacing()
	{
		var platform = CreatePlatform(out var cl);
		var group = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		var first = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		var second = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, group, first));
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, group, second));
		Set(ref platform, group, Horizontal, 1);
		Set(ref platform, group, HorizontalSpacing, 4);
		Set(ref platform, first, HorizontalWeight, 1);
		Set(ref platform, second, HorizontalWeight, 3);
		Assert.True(MuiGroupLayoutCore.Layout(ref platform, State, group, 5, 7,
			104, 20));
		Assert.Equal(5u, Get(ref platform, first, LeftEdge));
		Assert.Equal(25u, Get(ref platform, first, Width));
		Assert.Equal(34u, Get(ref platform, second, LeftEdge));
		Assert.Equal(75u, Get(ref platform, second, Width));
	}

	[Fact]
	public void GridGroupUsesColumnsRowsSpacingAndCenterAlignment()
	{
		var platform = CreatePlatform(out var cl);
		var group = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		var first = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		var second = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		var third = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		var fourth = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, group, first));
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, group, second));
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, group, third));
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, group, fourth));
		Assert.Equal(third, MuiFamilyCore.GetChild(ref platform, State, group, 2,
			APTR.Null));
		Assert.Equal(fourth, MuiFamilyCore.GetChild(ref platform, State, group, 3,
			APTR.Null));
		Set(ref platform, group, Columns, 2);
		Set(ref platform, group, HorizontalSpacing, 4);
		Set(ref platform, group, VerticalSpacing, 6);
		Set(ref platform, group, HorizontalCenter, 2);
		Set(ref platform, group, VerticalCenter, 2);
		foreach (var child in new[] { first, second, third, fourth })
		{
			Set(ref platform, child, FixWidth, 20);
			Set(ref platform, child, FixHeight, 10);
		}

		Assert.True(MuiGroupLayoutCore.Layout(ref platform, State, group, 5, 7,
			100, 60));
		Assert.Equal(33u, Get(ref platform, first, LeftEdge));
		Assert.Equal(24u, Get(ref platform, first, TopEdge));
		Assert.Equal(20u, Get(ref platform, first, Width));
		Assert.Equal(10u, Get(ref platform, first, Height));
		Assert.Equal(85u, Get(ref platform, second, LeftEdge));
		Assert.Equal(24u, Get(ref platform, second, TopEdge));
		Assert.Equal(33u, Get(ref platform, third, LeftEdge));
		Assert.Equal(57u, Get(ref platform, third, TopEdge));
		Assert.Equal(85u, Get(ref platform, fourth, LeftEdge));
		Assert.Equal(57u, Get(ref platform, fourth, TopEdge));
	}

	[Fact]
	public void GridRowsDeriveColumnsAndComputeColumnAndRowMinimums()
	{
		var platform = CreatePlatform(out var cl);
		var group = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		var first = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		var second = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		var third = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		var fourth = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, group, first));
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, group, second));
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, group, third));
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, group, fourth));
		Set(ref platform, group, Rows, 2);
		Set(ref platform, group, HorizontalSpacing, 4);
		Set(ref platform, group, VerticalSpacing, 6);
		var widths = new[] { 20u, 30u, 40u, 50u };
		var heights = new[] { 10u, 15u, 20u, 25u };
		var children = new[] { first, second, third, fourth };
		for (var index = 0; index < children.Length; index++)
		{
			Set(ref platform, children[index], FixWidth, widths[index]);
			Set(ref platform, children[index], FixHeight, heights[index]);
		}
		var childCount = 0;
		while (MuiFamilyCore.GetChild(ref platform, State, group, childCount,
			APTR.Null).IsNotNull) childCount++;
		Assert.Equal(4, childCount);

		var storage = APTR.FromPointer(0x1200);
		Assert.True(MuiGroupLayoutCore.AskMinMax(ref platform, State, group,
			storage));
		Assert.Equal((ushort)94, platform.ReadUInt16(storage, 0));
		Assert.Equal((ushort)46, platform.ReadUInt16(storage, 2));
		Assert.Equal((ushort)54, platform.ReadUInt16(storage, 4));
		Assert.Equal((ushort)36, platform.ReadUInt16(storage, 6));
		Assert.Equal((ushort)94, platform.ReadUInt16(storage, 8));
		Assert.Equal((ushort)46, platform.ReadUInt16(storage, 10));
	}

	[Fact]
	public void GridSameSizeKeepsWeightedColumnsEqual()
	{
		var platform = CreatePlatform(out var cl);
		var group = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		var first = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		var second = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, group, first));
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, group, second));
		Set(ref platform, group, Columns, 2);
		Set(ref platform, group, SameSize, 1);
		Set(ref platform, first, HorizontalWeight, 1);
		Set(ref platform, second, HorizontalWeight, 3);
		Set(ref platform, first, FixWidth, 10);
		Set(ref platform, second, FixWidth, 10);

		Assert.True(MuiGroupLayoutCore.Layout(ref platform, State, group, 0, 0,
			100, 20));
		Assert.Equal(20u, Get(ref platform, first, LeftEdge));
		Assert.Equal(70u, Get(ref platform, second, LeftEdge));
		Assert.Equal(10u, Get(ref platform, first, Width));
		Assert.Equal(10u, Get(ref platform, second, Width));
	}

	[Fact]
	public void GroupLayoutHookReceivesTypedChildListAndControlsMinMaxAndLayout()
	{
		var platform = CreatePlatform(out var cl);
		var groupName = APTR.FromPointer(0x1120);
		platform.WriteCString(groupName, "Group.mui");
		var groupClass = MuiHeadlessObjectCore.RegisterClass(ref platform, State,
			groupName, APTR.Null, 0, APTR.FromPointer(2), false);
		var group = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			groupClass, APTR.Null);
		var child = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, group, child));
		var hook = APTR.FromPointer(0x2800);
		platform.WriteUInt32(hook, 8, HookEntryGroupLayout);
		Set(ref platform, group, LayoutHook, hook.Raw);
		Assert.Equal(hook.Raw, Get(ref platform, group, LayoutHook));
		var groupRecord = MuiHeadlessObjectCore.FindObject(ref platform, State,
			group);
		Assert.True(MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, State,
			groupRecord, LayoutHook, 0, false));
		// The named hook state remains authoritative after a raw compatibility
		// write, so layout dispatch and OM_GET do not depend on an attribute slot.
		Assert.Equal(hook.Raw, Get(ref platform, group, LayoutHook));
		var hookGetMessage = APTR.FromPointer(0x1600);
		var hookGetStorage = APTR.FromPointer(0x1700);
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			hookGetMessage, MuiCommonPacketKind.Get, MuiCommonField.MethodId,
			MuiCommonControlPacketCore.OmGet));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			hookGetMessage, MuiCommonPacketKind.Get, MuiCommonField.Attribute,
			LayoutHook));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
			hookGetMessage, MuiCommonPacketKind.Get, MuiCommonField.Storage,
			hookGetStorage.Raw));
		Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			group, hookGetMessage));
		Assert.True(MuiGuestUlongStorageCodec.TryRead(ref platform,
			hookGetStorage, out var hookStored));
		Assert.Equal(hook.Raw, hookStored.Value);
		var hookSetMessage = APTR.FromPointer(0x1800);
		Assert.True(MuiCommonControlPacketCore.WriteAttribute(ref platform,
			hookSetMessage, MuiCommonControlPacketCore.NoNotifySet, LayoutHook,
			hook.Raw));
		Assert.Equal(0u, MuiCommonControlDispatcher.Dispatch(ref platform, State,
			group, hookSetMessage));

		var minMax = APTR.FromPointer(0x1200);
		Assert.True(MuiGroupLayoutCore.AskMinMax(ref platform, State, group,
			minMax));
		Assert.Equal((ushort)13, platform.ReadUInt16(minMax, 0));
		Assert.Equal((ushort)17, platform.ReadUInt16(minMax, 2));
		Assert.Equal((ushort)101, platform.ReadUInt16(minMax, 4));
		Assert.Equal((ushort)107, platform.ReadUInt16(minMax, 6));
		Assert.Equal((ushort)31, platform.ReadUInt16(minMax, 8));
		Assert.Equal((ushort)37, platform.ReadUInt16(minMax, 10));
		Assert.Equal(1u, platform.LayoutHookMinMaxCount);
		Assert.Equal(group, platform.LastHookA2);
		Assert.Equal(child, platform.LastLayoutHookFirstChild);
		Assert.True(platform.LastLayoutHookChildren.IsNotNull);

		Set(ref platform, child, LeftEdge, 91);
		Set(ref platform, group, Horizontal, 1);
		Assert.True(MuiGroupLayoutCore.Layout(ref platform, State, group, 5, 7,
			100, 40));
		Assert.Equal(1u, platform.LayoutHookLayoutCount);
		Assert.Equal(91u, Get(ref platform, child, LeftEdge));
		Assert.True(MuiGroupLayoutCore.TryGetLayoutState(ref platform, State, group,
			out var policy));
		Assert.Equal(MuiGroupLayoutPolicyStateRecord.Cookie, policy.Magic);
		Assert.Equal(1u, policy.Horizontal);
	}

	[Fact]
	public void MalformedLayoutHookStateFailsClosedBeforeGetterAndLayout()
	{
		var platform = CreatePlatform(out var cl);
		var groupName = APTR.FromPointer(0x1120);
		platform.WriteCString(groupName, "Group.mui");
		var groupClass = MuiHeadlessObjectCore.RegisterClass(ref platform, State,
			groupName, APTR.Null, 0, APTR.FromPointer(2), false);
		var group = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			groupClass, APTR.Null);
		var child = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, group, child));
		var hook = APTR.FromPointer(0x2800);
		platform.WriteUInt32(hook, 8, HookEntryGroupLayout);
		Set(ref platform, group, LayoutHook, hook.Raw);

		var storage = APTR.FromPointer(0x1200);
		Assert.True(MuiGroupLayoutCore.AskMinMax(ref platform, State, group,
			storage));
		Set(ref platform, child, LeftEdge, 77);
		Assert.True(MuiGroupLayoutCore.Layout(ref platform, State, group, 5, 7,
			100, 40));
		Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State,
			child, LeftEdge, out var beforeLeft));
		var beforeMinMaxCount = platform.LayoutHookMinMaxCount;
		var beforeLayoutCount = platform.LayoutHookLayoutCount;
		Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State,
			group, LayoutHookStateAttribute, out var stateRaw));
		var stateBlock = APTR.FromPointer(stateRaw);
		var cursor = default(MuiGroupLayoutHookStateFieldCursor);
		cursor.Record = stateBlock;
		cursor.Field = MuiGroupLayoutHookStateField.Magic;
		Assert.True(MuiGroupLayoutHookStateFieldCursorCodec.TryWriteUInt32(
			ref platform, stateBlock, cursor.Field, 0));

		Assert.False(MuiHeadlessObjectCore.GetAttribute(ref platform, State, group,
			LayoutHook, out _));
		Assert.False(MuiGroupLayoutHookCore.TryGetAttribute(ref platform, State,
			group, LayoutHook, out _));
		Assert.False(MuiHeadlessObjectCore.SetAttribute(ref platform, State, group,
			LayoutHook, hook.Raw, false));
		Assert.False(MuiGroupLayoutCore.AskMinMax(ref platform, State, group,
			storage));
		Assert.False(MuiGroupLayoutCore.Layout(ref platform, State, group, 5, 7,
			100, 40));
		Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State,
			child, LeftEdge, out var afterLeft));
		Assert.Equal(beforeLeft, afterLeft);
		Assert.Equal(beforeMinMaxCount, platform.LayoutHookMinMaxCount);
		Assert.Equal(beforeLayoutCount, platform.LayoutHookLayoutCount);
		Assert.True(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State,
			group, LayoutHook, out var rawHook));
		Assert.Equal(hook.Raw, rawHook);
		Assert.True(MuiGroupLayoutHookStateRecordCodec.TryReadStructural(
			ref platform, stateBlock, out var malformed));
		Assert.Equal(0u, malformed.Magic);
		Assert.False(MuiGroupLayoutHookStateAdmission.Validate(malformed));
		Assert.False(MuiGroupLayoutHookStateAdmission.ValidateLive(ref platform,
			State, group, malformed));
	}

	[Fact]
	public void LayoutHookStateAdmissionRequiresCookieAndLiveOwner()
	{
		var platform = CreatePlatform(out var cl);
		var groupName = APTR.FromPointer(0x1120);
		platform.WriteCString(groupName, "Group.mui");
		var groupClass = MuiHeadlessObjectCore.RegisterClass(ref platform, State,
			groupName, APTR.Null, 0, APTR.FromPointer(2), false);
		var group = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			groupClass, APTR.Null);
		var valid = default(MuiGroupLayoutHookStateRecord);
		valid.Magic = MuiGroupLayoutHookStateRecord.Cookie;
		valid.Hook = APTR.FromPointer(0x2800);
		Assert.True(MuiGroupLayoutHookStateAdmission.Validate(valid));
		Assert.True(MuiGroupLayoutHookStateAdmission.ValidateLive(ref platform,
			State, group, valid));
		var malformed = valid;
		malformed.Magic = 0;
		Assert.False(MuiGroupLayoutHookStateAdmission.Validate(malformed));
		Assert.False(MuiGroupLayoutHookStateAdmission.ValidateLive(ref platform,
			State, group, malformed));
		Assert.False(MuiGroupLayoutHookStateAdmission.ValidateLive(ref platform,
			State, APTR.FromPointer(0xDEAD), valid));
	}

	[Fact]
	public void LayoutHookRecordUsesDedicatedStructMemoryAdapter()
	{
		var platform = CreatePlatform(out _);
		var address = APTR.FromPointer(0x1D20);
		var value = new MuiGroupLayoutHookStateRecord
		{
			Magic = MuiGroupLayoutHookStateRecord.Cookie,
			Hook = APTR.FromPointer(0x1D80),
		};
		Assert.True(MuiGroupLayoutHookStateRecordCodec.Write(ref platform, address,
			value));
		Assert.True(MuiGroupLayoutHookStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiGroupLayoutHookStateField.Hook,
			out var hookAddress));
		Assert.Equal(address.Raw + MuiGroupLayoutHookStateRecord.HookOffset,
			hookAddress.Raw);
		Assert.True(MuiGroupLayoutHookStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, MuiGroupLayoutHookStateField.Hook, out var hook));
		Assert.Equal(0x1D80u, hook);
		Assert.True(MuiGroupLayoutHookStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiGroupLayoutHookStateField.Hook, 0));
		Assert.True(MuiGroupLayoutHookStateRecordCodec.TryReadStructural(ref platform,
			address, out var decoded));
		Assert.True(decoded.Hook.IsNull);
		Assert.False(MuiGroupLayoutHookStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, (MuiGroupLayoutHookStateField)255, out _));
		Assert.False(MuiGroupLayoutHookStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiGroupLayoutHookStateField.Magic, out _));
		Assert.False(MuiGroupLayoutHookStateRecordCodec.TryReadStructural(
			ref platform, APTR.Null, out _));
	}

	[Fact]
	public void ExactLayoutPacketsRouteAndScheduleRedraw()
	{
		var platform = CreatePlatform(out var cl);
		var area = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		var packet = APTR.FromPointer(0x1200);
		var renderInfo = APTR.FromPointer(0x1300);
		var rastPort = APTR.FromPointer(0x1400);
		var text = APTR.FromPointer(0x1500);
		platform.WriteCString(text, "abc");
		platform.WriteUInt32(renderInfo, 20, rastPort.Raw);

		platform.WriteUInt32(packet, 0, 0x80428354);
		platform.WriteUInt32(packet, 4, renderInfo.Raw);
		Assert.Equal(1u, MuiLayoutDispatcher.Dispatch(ref platform, State, area,
			packet));
		platform.WriteUInt32(packet, 0, 0x8042845B);
		platform.WriteUInt32(packet, 4, 3);
		platform.WriteUInt32(packet, 8, 4);
		platform.WriteUInt32(packet, 12, 30);
		platform.WriteUInt32(packet, 16, 15);
		platform.WriteUInt32(packet, 20, 0);
		Assert.Equal(1u, MuiLayoutDispatcher.Dispatch(ref platform, State, area,
			packet));
		platform.WriteUInt32(packet, 0, 0x8042B381);
		platform.WriteUInt32(packet, 4, 2);
		Assert.Equal(1u, MuiLayoutDispatcher.Dispatch(ref platform, State, area,
			packet));
		Assert.Equal(1u, platform.RedrawCount);
		var preParse = APTR.FromPointer(0x1600);
		platform.WriteCString(preParse, "\u001bP[112233]");
		Set(ref platform, area, Unicode, 1);
		platform.WriteUInt32(packet, 0, MuiLayoutPacketCore.Text);
		platform.WriteUInt32(packet, 4, 5);
		platform.WriteUInt32(packet, 8, 6);
		platform.WriteUInt32(packet, 12, 40);
		platform.WriteUInt32(packet, 16, 12);
		platform.WriteUInt32(packet, 20, text.Raw);
		platform.WriteUInt32(packet, 24, 3);
		platform.WriteUInt32(packet, 28, preParse.Raw);
		platform.WriteUInt32(packet, 32, 0xA5u);
		Assert.Equal(1u, MuiLayoutDispatcher.Dispatch(ref platform, State, area,
			packet));
		Assert.Equal(1u, platform.MuiTextMethodApplyCount);
		var textRequest = platform.LastMuiTextMethodRequest;
		Assert.Equal(area, textRequest.Object);
		Assert.Equal(rastPort, textRequest.RastPort);
		Assert.Equal(text, textRequest.Text);
		Assert.Equal(preParse, textRequest.PreParse);
		Assert.Equal(5, textRequest.Left);
		Assert.Equal(6, textRequest.Top);
		Assert.Equal(40, textRequest.Width);
		Assert.Equal(12, textRequest.Height);
		Assert.Equal(3, textRequest.Length);
		Assert.Equal(0xA5u, textRequest.Flags);
		Assert.Equal(1u, textRequest.Unicode);
		Assert.Equal(1u, textRequest.Present);
		Assert.Equal(1u, platform.MuiTextInlineColorApplyCount);
		var colorRequest = platform.LastMuiTextInlineColorRequest;
		Assert.Equal(area, colorRequest.Object);
		Assert.Equal(rastPort, colorRequest.RastPort);
		Assert.Equal(0x00112233u, colorRequest.Color);
		Assert.Equal(0u, colorRequest.Alpha);
		Assert.Equal(MuiTextInlineColorFlags.HasColor, colorRequest.Flags);
		Assert.Equal(1u, colorRequest.Present);
		platform.WriteUInt32(packet, 0, 0x80422AD7);
		var unicodeText = APTR.FromPointer(0x1700);
		var utf8 = new byte[] { 0xC3, 0x85, 0xCE, 0xB2, 0xF0, 0x9F, 0x99, 0x82 };
		for (var i = 0; i < utf8.Length; i++)
			platform.WriteUInt8(unicodeText, i, utf8[i]);
		platform.WriteUInt8(unicodeText, utf8.Length, 0);
		platform.WriteUInt32(packet, 4, unicodeText.Raw);
		platform.WriteUInt32(packet, 8, 8);
		platform.WriteUInt32(packet, 12, preParse.Raw);
		platform.WriteUInt32(packet, 16, 0x5Au);
		Assert.Equal(0x00080018u, MuiLayoutDispatcher.Dispatch(ref platform, State,
			area, packet));
		Assert.Equal(1u, platform.MuiTextDimensionApplyCount);
		var dimensionRequest = platform.LastMuiTextDimensionRequest;
		Assert.Equal(area, dimensionRequest.Object);
		Assert.Equal(rastPort, dimensionRequest.RastPort);
		Assert.Equal(unicodeText, dimensionRequest.Text);
		Assert.Equal(preParse, dimensionRequest.PreParse);
		Assert.Equal(8, dimensionRequest.Length);
		Assert.Equal(0x5Au, dimensionRequest.Flags);
		Assert.Equal(1u, dimensionRequest.Unicode);
		Assert.Equal(1u, dimensionRequest.Present);

		var multilineText = APTR.FromPointer(0x1800);
		var multilineUtf8 = new byte[]
		{
			0xC3, 0x85, 0x0A, 0xCE, 0xB2, 0xF0, 0x9F, 0x99, 0x82,
		};
		for (var i = 0; i < multilineUtf8.Length; i++)
			platform.WriteUInt8(multilineText, i, multilineUtf8[i]);
		platform.WriteUInt8(multilineText, multilineUtf8.Length, 0);
		platform.WriteUInt32(packet, 4, multilineText.Raw);
		platform.WriteUInt32(packet, 8, unchecked((uint)multilineUtf8.Length));
		Assert.Equal(0x00100010u, MuiLayoutDispatcher.Dispatch(ref platform,
			State, area, packet));
		Assert.Equal(2u, platform.MuiTextDimensionApplyCount);
		Assert.Equal(multilineText,
			platform.LastMuiTextDimensionRequest.Text);
	}

	[Fact]
	public void PlatformTextCapabilityRecordsUseNamedPackedLayouts()
	{
		Assert.Equal(20, System.Runtime.InteropServices.Marshal.SizeOf<
			MuiImageSpec>());
		Assert.Equal(0, System.Runtime.InteropServices.Marshal.OffsetOf<
			MuiImageSpec>(nameof(MuiImageSpec.Kind)).ToInt32());
		Assert.Equal(16, System.Runtime.InteropServices.Marshal.OffsetOf<
			MuiImageSpec>(nameof(MuiImageSpec.Blue)).ToInt32());

		Assert.Equal(56, System.Runtime.InteropServices.Marshal.SizeOf<
			MuiTextColorResolutionRequest>());
		Assert.Equal(20, System.Runtime.InteropServices.Marshal.OffsetOf<
			MuiTextColorResolutionRequest>(nameof(MuiTextColorResolutionRequest.CustomFontSpec)).ToInt32());
		Assert.Equal(8, System.Runtime.InteropServices.Marshal.SizeOf<
			MuiTextColorRenderRequest>());
		Assert.Equal(48, System.Runtime.InteropServices.Marshal.SizeOf<
			MuiCustomFontRenderRequest>());
		Assert.Equal(12, System.Runtime.InteropServices.Marshal.OffsetOf<
			MuiCustomFontRenderRequest>(nameof(MuiCustomFontRenderRequest.Spec)).ToInt32());
		Assert.Equal(20, System.Runtime.InteropServices.Marshal.SizeOf<
			MuiTextStyleRenderRequest>());
		Assert.Equal(24, System.Runtime.InteropServices.Marshal.SizeOf<
			MuiTextInlineColorRenderRequest>());
		Assert.Equal(48, System.Runtime.InteropServices.Marshal.SizeOf<
			MuiTextInlineImageRenderRequest>());
		Assert.Equal(8, System.Runtime.InteropServices.Marshal.OffsetOf<
			MuiTextInlineImageRenderRequest>(nameof(MuiTextInlineImageRenderRequest.Spec)).ToInt32());
		Assert.Equal(52, System.Runtime.InteropServices.Marshal.SizeOf<
			MuiTextMethodRenderRequest>());
		Assert.Equal(44, System.Runtime.InteropServices.Marshal.OffsetOf<
			MuiTextMethodRenderRequest>(nameof(MuiTextMethodRenderRequest.Unicode)).ToInt32());
		Assert.Equal(44, System.Runtime.InteropServices.Marshal.SizeOf<
			MuiTextDimensionRequest>());
		Assert.Equal(32, System.Runtime.InteropServices.Marshal.OffsetOf<
			MuiTextDimensionRequest>(nameof(MuiTextDimensionRequest.Width)).ToInt32());
	}

	[Fact]
	public void LayoutMethodHeaderUsesNamedField()
	{
		var platform = CreatePlatform(out _);
		var packet = APTR.FromPointer(0x1200);
		platform.WriteUInt32(packet, 0, MuiLayoutPacketCore.Layout);
		Assert.True(MuiLayoutPacketCodec.TryReadMethodId(ref platform, packet,
			out var header));
		Assert.Equal(MuiLayoutPacketCore.Layout, header.MethodId);
		Assert.False(MuiLayoutPacketCodec.TryReadMethodId(ref platform,
			APTR.Null, out _));
	}

	[Fact]
	public void LayoutMethodHeaderUsesSharedUlongStorageBoundary()
	{
		var platform = CreatePlatform(out _);
		var packet = APTR.FromPointer(0x1200);
		const uint methodId = 0xC2468ACEu;
		Assert.True(MuiLayoutMethodHeaderCodec.WriteValue(ref platform, packet,
			methodId));
		Assert.True(MuiLayoutMethodHeaderCodec.TryReadValue(ref platform, packet,
			out var readMethodId));
		Assert.Equal(methodId, readMethodId);
		Assert.False(MuiLayoutMethodHeaderCodec.TryReadValue(ref platform,
			APTR.FromPointer(0x20FFF), out _));
		Assert.False(MuiLayoutMethodHeaderCodec.WriteValue(ref platform,
			APTR.Null, methodId));
	}

	[Fact]
	public void LayoutTypedReadersUseNamedMethodHeader()
	{
		var platform = CreatePlatform(out _);
		var packet = APTR.FromPointer(0x1200);
		platform.WriteUInt32(packet, 0, MuiLayoutPacketCore.Layout);
		platform.WriteUInt32(packet, 4, 3);
		platform.WriteUInt32(packet, 8, 4);
		platform.WriteUInt32(packet, 12, 20);
		platform.WriteUInt32(packet, 16, 10);
		platform.WriteUInt32(packet, 20, 0x100);
		Assert.True(MuiLayoutPacketCodec.TryReadLayout(ref platform, packet,
			out var layout));
		Assert.Equal(MuiLayoutPacketCore.Layout, layout.MethodId);

		platform.WriteUInt32(packet, 0, MuiLayoutPacketCore.Relayout);
		Assert.False(MuiLayoutPacketCodec.TryReadLayout(ref platform, packet,
			out _));
		Assert.True(MuiLayoutPacketCodec.TryReadRelayout(ref platform, packet,
			out var relayout));
		Assert.Equal(MuiLayoutPacketCore.Relayout, relayout.MethodId);
	}

	[Fact]
	public void LayoutFieldCursorUsesNamedMixedPacketBoundaries()
	{
		var platform = CreatePlatform(out _);
		var packet = APTR.FromPointer(0x1200);
		var cursor = default(MuiLayoutFieldCursor);
		cursor.Message = packet;
		cursor.Packet = MuiLayoutPacketKind.Layout;
		cursor.Field = MuiLayoutField.MethodId;
		Assert.True(MuiLayoutFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out var address));
		Assert.Equal(packet.Raw, address.Raw);
		cursor.Field = MuiLayoutField.Left;
		Assert.True(MuiLayoutFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out address));
		Assert.Equal(packet.Raw + 4, address.Raw);
		cursor.Field = MuiLayoutField.Flags;
		Assert.True(MuiLayoutFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out address));
		Assert.Equal(packet.Raw + 20, address.Raw);

		Assert.True(MuiLayoutFieldCursorCodec.TryReadUInt32(ref platform,
			packet, MuiLayoutPacketKind.Rectangle, MuiLayoutField.Reserved2,
			out var reserved));
		Assert.Equal(0u, reserved);

		cursor.Packet = MuiLayoutPacketKind.Method;
		cursor.Field = MuiLayoutField.Flags;
		Assert.False(MuiLayoutFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out _));
		cursor.Message = APTR.FromPointer(0xFFFFFFF0u);
		cursor.Packet = MuiLayoutPacketKind.Text;
		cursor.Message = packet;
		cursor.Field = MuiLayoutField.PreParse;
		Assert.True(MuiLayoutFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out address));
		Assert.Equal(packet.Raw + 28, address.Raw);
		cursor.Field = MuiLayoutField.TextFlags;
		Assert.True(MuiLayoutFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out address));
		Assert.Equal(packet.Raw + 32, address.Raw);
		cursor.Packet = MuiLayoutPacketKind.TextDimensions;
		cursor.Message = packet;
		cursor.Field = MuiLayoutField.PreParse;
		Assert.True(MuiLayoutFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out address));
		Assert.Equal(packet.Raw + 12, address.Raw);
		cursor.Field = MuiLayoutField.TextFlags;
		Assert.True(MuiLayoutFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out address));
		Assert.Equal(packet.Raw + 16, address.Raw);
		cursor.Message = APTR.FromPointer(0xFFFFFFF0u);
		cursor.Field = MuiLayoutField.Reserved1;
		Assert.False(MuiLayoutFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out _));
	}

	[Fact]
	public void PublicLayoutServiceUsesScalarAndGuestPacketSeams()
	{
		var platform = CreatePlatform(out var cl);
		var area = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		Assert.True(MuiLayoutServiceCore.Layout(ref platform, State, area, 11, 13,
			30, 15, 0));
		Assert.Equal(11u, Get(ref platform, area, LeftEdge));
		Assert.Equal(30u, Get(ref platform, area, Width));

		var packet = APTR.FromPointer(0x1200);
		platform.WriteUInt32(packet, 0, 0x8042845B);
		platform.WriteUInt32(packet, 4, 3);
		platform.WriteUInt32(packet, 8, 4);
		platform.WriteUInt32(packet, 12, 20);
		platform.WriteUInt32(packet, 16, 10);
		platform.WriteUInt32(packet, 20, 0x100);
		Assert.Equal(1u, MuiLayoutServiceCore.Dispatch(ref platform, State, area,
			packet));
		Assert.Equal(3u, Get(ref platform, area, LeftEdge));
		Assert.Equal(20u, Get(ref platform, area, Width));

		Assert.False(MuiLayoutServiceCore.Layout(ref platform, State, area, 0, 0,
			-1, 10, 0));
	}

	[Fact]
	public void PublicLayoutServiceRejectsUnknownMethodThroughNamedPacketCodec()
	{
		var platform = CreatePlatform(out var cl);
		var area = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		var packet = APTR.FromPointer(0x1200);
		platform.WriteUInt32(packet, 0, 0xDEADBEEFu);
		Assert.Equal(0u, MuiLayoutServiceCore.Dispatch(ref platform, State, area,
			packet));
		Assert.Equal(0u, MuiLayoutServiceCore.Dispatch(ref platform, State, area,
			APTR.Null));
	}

	[Fact]
	public void PublicRedrawServiceValidatesObjectAndDrawFlags()
	{
		var platform = CreatePlatform(out var cl);
		var area = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		Assert.True(MuiRedrawServiceCore.Redraw(ref platform, State, area,
			MuiRedrawServiceCore.DrawObject));
		Assert.True(MuiRedrawServiceCore.Redraw(ref platform, State, area,
			MuiRedrawServiceCore.DrawUpdate));
		Assert.True(MuiRedrawServiceCore.Redraw(ref platform, State, area,
			MuiRedrawServiceCore.DrawObject | MuiRedrawServiceCore.DrawUpdate));
		Assert.Equal(3u, platform.RedrawCount);
		Assert.False(MuiRedrawServiceCore.Redraw(ref platform, State, area, 0));
		Assert.False(MuiRedrawServiceCore.Redraw(ref platform, State, area, 4));
		Assert.False(MuiRedrawServiceCore.Redraw(ref platform, State,
			APTR.FromPointer(0x1F000), MuiRedrawServiceCore.DrawObject));
		Assert.Equal(3u, platform.RedrawCount);
	}

	private static MuiHeadlessTestPlatform CreatePlatform(out APTR cl)
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var name = APTR.FromPointer(0x1100);
		platform.WriteCString(name, "Area.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		cl = MuiHeadlessObjectCore.RegisterClass(ref platform, State, name,
			APTR.Null, 0, APTR.FromPointer(1), false);
		return platform;
	}

	private static void Set(ref MuiHeadlessTestPlatform platform, APTR obj,
		uint attribute, uint value) => Assert.True(
		MuiHeadlessObjectCore.SetAttribute(ref platform, State, obj, attribute,
			value, false));

	private static uint Get(ref MuiHeadlessTestPlatform platform, APTR obj,
		uint attribute)
	{
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, obj,
			attribute, out var value));
		return value;
	}
}
