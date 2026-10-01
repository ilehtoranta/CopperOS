/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiNativeWindowRootObjectOwnershipTests
{
	private static readonly APTR Registry = APTR.FromPointer(0x1000);
	private static readonly APTR WindowBinding = APTR.FromPointer(0x1020);
	private static readonly APTR FirstRootBinding = APTR.FromPointer(0x1050);
	private static readonly APTR LastRootBinding = APTR.FromPointer(0x1080);
	private static readonly APTR WindowSidecar = APTR.FromPointer(0x10B0);
	private static readonly APTR FirstRootSidecar = APTR.FromPointer(0x1120);
	private static readonly APTR LastRootSidecar = APTR.FromPointer(0x1190);
	private static readonly APTR WindowClass = APTR.FromPointer(0x1200);
	private static readonly APTR FirstRootClass = APTR.FromPointer(0x1260);
	private static readonly APTR LastRootClass = APTR.FromPointer(0x12C0);
	private static readonly APTR WindowName = APTR.FromPointer(0x1320);
	private static readonly APTR FirstRootName = APTR.FromPointer(0x1340);
	private static readonly APTR LastRootName = APTR.FromPointer(0x1360);
	private static readonly APTR Window = APTR.FromPointer(0x1380);
	private static readonly APTR FirstRoot = APTR.FromPointer(0x13A0);
	private static readonly APTR LastRoot = APTR.FromPointer(0x13C0);
	private static readonly APTR OwnerRoot = APTR.FromPointer(0x13E0);
	private static readonly APTR Tags = APTR.FromPointer(0x1400);

	[Fact]
	public void WindowTracksOnlyTheFinalNamedRootObjectTag()
	{
		var memory = CreateMemory();
		Assert.True(WriteTag(ref memory, 0, MuiWindowPublicCore.RootObject,
			FirstRoot.Raw));
		Assert.True(WriteTag(ref memory, 1, MuiWindowPublicCore.RootObject,
			LastRoot.Raw));
		Assert.True(WriteTag(ref memory, 2, MuiAslTagListCore.TagDone, 0));

		Assert.True(MuiNativePublicObjectCore.TryLinkWindowRootObject(ref memory,
			Registry, OwnerRoot, Window, Tags));

		Assert.True(MuiNativePublicObjectBindingCodec.TryRead(ref memory,
			FirstRootBinding, out var firstBinding));
		Assert.True(MuiNativeMuiObjectCodec.TryRead(ref memory,
			FirstRootSidecar, out var firstSidecar));
		Assert.Equal(APTR.Null, firstBinding.Parent);
		Assert.Equal(APTR.Null, firstSidecar.Parent);

		Assert.True(MuiNativePublicObjectBindingCodec.TryRead(ref memory,
			LastRootBinding, out var lastBinding));
		Assert.True(MuiNativeMuiObjectCodec.TryRead(ref memory,
			LastRootSidecar, out var lastSidecar));
		Assert.Equal(Window, lastBinding.Parent);
		Assert.Equal(Window, lastSidecar.Parent);
		Assert.Equal(MuiNativeMuiObjectRecord.ObjectParentWindowRootObject,
			lastSidecar.Flags & MuiNativeMuiObjectRecord.ObjectParentKindMask);
	}

	[Fact]
	public void WindowRootObjectRejectsAlreadyOwnedObjects()
	{
		var memory = CreateMemory();
		Assert.True(WriteTag(ref memory, 0, MuiWindowPublicCore.RootObject,
			LastRoot.Raw));
		Assert.True(WriteTag(ref memory, 1, MuiAslTagListCore.TagDone, 0));
		Assert.True(MuiNativePublicObjectBindingCodec.TryRead(ref memory,
			LastRootBinding, out var binding));
		binding.Parent = Window;
		Assert.True(MuiNativePublicObjectBindingCodec.Write(ref memory,
			LastRootBinding, binding));

		Assert.False(MuiNativePublicObjectCore.TryLinkWindowRootObject(ref memory,
			Registry, OwnerRoot, Window, Tags));
	}

	[Fact]
	public void NullWindowRootObjectTagCreatesNoSidecarParent()
	{
		var memory = CreateMemory();
		Assert.True(WriteTag(ref memory, 0, MuiWindowPublicCore.RootObject, 0));
		Assert.True(WriteTag(ref memory, 1, MuiAslTagListCore.TagDone, 0));

		Assert.True(MuiNativePublicObjectCore.TryLinkWindowRootObject(ref memory,
			Registry, OwnerRoot, Window, Tags));
		Assert.True(MuiNativePublicObjectBindingCodec.TryRead(ref memory,
			LastRootBinding, out var binding));
		Assert.Equal(APTR.Null, binding.Parent);
	}

	private static MuiHeadlessTestPlatform CreateMemory()
	{
		var memory = new MuiHeadlessTestPlatform(0x1000, 0x3000, 0, Registry);
		memory.WriteCString(WindowName, "Window.mui");
		memory.WriteCString(FirstRootName, "Group.mui");
		memory.WriteCString(LastRootName, "Group.mui");
		BOOPSIGuestCodec.WriteClass(ref memory, WindowClass,
			new IClass { cl_ID = WindowName });
		BOOPSIGuestCodec.WriteClass(ref memory, FirstRootClass,
			new IClass { cl_ID = FirstRootName });
		BOOPSIGuestCodec.WriteClass(ref memory, LastRootClass,
			new IClass { cl_ID = LastRootName });
		Assert.True(MuiNativePublicObjectRegistryCodec.Write(ref memory, Registry,
			new MuiNativePublicObjectRegistryRecord
			{
				Signature = MuiNativePublicObjectRegistryRecord.Magic,
				Revision = MuiNativePublicObjectRegistryRecord.Version,
				Head = WindowBinding,
			}));
		Assert.True(MuiNativePublicObjectBindingCodec.Write(ref memory,
			WindowBinding, Binding(Window, WindowClass, WindowSidecar,
				FirstRootBinding)));
		Assert.True(MuiNativePublicObjectBindingCodec.Write(ref memory,
			FirstRootBinding, Binding(FirstRoot, FirstRootClass,
				FirstRootSidecar, LastRootBinding)));
		Assert.True(MuiNativePublicObjectBindingCodec.Write(ref memory,
			LastRootBinding, Binding(LastRoot, LastRootClass, LastRootSidecar,
				APTR.Null)));
		Assert.True(MuiNativeMuiObjectCodec.Write(ref memory, WindowSidecar,
			Sidecar(Window, WindowClass)));
		Assert.True(MuiNativeMuiObjectCodec.Write(ref memory, FirstRootSidecar,
			Sidecar(FirstRoot, FirstRootClass)));
		Assert.True(MuiNativeMuiObjectCodec.Write(ref memory, LastRootSidecar,
			Sidecar(LastRoot, LastRootClass)));
		return memory;
	}

	private static bool WriteTag(ref MuiHeadlessTestPlatform memory, uint index,
		uint tag, uint data) => MuiAslTagItemVectorCodec.TryWrite(ref memory,
		Tags, index, new MuiAslTagItemRecord { Tag = tag, Data = data });

	private static MuiNativePublicObjectBinding Binding(APTR obj, APTR cls,
		APTR sidecar, APTR next) => new()
	{
		Signature = MuiNativePublicObjectBinding.Magic,
		Next = next,
		Object = obj,
		Class = cls,
		OwnerRoot = OwnerRoot,
		Sidecar = sidecar,
		DisposeState = MuiNativePublicObjectBinding.StateLive,
	};

	private static MuiNativeMuiObjectRecord Sidecar(APTR obj, APTR cls) => new()
	{
		Signature = MuiNativeMuiObjectRecord.Magic,
		Revision = MuiNativeMuiObjectRecord.Version,
		Object = obj,
		Class = cls,
		OwnerRoot = OwnerRoot,
		Flags = MuiNativeMuiObjectRecord.ObjectInitialized,
		LifecycleState = MuiNativeMuiObjectRecord.StateLive,
	};
}
