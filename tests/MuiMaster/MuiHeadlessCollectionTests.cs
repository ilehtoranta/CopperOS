using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiHeadlessCollectionTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);

	[Fact]
	public void StoreIterationCounterUsesNamedOrdinal()
	{
		var platform = CreatePlatform(out _);
		var address = APTR.FromPointer(0x1180);
		var expected = new MuiStoreIterationCounter { Ordinal = 3 };

		Assert.True(MuiStoreIterationCounterCodec.Write(ref platform, address,
			expected));
		Assert.True(MuiStoreIterationCounterCodec.TryRead(ref platform, address,
			out var actual));
		Assert.Equal(expected.Ordinal, actual.Ordinal);
		Assert.False(MuiStoreIterationCounterCodec.TryRead(ref platform,
			APTR.Null, out _));
	}

	[Fact]
	public void StoreIterationCounterUsesNamedFieldCursor()
	{
		var platform = CreatePlatform(out _);
		var record = APTR.FromPointer(0x11C0);
		var cursor = new MuiStoreIterationCounterFieldCursor
		{
			Record = record,
			Field = MuiStoreIterationCounterField.Ordinal,
		};
		Assert.True(MuiStoreIterationCounterFieldCursorCodec.TryGetAddress(
			ref platform, cursor, out var address));
		Assert.Equal(record, address);
		Assert.True(MuiStoreIterationCounterFieldCursorCodec.TryWriteUInt32(
			ref platform, record, MuiStoreIterationCounterField.Ordinal, 5));
		Assert.True(MuiStoreIterationCounterFieldCursorCodec.TryReadUInt32(
			ref platform, record, MuiStoreIterationCounterField.Ordinal,
			out var ordinal));
		Assert.Equal(5u, ordinal);
		Assert.False(MuiStoreIterationCounterFieldCursorCodec.TryReadUInt32(
			ref platform, record,
			unchecked((MuiStoreIterationCounterField)255), out _));
	}

	[Fact]
	public void FamilyMutationVectorCodecUsesNamedObjectField()
	{
		var platform = CreatePlatform(out _);
		var address = APTR.FromPointer(0x1200);
		var expected = default(MuiFamilyMutationVectorEntry);
		expected.Object = APTR.FromPointer(0x1800);
		Assert.True(MuiFamilyMutationVectorCodec.Write(ref platform, address,
			expected));
		Assert.True(MuiFamilyMutationVectorCodec.TryRead(ref platform, address,
			out var actual));
		Assert.Equal(expected.Object, actual.Object);
		Assert.False(MuiFamilyMutationVectorCodec.TryRead(ref platform,
			APTR.FromPointer(0x40000), out _));
	}

	[Fact]
	public void FamilyMutationVectorAddressesUseNamedEntryBoundary()
	{
		var platform = CreatePlatform(out _);
		var packet = APTR.FromPointer(0x1200);

		Assert.True(MuiFamilyMutationMessageCodec.TryGetVectorBase(
			ref platform, packet, 8, out var vector));
		Assert.Equal(APTR.FromPointer(0x1208), vector);
		Assert.True(MuiFamilyMutationMessageCodec.TryGetVectorEntry(
			ref platform, packet, 8, 2, out var entry));
		Assert.Equal(APTR.FromPointer(0x1210), entry);
		Assert.False(MuiFamilyMutationMessageCodec.TryGetVectorEntry(
			ref platform, APTR.Null, 8, 0, out _));
		Assert.False(MuiFamilyMutationMessageCodec.TryGetVectorEntry(
			ref platform, APTR.FromPointer(0x30FFE), 8, 0, out _));
	}

	[Fact]
	public void FamilyInlineVectorCursorUsesNamedPacketBoundary()
	{
		var platform = CreatePlatform(out _);
		var cursor = new MuiFamilyInlineVectorCursor
		{
			Message = APTR.FromPointer(0x1200),
			ArrayOffset = 8,
			Index = 2,
		};

		Assert.True(MuiFamilyInlineVectorCursorCodec.TryGetEntry(ref platform,
			cursor, out var address));
		Assert.Equal(APTR.FromPointer(0x1210), address);
		cursor.Message = APTR.FromPointer(0xFFFFFFFC);
		Assert.False(MuiFamilyInlineVectorCursorCodec.TryGetEntry(ref platform,
			cursor, out _));
	}

	[Fact]
	public void FamilyMutationVectorCursorUsesNamedEntryBoundary()
	{
		var platform = CreatePlatform(out _);
		var cursor = new MuiFamilyMutationVectorCursor
		{
			Base = APTR.FromPointer(0x1800),
			Index = 2,
		};

		Assert.True(MuiFamilyMutationVectorCodec.TryGetEntry(ref platform,
			cursor, out var address));
		Assert.Equal(APTR.FromPointer(0x1808), address);
		cursor.Base = APTR.FromPointer(0x30FFE);
		cursor.Index = 0;
		Assert.False(MuiFamilyMutationVectorCodec.TryGetEntry(ref platform,
			cursor, out _));
	}

	[Fact]
	public void FamilyOwnsOrdersRemovesAndDisposesChildren()
	{
		var platform = CreatePlatform(out var cl);
		var family = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		var first = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		var second = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, family, first));
		Assert.True(MuiFamilyCore.AddHead(ref platform, State, family, second));
		Assert.Equal(second, MuiFamilyCore.GetChild(ref platform, State, family,
			0, APTR.Null));
		Assert.Equal(first, MuiFamilyCore.GetChild(ref platform, State, family,
			-1, APTR.Null));
		var order = APTR.FromPointer(0x1300);
		platform.WriteUInt32(order, 0, first.Raw);
		platform.WriteUInt32(order, 4, second.Raw);
		platform.WriteUInt32(order, 8, 0);
		Assert.True(MuiFamilyCore.Sort(ref platform, State, family, order));
		Assert.Equal(first, MuiFamilyCore.GetChild(ref platform, State, family,
			0, APTR.Null));
		platform.WriteUInt32(order, 0, first.Raw);
		platform.WriteUInt32(order, 4, 0);
		Assert.True(MuiFamilyCore.Reorder(ref platform, State, family, second,
			order));
		Assert.Equal(second, MuiFamilyCore.GetChild(ref platform, State, family,
			0, APTR.Null));
		Assert.False(MuiFamilyCore.AddTail(ref platform, State, family, first));
		Assert.True(MuiFamilyCore.Remove(ref platform, State, family, second));
		Assert.Equal(1u, platform.ReadUInt32(second, 4));
		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State,
			family));
		Assert.Equal(APTR.Null, MuiHeadlessObjectCore.FindObject(ref platform,
			State, first));
		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State,
			second));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State, cl));
	}

	[Fact]
	public void DataspaceDatamapAndObjectmapOwnAndIterateEntries()
	{
		var platform = CreatePlatform(out var cl);
		var store = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		var data = APTR.FromPointer(0x1300);
		platform.WriteUInt32(data, 0, 0x11223344);
		Assert.True(MuiStoreCore.DataspaceAdd(ref platform, State, store, 7,
			data, 4));
		platform.WriteUInt32(data, 0, 0);
		var size = APTR.FromPointer(0x1310);
		var stored = MuiStoreCore.DataspaceGet(ref platform, State, store, 7,
			size);
		Assert.Equal(4u, platform.ReadUInt32(size, 0));
		Assert.Equal(0x11223344u, platform.ReadUInt32(stored, 0));
		var mergedStore = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			cl, APTR.Null);
		Assert.True(MuiStoreCore.DataspaceMerge(ref platform, State, mergedStore,
			store));
		var merged = MuiStoreCore.DataspaceFind(ref platform, State, mergedStore, 7);
		Assert.NotEqual(stored, merged);
		Assert.Equal(0x11223344u, platform.ReadUInt32(merged, 0));

		var key = APTR.FromPointer(0x1340);
		var keyCopy = APTR.FromPointer(0x1380);
		platform.WriteCString(key, "alpha");
		platform.WriteCString(keyCopy, "alpha");
		platform.WriteUInt32(data, 0, 0x55667788);
		Assert.True(MuiStoreCore.DatamapSet(ref platform, State, store, key,
			data, 4, true));
		Assert.Equal(0x55667788u, platform.ReadUInt32(
			MuiStoreCore.DatamapFind(ref platform, State, store, keyCopy), 0));
		Assert.True(MuiStoreCore.ObjectmapSet(ref platform, State, store,
			APTR.FromPointer(0xABC0), APTR.FromPointer(0xDEF0)));
		Assert.Equal(APTR.FromPointer(0xDEF0), MuiStoreCore.ObjectmapFind(
			ref platform, State, store, APTR.FromPointer(0xABC0)));
		var counter = APTR.FromPointer(0x13C0);
		platform.WriteUInt32(counter, 0, 0);
		Assert.Equal(APTR.FromPointer(0xABC0),
			MuiStoreCore.ObjectmapIterationKey(ref platform, State, store,
				counter));
		Assert.True(MuiStoreCore.DataspaceRemove(ref platform, State, store, 7));
		Assert.True(MuiStoreCore.DatamapRemove(ref platform, State, store,
			keyCopy));
		Assert.True(MuiStoreCore.ObjectmapRemove(ref platform, State, store,
			APTR.FromPointer(0xABC0)));
		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State,
			store));
		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State,
			mergedStore));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State, cl));
	}

	[Fact]
	public void SemaphoreTracksExclusiveSharedAndNestedOwnership()
	{
		var platform = CreatePlatform(out var cl);
		var semaphore = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			cl, APTR.Null);
		Assert.True(MuiSemaphoreCore.Attempt(ref platform, State, semaphore));
		Assert.True(MuiSemaphoreCore.Obtain(ref platform, State, semaphore));
		platform.CurrentTask = 2;
		Assert.False(MuiSemaphoreCore.Attempt(ref platform, State, semaphore));
		Assert.False(MuiSemaphoreCore.AttemptShared(ref platform, State,
			semaphore));
		platform.CurrentTask = 1;
		Assert.True(MuiSemaphoreCore.Release(ref platform, State, semaphore));
		Assert.True(MuiSemaphoreCore.Release(ref platform, State, semaphore));
		platform.CurrentTask = 2;
		Assert.True(MuiSemaphoreCore.ObtainShared(ref platform, State, semaphore));
		platform.CurrentTask = 1;
		Assert.False(MuiSemaphoreCore.Attempt(ref platform, State, semaphore));
		platform.CurrentTask = 2;
		Assert.True(MuiSemaphoreCore.Release(ref platform, State, semaphore));
		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State,
			semaphore));
		Assert.True(MuiHeadlessObjectCore.DeleteClass(ref platform, State, cl));
	}

	private static MuiHeadlessTestPlatform CreatePlatform(out APTR cl)
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x5000,
			State);
		var name = APTR.FromPointer(0x1100);
		platform.WriteCString(name, "test.mui");
		MuiHeadlessObjectCore.Initialize(ref platform, State);
		cl = MuiHeadlessObjectCore.RegisterClass(ref platform, State, name,
			APTR.Null, 0, APTR.FromPointer(1), false);
		return platform;
	}
}
