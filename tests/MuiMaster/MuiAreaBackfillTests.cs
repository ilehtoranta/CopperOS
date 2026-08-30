using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiAreaBackfillTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);

	[Fact]
	public void RenderingCapabilityRequestsUseNamedPackedLayouts()
	{
		Assert.Equal(56, System.Runtime.InteropServices.Marshal.SizeOf<
			MuiDoubleBufferRenderRequest>());
		Assert.Equal(0, System.Runtime.InteropServices.Marshal.OffsetOf<
			MuiDoubleBufferRenderRequest>(nameof(MuiDoubleBufferRenderRequest.Object)).ToInt32());
		Assert.Equal(20, System.Runtime.InteropServices.Marshal.OffsetOf<
			MuiDoubleBufferRenderRequest>(nameof(MuiDoubleBufferRenderRequest.Left)).ToInt32());
		Assert.Equal(52, System.Runtime.InteropServices.Marshal.OffsetOf<
			MuiDoubleBufferRenderRequest>(nameof(MuiDoubleBufferRenderRequest.Flags)).ToInt32());

		Assert.Equal(64, System.Runtime.InteropServices.Marshal.SizeOf<
			MuiBackfillRenderRequest>());
		Assert.Equal(0, System.Runtime.InteropServices.Marshal.OffsetOf<
			MuiBackfillRenderRequest>(nameof(MuiBackfillRenderRequest.Object)).ToInt32());
		Assert.Equal(12, System.Runtime.InteropServices.Marshal.OffsetOf<
			MuiBackfillRenderRequest>(nameof(MuiBackfillRenderRequest.Left)).ToInt32());
		Assert.Equal(44, System.Runtime.InteropServices.Marshal.OffsetOf<
			MuiBackfillRenderRequest>(nameof(MuiBackfillRenderRequest.Brightness)).ToInt32());
		Assert.Equal(60, System.Runtime.InteropServices.Marshal.OffsetOf<
			MuiBackfillRenderRequest>(nameof(MuiBackfillRenderRequest.Background)).ToInt32());
	}

	[Fact]
	public void DrawBackgroundPacketPublishesNamedOffsetsToProvider()
	{
		var platform = CreatePlatform(out var areaClass, out var area);
		platform.BackfillCapabilityAvailable = true;
		platform.BackfillHandled = true;
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			area, MuiCommonControlCore.CustomBackfill, 1, false));
		var packet = APTR.FromPointer(0x1200);
		WriteRectangle(ref platform, packet, MuiLayoutPacketCore.DrawBackground,
			3, 4, 20, 10, -5, 7, 0x77);

		Assert.Equal(1u, MuiLayoutDispatcher.Dispatch(ref platform, State, area,
			packet));
		Assert.Equal(1u, platform.BackfillCount);
		var request = platform.LastBackfillRequest;
		Assert.Equal(MuiBackfillRenderRequest.DrawBackground, request.Kind);
		Assert.Equal(-5, request.XOffset);
		Assert.Equal(7, request.YOffset);
		Assert.Equal(0x77u, request.Flags);
		Assert.Equal(0, request.Brightness);
		Assert.Equal(20, request.Width);
		Assert.Equal(10, request.Height);
		Assert.Equal(22, request.Right);
		Assert.Equal(13, request.Bottom);
		Assert.Equal(1u, request.CustomBackfill);
		Assert.Equal(0u, platform.FillCount);
	}

	[Fact]
	public void BackfillPacketPublishesEdgesBrightnessAndOffsets()
	{
		var platform = CreatePlatform(out _, out var area);
		platform.BackfillCapabilityAvailable = true;
		platform.BackfillHandled = true;
		Assert.True(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			area, MuiCommonControlCore.CustomBackfill, 1, false));
		var packet = APTR.FromPointer(0x1200);
		WriteRectangle(ref platform, packet, MuiLayoutPacketCore.Backfill,
			3, 4, 22, 13, -2, 6, 80);

		Assert.Equal(1u, MuiLayoutDispatcher.Dispatch(ref platform, State, area,
			packet));
		var request = platform.LastBackfillRequest;
		Assert.Equal(MuiBackfillRenderRequest.Backfill, request.Kind);
		Assert.Equal(3, request.Left);
		Assert.Equal(4, request.Top);
		Assert.Equal(22, request.Right);
		Assert.Equal(13, request.Bottom);
		Assert.Equal(20, request.Width);
		Assert.Equal(10, request.Height);
		Assert.Equal(-2, request.XOffset);
		Assert.Equal(6, request.YOffset);
		Assert.Equal(80, request.Brightness);
		Assert.Equal(0u, request.Flags);
	}

	[Fact]
	public void BackfillProviderDeclineUsesDeterministicFillFallback()
	{
		var platform = CreatePlatform(out _, out var area);
		platform.BackfillCapabilityAvailable = true;
		platform.BackfillHandled = false;
		var packet = APTR.FromPointer(0x1200);
		WriteRectangle(ref platform, packet, MuiLayoutPacketCore.DrawBackground,
			3, 4, 20, 10, 0, 0, 0);

		Assert.Equal(1u, MuiLayoutDispatcher.Dispatch(ref platform, State, area,
			packet));
		Assert.Equal(1u, platform.BackfillCount);
		Assert.Equal(1u, platform.FillCount);
		Assert.Equal(3, platform.LastLeft);
		Assert.Equal(4, platform.LastTop);
		Assert.Equal(22, platform.LastRight);
		Assert.Equal(13, platform.LastBottom);
	}

	[Fact]
	public void BackfillProviderIdentityMutationFailsClosed()
	{
		var platform = CreatePlatform(out _, out var area);
		platform.BackfillCapabilityAvailable = true;
		platform.BackfillHandled = true;
		platform.BackfillMutatesIdentity = true;
		var packet = APTR.FromPointer(0x1200);
		WriteRectangle(ref platform, packet, MuiLayoutPacketCore.DrawBackground,
			3, 4, 20, 10, 0, 0, 0);

		Assert.Equal(0u, MuiLayoutDispatcher.Dispatch(ref platform, State, area,
			packet));
		Assert.Equal(1u, platform.BackfillCount);
		Assert.Equal(0u, platform.FillCount);
	}

	private static void WriteRectangle(ref MuiHeadlessTestPlatform platform,
		APTR packet, uint method, int left, int top, int widthOrRight,
		int heightOrBottom, int xOffset, int yOffset, uint auxiliary)
	{
		platform.WriteUInt32(packet, 0, method);
		platform.WriteUInt32(packet, 4, unchecked((uint)left));
		platform.WriteUInt32(packet, 8, unchecked((uint)top));
		platform.WriteUInt32(packet, 12, unchecked((uint)widthOrRight));
		platform.WriteUInt32(packet, 16, unchecked((uint)heightOrBottom));
		platform.WriteUInt32(packet, 20, unchecked((uint)xOffset));
		platform.WriteUInt32(packet, 24, unchecked((uint)yOffset));
		platform.WriteUInt32(packet, 28, auxiliary);
	}

	private static MuiHeadlessTestPlatform CreatePlatform(out APTR areaClass,
		out APTR area)
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		var name = APTR.FromPointer(0x1100);
		platform.WriteCString(name, "Rectangle.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		areaClass = MuiHeadlessObjectCore.RegisterClass(ref platform, State,
			name, APTR.Null, 0, APTR.FromPointer(1), false);
		area = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, areaClass,
			APTR.Null);
		var renderInfo = APTR.FromPointer(0x1300);
		var rastPort = APTR.FromPointer(0x1400);
		platform.WriteUInt32(renderInfo, 20, rastPort.Raw);
		Assert.True(MuiAreaLayoutCore.Setup(ref platform, State, area,
			renderInfo));
		return platform;
	}
}
