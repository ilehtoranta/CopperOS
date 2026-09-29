using Amiga;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiBoopsiBaseDisposalTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);
	private static readonly APTR Message = APTR.FromPointer(0x1300);

	[Theory]
	[InlineData(0u)]
	[InlineData(73u)]
	public void CleanupPrecedesExactlyOneSuperclassCallAndPreservesItsResult(uint superclassResult)
	{
		var platform = Create(out var cls, out var obj);
		platform.DispatchResult = superclassResult;
		Assert.True(MuiHeadlessClassCodec.TryRead(ref platform, cls, out var classValue));
		platform.BeforeSuperMethod = (actualClass, actualObject, actualMessage) =>
		{
			Assert.Equal(classValue.Boopsi, actualClass);
			Assert.Equal(obj, actualObject);
			Assert.Equal(Message, actualMessage);
			Assert.True(MuiHeadlessObjectCore.FindObject(ref platform, State, obj).IsNull);
			Assert.True(MuiHeadlessClassCodec.TryRead(ref platform, cls, out var retired));
			Assert.Equal(0u, retired.ObjectCount);
			Assert.Equal(0u, platform.NativeObjectDisposalCount);
			Assert.False(MuiBoopsiBaseDisposalCore.TryDispose(ref platform, State,
				cls, obj, Message, out _));
		};
		Assert.True(MuiBoopsiBaseDisposalCore.TryDispose(ref platform, State, cls, obj, Message, out var result));
		Assert.Equal(superclassResult, result);
		Assert.Equal(1u, platform.SuperMethodCalls);
		Assert.Equal(1u, platform.NativeObjectDisposalCount);
		Assert.False(MuiBoopsiBaseDisposalCore.TryDispose(ref platform, State, cls, obj, Message, out _));
		Assert.Equal(1u, platform.SuperMethodCalls);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void PendingChildKeepsSuperclassUntouchedUntilRetry(bool precedingSibling)
	{
		var platform = Create(out var cls, out var obj);
		if (precedingSibling)
		{
			var sibling = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cls, APTR.Null);
			Assert.True(MuiFamilyCore.AddTail(ref platform, State, obj, sibling));
		}
		var child = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cls, APTR.Null);
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, obj, child));
		var childRecord = MuiHeadlessObjectCore.FindObject(ref platform, State, child);
		Assert.True(MuiHeadlessObjectCodec.TryRead(ref platform, childRecord, out var saved));
		var busy = saved;
		busy.Flags |= MuiHeadlessObjectCore.ObjectProviderBusy;
		Assert.True(MuiHeadlessObjectCodec.Write(ref platform, childRecord, busy));
		Assert.False(MuiBoopsiBaseDisposalCore.TryDispose(ref platform, State, cls, obj, Message, out var result));
		Assert.Equal(0u, result);
		Assert.Equal(0u, platform.SuperMethodCalls);
		Assert.Equal(precedingSibling ? 1u : 0u, platform.NativeObjectDisposalCount);
		Assert.True(MuiHeadlessObjectCore.FindObject(ref platform, State, obj).IsNotNull);
		Assert.False(MuiHeadlessObjectCore.DisposeObject(ref platform, State, obj)); // Cannot switch cleanup mode.
		Assert.True(MuiHeadlessObjectCodec.TryRead(ref platform, childRecord, out var pending));
		pending.Flags &= ~MuiHeadlessObjectCore.ObjectProviderBusy;
		Assert.True(MuiHeadlessObjectCodec.Write(ref platform, childRecord, pending));
		Assert.True(MuiBoopsiBaseDisposalCore.TryDispose(ref platform, State, cls, obj, Message, out _));
		Assert.Equal(1u, platform.SuperMethodCalls);
		Assert.Equal(precedingSibling ? 3u : 2u, platform.NativeObjectDisposalCount);
		Assert.True(MuiHeadlessStateCodec.TryRead(ref platform, State, out var registry));
		Assert.True(registry.Objects.IsNull);
	}

	[Fact]
	public void UsesDispatchingBaseClassRatherThanMostDerivedOwningClass()
	{
		var platform = Create(out var baseClass, out var initial);
		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State, initial));
		Assert.True(MuiHeadlessClassCodec.TryRead(ref platform, baseClass, out var baseValue));
		var name = APTR.FromPointer(0x1400);
		platform.WriteCString(name, "Custom.mui");
		var derived = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform, State,
			name, baseValue.Boopsi, 8, APTR.FromPointer(0xD100));
		var obj = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, derived, APTR.Null);
		Assert.True(MuiBoopsiBaseDisposalCore.TryDispose(ref platform, State, baseClass, obj, Message, out _));
		Assert.Equal(baseValue.Boopsi, platform.LastSuperClass);
		Assert.True(MuiHeadlessClassCodec.TryRead(ref platform, derived, out var after));
		Assert.Equal(0u, after.ObjectCount);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(1)]
	[InlineData(2)]
	[InlineData(3)]
	[InlineData(4)]
	public void InvalidMessageOrClassCannotStartCleanup(int invalid)
	{
		var platform = Create(out var cls, out var obj);
		var message = Message;
		var selected = cls;
		if (invalid == 0) message = APTR.FromPointer(0xFFFFFFFC);
		if (invalid == 1) BOOPSIGuestCodec.WriteMethodId(ref platform, message, BOOPSI.OM_SET);
		if (invalid == 2) selected = APTR.Null;
		if (invalid == 4)
		{
			message = APTR.FromPointer(Message.Raw + 1);
			BOOPSIGuestCodec.WriteMethodId(ref platform, message, BOOPSI.OM_DISPOSE);
		}
		if (invalid == 3)
		{
			var name = APTR.FromPointer(0x1400);
			platform.WriteCString(name, "Unrelated.mui");
			selected = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform, State,
				name, APTR.Null, 8, APTR.FromPointer(0xD100));
		}
		var record = MuiHeadlessObjectCore.FindObject(ref platform, State, obj);
		Assert.True(MuiHeadlessObjectCodec.TryRead(ref platform, record, out var before));
		var frees = platform.FreeCount;
		Assert.False(MuiBoopsiBaseDisposalCore.TryDispose(ref platform, State, selected, obj, message, out _));
		Assert.Equal(0u, platform.SuperMethodCalls);
		Assert.Equal(frees, platform.FreeCount);
		Assert.True(MuiHeadlessObjectCodec.TryRead(ref platform, record, out var after));
		Assert.Equal(before, after);
		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State, obj));
	}

	[Fact]
	public void SuperclassCycleRejectsUnrelatedDispatcherWithoutRetiringObject()
	{
		var platform = Create(out var cls, out var obj);
		var name = APTR.FromPointer(0x1400);
		platform.WriteCString(name, "Unrelated.mui");
		var unrelated = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform, State,
			name, APTR.Null, 8, APTR.FromPointer(0xD100));
		Assert.True(MuiHeadlessClassCodec.TryRead(ref platform, cls, out var saved));
		var cyclic = saved;
		cyclic.Super = cyclic.Boopsi;
		Assert.True(MuiHeadlessClassCodec.Write(ref platform, cls, cyclic));
		Assert.False(MuiBoopsiBaseDisposalCore.TryDispose(ref platform, State, unrelated, obj, Message, out _));
		Assert.Equal(0u, platform.SuperMethodCalls);
		Assert.Equal(0u, platform.NativeObjectDisposalCount);
		Assert.True(MuiHeadlessObjectCore.FindObject(ref platform, State, obj).IsNotNull);
		Assert.True(MuiHeadlessClassCodec.Write(ref platform, cls, saved));
		Assert.True(MuiBoopsiBaseDisposalCore.TryDispose(ref platform, State, cls, obj, Message, out _));
	}

	private static MuiHeadlessTestPlatform Create(out APTR cls, out APTR obj)
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000, State);
		var name = APTR.FromPointer(0x1100);
		platform.WriteCString(name, "Notify.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		cls = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform, State, name,
			APTR.Null, 8, APTR.FromPointer(0xD000));
		obj = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cls, APTR.Null);
		Assert.True(obj.IsNotNull);
		BOOPSIGuestCodec.WriteMethodId(ref platform, Message, BOOPSI.OM_DISPOSE);
		platform.SuperDisposeNative = true;
		return platform;
	}
}
