using Amiga;
using CopperSharp.Compiler;
using CopperSharp.Sdk.Amiga;
using System.Runtime.InteropServices;

namespace CopperOS.MuiMaster.NativeRoot;

// Real native dependency proof, not a substitute MUI/Intuition implementation.
// The harness supplies an owned BOOPSI base and its real DoSuperMethodA thunk;
// all classes, objects, dispatch and memory management execute on the guest CPU.
public static class MuiNativeClassIntegrationRoots
{
	public const string ClientExport = "copperos.mui.test.class-client";
	public const string BaseExport = "copperos.mui.test.class.base-dispatch";
	public const string CustomExport = "copperos.mui.test.class.custom-dispatch";
	private const uint PublicCallbackBase = 0x81234560;
	private const uint PrivateCallerBase = 0x8BAD0100;
	private const uint ProbeMethod = 0xF00D0001;
	private const uint ProbeResult = 0x13572468;

	private struct NativeRedrawLayerFixture
	{
		internal APTR Object;
		internal APTR Sidecar;
		internal APTR PublicObjects;
		internal APTR OwnerRoot;
		internal APTR RenderInfo;
		internal APTR ReplacementRenderInfo;
		internal APTR RastPort;
		internal APTR ReplacementRastPort;
		internal APTR Layer;
		internal APTR ReplacementLayer;
	}

	[M68kExport(ClientExport)]
	[return: M68kRegister(M68kRegister.D0)]
	public static uint ClassClient([M68kRegister(M68kRegister.A0)] APTR intuition,
		[M68kRegister(M68kRegister.A1)] APTR doSuper)
	{
		if (intuition.IsNull || doSuper.IsNull) return 401;
		var flags = Exec.MemoryFlags.Public;
		var initialAvailable = Exec.AvailMem(flags);
		var initialLargest = Exec.AvailMem(flags | Exec.MemoryFlags.Largest);
		if (!MuiExecPrivateRootOwner.TryCreate(out var root)) return 402;
		var memory = default(MuiNativeClassMemory);
		var providerContext = Exec.AllocMem(MuiNativeClassContext.Size, flags | Exec.MemoryFlags.Clear);
		var invalidContext = Exec.AllocMem(MuiNativeClassContext.Size, flags | Exec.MemoryFlags.Clear);
		if (providerContext.IsNull || invalidContext.IsNull ||
			!MuiNativeClassContextCodec.Write(ref memory, providerContext,
				new MuiNativeClassContext { IntuitionBase = intuition, OwnerRoot = root }) ||
			!MuiNativeClassContextCodec.Write(ref memory, invalidContext,
				new MuiNativeClassContext { IntuitionBase = intuition, OwnerRoot = APTR.Null })) return 436;
		var platform = new MuiNativeClassPlatform { Context = providerContext };
		// Intuition-internal dispatch does not specify the incoming A6 value.
		// Observe it for private/base classes; public bindings are always strict.
		var baseContext = CreateContext(ref platform, intuition, root, doSuper, APTR.Null, 0);
		var publicContext = CreateContext(ref platform, intuition, root, doSuper,
			APTR.FromPointer(PublicCallbackBase), 2);
		var privateContext = CreateContext(ref platform, intuition, root, doSuper, APTR.Null, 1);
		var probe = platform.Allocate(ClassProbeMessage.Size, (uint)flags);
		if (baseContext.IsNull || publicContext.IsNull || privateContext.IsNull || probe.IsNull) return 403;
		var probeMessage = new ClassProbeMessage { MethodId = ProbeMethod };
		if (!ClassProbeMessageCodec.Write(ref memory, probe, ref probeMessage)) return 404;
		// Isolate the provider's named `rootclass` lookup from creation of the
		// named public test class below. The probe uses the same native MUI
		// platform adapter and is freed before the class-service assertions.
		var namedRootProbe = platform.MakeClass(APTR.Null, APTR.Null, 4,
			APTR.ExportAddress(BaseExport));
		if (namedRootProbe.IsNull) return 436;
		if (!platform.FreeClass(namedRootProbe)) return 437;
		var baseName = APTR.FromPointer(CString.ToUInt32(CString.FromLiteral("CopperOSNativeClassBase")));
		var baseClass = platform.MakeClass(baseName, APTR.Null, 4, APTR.ExportAddress(BaseExport));
		if (baseClass.IsNull || !SetContext(ref platform, baseClass, baseContext)) return 405;
		var baseValue = BOOPSIGuestCodec.ReadClass(ref memory, baseClass);
		if (baseValue.cl_Super.IsNull || baseValue.cl_InstOffset != 0 ||
			baseValue.cl_InstSize != 4) return 406;
		if (!platform.AddClass(baseClass)) return 438;
		if (!platform.RemoveClass(baseClass)) return 439;
		if (!platform.AddClass(baseClass)) return 440;
		var serviceProof = CheckClassService(ref platform, root, baseName, baseClass,
			baseContext);
		if (serviceProof != 0) return serviceProof;
		var publicClass = platform.MakeCustomClass(baseClass, 8, APTR.ExportAddress(CustomExport),
			APTR.FromPointer(PublicCallbackBase));
		if (publicClass.IsNull || !SetContext(ref platform, publicClass, publicContext)) return 407;
		var privateClass = platform.MakeCustomClass(publicClass, 12, APTR.ExportAddress(CustomExport), APTR.Null);
		if (privateClass.IsNull || !SetContext(ref platform, privateClass, privateContext)) return 408;
		var publicValue = BOOPSIGuestCodec.ReadClass(ref memory, publicClass);
		var privateValue = BOOPSIGuestCodec.ReadClass(ref memory, privateClass);
		baseValue = BOOPSIGuestCodec.ReadClass(ref memory, baseClass);
		if (baseValue.cl_SubclassCount != 1 || publicValue.cl_SubclassCount != 1 ||
			privateValue.cl_SubclassCount != 0 || publicValue.cl_Super != baseClass ||
			privateValue.cl_Super != publicClass || publicValue.cl_InstOffset != 4 ||
			privateValue.cl_InstOffset != 12) return 409;
		if (platform.FreeCustomClass(publicClass) || platform.FreeClass(baseClass)) return 410;
		var borrowedRoot = root;
		if (MuiExecPrivateRootOwner.TryDestroy(ref borrowedRoot) || borrowedRoot != root) return 411;

		// OM_NEW traverses the real private -> public -> base -> rootclass chain.
		// Only rootclass allocates the actual BOOPSI object and ownership record.
		var obj = platform.NewObject(privateClass, APTR.Null);
		if (obj.IsNull) return 412;
		var objectHeader = BOOPSIGuestCodec.ReadObjectHeader(ref memory,
			APTR.FromPointer(obj.Raw - _Object.Size));
		privateValue = BOOPSIGuestCodec.ReadClass(ref memory, privateClass);
		publicValue = BOOPSIGuestCodec.ReadClass(ref memory, publicClass);
		baseValue = BOOPSIGuestCodec.ReadClass(ref memory, baseClass);
		if (objectHeader.o_Class != privateClass || privateValue.cl_ObjectCount != 1 ||
			publicValue.cl_ObjectCount != 0 || baseValue.cl_ObjectCount != 0 ||
			!MuiNativeClassBindingCodec.TryRead(ref memory,
				privateValue.cl_Dispatcher.Data, out var privateBinding) ||
			privateBinding.ActiveObjects != 1 || privateBinding.ObjectHead.IsNull ||
			!MuiNativeClassObjectBindingCodec.TryRead(ref memory,
				privateBinding.ObjectHead, out var privateObjectBinding) ||
			privateObjectBinding.Sidecar.IsNull ||
			!MuiNativeMuiObjectCodec.TryRead(ref memory,
				privateObjectBinding.Sidecar, out var privateObjectSidecar) ||
			privateObjectSidecar.Object != obj ||
			!MuiNativeClassBindingCodec.TryRead(ref memory,
				publicValue.cl_Dispatcher.Data, out var publicBinding) ||
			publicBinding.ActiveObjects != 1 || publicBinding.ObjectHead.IsNull ||
			!MuiNativeClassObjectBindingCodec.TryRead(ref memory,
				publicBinding.ObjectHead, out var publicObjectBinding) ||
			publicObjectBinding.Sidecar.IsNull ||
			!MuiNativeMuiObjectCodec.TryRead(ref memory,
				publicObjectBinding.Sidecar, out var publicObjectSidecar) ||
			publicObjectSidecar.Object != obj ||
			platform.FreeCustomClass(privateClass)) return 413;
		if (!CheckContext(ref platform, privateContext, 1, 0, 0) ||
			!CheckContext(ref platform, publicContext, 1, 0, 0) ||
			!CheckContext(ref platform, baseContext, 2, 1, 0)) return 414;
		platform.DisposeObject(obj);
		if (BOOPSIGuestCodec.ReadClass(ref memory, privateClass).cl_ObjectCount != 0 ||
			!MuiNativeClassBindingCodec.TryRead(ref memory,
				privateValue.cl_Dispatcher.Data, out privateBinding) ||
			privateBinding.ActiveObjects != 0 || privateBinding.ObjectHead.IsNotNull ||
			!MuiNativeClassBindingCodec.TryRead(ref memory,
				publicValue.cl_Dispatcher.Data, out publicBinding) ||
			publicBinding.ActiveObjects != 0 || publicBinding.ObjectHead.IsNotNull ||
			!CheckContext(ref platform, privateContext, 1, 1, 0) ||
			!CheckContext(ref platform, publicContext, 1, 1, 0) ||
			!CheckContext(ref platform, baseContext, 2, 2, 0)) return 415;
		if (!CheckDispatcherRejection(ref platform, root, privateClass, probe) ||
			!CheckContext(ref platform, privateContext, 1, 1, 0)) return 435;

		// A private callback preserves its caller's A6; a public callback substitutes
		// its explicit library base. With no objects/subclasses the private callback
		// would actually be freed by an unsafe reentrant FreeCustomClass operation.
		if (!ClassContextCodec.TryRead(ref memory, privateContext, out var privateState)) return 416;
		privateState.ExpectedBase = APTR.FromPointer(PrivateCallerBase);
		if (!ClassContextCodec.Write(ref memory, privateContext, privateState)) return 417;
		privateValue = BOOPSIGuestCodec.ReadClass(ref memory, privateClass);
		if (CallDispatcher(privateValue.cl_Dispatcher.Entry, privateClass, APTR.Null, probe,
			APTR.FromPointer(PrivateCallerBase)) != ProbeResult) return 418;
		if (!ClassContextCodec.TryRead(ref memory, publicContext, out var publicState)) return 419;
		publicState.ExpectedDepth = 1;
		if (!ClassContextCodec.Write(ref memory, publicContext, publicState)) return 420;
		publicValue = BOOPSIGuestCodec.ReadClass(ref memory, publicClass);
		if (CallDispatcher(publicValue.cl_Dispatcher.Entry, publicClass, APTR.Null, probe,
			APTR.FromPointer(PrivateCallerBase)) != ProbeResult ||
			!CheckContext(ref platform, privateContext, 1, 1, 1) ||
			!CheckContext(ref platform, publicContext, 1, 1, 1)) return 421;
		if (!MuiMasterPrivateRootCodec.TryRead(ref memory, root, out var rootValue) ||
			rootValue.ActiveCallbackDepth != 0 || rootValue.ActiveDispatchDepth != 0 ||
			rootValue.ExternalClassHead == 0) return 422;
		var originalBindings = rootValue.ExternalClassHead;

		// A missing owner cannot acquire a binding or native class.
		var invalidOwner = platform;
		invalidOwner.Context = invalidContext;
		var beforeFailure = Exec.AvailMem(flags);
		if (invalidOwner.MakeCustomClass(baseClass, 4, APTR.ExportAddress(CustomExport), APTR.Null).IsNotNull ||
			Exec.AvailMem(flags) != beforeFailure) return 423;

		// First allocation stage: the binding itself cannot fit.
		var largest = Exec.AvailMem(flags | Exec.MemoryFlags.Largest);
		var held = Exec.AllocMem(largest, flags);
		if (held.IsNull) return 424;
		var exhausted = Exec.AvailMem(flags);
		if (platform.MakeCustomClass(baseClass, 4, APTR.ExportAddress(CustomExport), APTR.Null).IsNotNull ||
			Exec.AvailMem(flags) != exhausted || !BindingsEqual(ref platform, root, originalBindings)) return 425;
		Exec.FreeMem(held, largest);
		if (Exec.AvailMem(flags) != beforeFailure) return 426;

		// Second stage: leave precisely one allocator-rounded binding free.
		// Measure the actual debit instead of assuming an allocation quantum or
		// free-block metadata size; the one usable extent must fit the binding
		// but cannot hold Intuition's IClass once the binding is live.
		var beforeBinding = Exec.AvailMem(flags);
		var bindingRoom = Exec.AllocMem(MuiNativeClassBinding.Size, flags);
		if (bindingRoom.IsNull) return 427;
		var bindingDebit = beforeBinding - Exec.AvailMem(flags);
		if (bindingDebit < MuiNativeClassBinding.Size || bindingDebit >= IClass.Size) return 427;
		var remaining = Exec.AvailMem(flags | Exec.MemoryFlags.Largest);
		held = Exec.AllocMem(remaining, flags);
		if (held.IsNull) return 428;
		Exec.FreeMem(bindingRoom, MuiNativeClassBinding.Size);
		var bindingOnly = Exec.AvailMem(flags);
		if (bindingOnly < MuiNativeClassBinding.Size || bindingOnly >= IClass.Size ||
			bindingOnly > bindingDebit || Exec.AvailMem(flags | Exec.MemoryFlags.Largest) != bindingOnly ||
			platform.MakeCustomClass(baseClass, 4, APTR.ExportAddress(CustomExport), APTR.Null).IsNotNull ||
			Exec.AvailMem(flags) != bindingOnly || !BindingsEqual(ref platform, root, originalBindings)) return 429;
		Exec.FreeMem(held, remaining);
		if (Exec.AvailMem(flags) != beforeFailure) return 430;

		if (!platform.FreeCustomClass(privateClass) ||
			BOOPSIGuestCodec.ReadClass(ref memory, publicClass).cl_SubclassCount != 0 ||
			!platform.FreeCustomClass(publicClass)) return 431;
		baseValue = BOOPSIGuestCodec.ReadClass(ref memory, baseClass);
		if (baseValue.cl_SubclassCount != 0 || baseValue.cl_ObjectCount != 0 ||
			!platform.RemoveClass(baseClass) || !platform.FreeClass(baseClass)) return 432;
		if (!BindingsEqual(ref platform, root, 0) || !MuiExecPrivateRootOwner.TryDestroy(ref root) ||
			root.IsNotNull) return 433;
		platform.Free(baseContext, ClassContext.Size);
		platform.Free(publicContext, ClassContext.Size);
		platform.Free(privateContext, ClassContext.Size);
		platform.Free(probe, ClassProbeMessage.Size);
		Exec.FreeMem(invalidContext, MuiNativeClassContext.Size);
		Exec.FreeMem(providerContext, MuiNativeClassContext.Size);
		if (Exec.AvailMem(flags) != initialAvailable ||
			Exec.AvailMem(flags | Exec.MemoryFlags.Largest) != initialLargest) return 434;
		return 42;
	}

	private static bool CheckNativeRedrawLayerBinding(
		ref MuiNativeClassPlatform platform, ref MuiNativeClassMemory memory,
		APTR publicObjects, APTR ownerRoot, APTR obj, APTR sidecar)
	{
		var fixture = default(NativeRedrawLayerFixture);
		fixture.Object = obj;
		fixture.Sidecar = sidecar;
		fixture.PublicObjects = publicObjects;
		fixture.OwnerRoot = ownerRoot;
		fixture.RenderInfo = platform.Allocate(MuiDrawingRenderInfoRecord.Size,
			MuiHeadlessLayout.AllocationFlags);
		fixture.ReplacementRenderInfo = platform.Allocate(
			MuiDrawingRenderInfoRecord.Size, MuiHeadlessLayout.AllocationFlags);
		fixture.RastPort = platform.Allocate(MuiDrawingRasterPortRecord.Size,
			MuiHeadlessLayout.AllocationFlags);
		fixture.ReplacementRastPort = platform.Allocate(
			MuiDrawingRasterPortRecord.Size, MuiHeadlessLayout.AllocationFlags);
		fixture.Layer = platform.Allocate(Layer.Size,
			MuiHeadlessLayout.AllocationFlags);
		fixture.ReplacementLayer = platform.Allocate(Layer.Size,
			MuiHeadlessLayout.AllocationFlags);
		var complete = fixture.RenderInfo.IsNotNull &&
			fixture.ReplacementRenderInfo.IsNotNull && fixture.RastPort.IsNotNull &&
			fixture.ReplacementRastPort.IsNotNull && fixture.Layer.IsNotNull &&
			fixture.ReplacementLayer.IsNotNull;
		var result = false;
		var recordsInitialized = false;
		var attributePublished = false;
		var renderInfoValue = default(MuiDrawingRenderInfoRecord);
		var rasterPortValue = default(MuiDrawingRasterPortRecord);
		var planned = default(MuiNativeRedrawLayerBinding);
		if (complete)
		{
			renderInfoValue.RastPort = fixture.RastPort;
			var replacementRenderInfoValue = default(MuiDrawingRenderInfoRecord);
			replacementRenderInfoValue.RastPort = fixture.ReplacementRastPort;
			rasterPortValue.Layer = fixture.Layer;
			var replacementRasterPortValue = default(MuiDrawingRasterPortRecord);
			replacementRasterPortValue.Layer = fixture.Layer;
			recordsInitialized = true;
			result = MuiDrawingRenderInfoCodec.Write(ref memory,
				fixture.RenderInfo, renderInfoValue) &&
			MuiDrawingRenderInfoCodec.Write(ref memory,
				fixture.ReplacementRenderInfo, replacementRenderInfoValue) &&
			MuiDrawingRasterPortStructCodec.Write(ref memory, fixture.RastPort,
				rasterPortValue) &&
			MuiDrawingRasterPortStructCodec.Write(ref memory,
				fixture.ReplacementRastPort, replacementRasterPortValue);
			if (result)
			{
				result = MuiNativeObjectStateCore.SetAttribute(ref platform,
					fixture.Sidecar,
					MuiAreaWindowRelationshipCore.RenderInfoAttribute,
					fixture.RenderInfo.Raw, false);
				attributePublished = result;
			}
			if (result)
				result = MuiNativeRedrawServiceCore.TryReadLayerBinding(ref memory,
					fixture.Object, fixture.Sidecar, out planned);
			if (result)
				result = planned.Object == fixture.Object &&
					planned.Sidecar == fixture.Sidecar &&
					planned.RenderInfo == fixture.RenderInfo &&
					planned.RastPort == fixture.RastPort &&
					planned.Layer == fixture.Layer;
			if (result)
				result = MuiNativeRedrawServiceCore.TryValidateLayerBinding(
					ref memory, fixture.PublicObjects, fixture.OwnerRoot,
					fixture.Object, planned);

			if (result)
			{
				renderInfoValue.RastPort = fixture.ReplacementRastPort;
				result = MuiDrawingRenderInfoCodec.Write(ref memory,
					fixture.RenderInfo, renderInfoValue) &&
				!MuiNativeRedrawServiceCore.TryValidateLayerBinding(ref memory,
					fixture.PublicObjects, fixture.OwnerRoot, fixture.Object,
					planned);
				renderInfoValue.RastPort = fixture.RastPort;
			}
			if (result)
			{
				rasterPortValue.Layer = fixture.ReplacementLayer;
				result = MuiDrawingRasterPortStructCodec.Write(ref memory,
					fixture.RastPort, rasterPortValue) &&
				!MuiNativeRedrawServiceCore.TryValidateLayerBinding(ref memory,
					fixture.PublicObjects, fixture.OwnerRoot, fixture.Object,
					planned);
				rasterPortValue.Layer = fixture.Layer;
			}
			if (result)
				result = MuiNativeObjectStateCore.SetAttribute(ref platform,
					fixture.Sidecar,
					MuiAreaWindowRelationshipCore.RenderInfoAttribute,
					fixture.ReplacementRenderInfo.Raw, false) &&
				!MuiNativeRedrawServiceCore.TryValidateLayerBinding(ref memory,
					fixture.PublicObjects, fixture.OwnerRoot, fixture.Object,
					planned);
		}

		if (recordsInitialized)
		{
			var renderInfoRestored = MuiDrawingRenderInfoCodec.Write(ref memory,
				fixture.RenderInfo, renderInfoValue);
			var rasterPortRestored = MuiDrawingRasterPortStructCodec.Write(
				ref memory, fixture.RastPort, rasterPortValue);
			result = result && renderInfoRestored && rasterPortRestored;
		}
		if (attributePublished)
			result = MuiNativeObjectStateCore.SetAttribute(ref platform,
				fixture.Sidecar,
				MuiAreaWindowRelationshipCore.RenderInfoAttribute,
				fixture.RenderInfo.Raw, false) && result;
		if (fixture.ReplacementLayer.IsNotNull)
			platform.Free(fixture.ReplacementLayer, Layer.Size);
		if (fixture.Layer.IsNotNull) platform.Free(fixture.Layer, Layer.Size);
		if (fixture.ReplacementRastPort.IsNotNull)
			platform.Free(fixture.ReplacementRastPort,
				MuiDrawingRasterPortRecord.Size);
		if (fixture.RastPort.IsNotNull)
			platform.Free(fixture.RastPort, MuiDrawingRasterPortRecord.Size);
		if (fixture.ReplacementRenderInfo.IsNotNull)
			platform.Free(fixture.ReplacementRenderInfo,
				MuiDrawingRenderInfoRecord.Size);
		if (fixture.RenderInfo.IsNotNull)
			platform.Free(fixture.RenderInfo, MuiDrawingRenderInfoRecord.Size);
		return result;
	}

	[M68kExport(BaseExport)]
	[return: M68kRegister(M68kRegister.D0)]
	public static uint BaseDispatch([M68kRegister(M68kRegister.A0)] APTR cls,
		[M68kRegister(M68kRegister.A2)] APTR obj, [M68kRegister(M68kRegister.A1)] APTR message,
		[M68kRegister(M68kRegister.A6)] APTR callerBase) =>
		Dispatch(cls, obj, message, callerBase, false);

	[M68kExport(CustomExport)]
	[return: M68kRegister(M68kRegister.D0)]
	public static uint CustomDispatch([M68kRegister(M68kRegister.A0)] APTR cls,
		[M68kRegister(M68kRegister.A2)] APTR obj, [M68kRegister(M68kRegister.A1)] APTR message,
		[M68kRegister(M68kRegister.A6)] APTR callerBase) =>
		Dispatch(cls, obj, message, callerBase, true);

	private static uint Dispatch(APTR cls, APTR obj, APTR message, APTR callerBase, bool bound)
	{
		var memory = default(MuiNativeClassMemory);
		var classValue = BOOPSIGuestCodec.ReadClass(ref memory, cls);
		var context = APTR.FromPointer(classValue.cl_UserData);
		if (!ClassContextCodec.TryRead(ref memory, context, out var state) ||
			!ClassProbeMessageCodec.TryReadMethodId(ref memory, message, out var methodId)) return 0;
		state.LastBase = callerBase;
		if (state.ExpectedBase.IsNotNull && callerBase != state.ExpectedBase) state.Failure |= 1;
		if (bound)
		{
			var platform = new MuiNativeClassPlatform { Context = context };
			var root = state.Platform.OwnerRoot;
			if (!MuiNativeClassBindingCodec.TryRead(ref memory, classValue.cl_Dispatcher.Data, out var binding) ||
				binding.ActiveCalls != 1 || !MuiMasterPrivateRootCodec.TryRead(ref memory, root, out var rootState) ||
				rootState.ActiveCallbackDepth != state.ExpectedDepth) state.Failure |= 2;
			if (platform.FreeCustomClass(cls)) state.Failure |= 4;
			if (MuiExecPrivateRootOwner.TryDestroy(ref root) || root != state.Platform.OwnerRoot) state.Failure |= 8;
		}
		if (methodId == BOOPSI.OM_NEW) state.NewCount++;
		else if (methodId == BOOPSI.OM_DISPOSE) state.DisposeCount++;
		else if (methodId == ProbeMethod) state.ProbeCount++;
		else state.Failure |= 16;
		if (!ClassContextCodec.Write(ref memory, context, state)) return 0;
		if (methodId == ProbeMethod) return ProbeResult;
		return CallSuper(state.SuperEntry, state.Platform.IntuitionBase, cls, obj, message);
	}

	private static uint CheckClassService(ref MuiNativeClassPlatform platform,
		APTR root, APTR baseName, APTR baseClass, APTR baseContext)
	{
		// This fixture owns these temporary state records. They are a native
		// generic-backend proof, not a public library's acquisition/lifecycle.
		// The registry entry borrows the already-real IClass and must not free it.
		var memory = default(MuiNativeClassMemory);
		var flags = Exec.MemoryFlags.Public;
		var available = Exec.AvailMem(flags);
		var largest = Exec.AvailMem(flags | Exec.MemoryFlags.Largest);
		var headless = platform.Allocate(MuiHeadlessStateRecord.Size, (uint)(flags | Exec.MemoryFlags.Clear));
		var service = platform.Allocate(MuiClassServiceStateRecord.Size, (uint)(flags | Exec.MemoryFlags.Clear));
		var publicObjects = platform.Allocate(MuiNativePublicObjectRegistryRecord.Size,
			(uint)(flags | Exec.MemoryFlags.Clear));
		if (headless.IsNull || service.IsNull || publicObjects.IsNull) return 440;
		if (!MuiHeadlessObjectCore.Initialize(ref platform, headless) ||
			!MuiClassServiceCore.Initialize(ref platform, service, headless)) return 441;
		var publicState = default(MuiNativePublicObjectRegistryRecord);
		publicState.Signature = MuiNativePublicObjectRegistryRecord.Magic;
		publicState.Revision = MuiNativePublicObjectRegistryRecord.Version;
		if (!MuiNativePublicObjectRegistryCodec.Write(ref platform, publicObjects,
			publicState)) return 441;
		var originalClass = BOOPSIGuestCodec.ReadClass(ref memory, baseClass);
		var registry = MuiHeadlessObjectCore.RegisterExternalClass(ref platform,
			headless, baseName, baseClass, originalClass.cl_Super);
		if (registry.IsNull || MuiHeadlessObjectCore.FindClassByName(ref platform,
			headless, baseName) != registry) return 442;
		// The public-object path keeps the Intuition object native and records
		// only a named binding/lease in the library-owned registry. This exercises
		// NewObjectA/DisposeObject semantics without inferring any caller object
		// layout or using an offset-based side table.
		var nativeTags = platform.Allocate(MuiAslTagItemRecord.Size * 3,
			(uint)(flags | Exec.MemoryFlags.Clear));
		var nativeTag = default(MuiAslTagItemRecord);
		nativeTag.Tag = 0xF00D0001;
		nativeTag.Data = 0x11223344;
		var nativeDone = default(MuiAslTagItemRecord);
		nativeDone.Tag = MuiAslTagListCore.TagDone;
		if (nativeTags.IsNull ||
			!MuiAslTagItemVectorCodec.TryWrite(ref platform, nativeTags, 0,
				nativeTag) || !MuiAslTagItemVectorCodec.TryWrite(ref platform,
				nativeTags, 1, nativeDone) ||
			!MuiAslTagItemVectorCodec.TryWrite(ref platform, nativeTags, 2,
				nativeDone)) return 487;
		var publicObject = MuiNativePublicObjectCore.NewObject(ref platform, service,
			root, publicObjects, baseName, nativeTags);
		if (publicObject.IsNull) return 451;
		if (MuiClassServiceCore.ReferenceCount(ref platform, service, baseClass) != 1)
			return 452;
		if (MuiClassServiceCore.ObjectLeaseCount(ref platform, service, baseClass) != 1)
			return 453;
		if (!MuiNativePublicObjectRegistryCodec.TryRead(ref memory, publicObjects,
			out var objectState))
			return 454;
		if (objectState.Head.IsNull) return 455;
		if (!MuiNativePublicObjectBindingCodec.TryRead(ref memory, objectState.Head,
			out var publicBinding) || publicBinding.Object != publicObject ||
			publicBinding.Sidecar.IsNull ||
			!MuiNativeMuiObjectCodec.TryRead(ref memory, publicBinding.Sidecar,
				out var publicSidecar) || publicSidecar.Object != publicObject ||
			publicSidecar.Class != baseClass || publicSidecar.OwnerRoot != root ||
			(publicSidecar.Flags & MuiNativeMuiObjectRecord.ObjectInitialized) == 0)
			return 456;
		if (publicSidecar.Attributes.IsNull ||
			!MuiNativeObjectAttributeCodec.TryRead(ref memory,
				publicSidecar.Attributes, out var publicAttribute) ||
			publicAttribute.Attribute != 0xF00D0001 ||
			publicAttribute.Value != 0x11223344 ||
			!MuiNativePublicObjectCore.GetAttribute(ref platform, publicObjects,
				root, publicObject, 0xF00D0001, out var publicAttributeValue) ||
			publicAttributeValue != 0x11223344 ||
			!MuiNativePublicObjectCore.SetAttribute(ref platform, publicObjects,
				root, publicObject, 0xF00D0001, 0x55667788) ||
			!MuiNativePublicObjectCore.GetAttribute(ref platform, publicObjects,
				root, publicObject, 0xF00D0001, out publicAttributeValue) ||
			publicAttributeValue != 0x55667788 ||
			!MuiNativePublicObjectCore.AddNotification(ref platform,
				publicObjects, root, publicObject, 0xF00D0001, 0x55667788,
				publicObject, 2, 0, out var nativeNotification)) return 456;
		if (nativeNotification.IsNull ||
			!MuiNativeMuiObjectCodec.TryRead(ref memory, publicBinding.Sidecar,
				out publicSidecar) || publicSidecar.Notifications.IsNull ||
			!MuiNativeObjectNotificationCodec.TryRead(ref memory,
				publicSidecar.Notifications, out var notificationValue) ||
			notificationValue.Destination != publicObject ||
			notificationValue.FollowCount != 2) return 456;
		if (!CheckNativeRedrawLayerBinding(ref platform, ref memory,
			publicObjects, root, publicObject, publicBinding.Sidecar)) return 488;
		if (!MuiNativePublicObjectCore.DisposeObject(ref platform, service, root,
			publicObjects, publicObject)) return 456;
		platform.Free(nativeTags, MuiAslTagItemRecord.Size * 3);
		if (MuiClassServiceCore.ReferenceCount(ref platform, service, baseClass) != 0)
			return 457;
		if (MuiClassServiceCore.ObjectLeaseCount(ref platform, service, baseClass) != 0)
			return 458;
		if (!MuiNativePublicObjectRegistryCodec.TryRead(ref memory, publicObjects,
			out objectState))
			return 459;
		if (objectState.Head.IsNotNull) return 460;
		if (!ClassContextCodec.TryRead(ref memory, baseContext,
			out var contextBeforeRectangles)) return 461;

		// The resident MakeObjectA bridge is qualified against the same real
		// Intuition object path. Alias the simple MorphOS builtin class names to
		// the already-live base class so this exercises named class/tag records,
		// class leasing, public binding, and native OM_DISPOSE without inventing
		// another class.
		var rectangleName = APTR.FromPointer(CString.ToUInt32(
			CString.FromLiteral("Rectangle.mui")));
		var rectangleRegistry = MuiHeadlessObjectCore.RegisterExternalClass(ref platform,
			headless, rectangleName, baseClass, originalClass.cl_Super);
		if (rectangleRegistry.IsNull) return 462;
		var textName = APTR.FromPointer(CString.ToUInt32(
			CString.FromLiteral("Text.mui")));
		var textRegistry = MuiHeadlessObjectCore.RegisterExternalClass(ref platform,
			headless, textName, baseClass, originalClass.cl_Super);
		if (textRegistry.IsNull) return 463;
		var imageName = APTR.FromPointer(CString.ToUInt32(
			CString.FromLiteral("Image.mui")));
		var imageRegistry = MuiHeadlessObjectCore.RegisterExternalClass(ref platform,
			headless, imageName, baseClass, originalClass.cl_Super);
		if (imageRegistry.IsNull) return 464;
		var cycleName = APTR.FromPointer(CString.ToUInt32(
			CString.FromLiteral("Cycle.mui")));
		var cycleRegistry = MuiHeadlessObjectCore.RegisterExternalClass(ref platform,
			headless, cycleName, baseClass, originalClass.cl_Super);
		if (cycleRegistry.IsNull) return 465;
		var radioName = APTR.FromPointer(CString.ToUInt32(
			CString.FromLiteral("Radio.mui")));
		var radioRegistry = MuiHeadlessObjectCore.RegisterExternalClass(ref platform,
			headless, radioName, baseClass, originalClass.cl_Super);
		if (radioRegistry.IsNull) return 466;
		var sliderName = APTR.FromPointer(CString.ToUInt32(
			CString.FromLiteral("Slider.mui")));
		var sliderRegistry = MuiHeadlessObjectCore.RegisterExternalClass(ref platform,
			headless, sliderName, baseClass, originalClass.cl_Super);
		if (sliderRegistry.IsNull) return 467;
		var stringName = APTR.FromPointer(CString.ToUInt32(
			CString.FromLiteral("String.mui")));
		var stringRegistry = MuiHeadlessObjectCore.RegisterExternalClass(ref platform,
			headless, stringName, baseClass, originalClass.cl_Super);
		if (stringRegistry.IsNull) return 468;
		var numericButtonName = APTR.FromPointer(CString.ToUInt32(
			CString.FromLiteral("Numericbutton.mui")));
		var numericButtonRegistry = MuiHeadlessObjectCore.RegisterExternalClass(ref platform,
			headless, numericButtonName, baseClass, originalClass.cl_Super);
		if (numericButtonRegistry.IsNull) return 469;
		var menuitemName = APTR.FromPointer(CString.ToUInt32(
			CString.FromLiteral("Menuitem.mui")));
		var menuitemRegistry = MuiHeadlessObjectCore.RegisterExternalClass(ref platform,
			headless, menuitemName, baseClass, originalClass.cl_Super);
		if (menuitemRegistry.IsNull) return 481;
		var menuName = APTR.FromPointer(CString.ToUInt32(
			CString.FromLiteral("Menu.mui")));
		var menuRegistry = MuiHeadlessObjectCore.RegisterExternalClass(ref platform,
			headless, menuName, baseClass, originalClass.cl_Super);
		if (menuRegistry.IsNull) return 482;
		var menustripName = APTR.FromPointer(CString.ToUInt32(
			CString.FromLiteral("Menustrip.mui")));
		var menustripRegistry = MuiHeadlessObjectCore.RegisterExternalClass(ref platform,
			headless, menustripName, baseClass, originalClass.cl_Super);
		if (menustripRegistry.IsNull) return 483;
		var rectangleParameters = platform.Allocate(
			MuiMakeObjectParameterRecord.Size, (uint)(flags | Exec.MemoryFlags.Clear));
		if (rectangleParameters.IsNull) return 470;
		if (!CheckNativeRectangle(ref platform, service, root, publicObjects,
			rectangleParameters, MuiMakeObjectServiceCore.MUIO_HSpace, 4) ||
			!CheckNativeRectangle(ref platform, service, root, publicObjects,
				rectangleParameters, MuiMakeObjectServiceCore.MUIO_VSpace, 5) ||
			!CheckNativeRectangle(ref platform, service, root, publicObjects,
				rectangleParameters, MuiMakeObjectServiceCore.MUIO_HBar, 1) ||
			!CheckNativeRectangle(ref platform, service, root, publicObjects,
				rectangleParameters, MuiMakeObjectServiceCore.MUIO_VBar, 1) ||
			!CheckNativeRectangle(ref platform, service, root, publicObjects,
				rectangleParameters, MuiMakeObjectServiceCore.MUIO_BarTitle,
				CString.ToUInt32(CString.FromLiteral("Native bar"))) ||
			!CheckNativeMakeObject(ref platform, service, root, publicObjects,
				rectangleParameters, MuiMakeObjectServiceCore.MUIO_Button, 1,
				CString.ToUInt32(CString.FromLiteral("Native button")), 0) ||
			!CheckNativeMakeObject(ref platform, service, root, publicObjects,
				rectangleParameters, MuiMakeObjectServiceCore.MUIO_Label, 2,
				CString.ToUInt32(CString.FromLiteral("Native label")), 0) ||
			!CheckNativeMakeObject(ref platform, service, root, publicObjects,
				rectangleParameters, MuiMakeObjectServiceCore.MUIO_Checkmark, 1,
				1, 0) ||
			!CheckNativeMakeObject(ref platform, service, root, publicObjects,
				rectangleParameters, MuiMakeObjectServiceCore.MUIO_PopButton, 1,
				15, 0) ||
			!CheckNativeMakeObject(ref platform, service, root, publicObjects,
				rectangleParameters, MuiMakeObjectServiceCore.MUIO_Cycle, 2,
				CString.ToUInt32(CString.FromLiteral("Native cycle")),
				CString.ToUInt32(CString.FromLiteral("Native cycle entries"))) ||
			!CheckNativeMakeObject(ref platform, service, root, publicObjects,
				rectangleParameters, MuiMakeObjectServiceCore.MUIO_Radio, 2,
				CString.ToUInt32(CString.FromLiteral("Native radio")),
				CString.ToUInt32(CString.FromLiteral("Native radio entries"))) ||
			!CheckNativeMakeObject(ref platform, service, root, publicObjects,
				rectangleParameters, MuiMakeObjectServiceCore.MUIO_Slider, 3,
				CString.ToUInt32(CString.FromLiteral("Native slider")), 0,
				100) ||
			!CheckNativeMakeObject(ref platform, service, root, publicObjects,
				rectangleParameters, MuiMakeObjectServiceCore.MUIO_String, 2,
				CString.ToUInt32(CString.FromLiteral("Native string")), 32) ||
			!CheckNativeMakeObject(ref platform, service, root, publicObjects,
				rectangleParameters, MuiMakeObjectServiceCore.MUIO_NumericButton, 4,
				CString.ToUInt32(CString.FromLiteral("Native numeric")), 0,
				100, CString.ToUInt32(CString.FromLiteral("%ld"))) ||
			!CheckNativeMakeObject(ref platform, service, root, publicObjects,
				rectangleParameters, MuiMakeObjectServiceCore.MUIO_Menuitem, 4,
				CString.ToUInt32(CString.FromLiteral("Native menu item")),
				CString.ToUInt32(CString.FromLiteral("m")), 0, 0)) return 471;
		platform.Free(rectangleParameters, MuiMakeObjectParameterRecord.Size);
		var menuProof = CheckNativeMenustrip(ref platform, service, root,
			publicObjects, baseClass);
		if (menuProof != 0) return menuProof;
		if (!MuiHeadlessObjectCore.DeleteClass(ref platform, headless,
			rectangleRegistry) || MuiHeadlessObjectCore.FindClassByName(ref platform,
				headless, rectangleName).IsNotNull ||
			!MuiHeadlessObjectCore.DeleteClass(ref platform, headless,
				textRegistry) || MuiHeadlessObjectCore.FindClassByName(ref platform,
					headless, textName).IsNotNull ||
			!MuiHeadlessObjectCore.DeleteClass(ref platform, headless,
				imageRegistry) || MuiHeadlessObjectCore.FindClassByName(ref platform,
					headless, imageName).IsNotNull ||
			!MuiHeadlessObjectCore.DeleteClass(ref platform, headless,
				cycleRegistry) || MuiHeadlessObjectCore.FindClassByName(ref platform,
					headless, cycleName).IsNotNull ||
			!MuiHeadlessObjectCore.DeleteClass(ref platform, headless,
				radioRegistry) || MuiHeadlessObjectCore.FindClassByName(ref platform,
					headless, radioName).IsNotNull ||
			!MuiHeadlessObjectCore.DeleteClass(ref platform, headless,
				sliderRegistry) || MuiHeadlessObjectCore.FindClassByName(ref platform,
					headless, sliderName).IsNotNull ||
			!MuiHeadlessObjectCore.DeleteClass(ref platform, headless,
				stringRegistry) || MuiHeadlessObjectCore.FindClassByName(ref platform,
					headless, stringName).IsNotNull ||
			!MuiHeadlessObjectCore.DeleteClass(ref platform, headless,
				numericButtonRegistry) || MuiHeadlessObjectCore.FindClassByName(ref platform,
				headless, numericButtonName).IsNotNull ||
			!MuiHeadlessObjectCore.DeleteClass(ref platform, headless,
				menuitemRegistry) || MuiHeadlessObjectCore.FindClassByName(ref platform,
					headless, menuitemName).IsNotNull ||
			!MuiHeadlessObjectCore.DeleteClass(ref platform, headless,
				menuRegistry) || MuiHeadlessObjectCore.FindClassByName(ref platform,
					headless, menuName).IsNotNull ||
			!MuiHeadlessObjectCore.DeleteClass(ref platform, headless,
				menustripRegistry) || MuiHeadlessObjectCore.FindClassByName(ref platform,
					headless, menustripName).IsNotNull ||
			!ClassContextCodec.Write(ref memory, baseContext,
				contextBeforeRectangles)) return 472;
		if (MuiClassServiceCore.GetClass(ref platform, service, baseName) != baseClass ||
			MuiClassServiceCore.GetClass(ref platform, service, baseName) != baseClass ||
			MuiClassServiceCore.ReferenceCount(ref platform, service, baseClass) != 2) return 443;
		if (!MuiClassServiceCore.FreeClass(ref platform, service, baseClass) ||
			MuiClassServiceCore.ReferenceCount(ref platform, service, baseClass) != 1 ||
			!MuiClassServiceCore.FreeClass(ref platform, service, baseClass) ||
			MuiClassServiceCore.ReferenceCount(ref platform, service, baseClass) != 0 ||
			MuiClassServiceCore.FreeClass(ref platform, service, baseClass) ||
			!MuiClassServiceStateCodec.TryRead(ref platform, service, out var serviceValue) ||
			serviceValue.Head.IsNotNull || serviceValue.Headless != headless) return 444;

		// The service now creates an actual private native class, publishes its
		// typed MUI_CustomClass, and owns the named-super lease until deletion.
		// No object is created here; the main fixture proves actual dispatch.
		var custom = MuiClassServiceCore.CreateCustomClass(ref platform, service,
			APTR.Null, baseName, APTR.Null, 6, APTR.ExportAddress(CustomExport));
		if (custom.IsNull || !MuiCustomClassCodec.TryRead(ref platform, custom,
			out var customValue) || customValue.Super != baseClass || customValue.Class.IsNull) return 445;
		var nativeClass = BOOPSIGuestCodec.ReadClass(ref memory, customValue.Class);
		var parentClass = BOOPSIGuestCodec.ReadClass(ref memory, baseClass);
		if (!ClassContextCodec.TryRead(ref memory, baseContext, out var baseState)) return 445;
		var customContext = CreateContext(ref platform,
			baseState.Platform.IntuitionBase, root, baseState.SuperEntry,
			APTR.Null, 1);
		if (customContext.IsNull || !SetContext(ref platform, customValue.Class,
			customContext)) return 445;
		if (nativeClass.cl_Super != baseClass || nativeClass.cl_InstOffset != 4 ||
			nativeClass.cl_InstSize != 6 || nativeClass.cl_ObjectCount != 0 ||
			parentClass.cl_SubclassCount != originalClass.cl_SubclassCount + 1 ||
			MuiClassServiceCore.ReferenceCount(ref platform, service, baseClass) != 1 ||
			!MuiMasterPrivateRootCodec.TryRead(ref memory, root, out var rootValue) ||
			rootValue.ExternalClassHead == 0) return 446;
		var directCustomObject = MuiClassServiceCore.CreateCustomObject(ref platform,
			service, custom, APTR.Null);
		if (directCustomObject.IsNull) return 476;
		if (MuiClassServiceCore.ObjectLeaseCount(ref platform, service,
			customValue.Class) != 1) return 477;
		if (!MuiClassServiceCore.DisposeCustomObject(ref platform, service, custom,
			directCustomObject) ||
			MuiClassServiceCore.ObjectLeaseCount(ref platform, service,
				customValue.Class) != 0) return 478;
		var customObject = MuiNativePublicObjectCore.NewCustomObject(ref platform,
			service, root, publicObjects, custom, APTR.Null);
		if (customObject.IsNull) return 473;
		var customNativeClass = BOOPSIGuestCodec.ReadClass(ref memory,
			customValue.Class);
		if (!MuiNativePublicObjectCore.TryFindBinding(ref platform, publicObjects,
			root, customObject, out var customPublicBinding) ||
			!MuiNativeClassBindingCodec.TryRead(ref memory,
				customNativeClass.cl_Dispatcher.Data, out var customNativeBinding) ||
			customNativeBinding.ObjectHead.IsNull ||
			!MuiNativeClassObjectBindingCodec.TryRead(ref memory,
				customNativeBinding.ObjectHead, out var customNativeObjectBinding) ||
			customNativeObjectBinding.Sidecar != customPublicBinding.Sidecar ||
			(customNativeObjectBinding.LifecycleFlags &
				MuiNativeClassObjectBinding.PublicSidecarBorrowed) == 0)
			return 481;
		if (MuiClassServiceCore.ObjectLeaseCount(ref platform, service,
			customValue.Class) != 1) return 475;
		if (!MuiNativePublicObjectCore.DisposeObject(ref platform, service, root,
			publicObjects, customObject) ||
			MuiClassServiceCore.ObjectLeaseCount(ref platform, service,
				customValue.Class) != 0)
			return 474;
		if (!CheckContext(ref platform, customContext, 2, 2, 0)) return 479;
		// The custom dispatcher deliberately super-dispatches through the shared
		// base class. Restore that caller-owned fixture context before the outer
		// class qualification continues; the service slice has its own context
		// counters and must not perturb the earlier base-class assertions.
		if (!ClassContextCodec.Write(ref memory, baseContext, baseState)) return 480;
		if (!MuiClassServiceCore.DeleteCustomClass(ref platform, service, custom) ||
			MuiClassServiceCore.ReferenceCount(ref platform, service, baseClass) != 0 ||
			!MuiClassServiceStateCodec.TryRead(ref platform, service, out serviceValue) ||
			serviceValue.Head.IsNotNull || !BindingsEqual(ref platform, root, 0) ||
			BOOPSIGuestCodec.ReadClass(ref memory, baseClass).cl_SubclassCount != originalClass.cl_SubclassCount)
			return 447;
		if (!MuiHeadlessObjectCore.DeleteClass(ref platform, headless, registry))
			return 448;
		if (!MuiHeadlessStateCodec.TryRead(ref platform, headless,
			out var headlessValue)) return 449;
		if (headlessValue.Classes.IsNotNull) return 450;
		if (headlessValue.Objects.IsNotNull) return 451;
		parentClass = BOOPSIGuestCodec.ReadClass(ref memory, baseClass);
		if (parentClass.cl_ID != originalClass.cl_ID) return 452;
		if (parentClass.cl_Flags != originalClass.cl_Flags) return 453;
		if (parentClass.cl_ObjectCount != originalClass.cl_ObjectCount)
			return 500 + parentClass.cl_ObjectCount;
		if (parentClass.cl_SubclassCount != originalClass.cl_SubclassCount) return 455;
		platform.Free(service, MuiClassServiceStateRecord.Size);
		platform.Free(headless, MuiHeadlessStateRecord.Size);
		platform.Free(publicObjects, MuiNativePublicObjectRegistryRecord.Size);
		platform.Free(customContext, ClassContext.Size);
		if (Exec.AvailMem(flags) != available || Exec.AvailMem(flags | Exec.MemoryFlags.Largest) != largest)
			return 449;
		return 0;
	}

	private static uint CheckNativeMenustrip(ref MuiNativeClassPlatform platform,
		APTR service, APTR root, APTR publicObjects, APTR baseClass)
	{
		var memory = default(MuiNativeClassMemory);
		var flags = Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear;
		var vectorBytes = MuiNewMenuRecord.Size * 4;
		var newMenu = platform.Allocate(vectorBytes, (uint)flags);
		var parameters = platform.Allocate(MuiMakeObjectParameterRecord.Size,
			(uint)flags);
		if (newMenu.IsNull || parameters.IsNull)
		{
			if (newMenu.IsNotNull) platform.Free(newMenu, vectorBytes);
			if (parameters.IsNotNull)
				platform.Free(parameters, MuiMakeObjectParameterRecord.Size);
			return 484;
		}
		var title = default(MuiNewMenuRecord);
		title.Type = MuiNewMenuTypeRecord.Title;
		title.Label = CString.ToUInt32(CString.FromLiteral("Native File"));
		title.UserData = 0xA1;
		var item = default(MuiNewMenuRecord);
		item.Type = MuiNewMenuTypeRecord.Item;
		item.Label = CString.ToUInt32(CString.FromLiteral("Open"));
		item.CommandKey = CString.ToUInt32(CString.FromLiteral("o"));
		item.UserData = 0xA2;
		var sub = default(MuiNewMenuRecord);
		sub.Type = MuiNewMenuTypeRecord.Sub;
		sub.Label = CString.ToUInt32(CString.FromLiteral("Recent"));
		sub.CommandKey = CString.ToUInt32(CString.FromLiteral("r"));
		sub.UserData = 0xA3;
		var end = default(MuiNewMenuRecord);
		end.Type = MuiNewMenuTypeRecord.End;
		var menuCursor = default(MuiNewMenuCursor);
		menuCursor.Base = newMenu;
		menuCursor.Index = 0;
		var wrote = MuiNewMenuVectorCodec.TryWrite(ref memory, menuCursor, title);
		menuCursor.Index = 1;
		wrote = wrote && MuiNewMenuVectorCodec.TryWrite(ref memory,
			menuCursor, item);
		menuCursor.Index = 2;
		wrote = wrote && MuiNewMenuVectorCodec.TryWrite(ref memory,
			menuCursor, sub);
		menuCursor.Index = 3;
		wrote = wrote && MuiNewMenuVectorCodec.TryWrite(ref memory,
			menuCursor, end);
		var parameterRecord = default(MuiMakeObjectParameterRecord);
		parameterRecord.First = newMenu.Raw;
		parameterRecord.Second = 0;
		wrote = wrote && MuiMakeObjectParameterCodec.TryWrite(ref memory,
			parameters, 2, parameterRecord);
		if (!wrote)
		{
			platform.Free(newMenu, vectorBytes);
			platform.Free(parameters, MuiMakeObjectParameterRecord.Size);
			return 485;
		}
		var strip = MuiMakeObjectServiceCore.MakeNativeObjectA(ref platform,
			service, root, publicObjects,
			MuiMakeObjectServiceCore.MUIO_MenustripNM, parameters);
		if (strip.IsNull)
		{
			platform.Free(newMenu, vectorBytes);
			platform.Free(parameters, MuiMakeObjectParameterRecord.Size);
			return 486;
		}
		var leaseCountBeforeDispose = NativeLeaseObjectCount(ref platform, service,
			baseClass);
		var leasesBeforeDispose = leaseCountBeforeDispose == 4;
		if (!leasesBeforeDispose)
		{
			MuiNativePublicObjectCore.DisposeObject(ref platform, service, root,
				publicObjects, strip);
			platform.Free(newMenu, vectorBytes);
			platform.Free(parameters, MuiMakeObjectParameterRecord.Size);
			return 500 + leaseCountBeforeDispose;
		}
		// The fixture aliases MUI Family classes to a plain BOOPSI base class,
		// which does not implement family-owned child disposal. Emulate the
		// native family's OM_DISPOSE child pass before exercising the public
		// binding teardown; production MUI performs this pass inside the parent.
		DisposeNativeMenuChildrenForFixture(ref platform, publicObjects, strip);
		var disposed = MuiNativePublicObjectCore.DisposeObject(
			ref platform, service, root, publicObjects, strip);
		if (!disposed)
		{
			platform.Free(newMenu, vectorBytes);
			platform.Free(parameters, MuiMakeObjectParameterRecord.Size);
			return 488;
		}
		var objectStateValid = MuiNativePublicObjectRegistryCodec.TryRead(
			ref memory, publicObjects, out var objectState);
		if (!objectStateValid)
		{
			platform.Free(newMenu, vectorBytes);
			platform.Free(parameters, MuiMakeObjectParameterRecord.Size);
			return 489;
		}
		var leasesAfterDispose = NativeLeaseObjectCount(ref platform, service,
			baseClass) == 0;
		platform.Free(newMenu, vectorBytes);
		platform.Free(parameters, MuiMakeObjectParameterRecord.Size);
		if (objectState.Head.IsNotNull) return 490;
		return leasesAfterDispose ? 0u : 491u;
	}

	private static void DisposeNativeMenuChildrenForFixture(
		ref MuiNativeClassPlatform platform, APTR publicObjects, APTR strip)
	{
		var memory = default(MuiNativeClassMemory);
		if (!MuiNativePublicObjectRegistryCodec.TryRead(ref memory, publicObjects,
			out var state)) return;
		var current = state.Head;
		for (var visited = 0u; current.IsNotNull &&
			visited < MuiHeadlessLayout.MaximumTraversal; visited++)
		{
			if (!MuiNativePublicObjectBindingCodec.TryRead(ref memory, current,
				out var value)) return;
			if (value.Object != strip) platform.DisposeObject(value.Object);
			current = value.Next;
		}
	}

	private static uint NativeLeaseObjectCount(ref MuiNativeClassPlatform platform,
		APTR service, APTR boopsi)
	{
		if (!MuiClassServiceStateCodec.TryRead(ref platform, service,
			out var state)) return uint.MaxValue;
		var current = state.Head;
		uint total = 0;
		for (var visited = 0u; current.IsNotNull &&
			visited < MuiClassServiceLayout.MaximumTraversal; visited++)
		{
			if (!MuiClassServiceLeaseCodec.TryRead(ref platform, current,
				out var value)) return uint.MaxValue;
			if (value.Boopsi == boopsi)
			{
				if (total > uint.MaxValue - value.ObjectCount)
					return uint.MaxValue;
				total += value.ObjectCount;
			}
			current = value.Next;
		}
		return current.IsNull ? total : uint.MaxValue;
	}

	private static bool CheckNativeRectangle(ref MuiNativeClassPlatform platform,
		APTR service, APTR root, APTR publicObjects, APTR parameters,
		uint type, uint parameter)
		=> CheckNativeMakeObject(ref platform, service, root, publicObjects,
			parameters, type, 1, parameter, 0);

	private static bool CheckNativeMakeObject(ref MuiNativeClassPlatform platform,
		APTR service, APTR root, APTR publicObjects, APTR parameters,
		uint type, uint count, uint first, uint second, uint third = 0,
		uint fourth = 0)
	{
		var memory = default(MuiNativeClassMemory);
		var value = default(MuiMakeObjectParameterRecord);
		value.First = first;
		value.Second = second;
		value.Third = third;
		value.Fourth = fourth;
		if (!MuiMakeObjectParameterCodec.TryWrite(ref memory, parameters, count,
			value)) return false;
		var obj = MuiMakeObjectServiceCore.MakeNativeObjectA(ref platform,
			service, root, publicObjects, type, parameters);
		return obj.IsNotNull && MuiNativePublicObjectCore.DisposeObject(ref platform,
			service, root, publicObjects, obj);
	}

	private static APTR CreateContext(ref MuiNativeClassPlatform platform, APTR intuition,
		APTR root, APTR doSuper, APTR expectedBase, uint expectedDepth)
	{
		var memory = default(MuiNativeClassMemory);
		var address = platform.Allocate(ClassContext.Size, (uint)(Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear));
		if (address.IsNull) return APTR.Null;
		var value = new ClassContext
		{
			Platform = new MuiNativeClassContext { IntuitionBase = intuition, OwnerRoot = root },
			SuperEntry = doSuper,
			ExpectedBase = expectedBase, ExpectedDepth = expectedDepth,
		};
		if (ClassContextCodec.Write(ref memory, address, value)) return address;
		platform.Free(address, ClassContext.Size);
		return APTR.Null;
	}

	private static bool SetContext(ref MuiNativeClassPlatform platform, APTR cls, APTR context)
	{
		if (cls.IsNull || context.IsNull) return false;
		var memory = default(MuiNativeClassMemory);
		var value = BOOPSIGuestCodec.ReadClass(ref memory, cls);
		value.cl_UserData = context.Raw;
		BOOPSIGuestCodec.WriteClass(ref memory, cls, value);
		return true;
	}

	private static bool CheckContext(ref MuiNativeClassPlatform platform, APTR context,
		uint news, uint disposes, uint probes)
	{
		var memory = default(MuiNativeClassMemory);
		return ClassContextCodec.TryRead(ref memory, context, out var value) && value.Failure == 0 &&
		value.NewCount == news && value.DisposeCount == disposes && value.ProbeCount == probes &&
		(value.ExpectedBase.IsNull || value.LastBase == value.ExpectedBase);
	}

	private static bool BindingsEqual(ref MuiNativeClassPlatform platform, APTR root, uint bindings)
	{
		var memory = default(MuiNativeClassMemory);
		return MuiMasterPrivateRootCodec.TryRead(ref memory, root, out var value) &&
		value.ExternalClassHead == bindings && value.ActiveCallbackDepth == 0 && value.ActiveDispatchDepth == 0;
	}

	private static bool CheckDispatcherRejection(ref MuiNativeClassPlatform platform,
		APTR root, APTR cls, APTR probe)
	{
		var memory = default(MuiNativeClassMemory);
		var entry = BOOPSIGuestCodec.ReadClass(ref memory, cls).cl_Dispatcher.Entry;
		if (CallDispatcher(entry, APTR.Null, APTR.Null, probe, platform.IntuitionBase) != 0 ||
			CallDispatcher(entry, cls, APTR.Null, APTR.Null, platform.IntuitionBase) != 0 ||
			!MuiMasterPrivateRootCodec.TryRead(ref memory, root, out var original)) return false;
		var invalid = original;
		invalid.RegistryGeneration = 0;
		if (!MuiMasterPrivateRootCodec.Write(ref memory, root, invalid)) return false;
		var rejected = CallDispatcher(entry, cls, APTR.Null, probe, platform.IntuitionBase);
		if (!MuiMasterPrivateRootCodec.TryRead(ref memory, root, out var after)) return false;
		var untouched = after.RegistryGeneration == 0 && after.ExternalClassHead == original.ExternalClassHead &&
			after.ActiveCallbackDepth == original.ActiveCallbackDepth && after.ActiveDispatchDepth == original.ActiveDispatchDepth;
		after.RegistryGeneration = original.RegistryGeneration;
		if (!MuiMasterPrivateRootCodec.Write(ref memory, root, after)) return false;
		return rejected == 0 && untouched;
	}

	[AmigaIndirectCall(M68kRegister.A3)]
	[return: M68kRegister(M68kRegister.D0)]
	private static extern uint CallSuper([M68kRegister(M68kRegister.A3)] APTR entry,
		[M68kRegister(M68kRegister.A6)] APTR intuition, [M68kRegister(M68kRegister.A0)] APTR cls,
		[M68kRegister(M68kRegister.A2)] APTR obj, [M68kRegister(M68kRegister.A1)] APTR message);

	[AmigaIndirectCall(M68kRegister.A3)]
	[return: M68kRegister(M68kRegister.D0)]
	private static extern uint CallDispatcher([M68kRegister(M68kRegister.A3)] APTR entry,
		[M68kRegister(M68kRegister.A0)] APTR cls, [M68kRegister(M68kRegister.A2)] APTR obj,
		[M68kRegister(M68kRegister.A1)] APTR message, [M68kRegister(M68kRegister.A6)] APTR callerBase);

	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	private struct ClassProbeMessage
	{
		internal const uint Size = 4;
		internal uint MethodId;
	}

	private static class ClassProbeMessageCodec
	{
		// Method ID is the shared scalar field of every BOOPSI packet. Admit its
		// named message extent here, then use the SDK field accessor; transporting
		// the complete one-field packet by value currently confuses native lowering.
		internal static bool TryReadMethodId<T>(ref T memory, APTR address, out uint value)
			where T : struct, IAmigaGuestMemory
		{
			value = default;
			if (!MuiGuestStructCursor.TryCreate(ref memory, address, ClassProbeMessage.Size, out _)) return false;
			value = BOOPSIGuestCodec.ReadMethodId(ref memory, address);
			return true;
		}
		// Explicit address semantics avoid scalar/by-value ambiguity for this
		// one-field packet in the native calling convention.
		internal static bool Write<T>(ref T memory, APTR address, ref ClassProbeMessage value)
			where T : struct, IAmigaGuestMemory =>
			MuiGuestStructCursor.TryCreate(ref memory, address, ClassProbeMessage.Size, out var cursor) &&
			MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor, value.MethodId);
	}

	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	private struct ClassContext
	{
		internal const uint Size = 40;
		// The provider's named context is the record prefix, so callbacks can
		// borrow this exact guest address as their scalar provider handle.
		internal MuiNativeClassContext Platform;
		internal APTR SuperEntry;
		internal APTR ExpectedBase;
		internal uint NewCount;
		internal uint DisposeCount;
		internal uint ProbeCount;
		internal uint Failure;
		internal uint ExpectedDepth;
		internal APTR LastBase;
	}

	private static class ClassContextCodec
	{
		internal static bool TryRead<T>(ref T memory, APTR address, out ClassContext value)
			where T : struct, IAmigaGuestMemory
		{
			value = default;
			if (!MuiGuestStructCursor.TryCreate(ref memory, address, ClassContext.Size, out var cursor) ||
				!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor, out var intuition) ||
				!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor, out var root) ||
				!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor, out var super) ||
				!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor, out var expected) ||
				!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor, out value.NewCount) ||
				!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor, out value.DisposeCount) ||
				!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor, out value.ProbeCount) ||
				!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor, out value.Failure) ||
				!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor, out value.ExpectedDepth) ||
				!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor, out var last)) return false;
			value.Platform.IntuitionBase = APTR.FromPointer(intuition);
			value.Platform.OwnerRoot = APTR.FromPointer(root);
			value.SuperEntry = APTR.FromPointer(super);
			value.ExpectedBase = APTR.FromPointer(expected);
			value.LastBase = APTR.FromPointer(last);
			return MuiGuestStructCursor.IsComplete(cursor);
		}
		internal static bool Write<T>(ref T memory, APTR address, ClassContext value)
			where T : struct, IAmigaGuestMemory =>
			MuiGuestStructCursor.TryCreate(ref memory, address, ClassContext.Size, out var cursor) &&
			MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor, value.Platform.IntuitionBase.Raw) &&
			MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor, value.Platform.OwnerRoot.Raw) &&
			MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor, value.SuperEntry.Raw) &&
			MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor, value.ExpectedBase.Raw) &&
			MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor, value.NewCount) &&
			MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor, value.DisposeCount) &&
			MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor, value.ProbeCount) &&
			MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor, value.Failure) &&
			MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor, value.ExpectedDepth) &&
			MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor, value.LastBase.Raw) &&
			MuiGuestStructCursor.IsComplete(cursor);
	}
}
