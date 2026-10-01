using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiNativeApplicationSleepTests
{
	private static readonly APTR Sidecar = APTR.FromPointer(0x1100);
	private static readonly APTR SleepAttribute = APTR.FromPointer(0x1200);
	private static readonly APTR DepthAttribute = APTR.FromPointer(0x1220);
	private static readonly APTR SavedDisabledAttribute =
		APTR.FromPointer(0x1240);
	private static readonly APTR DisabledAttribute = APTR.FromPointer(0x1260);

	[Fact]
	public void NativeWindowSleepDepthReadsNamedPublicAndPrivateAttributes()
	{
		var memory = CreateMemory();
		var sidecar = default(MuiNativeMuiObjectRecord);
		sidecar.Signature = MuiNativeMuiObjectRecord.Magic;
		sidecar.Revision = MuiNativeMuiObjectRecord.Version;
		sidecar.Flags = MuiNativeMuiObjectRecord.ObjectInitialized;
		sidecar.Attributes = SleepAttribute;
		Assert.True(MuiNativeMuiObjectCodec.Write(ref memory, Sidecar, sidecar));
		Assert.True(MuiNativeObjectAttributeCodec.Write(ref memory, SleepAttribute,
			Attribute(SleepAttribute, MuiWindowPublicCore.Sleep, 2,
				DepthAttribute)));
		Assert.True(MuiNativeObjectAttributeCodec.Write(ref memory, DepthAttribute,
			Attribute(DepthAttribute, 0x7FFE003D, 2,
				SavedDisabledAttribute)));
		Assert.True(MuiNativeObjectAttributeCodec.Write(ref memory,
			SavedDisabledAttribute, Attribute(SavedDisabledAttribute,
				0x7FFE003E, 0, DisabledAttribute)));
		Assert.True(MuiNativeObjectAttributeCodec.Write(ref memory,
			DisabledAttribute, Attribute(DisabledAttribute,
				MuiCommonControlCore.Disabled, 1, APTR.Null)));

		Assert.True(MuiNativeApplicationSleep.TryReadWindowSleepDepth(
			ref memory, Sidecar, out var depth));
		Assert.Equal(2u, depth);
	}

	[Fact]
	public void NativeWindowSleepDepthRejectsCounterDisagreement()
	{
		var memory = CreateMemory();
		var sidecar = default(MuiNativeMuiObjectRecord);
		sidecar.Signature = MuiNativeMuiObjectRecord.Magic;
		sidecar.Revision = MuiNativeMuiObjectRecord.Version;
		sidecar.Flags = MuiNativeMuiObjectRecord.ObjectInitialized;
		sidecar.Attributes = SleepAttribute;
		Assert.True(MuiNativeMuiObjectCodec.Write(ref memory, Sidecar, sidecar));
		Assert.True(MuiNativeObjectAttributeCodec.Write(ref memory, SleepAttribute,
			Attribute(SleepAttribute, MuiWindowPublicCore.Sleep, 1,
				DepthAttribute)));
		Assert.True(MuiNativeObjectAttributeCodec.Write(ref memory, DepthAttribute,
			Attribute(DepthAttribute, 0x7FFE003D, 2, APTR.Null)));

		Assert.False(MuiNativeApplicationSleep.TryReadWindowSleepDepth(
			ref memory, Sidecar, out var depth));
		Assert.Equal(0u, depth);
	}

	[Fact]
	public void NativeWindowWithoutSleepAttributesStartsAwake()
	{
		var memory = CreateMemory();
		var sidecar = default(MuiNativeMuiObjectRecord);
		sidecar.Signature = MuiNativeMuiObjectRecord.Magic;
		sidecar.Revision = MuiNativeMuiObjectRecord.Version;
		sidecar.Flags = MuiNativeMuiObjectRecord.ObjectInitialized;
		Assert.True(MuiNativeMuiObjectCodec.Write(ref memory, Sidecar, sidecar));

		Assert.True(MuiNativeApplicationSleep.TryReadWindowSleepDepth(
			ref memory, Sidecar, out var depth));
		Assert.Equal(0u, depth);
	}

	[Fact]
	public void SleepClassAdmissionWalksTypedIClassSuperclassChain()
	{
		var memory = new MuiHeadlessTestPlatform(0x1000, 0x8000, 0,
			APTR.FromPointer(0x1000));
		var applicationName = APTR.FromPointer(0x1400);
		var subclassName = APTR.FromPointer(0x1420);
		var windowName = APTR.FromPointer(0x1440);
		var applicationClassAddress = APTR.FromPointer(0x1100);
		var subclassAddress = APTR.FromPointer(0x1180);
		var windowClassAddress = APTR.FromPointer(0x1200);
		memory.WriteCString(applicationName, "Application.mui");
		memory.WriteCString(subclassName, "CopperApplication.mcc");
		memory.WriteCString(windowName, "Window.mui");
		BOOPSIGuestCodec.WriteClass(ref memory, applicationClassAddress,
			new IClass { cl_ID = applicationName });
		BOOPSIGuestCodec.WriteClass(ref memory, subclassAddress, new IClass
		{
			cl_ID = subclassName,
			cl_Super = applicationClassAddress,
		});
		BOOPSIGuestCodec.WriteClass(ref memory, windowClassAddress,
			new IClass { cl_ID = windowName });

		Assert.True(MuiNativeApplicationSleep.TryClassIsOrDerives(ref memory,
			subclassAddress, 0xC243A52E, out var isApplication));
		Assert.True(isApplication);
		Assert.True(MuiNativeApplicationSleep.TryIsApplicationClass(ref memory,
			subclassAddress, out isApplication));
		Assert.True(isApplication);
		Assert.True(MuiNativeApplicationSleep.TryClassIsOrDerives(ref memory,
			subclassAddress, 0x61DACF36, out var isWindow));
		Assert.False(isWindow);
		Assert.True(MuiNativeApplicationSleep.TryClassIsOrDerives(ref memory,
			windowClassAddress, 0x61DACF36, out isWindow));
		Assert.True(isWindow);
		Assert.True(MuiNativeApplicationSleep.TryIsWindowClass(ref memory,
			windowClassAddress, out isWindow));
		Assert.True(isWindow);
	}

	[Fact]
	public void InitialSleepTagReadsLastEffectiveValueAcrossTagLists()
	{
		var memory = CreateMemory();
		var tags = APTR.FromPointer(0x1800);
		var moreTags = APTR.FromPointer(0x1900);
		Assert.True(WriteTag(ref memory, tags, 0,
			MuiAslTagListCore.TagIgnore, 0));
		Assert.True(WriteTag(ref memory, tags, 1,
			MuiAslTagListCore.TagSkip, 1));
		Assert.True(WriteTag(ref memory, tags, 2,
			MuiWindowPublicCore.Sleep, 9));
		Assert.True(WriteTag(ref memory, tags, 3,
			MuiWindowPublicCore.Sleep, 3));
		Assert.True(WriteTag(ref memory, tags, 4,
			MuiAslTagListCore.TagMore, moreTags.Raw));
		Assert.True(WriteTag(ref memory, moreTags, 0,
			MuiWindowPublicCore.Sleep, 1));
		Assert.True(WriteTag(ref memory, moreTags, 1,
			MuiAslTagListCore.TagDone, 0));

		Assert.True(MuiNativeApplicationSleep.TryGetLastTagValue(ref memory,
			tags, MuiWindowPublicCore.Sleep, out var found, out var value));
		Assert.True(found);
		Assert.Equal(1u, value);
	}

	private static MuiNativeObjectAttributeRecord Attribute(APTR address,
		uint attribute, uint value, APTR next) => new()
	{
		Signature = MuiNativeObjectAttributeRecord.Magic,
		Revision = MuiNativeObjectAttributeRecord.Version,
		Next = next,
		Attribute = attribute,
		Value = value,
	};

	private static MuiHeadlessTestPlatform CreateMemory() =>
		new(0x1000, 0x4000, 0, APTR.FromPointer(0x1000));

	private static bool WriteTag(ref MuiHeadlessTestPlatform memory, APTR tags,
		uint index, uint tag, uint data) => MuiAslTagItemVectorCodec.TryWrite(
		ref memory, tags, index, new MuiAslTagItemRecord { Tag = tag, Data = data });
}
