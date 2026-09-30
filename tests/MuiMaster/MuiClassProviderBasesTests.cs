/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster.Tests;

// Host record/publication tests only. The supplied handles are deterministic
// values, not opened OS libraries or native public-wrapper qualification.
public sealed class MuiClassProviderBasesTests
{
	private static readonly APTR Service = APTR.FromPointer(0x1000);
	private static readonly APTR Headless = APTR.FromPointer(0x1080);
	private static readonly APTR Name = APTR.FromPointer(0x1100);
	private static readonly APTR Dispatcher = APTR.FromPointer(0xD000);
	private static readonly APTR CallbackLibrary = APTR.FromPointer(0x2900);

	[Fact]
	public void ProviderBasesHaveTheNamedPackedSixteenByteShape()
	{
		Assert.Equal((int)MuiClassProviderBases.Size, Marshal.SizeOf<MuiClassProviderBases>());
		Assert.Equal(16u, MuiClassProviderBases.Size);
		Assert.Equal(LayoutKind.Sequential, typeof(MuiClassProviderBases).StructLayoutAttribute!.Value);
		Assert.Equal(2, typeof(MuiClassProviderBases).StructLayoutAttribute!.Pack);
		Assert.Equal(new[] { "UtilityBase", "DosBase", "GraphicsBase", "IntuitionBase" },
			typeof(MuiClassProviderBases).GetFields().Where(field => !field.IsStatic)
				.Select(field => field.Name));
		Assert.All(typeof(MuiClassProviderBases).GetFields().Where(field => !field.IsStatic),
			field => Assert.Equal(typeof(APTR), field.FieldType));
	}

	[Theory]
	[InlineData(0)]
	[InlineData(7)]
	[InlineData(11)]
	[InlineData(13)]
	[InlineData(14)]
	public void IncompleteProviderBasesRejectBeforeAcquiringAnything(int present)
	{
		var platform = NewPlatform();
		var providers = Providers();
		if ((present & 1) == 0) providers.UtilityBase = APTR.Null;
		if ((present & 2) == 0) providers.DosBase = APTR.Null;
		if ((present & 4) == 0) providers.GraphicsBase = APTR.Null;
		if ((present & 8) == 0) providers.IntuitionBase = APTR.Null;
		var allocations = platform.Fixture.AllocationCount;
		var frees = platform.Fixture.FreeCount;
		Assert.False(providers.IsComplete);

		Assert.Equal(APTR.Null, MuiClassServiceCore.CreateCustomClass(ref platform,
			Service, CallbackLibrary, Name, APTR.Null, 12, Dispatcher, providers));

		Assert.Equal(allocations, platform.Fixture.AllocationCount);
		Assert.Equal(frees, platform.Fixture.FreeCount);
		Assert.Equal(0u, platform.Fixture.MakeCustomClassCount);
		Assert.Equal(0u, platform.Fixture.OpenLibraryCount);
		Assert.Equal(0u, platform.Fixture.CloseLibraryCount);
		Assert.Equal(APTR.Null, Snapshot(ref platform).Head);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void CompleteProviderBasesAreDistinctFromDispatcherLibraryBinding(bool publicClass)
	{
		var platform = NewPlatform();
		var providers = Providers();
		var library = publicClass ? CallbackLibrary : APTR.Null;
		Assert.True(providers.IsComplete);

		var custom = MuiClassServiceCore.CreateCustomClass(ref platform, Service,
			library, Name, APTR.Null, 12, Dispatcher, providers);

		Assert.True(custom.IsNotNull);
		Assert.True(MuiCustomClassCodec.TryRead(ref platform, custom, out var record));
		AssertProviders(providers, record);
		Assert.Equal(APTR.Null, record.UserData);
		Assert.Equal(library, platform.Fixture.LastCustomLibraryBase);
		Assert.Equal(library, platform.Fixture.CustomClassLibraryBase(record.Class));
		Assert.DoesNotContain(CallbackLibrary, new[] { providers.UtilityBase,
			providers.DosBase, providers.GraphicsBase, providers.IntuitionBase });
		Assert.True(MuiClassServiceCore.DeleteCustomClass(ref platform, Service, custom));
		Assert.Equal(0u, platform.Fixture.OpenLibraryCount);
		Assert.Equal(0u, platform.Fixture.CloseLibraryCount);
	}

	[Fact]
	public void LegacyPortableOverloadKeepsEmptyProviderBases()
	{
		var platform = NewPlatform();
		var custom = MuiClassServiceCore.CreateCustomClass(ref platform, Service,
			CallbackLibrary, Name, APTR.Null, 12, Dispatcher);
		Assert.True(custom.IsNotNull);
		Assert.True(MuiCustomClassCodec.TryRead(ref platform, custom, out var record));
		AssertProviders(default, record);
		Assert.True(MuiClassServiceCore.DeleteCustomClass(ref platform, Service, custom));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void CompleteBasesPrecedePublicationAndFailedPublicationUnwinds(bool rejectPublication)
	{
		var platform = NewPlatform();
		var parent = MuiClassServiceCore.CreateCustomClass(ref platform, Service,
			APTR.Null, Name, APTR.Null, 12, Dispatcher);
		Assert.True(parent.IsNotNull);
		var original = Snapshot(ref platform);
		var live = platform.Fixture.AllocationCount - platform.Fixture.FreeCount;
		var made = platform.Fixture.MakeCustomClassCount;
		var freed = platform.Fixture.FreeCustomClassCount;
		platform.ObserveNextPublication = true;
		platform.RejectPublication = rejectPublication;
		var providers = Providers();

		var child = MuiClassServiceCore.CreateCustomClass(ref platform, Service,
			CallbackLibrary, APTR.Null, parent, 12, Dispatcher, providers);

		Assert.True(platform.SawPublication);
		Assert.Equal(rejectPublication, platform.PublicationRejected);
		Assert.True(platform.CapturedCompleteCustomRecord);
		AssertProviders(providers, platform.CustomRecordAtPublication);
		Assert.Equal(made + 1, platform.Fixture.MakeCustomClassCount);
		Assert.Equal(rejectPublication, child.IsNull);
		if (rejectPublication)
		{
			Assert.Equal(freed + 1, platform.Fixture.FreeCustomClassCount);
			Assert.Equal(live, platform.Fixture.AllocationCount - platform.Fixture.FreeCount);
			Assert.Equal(original.Head, Snapshot(ref platform).Head);
		}
		else Assert.True(MuiClassServiceCore.DeleteCustomClass(ref platform, Service, child));
		Assert.True(MuiClassServiceLeaseCodec.TryRead(ref platform, original.Head, out var parentLease));
		Assert.Equal(parent, parentLease.CustomClass);
		Assert.Equal(0u, parentLease.ChildCount);
		Assert.True(MuiClassServiceCore.DeleteCustomClass(ref platform, Service, parent));
		Assert.Equal(0u, platform.Fixture.OpenLibraryCount);
		Assert.Equal(0u, platform.Fixture.CloseLibraryCount);
	}

	[Fact]
	public void MutatingPublishedProviderBasesDoesNotTransferOwnershipOrAlterA6()
	{
		var platform = NewPlatform();
		var custom = MuiClassServiceCore.CreateCustomClass(ref platform, Service,
			CallbackLibrary, Name, APTR.Null, 12, Dispatcher, Providers());
		Assert.True(custom.IsNotNull);
		Assert.True(MuiCustomClassCodec.TryRead(ref platform, custom, out var record));
		record.UtilityBase = APTR.FromPointer(0x3100);
		record.DosBase = APTR.Null;
		record.GfxBase = APTR.FromPointer(0x3200);
		record.IntuitionBase = CallbackLibrary;
		Assert.True(MuiCustomClassCodec.Write(ref platform, custom, record));
		Assert.Equal(CallbackLibrary, platform.Fixture.CustomClassLibraryBase(record.Class));

		Assert.True(MuiClassServiceCore.DeleteCustomClass(ref platform, Service, custom));

		Assert.Equal(1u, platform.Fixture.FreeCustomClassCount);
		Assert.Equal(0u, platform.Fixture.OpenLibraryCount);
		Assert.Equal(0u, platform.Fixture.CloseLibraryCount);
		Assert.Equal(APTR.Null, Snapshot(ref platform).Head);
	}

	private static MuiClassProviderBases Providers() => new()
	{
		UtilityBase = APTR.FromPointer(0x2200),
		DosBase = APTR.FromPointer(0x2300),
		GraphicsBase = APTR.FromPointer(0x2400),
		IntuitionBase = APTR.FromPointer(0x2500),
	};

	private static void AssertProviders(MuiClassProviderBases expected, MuiCustomClassRecord actual)
	{
		Assert.Equal(expected.UtilityBase, actual.UtilityBase);
		Assert.Equal(expected.DosBase, actual.DosBase);
		Assert.Equal(expected.GraphicsBase, actual.GfxBase);
		Assert.Equal(expected.IntuitionBase, actual.IntuitionBase);
	}

	private static ProviderPlatform NewPlatform()
	{
		var platform = new ProviderPlatform
		{
			Fixture = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000, Headless),
		};
		platform.Fixture.WriteCString(Name, "Notify.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, Headless));
		Assert.True(MuiClassServiceCore.Initialize(ref platform, Service, Headless));
		Assert.True(MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			Headless, Name, APTR.Null, 8, Dispatcher).IsNotNull);
		return platform;
	}

	private static MuiClassServiceStateRecord Snapshot(ref ProviderPlatform platform)
	{
		Assert.True(MuiClassServiceStateCodec.TryRead(ref platform, Service, out var state));
		return state;
	}

	private struct ProviderPlatform : IMuiClassServicePlatform
	{
		internal MuiHeadlessTestPlatform Fixture;
		internal bool ObserveNextPublication;
		internal bool RejectPublication;
		internal bool SawPublication;
		internal bool PublicationRejected;
		internal bool CapturedCompleteCustomRecord;
		internal MuiCustomClassRecord CustomRecordAtPublication;
		private APTR _customRecord;
		private APTR _leaseRecord;
		private uint _leaseWrittenBytes;
		private bool _publicationArmed;

		public bool IsMapped(APTR address, uint bytes)
		{
			if (_publicationArmed && address == Service && bytes == MuiClassServiceStateRecord.Size)
			{
				_publicationArmed = false;
				ObserveNextPublication = false;
				SawPublication = true;
				CapturedCompleteCustomRecord = MuiCustomClassCodec.TryRead(ref Fixture,
					_customRecord, out CustomRecordAtPublication);
				if (RejectPublication)
				{
					PublicationRejected = true;
					return false;
				}
			}
			return Fixture.IsMapped(address, bytes);
		}

		public byte ReadUInt8(APTR address, int offset) => Fixture.ReadUInt8(address, offset);
		public ushort ReadUInt16(APTR address, int offset) => Fixture.ReadUInt16(address, offset);
		public uint ReadUInt32(APTR address, int offset) => Fixture.ReadUInt32(address, offset);
		public void WriteUInt8(APTR address, int offset, byte value) => Fixture.WriteUInt8(address, offset, value);
		public void WriteUInt16(APTR address, int offset, ushort value) => Fixture.WriteUInt16(address, offset, value);
		public void WriteUInt32(APTR address, int offset, uint value)
		{
			Fixture.WriteUInt32(address, offset, value);
			if (_leaseRecord.IsNotNull && address.Raw >= _leaseRecord.Raw &&
				address.Raw - _leaseRecord.Raw < MuiClassServiceLeaseRecord.Size)
			{
				_leaseWrittenBytes += sizeof(uint);
				if (_leaseWrittenBytes == MuiClassServiceLeaseRecord.Size)
				{
					_leaseRecord = APTR.Null;
					_publicationArmed = true;
				}
			}
		}

		public void Clear(APTR address, uint bytes) => Fixture.Clear(address, bytes);
		public void Copy(APTR source, APTR destination, uint bytes) => Fixture.Copy(source, destination, bytes);
		public APTR Allocate(uint bytes, uint flags)
		{
			var address = Fixture.Allocate(bytes, flags);
			if (ObserveNextPublication && address.IsNotNull)
			{
				if (bytes == MuiCustomClassRecord.Size) _customRecord = address;
				if (bytes == MuiClassServiceLeaseRecord.Size) _leaseRecord = address;
			}
			return address;
		}
		public void Free(APTR address, uint bytes) => Fixture.Free(address, bytes);
		public APTR MakeClass(APTR classId, APTR superClass, ushort size, APTR dispatcher) =>
			Fixture.MakeClass(classId, superClass, size, dispatcher);
		public bool AddClass(APTR cls) => Fixture.AddClass(cls);
		public bool RemoveClass(APTR cls) => Fixture.RemoveClass(cls);
		public bool FreeClass(APTR cls) => Fixture.FreeClass(cls);
		public APTR OpenLibrary(APTR name, ushort version) => Fixture.OpenLibrary(name, version);
		public void CloseLibrary(APTR library) => Fixture.CloseLibrary(library);
		public APTR MakeCustomClass(APTR super, ushort size, APTR dispatcher, APTR library) =>
			Fixture.MakeCustomClass(super, size, dispatcher, library);
		public bool FreeCustomClass(APTR cls) => Fixture.FreeCustomClass(cls);
		public APTR ResolveExternalClass(APTR library, APTR classId) => Fixture.ResolveExternalClass(library, classId);
	}
}
