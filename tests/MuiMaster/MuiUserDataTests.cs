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
		platform.WriteUInt32(packet, 0, FindUData);
		platform.WriteUInt32(packet, 4, 0x22);
		Assert.Equal(nested.Raw, MuiHeadlessDispatcher.Dispatch(ref platform, State,
			root, packet));

		var storage = APTR.FromPointer(0x1710);
		platform.WriteUInt32(packet, 0, GetUData);
		platform.WriteUInt32(packet, 4, 0x22);
		platform.WriteUInt32(packet, 8, ValueAttribute);
		platform.WriteUInt32(packet, 12, storage.Raw);
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
		platform.WriteUInt32(packet, 0, SetUData);
		platform.WriteUInt32(packet, 4, 0x77);
		platform.WriteUInt32(packet, 8, ValueAttribute);
		platform.WriteUInt32(packet, 12, 0x1111);
		Assert.Equal(1u, MuiHeadlessDispatcher.Dispatch(ref platform, State, root,
			packet));
		Assert.Equal(0x1111u, GetAttribute(ref platform, root));
		Assert.Equal(0x1111u, GetAttribute(ref platform, nested));
		Assert.Equal(0x1111u, GetAttribute(ref platform, second));

		platform.WriteUInt32(packet, 0, SetUDataOnce);
		platform.WriteUInt32(packet, 12, 0x2222);
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
		platform.WriteUInt32(packet, 0, GetUData);
		platform.WriteUInt32(packet, 4, 1);
		platform.WriteUInt32(packet, 8, ValueAttribute);
		platform.WriteUInt32(packet, 12, 0x5FFF);
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
	public void UserDataMethodHeaderUsesNamedField()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x7000, 0x2000,
			State);
		var packet = APTR.FromPointer(0x1700);
		platform.WriteUInt32(packet, 0, FindUData);
		Assert.True(MuiNotifyUserDataMessageCodec.TryReadMethodId(ref platform,
			packet, out var header));
		Assert.Equal(FindUData, header.MethodId);
		Assert.False(MuiNotifyUserDataMessageCodec.TryReadMethodId(ref platform,
			APTR.Null, out _));
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
