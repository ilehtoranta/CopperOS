/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiNativeApplicationWindowSignalsTests
{
	private static readonly APTR Registry = APTR.FromPointer(0x1000);
	private static readonly APTR FirstBinding = APTR.FromPointer(0x1020);
	private static readonly APTR SecondBinding = APTR.FromPointer(0x1060);
	private static readonly APTR ThirdBinding = APTR.FromPointer(0x10A0);
	private static readonly APTR FirstSidecar = APTR.FromPointer(0x10D0);
	private static readonly APTR SecondSidecar = APTR.FromPointer(0x1130);
	private static readonly APTR ThirdSidecar = APTR.FromPointer(0x1190);
	private static readonly APTR FirstWindow = APTR.FromPointer(0x11F0);
	private static readonly APTR SecondWindow = APTR.FromPointer(0x1280);
	private static readonly APTR ThirdWindow = APTR.FromPointer(0x1310);
	private static readonly APTR FirstPort = APTR.FromPointer(0x13A0);
	private static readonly APTR SecondPort = APTR.FromPointer(0x13D0);
	private static readonly APTR ThirdPort = APTR.FromPointer(0x1400);
	private static readonly APTR ApplicationBinding = APTR.FromPointer(0x1440);
	private static readonly APTR ApplicationSidecar = APTR.FromPointer(0x1480);
	private static readonly APTR InputHandlerNode = APTR.FromPointer(0x14E0);
	private static readonly APTR InputHandler = APTR.FromPointer(0x1600);
	private static readonly APTR BufferedReturnIdRecord = APTR.FromPointer(0x1660);
	private static readonly APTR Owner = APTR.FromPointer(0x9000);
	private static readonly APTR Task = APTR.FromPointer(0x9100);
	private static readonly APTR OtherTask = APTR.FromPointer(0x9200);
	private static readonly APTR Application = APTR.FromPointer(0x9300);
	private static readonly APTR OtherApplication = APTR.FromPointer(0x9400);

	[Fact]
	public void InputHandlerMethodPacketFitsInsideTheNamedEntryRecord()
	{
		var address = APTR.FromPointer(0x2000);
		var memory = new MuiHeadlessTestPlatform(address.Raw,
			(int)MuiNativeInputHandlerEntryRecord.Size, 0, address);
		var method = 0x8042F456u;
		var entry = default(MuiNativeInputHandlerEntryRecord);
		entry.Method.MethodId = method;
		Assert.True(MuiNativeInputHandlerEntryCodec.Write(ref memory, address,
			entry));
		Assert.True(MuiNativeInputHandlerEntryCodec.TryGetPayload(ref memory,
			address, MuiApplicationMethodHeaderMessage.Size, out var payload));
		Assert.True(MuiApplicationMethodHeaderCodec.WriteValue(ref memory,
			payload, method));
		Assert.True(MuiNativeInputHandlerEntryCodec.TryRead(ref memory, address,
			out var decoded));
		Assert.Equal(method, decoded.Method.MethodId);
		Assert.False(MuiNativeInputHandlerEntryCodec.TryGetPayload(ref memory,
			address, MuiApplicationMethodHeaderMessage.Size + 4, out _));
	}

	[Fact]
	public void WindowSignalMaskUnionsOnlyPortsOwnedByTheCurrentTask()
	{
		var memory = new MuiHeadlessTestPlatform(Registry.Raw, 0x1000, 0,
			Registry);
		var state = new MuiNativePublicObjectRegistryRecord
		{
			Signature = MuiNativePublicObjectRegistryRecord.Magic,
			Revision = MuiNativePublicObjectRegistryRecord.Version,
			Head = FirstBinding,
		};
		Assert.True(MuiNativePublicObjectRegistryCodec.Write(ref memory,
			Registry, state));
		Assert.True(MuiNativePublicObjectBindingCodec.Write(ref memory,
			FirstBinding, Binding(FirstBinding, FirstSidecar, FirstWindow,
				Application,
				SecondBinding)));
		Assert.True(MuiNativePublicObjectBindingCodec.Write(ref memory,
			SecondBinding, Binding(SecondBinding, SecondSidecar, SecondWindow,
				Application, ThirdBinding)));
		Assert.True(MuiNativePublicObjectBindingCodec.Write(ref memory,
			ThirdBinding, Binding(ThirdBinding, ThirdSidecar, ThirdWindow,
				OtherApplication, ApplicationBinding)));
		Assert.True(MuiNativePublicObjectBindingCodec.Write(ref memory,
			ApplicationBinding, Binding(Application, ApplicationSidecar,
				APTR.Null, APTR.Null, APTR.Null)));
		Assert.True(MuiNativeMuiObjectCodec.Write(ref memory, FirstSidecar,
			Sidecar(FirstBinding, FirstWindow, parent: Application)));
		Assert.True(MuiNativeMuiObjectCodec.Write(ref memory, SecondSidecar,
			Sidecar(SecondBinding, SecondWindow, parent: Application)));
		Assert.True(MuiNativeMuiObjectCodec.Write(ref memory, ThirdSidecar,
			Sidecar(ThirdBinding, ThirdWindow, parent: OtherApplication)));
		Assert.True(MuiNativeReturnIdRecordCodec.Write(ref memory,
			BufferedReturnIdRecord, new MuiNativeReturnIdRecord
			{
				Signature = MuiNativeReturnIdRecord.Magic,
				Revision = MuiNativeReturnIdRecord.Version,
				ReturnId = 0x1234,
			}));
		Assert.True(MuiNativeMuiObjectCodec.Write(ref memory, ApplicationSidecar,
			Sidecar(Application, APTR.Null,
				returnIdQueue: BufferedReturnIdRecord)));
		IntuitionScreenWindowGuestCodec.WriteWindow(ref memory, FirstWindow,
			new Window { UserPort = FirstPort });
		IntuitionScreenWindowGuestCodec.WriteWindow(ref memory, SecondWindow,
			new Window { UserPort = SecondPort });
		IntuitionScreenWindowGuestCodec.WriteWindow(ref memory, ThirdWindow,
			new Window { UserPort = ThirdPort });
		ExecMsgPortCodec.Write(ref memory, FirstPort, new MsgPort
		{
			SignalBit = 2,
			SignalTask = Task,
		});
		ExecMsgPortCodec.Write(ref memory, SecondPort, new MsgPort
		{
			SignalBit = 7,
			SignalTask = OtherTask,
		});
		ExecMsgPortCodec.Write(ref memory, ThirdPort, new MsgPort
		{
			SignalBit = 9,
			SignalTask = Task,
		});

		Assert.True(MuiNativeApplicationWindowSignals.TryBuildMask(ref memory,
			Registry, Owner, Application, Task, out var mask));
		Assert.Equal(1u << 2, mask);
		Assert.True(MuiNativeApplicationWindowSignals.TryGetPendingInputSignals(
			ref memory, Registry, Owner, Application, Task,
			(1u << 2) | (1u << 7) | (1u << 31), out var pendingInputSignals));
		Assert.Equal(1u << 2, pendingInputSignals);
		Assert.True(MuiNativeMuiObjectCodec.TryRead(ref memory,
			ApplicationSidecar, out var applicationSidecar));
		Assert.Equal(BufferedReturnIdRecord,
			applicationSidecar.ReturnIdQueue);
		Assert.True(MuiNativeReturnIdRecordCodec.TryRead(ref memory,
			applicationSidecar.ReturnIdQueue, out var queuedReturnId));
		Assert.Equal(0x1234u, queuedReturnId.ReturnId);
	}

	[Fact]
	public void ApplicationInputHandlerSignalsJoinTheWaitMask()
	{
		var memory = new MuiHeadlessTestPlatform(Registry.Raw, 0x1000, 0,
			Registry);
		Assert.True(MuiNativePublicObjectRegistryCodec.Write(ref memory,
			Registry, new MuiNativePublicObjectRegistryRecord
			{
				Signature = MuiNativePublicObjectRegistryRecord.Magic,
				Revision = MuiNativePublicObjectRegistryRecord.Version,
				Head = ApplicationBinding,
			}));
		Assert.True(MuiNativePublicObjectBindingCodec.Write(ref memory,
			ApplicationBinding, Binding(Application, ApplicationSidecar,
				APTR.Null, APTR.Null, APTR.Null)));
		Assert.True(MuiNativeMuiObjectCodec.Write(ref memory, ApplicationSidecar,
			Sidecar(Application, APTR.Null, InputHandlerNode,
				inputHandlerGeneration: 1)));
		var method = 0x8042F456u;
		Assert.True(MuiNativeInputHandlerEntryCodec.Write(ref memory,
			InputHandlerNode, new MuiNativeInputHandlerEntryRecord
			{
				Handler = InputHandler,
				Sequence = 1,
				Method = new MuiApplicationMethodHeaderMessage
				{
					MethodId = method,
				},
			}));
		Assert.True(MuiNativeInputHandlerEntryCodec.TryGetPayload(ref memory,
			InputHandlerNode, MuiApplicationMethodHeaderMessage.Size,
			out var payload));
		Assert.True(MuiApplicationMethodHeaderCodec.WriteValue(ref memory,
			payload, method));
		Assert.True(MuiInputHandlerCodec.Write(ref memory, InputHandler,
			new MuiInputHandlerRecord
			{
				Object = APTR.FromPointer(0x1550),
				Events = 1u << 11,
				Packet = method,
			}));
		Assert.True(MuiNativeInputHandlerEntryCodec.TryRead(ref memory,
			InputHandlerNode, out var entry));
		Assert.Equal(InputHandler, entry.Handler);
		Assert.Equal(method, entry.Method.MethodId);
		Assert.True(MuiInputHandlerCodec.TryRead(ref memory, InputHandler,
			out var handler));
		Assert.Equal(method, handler.Method);
		Assert.Equal(1u << 11, handler.Value.Signals);
		Assert.True(MuiNativeInputHandlerEntryCodec.TryGetPayload(ref memory,
			InputHandlerNode, MuiApplicationMethodHeaderMessage.Size,
			out var handlerMessage));
		Assert.True(MuiApplicationMethodHeaderCodec.TryReadValue(ref memory,
			handlerMessage, out var handlerMethod));
		Assert.Equal(method, handlerMethod);
		Assert.True(MuiNativeApplicationInputHandlerQueue.Validate(ref memory,
			InputHandlerNode, 1));
		Assert.True(MuiNativeApplicationInputHandlerQueue.TryReadSignalMask(
			ref memory, InputHandlerNode, 1, APTR.Null, Task,
			out var inputHandlerMask));
		Assert.Equal(1u << 11, inputHandlerMask);

		Assert.True(MuiNativeApplicationWindowSignals.TryBuildMask(ref memory,
			Registry, Owner, Application, Task, out var mask));
		Assert.Equal(1u << 11, mask);
	}

	[Fact]
	public void MalformedPublicObjectChainDoesNotPublishPartialWindowMask()
	{
		var memory = new MuiHeadlessTestPlatform(Registry.Raw, 0x1000, 0,
			Registry);
		Assert.True(MuiNativePublicObjectRegistryCodec.Write(ref memory,
			Registry, new MuiNativePublicObjectRegistryRecord
			{
				Signature = MuiNativePublicObjectRegistryRecord.Magic,
				Revision = MuiNativePublicObjectRegistryRecord.Version,
				Head = FirstBinding,
			}));
		Assert.False(MuiNativeApplicationWindowSignals.TryBuildMask(ref memory,
			Registry, Owner, Application, Task, out var mask));
		Assert.Equal(0u, mask);
	}

	private static MuiNativePublicObjectBinding Binding(APTR obj,
		APTR sidecar, APTR nativeWindow, APTR parent, APTR next) => new()
	{
		Signature = MuiNativePublicObjectBinding.Magic,
		Next = next,
		Object = obj,
		Class = APTR.FromPointer(obj.Raw + 0x100),
		OwnerRoot = Owner,
		Parent = parent,
		Sidecar = sidecar,
		DisposeState = MuiNativePublicObjectBinding.StateLive,
	};

	private static MuiNativeMuiObjectRecord Sidecar(APTR obj,
		APTR nativeWindow, APTR inputHandlers = default,
		APTR parent = default, uint inputHandlerGeneration = 0,
		APTR returnIdQueue = default) => new()
	{
		Signature = MuiNativeMuiObjectRecord.Magic,
		Revision = MuiNativeMuiObjectRecord.Version,
		Object = obj,
		Class = APTR.FromPointer(obj.Raw + 0x100),
		OwnerRoot = Owner,
		Flags = MuiNativeMuiObjectRecord.ObjectInitialized,
		LifecycleState = MuiNativeMuiObjectRecord.StateLive,
		Parent = parent,
		NativeWindow = nativeWindow,
		InputHandlers = inputHandlers,
		InputHandlerGeneration = inputHandlerGeneration,
		ReturnIdQueue = returnIdQueue,
	};
}
