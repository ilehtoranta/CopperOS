using Amiga;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiNativeRedrawTests
{
	[Fact]
	public void PublicRedrawVectorHasTheDocumentedVoidReturn()
	{
		var method = typeof(MuiNativeLibraryEntrypoints).GetMethod(
			nameof(MuiNativeLibraryEntrypoints.Redraw));

		Assert.NotNull(method);
		Assert.Equal(typeof(void), method.ReturnType);
	}

	[Fact]
	public void NativeRedrawMessageCodecUsesCompleteNamedRecord()
	{
		Assert.Equal(MuiRedrawServiceCore.DrawObject,
			MuiNativeRedrawMessage.DrawObjectFlag);
		Assert.Equal(MuiRedrawServiceCore.DrawUpdate,
			MuiNativeRedrawMessage.DrawUpdateFlag);
		Assert.Equal(MuiNativeRedrawMessage.DrawObjectFlag |
			MuiNativeRedrawMessage.DrawUpdateFlag,
			MuiNativeRedrawMessage.AllowedFlags);

		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1800));
		var message = APTR.FromPointer(0x1500);
		var value = default(MuiNativeRedrawMessage);
		value.MethodId = MuiNativeRedrawMessage.DrawMethodId;
		value.Flags = MuiNativeRedrawMessage.DrawUpdateFlag;

		Assert.True(MuiNativeRedrawMessageCodec.Write(ref platform, message, value));
		Assert.True(MuiNativeRedrawMessageCodec.TryRead(ref platform, message,
			out var decoded));
		Assert.Equal(value.MethodId, decoded.MethodId);
		Assert.Equal(value.Flags, decoded.Flags);

	var malformed = value;
	malformed.MethodId = 0x80420000u;
	Assert.True(MuiNativeRedrawMessageCodec.Write(ref platform, message,
		malformed));
		Assert.False(MuiNativeRedrawMessageCodec.TryRead(ref platform, message,
			out _));
		Assert.False(MuiNativeRedrawMessageCodec.Write(ref platform,
			APTR.FromPointer(0x20FFC), value));
	}

	[Fact]
	public void RedrawClipPlanIntersectsNestedVirtualGroupViewports()
	{
		var plan = new MuiNativeRedrawClipPlan { DrawEligible = 1 };
		var outer = new MuiNativeAreaRectangle
		{
			Left = 10,
			Top = 20,
			Width = 100,
			Height = 80,
		};
		var inner = new MuiNativeAreaRectangle
		{
			Left = 40,
			Top = 5,
			Width = 90,
			Height = 60,
		};

		Assert.True(MuiNativeRedrawClipPlanCore.IncludeViewport(ref plan,
			outer));
		Assert.True(MuiNativeRedrawClipPlanCore.IncludeViewport(ref plan,
			inner));
		Assert.Equal(1u, plan.HasClip);
		Assert.Equal(1u, plan.DrawEligible);
		Assert.Equal(40, plan.Bounds.Left);
		Assert.Equal(20, plan.Bounds.Top);
		Assert.Equal(70, plan.Bounds.Width);
		Assert.Equal(45, plan.Bounds.Height);
	}

	[Fact]
	public void RedrawClipPlanSuppressesDisjointAndEmptyVirtualGroupViewports()
	{
		var plan = new MuiNativeRedrawClipPlan { DrawEligible = 1 };
		var first = new MuiNativeAreaRectangle
		{
			Left = -20,
			Top = 3,
			Width = 15,
			Height = 12,
		};
		var disjoint = new MuiNativeAreaRectangle
		{
			Left = 0,
			Top = 3,
			Width = 15,
			Height = 12,
		};
		Assert.True(MuiNativeRedrawClipPlanCore.IncludeViewport(ref plan,
			first));
		Assert.True(MuiNativeRedrawClipPlanCore.IncludeViewport(ref plan,
			disjoint));
		Assert.Equal(0u, plan.DrawEligible);

		plan = new MuiNativeRedrawClipPlan { DrawEligible = 1 };
		var empty = new MuiNativeAreaRectangle
		{
			Left = 8,
			Top = 9,
			Width = 0,
			Height = 10,
		};
		Assert.True(MuiNativeRedrawClipPlanCore.IncludeViewport(ref plan,
			empty));
		Assert.Equal(1u, plan.HasClip);
		Assert.Equal(0u, plan.DrawEligible);
	}

	[Fact]
	public void RedrawClipPlanRejectsMalformedViewportExtents()
	{
		var plan = new MuiNativeRedrawClipPlan { DrawEligible = 1 };
		var malformed = new MuiNativeAreaRectangle
		{
			Left = 0,
			Top = 0,
			Width = -1,
			Height = 10,
		};

		Assert.False(MuiNativeRedrawClipPlanCore.IncludeViewport(ref plan,
			malformed));
		Assert.Equal(0u, plan.HasClip);
		Assert.Equal(1u, plan.DrawEligible);
	}

	[Fact]
	public void RedrawAncestorSnapshotRejectsAReparentedObject()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1800));
		var publicObjects = APTR.FromPointer(0x1100);
		var ownerRoot = APTR.FromPointer(0x1000);
		var target = APTR.FromPointer(0x1800);
		var parent = APTR.FromPointer(0x1840);
		var targetBindingAddress = APTR.FromPointer(0x1200);
		var parentBindingAddress = APTR.FromPointer(0x1240);
		var targetSidecar = APTR.FromPointer(0x1300);
		var parentSidecar = APTR.FromPointer(0x1380);
		var classPointer = APTR.FromPointer(0x1900);

		var registry = default(MuiNativePublicObjectRegistryRecord);
		registry.Signature = MuiNativePublicObjectRegistryRecord.Magic;
		registry.Revision = MuiNativePublicObjectRegistryRecord.Version;
		registry.Head = targetBindingAddress;
		Assert.True(MuiNativePublicObjectRegistryCodec.Write(ref platform,
			publicObjects, registry));

		WriteLiveObject(ref platform, targetSidecar, target, classPointer,
			ownerRoot);
		WriteLiveObject(ref platform, parentSidecar, parent, classPointer,
			ownerRoot);
		var targetBinding = LiveBinding(target,
			classPointer, ownerRoot, parent, targetSidecar, parentBindingAddress);
		var parentBinding = LiveBinding(parent,
			classPointer, ownerRoot, APTR.Null, parentSidecar, APTR.Null);
		Assert.True(MuiNativePublicObjectBindingCodec.Write(ref platform,
			targetBindingAddress, targetBinding));
		Assert.True(MuiNativePublicObjectBindingCodec.Write(ref platform,
			parentBindingAddress, parentBinding));

		var ancestors = APTR.FromPointer(0x1500);
		Assert.True(MuiGuestStructCursor.TryCreate(ref platform, ancestors,
			2 * MuiNativeRedrawAncestorRecord.Size, out var cursor));
		var targetRecord = default(MuiNativeRedrawAncestorRecord);
		targetRecord.Object = target;
		targetRecord.Sidecar = targetSidecar;
		var parentRecord = default(MuiNativeRedrawAncestorRecord);
		parentRecord.Object = parent;
		parentRecord.Sidecar = parentSidecar;
		Assert.True(MuiGuestStructCursor.TryTake(ref platform, ref cursor,
			MuiNativeRedrawAncestorRecord.Size, out var targetRecordAddress));
		Assert.True(MuiNativeRedrawAncestorCodec.Write(ref platform,
			targetRecordAddress, targetRecord));
		Assert.True(MuiGuestStructCursor.TryTake(ref platform, ref cursor,
			MuiNativeRedrawAncestorRecord.Size, out var parentRecordAddress));
		Assert.True(MuiNativeRedrawAncestorCodec.Write(ref platform,
			parentRecordAddress, parentRecord));

		Assert.True(MuiNativeRedrawServiceCore.TryCountAncestorPath(ref platform,
			publicObjects, ownerRoot, target, out var pathCount));
		Assert.Equal(2u, pathCount);
		Assert.True(MuiNativeRedrawServiceCore.TryValidateAncestorPath(
			ref platform, publicObjects, ownerRoot, ancestors, pathCount));

		targetBinding.Parent = APTR.Null;
		Assert.True(MuiNativePublicObjectBindingCodec.Write(ref platform,
			targetBindingAddress, targetBinding));
		Assert.False(MuiNativeRedrawServiceCore.TryValidateAncestorPath(
			ref platform, publicObjects, ownerRoot, ancestors, pathCount));
	}

	[Fact]
	public void RedrawRequiresLiveRenderInfoAndRastPortButNotAnUnneededLayer()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1800));
		var publicObjects = APTR.FromPointer(0x1100);
		var ownerRoot = APTR.FromPointer(0x1000);
		var target = APTR.FromPointer(0x1800);
		var bindingAddress = APTR.FromPointer(0x1200);
		var sidecarAddress = APTR.FromPointer(0x1300);
		var attributeAddress = APTR.FromPointer(0x1400);
		var classPointer = APTR.FromPointer(0x1900);
		var renderInfoAddress = APTR.FromPointer(0x1A00);
		var rastPortAddress = APTR.FromPointer(0x1B00);
		var layerAddress = APTR.FromPointer(0x1C00);
		var replacementRenderInfoAddress = APTR.FromPointer(0x1D00);

		var registry = new MuiNativePublicObjectRegistryRecord
		{
			Signature = MuiNativePublicObjectRegistryRecord.Magic,
			Revision = MuiNativePublicObjectRegistryRecord.Version,
			Head = bindingAddress,
		};
		Assert.True(MuiNativePublicObjectRegistryCodec.Write(ref platform,
			publicObjects, registry));
		WriteLiveObject(ref platform, sidecarAddress, target, classPointer,
			ownerRoot);
		Assert.True(MuiNativeMuiObjectCodec.TryRead(ref platform, sidecarAddress,
			out var sidecar));
		sidecar.Attributes = attributeAddress;
		Assert.True(MuiNativeMuiObjectCodec.Write(ref platform, sidecarAddress,
			sidecar));
		var attribute = new MuiNativeObjectAttributeRecord
		{
			Signature = MuiNativeObjectAttributeRecord.Magic,
			Revision = MuiNativeObjectAttributeRecord.Version,
			Attribute = MuiAreaWindowRelationshipCore.RenderInfoAttribute,
			Generation = 1,
		};
		Assert.True(MuiNativeObjectAttributeCodec.Write(ref platform,
			attributeAddress, attribute));
		var targetBinding = LiveBinding(target, classPointer, ownerRoot,
			APTR.Null, sidecarAddress, APTR.Null);
		Assert.True(MuiNativePublicObjectBindingCodec.Write(ref platform,
			bindingAddress, targetBinding));

		Assert.False(MuiNativeRedrawServiceCore.TryReadRenderBinding(ref platform,
			target, sidecarAddress, out _));
		attribute.Value = renderInfoAddress.Raw;
		Assert.True(MuiNativeObjectAttributeCodec.Write(ref platform,
			attributeAddress, attribute));
		Assert.True(MuiDrawingRenderInfoCodec.Write(ref platform,
			renderInfoAddress, new MuiDrawingRenderInfoRecord
			{
				RastPort = APTR.Null,
			}));
		Assert.False(MuiNativeRedrawServiceCore.TryReadRenderBinding(ref platform,
			target, sidecarAddress, out _));
		Assert.True(MuiDrawingRenderInfoCodec.Write(ref platform,
			renderInfoAddress, new MuiDrawingRenderInfoRecord
			{
				RastPort = rastPortAddress,
			}));
		Assert.True(MuiDrawingRasterPortStructCodec.Write(ref platform, rastPortAddress,
			new MuiDrawingRasterPortRecord { Layer = APTR.Null }));

		Assert.True(MuiNativeRedrawServiceCore.TryReadRenderBinding(ref platform,
			target, sidecarAddress, out var renderBinding));
		Assert.Equal(target, renderBinding.Object);
		Assert.Equal(sidecarAddress, renderBinding.Sidecar);
		Assert.Equal(renderInfoAddress, renderBinding.RenderInfo);
		Assert.Equal(rastPortAddress, renderBinding.RastPort);
		Assert.True(MuiNativeRedrawServiceCore.TryValidateRenderBinding(
			ref platform, publicObjects, ownerRoot, target, renderBinding));
		Assert.False(MuiNativeRedrawServiceCore.TryReadLayerBinding(ref platform,
			target, sidecarAddress, out _));

		Assert.True(MuiDrawingRasterPortStructCodec.Write(ref platform, rastPortAddress,
			new MuiDrawingRasterPortRecord { Layer = layerAddress }));
		Assert.True(MuiNativeRedrawServiceCore.TryReadLayerBinding(ref platform,
			target, sidecarAddress, out var layerBinding));
		Assert.Equal(layerAddress, layerBinding.Layer);

		Assert.True(MuiDrawingRenderInfoCodec.Write(ref platform,
			replacementRenderInfoAddress, new MuiDrawingRenderInfoRecord
			{
				RastPort = rastPortAddress,
			}));
		attribute.Value = replacementRenderInfoAddress.Raw;
		Assert.True(MuiNativeObjectAttributeCodec.Write(ref platform,
			attributeAddress, attribute));
		Assert.False(MuiNativeRedrawServiceCore.TryValidateRenderBinding(
			ref platform, publicObjects, ownerRoot, target, renderBinding));
	}

	[Fact]
	public void RedrawLayerBindingUsesNamedRenderChainAndRejectsReplacement()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			APTR.FromPointer(0x1800));
		var publicObjects = APTR.FromPointer(0x1100);
		var ownerRoot = APTR.FromPointer(0x1000);
		var target = APTR.FromPointer(0x1800);
		var classPointer = APTR.FromPointer(0x1900);
		var bindingAddress = APTR.FromPointer(0x1200);
		var sidecarAddress = APTR.FromPointer(0x1300);
		var attributeAddress = APTR.FromPointer(0x1400);
		var renderInfoAddress = APTR.FromPointer(0x1A00);
		var replacementRenderInfoAddress = APTR.FromPointer(0x1A40);
		var rasterPortAddress = APTR.FromPointer(0x1B00);
		var replacementRasterPortAddress = APTR.FromPointer(0x1B20);
		var layerAddress = APTR.FromPointer(0x1C00);
		var replacementLayerAddress = APTR.FromPointer(0x1D00);

		var registry = default(MuiNativePublicObjectRegistryRecord);
		registry.Signature = MuiNativePublicObjectRegistryRecord.Magic;
		registry.Revision = MuiNativePublicObjectRegistryRecord.Version;
		registry.Head = bindingAddress;
		Assert.True(MuiNativePublicObjectRegistryCodec.Write(ref platform,
			publicObjects, registry));
		WriteLiveObject(ref platform, sidecarAddress, target, classPointer,
			ownerRoot);
		var binding = LiveBinding(target, classPointer, ownerRoot, APTR.Null,
			sidecarAddress, APTR.Null);
		Assert.True(MuiNativePublicObjectBindingCodec.Write(ref platform,
			bindingAddress, binding));
		Assert.True(MuiNativeMuiObjectCodec.TryRead(ref platform, sidecarAddress,
			out var sidecar));
		sidecar.Attributes = attributeAddress;
		Assert.True(MuiNativeMuiObjectCodec.Write(ref platform, sidecarAddress,
			sidecar));
		var renderAttribute = new MuiNativeObjectAttributeRecord
		{
			Signature = MuiNativeObjectAttributeRecord.Magic,
			Revision = MuiNativeObjectAttributeRecord.Version,
			Attribute = MuiAreaWindowRelationshipCore.RenderInfoAttribute,
			Value = renderInfoAddress.Raw,
			Generation = 1,
		};
		Assert.True(MuiNativeObjectAttributeCodec.Write(ref platform,
			attributeAddress, renderAttribute));
		Assert.True(MuiDrawingRenderInfoCodec.Write(ref platform,
			renderInfoAddress, new MuiDrawingRenderInfoRecord
			{
				RastPort = rasterPortAddress,
			}));
		Assert.True(MuiDrawingRasterPortStructCodec.Write(ref platform,
			rasterPortAddress, new MuiDrawingRasterPortRecord
			{
				Layer = layerAddress,
			}));

		Assert.True(MuiNativeRedrawServiceCore.TryReadLayerBinding(ref platform,
			target, sidecarAddress, out var planned));
		Assert.Equal(target, planned.Object);
		Assert.Equal(sidecarAddress, planned.Sidecar);
		Assert.Equal(renderInfoAddress, planned.RenderInfo);
		Assert.Equal(rasterPortAddress, planned.RastPort);
		Assert.Equal(layerAddress, planned.Layer);
		Assert.True(MuiNativeRedrawServiceCore.TryValidateLayerBinding(
			ref platform, publicObjects, ownerRoot, target, planned));

		Assert.True(MuiDrawingRasterPortStructCodec.Write(ref platform,
			rasterPortAddress, new MuiDrawingRasterPortRecord
			{
				Layer = replacementLayerAddress,
			}));
		Assert.False(MuiNativeRedrawServiceCore.TryValidateLayerBinding(
			ref platform, publicObjects, ownerRoot, target, planned));
		Assert.True(MuiDrawingRasterPortStructCodec.Write(ref platform,
			rasterPortAddress, new MuiDrawingRasterPortRecord
			{
				Layer = layerAddress,
			}));

		Assert.True(MuiDrawingRenderInfoCodec.TryRead(ref platform,
			renderInfoAddress, out var renderInfo));
		renderInfo.RastPort = replacementRasterPortAddress;
		Assert.True(MuiDrawingRenderInfoCodec.Write(ref platform,
			renderInfoAddress, renderInfo));
		Assert.False(MuiNativeRedrawServiceCore.TryValidateLayerBinding(
			ref platform, publicObjects, ownerRoot, target, planned));
		renderInfo.RastPort = rasterPortAddress;
		Assert.True(MuiDrawingRenderInfoCodec.Write(ref platform,
			renderInfoAddress, renderInfo));

		Assert.True(MuiDrawingRenderInfoCodec.Write(ref platform,
			replacementRenderInfoAddress, new MuiDrawingRenderInfoRecord
			{
				RastPort = replacementRasterPortAddress,
			}));
		Assert.True(MuiDrawingRasterPortStructCodec.Write(ref platform,
			replacementRasterPortAddress, new MuiDrawingRasterPortRecord
			{
				Layer = layerAddress,
			}));
		renderAttribute.Value = replacementRenderInfoAddress.Raw;
		Assert.True(MuiNativeObjectAttributeCodec.Write(ref platform,
			attributeAddress, renderAttribute));
		Assert.False(MuiNativeRedrawServiceCore.TryValidateLayerBinding(
			ref platform, publicObjects, ownerRoot, target, planned));
	}

	private static void WriteLiveObject(ref MuiHeadlessTestPlatform platform,
		APTR sidecarAddress, APTR obj, APTR classPointer, APTR ownerRoot)
	{
		var sidecar = default(MuiNativeMuiObjectRecord);
		sidecar.Signature = MuiNativeMuiObjectRecord.Magic;
		sidecar.Revision = MuiNativeMuiObjectRecord.Version;
		sidecar.Object = obj;
		sidecar.Class = classPointer;
		sidecar.OwnerRoot = ownerRoot;
		sidecar.Flags = MuiNativeMuiObjectRecord.ObjectInitialized;
		sidecar.LifecycleState = MuiNativeMuiObjectRecord.StateLive;
		Assert.True(MuiNativeMuiObjectCodec.Write(ref platform, sidecarAddress,
			sidecar));
	}

	private static MuiNativePublicObjectBinding LiveBinding(APTR obj,
		APTR classPointer, APTR ownerRoot, APTR parent,
		APTR sidecar, APTR next)
	{
		var binding = default(MuiNativePublicObjectBinding);
		binding.Signature = MuiNativePublicObjectBinding.Magic;
		binding.Next = next;
		binding.Object = obj;
		binding.Class = classPointer;
		binding.OwnerRoot = ownerRoot;
		binding.Parent = parent;
		binding.Sidecar = sidecar;
		binding.DisposeState = MuiNativePublicObjectBinding.StateLive;
		return binding;
	}
}
