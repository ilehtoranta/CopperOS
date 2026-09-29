/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Reflection;
using Amiga;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiClassServiceMinimalPlatformTests
{
	private static readonly APTR Service = APTR.FromPointer(0x1000);
	private static readonly APTR Headless = APTR.FromPointer(0x1080);
	private static readonly APTR BuiltinName = APTR.FromPointer(0x1100);
	private static readonly APTR ExternalName = APTR.FromPointer(0x1140);
	private static readonly APTR LibraryName = APTR.FromPointer(0x1180);
	private static readonly APTR Dispatcher = APTR.FromPointer(0xD000);

	[Fact]
	public void ClassProviderHasExactlyTwentyCapabilitiesAndNoObjectPlatform()
	{
		var contract = typeof(IMuiClassServicePlatform);
		var methods = contract.GetInterfaces().Append(contract)
			.SelectMany(type => type.GetMethods()).Distinct().ToArray();
		Assert.Equal(20, methods.Length);
		Assert.Equal(20, typeof(MinimalClassPlatform).GetMethods(
			BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly).Length);
		Assert.False(typeof(IMuiHeadlessPlatform).IsAssignableFrom(typeof(MinimalClassPlatform)));
		Assert.False(typeof(IMuiServicePlatform).IsAssignableFrom(typeof(MinimalClassPlatform)));
		Assert.False(typeof(IMuiBoopsiObjectLifetimeCapability).IsAssignableFrom(contract));
		Assert.False(typeof(IMuiExecCapability).IsAssignableFrom(contract));
		Assert.True(contract.IsAssignableFrom(typeof(IMuiServicePlatform)));
		Assert.True(typeof(IMuiClassRegistryPlatform).IsAssignableFrom(typeof(IMuiHeadlessPlatform)));
	}

	[Fact]
	public void BuiltinRegistryAndLeaseOperationsCompileWithoutObjectCapabilities()
	{
		var platform = NewPlatform();
		var classRecord = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			Headless, BuiltinName, APTR.Null, 8, Dispatcher, 1, 2);
		Assert.True(classRecord.IsNotNull);
		Assert.Equal(classRecord, MuiHeadlessObjectCore.FindClassByName(ref platform,
			Headless, BuiltinName));
		var cl = MuiHeadlessObjectCore.ClassPointer(ref platform, classRecord);
		Assert.Equal(cl, MuiClassServiceCore.GetClass(ref platform, Service, BuiltinName));
		Assert.Equal(cl, MuiClassServiceCore.GetClass(ref platform, Service, BuiltinName));
		Assert.Equal(2u, MuiClassServiceCore.ReferenceCount(ref platform, Service, cl));
		Assert.True(MuiClassServiceCore.FreeClass(ref platform, Service, cl));
		Assert.True(MuiClassServiceCore.TrackObjectLease(ref platform, Service, cl));
		Assert.Equal(1u, MuiClassServiceCore.ObjectLeaseCount(ref platform, Service, cl));
		Assert.False(MuiClassServiceCore.FreeClass(ref platform, Service, cl));
		Assert.True(MuiClassServiceCore.ReleaseObjectLease(ref platform, Service, cl));
		Assert.Equal(0u, MuiClassServiceCore.ReferenceCount(ref platform, Service, cl));
		Assert.Equal(0u, MuiClassServiceCore.ObjectLeaseCount(ref platform, Service, cl));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, Headless, classRecord));
		Assert.True(MuiHeadlessObjectCore.FindClassByName(ref platform, Headless, BuiltinName).IsNull);
		Assert.Equal(0u, platform.Fixture.OpenLibraryCount);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void CustomClassCreationAndDeletionNeedNoObjectDispatch(bool publicClass)
	{
		var platform = NewPlatform();
		var classRecord = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			Headless, BuiltinName, APTR.Null, 8, Dispatcher);
		var library = publicClass ? APTR.FromPointer(0x2000) : APTR.Null;
		var custom = MuiClassServiceCore.CreateCustomClass(ref platform, Service,
			library, BuiltinName, APTR.Null, 12, Dispatcher);
		Assert.True(custom.IsNotNull);
		Assert.True(MuiCustomClassCodec.TryRead(ref platform, custom, out var record));
		Assert.Equal(MuiHeadlessObjectCore.ClassPointer(ref platform, classRecord), record.Super);
		Assert.Equal(library, platform.Fixture.LastCustomLibraryBase);
		Assert.True(MuiClassServiceCore.DeleteCustomClass(ref platform, Service, custom));
		Assert.Equal(1u, platform.Fixture.MakeCustomClassCount);
		Assert.Equal(1u, platform.Fixture.FreeCustomClassCount);
		Assert.Equal(0u, MuiClassServiceCore.ReferenceCount(ref platform, Service, record.Super));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, Headless, classRecord));
	}

	[Fact]
	public void ExternalClassLeaseUsesOnlyClassAndLoaderCapabilities()
	{
		var platform = NewPlatform();
		platform.Fixture.LoadableLibraryName = LibraryName;
		platform.Fixture.LoadableLibraryBase = APTR.FromPointer(0x2000);
		platform.Fixture.LoadablePublicClassId = ExternalName;
		platform.Fixture.LoadablePublicClass = APTR.FromPointer(0x2100);
		var cl = MuiClassServiceCore.GetClass(ref platform, Service, ExternalName);
		Assert.Equal(platform.Fixture.LoadablePublicClass, cl);
		Assert.Equal(1u, platform.Fixture.OpenLibraryCount);
		Assert.Equal(1u, platform.Fixture.ResolvePublicClassCount);
		Assert.True(MuiHeadlessObjectCore.FindClassByName(ref platform,
			Headless, ExternalName).IsNotNull);
		Assert.True(MuiClassServiceCore.FreeClass(ref platform, Service, cl));
		Assert.True(MuiHeadlessObjectCore.FindClassByName(ref platform,
			Headless, ExternalName).IsNull);
		Assert.Equal(1u, platform.Fixture.CloseLibraryCount);
		Assert.Equal(platform.Fixture.AllocationCount, platform.Fixture.FreeCount);
	}

	private static MinimalClassPlatform NewPlatform()
	{
		var platform = new MinimalClassPlatform
		{
			Fixture = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000, Headless),
		};
		platform.Fixture.WriteCString(BuiltinName, "Notify.mui");
		platform.Fixture.WriteCString(ExternalName, "Example.mcc");
		platform.Fixture.WriteCString(LibraryName, "mui/Example.mcc");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, Headless));
		Assert.True(MuiClassServiceCore.Initialize(ref platform, Service, Headless));
		Assert.True(MuiHeadlessStateCodec.TryRead(ref platform, Headless, out var headless));
		Assert.Equal(MuiHeadlessLayout.Magic, headless.Magic);
		Assert.True(MuiClassServiceStateCodec.TryRead(ref platform, Service, out var service));
		Assert.Equal(Headless, service.Headless);
		return platform;
	}

	// This is only a host contract regression over the established deterministic
	// test model, not a native provider or proof of Intuition behavior. The wrapper
	// intentionally exposes exactly 20 methods. Any aggregate creep at a class-only
	// generic call above becomes a build error, even though its backing fixture
	// also supports broader tests elsewhere.
	private struct MinimalClassPlatform : IMuiClassServicePlatform
	{
		internal MuiHeadlessTestPlatform Fixture;
		public bool IsMapped(APTR address, uint bytes) => Fixture.IsMapped(address, bytes);
		public byte ReadUInt8(APTR address, int offset) => Fixture.ReadUInt8(address, offset);
		public ushort ReadUInt16(APTR address, int offset) => Fixture.ReadUInt16(address, offset);
		public uint ReadUInt32(APTR address, int offset) => Fixture.ReadUInt32(address, offset);
		public void WriteUInt8(APTR address, int offset, byte value) => Fixture.WriteUInt8(address, offset, value);
		public void WriteUInt16(APTR address, int offset, ushort value) => Fixture.WriteUInt16(address, offset, value);
		public void WriteUInt32(APTR address, int offset, uint value) => Fixture.WriteUInt32(address, offset, value);
		public void Clear(APTR address, uint bytes) => Fixture.Clear(address, bytes);
		public void Copy(APTR source, APTR destination, uint bytes) => Fixture.Copy(source, destination, bytes);
		public APTR Allocate(uint bytes, uint flags) => Fixture.Allocate(bytes, flags);
		public void Free(APTR address, uint bytes) => Fixture.Free(address, bytes);
		public APTR MakeClass(APTR classId, APTR superClass, ushort instanceSize, APTR dispatcher) =>
			Fixture.MakeClass(classId, superClass, instanceSize, dispatcher);
		public bool AddClass(APTR cl) => Fixture.AddClass(cl);
		public bool RemoveClass(APTR cl) => Fixture.RemoveClass(cl);
		public bool FreeClass(APTR cl) => Fixture.FreeClass(cl);
		public APTR OpenLibrary(APTR name, ushort minimumVersion) => Fixture.OpenLibrary(name, minimumVersion);
		public void CloseLibrary(APTR library) => Fixture.CloseLibrary(library);
		public APTR MakeCustomClass(APTR superClass, ushort instanceSize, APTR dispatcher, APTR libraryBase) =>
			Fixture.MakeCustomClass(superClass, instanceSize, dispatcher, libraryBase);
		public bool FreeCustomClass(APTR cl) => Fixture.FreeCustomClass(cl);
		public APTR ResolveExternalClass(APTR library, APTR classId) => Fixture.ResolveExternalClass(library, classId);
	}
}
