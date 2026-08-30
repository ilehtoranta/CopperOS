using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiDataspaceCountTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);

	[Fact]
	public void DataspaceCountProjectsNumericStoreRecords()
	{
		var platform = CreatePlatform(out var dataspaceClass);
		var dataspace = MuiCommonControlCore.CreateControl(ref platform, State,
			dataspaceClass, APTR.Null);
		Assert.True(dataspace.IsNotNull);
		Assert.Equal((uint)0, GetCount(ref platform, dataspace));

		var data = APTR.FromPointer(0x1800);
		platform.WriteUInt8(data, 0, 0x11);
		platform.WriteUInt8(data, 1, 0x22);
		platform.WriteUInt8(data, 2, 0x33);
		Assert.True(MuiStoreCore.DataspaceAdd(ref platform, State, dataspace,
			1, data, 3));
		Assert.True(MuiStoreCore.DataspaceAdd(ref platform, State, dataspace,
			2, data, 0));

		Assert.Equal((uint)2, GetCount(ref platform, dataspace));
		Assert.True(MuiStoreCore.DataspaceRemove(ref platform, State, dataspace,
			1));
		Assert.Equal((uint)1, GetCount(ref platform, dataspace));
		Assert.Equal((uint)1, MuiStoreCore.DataspaceCount(ref platform, State,
			dataspace));
		Assert.Equal((uint)1, MuiStoreCore.DataspaceClear(ref platform, State,
			dataspace));
		Assert.Equal((uint)0, GetCount(ref platform, dataspace));
	}

	[Fact]
	public void DataspaceCountIsGetterOnlyAndClassGated()
	{
		var platform = CreatePlatform(out var dataspaceClass);
		var dataspace = MuiCommonControlCore.CreateControl(ref platform, State,
			dataspaceClass, APTR.Null);
		Assert.True(dataspace.IsNotNull);
		Assert.False(MuiCommonControlCore.SetControlAttribute(ref platform,
			State, dataspace, MuiCommonControlCore.DataspaceCount, 99));
		Assert.False(MuiHeadlessObjectCore.GetRawAttribute(ref platform, State,
			dataspace, MuiCommonControlCore.DataspaceCount, out _));

		var unknownName = APTR.FromPointer(0x1200);
		platform.WriteCString(unknownName, "unknown.mui");
		var unknownClass = MuiHeadlessObjectCore.RegisterClass(ref platform,
			State, unknownName, APTR.Null, 0, APTR.FromPointer(1), false);
		var unknown = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			unknownClass, APTR.Null);
		Assert.True(unknown.IsNotNull);
		Assert.False(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			unknown, MuiCommonControlCore.DataspaceCount, out _));
	}

	[Fact]
	public void DataspaceCountFailsClosedForMalformedStoreLink()
	{
		var platform = CreatePlatform(out var dataspaceClass);
		var dataspace = MuiCommonControlCore.CreateControl(ref platform, State,
			dataspaceClass, APTR.Null);
		Assert.True(dataspace.IsNotNull);
		var data = APTR.FromPointer(0x1900);
		platform.WriteUInt8(data, 0, 0xA5);
		Assert.True(MuiStoreCore.DataspaceAdd(ref platform, State, dataspace,
			7, data, 1));

		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			dataspace, MuiCommonControlCore.DataspaceCount, out var before));
		Assert.Equal((uint)1, before);
		var owner = MuiHeadlessObjectCore.FindObject(ref platform, State,
			dataspace);
		Assert.True(MuiHeadlessObjectCodec.TryRead(ref platform, owner,
			out var ownerRecord));
		Assert.True(MuiStoreRecordCodec.TryRead(ref platform, ownerRecord.Stores,
			out var storeRecord));
		storeRecord.Next = APTR.FromPointer(0x2);
		Assert.True(MuiStoreRecordCodec.Write(ref platform, ownerRecord.Stores,
			storeRecord));

		Assert.False(MuiStoreCore.TryGetDataspaceCount(ref platform, State,
			dataspace, out _));
		Assert.False(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			dataspace, MuiCommonControlCore.DataspaceCount, out _));
	}

	[Fact]
	public void DatamapCountProjectsStringKeyRecords()
	{
		var platform = CreateDatamapPlatform(out var datamapClass);
		var datamap = MuiCommonControlCore.CreateControl(ref platform, State,
			datamapClass, APTR.Null);
		Assert.True(datamap.IsNotNull);
		var key = APTR.FromPointer(0x1800);
		var data = APTR.FromPointer(0x1900);
		platform.WriteCString(key, "alpha");
		platform.WriteUInt8(data, 0, 0x5A);
		Assert.True(MuiStoreCore.DatamapSet(ref platform, State, datamap, key,
			data, 1, true));
		Assert.Equal((uint)1, GetDatamapCount(ref platform, datamap));
		platform.WriteCString(key, "beta");
		Assert.True(MuiStoreCore.DatamapSet(ref platform, State, datamap, key,
			data, 1, true));
		Assert.Equal((uint)2, GetDatamapCount(ref platform, datamap));
		Assert.False(MuiCommonControlCore.SetControlAttribute(ref platform,
			State, datamap, MuiCommonControlCore.DatamapCount, 7));
		Assert.True(MuiStoreCore.DatamapRemove(ref platform, State, datamap,
			key));
		Assert.Equal((uint)1, GetDatamapCount(ref platform, datamap));
	}

	private static uint GetCount(ref MuiHeadlessTestPlatform platform,
		APTR dataspace)
	{
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			dataspace, MuiCommonControlCore.DataspaceCount, out var count));
		return count;
	}

	private static uint GetDatamapCount(ref MuiHeadlessTestPlatform platform,
		APTR datamap)
	{
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State,
			datamap, MuiCommonControlCore.DatamapCount, out var count));
		return count;
	}

	private static MuiHeadlessTestPlatform CreatePlatform(out APTR dataspaceClass)
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x8000,
			State);
		var className = APTR.FromPointer(0x1100);
		platform.WriteCString(className, "Dataspace.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		dataspaceClass = MuiHeadlessObjectCore.RegisterClass(ref platform,
			State, className, APTR.Null, 0, APTR.FromPointer(1), false);
		return platform;
	}

	private static MuiHeadlessTestPlatform CreateDatamapPlatform(
		out APTR datamapClass)
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x8000,
			State);
		var className = APTR.FromPointer(0x1100);
		platform.WriteCString(className, "Datamap.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		datamapClass = MuiHeadlessObjectCore.RegisterClass(ref platform,
			State, className, APTR.Null, 0, APTR.FromPointer(1), false);
		return platform;
	}
}
