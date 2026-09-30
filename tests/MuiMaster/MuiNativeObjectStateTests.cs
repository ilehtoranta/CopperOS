/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.CompilerServices;
using Amiga;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiNativeObjectStateTests
{
	private static readonly APTR SidecarAddress = APTR.FromPointer(0x1800);
	private static readonly APTR AttributeAddress = APTR.FromPointer(0x1840);
	private static readonly APTR NotificationAddress = APTR.FromPointer(0x1860);

	private static MuiNativeObjectAttributeRecord AttributeSample() => new()
	{
		Signature = MuiNativeObjectAttributeRecord.Magic,
		Revision = MuiNativeObjectAttributeRecord.Version,
		Next = APTR.FromPointer(0xABCDEF00),
		Attribute = 0x80420001,
		Value = 0x11223344,
		Generation = 7,
	};

	private static MuiNativeObjectNotificationRecord NotificationSample() => new()
	{
		Signature = MuiNativeObjectNotificationRecord.Magic,
		Revision = MuiNativeObjectNotificationRecord.Version,
		Next = APTR.FromPointer(0xABCDEF10),
		Sequence = 8,
		TriggerAttribute = 0x80420002,
		TriggerValue = 0x55667788,
		Destination = APTR.FromPointer(0x12345678),
		FollowCount = 2,
		Flags = MuiNativeObjectNotificationRecord.Active,
		Payload = APTR.FromPointer(0xA5A55A5A),
	};

	[Fact]
	public void NativeAttributeRecordRoundTripsNamedFields()
	{
		Assert.Equal((int)MuiNativeObjectAttributeRecord.Size,
			Unsafe.SizeOf<MuiNativeObjectAttributeRecord>());
		var memory = new MuiHeadlessTestPlatform(SidecarAddress.Raw, 64, 0,
			SidecarAddress);
		memory.WriteUInt32(SidecarAddress,
			(int)MuiNativeObjectAttributeRecord.Size, 0x5AA5A55A);
		var expected = AttributeSample();
		Assert.True(MuiNativeObjectAttributeCodec.Write(ref memory,
			SidecarAddress, expected));
		Assert.True(MuiNativeObjectAttributeCodec.TryRead(ref memory,
			SidecarAddress, out var actual));
		Assert.Equal(expected, actual);
		Assert.Equal(0x5AA5A55Au, memory.ReadUInt32(SidecarAddress,
			(int)MuiNativeObjectAttributeRecord.Size));
	}

	[Fact]
	public void NativeNotificationRecordRoundTripsNamedFields()
	{
		Assert.Equal((int)MuiNativeObjectNotificationRecord.Size,
			Unsafe.SizeOf<MuiNativeObjectNotificationRecord>());
		var memory = new MuiHeadlessTestPlatform(NotificationAddress.Raw, 64,
			0, NotificationAddress);
		memory.WriteUInt32(NotificationAddress,
			(int)MuiNativeObjectNotificationRecord.Size, 0x5AA5A55A);
		var expected = NotificationSample();
		Assert.True(MuiNativeObjectNotificationCodec.Write(ref memory,
			NotificationAddress, expected));
		Assert.True(MuiNativeObjectNotificationCodec.TryRead(ref memory,
			NotificationAddress, out var actual));
		Assert.Equal(expected, actual);
		Assert.Equal(0x5AA5A55Au, memory.ReadUInt32(NotificationAddress,
			(int)MuiNativeObjectNotificationRecord.Size));
	}

	[Fact]
	public void NativeInputEventNotificationOwnsTypedInputExpressionPointer()
	{
		var inputExpressionAddress = APTR.FromPointer(
			NotificationAddress.Raw + 0x40);
		var memory = new MuiHeadlessTestPlatform(NotificationAddress.Raw, 128,
			0, NotificationAddress);
		var inputExpression = new InputXpression
		{
			Version = MuiInputExpressionMemoryCodec.Version,
			Class = (byte)InputEventClass.RawKey,
			Code = 0x0033,
			CodeMask = 0x007F,
			Qualifier = (ushort)InputEventQualifier.RightCommand,
			QualifierMask = (ushort)InputEventQualifier.RightCommand,
		};
		Assert.True(MuiInputExpressionMemoryCodec.Write(ref memory,
			inputExpressionAddress, inputExpression));
		var notification = NotificationSample();
		notification.TriggerAttribute = MuiWindowPublicCore.InputEvent;
		notification.Flags |=
			MuiNativeObjectNotificationRecord.HasInputExpression;
		notification.InputExpression = inputExpressionAddress;
		Assert.True(MuiNativeObjectNotificationCodec.Write(ref memory,
			NotificationAddress, notification));
		Assert.True(MuiNativeObjectNotificationCodec.TryRead(ref memory,
			NotificationAddress, out var actual));
		Assert.Equal(inputExpressionAddress, actual.InputExpression);
		Assert.True(MuiInputExpressionMemoryCodec.TryRead(ref memory,
			actual.InputExpression, out var actualExpression));
		Assert.Equal(inputExpression, actualExpression);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(8)]
	[InlineData(23)]
	public void NativeAttributeRecordRejectsTruncationWithoutPartialWrites(
		int mappedBytes)
	{
		var memory = new MuiHeadlessTestPlatform(SidecarAddress.Raw,
			mappedBytes, 0, SidecarAddress);
		for (var index = 0; index < mappedBytes; index++)
			memory.WriteUInt8(SidecarAddress, index, 0xA5);
		Assert.False(MuiNativeObjectAttributeCodec.TryRead(ref memory,
			SidecarAddress, out var actual));
		Assert.Equal(default(MuiNativeObjectAttributeRecord), actual);
		Assert.False(MuiNativeObjectAttributeCodec.Write(ref memory,
			SidecarAddress, AttributeSample()));
		for (var index = 0; index < mappedBytes; index++)
			Assert.Equal(0xA5, memory.ReadUInt8(SidecarAddress, index));
	}

	[Theory]
	[InlineData(0)]
	[InlineData(8)]
	[InlineData(43)]
	public void NativeNotificationRecordRejectsTruncationWithoutPartialWrites(
		int mappedBytes)
	{
		var memory = new MuiHeadlessTestPlatform(NotificationAddress.Raw,
			mappedBytes, 0, NotificationAddress);
		for (var index = 0; index < mappedBytes; index++)
			memory.WriteUInt8(NotificationAddress, index, 0xA5);
		Assert.False(MuiNativeObjectNotificationCodec.TryRead(ref memory,
			NotificationAddress, out var actual));
		Assert.Equal(default(MuiNativeObjectNotificationRecord), actual);
		Assert.False(MuiNativeObjectNotificationCodec.Write(ref memory,
			NotificationAddress, NotificationSample()));
		for (var index = 0; index < mappedBytes; index++)
			Assert.Equal(0xA5, memory.ReadUInt8(NotificationAddress, index));
	}

	[Fact]
	public void NativeAttributeLookupUsesSidecarRootAndNamedRecords()
	{
		var memory = new MuiHeadlessTestPlatform(SidecarAddress.Raw, 128, 0,
			SidecarAddress);
		var sidecar = default(MuiNativeMuiObjectRecord);
		sidecar.Signature = MuiNativeMuiObjectRecord.Magic;
		sidecar.Revision = MuiNativeMuiObjectRecord.Version;
		sidecar.Flags = MuiNativeMuiObjectRecord.ObjectInitialized;
		sidecar.ObjectId = 0xCAFEBABE;
		sidecar.UserData = 0x12345678;
		sidecar.Attributes = AttributeAddress;
		Assert.True(MuiNativeMuiObjectCodec.Write(ref memory, SidecarAddress,
			sidecar));
		var attribute = AttributeSample();
		attribute.Next = APTR.Null;
		Assert.True(MuiNativeObjectAttributeCodec.Write(ref memory,
			AttributeAddress, attribute));

		Assert.True(MuiNativeObjectStateCore.TryGetAttribute(ref memory,
			SidecarAddress, attribute.Attribute, out var value));
		Assert.Equal(attribute.Value, value);
		Assert.True(MuiNativeObjectStateCore.TryGetAttribute(ref memory,
			SidecarAddress, MuiNativeObjectStateCore.ObjectIdAttribute,
			out value));
		Assert.Equal(0xCAFEBABEu, value);
		Assert.True(MuiNativeObjectStateCore.TryGetAttribute(ref memory,
			SidecarAddress, MuiNativeObjectStateCore.UserDataAttribute,
			out value));
		Assert.Equal(0x12345678u, value);
		Assert.False(MuiNativeObjectStateCore.TryGetAttribute(ref memory,
			SidecarAddress, 0x8042FFFF, out _));
	}

	[Fact]
	public void NativeIDCMPRequestsAreGuestResidentAndLastOperationWins()
	{
		var memory = new MuiHeadlessTestPlatform(SidecarAddress.Raw, 128, 0,
			SidecarAddress);
		var sidecar = new MuiNativeMuiObjectRecord
		{
			Signature = MuiNativeMuiObjectRecord.Magic,
			Revision = MuiNativeMuiObjectRecord.Version,
			Flags = MuiNativeMuiObjectRecord.ObjectInitialized,
		};
		Assert.True(MuiNativeMuiObjectCodec.Write(ref memory, SidecarAddress,
			sidecar));

		Assert.True(MuiNativeObjectStateCore.TryUpdateIDCMPRequest(ref memory,
			SidecarAddress, 0x18, true, out var requested, out var rejected));
		Assert.Equal(0x18u, requested);
		Assert.Equal(0u, rejected);
		Assert.True(MuiNativeObjectStateCore.TryUpdateIDCMPRequest(ref memory,
			SidecarAddress, 0x10, false, out requested, out rejected));
		Assert.Equal(0x08u, requested);
		Assert.Equal(0x10u, rejected);
		Assert.True(MuiNativeObjectStateCore.TryUpdateIDCMPRequest(ref memory,
			SidecarAddress, 0x10, true, out requested, out rejected));
		Assert.Equal(0x18u, requested);
		Assert.Equal(0u, rejected);
		Assert.True(MuiNativeMuiObjectCodec.TryRead(ref memory, SidecarAddress,
			out var stored));
		Assert.Equal(requested, stored.RequestedIDCMP);
		Assert.Equal(rejected, stored.RejectedIDCMP);
	}

	[Fact]
	public void NativeDispatcherAdmissionRequiresPublicBinding()
	{
		var registryAddress = APTR.FromPointer(0x1800);
		var bindingAddress = APTR.FromPointer(0x1810);
		var sidecarAddress = APTR.FromPointer(0x1840);
		var ownerRoot = APTR.FromPointer(0x1A00);
		var classAddress = APTR.FromPointer(0x1B00);
		var objectAddress = APTR.FromPointer(0x2000);
		var memory = new MuiHeadlessTestPlatform(registryAddress.Raw, 128, 0,
			registryAddress);
		var registry = new MuiNativePublicObjectRegistryRecord
		{
			Signature = MuiNativePublicObjectRegistryRecord.Magic,
			Revision = MuiNativePublicObjectRegistryRecord.Version,
			Head = bindingAddress,
		};
		var binding = new MuiNativePublicObjectBinding
		{
			Signature = MuiNativePublicObjectBinding.Magic,
			Object = objectAddress,
			Class = classAddress,
			OwnerRoot = ownerRoot,
			Sidecar = sidecarAddress,
		};
		Assert.True(MuiNativePublicObjectRegistryCodec.Write(ref memory,
			registryAddress, registry));
		Assert.True(MuiNativePublicObjectBindingCodec.Write(ref memory,
			bindingAddress, binding));
		Assert.True(MuiNativePublicObjectCore.TryFindBinding(ref memory,
			registryAddress, ownerRoot, objectAddress, out var found));
		Assert.Equal(objectAddress, found.Object);
		Assert.False(MuiNativePublicObjectCore.TryFindBinding(ref memory,
			registryAddress, ownerRoot, APTR.FromPointer(0x2004), out _));
	}
}
