using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiApplicationOwnedOmGetTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);

	[Fact]
	public void ApplicationCommandsAndWindowListUseNamedStateThroughCommonOmGet()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000,
			State);
		var className = APTR.FromPointer(0x1100);
		platform.WriteCString(className, "Application.mui");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		var applicationClass = MuiHeadlessObjectCore.RegisterBuiltinClass(ref
			platform, State, className, APTR.Null, 0, APTR.FromPointer(1));
		var application = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			applicationClass, APTR.Null);
		var firstWindow = MuiHeadlessObjectCore.CreateObjectA(ref platform, State,
			applicationClass, APTR.Null);
		Assert.NotEqual(APTR.Null, application);
		Assert.NotEqual(APTR.Null, firstWindow);
		Assert.True(MuiApplicationWindowCore.AddWindow(ref platform, State,
			application, firstWindow));

		var commandTable = APTR.FromPointer(0x2000);
		Assert.True(MuiApplicationCommandRecordCodec.Write(ref platform,
			commandTable, default));
		Assert.True(MuiApplicationCommandsCore.SetApplicationCommandsValue(
			ref platform, State, application, commandTable.Raw));

		var message = APTR.FromPointer(0x2800);
		var storage = APTR.FromPointer(0x2900);
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform, message,
			MuiCommonPacketKind.Get, MuiCommonField.MethodId,
			MuiCommonControlPacketCore.OmGet));
		Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform, message,
			MuiCommonPacketKind.Get, MuiCommonField.Storage, storage.Raw));

		foreach (var attribute in new[] { MuiApplicationCommandsCore.Commands,
			MuiApplicationWindowListCore.WindowList })
		{
			Assert.True(MuiCommonFieldCursorCodec.TryWriteUInt32(ref platform,
				message, MuiCommonPacketKind.Get, MuiCommonField.Attribute,
				attribute));
			Assert.Equal(1u, MuiCommonControlDispatcher.Dispatch(ref platform,
				State, application, message));
			Assert.True(MuiGuestUlongStorageCodec.TryRead(ref platform, storage,
				out var stored));
			if (attribute == MuiApplicationCommandsCore.Commands)
			{
				Assert.Equal(commandTable.Raw, stored.Value);
				Assert.True(MuiApplicationCommandsCore.TryGetApplicationCommandsState(
					ref platform, State, application, out var commandState));
				Assert.Equal(commandTable, commandState.Table);
			}
			else
			{
				var list = APTR.FromPointer(stored.Value);
				Assert.True(list.IsNotNull);
				var cursorRaw = LayersExecListCodec.ReadHead(ref platform, list).Raw;
				Assert.Equal(firstWindow, MuiApplicationWindowListCore.NextObject(
					ref platform, list, ref cursorRaw));
			}
		}
	}
}
