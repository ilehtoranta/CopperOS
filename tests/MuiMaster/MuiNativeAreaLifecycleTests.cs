/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiNativeAreaLifecycleTests
{
	private static readonly APTR Sidecar = APTR.FromPointer(0x1800);
	private static readonly APTR Object = APTR.FromPointer(0x1900);
	private static readonly APTR Message = APTR.FromPointer(0x2000);
	private static readonly APTR RenderInfo = APTR.FromPointer(0x2100);
	private static readonly APTR RastPort = APTR.FromPointer(0x2200);

	private static MuiHeadlessTestPlatform CreatePlatform()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x8000, 0x5000,
			Sidecar);
		var sidecar = new MuiNativeMuiObjectRecord
		{
			Signature = MuiNativeMuiObjectRecord.Magic,
			Revision = MuiNativeMuiObjectRecord.Version,
			Object = Object,
			Flags = MuiNativeMuiObjectRecord.ObjectInitialized,
			LifecycleState = MuiNativeMuiObjectRecord.StateLive,
		};
		Assert.True(MuiNativeMuiObjectCodec.Write(ref platform, Sidecar, sidecar));
		return platform;
	}

	private static void WriteValidSetup(ref MuiHeadlessTestPlatform platform)
	{
		Assert.True(MuiDrawingRenderInfoCodec.Write(ref platform, RenderInfo,
			new MuiDrawingRenderInfoRecord { RastPort = RastPort }));
		Assert.True(MuiLayoutPacketCore.WriteRenderInfo(ref platform, Message,
			MuiLayoutPacketCore.Setup, RenderInfo));
	}

	[Fact]
	public void SetupProjectsTypedRenderInfoIntoSidecarUsedByRedraw()
	{
		var platform = CreatePlatform();
		WriteValidSetup(ref platform);
		uint result = 1;

		Assert.True(MuiNativeAreaLifecycleCore.TryProjectAfterClassCallback(
			ref platform, Sidecar, Message, MuiLayoutPacketCore.Setup, ref result));
		Assert.Equal(1u, result);
		Assert.True(MuiNativeObjectStateCore.TryGetAttribute(ref platform,
			Sidecar, MuiAreaWindowRelationshipCore.RenderInfoAttribute,
			out var renderInfoValue));
		Assert.Equal(RenderInfo.Raw, renderInfoValue);
		Assert.True(MuiNativeRedrawServiceCore.TryReadRenderBinding(ref platform,
			Object, Sidecar, out var binding));
		Assert.Equal(RenderInfo, binding.RenderInfo);
		Assert.Equal(RastPort, binding.RastPort);
	}

	[Fact]
	public void SetupRejectsIncompleteRenderInfoAndDoesNotPublishIt()
	{
		var platform = CreatePlatform();
		Assert.True(MuiNativeObjectStateCore.SetBorrowedAttributeNoNotify(
			ref platform, Sidecar,
			MuiAreaWindowRelationshipCore.RenderInfoAttribute, RenderInfo.Raw));
		Assert.True(MuiDrawingRenderInfoCodec.Write(ref platform, RenderInfo,
			new MuiDrawingRenderInfoRecord { RastPort = APTR.Null }));
		Assert.True(MuiLayoutPacketCore.WriteRenderInfo(ref platform, Message,
			MuiLayoutPacketCore.Setup, RenderInfo));
		uint result = 1;

		Assert.True(MuiNativeAreaLifecycleCore.TryProjectAfterClassCallback(
			ref platform, Sidecar, Message, MuiLayoutPacketCore.Setup, ref result));
		Assert.Equal(0u, result);
		Assert.True(MuiNativeObjectStateCore.TryGetAttribute(ref platform,
			Sidecar, MuiAreaWindowRelationshipCore.RenderInfoAttribute,
			out var renderInfoValue));
		Assert.Equal(0u, renderInfoValue);
		Assert.False(MuiNativeRedrawServiceCore.TryReadRenderBinding(ref platform,
			Object, Sidecar, out _));
	}

	[Fact]
	public void FailedClassSetupClearsAnyStalePointerWithoutAllocating()
	{
		var platform = CreatePlatform();
		WriteValidSetup(ref platform);
		Assert.True(MuiNativeObjectStateCore.SetBorrowedAttributeNoNotify(
			ref platform, Sidecar,
			MuiAreaWindowRelationshipCore.RenderInfoAttribute, RenderInfo.Raw));
		var allocationCount = platform.AllocationCount;
		uint result = 0;

		Assert.True(MuiNativeAreaLifecycleCore.TryProjectAfterClassCallback(
			ref platform, Sidecar, Message, MuiLayoutPacketCore.Setup, ref result));
		Assert.Equal(0u, result);
		Assert.Equal(allocationCount, platform.AllocationCount);
		Assert.True(MuiNativeObjectStateCore.TryGetAttribute(ref platform,
			Sidecar, MuiAreaWindowRelationshipCore.RenderInfoAttribute,
			out var renderInfoValue));
		Assert.Equal(0u, renderInfoValue);
	}

	[Fact]
	public void SetupFailsClosedWhenSidecarCannotAllocateAttributeRecord()
	{
		var platform = CreatePlatform();
		platform.AllocationAdmission = (_, _) => false;
		WriteValidSetup(ref platform);
		uint result = 1;

		Assert.True(MuiNativeAreaLifecycleCore.TryProjectAfterClassCallback(
			ref platform, Sidecar, Message, MuiLayoutPacketCore.Setup, ref result));
		Assert.Equal(0u, result);
		Assert.False(MuiNativeObjectStateCore.TryGetAttribute(ref platform,
			Sidecar, MuiAreaWindowRelationshipCore.RenderInfoAttribute, out _));
	}

	[Fact]
	public void CleanupClearsBorrowedRenderInfoAndPreservesClassResult()
	{
		var platform = CreatePlatform();
		WriteValidSetup(ref platform);
		Assert.True(MuiNativeObjectStateCore.SetBorrowedAttributeNoNotify(
			ref platform, Sidecar,
			MuiAreaWindowRelationshipCore.RenderInfoAttribute, RenderInfo.Raw));
		Assert.True(MuiLayoutPacketCore.WriteMethod(ref platform, Message,
			MuiLayoutPacketCore.Cleanup));
		uint result = 0xAABBCCDD;

		Assert.True(MuiNativeAreaLifecycleCore.TryProjectAfterClassCallback(
			ref platform, Sidecar, Message, MuiLayoutPacketCore.Cleanup, ref result));
		Assert.Equal(0xAABBCCDDu, result);
		Assert.True(MuiNativeObjectStateCore.TryGetAttribute(ref platform,
			Sidecar, MuiAreaWindowRelationshipCore.RenderInfoAttribute,
			out var renderInfoValue));
		Assert.Equal(0u, renderInfoValue);
		Assert.False(MuiNativeRedrawServiceCore.TryReadRenderBinding(ref platform,
			Object, Sidecar, out _));
	}

	[Fact]
	public void CleanupRejectsWrongMethodWithoutClearingCurrentRenderInfo()
	{
		var platform = CreatePlatform();
		Assert.True(MuiNativeObjectStateCore.SetBorrowedAttributeNoNotify(
			ref platform, Sidecar,
			MuiAreaWindowRelationshipCore.RenderInfoAttribute, RenderInfo.Raw));
		Assert.True(MuiLayoutPacketCore.WriteMethod(ref platform, Message,
			MuiLayoutPacketCore.Setup));
		uint result = 0;

		Assert.False(MuiNativeAreaLifecycleCore.TryProjectAfterClassCallback(
			ref platform, Sidecar, Message, MuiLayoutPacketCore.Cleanup, ref result));
		Assert.True(MuiNativeObjectStateCore.TryGetAttribute(ref platform,
			Sidecar, MuiAreaWindowRelationshipCore.RenderInfoAttribute,
			out var renderInfoValue));
		Assert.Equal(RenderInfo.Raw, renderInfoValue);
	}
}
