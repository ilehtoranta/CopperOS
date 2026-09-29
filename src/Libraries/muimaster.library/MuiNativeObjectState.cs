/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;
using System.Runtime.InteropServices;

namespace CopperOS.MuiMaster;

// Native public objects keep their MUI attributes in a library-owned list.
// This is deliberately not the Intuition instance layout: every field is
// part of a complete guest-resident record and is admitted through its named
// codec.  The shape is compatible with the portable headless attribute node
// while adding identity/version fields so a damaged native list fails closed.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiNativeObjectAttributeRecord
{
	internal const uint Magic = 0x4D554154; // "MUAT"
	internal const uint Version = 2;
	internal const uint Size = 32;
	internal const uint FieldSize = 4;

	internal uint Signature;
	internal uint Revision;
	internal APTR Next;
	internal uint Attribute;
	internal uint Value;
	internal uint Generation;
	// MUIM_SetAsString-created attribute values are owned by the object. These
	// named fields keep that lifetime alongside the attribute rather than in an
	// offset-indexed side table.
	internal APTR OwnedText;
	internal uint OwnedTextSize;
}

internal static class MuiNativeObjectAttributeCodec
{
	internal static bool TryRead<T>(ref T memory, APTR address,
		out MuiNativeObjectAttributeRecord value)
		where T : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref memory, address,
			MuiNativeObjectAttributeRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out value.Signature) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out value.Revision) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out var next) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out value.Attribute) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out value.Value) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out value.Generation) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out var ownedText) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out value.OwnedTextSize) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		value.Next = APTR.FromPointer(next);
		value.OwnedText = APTR.FromPointer(ownedText);
		return value.Signature == MuiNativeObjectAttributeRecord.Magic &&
			value.Revision == MuiNativeObjectAttributeRecord.Version &&
			(value.OwnedText.IsNull == (value.OwnedTextSize == 0)) &&
			(value.OwnedText.IsNull || (value.Value == value.OwnedText.Raw &&
				value.OwnedTextSize <=
					MuiNotifySetAsStringCore.MaximumOutputLength + 1 &&
				memory.IsMapped(value.OwnedText, value.OwnedTextSize)));
	}

	internal static bool Write<T>(ref T memory, APTR address,
		MuiNativeObjectAttributeRecord value)
		where T : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref memory, address,
			MuiNativeObjectAttributeRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.Signature) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.Revision) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.Next.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.Attribute) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.Value) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.Generation) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.OwnedText.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.OwnedTextSize) &&
		MuiGuestStructCursor.IsComplete(cursor);
}

// The notification node owns the parsed input filter and a pointer to its
// copied follow-value vector. Both relationships are explicit named fields so
// event matching and teardown use the same bounded record shape.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiNativeObjectNotificationRecord
{
	internal const uint Magic = 0x4D554E54; // "MUNT"
	internal const uint Version = 2;
	internal const uint Size = 44;
	internal const uint Active = 1;
	internal const uint Consumed = 2;
	internal const uint HasInputExpression = 4;

	internal uint Signature;
	internal uint Revision;
	internal APTR Next;
	internal uint Sequence;
	internal uint TriggerAttribute;
	internal uint TriggerValue;
	internal APTR Destination;
	internal uint FollowCount;
	internal uint Flags;
	// A notification owns a copied follow-value vector. Keeping this pointer
	// in the named header makes the payload lifetime explicit without adding a
	// variable-sized managed field or relying on a caller packet offset.
	internal APTR Payload;
	// MUIA_Window_InputEvent stores the parsed commodities IX rather than the
	// caller's transient input-description string, matching MorphOS MUI 3.0.
	internal APTR InputExpression;
	// Source compatibility for older qualification fixtures. The wire field is
	// the named Payload pointer, not an anonymous reserved ULONG.
	internal uint Reserved
	{
		get => Payload.Raw;
		set => Payload = APTR.FromPointer(value);
	}
}

internal static class MuiNativeObjectNotificationCodec
{
	internal static bool TryRead<T>(ref T memory, APTR address,
		out MuiNativeObjectNotificationRecord value)
		where T : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref memory, address,
			MuiNativeObjectNotificationRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out value.Signature) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out value.Revision) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out var next) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out value.Sequence) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out value.TriggerAttribute) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out value.TriggerValue) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out var destination) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out value.FollowCount) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out value.Flags) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out var payload) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out var inputExpression) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		value.Next = APTR.FromPointer(next);
		value.Destination = APTR.FromPointer(destination);
		value.Payload = APTR.FromPointer(payload);
		value.InputExpression = APTR.FromPointer(inputExpression);
		var hasInputExpression = (value.Flags &
			MuiNativeObjectNotificationRecord.HasInputExpression) != 0;
		return value.Signature == MuiNativeObjectNotificationRecord.Magic &&
			value.Revision == MuiNativeObjectNotificationRecord.Version &&
			hasInputExpression == value.InputExpression.IsNotNull &&
			(!hasInputExpression || (value.TriggerAttribute ==
				MuiWindowPublicCore.InputEvent &&
				MuiInputExpressionMemoryCodec.TryRead(ref memory,
					value.InputExpression, out _)));
	}

	internal static bool Write<T>(ref T memory, APTR address,
		MuiNativeObjectNotificationRecord value)
		where T : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref memory, address,
			MuiNativeObjectNotificationRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.Signature) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.Revision) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.Next.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.Sequence) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.TriggerAttribute) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.TriggerValue) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.Destination.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.FollowCount) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.Flags) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.Payload.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.InputExpression.Raw) &&
		MuiGuestStructCursor.IsComplete(cursor);
}

// Native object state operations are intentionally narrow. They cover the
// library-owned attribute/notification roots and the bounded generic dispatcher
// projection. Derived-class policy and resident-vector behavior can build on
// these records later without changing the native object ABI.
internal static class MuiNativeObjectStateCore
{
	internal const uint MaximumNotificationFollowCount = 256;
	internal const uint MaximumNotificationDepth = 32;
	private const uint NotificationEveryTime = 1233727793u;
	private const uint NotificationTriggerValue = 1233727793u;
	private const uint NotificationNotTriggerValue = 1233727795u;
	// MUIA_ObjectID and MUIA_UserData are the two scalar fields carried by the
	// object sidecar itself.  No private Intuition offsets are involved.
	internal const uint ObjectIdAttribute = 0x8042D76E;
	internal const uint UserDataAttribute = 0x80420313;
	internal const uint NoNotifyAttribute = 0x804237F9;
	internal const uint NoNotifyMethodAttribute = 0x80420A74;

	internal static bool TryGetAttribute<T>(ref T memory, APTR sidecarAddress,
		uint attribute, out uint value)
		where T : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiNativeMuiObjectCodec.TryRead(ref memory, sidecarAddress,
			out var sidecar) || sidecar.LifecycleState !=
			MuiNativeMuiObjectRecord.StateLive ||
			(sidecar.Flags & MuiNativeMuiObjectRecord.ObjectInitialized) == 0 ||
			(sidecar.Flags & MuiNativeMuiObjectRecord.ObjectDisposing) != 0)
			return false;
		if (attribute == ObjectIdAttribute)
		{
			value = sidecar.ObjectId;
			return true;
		}
		if (attribute == UserDataAttribute)
		{
			value = sidecar.UserData;
			return true;
		}
		var current = sidecar.Attributes;
		for (uint visited = 0; current.IsNotNull &&
			visited++ < MuiHeadlessLayout.MaximumTraversal; visited++)
		{
			if (!MuiNativeObjectAttributeCodec.TryRead(ref memory, current,
				out var record)) return false;
			if (record.Attribute == attribute)
			{
				value = record.Value;
				return true;
			}
			current = record.Next;
		}
		return false;
	}

	internal static bool TryUpdateIDCMPRequest<T>(ref T memory,
		APTR sidecarAddress, uint flags, bool request, out uint requested,
		out uint rejected) where T : struct, IMuiGuestMemory
	{
		requested = 0;
		rejected = 0;
		if (flags == 0 || !MuiNativeMuiObjectCodec.TryRead(ref memory,
			sidecarAddress, out var sidecar) || sidecar.LifecycleState !=
			MuiNativeMuiObjectRecord.StateLive ||
			(sidecar.Flags & MuiNativeMuiObjectRecord.ObjectInitialized) == 0 ||
			(sidecar.Flags & MuiNativeMuiObjectRecord.ObjectDisposing) != 0)
			return false;

		if (request)
		{
			sidecar.RequestedIDCMP |= flags;
			sidecar.RejectedIDCMP &= ~flags;
		}
		else
		{
			sidecar.RejectedIDCMP |= flags;
			sidecar.RequestedIDCMP &= ~flags;
		}
		if (!MuiNativeMuiObjectCodec.Write(ref memory, sidecarAddress,
			sidecar)) return false;
		requested = sidecar.RequestedIDCMP;
		rejected = sidecar.RejectedIDCMP;
		return true;
	}

	internal static bool SetAttribute(ref MuiNativeClassPlatform platform,
		APTR sidecarAddress, uint attribute, uint value)
		=> SetAttribute(ref platform, sidecarAddress, attribute, value, true);

	internal static bool SetAttribute(ref MuiNativeClassPlatform platform,
		APTR sidecarAddress, uint attribute, uint value, bool notify)
	{
		if (attribute == NoNotifyAttribute ||
			attribute == NoNotifyMethodAttribute) return false;
		var memory = default(MuiNativeClassMemory);
		if (!MuiNativeMuiObjectCodec.TryRead(ref memory, sidecarAddress,
			out var sidecar) || sidecar.LifecycleState !=
			MuiNativeMuiObjectRecord.StateLive ||
			(sidecar.Flags & MuiNativeMuiObjectRecord.ObjectInitialized) == 0 ||
			(sidecar.Flags & MuiNativeMuiObjectRecord.ObjectDisposing) != 0)
			return false;
		if (attribute == ObjectIdAttribute)
		{
			sidecar.ObjectId = value;
			sidecar.Generation = NextGeneration(sidecar.Generation);
			if (!MuiNativeMuiObjectCodec.Write(ref memory, sidecarAddress,
				sidecar)) return false;
			if (notify) DispatchAttributeChange(ref platform, sidecarAddress,
				attribute, value);
			return true;
		}
		if (attribute == UserDataAttribute)
		{
			sidecar.UserData = value;
			sidecar.Generation = NextGeneration(sidecar.Generation);
			if (!MuiNativeMuiObjectCodec.Write(ref memory, sidecarAddress,
				sidecar)) return false;
			if (notify) DispatchAttributeChange(ref platform, sidecarAddress,
				attribute, value);
			return true;
		}

		var current = sidecar.Attributes;
		for (uint visited = 0; current.IsNotNull &&
			visited++ < MuiHeadlessLayout.MaximumTraversal; visited++)
		{
			if (!MuiNativeObjectAttributeCodec.TryRead(ref memory, current,
				out var record)) return false;
			if (record.Attribute == attribute)
			{
				var previous = record;
				var releasedText = APTR.Null;
				var releasedTextSize = 0u;
				if (record.OwnedText.IsNotNull && value != record.Value)
				{
					releasedText = record.OwnedText;
					releasedTextSize = record.OwnedTextSize;
					record.OwnedText = APTR.Null;
					record.OwnedTextSize = 0;
				}
				record.Value = value;
				record.Generation = NextGeneration(sidecar.Generation);
				if (!MuiNativeObjectAttributeCodec.Write(ref memory, current,
					record)) return false;
				sidecar.Generation = record.Generation;
				if (MuiNativeMuiObjectCodec.Write(ref memory, sidecarAddress,
					sidecar))
				{
					if (releasedText.IsNotNull)
						platform.Free(releasedText, releasedTextSize);
					if (notify) DispatchAttributeChange(ref platform,
						sidecarAddress, attribute, value);
					return true;
				}
				MuiNativeObjectAttributeCodec.Write(ref memory, current, previous);
				return false;
			}
			current = record.Next;
		}
		if (current.IsNotNull) return false;
		var allocation = platform.Allocate(MuiNativeObjectAttributeRecord.Size,
			MuiHeadlessLayout.AllocationFlags);
		if (allocation.IsNull) return false;
		var fresh = default(MuiNativeObjectAttributeRecord);
		fresh.Signature = MuiNativeObjectAttributeRecord.Magic;
		fresh.Revision = MuiNativeObjectAttributeRecord.Version;
		fresh.Next = sidecar.Attributes;
		fresh.Attribute = attribute;
		fresh.Value = value;
		fresh.Generation = NextGeneration(sidecar.Generation);
		if (!MuiNativeObjectAttributeCodec.Write(ref memory, allocation, fresh))
		{
			platform.Free(allocation, MuiNativeObjectAttributeRecord.Size);
			return false;
		}
		sidecar.Attributes = allocation;
		sidecar.Generation = fresh.Generation;
		if (MuiNativeMuiObjectCodec.Write(ref memory, sidecarAddress, sidecar))
		{
			if (notify) DispatchAttributeChange(ref platform, sidecarAddress,
				attribute, value);
			return true;
		}
		platform.Free(allocation, MuiNativeObjectAttributeRecord.Size);
		return false;
	}

	// Lifecycle-owned pointers such as MUI_RenderInfo are borrowed from MUI and
	// must never trigger application notifications or be mistaken for owned
	// MUIM_SetAsString text. Keep this narrow writer generic so the native path
	// and host tests share the same named-record mutation logic.
	internal static bool SetBorrowedAttributeNoNotify<TPlatform>(
		ref TPlatform platform, APTR sidecarAddress, uint attribute, uint value)
		where TPlatform : struct, IMuiAllocationPlatform
	{
		if (attribute == 0 || attribute == ObjectIdAttribute ||
			attribute == UserDataAttribute || attribute == NoNotifyAttribute ||
			attribute == NoNotifyMethodAttribute) return false;
		if (!MuiNativeMuiObjectCodec.TryRead(ref platform, sidecarAddress,
			out var sidecar) || sidecar.LifecycleState !=
			MuiNativeMuiObjectRecord.StateLive ||
			(sidecar.Flags & MuiNativeMuiObjectRecord.ObjectInitialized) == 0 ||
			(sidecar.Flags & MuiNativeMuiObjectRecord.ObjectDisposing) != 0)
			return false;

		var current = sidecar.Attributes;
		for (uint visited = 0; current.IsNotNull &&
			visited++ < MuiHeadlessLayout.MaximumTraversal; visited++)
		{
			if (!MuiNativeObjectAttributeCodec.TryRead(ref platform, current,
				out var record)) return false;
			if (record.Attribute == attribute)
			{
				// A lifecycle pointer is borrowed; do not silently take ownership
				// away from an existing class-managed text value.
				if (record.OwnedText.IsNotNull) return false;
				var previous = record;
				record.Value = value;
				record.Generation = NextGeneration(sidecar.Generation);
				if (!MuiNativeObjectAttributeCodec.Write(ref platform, current,
					record)) return false;
				sidecar.Generation = record.Generation;
				if (MuiNativeMuiObjectCodec.Write(ref platform, sidecarAddress,
					sidecar)) return true;
				MuiNativeObjectAttributeCodec.Write(ref platform, current, previous);
				return false;
			}
			current = record.Next;
		}
		if (current.IsNotNull) return false;
		// Clearing a borrowed transient pointer is a no-op when it was never
		// published; do not allocate an empty attribute node during a failed
		// Setup or an otherwise harmless Cleanup.
		if (value == 0) return true;

		var allocation = platform.Allocate(MuiNativeObjectAttributeRecord.Size,
			MuiHeadlessLayout.AllocationFlags);
		if (allocation.IsNull) return false;
		var fresh = default(MuiNativeObjectAttributeRecord);
		fresh.Signature = MuiNativeObjectAttributeRecord.Magic;
		fresh.Revision = MuiNativeObjectAttributeRecord.Version;
		fresh.Next = sidecar.Attributes;
		fresh.Attribute = attribute;
		fresh.Value = value;
		fresh.Generation = NextGeneration(sidecar.Generation);
		if (!MuiNativeObjectAttributeCodec.Write(ref platform, allocation, fresh))
		{
			platform.Free(allocation, MuiNativeObjectAttributeRecord.Size);
			return false;
		}
		sidecar.Attributes = allocation;
		sidecar.Generation = fresh.Generation;
		if (MuiNativeMuiObjectCodec.Write(ref platform, sidecarAddress, sidecar))
			return true;
		platform.Free(allocation, MuiNativeObjectAttributeRecord.Size);
		return false;
	}

	internal static bool SetOwnedTextAttribute(ref MuiNativeClassPlatform platform,
		APTR sidecarAddress, uint attribute, APTR text, uint textAllocationSize,
		bool notify)
	{
		if (attribute == NoNotifyAttribute ||
			attribute == NoNotifyMethodAttribute ||
			attribute == ObjectIdAttribute || attribute == UserDataAttribute ||
			(text.IsNull != (textAllocationSize == 0)) ||
			(text.IsNotNull && (textAllocationSize >
				MuiNotifySetAsStringCore.MaximumOutputLength + 1 ||
				!platform.IsMapped(text, textAllocationSize)))) return false;

		var memory = default(MuiNativeClassMemory);
		if (!MuiNativeMuiObjectCodec.TryRead(ref memory, sidecarAddress,
			out var sidecar) || sidecar.LifecycleState !=
			MuiNativeMuiObjectRecord.StateLive ||
			(sidecar.Flags & MuiNativeMuiObjectRecord.ObjectInitialized) == 0 ||
			(sidecar.Flags & MuiNativeMuiObjectRecord.ObjectDisposing) != 0)
			return false;

		var current = sidecar.Attributes;
		for (uint visited = 0; current.IsNotNull &&
			visited++ < MuiHeadlessLayout.MaximumTraversal; visited++)
		{
			if (!MuiNativeObjectAttributeCodec.TryRead(ref memory, current,
				out var record)) return false;
			if (record.Attribute != attribute)
			{
				current = record.Next;
				continue;
			}

			var previous = record;
			record.Value = text.Raw;
			record.OwnedText = text;
			record.OwnedTextSize = textAllocationSize;
			record.Generation = NextGeneration(sidecar.Generation);
			if (!MuiNativeObjectAttributeCodec.Write(ref memory, current, record))
				return false;
			sidecar.Generation = record.Generation;
			if (!MuiNativeMuiObjectCodec.Write(ref memory, sidecarAddress,
				sidecar))
			{
				MuiNativeObjectAttributeCodec.Write(ref memory, current, previous);
				return false;
			}
			if (previous.OwnedText.IsNotNull &&
				previous.OwnedText != text)
				platform.Free(previous.OwnedText, previous.OwnedTextSize);
			if (notify) DispatchAttributeChange(ref platform, sidecarAddress,
				attribute, text.Raw);
			return true;
		}
		if (current.IsNotNull) return false;

		var allocation = platform.Allocate(MuiNativeObjectAttributeRecord.Size,
			MuiHeadlessLayout.AllocationFlags);
		if (allocation.IsNull) return false;
		var fresh = default(MuiNativeObjectAttributeRecord);
		fresh.Signature = MuiNativeObjectAttributeRecord.Magic;
		fresh.Revision = MuiNativeObjectAttributeRecord.Version;
		fresh.Next = sidecar.Attributes;
		fresh.Attribute = attribute;
		fresh.Value = text.Raw;
		fresh.Generation = NextGeneration(sidecar.Generation);
		fresh.OwnedText = text;
		fresh.OwnedTextSize = textAllocationSize;
		if (!MuiNativeObjectAttributeCodec.Write(ref memory, allocation, fresh))
		{
			platform.Free(allocation, MuiNativeObjectAttributeRecord.Size);
			return false;
		}
		sidecar.Attributes = allocation;
		sidecar.Generation = fresh.Generation;
		if (!MuiNativeMuiObjectCodec.Write(ref memory, sidecarAddress, sidecar))
		{
			platform.Free(allocation, MuiNativeObjectAttributeRecord.Size);
			return false;
		}
		if (notify) DispatchAttributeChange(ref platform, sidecarAddress,
			attribute, text.Raw);
		return true;
	}

	internal static bool ApplyTags(ref MuiNativeClassPlatform platform,
		APTR sidecarAddress, APTR tags)
		=> ApplyTags(ref platform, sidecarAddress, tags, false);

	internal static bool ApplyTags(ref MuiNativeClassPlatform platform,
		APTR sidecarAddress, APTR tags, bool notify)
		=> ApplyTags(ref platform, sidecarAddress, tags, notify, APTR.Null,
			APTR.Null, APTR.Null);

	internal static bool ApplyTags(ref MuiNativeClassPlatform platform,
		APTR sidecarAddress, APTR tags, bool notify, APTR publicObjects,
		APTR ownerRoot, APTR obj)
	{
		if (tags.IsNull) return true;
		if (!TryReadNotificationSettings(ref platform, tags, out var noNotify,
			out var noNotifyMethod)) return false;
		var cursor = default(MuiAslTagItemCursor);
		cursor.Base = tags;
		var memory = default(MuiNativeClassMemory);
		if (!MuiNativeMuiObjectCodec.TryRead(ref memory, sidecarAddress,
			out var sidecar)) return false;
		var previousNoNotifyMethod = sidecar.NotifySuppressionMethod;
		if (noNotifyMethod != 0)
		{
			sidecar.NotifySuppressionMethod = noNotifyMethod;
			if (!MuiNativeMuiObjectCodec.Write(ref memory, sidecarAddress,
				sidecar)) return false;
		}
		uint visited = 0;
		var applied = false;
		while (cursor.Base.IsNotNull && visited++ <
			MuiAslTagListCore.MaximumSteps)
		{
			if (!MuiAslTagItemVectorCodec.TryRead(ref platform, cursor,
				out var item)) break;
			if (item.Tag == MuiAslTagListCore.TagDone)
			{
				applied = true;
				break;
			}
			if (item.Tag == MuiAslTagListCore.TagIgnore)
			{
				if (!MuiAslTagItemVectorCodec.TryAdvance(ref cursor, 1))
					break;
				continue;
			}
			if (item.Tag == MuiAslTagListCore.TagMore)
			{
				if (item.Data == 0)
				{
					applied = true;
					break;
				}
				cursor.Base = APTR.FromPointer(item.Data);
				cursor.Index = 0;
				continue;
			}
			if (item.Tag == MuiAslTagListCore.TagSkip)
			{
				if (item.Data == uint.MaxValue ||
					!MuiAslTagItemVectorCodec.TryAdvance(ref cursor,
						item.Data + 1u)) break;
				continue;
			}
			if (item.Tag == NoNotifyAttribute ||
				item.Tag == NoNotifyMethodAttribute)
			{
				if (!MuiAslTagItemVectorCodec.TryAdvance(ref cursor, 1))
					break;
				continue;
			}
			var attributeChanged = publicObjects.IsNotNull &&
				ownerRoot.IsNotNull && obj.IsNotNull
				? MuiNativePublicObjectCore.SetAttribute(ref platform,
					publicObjects, ownerRoot, obj, item.Tag, item.Data,
					notify && !noNotify)
				: SetAttribute(ref platform, sidecarAddress, item.Tag,
					item.Data, notify && !noNotify);
			if (!attributeChanged) break;
			if (!MuiAslTagItemVectorCodec.TryAdvance(ref cursor, 1))
				break;
		}
		if (!MuiNativeMuiObjectCodec.TryRead(ref memory, sidecarAddress,
			out sidecar)) return false;
		sidecar.NotifySuppressionMethod = previousNoNotifyMethod;
		if (!MuiNativeMuiObjectCodec.Write(ref memory, sidecarAddress,
			sidecar)) return false;
		return applied;
	}

	private static bool TryReadNotificationSettings(
		ref MuiNativeClassPlatform platform, APTR tags, out bool noNotify,
		out uint noNotifyMethod)
	{
		noNotify = false;
		noNotifyMethod = 0;
		if (tags.IsNull) return true;
		var cursor = default(MuiAslTagItemCursor);
		cursor.Base = tags;
		uint visited = 0;
		while (cursor.Base.IsNotNull && visited++ <
			MuiAslTagListCore.MaximumSteps)
		{
			if (!MuiAslTagItemVectorCodec.TryRead(ref platform, cursor,
				out var item)) return false;
			if (item.Tag == MuiAslTagListCore.TagDone) return true;
			if (item.Tag == MuiAslTagListCore.TagIgnore)
			{
				if (!MuiAslTagItemVectorCodec.TryAdvance(ref cursor, 1))
					return false;
				continue;
			}
			if (item.Tag == MuiAslTagListCore.TagMore)
			{
				if (item.Data == 0) return true;
				cursor.Base = APTR.FromPointer(item.Data);
				cursor.Index = 0;
				continue;
			}
			if (item.Tag == MuiAslTagListCore.TagSkip)
			{
				if (item.Data == uint.MaxValue ||
					!MuiAslTagItemVectorCodec.TryAdvance(ref cursor,
						item.Data + 1u)) return false;
				continue;
			}
			if (item.Tag == NoNotifyAttribute && item.Data != 0)
				noNotify = true;
			if (item.Tag == NoNotifyMethodAttribute)
				noNotifyMethod = item.Data;
			if (!MuiAslTagItemVectorCodec.TryAdvance(ref cursor, 1))
				return false;
		}
		return cursor.Base.IsNull;
	}

	private static void DispatchAttributeChange(
		ref MuiNativeClassPlatform platform, APTR sidecarAddress,
		uint attribute, uint value)
	{
		var memory = default(MuiNativeClassMemory);
		if (!MuiNativeMuiObjectCodec.TryRead(ref memory, sidecarAddress,
			out var state) || state.LifecycleState !=
			MuiNativeMuiObjectRecord.StateLive ||
			(state.Flags & MuiNativeMuiObjectRecord.ObjectDisposing) != 0 ||
			state.NotifyDepth >= MaximumNotificationDepth) return;
		state.NotifyDepth++;
		if (!MuiNativeMuiObjectCodec.Write(ref memory, sidecarAddress, state))
			return;
		var maximumSequence = state.Generation;
		var suppressedMethod = state.NotifySuppressionMethod;
		var commoditiesBase = APTR.Null;
		var triedCommodities = false;
		uint completed = 0;
		uint operations = 0;
		while (operations++ < MuiHeadlessLayout.MaximumTraversal)
		{
			// Re-read the named sidecar for every step. A destination may remove
			// the current notification or another node while its method runs; a
			// cached list head would otherwise revisit freed guest memory.
			if (!MuiNativeMuiObjectCodec.TryRead(ref memory, sidecarAddress,
				out var liveState)) break;
			var item = FindNextNotification(ref memory, liveState.Notifications,
				completed, maximumSequence);
			if (item.IsNull ||
				!MuiNativeObjectNotificationCodec.TryRead(ref memory, item,
					out var notification)) break;
			completed = notification.Sequence;
			if ((notification.Flags & MuiNativeObjectNotificationRecord.Active) == 0 ||
				notification.TriggerAttribute != attribute) continue;
			var matchesTrigger = false;
			if ((notification.Flags &
				MuiNativeObjectNotificationRecord.HasInputExpression) != 0)
			{
				var eventAddress = APTR.FromPointer(value);
				if (attribute != MuiWindowPublicCore.InputEvent ||
					eventAddress.IsNull || !memory.IsMapped(eventAddress,
						InputEvent.Size)) continue;
				if (!triedCommodities)
				{
					commoditiesBase = MuiNativeCommoditiesCalls.Open();
					triedCommodities = true;
				}
				if (commoditiesBase.IsNotNull)
					matchesTrigger = MuiNativeCommoditiesCalls.MatchIX(
						commoditiesBase, eventAddress,
						notification.InputExpression);
			}
			else
			{
				matchesTrigger = notification.TriggerValue ==
					NotificationEveryTime || notification.TriggerValue == value;
			}
			if (!matchesTrigger) continue;
			var destination = ResolveDestination(ref platform, ref memory,
				liveState, notification.Destination);
			if (destination.IsNull || !memory.IsMapped(destination, 1) ||
				notification.FollowCount == 0 ||
				notification.FollowCount > MaximumNotificationFollowCount)
				continue;
			var bytes = notification.FollowCount *
				MuiNativeObjectAttributeRecord.FieldSize;
			if (bytes / MuiNativeObjectAttributeRecord.FieldSize !=
				notification.FollowCount || notification.Payload.IsNull ||
				!memory.IsMapped(notification.Payload, bytes)) continue;
			var followCursor = default(MuiNotifyFollowParameterVectorCursor);
			followCursor.Base = notification.Payload;
			if (suppressedMethod != 0 &&
				MuiNotifyFollowParameterVectorCodec.TryReadValue(ref platform,
					followCursor, out var methodValue) &&
				methodValue == suppressedMethod) continue;
			var message = platform.Allocate(bytes, MuiHeadlessLayout.AllocationFlags);
			if (message.IsNull || !memory.IsMapped(message, bytes))
			{
				if (message.IsNotNull) platform.Free(message, bytes);
				continue;
			}
			platform.Copy(notification.Payload, message, bytes);
			var messageCursor = default(MuiNotifyFollowParameterVectorCursor);
			messageCursor.Base = message;
			var valid = true;
			for (var index = 0u; index < notification.FollowCount; index++)
			{
				if (index != 0 && !MuiNotifyFollowParameterVectorCodec.TryAdvance(
					ref messageCursor, 1))
				{
					valid = false;
					break;
				}
				if (!MuiNotifyFollowParameterVectorCodec.TryReadValue(ref platform,
					messageCursor, out var slotValue))
				{
					valid = false;
					break;
				}
				if (slotValue == NotificationTriggerValue)
					MuiNotifyFollowParameterVectorCodec.TryWriteValue(ref platform,
						messageCursor, value);
				else if (slotValue == NotificationNotTriggerValue)
					MuiNotifyFollowParameterVectorCodec.TryWriteValue(ref platform,
						messageCursor, value == 0 ? 1u : 0u);
			}
			if (valid) platform.DoMethod(destination, message);
			platform.Clear(message, bytes);
			platform.Free(message, bytes);
		}
		if (commoditiesBase.IsNotNull) Exec.CloseLibrary(commoditiesBase);
		if (!MuiNativeMuiObjectCodec.TryRead(ref memory, sidecarAddress,
			out state)) return;
		if (state.NotifyDepth != 0) state.NotifyDepth--;
		MuiNativeMuiObjectCodec.Write(ref memory, sidecarAddress, state);
	}

	private static APTR ResolveDestination(
		ref MuiNativeClassPlatform platform, ref MuiNativeClassMemory memory,
		MuiNativeMuiObjectRecord source, APTR destination)
	{
		if (destination.Raw == 1) return source.Object;
		if (destination.Raw <= 6)
		{
			if (destination.Raw < 4) return APTR.FromPointer(0);
			if (!TryGetPublicObjectState(ref memory, source.OwnerRoot,
				out var publicObjects)) return APTR.Null;
			var parent = source.Parent;
			var levels = destination.Raw - 3;
			while (levels-- != 0 && parent.IsNotNull)
			{
				if (!TryReadPublicSidecar(ref platform, publicObjects,
					source.OwnerRoot, parent, out var parentValue))
					return APTR.Null;
				parent = parentValue.Parent;
			}
			if (parent.IsNull) return APTR.FromPointer(0);
			return TryReadPublicSidecar(ref platform, publicObjects,
				source.OwnerRoot, parent, out var destinationValue)
				? destinationValue.Object : APTR.Null;
		}
		return destination;
	}

	private static bool TryGetPublicObjectState(
		ref MuiNativeClassMemory memory, APTR ownerRoot,
		out APTR publicObjects)
	{
		publicObjects = APTR.Null;
		if (!MuiMasterPrivateRootCodec.TryRead(ref memory, ownerRoot,
			out var root) || root.LoaderState == 0) return false;
		return MuiNativeClassOwnerCodec.TryGetPublicObjectStateAddress(ref memory,
			APTR.FromPointer(root.LoaderState), out publicObjects);
	}

	private static bool TryReadPublicSidecar(
		ref MuiNativeClassPlatform platform, APTR publicObjects,
		APTR ownerRoot, APTR obj, out MuiNativeMuiObjectRecord sidecar)
	{
		sidecar = default;
		if (!MuiNativePublicObjectCore.TryFindBinding(ref platform,
			publicObjects, ownerRoot, obj, out var binding) || binding.Sidecar.IsNull)
			return false;
		var memory = default(MuiNativeClassMemory);
		return MuiNativeMuiObjectCodec.TryRead(ref memory, binding.Sidecar,
			out sidecar) && sidecar.Object == obj &&
			sidecar.OwnerRoot == ownerRoot &&
			sidecar.LifecycleState == MuiNativeMuiObjectRecord.StateLive;
	}

	private static APTR FindNextNotification<TMemory>(ref TMemory memory,
		APTR head, uint afterSequence, uint maximumSequence)
		where TMemory : struct, IMuiGuestMemory
	{
		var current = head;
		var selected = APTR.Null;
		var selectedSequence = uint.MaxValue;
		uint visited = 0;
		while (current.IsNotNull && visited++ <
			MuiHeadlessLayout.MaximumTraversal)
		{
			if (!MuiNativeObjectNotificationCodec.TryRead(ref memory, current,
				out var notification)) return APTR.Null;
			if (notification.Sequence > afterSequence &&
				notification.Sequence <= maximumSequence &&
				notification.Sequence < selectedSequence)
			{
				selected = current;
				selectedSequence = notification.Sequence;
			}
			current = notification.Next;
		}
		return current.IsNull ? selected : APTR.Null;
	}

	// Install a validated notification node, parsing Window_InputEvent's
	// Commodities description immediately so the caller's string is not retained.
	internal static bool AddNotification(ref MuiNativeClassPlatform platform,
		APTR sidecarAddress, uint triggerAttribute, uint triggerValue,
		APTR destination, uint followCount, uint flags, out APTR notification)
	{
		if (followCount == 0)
			return AddNotification(ref platform, sidecarAddress,
				triggerAttribute, triggerValue, destination, 0, APTR.Null,
				flags, out notification);
		var bytes = followCount * MuiNativeObjectAttributeRecord.FieldSize;
		if (followCount > MaximumNotificationFollowCount ||
			bytes / MuiNativeObjectAttributeRecord.FieldSize != followCount)
		{
			notification = APTR.Null;
			return false;
		}
		var temporary = platform.Allocate(bytes,
			MuiHeadlessLayout.AllocationFlags);
		if (temporary.IsNull)
		{
			notification = APTR.Null;
			return false;
		}
		platform.Clear(temporary, bytes);
		var result = AddNotification(ref platform, sidecarAddress,
			triggerAttribute, triggerValue, destination, followCount,
			temporary, flags, out notification);
		platform.Free(temporary, bytes);
		return result;
	}

	internal static bool AddNotification(ref MuiNativeClassPlatform platform,
		APTR sidecarAddress, uint triggerAttribute, uint triggerValue,
		APTR destination, uint followCount, APTR followParameters, uint flags,
		out APTR notification)
	{
		notification = APTR.Null;
		if (destination.IsNull || triggerAttribute == NoNotifyAttribute ||
			triggerAttribute == NoNotifyMethodAttribute ||
			followCount > MaximumNotificationFollowCount) return false;
		var memory = default(MuiNativeClassMemory);
		if (!MuiNativeMuiObjectCodec.TryRead(ref memory, sidecarAddress,
			out var sidecar) || sidecar.LifecycleState !=
			MuiNativeMuiObjectRecord.StateLive ||
			(sidecar.Flags & MuiNativeMuiObjectRecord.ObjectInitialized) == 0 ||
			(sidecar.Flags & MuiNativeMuiObjectRecord.ObjectDisposing) != 0)
			return false;
		var payloadBytes = followCount * MuiNativeObjectAttributeRecord.FieldSize;
		if (payloadBytes / MuiNativeObjectAttributeRecord.FieldSize != followCount ||
			(followCount != 0 && followParameters.IsNull) ||
			(followCount != 0 && !platform.IsMapped(followParameters,
				payloadBytes))) return false;
		var inputExpression = APTR.Null;
		var notificationFlags = flags;
		if (triggerAttribute == MuiWindowPublicCore.InputEvent &&
			triggerValue != NotificationEveryTime)
		{
			if (!TryParseInputExpression(ref platform, triggerValue,
				out inputExpression)) return false;
			notificationFlags |=
				MuiNativeObjectNotificationRecord.HasInputExpression;
		}
		var allocation = platform.Allocate(
			MuiNativeObjectNotificationRecord.Size,
			MuiHeadlessLayout.AllocationFlags);
		if (allocation.IsNull)
		{
			if (inputExpression.IsNotNull)
				platform.Free(inputExpression, InputXpression.Size);
			return false;
		}
		var payload = APTR.Null;
		if (payloadBytes != 0)
		{
			payload = platform.Allocate(payloadBytes,
				MuiHeadlessLayout.AllocationFlags);
			if (payload.IsNull)
			{
				if (inputExpression.IsNotNull)
					platform.Free(inputExpression, InputXpression.Size);
				platform.Free(allocation, MuiNativeObjectNotificationRecord.Size);
				return false;
			}
			platform.Copy(followParameters, payload, payloadBytes);
		}
		var value = default(MuiNativeObjectNotificationRecord);
		value.Signature = MuiNativeObjectNotificationRecord.Magic;
		value.Revision = MuiNativeObjectNotificationRecord.Version;
		value.Next = sidecar.Notifications;
		value.Sequence = NextGeneration(sidecar.Generation);
		value.TriggerAttribute = triggerAttribute;
		// The borrowed descriptor is represented only by its parsed IX record.
		value.TriggerValue = inputExpression.IsNotNull ? 0 : triggerValue;
		value.Destination = destination;
		value.FollowCount = followCount;
		value.Flags = notificationFlags |
			MuiNativeObjectNotificationRecord.Active;
		value.Payload = payload;
		value.InputExpression = inputExpression;
		if (!MuiNativeObjectNotificationCodec.Write(ref memory, allocation,
			value))
		{
			if (payload.IsNotNull) platform.Free(payload, payloadBytes);
			if (inputExpression.IsNotNull)
				platform.Free(inputExpression, InputXpression.Size);
			platform.Free(allocation, MuiNativeObjectNotificationRecord.Size);
			return false;
		}
		sidecar.Notifications = allocation;
		sidecar.Generation = value.Sequence;
		if (!MuiNativeMuiObjectCodec.Write(ref memory, sidecarAddress,
			sidecar))
		{
			if (payload.IsNotNull) platform.Free(payload, payloadBytes);
			if (inputExpression.IsNotNull)
				platform.Free(inputExpression, InputXpression.Size);
			platform.Free(allocation, MuiNativeObjectNotificationRecord.Size);
			return false;
		}
		notification = allocation;
		return true;
	}

	private static bool TryParseInputExpression(
		ref MuiNativeClassPlatform platform, uint descriptionValue,
		out APTR inputExpression)
	{
		inputExpression = APTR.Null;
		var description = APTR.FromPointer(descriptionValue);
		var memory = default(MuiNativeClassMemory);
		if (!CStringCodec.TryReadLength(ref memory, description,
			MuiInputExpressionMemoryCodec.MaximumDescriptionLength, out _))
			return false;
		inputExpression = platform.Allocate(InputXpression.Size,
			MuiHeadlessLayout.AllocationFlags);
		if (inputExpression.IsNull) return false;
		platform.Clear(inputExpression, InputXpression.Size);
		var commoditiesBase = MuiNativeCommoditiesCalls.Open();
		if (commoditiesBase.IsNull)
		{
			platform.Free(inputExpression, InputXpression.Size);
			inputExpression = APTR.Null;
			return false;
		}
		var parseResult = MuiNativeCommoditiesCalls.ParseIX(commoditiesBase,
			description, inputExpression);
		Exec.CloseLibrary(commoditiesBase);
		if (parseResult == 0 && MuiInputExpressionMemoryCodec.TryRead(
			ref memory, inputExpression, out _)) return true;
		platform.Free(inputExpression, InputXpression.Size);
		inputExpression = APTR.Null;
		return false;
	}

	internal static uint RemoveNotifications(ref MuiNativeClassPlatform platform,
		APTR sidecarAddress, uint triggerAttribute, APTR destination,
		bool matchDestination)
	{
		var memory = default(MuiNativeClassMemory);
		if (!MuiNativeMuiObjectCodec.TryRead(ref memory, sidecarAddress,
			out var sidecar) || sidecar.LifecycleState !=
			MuiNativeMuiObjectRecord.StateLive ||
			(sidecar.Flags & MuiNativeMuiObjectRecord.ObjectInitialized) == 0 ||
			(sidecar.Flags & MuiNativeMuiObjectRecord.ObjectDisposing) != 0 ||
			!ValidateNotifications(ref memory, sidecar.Notifications)) return 0;
		var current = sidecar.Notifications;
		var previous = APTR.Null;
		uint removed = 0;
		while (current.IsNotNull)
		{
			if (!MuiNativeObjectNotificationCodec.TryRead(ref memory, current,
				out var value)) return removed;
			var next = value.Next;
			var matches = value.TriggerAttribute == triggerAttribute &&
				(!matchDestination || value.Destination == destination);
			if (matches)
			{
				if (previous.IsNull) sidecar.Notifications = next;
				else
				{
					if (!MuiNativeObjectNotificationCodec.TryRead(ref memory,
						previous, out var previousValue)) return removed;
					previousValue.Next = next;
					if (!MuiNativeObjectNotificationCodec.Write(ref memory,
						previous, previousValue)) return removed;
				}
				FreeNotification(ref platform, current, value);
				removed++;
			}
			else previous = current;
			current = next;
		}
		if (removed != 0 && !MuiNativeMuiObjectCodec.Write(ref memory,
			sidecarAddress, sidecar)) return 0;
		return removed;
	}

	internal static bool TryHandleNotifyMethod(ref MuiNativeClassPlatform platform,
		APTR publicObjects, APTR ownerRoot, APTR obj, APTR message,
		uint method, out uint result)
	{
		result = 0;
		if (method == MuiNotifyCore.NotifyMethod)
		{
			if (!MuiNotifyCore.TryReadNotify(ref platform, message, method,
				out var packet)) return true;
			var followParameters = MuiNotifyCore.FollowParameters(ref platform,
				message);
			result = MuiNativePublicObjectCore.AddNotification(ref platform,
				publicObjects, ownerRoot, obj,
				packet.TriggerAttribute, packet.TriggerValue,
				APTR.FromPointer(packet.Destination), packet.FollowCount,
				followParameters, 0, out _) ? 1u : 0u;
			return true;
		}
		if (method == MuiNotifyCore.KillNotifyMethod)
		{
			if (!MuiNotifyCore.TryReadKillNotify(ref platform, message, method,
				out var packet)) return true;
			if (!MuiNativePublicObjectCore.TryFindLive(ref platform,
				publicObjects, ownerRoot, obj, out var binding)) return true;
			result = RemoveNotifications(ref platform, binding.Sidecar,
				packet.TriggerAttribute, APTR.Null, false);
			return true;
		}
		if (method == MuiNotifyCore.KillNotifyObjectMethod)
		{
			if (!MuiNotifyCore.TryReadKillNotifyObject(ref platform, message,
				method, out var packet)) return true;
			if (!MuiNativePublicObjectCore.TryFindLive(ref platform,
				publicObjects, ownerRoot, obj, out var binding)) return true;
			result = RemoveNotifications(ref platform, binding.Sidecar,
				packet.TriggerAttribute, APTR.FromPointer(packet.Destination),
				true);
			return true;
		}
		return false;
	}

	// Validate both roots before clearing either one.  If validation or the
	// sidecar write fails, disposal retains the complete list for a later retry.
	internal static bool ReleaseLists<T>(ref T memory, APTR sidecarAddress)
		where T : struct, IMuiGuestMemory
	{
		if (!MuiNativeMuiObjectCodec.TryRead(ref memory, sidecarAddress,
			out var sidecar) || !ValidateAttributes(ref memory,
			sidecar.Attributes) || !ValidateNotifications(ref memory,
			sidecar.Notifications) || !MuiNativeReturnIdQueue.Validate(ref memory,
			sidecar) || !MuiNativeApplicationInputHandlerQueue.Validate(ref memory,
			sidecar.InputHandlers, sidecar.InputHandlerGeneration,
			sidecar.InputTimerPort) ||
			!MuiNativeApplicationPushMethodQueue.Validate(ref memory, sidecar) ||
			!MuiNativeWindowEventHandlerQueue.Validate(ref memory, sidecar))
			return false;
		var attributes = sidecar.Attributes;
		var notifications = sidecar.Notifications;
		var returnIds = sidecar.ReturnIdQueue;
		var inputHandlers = sidecar.InputHandlers;
		var inputTimerPort = sidecar.InputTimerPort;
		var applicationPushQueue = sidecar.ApplicationPushQueue;
		var windowEventHandlers = sidecar.WindowEventHandlers;
		sidecar.Attributes = APTR.Null;
		sidecar.Notifications = APTR.Null;
		sidecar.ReturnIdQueue = APTR.Null;
		sidecar.InputHandlers = APTR.Null;
		sidecar.InputTimerPort = APTR.Null;
		sidecar.ApplicationPushQueue = APTR.Null;
		sidecar.ApplicationPushGeneration = 0;
		sidecar.WindowEventHandlers = APTR.Null;
		sidecar.WindowEventHandlerGeneration = 0;
		sidecar.InputSignalTask = APTR.Null;
		sidecar.InputSignalMask = 0;
		if (!MuiNativeMuiObjectCodec.Write(ref memory, sidecarAddress,
			sidecar)) return false;
		if (!MuiNativeWindowEventHandlerQueue.DetachValidated(ref memory,
			windowEventHandlers)) return false;
		FreeAttributes(ref memory, attributes);
		FreeNotifications(ref memory, notifications);
		FreeReturnIds(ref memory, returnIds);
		MuiNativeApplicationInputHandlerQueue.FreeValidated(ref memory,
			inputHandlers, inputTimerPort);
		MuiNativeApplicationPushMethodQueue.FreeValidated(ref memory,
			applicationPushQueue);
		return true;
	}

	private static void FreeReturnIds<T>(ref T memory, APTR head)
		where T : struct, IMuiGuestMemory
	{
		var current = head;
		for (uint visited = 0; current.IsNotNull && visited <
			MuiHeadlessLayout.MaximumTraversal; visited++)
		{
			if (!MuiNativeReturnIdRecordCodec.TryRead(ref memory, current,
				out var record)) return;
			Exec.FreeMem(current, MuiNativeReturnIdRecord.Size);
			current = record.Next;
		}
	}

	private static bool ValidateAttributes<T>(ref T memory, APTR head)
		where T : struct, IMuiGuestMemory
	{
		var current = head;
		for (uint visited = 0; current.IsNotNull &&
			visited++ < MuiHeadlessLayout.MaximumTraversal; visited++)
		{
		if (!MuiNativeObjectAttributeCodec.TryRead(ref memory, current,
			out var value) || value.Next == current ||
			(value.OwnedText.IsNull != (value.OwnedTextSize == 0)) ||
			(value.OwnedText.IsNotNull && (value.OwnedTextSize >
				MuiNotifySetAsStringCore.MaximumOutputLength + 1 ||
				!memory.IsMapped(value.OwnedText, value.OwnedTextSize))))
			return false;
			current = value.Next;
		}
		return current.IsNull;
	}

	private static bool ValidateNotifications<T>(ref T memory, APTR head)
		where T : struct, IMuiGuestMemory
	{
		var current = head;
		for (uint visited = 0; current.IsNotNull &&
			visited++ < MuiHeadlessLayout.MaximumTraversal; visited++)
		{
			if (!MuiNativeObjectNotificationCodec.TryRead(ref memory, current,
				out var value) || value.Next == current ||
				value.FollowCount > MaximumNotificationFollowCount ||
				(value.FollowCount != 0 && (value.Payload.IsNull ||
					!memory.IsMapped(value.Payload,
						value.FollowCount * MuiNativeObjectAttributeRecord.FieldSize))))
				return false;
			current = value.Next;
		}
		return current.IsNull;
	}

	private static void FreeAttributes<T>(ref T memory, APTR head)
		where T : struct, IMuiGuestMemory
	{
		var current = head;
		for (uint visited = 0; current.IsNotNull &&
			visited++ < MuiHeadlessLayout.MaximumTraversal; visited++)
		{
			if (!MuiNativeObjectAttributeCodec.TryRead(ref memory, current,
				out var value)) return;
			if (value.OwnedText.IsNotNull)
				Exec.FreeMem(value.OwnedText, value.OwnedTextSize);
			Exec.FreeMem(current, MuiNativeObjectAttributeRecord.Size);
			current = value.Next;
		}
	}

	private static void FreeNotifications<T>(ref T memory, APTR head)
		where T : struct, IMuiGuestMemory
	{
		var current = head;
		for (uint visited = 0; current.IsNotNull &&
			visited++ < MuiHeadlessLayout.MaximumTraversal; visited++)
		{
			if (!MuiNativeObjectNotificationCodec.TryRead(ref memory, current,
				out var value)) return;
			var payloadBytes = value.FollowCount *
				MuiNativeObjectAttributeRecord.FieldSize;
			if (value.Payload.IsNotNull && payloadBytes != 0)
				Exec.FreeMem(value.Payload, payloadBytes);
			if (value.InputExpression.IsNotNull)
				Exec.FreeMem(value.InputExpression, InputXpression.Size);
			Exec.FreeMem(current, MuiNativeObjectNotificationRecord.Size);
			current = value.Next;
		}
	}

	private static void FreeNotification(ref MuiNativeClassPlatform platform,
		APTR address, MuiNativeObjectNotificationRecord value)
	{
		var payloadBytes = value.FollowCount *
			MuiNativeObjectAttributeRecord.FieldSize;
		if (value.Payload.IsNotNull && payloadBytes != 0)
			platform.Free(value.Payload, payloadBytes);
		if (value.InputExpression.IsNotNull)
			platform.Free(value.InputExpression, InputXpression.Size);
		platform.Free(address, MuiNativeObjectNotificationRecord.Size);
	}

	private static uint NextGeneration(uint value) => value == uint.MaxValue
		? uint.MaxValue : value + 1;
}
