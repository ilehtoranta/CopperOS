using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiRequesterApplicationGadgetsTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);
	private static readonly APTR Gadgets = APTR.FromPointer(0x1200);

	[Fact]
	public void ApplicationPlanOwnsLabelsAndPreservesMorphosButtonIds()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		WriteCString(ref platform, Gadgets, "_Save|*_Use|_Cancel");
		var allocationsBefore = platform.AllocationCount;
		var freesBefore = platform.FreeCount;

		Assert.True(MuiRequesterApplicationGadgetPlanCore.TryPrepare(
			ref platform, Gadgets, out var plan));
		Assert.Equal(3u, plan.ButtonCount);
		Assert.Equal(2u, plan.ActiveReturnId);
		Assert.Equal(1u, plan.HasActiveButton);
		Assert.True(plan.ButtonsSize >= 3u *
			MuiRequesterApplicationButtonRecord.Size);

		AssertButton(ref platform, plan, 0, "_Save", 1, (uint)'S', false);
		AssertButton(ref platform, plan, 1, "_Use", 2, (uint)'U', true);
		AssertButton(ref platform, plan, 2, "_Cancel", 0, (uint)'C', false);

		MuiRequesterApplicationGadgetPlanCore.Release(ref platform, ref plan);
		Assert.True(plan.Buttons.IsNull);
		Assert.True(plan.Labels.IsNull);
		Assert.Equal(platform.AllocationCount - allocationsBefore,
			platform.FreeCount - freesBefore);
		var freesAfterRelease = platform.FreeCount;
		MuiRequesterApplicationGadgetPlanCore.Release(ref platform, ref plan);
		Assert.Equal(freesAfterRelease, platform.FreeCount);
	}

	[Fact]
	public void ApplicationPlanMapsFinalActiveButtonToZero()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		WriteCString(ref platform, Gadgets, "One|*Two");

		Assert.True(MuiRequesterApplicationGadgetPlanCore.TryPrepare(
			ref platform, Gadgets, out var plan));
		Assert.Equal(0u, plan.ActiveReturnId);
		AssertButton(ref platform, plan, 0, "One", 1, 0, false);
		AssertButton(ref platform, plan, 1, "Two", 0, 0, true);
		MuiRequesterApplicationGadgetPlanCore.Release(ref platform, ref plan);
	}

	[Fact]
	public void ApplicationPlanPreservesUppercaseControlChar()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		WriteCString(ref platform, Gadgets, "_Xray");

		Assert.True(MuiRequesterApplicationGadgetPlanCore.TryPrepare(
			ref platform, Gadgets, out var plan));
		AssertButton(ref platform, plan, 0, "_Xray", 0, (uint)'X', false);
		MuiRequesterApplicationGadgetPlanCore.Release(ref platform, ref plan);
	}

	[Fact]
	public void ApplicationButtonRecordRetainsItsNativeObjectAsANamedField()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		WriteCString(ref platform, Gadgets, "Okay|Cancel");
		Assert.True(MuiRequesterApplicationGadgetPlanCore.TryPrepare(
			ref platform, Gadgets, out var plan));
		var buttonObject = platform.Allocate(4,
			MuiHeadlessLayout.AllocationFlags);
		Assert.True(buttonObject.IsNotNull);
		Assert.True(MuiRequesterApplicationButtonArrayCodec.TryRead(ref platform,
			plan.Buttons, plan.ButtonCount, 0, out var button));
		button.Object = buttonObject;
		Assert.True(MuiRequesterApplicationButtonArrayCodec.TryWrite(ref platform,
			plan.Buttons, plan.ButtonCount, 0, button));
		Assert.True(MuiRequesterApplicationButtonArrayCodec.TryRead(ref platform,
			plan.Buttons, plan.ButtonCount, 0, out button));
		Assert.Equal(buttonObject, button.Object);

		MuiRequesterApplicationGadgetPlanCore.Release(ref platform, ref plan);
		platform.Free(buttonObject, 4);
	}

	[Fact]
	public void ApplicationPlanRejectsAmbiguousActiveButtonsBeforeAllocation()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			State);
		WriteCString(ref platform, Gadgets, "*One|*Two");
		var allocationsBefore = platform.AllocationCount;

		Assert.False(MuiRequesterApplicationGadgetPlanCore.TryPrepare(
			ref platform, Gadgets, out var plan));
		Assert.True(plan.Buttons.IsNull);
		Assert.True(plan.Labels.IsNull);
		Assert.Equal(allocationsBefore, platform.AllocationCount);
	}

	private static void AssertButton(ref MuiHeadlessTestPlatform platform,
		MuiRequesterApplicationGadgetPlanRecord plan, uint index, string label,
		uint returnId, uint controlChar, bool active)
	{
		Assert.True(MuiRequesterApplicationButtonArrayCodec.TryRead(ref platform,
			plan.Buttons, plan.ButtonCount, index, out var button));
		Assert.Equal(label, ReadCString(ref platform, button.Label));
		Assert.Equal((uint)label.Length, button.LabelLength);
		Assert.Equal(returnId, button.ReturnId);
		Assert.Equal(controlChar, button.ControlChar);
		Assert.Equal(active ? 1u : 0u, button.IsActive);
	}

	private static void WriteCString(ref MuiHeadlessTestPlatform platform,
		APTR address, string value)
	{
		var cursor = default(MuiRequesterOutputByteCursor);
		cursor.Base = address;
		cursor.Capacity = unchecked((uint)value.Length + 1);
		for (var index = 0; index < value.Length; index++)
		{
			cursor.Index = unchecked((uint)index);
			Assert.True(MuiRequesterOutputByteCursorCodec.TryWriteByte(
				ref platform, cursor, unchecked((byte)value[index])));
		}
		cursor.Index = unchecked((uint)value.Length);
		Assert.True(MuiRequesterOutputByteCursorCodec.TryWriteByte(ref platform,
			cursor, 0));
	}

	private static string ReadCString(ref MuiHeadlessTestPlatform platform,
		APTR address)
	{
		var value = string.Empty;
		for (var index = 0u; index < MuiRequesterPayloadCore.MaximumStringLength;
			index++)
		{
			var cursor = default(MuiRequesterPayloadByteCursor);
			cursor.Base = address;
			cursor.Length = MuiRequesterPayloadCore.MaximumStringLength;
			cursor.Index = index;
			Assert.True(MuiRequesterPayloadByteCursorCodec.TryReadByte(
				ref platform, cursor, out var next));
			if (next == 0) return value;
			value += (char)next;
		}
		throw new Xunit.Sdk.XunitException("Requester label is not terminated.");
	}
}
