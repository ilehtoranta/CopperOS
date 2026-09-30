using System.Runtime.CompilerServices;
using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiNativeRequesterApplicationAdmissionTests
{
	[Fact]
	public void NativeRequesterPlatformUsesSinglePointerContextHandle()
	{
		Assert.Equal(4, Unsafe.SizeOf<MuiNativeRequesterPlatform>());
	}

	private static readonly APTR Registry = APTR.FromPointer(0x1000);
	private static readonly APTR ApplicationBinding = APTR.FromPointer(0x1020);
	private static readonly APTR WindowBinding = APTR.FromPointer(0x1050);
	private static readonly APTR ApplicationSidecar = APTR.FromPointer(0x1080);
	private static readonly APTR WindowSidecar = APTR.FromPointer(0x1100);
	private static readonly APTR ApplicationClass = APTR.FromPointer(0x1180);
	private static readonly APTR WindowClass = APTR.FromPointer(0x11C0);
	private static readonly APTR ObjectClass = APTR.FromPointer(0x1380);
	private static readonly APTR AreaClass = APTR.FromPointer(0x13C0);
	private static readonly APTR ApplicationName = APTR.FromPointer(0x1200);
	private static readonly APTR WindowName = APTR.FromPointer(0x1220);
	private static readonly APTR ObjectName = APTR.FromPointer(0x1400);
	private static readonly APTR AreaName = APTR.FromPointer(0x1420);
	private static readonly APTR Application = APTR.FromPointer(0x1240);
	private static readonly APTR Window = APTR.FromPointer(0x1260);
	private static readonly APTR Object = APTR.FromPointer(0x1440);
	private static readonly APTR OwnerRoot = APTR.FromPointer(0x1280);
	private static readonly APTR OtherApplication = APTR.FromPointer(0x12A0);
	private static readonly APTR ObjectBinding = APTR.FromPointer(0x12C0);
	private static readonly APTR ObjectSidecar = APTR.FromPointer(0x12F0);

	[Fact]
	public void ApplicationModalAdmissionRequiresLiveApplicationAndOwnedWindow()
	{
		var memory = CreateMemory();
		var request = Request(Application, Window);

		Assert.True(MuiNativeRequesterApplicationAdmissionCore.TryValidate(
			ref memory, request, Registry, OwnerRoot, out var admission));
		Assert.Equal(Application, admission.Application);
		Assert.Equal(Window, admission.Window);
		Assert.Equal(ApplicationSidecar, admission.ApplicationSidecar);
		Assert.Equal(WindowSidecar, admission.WindowSidecar);
		Assert.Equal(1u, admission.HasReferenceWindow);
	}

	[Fact]
	public void ApplicationModalAdmissionAllowsNoReferenceWindow()
	{
		var memory = CreateMemory();
		var request = Request(Application, APTR.Null);

		Assert.True(MuiNativeRequesterApplicationAdmissionCore.TryValidate(
			ref memory, request, Registry, OwnerRoot, out var admission));
		Assert.Equal(Application, admission.Application);
		Assert.True(admission.Window.IsNull);
		Assert.True(admission.WindowSidecar.IsNull);
		Assert.Equal(0u, admission.HasReferenceWindow);
	}

	[Fact]
	public void ApplicationModalAdmissionRejectsForeignWindowAndInvalidFlags()
	{
		var memory = CreateMemory();
		var request = Request(Application, Window);
		Assert.True(MuiNativePublicObjectBindingCodec.TryRead(ref memory,
			WindowBinding, out var windowBinding));
		windowBinding.Parent = OtherApplication;
		Assert.True(MuiNativePublicObjectBindingCodec.Write(ref memory,
			WindowBinding, windowBinding));
		Assert.False(MuiNativeRequesterApplicationAdmissionCore.TryValidate(
			ref memory, request, Registry, OwnerRoot, out _));

		request = Request(Application, APTR.Null);
		request.Flags = 1;
		Assert.False(MuiNativeRequesterApplicationAdmissionCore.TryValidate(
			ref memory, request, Registry, OwnerRoot, out _));
	}

	[Fact]
	public void ApplicationModalAdmissionRejectsNonMuiApplicationAndWindowObjects()
	{
		var memory = CreateMemory();
		memory.WriteCString(ApplicationName, "Group.mui");
		BOOPSIGuestCodec.WriteClass(ref memory, ApplicationClass,
			new IClass { cl_ID = ApplicationName });
		Assert.False(MuiNativeRequesterApplicationAdmissionCore.TryValidate(
			ref memory, Request(Application, APTR.Null), Registry, OwnerRoot,
			out _));

		memory.WriteCString(ApplicationName, "Application.mui");
		BOOPSIGuestCodec.WriteClass(ref memory, ApplicationClass,
			new IClass { cl_ID = ApplicationName });
		memory.WriteCString(WindowName, "Text.mui");
		BOOPSIGuestCodec.WriteClass(ref memory, WindowClass,
			new IClass { cl_ID = WindowName });
		Assert.False(MuiNativeRequesterApplicationAdmissionCore.TryValidate(
			ref memory, Request(Application, Window), Registry, OwnerRoot,
			out _));
	}

	[Fact]
	public void ObjectRequesterAdmissionRequiresLiveUnparentedAreaObject()
	{
		var memory = CreateMemory();

		Assert.True(MuiNativeRequesterApplicationAdmissionCore
			.TryValidateAreaObject(ref memory, Registry, OwnerRoot, Object,
				out var admission));
		Assert.Equal(Object, admission.Object);
		Assert.Equal(ObjectSidecar, admission.Sidecar);

		Assert.False(MuiNativeRequesterApplicationAdmissionCore
			.TryValidateAreaObject(ref memory, Registry, OwnerRoot,
				APTR.Null, out _));
	}

	[Fact]
	public void ObjectRequesterAdmissionRejectsNonAreaAndParentedObjects()
	{
		var memory = CreateMemory();
		memory.WriteCString(ObjectName, "Application.mui");
		BOOPSIGuestCodec.WriteClass(ref memory, ObjectClass,
			new IClass { cl_ID = ObjectName });
		Assert.False(MuiNativeRequesterApplicationAdmissionCore
			.TryValidateAreaObject(ref memory, Registry, OwnerRoot, Object,
				out _));

		memory.WriteCString(ObjectName, "Text.mui");
		BOOPSIGuestCodec.WriteClass(ref memory, ObjectClass,
			new IClass { cl_ID = ObjectName, cl_Super = AreaClass });
		Assert.True(MuiNativePublicObjectBindingCodec.TryRead(ref memory,
			ObjectBinding, out var binding));
		binding.Parent = Window;
		Assert.True(MuiNativePublicObjectBindingCodec.Write(ref memory,
			ObjectBinding, binding));
		Assert.True(MuiNativeMuiObjectCodec.TryRead(ref memory, ObjectSidecar,
			out var sidecar));
		sidecar.Parent = Window;
		Assert.True(MuiNativeMuiObjectCodec.Write(ref memory, ObjectSidecar,
			sidecar));
		Assert.False(MuiNativeRequesterApplicationAdmissionCore
			.TryValidateAreaObject(ref memory, Registry, OwnerRoot, Object,
				out _));
	}

	private static MuiHeadlessTestPlatform CreateMemory()
	{
		var memory = new MuiHeadlessTestPlatform(0x1000, 0x3000, 0,
			Registry);
		memory.WriteCString(ApplicationName, "Application.mui");
		memory.WriteCString(WindowName, "Window.mui");
		memory.WriteCString(ObjectName, "Text.mui");
		memory.WriteCString(AreaName, "Area.mui");
		BOOPSIGuestCodec.WriteClass(ref memory, ApplicationClass,
			new IClass { cl_ID = ApplicationName });
		BOOPSIGuestCodec.WriteClass(ref memory, WindowClass,
			new IClass { cl_ID = WindowName });
		BOOPSIGuestCodec.WriteClass(ref memory, ObjectClass,
			new IClass { cl_ID = ObjectName, cl_Super = AreaClass });
		BOOPSIGuestCodec.WriteClass(ref memory, AreaClass,
			new IClass { cl_ID = AreaName });
		Assert.True(MuiNativePublicObjectRegistryCodec.Write(ref memory, Registry,
			new MuiNativePublicObjectRegistryRecord
			{
				Signature = MuiNativePublicObjectRegistryRecord.Magic,
				Revision = MuiNativePublicObjectRegistryRecord.Version,
				Head = ApplicationBinding,
			}));
		Assert.True(MuiNativePublicObjectBindingCodec.Write(ref memory,
			ApplicationBinding, Binding(Application, ApplicationClass,
				ApplicationSidecar, WindowBinding, APTR.Null)));
		Assert.True(MuiNativePublicObjectBindingCodec.Write(ref memory,
			WindowBinding, Binding(Window, WindowClass, WindowSidecar,
			ObjectBinding, Application)));
		Assert.True(MuiNativePublicObjectBindingCodec.Write(ref memory,
			ObjectBinding, Binding(Object, ObjectClass, ObjectSidecar,
				APTR.Null, APTR.Null)));
		Assert.True(MuiNativeMuiObjectCodec.Write(ref memory,
			ApplicationSidecar, Sidecar(Application, ApplicationClass,
				APTR.Null)));
		Assert.True(MuiNativeMuiObjectCodec.Write(ref memory, WindowSidecar,
			Sidecar(Window, WindowClass, Application)));
		Assert.True(MuiNativeMuiObjectCodec.Write(ref memory, ObjectSidecar,
			Sidecar(Object, ObjectClass, APTR.Null)));
		return memory;
	}

	private static MuiNativePublicObjectBinding Binding(APTR obj, APTR cls,
		APTR sidecar, APTR next, APTR parent) => new()
	{
		Signature = MuiNativePublicObjectBinding.Magic,
		Next = next,
		Object = obj,
		Class = cls,
		OwnerRoot = OwnerRoot,
		Parent = parent,
		Sidecar = sidecar,
		DisposeState = MuiNativePublicObjectBinding.StateLive,
	};

	private static MuiNativeMuiObjectRecord Sidecar(APTR obj, APTR cls,
		APTR parent) => new()
	{
		Signature = MuiNativeMuiObjectRecord.Magic,
		Revision = MuiNativeMuiObjectRecord.Version,
		Object = obj,
		Class = cls,
		OwnerRoot = OwnerRoot,
		Parent = parent,
		Flags = MuiNativeMuiObjectRecord.ObjectInitialized,
		LifecycleState = MuiNativeMuiObjectRecord.StateLive,
	};

	private static MuiRequesterCallRecord Request(APTR application,
		APTR window) => new()
	{
		Application = application,
		Window = window,
	};
}
