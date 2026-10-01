using Amiga;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiBoopsiBaseConstructionTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);
	private static readonly APTR Message = APTR.FromPointer(0x1300);
	private static readonly APTR Tags = APTR.FromPointer(0x1400);
	private static readonly APTR Operand = APTR.FromPointer(0xDADA);

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void SuperclassConstructionPrecedesSingleAttachmentToOwningClass(bool derived)
	{
		var platform = Create(out var baseClass);
		var owning = baseClass;
		if (derived)
		{
			platform.WriteCString(APTR.FromPointer(0x1500), "Derived.mui");
			owning = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform, State,
				APTR.FromPointer(0x1500), MuiHeadlessObjectCore.ClassPointer(ref platform, baseClass),
				8, APTR.FromPointer(0xD100));
		}
		var native = platform.NewObject(MuiHeadlessObjectCore.ClassPointer(ref platform, owning), APTR.Null);
		platform.DispatchResult = native.Raw;
		platform.BeforeSuperMethod = (actualClass, operand, message) =>
		{
			Assert.Equal(MuiHeadlessObjectCore.ClassPointer(ref platform, baseClass), actualClass);
			Assert.Equal(Operand, operand);
			Assert.Equal(Message, message);
			Assert.True(MuiHeadlessObjectCore.FindObject(ref platform, State, native).IsNull);
		};
		Assert.True(MuiBoopsiBaseConstructionCore.TryConstruct(ref platform, State, baseClass,
			owning, Operand, Message, out var result));
		Assert.Equal(native, result.NativeObject);
		Assert.Equal(native, result.InitializedObject);
		Assert.Equal(MuiObjectAttachmentOwnership.Transferred, result.Ownership);
		Assert.Equal(1u, platform.SuperMethodCalls);
		Assert.True(MuiHeadlessObjectCodec.TryRead(ref platform,
			MuiHeadlessObjectCore.FindObject(ref platform, State, native), out var objectValue));
		Assert.Equal(owning, objectValue.Class);
		Assert.NotEqual(0u, objectValue.Flags & MuiHeadlessObjectCore.ObjectInitialized);
		var allocations = platform.AllocationCount;
		Assert.Equal(native, MuiHeadlessObjectCore.CompleteConstructedObject(ref platform,
			State, owning, native, APTR.FromPointer(0xFFFFFFFC)));
		Assert.Equal(allocations, platform.AllocationCount);
		Assert.True(MuiHeadlessClassCodec.TryRead(ref platform, owning, out var owner));
		Assert.Equal(1u, owner.ObjectCount);
		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State, native));
	}

	[Fact]
	public void AllocationFailureBeforePublicationLeavesNativeObjectWithCaller()
	{
		var platform = Create(out var cls);
		var native = platform.NewObject(MuiHeadlessObjectCore.ClassPointer(ref platform, cls), APTR.Null);
		platform.DispatchResult = native.Raw;
		platform.AllocationAdmission = (size, _) => size != MuiHeadlessObjectRecord.Size;
		Assert.False(MuiBoopsiBaseConstructionCore.TryConstruct(ref platform, State, cls, cls,
			Operand, Message, out var result));
		Assert.Equal(native, result.NativeObject);
		Assert.True(result.InitializedObject.IsNull);
		Assert.Equal(MuiObjectAttachmentOwnership.CallerOwned, result.Ownership);
		Assert.Equal(0u, platform.NativeObjectDisposalCount);
		Assert.True(MuiHeadlessObjectCore.FindObject(ref platform, State, native).IsNull);
		Assert.True(MuiHeadlessClassCodec.TryRead(ref platform, cls, out var classValue));
		Assert.Equal(0u, classValue.ObjectCount);
		platform.AllocationAdmission = null;
		platform.DisposeObject(native);
	}

	[Fact]
	public void InitializationFailureRetainsPublishedSidecarForExplicitDestructor()
	{
		var platform = Create(out var cls);
		var native = platform.NewObject(MuiHeadlessObjectCore.ClassPointer(ref platform, cls), APTR.Null);
		platform.DispatchResult = native.Raw;
		platform.BeforeSuperMethod = (_, _, _) => platform.MappingAdmission = (address, _) => address != Tags;
		Assert.False(MuiBoopsiBaseConstructionCore.TryConstruct(ref platform, State, cls, cls,
			Operand, Message, out var result));
		Assert.Equal(native, result.NativeObject);
		Assert.True(result.InitializedObject.IsNull);
		Assert.Equal(MuiObjectAttachmentOwnership.Transferred, result.Ownership);
		Assert.Equal(0u, platform.NativeObjectDisposalCount);
		platform.MappingAdmission = null;
		Assert.True(MuiHeadlessObjectCodec.TryRead(ref platform,
			MuiHeadlessObjectCore.FindObject(ref platform, State, native), out var partial));
		Assert.Equal(0u, partial.Flags & MuiHeadlessObjectCore.ObjectInitialized);
		Assert.True(MuiHeadlessClassCodec.TryRead(ref platform, cls, out var classValue));
		Assert.Equal(1u, classValue.ObjectCount);
		BOOPSIGuestCodec.WriteMethodId(ref platform, Message, BOOPSI.OM_DISPOSE);
		platform.SuperDisposeNative = true;
		platform.DispatchResult = 0;
		Assert.True(MuiBoopsiBaseDisposalCore.TryDispose(ref platform, State, cls, native, Message, out var disposed));
		Assert.Equal(0u, disposed);
		Assert.Equal(1u, platform.NativeObjectDisposalCount);
		Assert.True(MuiHeadlessObjectCore.FindObject(ref platform, State, native).IsNull);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void ExistingOrUnreadableSuperclassResultDoesNotConsumeAlias(bool unreadable)
	{
		var platform = Create(out var cls);
		var existing = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cls, APTR.Null);
		var record = MuiHeadlessObjectCore.FindObject(ref platform, State, existing);
		platform.DispatchResult = existing.Raw;
		if (unreadable)
			platform.BeforeSuperMethod = (_, _, _) => platform.MappingAdmission = (address, _) => address != record;
		var allocations = platform.AllocationCount;
		var frees = platform.FreeCount;
		Assert.False(MuiBoopsiBaseConstructionCore.TryConstruct(ref platform, State, cls, cls,
			Operand, Message, out var result));
		Assert.Equal(existing, result.NativeObject);
		Assert.True(result.InitializedObject.IsNull);
		Assert.Equal(unreadable ? MuiObjectAttachmentOwnership.Unresolved :
			MuiObjectAttachmentOwnership.AlreadyRegistered, result.Ownership);
		Assert.Equal(allocations, platform.AllocationCount);
		Assert.Equal(frees, platform.FreeCount);
		Assert.Equal(0u, platform.NativeObjectDisposalCount);
		platform.MappingAdmission = null;
		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State, existing));
	}

	[Fact]
	public void NullSuperclassResultDoesNotAllocateOrAttach()
	{
		var platform = Create(out var cls);
		platform.DispatchResult = 0;
		var allocations = platform.AllocationCount;
		Assert.False(MuiBoopsiBaseConstructionCore.TryConstruct(ref platform, State, cls, cls,
			Operand, Message, out var result));
		Assert.True(result.NativeObject.IsNull);
		Assert.True(result.InitializedObject.IsNull);
		Assert.Equal(1u, platform.SuperMethodCalls);
		Assert.Equal(allocations, platform.AllocationCount);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(1)]
	[InlineData(2)]
	[InlineData(3)]
	[InlineData(4)]
	public void InvalidInputsDoNotInvokeSuperclass(int invalid)
	{
		var platform = Create(out var cls);
		var message = Message;
		var owner = cls;
		if (invalid == 0) message = APTR.FromPointer(0xFFFFFFFC);
		if (invalid == 1) BOOPSIGuestCodec.WriteMethodId(ref platform, message, BOOPSI.OM_DISPOSE);
		if (invalid == 2) BOOPSIGuestCodec.WriteOpSet(ref platform, message,
			new opSet { MethodID = BOOPSI.OM_NEW, ops_AttrList = APTR.FromPointer(0xFFFFFFFC) });
		if (invalid == 3) owner = APTR.Null;
		if (invalid == 4)
		{
			message = APTR.FromPointer(Message.Raw + 1);
			BOOPSIGuestCodec.WriteOpSet(ref platform, message, new opSet { MethodID = BOOPSI.OM_NEW });
		}
		Assert.False(MuiBoopsiBaseConstructionCore.TryConstruct(ref platform, State, cls, owner,
			Operand, message, out var result));
		Assert.True(result.NativeObject.IsNull);
		Assert.Equal(0u, platform.SuperMethodCalls);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void FullTagValidationDoesNotStopAtMaximumPayloadTag(bool malformedTail)
	{
		var platform = Create(out var cls);
		Assert.True(MuiAslTagItemCodec.Write(ref platform, Tags,
			new MuiAslTagItemRecord { Tag = uint.MaxValue, Data = 73 }));
		Assert.True(MuiAslTagItemVectorMemoryCodec.TryGetEntry(ref platform, Tags, 1, out var next));
		Assert.True(MuiAslTagItemCodec.Write(ref platform, next, malformedTail
			? new MuiAslTagItemRecord { Tag = MuiAslTagListCore.TagMore, Data = 0xFFFFFFFC }
			: default));
		Assert.True(MuiAslTagListCore.TryGetData(ref platform, Tags, uint.MaxValue, 0, out var data));
		Assert.Equal(73u, data); // First-match lookup remains a distinct operation.
		Assert.Equal(!malformedTail, MuiAslTagListCore.Validate(ref platform, Tags));
		if (malformedTail)
		{
			Assert.False(MuiBoopsiBaseConstructionCore.TryConstruct(ref platform, State, cls, cls,
				Operand, Message, out _));
			Assert.Equal(0u, platform.SuperMethodCalls);
		}
	}

	private static MuiHeadlessTestPlatform Create(out APTR cls)
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000, State);
		platform.WriteCString(APTR.FromPointer(0x1100), "Notify.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		cls = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform, State,
			APTR.FromPointer(0x1100), APTR.Null, 8, APTR.FromPointer(0xD000));
		BOOPSIGuestCodec.WriteOpSet(ref platform, Message,
			new opSet { MethodID = BOOPSI.OM_NEW, ops_AttrList = Tags, ops_GInfo = APTR.FromPointer(0x1600) });
		return platform; // Zeroed Tags is a bounded TAG_DONE item.
	}
}
