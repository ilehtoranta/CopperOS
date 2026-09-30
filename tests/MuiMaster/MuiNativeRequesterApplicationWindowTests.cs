/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiNativeRequesterApplicationWindowTests
{
	private static readonly APTR Registry = APTR.FromPointer(0x1000);
	private static readonly APTR ApplicationBinding = APTR.FromPointer(0x1020);
	private static readonly APTR WindowBinding = APTR.FromPointer(0x1050);
	private static readonly APTR ApplicationSidecar = APTR.FromPointer(0x1080);
	private static readonly APTR WindowSidecar = APTR.FromPointer(0x1100);
	private static readonly APTR OpenAttribute = APTR.FromPointer(0x1140);
	private static readonly APTR ApplicationClass = APTR.FromPointer(0x1180);
	private static readonly APTR WindowClass = APTR.FromPointer(0x11C0);
	private static readonly APTR ApplicationName = APTR.FromPointer(0x1200);
	private static readonly APTR WindowName = APTR.FromPointer(0x1220);
	private static readonly APTR Application = APTR.FromPointer(0x1240);
	private static readonly APTR Window = APTR.FromPointer(0x1260);
	private static readonly APTR OwnerRoot = APTR.FromPointer(0x1280);

	[Fact]
	public void PrivateRequesterWindowAdmissionAcceptsDetachedApplicationWindow()
	{
		var memory = CreateMemory();

		Assert.True(MuiNativeRequesterApplicationWindowCore.CanAttach(ref memory,
			Registry, OwnerRoot, Application, Window));
	}

	[Fact]
	public void PrivateRequesterWindowDetachmentRequiresClosedAppChild()
	{
		var memory = CreateMemory();
		SetParent(ref memory, 0);
		SetWindowOpen(ref memory, 0);

		Assert.True(MuiNativeRequesterApplicationWindowCore.CanDetach(ref memory,
			Registry, OwnerRoot, Application, Window));

		SetWindowOpen(ref memory, 1);
		Assert.False(MuiNativeRequesterApplicationWindowCore.CanDetach(ref memory,
			Registry, OwnerRoot, Application, Window));
	}

	[Fact]
	public void PrivateRequesterWindowAdmissionRejectsWrongClassesAndExistingParents()
	{
		var memory = CreateMemory();
		memory.WriteCString(WindowName, "Text.mui");
		BOOPSIGuestCodec.WriteClass(ref memory, WindowClass,
			new IClass { cl_ID = WindowName });
		Assert.False(MuiNativeRequesterApplicationWindowCore.CanAttach(ref memory,
			Registry, OwnerRoot, Application, Window));

		memory.WriteCString(WindowName, "Window.mui");
		BOOPSIGuestCodec.WriteClass(ref memory, WindowClass,
			new IClass { cl_ID = WindowName });
		SetParent(ref memory,
			MuiNativeMuiObjectRecord.ObjectParentFamilyChild);
		Assert.False(MuiNativeRequesterApplicationWindowCore.CanAttach(ref memory,
			Registry, OwnerRoot, Application, Window));
	}

	private static MuiHeadlessTestPlatform CreateMemory()
	{
		var memory = new MuiHeadlessTestPlatform(0x1000, 0x3000, 0, Registry);
		memory.WriteCString(ApplicationName, "Application.mui");
		memory.WriteCString(WindowName, "Window.mui");
		BOOPSIGuestCodec.WriteClass(ref memory, ApplicationClass,
			new IClass { cl_ID = ApplicationName });
		BOOPSIGuestCodec.WriteClass(ref memory, WindowClass,
			new IClass { cl_ID = WindowName });
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
				APTR.Null, APTR.Null)));
		Assert.True(MuiNativeMuiObjectCodec.Write(ref memory, ApplicationSidecar,
			Sidecar(Application, ApplicationClass, APTR.Null)));
		Assert.True(MuiNativeMuiObjectCodec.Write(ref memory, WindowSidecar,
			Sidecar(Window, WindowClass, APTR.Null)));
		return memory;
	}

	private static void SetParent(ref MuiHeadlessTestPlatform memory,
		uint parentKind)
	{
		Assert.True(MuiNativePublicObjectBindingCodec.TryRead(ref memory,
			WindowBinding, out var binding));
		binding.Parent = Application;
		Assert.True(MuiNativePublicObjectBindingCodec.Write(ref memory,
			WindowBinding, binding));
		Assert.True(MuiNativeMuiObjectCodec.TryRead(ref memory, WindowSidecar,
			out var sidecar));
		sidecar.Parent = Application;
		sidecar.Flags |= parentKind;
		Assert.True(MuiNativeMuiObjectCodec.Write(ref memory, WindowSidecar,
			sidecar));
	}

	private static void SetWindowOpen(ref MuiHeadlessTestPlatform memory,
		uint isOpen)
	{
		Assert.True(MuiNativeObjectAttributeCodec.Write(ref memory, OpenAttribute,
			new MuiNativeObjectAttributeRecord
			{
				Signature = MuiNativeObjectAttributeRecord.Magic,
				Revision = MuiNativeObjectAttributeRecord.Version,
				Attribute = MuiWindowPublicCore.Open,
				Value = isOpen,
			}));
		Assert.True(MuiNativeMuiObjectCodec.TryRead(ref memory, WindowSidecar,
			out var sidecar));
		sidecar.Attributes = OpenAttribute;
		Assert.True(MuiNativeMuiObjectCodec.Write(ref memory, WindowSidecar,
			sidecar));
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
}
