/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiClassServiceTests
{
	private static readonly APTR ServiceState = APTR.FromPointer(0x1000);
	private static readonly APTR HeadlessState = APTR.FromPointer(0x1080);
	private static readonly APTR AreaName = APTR.FromPointer(0x1100);
	private static readonly APTR FooId = APTR.FromPointer(0x1200);
	private static readonly APTR FooIdLower = APTR.FromPointer(0x1240);
	private static readonly APTR BarId = APTR.FromPointer(0x1280);
	private static readonly APTR NopeId = APTR.FromPointer(0x12C0);
	private static readonly APTR LibNameFoo = APTR.FromPointer(0x1300);
	private static readonly APTR LibNameBar = APTR.FromPointer(0x1340);
	private static readonly APTR Dispatcher = APTR.FromPointer(0xD001);
	private static readonly APTR Dispatcher2 = APTR.FromPointer(0xD002);
	private static readonly APTR LibraryBase = APTR.FromPointer(0x2000);
	private static readonly APTR PublicClass = APTR.FromPointer(0x2100);

	private static MuiHeadlessTestPlatform NewPlatform()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			HeadlessState);
		platform.WriteCString(AreaName, "Area.mui");
		platform.WriteCString(FooId, "Foo.mcc");
		platform.WriteCString(FooIdLower, "foo.mcc");
		platform.WriteCString(BarId, "Bar.mcc");
		platform.WriteCString(NopeId, "Nope");
		platform.WriteCString(LibNameFoo, "mui/Foo.mcc");
		platform.WriteCString(LibNameBar, "mui/Bar.mcc");
		Assert.True(MuiClassServiceCore.Initialize(ref platform, ServiceState,
			HeadlessState));
		return platform;
	}

	private static APTR RegisterArea(ref MuiHeadlessTestPlatform platform) =>
		MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform, HeadlessState,
			AreaName, APTR.Null, 8, APTR.FromPointer(0xC001));

	[Fact]
	public void ClassServiceStringByteCursorUsesBoundedNamedReads()
	{
		var platform = NewPlatform();
		var cursor = default(MuiClassServiceStringByteCursor);
		cursor.Text = FooId;
		cursor.Index = 1;
		Assert.True(MuiClassServiceStringByteCursorCodec.TryReadByte(ref platform,
			cursor, out var value));
		Assert.Equal((byte)'o', value);
		Assert.True(MuiClassServiceStringByteCursorCodec.TryReadAt(ref platform,
			FooId, 6, out value));
		Assert.Equal((byte)'c', value);
		Assert.False(MuiClassServiceStringByteCursorCodec.TryReadAt(ref platform,
			FooId, -1, out _));
		Assert.False(MuiClassServiceStringByteCursorCodec.TryReadAt(ref platform,
			FooId, (int)MuiClassServiceStringByteCursor.MaximumLength, out _));
		Assert.False(MuiClassServiceStringByteCursorCodec.TryReadAt(ref platform,
			APTR.FromPointer(0x300000), 0, out _));
		Assert.False(MuiClassServiceStringByteCursorCodec.TryReadAt(ref platform,
			APTR.FromPointer(uint.MaxValue - 1u), 2, out _));
	}

	[Fact]
	public void ClassServiceStringByteCursorSupportsBoundedWritesAndPrefixPayload()
	{
		var platform = NewPlatform();
		var writeCursor = default(MuiClassServiceStringByteCursor);
		writeCursor.Text = LibNameFoo;
		writeCursor.Index = 0;
		Assert.True(MuiClassServiceStringByteCursorCodec.TryWriteByte(ref platform,
			writeCursor, (byte)'M'));
		Assert.Equal((byte)'M', platform.ReadUInt8(LibNameFoo, 0));
		Assert.True(MuiClassServiceLibraryPrefixRecordCodec.TryGetPayloadAddress(
			ref platform, LibNameFoo, out var payload));
		Assert.Equal(LibNameFoo.Raw + MuiClassServiceLibraryPrefixRecord.Size,
			payload.Raw);
		Assert.False(MuiClassServiceLibraryPrefixRecordCodec.TryGetPayloadAddress(
			ref platform, APTR.FromPointer(uint.MaxValue), out _));
	}

	[Fact]
	public void ClassRecordFieldCursorUsesSemanticRecordKinds()
	{
		var platform = NewPlatform();
		var cursor = default(MuiClassRecordFieldCursor);
		cursor.Address = APTR.FromPointer(0x1400);
		cursor.Record = MuiClassRecordKind.Lease;
		cursor.Field = MuiClassRecordField.SuperService;
		Assert.True(MuiClassRecordFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out var fieldAddress));
		Assert.Equal(0x1420u, fieldAddress.Raw);
		Assert.True(MuiClassRecordFieldCursorCodec.TryWriteUInt32(ref platform,
			cursor.Address, MuiClassRecordKind.State,
			MuiClassRecordField.Generation, 9));
		Assert.True(MuiClassRecordFieldCursorCodec.TryReadUInt32(ref platform,
			cursor.Address, MuiClassRecordKind.State,
			MuiClassRecordField.Generation, out var generation));
		Assert.Equal(9u, generation);
		Assert.True(MuiClassRecordFieldCursorCodec.TryWriteUInt32(ref platform,
			cursor.Address, MuiClassRecordKind.CustomClass,
			MuiClassRecordField.Class, 0xDEADBEEFu));
		Assert.True(MuiClassRecordFieldCursorCodec.TryReadUInt32(ref platform,
			cursor.Address, MuiClassRecordKind.CustomClass,
			MuiClassRecordField.Class, out var classPointer));
		Assert.Equal(0xDEADBEEFu, classPointer);
		cursor.Record = MuiClassRecordKind.CustomClass;
		cursor.Field = MuiClassRecordField.ChildCount;
		Assert.False(MuiClassRecordFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out _));
		cursor.Address = APTR.FromPointer(0xFFFFFFF0u);
		cursor.Field = MuiClassRecordField.Class;
		Assert.False(MuiClassRecordFieldCursorCodec.TryGetAddress(ref platform,
			cursor, out _));
	}

	[Fact]
	public void ClassRecordMemoryAdapterOwnsNamedStructBounds()
	{
		var platform = NewPlatform();
		var state = APTR.FromPointer(0x1800);
		var lease = APTR.FromPointer(0x1900);
		var custom = APTR.FromPointer(0x1A00);

		Assert.True(MuiClassRecordMemoryCodec.TryGetAddress(ref platform, state,
			MuiClassRecordKind.State, MuiClassRecordField.Generation,
			out var generation));
		Assert.Equal(APTR.FromPointer(0x180C), generation);
		Assert.True(MuiClassRecordMemoryCodec.TryWriteUInt32(ref platform, state,
			MuiClassRecordKind.State, MuiClassRecordField.Generation, 7));
		Assert.True(MuiClassRecordMemoryCodec.TryReadUInt32(ref platform, state,
			MuiClassRecordKind.State, MuiClassRecordField.Generation, out var version));
		Assert.Equal(7u, version);

		Assert.True(MuiClassRecordMemoryCodec.TryGetAddress(ref platform, lease,
			MuiClassRecordKind.Lease, MuiClassRecordField.ChildCount,
			out var childCount));
		Assert.Equal(APTR.FromPointer(0x1928), childCount);
		Assert.True(MuiClassRecordMemoryCodec.TryWriteUInt32(ref platform, custom,
			MuiClassRecordKind.CustomClass, MuiClassRecordField.Class,
			0xDEADBEEFu));
		Assert.True(MuiClassRecordMemoryCodec.TryReadUInt32(ref platform, custom,
			MuiClassRecordKind.CustomClass, MuiClassRecordField.Class,
			out var classPointer));
		Assert.Equal(0xDEADBEEFu, classPointer);

		Assert.False(MuiClassRecordMemoryCodec.TryGetAddress(ref platform,
			APTR.FromPointer(0x20FF0), MuiClassRecordKind.Lease,
			MuiClassRecordField.ChildCount, out _));
		Assert.False(MuiClassRecordMemoryCodec.TryGetAddress(ref platform, state,
			(MuiClassRecordKind)255, MuiClassRecordField.Generation, out _));
		Assert.False(MuiClassRecordMemoryCodec.TryGetAddress(ref platform, custom,
			MuiClassRecordKind.CustomClass, (MuiClassRecordField)255, out _));
	}

	[Fact]
	public void ClassRecordFieldAccessUsesCompleteNamedRecords()
	{
		var platform = NewPlatform();
		var stateAddress = APTR.FromPointer(0x1B00);
		var leaseAddress = APTR.FromPointer(0x1B20);
		var customAddress = APTR.FromPointer(0x1B60);
		var state = new MuiClassServiceStateRecord
		{
			Magic = MuiClassServiceLayout.Magic,
			Head = leaseAddress,
			Headless = HeadlessState,
			Generation = 3,
		};
		var lease = new MuiClassServiceLeaseRecord
		{
			Next = stateAddress,
			Flags = 0x80000011u,
			ClassId = FooId,
			Boopsi = APTR.FromPointer(0x5000),
			LibraryBase = LibraryBase,
			RefCount = 2,
			HeadlessClass = HeadlessState,
			CustomClass = customAddress,
			SuperService = stateAddress,
			ObjectCount = 4,
			ChildCount = 5,
		};
		var custom = new MuiCustomClassRecord
		{
			UserData = APTR.FromPointer(0x6000),
			UtilityBase = APTR.FromPointer(0x6010),
			DosBase = APTR.FromPointer(0x6020),
			GfxBase = APTR.FromPointer(0x6030),
			IntuitionBase = APTR.FromPointer(0x6040),
			Super = APTR.FromPointer(0x6050),
			Class = APTR.FromPointer(0x6060),
		};

		Assert.True(MuiClassServiceStateStructCodec.Write(ref platform,
			stateAddress, state));
		Assert.True(MuiClassServiceLeaseStructCodec.Write(ref platform,
			leaseAddress, lease));
		Assert.True(MuiCustomClassStructCodec.Write(ref platform,
			customAddress, custom));
		Assert.True(MuiClassRecordMemoryCodec.TryWriteUInt32(ref platform,
			stateAddress, MuiClassRecordKind.State, MuiClassRecordField.Generation,
			9));
		Assert.True(MuiClassRecordMemoryCodec.TryWriteUInt32(ref platform,
			leaseAddress, MuiClassRecordKind.Lease, MuiClassRecordField.ChildCount,
			8));
		Assert.True(MuiClassRecordMemoryCodec.TryWriteUInt32(ref platform,
			customAddress, MuiClassRecordKind.CustomClass, MuiClassRecordField.Class,
			0xDEADBEEFu));
		Assert.True(MuiClassServiceStateStructCodec.TryRead(ref platform,
			stateAddress, out var updatedState));
		Assert.Equal(state.Head, updatedState.Head);
		Assert.Equal(state.Headless, updatedState.Headless);
		Assert.Equal(9u, updatedState.Generation);
		Assert.True(MuiClassServiceLeaseStructCodec.TryRead(ref platform,
			leaseAddress, out var updatedLease));
		Assert.Equal(lease.Flags, updatedLease.Flags);
		Assert.Equal(lease.CustomClass, updatedLease.CustomClass);
		Assert.Equal(8u, updatedLease.ChildCount);
		Assert.True(MuiCustomClassStructCodec.TryRead(ref platform,
			customAddress, out var updatedCustom));
		Assert.Equal(custom.Super, updatedCustom.Super);
		Assert.Equal(0xDEADBEEFu, updatedCustom.Class.Raw);

		Assert.True(MuiClassRecordMemoryCodec.TryReadUInt32(ref platform,
			stateAddress, MuiClassRecordKind.State, MuiClassRecordField.Headless,
			out var headless));
		Assert.Equal(state.Headless.Raw, headless);
		Assert.True(MuiClassRecordMemoryCodec.TryReadUInt32(ref platform,
			leaseAddress, MuiClassRecordKind.Lease, MuiClassRecordField.Boopsi,
			out var boopsi));
		Assert.Equal(lease.Boopsi.Raw, boopsi);
		Assert.True(MuiClassRecordMemoryCodec.TryReadUInt32(ref platform,
			customAddress, MuiClassRecordKind.CustomClass,
			MuiClassRecordField.UtilityBase, out var utility));
		Assert.Equal(custom.UtilityBase.Raw, utility);

		Assert.False(MuiClassRecordMemoryCodec.TryReadUInt32(ref platform,
			APTR.FromPointer(0x20FF0), MuiClassRecordKind.Lease,
			MuiClassRecordField.Flags, out _));
		Assert.False(MuiClassRecordMemoryCodec.TryWriteUInt32(ref platform,
			stateAddress, (MuiClassRecordKind)255, MuiClassRecordField.Magic, 1));
		Assert.False(MuiClassRecordMemoryCodec.TryReadUInt32(ref platform,
			customAddress, MuiClassRecordKind.CustomClass,
			(MuiClassRecordField)255, out _));
	}

	[Fact]
	public void ClassServiceSequentialStructCodecsRoundTripCompleteRecords()
	{
		var platform = NewPlatform();
		var stateAddress = APTR.FromPointer(0x1B00);
		var leaseAddress = APTR.FromPointer(0x1B20);
		var customAddress = APTR.FromPointer(0x1B60);
		var state = new MuiClassServiceStateRecord
		{
			Magic = MuiClassServiceLayout.Magic,
			Head = leaseAddress,
			Headless = HeadlessState,
			Generation = 0xFEDCBA98u,
		};
		Assert.True(MuiClassServiceStateStructCodec.Write(ref platform,
			stateAddress, state));
		Assert.True(MuiClassServiceStateStructCodec.TryRead(ref platform,
			stateAddress, out var readState));
		Assert.Equal(state.Head, readState.Head);
		Assert.Equal(state.Headless, readState.Headless);
		Assert.Equal(state.Generation, readState.Generation);
		Assert.True(MuiClassServiceStateCodec.TryRead(ref platform, stateAddress,
			out var wrappedState));
		Assert.Equal(state.Magic, wrappedState.Magic);

		var lease = new MuiClassServiceLeaseRecord
		{
			Next = APTR.FromPointer(0x1BA0),
			Flags = 0x80000011u,
			ClassId = APTR.FromPointer(0x1BC0),
			Boopsi = APTR.FromPointer(0x1BE0),
			LibraryBase = APTR.FromPointer(0x1C00),
			RefCount = 0xFEDCBA98u,
			HeadlessClass = APTR.FromPointer(0x1C20),
			CustomClass = customAddress,
			SuperService = stateAddress,
			ObjectCount = 0x81234567u,
			ChildCount = 7,
		};
		Assert.True(MuiClassServiceLeaseStructCodec.Write(ref platform,
			leaseAddress, lease));
		Assert.True(MuiClassServiceLeaseStructCodec.TryRead(ref platform,
			leaseAddress, out var readLease));
		Assert.Equal(lease.Flags, readLease.Flags);
		Assert.Equal(lease.Boopsi, readLease.Boopsi);
		Assert.Equal(lease.ObjectCount, readLease.ObjectCount);
		Assert.Equal(lease.ChildCount, readLease.ChildCount);

		var custom = new MuiCustomClassRecord
		{
			UserData = APTR.FromPointer(0xF1234567u),
			UtilityBase = APTR.FromPointer(0x1C40),
			DosBase = APTR.FromPointer(0x1C60),
			GfxBase = APTR.FromPointer(0x1C80),
			IntuitionBase = APTR.FromPointer(0x1CA0),
			Super = APTR.FromPointer(0x1CC0),
			Class = APTR.FromPointer(0x1CE0),
		};
		Assert.True(MuiCustomClassStructCodec.Write(ref platform, customAddress,
			custom));
		Assert.True(MuiCustomClassStructCodec.TryRead(ref platform, customAddress,
			out var readCustom));
		Assert.Equal(custom.UserData, readCustom.UserData);
		Assert.Equal(custom.Class, readCustom.Class);
		Assert.True(MuiCustomClassCodec.TryRead(ref platform, customAddress,
			out var wrappedCustom));
		Assert.Equal(custom.Super, wrappedCustom.Super);

		Assert.False(MuiClassServiceLeaseStructCodec.TryRead(ref platform,
			APTR.FromPointer(0x20FF0), out _));
		Assert.False(MuiClassServiceStateStructCodec.TryRead(ref platform,
			APTR.FromPointer(0x20FF1), out _));
		Assert.False(MuiCustomClassStructCodec.Write(ref platform, APTR.Null,
			custom));
	}

	// ---- Initialize ----------------------------------------------------------

	[Fact]
	public void InitializeEstablishesGuestResidentStateAndGatesCalls()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			HeadlessState);
		// Before initialization every entry point refuses to touch guest memory.
		Assert.Equal(APTR.Null, MuiClassServiceCore.GetClass(ref platform,
			ServiceState, AreaName));
		Assert.False(MuiClassServiceCore.FreeClass(ref platform, ServiceState,
			PublicClass));
		Assert.True(MuiClassServiceCore.Initialize(ref platform, ServiceState,
			HeadlessState));
		Assert.False(MuiClassServiceCore.Initialize(ref platform, APTR.Null,
			HeadlessState));
	}

	// ---- MUI_CreateCustomClass: named super ---------------------------------

	[Fact]
	public void NamedSuperCustomClassPublishesExactMccAndBindsA6Base()
	{
		var platform = NewPlatform();
		var area = RegisterArea(ref platform);
		Assert.True(area.IsNotNull);
		var superPtr = MuiHeadlessObjectCore.ClassPointer(ref platform, area);

		var mcc = MuiClassServiceCore.CreateCustomClass(ref platform, ServiceState,
			LibraryBase, AreaName, APTR.Null, 96, Dispatcher);
		Assert.True(mcc.IsNotNull);

		// Exact 28-byte struct MUI_CustomClass: five leading APTR fields are the
		// library bases (unmodelled here, hence zero), then mcc_Super at 20 and
		// mcc_Class at 24.
		Assert.True(platform.IsMapped(mcc, 28));
		Assert.Equal(0u, platform.ReadUInt32(mcc, 0));    // mcc_UserData
		Assert.Equal(0u, platform.ReadUInt32(mcc, 4));    // mcc_UtilityBase
		Assert.Equal(0u, platform.ReadUInt32(mcc, 8));    // mcc_DOSBase
		Assert.Equal(0u, platform.ReadUInt32(mcc, 12));   // mcc_GfxBase
		Assert.Equal(0u, platform.ReadUInt32(mcc, 16));   // mcc_IntuitionBase
		Assert.Equal(superPtr.Raw, platform.ReadUInt32(mcc, 20));   // mcc_Super
		var classPtr = APTR.FromPointer(platform.ReadUInt32(mcc, 24)); // mcc_Class
		Assert.True(classPtr.IsNotNull);
		Assert.Equal(superPtr.Raw, platform.ReadUInt32(classPtr, 0));

		// Public class (non-null base) binds A6 to the library base.
		Assert.Equal(LibraryBase.Raw,
			platform.CustomClassLibraryBase(classPtr).Raw);
		Assert.Equal(1u, platform.MakeCustomClassCount);
	}

	[Fact]
	public void PrivateCustomClassLeavesA6Unbound()
	{
		var platform = NewPlatform();
		RegisterArea(ref platform);
		var mcc = MuiClassServiceCore.CreateCustomClass(ref platform, ServiceState,
			APTR.Null, AreaName, APTR.Null, 32, Dispatcher);
		Assert.True(mcc.IsNotNull);
		var classPtr = APTR.FromPointer(platform.ReadUInt32(mcc, 24));
		Assert.Equal(APTR.Null, platform.CustomClassLibraryBase(classPtr));
	}

	// ---- MUI_CreateCustomClass: private mcc super ---------------------------

	[Fact]
	public void PrivateMccSuperResolvesThroughSuperClassStructure()
	{
		var platform = NewPlatform();
		RegisterArea(ref platform);
		var parent = MuiClassServiceCore.CreateCustomClass(ref platform,
			ServiceState, APTR.Null, AreaName, APTR.Null, 32, Dispatcher);
		Assert.True(parent.IsNotNull);
		var parentClass = platform.ReadUInt32(parent, 24);

		var child = MuiClassServiceCore.CreateCustomClass(ref platform, ServiceState,
			APTR.Null, APTR.Null, parent, 48, Dispatcher2);
		Assert.True(child.IsNotNull);
		// The child's super is the parent's struct IClass* (mcc_Class of parent).
		Assert.Equal(parentClass, platform.ReadUInt32(child, 20));
	}

	// ---- Superclass-source enforcement --------------------------------------

	[Fact]
	public void CreateCustomClassEnforcesExactlyOneSuperSource()
	{
		var platform = NewPlatform();
		RegisterArea(ref platform);
		var mcc = MuiClassServiceCore.CreateCustomClass(ref platform, ServiceState,
			APTR.Null, AreaName, APTR.Null, 16, Dispatcher);
		Assert.True(mcc.IsNotNull);

		// Neither source.
		Assert.Equal(APTR.Null, MuiClassServiceCore.CreateCustomClass(ref platform,
			ServiceState, APTR.Null, APTR.Null, APTR.Null, 16, Dispatcher));
		// Both sources.
		Assert.Equal(APTR.Null, MuiClassServiceCore.CreateCustomClass(ref platform,
			ServiceState, APTR.Null, AreaName, mcc, 16, Dispatcher));
	}

	[Fact]
	public void CreateCustomClassRejectsNullDispatcherAndOutOfBoundsDataSize()
	{
		var platform = NewPlatform();
		RegisterArea(ref platform);
		Assert.Equal(APTR.Null, MuiClassServiceCore.CreateCustomClass(ref platform,
			ServiceState, APTR.Null, AreaName, APTR.Null, 16, APTR.Null));
		Assert.Equal(APTR.Null, MuiClassServiceCore.CreateCustomClass(ref platform,
			ServiceState, APTR.Null, AreaName, APTR.Null, -1, Dispatcher));
		Assert.Equal(APTR.Null, MuiClassServiceCore.CreateCustomClass(ref platform,
			ServiceState, APTR.Null, AreaName, APTR.Null, 65536, Dispatcher));
		// Boundary: exactly the UWORD maximum is accepted.
		Assert.True(MuiClassServiceCore.CreateCustomClass(ref platform, ServiceState,
			APTR.Null, AreaName, APTR.Null, 65535, Dispatcher).IsNotNull);
	}

	[Fact]
	public void CreateCustomClassWithUnknownNamedSuperFailsAtomically()
	{
		var platform = NewPlatform();
		// "Area.mui" was never registered, so GetClass cannot resolve the super.
		Assert.Equal(APTR.Null, MuiClassServiceCore.CreateCustomClass(ref platform,
			ServiceState, APTR.Null, AreaName, APTR.Null, 16, Dispatcher));
		Assert.Equal(0u, platform.MakeCustomClassCount);
	}

	// ---- MUI_DeleteCustomClass ----------------------------------------------

	[Fact]
	public void DeleteCustomClassFailsWithOutstandingObjectsThenSucceeds()
	{
		var platform = NewPlatform();
		RegisterArea(ref platform);
		var mcc = MuiClassServiceCore.CreateCustomClass(ref platform, ServiceState,
			APTR.Null, AreaName, APTR.Null, 24, Dispatcher);
		Assert.True(mcc.IsNotNull);

		var obj = MuiClassServiceCore.CreateCustomObject(ref platform, ServiceState,
			mcc, APTR.Null);
		Assert.True(obj.IsNotNull);
		// Outstanding object: deletion must fail and free nothing.
		Assert.False(MuiClassServiceCore.DeleteCustomClass(ref platform,
			ServiceState, mcc));
		Assert.Equal(0u, platform.FreeCustomClassCount);

		Assert.True(MuiClassServiceCore.DisposeCustomObject(ref platform,
			ServiceState, mcc, obj));
		Assert.True(MuiClassServiceCore.DeleteCustomClass(ref platform, ServiceState,
			mcc));
		Assert.Equal(1u, platform.FreeCustomClassCount);
	}

	[Fact]
	public void DeleteCustomClassFailsWithSubClassesThenSucceeds()
	{
		var platform = NewPlatform();
		RegisterArea(ref platform);
		var parent = MuiClassServiceCore.CreateCustomClass(ref platform,
			ServiceState, APTR.Null, AreaName, APTR.Null, 24, Dispatcher);
		var child = MuiClassServiceCore.CreateCustomClass(ref platform, ServiceState,
			APTR.Null, APTR.Null, parent, 24, Dispatcher2);
		Assert.True(parent.IsNotNull);
		Assert.True(child.IsNotNull);

		// Parent has an outstanding sub class.
		Assert.False(MuiClassServiceCore.DeleteCustomClass(ref platform,
			ServiceState, parent));
		Assert.True(MuiClassServiceCore.DeleteCustomClass(ref platform, ServiceState,
			child));
		Assert.True(MuiClassServiceCore.DeleteCustomClass(ref platform, ServiceState,
			parent));
	}

	[Fact]
	public void DeleteCustomClassReleasesNamedSuperLease()
	{
		var platform = NewPlatform();
		var area = RegisterArea(ref platform);
		var superPtr = MuiHeadlessObjectCore.ClassPointer(ref platform, area);

		var mcc = MuiClassServiceCore.CreateCustomClass(ref platform, ServiceState,
			APTR.Null, AreaName, APTR.Null, 24, Dispatcher);
		Assert.True(mcc.IsNotNull);
		// CreateCustomClass leased the named super once.
		Assert.Equal(1u, MuiClassServiceCore.ReferenceCount(ref platform,
			ServiceState, superPtr));

		Assert.True(MuiClassServiceCore.DeleteCustomClass(ref platform, ServiceState,
			mcc));
		// The named super lease was released with the class.
		Assert.Equal(0u, MuiClassServiceCore.ReferenceCount(ref platform,
			ServiceState, superPtr));
	}

	// ---- MUI_GetClass external loading --------------------------------------

	[Fact]
	public void DeleteCustomClassHonorsPlatformRefusalWithoutFreeingState()
	{
		var platform = NewPlatform();
		RegisterArea(ref platform);
		var mcc = MuiClassServiceCore.CreateCustomClass(ref platform, ServiceState,
			APTR.Null, AreaName, APTR.Null, 24, Dispatcher);
		Assert.True(mcc.IsNotNull);
		var classPtr = APTR.FromPointer(platform.ReadUInt32(mcc, 24));

		// The platform models a BOOPSI refusal (for example, an object created
		// outside the tracked helper) by invalidating its releasable marker.
		platform.WriteUInt32(classPtr, 16, 0);
		Assert.False(MuiClassServiceCore.DeleteCustomClass(ref platform,
			ServiceState, mcc));
		Assert.Equal(0u, platform.FreeCustomClassCount);
		Assert.Equal(classPtr.Raw, platform.ReadUInt32(mcc, 24));

		platform.WriteUInt32(classPtr, 16, 0x00C1A550u);
		Assert.True(MuiClassServiceCore.DeleteCustomClass(ref platform,
			ServiceState, mcc));
	}

	[Fact]
	public void ExternalClassRegistryOwnsAStableCopyOfCallerClassId()
	{
		var platform = NewPlatform();
		var query = APTR.FromPointer(0x1380);
		platform.WriteCString(query, "Foo.mcc");
		platform.LoadableLibraryName = LibNameFoo;
		platform.LoadableLibraryBase = LibraryBase;
		platform.LoadablePublicClassId = FooId;
		platform.LoadablePublicClass = PublicClass;

		Assert.Equal(PublicClass.Raw, MuiClassServiceCore.GetClass(ref platform,
			ServiceState, FooId).Raw);
		// The caller owns its input buffer and may immediately reuse it.
		platform.WriteCString(FooId, "reused");
		Assert.True(MuiHeadlessObjectCore.FindClassByName(ref platform,
			HeadlessState, query).IsNotNull);
		Assert.Equal(PublicClass.Raw, MuiClassServiceCore.GetClass(ref platform,
			ServiceState, query).Raw);
		Assert.Equal(1u, platform.OpenLibraryCount);
		Assert.True(MuiClassServiceCore.FreeClass(ref platform, ServiceState,
			PublicClass));
		Assert.True(MuiClassServiceCore.FreeClass(ref platform, ServiceState,
			PublicClass));
	}

	[Fact]
	public void GetClassLoadsExternalClassThroughMuiLibraryAndRegistersIt()
	{
		var platform = NewPlatform();
		platform.LoadableLibraryName = LibNameFoo;
		platform.LoadableLibraryBase = LibraryBase;
		platform.LoadablePublicClassId = FooId;
		platform.LoadablePublicClass = PublicClass;

		var cls = MuiClassServiceCore.GetClass(ref platform, ServiceState, FooId);
		Assert.Equal(PublicClass.Raw, cls.Raw);
		Assert.Equal(1u, platform.OpenLibraryCount);
		Assert.Equal(0u, platform.CloseLibraryCount);
		// The external class is now visible in the headless registry.
		Assert.True(MuiHeadlessObjectCore.FindClassByName(ref platform,
			HeadlessState, FooId).IsNotNull);
	}

	[Fact]
	public void GetClassIsCaseSensitiveOnTheClassId()
	{
		var platform = NewPlatform();
		platform.LoadableLibraryName = LibNameFoo;
		platform.LoadableLibraryBase = LibraryBase;
		platform.LoadablePublicClassId = FooId;
		platform.LoadablePublicClass = PublicClass;

		// "foo.mcc" builds "mui/foo.mcc" which the loader does not recognise.
		Assert.Equal(APTR.Null, MuiClassServiceCore.GetClass(ref platform,
			ServiceState, FooIdLower));
		Assert.Equal(0u, platform.CloseLibraryCount);
		// The exact-case id still resolves.
		Assert.Equal(PublicClass.Raw, MuiClassServiceCore.GetClass(ref platform,
			ServiceState, FooId).Raw);
	}

	[Fact]
	public void GetClassRefcountsSharedExternalClassAndFreeClassClosesLoader()
	{
		var platform = NewPlatform();
		platform.LoadableLibraryName = LibNameFoo;
		platform.LoadableLibraryBase = LibraryBase;
		platform.LoadablePublicClassId = FooId;
		platform.LoadablePublicClass = PublicClass;

		var first = MuiClassServiceCore.GetClass(ref platform, ServiceState, FooId);
		var second = MuiClassServiceCore.GetClass(ref platform, ServiceState, FooId);
		Assert.Equal(PublicClass.Raw, first.Raw);
		Assert.Equal(PublicClass.Raw, second.Raw);
		// Second GetClass shares the lease; the library is opened exactly once.
		Assert.Equal(1u, platform.OpenLibraryCount);
		Assert.Equal(2u, MuiClassServiceCore.ReferenceCount(ref platform,
			ServiceState, PublicClass));

		// First FreeClass only drops the reference count.
		Assert.True(MuiClassServiceCore.FreeClass(ref platform, ServiceState,
			PublicClass));
		Assert.Equal(0u, platform.CloseLibraryCount);
		Assert.True(MuiHeadlessObjectCore.FindClassByName(ref platform,
			HeadlessState, FooId).IsNotNull);

		// Final FreeClass closes the loader lease and unregisters the class.
		Assert.True(MuiClassServiceCore.FreeClass(ref platform, ServiceState,
			PublicClass));
		Assert.Equal(1u, platform.CloseLibraryCount);
		Assert.False(MuiHeadlessObjectCore.FindClassByName(ref platform,
			HeadlessState, FooId).IsNotNull);
		Assert.Equal(0u, MuiClassServiceCore.ReferenceCount(ref platform,
			ServiceState, PublicClass));
	}

	[Fact]
	public void GetClassRollsBackLoaderLeaseWhenPublicClassCannotBeResolved()
	{
		var platform = NewPlatform();
		// OpenLibrary succeeds for "mui/Bar.mcc" but the public class id the
		// library publishes does not match, so resolution fails mid-load.
		platform.LoadableLibraryName = LibNameBar;
		platform.LoadableLibraryBase = LibraryBase;
		platform.LoadablePublicClassId = NopeId;
		platform.LoadablePublicClass = PublicClass;

		Assert.Equal(APTR.Null, MuiClassServiceCore.GetClass(ref platform,
			ServiceState, BarId));
		// The loader lease opened during the attempt is closed on rollback.
		Assert.Equal(1u, platform.OpenLibraryCount);
		Assert.Equal(1u, platform.CloseLibraryCount);
		// Nothing is registered or leased.
		Assert.False(MuiHeadlessObjectCore.FindClassByName(ref platform,
			HeadlessState, BarId).IsNotNull);
		Assert.Equal(0u, MuiClassServiceCore.ReferenceCount(ref platform,
			ServiceState, PublicClass));
	}

	[Fact]
	public void GetClassReturnsBuiltinRegistryClassWithoutLoading()
	{
		var platform = NewPlatform();
		var area = RegisterArea(ref platform);
		var superPtr = MuiHeadlessObjectCore.ClassPointer(ref platform, area);

		var cls = MuiClassServiceCore.GetClass(ref platform, ServiceState, AreaName);
		Assert.Equal(superPtr.Raw, cls.Raw);
		// A builtin class is never loaded through the mui/<id> loader.
		Assert.Equal(0u, platform.OpenLibraryCount);
		// FreeClass on a builtin lease never closes a library.
		Assert.True(MuiClassServiceCore.FreeClass(ref platform, ServiceState, cls));
		Assert.Equal(0u, platform.CloseLibraryCount);
	}
}
