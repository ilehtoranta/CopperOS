using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiUserDataTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);
	private const uint FindUData = 0x8042C196;
	private const uint GetUData = 0x8042ED0C;
	private const uint SetUData = 0x8042C920;
	private const uint SetUDataOnce = 0x8042CA19;
	private const uint UserData = 0x80420313;
	private const uint ValueAttribute = 0x80420020;

	[Fact]
	public void UserDataPacketCodecsUseCompleteNamedRecords()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x7000, 0x2000,
			State);
		var packet = APTR.FromPointer(0x1700);
		var findExpected = new MuiFindUDataMessage
		{
			MethodId = FindUData,
			UserData = 0x22,
		};
		Assert.True(MuiFindUDataMessageCodec.Write(ref platform, packet,
			findExpected));
		Assert.True(MuiFindUDataMessageCodec.TryRead(ref platform, packet,
			out var findActual));
		Assert.Equal(findExpected.MethodId, findActual.MethodId);
		Assert.Equal(findExpected.UserData, findActual.UserData);

		var getExpected = new MuiGetUDataMessage
		{
			MethodId = GetUData,
			UserData = 0x22,
			Attribute = ValueAttribute,
			Storage = 0x1710,
		};
		Assert.True(MuiGetUDataMessageCodec.Write(ref platform, packet,
			getExpected));
		Assert.True(MuiGetUDataMessageCodec.TryRead(ref platform, packet,
			out var getActual));
		Assert.Equal(getExpected.MethodId, getActual.MethodId);
		Assert.Equal(getExpected.UserData, getActual.UserData);
		Assert.Equal(getExpected.Attribute, getActual.Attribute);
		Assert.Equal(getExpected.Storage, getActual.Storage);

		var setExpected = new MuiSetUDataMessage
		{
			MethodId = SetUData,
			UserData = 0x22,
			Attribute = ValueAttribute,
			Value = 0xCAFEBABEu,
		};
		Assert.True(MuiSetUDataMessageCodec.Write(ref platform, packet,
			setExpected));
		Assert.True(MuiSetUDataMessageCodec.TryRead(ref platform, packet,
			out var setActual));
		Assert.Equal(setExpected.MethodId, setActual.MethodId);
		Assert.Equal(setExpected.UserData, setActual.UserData);
		Assert.Equal(setExpected.Attribute, setActual.Attribute);
		Assert.Equal(setExpected.Value, setActual.Value);

		Assert.False(MuiFindUDataMessageCodec.TryRead(ref platform,
			APTR.FromPointer(0x7FFC), out _));
		Assert.False(MuiGetUDataMessageCodec.TryRead(ref platform,
			APTR.FromPointer(0x7FFC), out _));
		Assert.False(MuiSetUDataMessageCodec.TryRead(ref platform,
			APTR.FromPointer(0x7FFC), out _));
		Assert.False(MuiFindUDataMessageCodec.Write(ref platform,
			APTR.FromPointer(0x7FFC), findExpected));
		Assert.False(MuiGetUDataMessageCodec.Write(ref platform,
			APTR.FromPointer(0x7FFC), getExpected));
		Assert.False(MuiSetUDataMessageCodec.Write(ref platform,
			APTR.FromPointer(0x7FFC), setExpected));
	}

	[Fact]
	public void FindAndGetUDataWalkTheObjectTreeInPreorder()
	{
		var platform = CreatePlatform(out var root, out var first, out var second,
			out var nested);
		SetUserData(ref platform, first, 0x10);
		SetUserData(ref platform, nested, 0x22);
		SetUserData(ref platform, second, 0x22);
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, root, first));
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, root, second));
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, first, nested));
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, nested,
			ValueAttribute, 0xCAFEBABEu, false));

		var packet = APTR.FromPointer(0x1700);
		Assert.True(MuiFindUDataMessageCodec.Write(ref platform, packet,
			new MuiFindUDataMessage
			{
				MethodId = FindUData,
				UserData = 0x22,
			}));
		Assert.Equal(nested.Raw, MuiHeadlessDispatcher.Dispatch(ref platform, State,
			root, packet));

		var storage = APTR.FromPointer(0x1710);
		Assert.True(MuiGetUDataMessageCodec.Write(ref platform, packet,
			new MuiGetUDataMessage
			{
				MethodId = GetUData,
				UserData = 0x22,
				Attribute = ValueAttribute,
				Storage = storage.Raw,
			}));
		platform.WriteUInt32(storage, 0, 0xDEADBEEFu);
		Assert.Equal(1u, MuiHeadlessDispatcher.Dispatch(ref platform, State, root,
			packet));
		Assert.Equal(0xCAFEBABEu, platform.ReadUInt32(storage, 0));
	}

	[Fact]
	public void SetUDataUpdatesEveryMatchAndOnceStopsAtFirstMatch()
	{
		var platform = CreatePlatform(out var root, out var first, out var second,
			out var nested);
		SetUserData(ref platform, root, 0x77);
		SetUserData(ref platform, first, 0x77);
		SetUserData(ref platform, second, 0x77);
		SetUserData(ref platform, nested, 0x77);
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, root, first));
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, root, second));
		Assert.True(MuiFamilyCore.AddTail(ref platform, State, first, nested));

		var packet = APTR.FromPointer(0x1700);
		Assert.True(MuiSetUDataMessageCodec.Write(ref platform, packet,
			new MuiSetUDataMessage
			{
				MethodId = SetUData,
				UserData = 0x77,
				Attribute = ValueAttribute,
				Value = 0x1111,
			}));
		Assert.Equal(1u, MuiHeadlessDispatcher.Dispatch(ref platform, State, root,
			packet));
		Assert.Equal(0x1111u, GetAttribute(ref platform, root));
		Assert.Equal(0x1111u, GetAttribute(ref platform, nested));
		Assert.Equal(0x1111u, GetAttribute(ref platform, second));

		Assert.True(MuiSetUDataMessageCodec.Write(ref platform, packet,
			new MuiSetUDataMessage
			{
				MethodId = SetUDataOnce,
				UserData = 0x77,
				Attribute = ValueAttribute,
				Value = 0x2222,
			}));
		Assert.Equal(1u, MuiHeadlessDispatcher.Dispatch(ref platform, State, root,
			packet));
		Assert.Equal(0x2222u, GetAttribute(ref platform, root));
		Assert.Equal(0x1111u, GetAttribute(ref platform, first));
		Assert.Equal(0x1111u, GetAttribute(ref platform, nested));
	}

	[Fact]
	public void UserDataMethodsRejectMalformedPacketsWithoutChangingState()
	{
		var platform = CreatePlatform(out var root, out _, out _, out _);
		var packet = APTR.FromPointer(0x1700);
		Assert.True(MuiGetUDataMessageCodec.Write(ref platform, packet,
			new MuiGetUDataMessage
			{
				MethodId = GetUData,
				UserData = 1,
				Attribute = ValueAttribute,
				Storage = 0x5FFF,
			}));
		Assert.Equal(0u, MuiHeadlessDispatcher.Dispatch(ref platform, State, root,
			packet));

		platform.WriteUInt32(packet, 0, FindUData);
		Assert.Equal(0u, MuiHeadlessDispatcher.Dispatch(ref platform, State, root,
			APTR.FromPointer(0x5FFE)));
	}

	[Fact]
	public void UserDataTraversalFrameCodecUsesNamedPointerState()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x7000, 0x2000,
			State);
		var address = APTR.FromPointer(0x1800);
		var expected = new MuiUDataTraversalFrame
		{
			Object = APTR.FromPointer(0x2400),
			NextChild = 5
		};

		Assert.True(MuiNotifyUserDataRecords.WriteFrame(ref platform, address,
			expected));
		var actual = default(MuiUDataTraversalFrame);
		Assert.True(MuiNotifyUserDataRecords.TryReadFrame(ref platform, address,
			ref actual));
		Assert.Equal(expected.Object, actual.Object);
		Assert.Equal(expected.NextChild, actual.NextChild);
		Assert.False(MuiNotifyUserDataRecords.TryReadFrame(ref platform,
			APTR.Null, ref actual));
	}

	[Fact]
	public void UserDataTraversalFrameUsesNamedCursorBoundary()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x7000, 0x2000,
			State);
		var cursor = new MuiUDataTraversalCursor
		{
			Base = APTR.FromPointer(0x1800),
			Index = 2,
		};

		Assert.True(MuiUDataTraversalFrameCodec.TryGetEntry(ref platform,
			cursor, out var address));
		Assert.Equal(APTR.FromPointer(0x1810), address);
		cursor.Base = APTR.FromPointer(0x7FFC);
		cursor.Index = 0;
		Assert.False(MuiUDataTraversalFrameCodec.TryGetEntry(ref platform,
			cursor, out _));
	}

	[Fact]
	public void UserDataTraversalFrameVectorMemoryAdapterOwnsEntryBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x7000, 0x2000,
			State);
		var vector = APTR.FromPointer(0x1800);
		Assert.Equal(8u, MuiUDataTraversalFrame.Size);
		Assert.True(MuiUDataTraversalFrameVectorMemoryCodec.TryGetEntry(
			ref platform, vector, 255, out var address));
		Assert.Equal(APTR.FromPointer(0x1FF8), address);
		Assert.False(MuiUDataTraversalFrameVectorMemoryCodec.TryGetEntry(
			ref platform, vector, MuiUDataTraversalCursor.MaximumEntries, out _));
		Assert.False(MuiUDataTraversalFrameVectorMemoryCodec.TryGetEntry(
			ref platform, APTR.FromPointer(0x7FF9), 0, out _));
		Assert.False(MuiUDataTraversalFrameVectorMemoryCodec.TryGetEntry(
			ref platform, APTR.FromPointer(0xFFFFFFFF), 1, out _));
	}

	[Fact]
	public void UserDataTraversalFrameVectorBridgeUsesNamedRecords()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x7000, 0x2000,
			State);
		var cursor = new MuiUDataTraversalCursor
		{
			Base = APTR.FromPointer(0x1800),
			Index = 2,
		};
		var expected = new MuiUDataTraversalFrame
		{
			Object = APTR.FromPointer(0xFEDCBA98u),
			NextChild = 0x10203040u,
		};

		Assert.True(MuiUDataTraversalFrameCodec.TryWrite(ref platform, cursor,
			expected));
		Assert.True(MuiUDataTraversalFrameCodec.TryRead(ref platform, cursor,
			out var actual));
		Assert.Equal(expected.Object, actual.Object);
		Assert.Equal(expected.NextChild, actual.NextChild);

		cursor.Base = APTR.FromPointer(0x7FF9);
		Assert.False(MuiUDataTraversalFrameCodec.TryRead(ref platform, cursor,
			out _));
		cursor.Base = APTR.FromPointer(0xFFFFFFFFu);
		Assert.False(MuiUDataTraversalFrameCodec.TryWrite(ref platform, cursor,
			expected));
		cursor.Base = APTR.FromPointer(0x1800);
		cursor.Index = MuiUDataTraversalCursor.MaximumEntries;
		Assert.False(MuiUDataTraversalFrameCodec.TryRead(ref platform, cursor,
			out _));
	}

	[Fact]
	public void UserDataMethodHeaderUsesNamedField()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x7000, 0x2000,
			State);
		var packet = APTR.FromPointer(0x1700);
		platform.WriteUInt32(packet, 0, FindUData);
		Assert.True(MuiNotifyUserDataMessageCodec.TryReadMethodId(ref platform,
			packet, out var header));
		Assert.Equal(FindUData, header.MethodId);
		Assert.True(MuiNotifyUserDataMessageCodec.TryReadValue(ref platform,
			packet, out var methodId));
		Assert.Equal(FindUData, methodId);
		Assert.True(MuiNotifyUserDataMessageCodec.WriteValue(ref platform,
			packet, 0xF1234567u));
		Assert.True(MuiNotifyUserDataMessageCodec.TryReadValue(ref platform,
			packet, out methodId));
		Assert.Equal(0xF1234567u, methodId);
		Assert.False(MuiNotifyUserDataMessageCodec.TryReadValue(ref platform,
			APTR.FromPointer(0x7FFEu), out _));
		Assert.False(MuiNotifyUserDataMessageCodec.WriteValue(ref platform,
			APTR.Null, 1));
		Assert.False(MuiNotifyUserDataMessageCodec.TryReadMethodId(ref platform,
			APTR.Null, out _));
	}

	[Fact]
	public void UserDataPacketAndFrameMemoryAdaptersOwnStructBounds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x7000, 0x2000,
			State);
		var packet = APTR.FromPointer(0x1800);
		Assert.True(MuiNotifyUserDataPacketMemoryCodec.TryGetAddress(ref platform,
			packet, MuiNotifyUserDataPacketKind.Get,
			MuiNotifyUserDataPacketField.Storage, out var storageAddress));
		Assert.Equal(APTR.FromPointer(0x180C), storageAddress);
		Assert.True(MuiNotifyUserDataPacketMemoryCodec.TryWriteUInt32(ref platform,
			packet, MuiNotifyUserDataPacketKind.Get,
			MuiNotifyUserDataPacketField.Attribute, 0x8042AAAA));
		Assert.True(MuiNotifyUserDataPacketMemoryCodec.TryReadUInt32(ref platform,
			packet, MuiNotifyUserDataPacketKind.Get,
			MuiNotifyUserDataPacketField.Attribute, out var attribute));
		Assert.Equal(0x8042AAAAu, attribute);
		Assert.False(MuiNotifyUserDataPacketMemoryCodec.TryGetAddress(ref platform,
			packet, MuiNotifyUserDataPacketKind.Get,
			MuiNotifyUserDataPacketField.Value, out _));
		Assert.False(MuiNotifyUserDataPacketMemoryCodec.TryGetAddress(ref platform,
			APTR.FromPointer(0x7FFC), MuiNotifyUserDataPacketKind.Get,
			MuiNotifyUserDataPacketField.Storage, out _));

		var frame = APTR.FromPointer(0x1900);
		Assert.True(MuiUDataTraversalFrameMemoryCodec.TryWriteUInt32(ref platform,
			frame, MuiUDataTraversalField.Object, 0x2400));
		Assert.True(MuiUDataTraversalFrameMemoryCodec.TryReadUInt32(ref platform,
			frame, MuiUDataTraversalField.Object, out var rawObject));
		Assert.Equal(0x2400u, rawObject);
		Assert.True(MuiUDataTraversalFrameMemoryCodec.TryGetAddress(ref platform,
			frame, MuiUDataTraversalField.NextChild, out var nextChildAddress));
		Assert.Equal(APTR.FromPointer(0x1904), nextChildAddress);
		Assert.False(MuiUDataTraversalFrameMemoryCodec.TryGetAddress(ref platform,
			APTR.FromPointer(0x7FFC), MuiUDataTraversalField.Object, out _));
	}

	private static MuiHeadlessTestPlatform CreatePlatform(out APTR root,
		out APTR first, out APTR second, out APTR nested)
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x7000, 0x2000,
			State);
		var name = APTR.FromPointer(0x1100);
		platform.WriteCString(name, "Notify.mui");
		MuiHeadlessObjectCore.Initialize(ref platform, State);
		var cl = MuiHeadlessObjectCore.RegisterClass(ref platform, State, name,
			APTR.Null, 0, APTR.FromPointer(1), false);
		root = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		first = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		second = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		nested = MuiHeadlessObjectCore.CreateObjectA(ref platform, State, cl,
			APTR.Null);
		return platform;
	}

	private static void SetUserData(ref MuiHeadlessTestPlatform platform,
		APTR obj, uint value) =>
		Assert.True(MuiHeadlessObjectCore.SetAttribute(ref platform, State, obj,
			UserData, value, false));

	private static uint GetAttribute(ref MuiHeadlessTestPlatform platform,
		APTR obj)
	{
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, obj,
			ValueAttribute, out var value));
		return value;
	}
}
