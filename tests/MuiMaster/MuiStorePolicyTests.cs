using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiStorePolicyTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);

	[Fact]
	public void DatamapPolicyUsesNamedRecordAndIsInitOnly()
	{
		var platform = CreatePlatform("Datamap.mui", out var classRecord);
		var datamap = MuiCommonControlCore.CreateControl(ref platform, State,
			classRecord, APTR.Null);
		Assert.NotEqual(APTR.Null, datamap);
		Assert.Equal(MuiControlClass.Datamap,
			MuiCommonControlCore.Classify(ref platform, State, datamap));

		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State,
			datamap, MuiStorePolicyCore.DatamapAutoLockAttribute, 1, false));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State,
			datamap, MuiStorePolicyCore.DatamapCopyKeysAttribute, 1, false));
		Assert.True(MuiStorePolicyCore.TryRead(ref platform, State, datamap,
			MuiStorePolicyKind.Datamap, out var policy));
		Assert.Equal((uint)1, policy.AutoLock);
		Assert.Equal((uint)1, policy.CopyKeys);
		Assert.True(MuiStorePolicyCore.AutoLockEnabled(ref platform, State,
			datamap, MuiStorePolicyKind.Datamap));
		Assert.True(MuiStorePolicyCore.CopyKeysEnabled(ref platform, State,
			datamap, MuiStorePolicyKind.Datamap));

		Assert.False(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			datamap, MuiStorePolicyCore.DatamapCopyKeysAttribute, 0));
		Assert.False(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			datamap, MuiStorePolicyCore.DatamapCopyKeysAttribute, out _));
		Assert.False(MuiHeadlessObjectCore.SetAttribute(ref platform, State,
			datamap, MuiStorePolicyCore.ObjectmapCopyKeysAttribute, 1, false));
	}

	[Fact]
	public void DatamapCopyKeysPolicyControlsDispatcherOwnership()
	{
		var platform = CreatePlatform("Datamap.mui", out var classRecord);
		var datamap = MuiCommonControlCore.CreateControl(ref platform, State,
			classRecord, APTR.Null);
		var packet = APTR.FromPointer(0x1200);
		var data = APTR.FromPointer(0x1400);
		var key = APTR.FromPointer(0x1500);
		var lookup = APTR.FromPointer(0x1600);
		platform.WriteUInt8(data, 0, 0x5A);
		platform.WriteCString(key, "alpha");
		platform.WriteCString(lookup, "alpha");
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State,
			datamap, MuiStorePolicyCore.DatamapCopyKeysAttribute, 1, false));
		Assert.True(MuiStoreMessageCore.WriteDatamapSetRecord(ref platform,
			packet, data, 1, key));
		Assert.Equal((uint)1, MuiHeadlessDispatcher.Dispatch(ref platform, State,
			datamap, packet));
		platform.WriteCString(key, "changed");
		Assert.NotEqual(APTR.Null, MuiStoreCore.DatamapFind(ref platform, State,
			datamap, lookup));

		platform.WriteCString(key, "beta");
		platform.WriteCString(lookup, "beta");
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State,
			datamap, MuiStorePolicyCore.DatamapCopyKeysAttribute, 0, false));
		Assert.True(MuiStoreMessageCore.WriteDatamapSetRecord(ref platform,
			packet, data, 1, key));
		Assert.Equal((uint)1, MuiHeadlessDispatcher.Dispatch(ref platform, State,
			datamap, packet));
		platform.WriteCString(key, "mutated");
		Assert.Equal(APTR.Null, MuiStoreCore.DatamapFind(ref platform, State,
			datamap, lookup));
	}

	[Fact]
	public void ObjectmapClassUsesSeparatePolicyAndRejectsDatamapPolicy()
	{
		var platform = CreatePlatform("Objectmap.mui", out var classRecord);
		var objectmap = MuiCommonControlCore.CreateControl(ref platform, State,
			classRecord, APTR.Null);
		Assert.NotEqual(APTR.Null, objectmap);
		Assert.Equal(MuiControlClass.Objectmap,
			MuiCommonControlCore.Classify(ref platform, State, objectmap));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State,
			objectmap, MuiStorePolicyCore.ObjectmapAutoLockAttribute, 1, false));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State,
			objectmap, MuiStorePolicyCore.ObjectmapCopyKeysAttribute, 1, false));
		Assert.True(MuiStorePolicyCore.AutoLockEnabled(ref platform, State,
			objectmap, MuiStorePolicyKind.Objectmap));
		Assert.True(MuiStorePolicyCore.CopyKeysEnabled(ref platform, State,
			objectmap, MuiStorePolicyKind.Objectmap));
		var child = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			classRecord, APTR.Null);
		var packet = APTR.FromPointer(0x1200);
		var key = APTR.FromPointer(0x1500);
		platform.WriteCString(key, "child");
		Assert.True(MuiStoreMessageCore.WriteObjectmapSetRecord(ref platform,
			packet, child, key));
		Assert.Equal((uint)1, MuiHeadlessDispatcher.Dispatch(ref platform, State,
			objectmap, packet));
		Assert.True(MuiStoreCore.ObjectmapRemove(ref platform, State, objectmap,
			key));
		Assert.False(MuiHeadlessObjectCore.SetAttribute(ref platform, State,
			objectmap, MuiStorePolicyCore.DatamapAutoLockAttribute, 1, false));
		Assert.False(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			objectmap, MuiStorePolicyCore.ObjectmapAutoLockAttribute, out _));
		Assert.False(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			objectmap, MuiStorePolicyCore.ObjectmapAutoLockAttribute, 0));
	}

	[Fact]
	public void KnownStoreDispatchRejectsPacketsForAnotherStoreClass()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x8000,
			State);
		var datamapName = APTR.FromPointer(0x1100);
		var dataspaceName = APTR.FromPointer(0x1180);
		platform.WriteCString(datamapName, "Datamap.mui");
		platform.WriteCString(dataspaceName, "Dataspace.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		var datamapClass = MuiHeadlessObjectCore.RegisterClass(ref platform,
			State, datamapName, APTR.Null, 0, APTR.FromPointer(1), false);
		var dataspaceClass = MuiHeadlessObjectCore.RegisterClass(ref platform,
			State, dataspaceName, APTR.Null, 0, APTR.FromPointer(1), false);
		var datamap = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			datamapClass, APTR.Null);
		var dataspace = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			dataspaceClass, APTR.Null);
		var packet = APTR.FromPointer(0x1200);
		var data = APTR.FromPointer(0x1400);
		platform.WriteUInt8(data, 0, 0xA5);
		Assert.True(MuiDataspaceMessageCore.WriteAddRecord(ref platform, packet,
			data, 1, 7));
		Assert.Equal((uint)0, MuiHeadlessDispatcher.Dispatch(ref platform, State,
			datamap, packet));
		Assert.Equal((uint)0, MuiHeadlessDispatcher.DispatchDataspace(
			ref platform, State, datamap, packet));

		var key = APTR.FromPointer(0x1500);
		platform.WriteCString(key, "key");
		Assert.True(MuiStoreMessageCore.WriteDatamapSetRecord(ref platform,
			packet, data, 1, key));
		Assert.Equal((uint)0, MuiHeadlessDispatcher.Dispatch(ref platform, State,
			dataspace, packet));
	}

	[Fact]
	public void DatamapPoolPolicyUsesNamedRecordAndPooledOwnership()
	{
		var platform = CreatePlatform("Datamap.mui", out var classRecord);
		var pool = platform.CreatePool(0, 2008, 1024);
		Assert.NotEqual(APTR.Null, pool);
		var tags = APTR.FromPointer(0x1200);
		Assert.True(MuiAslTagItemCodec.Write(ref platform, tags,
			new MuiAslTagItemRecord
			{
				Tag = MuiStorePolicyCore.DatamapPoolAttribute,
				Data = pool.Raw,
			}));
		Assert.True(MuiAslTagItemCodec.Write(ref platform,
			APTR.FromPointer(tags.Raw + MuiAslTagItemRecord.Size),
			new MuiAslTagItemRecord
			{
				Tag = MuiStorePolicyCore.DatamapCopyKeysAttribute,
				Data = 1,
			}));
		Assert.True(MuiAslTagItemCodec.Write(ref platform,
			APTR.FromPointer(tags.Raw + MuiAslTagItemRecord.Size * 2u),
			new MuiAslTagItemRecord { Tag = MuiAslTagListCore.TagDone }));
		var datamap = MuiCommonControlCore.CreateControl(ref platform, State,
			classRecord, tags);
		Assert.NotEqual(APTR.Null, datamap);
		Assert.True(MuiStorePolicyCore.TryReadPool(ref platform, State, datamap,
			MuiStorePolicyKind.Datamap, out var poolPolicy));
		Assert.Equal(pool.Raw, poolPolicy.Pool.Raw);
		Assert.Equal((uint)1, poolPolicy.UsesExternalPool);
		Assert.False(MuiCommonControlCore.SetControlAttribute(ref platform, State,
			datamap, MuiStorePolicyCore.DatamapPoolAttribute, pool.Raw));
		Assert.False(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			datamap, MuiStorePolicyCore.DatamapPoolAttribute, out _));

		var data = APTR.FromPointer(0x1400);
		var key = APTR.FromPointer(0x1500);
		platform.WriteUInt8(data, 0, 0x5A);
		platform.WriteCString(key, "pooled");
		var pooledBefore = platform.PooledAllocationCount;
		Assert.True(MuiStoreCore.DatamapSet(ref platform, State, datamap, key,
			data, 1, true));
		Assert.True(platform.PooledAllocationCount >= pooledBefore + 3);
		Assert.NotEqual(APTR.Null, MuiStoreCore.DatamapFind(ref platform, State,
			datamap, key));

		var pooledFreesBefore = platform.PooledFreeCount;
		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State,
			datamap));
		Assert.True(platform.PooledFreeCount >= pooledFreesBefore + 3);
		platform.DeletePool(pool);
		Assert.Equal((uint)1, platform.PoolDeleteCount);
	}

	[Fact]
	public void StorePoolAttributesAreClassSpecificAndGetterless()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x8000,
			State);
		var datamapName = APTR.FromPointer(0x1100);
		var dataspaceName = APTR.FromPointer(0x1180);
		var objectmapName = APTR.FromPointer(0x1200);
		platform.WriteCString(datamapName, "Datamap.mui");
		platform.WriteCString(dataspaceName, "Dataspace.mui");
		platform.WriteCString(objectmapName, "Objectmap.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		var datamapClass = MuiHeadlessObjectCore.RegisterClass(ref platform,
			State, datamapName, APTR.Null, 0, APTR.FromPointer(1), false);
		var dataspaceClass = MuiHeadlessObjectCore.RegisterClass(ref platform,
			State, dataspaceName, APTR.Null, 0, APTR.FromPointer(1), false);
		var objectmapClass = MuiHeadlessObjectCore.RegisterClass(ref platform,
			State, objectmapName, APTR.Null, 0, APTR.FromPointer(1), false);
		var datamap = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			datamapClass, APTR.Null);
		var dataspace = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			dataspaceClass, APTR.Null);
		var objectmap = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			objectmapClass, APTR.Null);
		var pool = platform.CreatePool(0, 2008, 1024);
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State,
			datamap, MuiStorePolicyCore.DatamapPoolAttribute, pool.Raw, false));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State,
			dataspace, MuiStorePolicyCore.DataspacePoolAttribute, pool.Raw, false));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State,
			objectmap, MuiStorePolicyCore.ObjectmapPoolAttribute, pool.Raw, false));
		Assert.False(MuiHeadlessObjectCore.SetAttribute(ref platform, State,
			datamap, MuiStorePolicyCore.ObjectmapPoolAttribute, pool.Raw, false));
		Assert.False(MuiHeadlessObjectCore.SetAttribute(ref platform, State,
			dataspace, MuiStorePolicyCore.DatamapPoolAttribute, pool.Raw, false));
		Assert.False(MuiHeadlessObjectCore.SetAttribute(ref platform, State,
			objectmap, MuiStorePolicyCore.DataspacePoolAttribute, pool.Raw, false));
		Assert.False(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			dataspace, MuiStorePolicyCore.DataspacePoolAttribute, out _));
		Assert.True(MuiStorePolicyCore.TryReadPool(ref platform, State, dataspace,
			MuiStorePolicyKind.Dataspace, out var policy));
		Assert.Equal(pool.Raw, policy.Pool.Raw);
	}

	[Fact]
	public void ObjectmapOwnsReplacedValuesAndRemoveTransfersOwnership()
	{
		var platform = CreatePlatform("Objectmap.mui", out var classRecord);
		var objectmap = MuiCommonControlCore.CreateControl(ref platform, State,
			classRecord, APTR.Null);
		var oldValue = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			classRecord, APTR.Null);
		var replacement = MuiHeadlessObjectCore.CreateObjectA(ref platform,
			State, classRecord, APTR.Null);
		var key = APTR.FromPointer(0x1500);
		platform.WriteCString(key, "owned");

		Assert.True(MuiStoreCore.ObjectmapSet(ref platform, State, objectmap,
			key, oldValue));
		Assert.Equal(oldValue, MuiStoreCore.ObjectmapFind(ref platform, State,
			objectmap, key));
		Assert.True(MuiStoreCore.ObjectmapSet(ref platform, State, objectmap,
			key, replacement));
		Assert.Equal(APTR.Null, MuiHeadlessObjectCore.FindObject(ref platform,
			State, oldValue));
		Assert.Equal(replacement, MuiStoreCore.ObjectmapFind(ref platform, State,
			objectmap, key));

		var removed = MuiStoreCore.ObjectmapRemoveObject(ref platform, State,
			objectmap, key);
		Assert.Equal(replacement, removed);
		Assert.Equal(APTR.Null, MuiStoreCore.ObjectmapFind(ref platform, State,
			objectmap, key));
		Assert.NotEqual(APTR.Null, MuiHeadlessObjectCore.FindObject(ref platform,
			State, replacement));
		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State,
			replacement));
		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State,
			objectmap));
	}

	[Fact]
	public void ObjectmapClearDisposesAllNamedOwnedValues()
	{
		var platform = CreatePlatform("Objectmap.mui", out var classRecord);
		var objectmap = MuiCommonControlCore.CreateControl(ref platform, State,
			classRecord, APTR.Null);
		var first = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			classRecord, APTR.Null);
		var second = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			classRecord, APTR.Null);
		var firstKey = APTR.FromPointer(0x1500);
		var secondKey = APTR.FromPointer(0x1580);
		platform.WriteCString(firstKey, "first");
		platform.WriteCString(secondKey, "second");
		Assert.True(MuiStoreCore.ObjectmapSet(ref platform, State, objectmap,
			firstKey, first));
		Assert.True(MuiStoreCore.ObjectmapSet(ref platform, State, objectmap,
			secondKey, second));

		Assert.Equal((uint)2, MuiStoreCore.ObjectmapClear(ref platform, State,
			objectmap));
		Assert.Equal(APTR.Null, MuiHeadlessObjectCore.FindObject(ref platform,
			State, first));
		Assert.Equal(APTR.Null, MuiHeadlessObjectCore.FindObject(ref platform,
			State, second));
		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State,
			objectmap));
	}

	[Fact]
	public void ObjectmapCopyKeysUsesNamedStringStorage()
	{
		var platform = CreatePlatform("Objectmap.mui", out var classRecord);
		var objectmap = MuiCommonControlCore.CreateControl(ref platform, State,
			classRecord, APTR.Null);
		var key = APTR.FromPointer(0x1500);
		var lookup = APTR.FromPointer(0x1580);
		var counter = APTR.FromPointer(0x1600);
		var value = APTR.FromPointer(0x1680);
		platform.WriteCString(key, "first");
		platform.WriteCString(lookup, "first");
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State,
			objectmap, MuiStorePolicyCore.ObjectmapCopyKeysAttribute, 1, false));
		Assert.True(MuiStoreCore.ObjectmapSet(ref platform, State, objectmap,
			key, value, true));
		platform.WriteCString(key, "changed");
		Assert.Equal(value, MuiStoreCore.ObjectmapFind(ref platform, State,
			objectmap, lookup));
		platform.WriteUInt32(counter, 0, 0);
		Assert.Equal(value, MuiStoreCore.ObjectmapIterate(ref platform, State,
			objectmap, counter));
		var copiedKey = MuiStoreCore.ObjectmapIterationKey(ref platform, State,
			objectmap, counter);
		Assert.NotEqual(key, copiedKey);
		Assert.Equal((byte)'f', platform.ReadUInt8(copiedKey, 0));
		Assert.Equal(value, MuiStoreCore.ObjectmapRemoveObject(ref platform,
			State, objectmap, lookup));
		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State,
			objectmap));
	}

	[Fact]
	public void StringStoresIterateInMorphosKeyOrderAndExposeCurrentKey()
	{
		var platform = CreatePlatform("Datamap.mui", out var classRecord);
		var datamap = MuiCommonControlCore.CreateControl(ref platform, State,
			classRecord, APTR.Null);
		var firstKey = APTR.FromPointer(0x1400);
		var secondKey = APTR.FromPointer(0x1480);
		var thirdKey = APTR.FromPointer(0x1500);
		platform.WriteCString(firstKey, "zulu");
		platform.WriteCString(secondKey, "alpha");
		platform.WriteCString(thirdKey, "middle");
		var firstData = APTR.FromPointer(0x1580);
		var secondData = APTR.FromPointer(0x1600);
		var thirdData = APTR.FromPointer(0x1680);
		platform.WriteUInt8(firstData, 0, 1);
		platform.WriteUInt8(secondData, 0, 2);
		platform.WriteUInt8(thirdData, 0, 3);
		Assert.True(MuiStoreCore.DatamapSet(ref platform, State, datamap,
			firstKey, firstData, 1, false));
		Assert.True(MuiStoreCore.DatamapSet(ref platform, State, datamap,
			secondKey, secondData, 1, false));
		Assert.True(MuiStoreCore.DatamapSet(ref platform, State, datamap,
			thirdKey, thirdData, 1, false));
		var counter = APTR.FromPointer(0x1700);
		platform.WriteUInt32(counter, 0, 0);
		var iterated = MuiStoreCore.DatamapIterate(ref platform, State, datamap,
			counter);
		Assert.Equal((byte)2, platform.ReadUInt8(iterated, 0));
		Assert.Equal(secondKey, MuiStoreCore.DatamapIterationKey(ref platform,
			State, datamap, counter));
		iterated = MuiStoreCore.DatamapIterate(ref platform, State, datamap,
			counter);
		Assert.Equal((byte)3, platform.ReadUInt8(iterated, 0));
		Assert.Equal(thirdKey, MuiStoreCore.DatamapIterationKey(ref platform,
			State, datamap, counter));
		iterated = MuiStoreCore.DatamapIterate(ref platform, State, datamap,
			counter);
		Assert.Equal((byte)1, platform.ReadUInt8(iterated, 0));
		Assert.Equal(firstKey, MuiStoreCore.DatamapIterationKey(ref platform,
			State, datamap, counter));
		Assert.Equal(APTR.Null, MuiStoreCore.DatamapIterate(ref platform, State,
			datamap, counter));
		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State,
			datamap));

		platform = CreatePlatform("Objectmap.mui", out classRecord);
		var objectmap = MuiCommonControlCore.CreateControl(ref platform, State,
			classRecord, APTR.Null);
		platform.WriteCString(firstKey, "zulu");
		platform.WriteCString(secondKey, "alpha");
		platform.WriteCString(thirdKey, "middle");
		var valueA = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			classRecord, APTR.Null);
		var valueB = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			classRecord, APTR.Null);
		var valueC = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			classRecord, APTR.Null);
		Assert.True(MuiStoreCore.ObjectmapSet(ref platform, State, objectmap,
			firstKey, valueA));
		Assert.True(MuiStoreCore.ObjectmapSet(ref platform, State, objectmap,
			secondKey, valueB));
		Assert.True(MuiStoreCore.ObjectmapSet(ref platform, State, objectmap,
			thirdKey, valueC));
		counter = APTR.FromPointer(0x1780);
		platform.WriteUInt32(counter, 0, 0);
		Assert.Equal(valueB, MuiStoreCore.ObjectmapIterate(ref platform, State,
			objectmap, counter));
		Assert.Equal(secondKey, MuiStoreCore.ObjectmapIterationKey(ref platform,
			State, objectmap, counter));
		Assert.Equal(valueC, MuiStoreCore.ObjectmapIterate(ref platform, State,
			objectmap, counter));
		Assert.Equal(thirdKey, MuiStoreCore.ObjectmapIterationKey(ref platform,
			State, objectmap, counter));
		Assert.Equal(valueA, MuiStoreCore.ObjectmapIterate(ref platform, State,
			objectmap, counter));
		Assert.Equal(firstKey, MuiStoreCore.ObjectmapIterationKey(ref platform,
			State, objectmap, counter));
		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State,
			objectmap));
	}

	[Fact]
	public void StringMapIterationContinuesAfterRemovingCurrentEntry()
	{
		var platform = CreatePlatform("Datamap.mui", out var classRecord);
		var datamap = MuiCommonControlCore.CreateControl(ref platform, State,
			classRecord, APTR.Null);
		var alpha = APTR.FromPointer(0x1800);
		var middle = APTR.FromPointer(0x1880);
		var zulu = APTR.FromPointer(0x1900);
		platform.WriteCString(alpha, "alpha");
		platform.WriteCString(middle, "middle");
		platform.WriteCString(zulu, "zulu");
		var alphaData = APTR.FromPointer(0x1980);
		var middleData = APTR.FromPointer(0x1A00);
		var zuluData = APTR.FromPointer(0x1A80);
		platform.WriteUInt8(alphaData, 0, 1);
		platform.WriteUInt8(middleData, 0, 2);
		platform.WriteUInt8(zuluData, 0, 3);
		Assert.True(MuiStoreCore.DatamapSet(ref platform, State, datamap,
			zulu, zuluData, 1, false));
		Assert.True(MuiStoreCore.DatamapSet(ref platform, State, datamap,
			alpha, alphaData, 1, false));
		Assert.True(MuiStoreCore.DatamapSet(ref platform, State, datamap,
			middle, middleData, 1, false));
		var counter = APTR.FromPointer(0x1B00);
		platform.WriteUInt32(counter, 0, 0);
		var iterated = MuiStoreCore.DatamapIterate(ref platform, State, datamap,
			counter);
		Assert.Equal((byte)1, platform.ReadUInt8(iterated, 0));
		Assert.Equal(alpha, MuiStoreCore.DatamapIterationKey(ref platform, State,
			datamap, counter));
		Assert.True(MuiStoreCore.DatamapRemove(ref platform, State, datamap,
			alpha));
		iterated = MuiStoreCore.DatamapIterate(ref platform, State, datamap,
			counter);
		Assert.Equal((byte)2, platform.ReadUInt8(iterated, 0));
		Assert.Equal(middle, MuiStoreCore.DatamapIterationKey(ref platform, State,
			datamap, counter));
		Assert.True(MuiStoreCore.DatamapRemove(ref platform, State, datamap,
			middle));
		iterated = MuiStoreCore.DatamapIterate(ref platform, State, datamap,
			counter);
		Assert.Equal((byte)3, platform.ReadUInt8(iterated, 0));
		Assert.Equal(zulu, MuiStoreCore.DatamapIterationKey(ref platform, State,
			datamap, counter));
		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State,
			datamap));

		platform = CreatePlatform("Objectmap.mui", out classRecord);
		var objectmap = MuiCommonControlCore.CreateControl(ref platform, State,
			classRecord, APTR.Null);
		platform.WriteCString(alpha, "alpha");
		platform.WriteCString(middle, "middle");
		platform.WriteCString(zulu, "zulu");
		var alphaObject = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			classRecord, APTR.Null);
		var middleObject = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			classRecord, APTR.Null);
		var zuluObject = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			classRecord, APTR.Null);
		Assert.True(MuiStoreCore.ObjectmapSet(ref platform, State, objectmap,
			zulu, zuluObject));
		Assert.True(MuiStoreCore.ObjectmapSet(ref platform, State, objectmap,
			alpha, alphaObject));
		Assert.True(MuiStoreCore.ObjectmapSet(ref platform, State, objectmap,
			middle, middleObject));
		counter = APTR.FromPointer(0x1B00);
		platform.WriteUInt32(counter, 0, 0);
		Assert.Equal(alphaObject, MuiStoreCore.ObjectmapIterate(ref platform,
			State, objectmap, counter));
		Assert.Equal(alpha, MuiStoreCore.ObjectmapIterationKey(ref platform, State,
			objectmap, counter));
		Assert.Equal(alphaObject, MuiStoreCore.ObjectmapRemoveObject(ref platform,
			State, objectmap, alpha));
		Assert.Equal(middleObject, MuiStoreCore.ObjectmapIterate(ref platform,
			State, objectmap, counter));
		Assert.Equal(middle, MuiStoreCore.ObjectmapIterationKey(ref platform, State,
			objectmap, counter));
		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State,
			alphaObject));
		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State,
			objectmap));
	}

	[Fact]
	public void DatamapWithoutPoolCreatesAndReleasesNamedOwnedPool()
	{
		var platform = CreatePlatform("Datamap.mui", out var classRecord);
		var createsBefore = platform.PoolCreateCount;
		var datamap = MuiCommonControlCore.CreateControl(ref platform, State,
			classRecord, APTR.Null);
		Assert.NotEqual(APTR.Null, datamap);
		Assert.Equal(createsBefore + 1, platform.PoolCreateCount);
		var data = APTR.FromPointer(0x1400);
		var key = APTR.FromPointer(0x1500);
		platform.WriteUInt8(data, 0, 0x5A);
		platform.WriteCString(key, "default-pool");
		Assert.True(MuiStoreCore.DatamapSet(ref platform, State, datamap, key,
			data, 1, true));
		Assert.Equal(createsBefore + 1, platform.PoolCreateCount);
		Assert.True(MuiStorePolicyCore.TryReadOwnedPoolState(ref platform, State,
			datamap, MuiStorePolicyKind.Datamap, out var stateAddress,
			out var poolState));
		Assert.Equal((uint)1, poolState.OwnsPool);
		Assert.Equal(MuiStorePoolStateRecord.MagicValue, poolState.Magic);
		Assert.True(platform.PooledAllocationCount >= 3);
		var deletesBefore = platform.PoolDeleteCount;
		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State,
			datamap));
		Assert.Equal(deletesBefore + 1, platform.PoolDeleteCount);
		Assert.Equal(0u, platform.ReadUInt32(stateAddress, 0));
	}

	[Fact]
	public void DataspaceAndObjectmapWithoutPoolUseIndependentOwnedPools()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x8000,
			State);
		var dataspaceName = APTR.FromPointer(0x1100);
		var objectmapName = APTR.FromPointer(0x1180);
		platform.WriteCString(dataspaceName, "Dataspace.mui");
		platform.WriteCString(objectmapName, "Objectmap.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		var dataspaceClass = MuiHeadlessObjectCore.RegisterClass(ref platform,
			State, dataspaceName, APTR.Null, 0, APTR.FromPointer(1), false);
		var objectmapClass = MuiHeadlessObjectCore.RegisterClass(ref platform,
			State, objectmapName, APTR.Null, 0, APTR.FromPointer(1), false);
		var dataspace = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			dataspaceClass, APTR.Null);
		var objectmap = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			objectmapClass, APTR.Null);
		var data = APTR.FromPointer(0x1400);
		var key = APTR.FromPointer(0x1500);
		platform.WriteUInt8(data, 0, 0x33);
		platform.WriteCString(key, "object");
		Assert.True(MuiStoreCore.DataspaceAdd(ref platform, State, dataspace,
			7, data, 1));
		Assert.True(MuiStoreCore.ObjectmapSet(ref platform, State, objectmap,
			key, APTR.FromPointer(0x1700)));
		Assert.Equal((uint)2, platform.PoolCreateCount);
		Assert.True(MuiStorePolicyCore.TryReadOwnedPoolState(ref platform, State,
			dataspace, MuiStorePolicyKind.Dataspace, out _, out _));
		Assert.True(MuiStorePolicyCore.TryReadOwnedPoolState(ref platform, State,
			objectmap, MuiStorePolicyKind.Objectmap, out _, out _));
		var deletesBefore = platform.PoolDeleteCount;
		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State,
			dataspace));
		Assert.True(MuiHeadlessObjectCore.DisposeObject(ref platform, State,
			objectmap));
		Assert.Equal(deletesBefore + 2, platform.PoolDeleteCount);
	}

	private static MuiHeadlessTestPlatform CreatePlatform(string name,
		out APTR classRecord)
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x8000,
			State);
		var className = APTR.FromPointer(0x1100);
		platform.WriteCString(className, name);
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		classRecord = MuiHeadlessObjectCore.RegisterClass(ref platform, State,
			className, APTR.Null, 0, APTR.FromPointer(1), false);
		Assert.NotEqual(APTR.Null, classRecord);
		return platform;
	}
}
