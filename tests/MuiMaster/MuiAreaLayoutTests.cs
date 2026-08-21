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
	private const uint LeftEdge = 0x8042BEC6;
	private const uint TopEdge = 0x8042509B;
	private const uint Width = 0x8042B59C;
	private const uint Height = 0x80423237;
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
		var platform = CreatePlatform(out _);
		var address = APTR.FromPointer(0x1800);
		var value = default(MuiMinMaxValues);
		value.MinWidth = -2;
		value.MinHeight = 3;
		value.MaxWidth = 10000;
		value.MaxHeight = 20000;
		value.DefWidth = 640;
		value.DefHeight = 480;
		Assert.True(MuiMinMaxRecordCodec.Write(ref platform, address, value));
		Assert.True(MuiMinMaxRecordCodec.TryRead(ref platform, address,
			out var decoded));
		Assert.Equal(value.MinWidth, decoded.MinWidth);
		Assert.Equal(value.MinHeight, decoded.MinHeight);
		Assert.Equal(value.MaxWidth, decoded.MaxWidth);
		Assert.Equal(value.MaxHeight, decoded.MaxHeight);
		Assert.Equal(value.DefWidth, decoded.DefWidth);
		Assert.Equal(value.DefHeight, decoded.DefHeight);
	}

	[Fact]
	public void MinMaxFieldCursorUsesNamedSignedBoundary()
	{
		var platform = CreatePlatform(out _);
		var record = APTR.FromPointer(0x1a00);
		var cursor = default(MuiMinMaxFieldCursor);
		cursor.Record = record;
		var fields = new[]
		{
			MuiMinMaxField.MinWidth,
			MuiMinMaxField.MinHeight,
			MuiMinMaxField.MaxWidth,
			MuiMinMaxField.MaxHeight,
			MuiMinMaxField.DefWidth,
			MuiMinMaxField.DefHeight,
		};
		for (var i = 0; i < fields.Length; i++)
		{
			cursor.Field = fields[i];
			Assert.True(MuiMinMaxFieldCursorCodec.TryGetAddress(ref platform,
				cursor, out var address));
			Assert.Equal(record.Raw + (uint)(i * 2), address.Raw);
			Assert.True(MuiMinMaxFieldCursorCodec.TryWrite(ref platform, record,
				fields[i], (short)(-10 + i)));
		}
		Assert.True(MuiMinMaxFieldCursorCodec.TryRead(ref platform, record,
			MuiMinMaxField.MaxHeight, out var maxHeight));
		Assert.Equal((short)-7, maxHeight);
		Assert.True(MuiMinMaxRecordCodec.TryRead(ref platform, record,
			out var decoded));
		Assert.Equal((short)-10, decoded.MinWidth);
		Assert.Equal((short)-5, decoded.DefHeight);
		cursor.Field = (MuiMinMaxField)255;
		Assert.False(MuiMinMaxFieldCursorCodec.TryGetAddress(ref platform, cursor,
			out _));
		cursor.Record = APTR.FromPointer(0xfffffff0u);
		cursor.Field = MuiMinMaxField.DefHeight;
		Assert.False(MuiMinMaxFieldCursorCodec.TryGetAddress(ref platform, cursor,
			out _));
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
		platform.WriteUInt32(packet, 0, 0x80422AD7);
		platform.WriteUInt32(packet, 4, text.Raw);
		platform.WriteUInt32(packet, 8, 3);
		platform.WriteUInt32(packet, 12, 0);
		platform.WriteUInt32(packet, 16, 0);
		Assert.Equal(0x00080018u, MuiLayoutDispatcher.Dispatch(ref platform, State,
			area, packet));
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
