using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

// Public MUI_NewObjectA must run the same class-aware construction
// normalization as the common-control creation path. This guards the
// production factory boundary rather than only testing CommonControlCore in
// isolation.
public sealed class MuiObjectFactoryCommonControlTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);
	private static readonly APTR ClassName = APTR.FromPointer(0x1100);
	private static readonly APTR Format = APTR.FromPointer(0x1200);
	private static readonly APTR Tags = APTR.FromPointer(0x1300);
	private const uint NumericMin = 0x8042E404u;
	private const uint NumericMax = 0x8042D78Au;
	private const uint NumericValue = 0x8042AE3Au;
	private const uint NumericFormat = 0x804263E9u;

	[Fact]
	public void PublicFactoryRunsCommonControlConstructionNormalization()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x8000,
			State);
		platform.WriteCString(ClassName, "Numeric.mui");
		platform.WriteCString(Format, "value=%ld");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		Assert.True(MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform, State,
			ClassName, APTR.Null, 0, APTR.FromPointer(1)).IsNotNull);

		// CreateObjectA applies these raw tags first. Construct must then enforce
		// the Numeric range and move the caller-owned format into the object store.
		platform.WriteUInt32(Tags, 0, NumericMin);
		platform.WriteUInt32(Tags, 4, 10);
		platform.WriteUInt32(Tags, 8, NumericMax);
		platform.WriteUInt32(Tags, 12, 20);
		platform.WriteUInt32(Tags, 16, NumericValue);
		platform.WriteUInt32(Tags, 20, 99);
		platform.WriteUInt32(Tags, 24, NumericFormat);
		platform.WriteUInt32(Tags, 28, Format.Raw);
		platform.WriteUInt32(Tags, 32, MuiAslTagListCore.TagDone);
		platform.WriteUInt32(Tags, 36, 0);

		var obj = MuiObjectFactoryServiceCore.NewObjectA(ref platform, State,
			ClassName, Tags);
		Assert.True(obj.IsNotNull);
		Assert.Equal(MuiControlClass.Numeric,
			MuiCommonControlCore.Classify(ref platform, State, obj));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, obj,
			NumericValue, out var value));
		Assert.Equal(20u, value); // construction clamps the out-of-range value
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, obj,
			NumericFormat, out var copiedFormat));
		Assert.NotEqual(Format.Raw, copiedFormat);
		Assert.Equal((byte)'v', platform.ReadUInt8(APTR.FromPointer(copiedFormat), 0));
		Assert.Equal((byte)'=', platform.ReadUInt8(APTR.FromPointer(copiedFormat), 5));

		Assert.True(MuiObjectDisposalServiceCore.DisposeObject(ref platform, State,
			obj));
	}

	[Fact]
	public void ClassServiceFactoryRunsCommonControlConstructionNormalization()
	{
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x8000,
			State);
		var serviceState = APTR.FromPointer(0x1080);
		platform.WriteCString(ClassName, "Numeric.mui");
		platform.WriteCString(Format, "value=%ld");
		Assert.True(MuiHeadlessObjectCore.Initialize(ref platform, State));
		Assert.True(MuiClassServiceCore.Initialize(ref platform, serviceState,
			State));
		Assert.True(MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform, State,
			ClassName, APTR.Null, 0, APTR.FromPointer(1)).IsNotNull);

		// The class-service factory shares the same raw-tag and adoption seam as
		// MUI_NewObjectA, while also holding a builtin class lease until disposal.
		platform.WriteUInt32(Tags, 0, NumericMin);
		platform.WriteUInt32(Tags, 4, 10);
		platform.WriteUInt32(Tags, 8, NumericMax);
		platform.WriteUInt32(Tags, 12, 20);
		platform.WriteUInt32(Tags, 16, NumericValue);
		platform.WriteUInt32(Tags, 20, 99);
		platform.WriteUInt32(Tags, 24, NumericFormat);
		platform.WriteUInt32(Tags, 28, Format.Raw);
		platform.WriteUInt32(Tags, 32, MuiAslTagListCore.TagDone);
		platform.WriteUInt32(Tags, 36, 0);

		var obj = MuiObjectFactoryServiceCore.NewObjectAWithClassService(ref platform,
			serviceState, State, ClassName, Tags);
		Assert.True(obj.IsNotNull);
		Assert.Equal(MuiControlClass.Numeric,
			MuiCommonControlCore.Classify(ref platform, State, obj));
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, obj,
			NumericValue, out var value));
		Assert.Equal(20u, value);
		Assert.True(MuiHeadlessObjectCore.GetAttribute(ref platform, State, obj,
			NumericFormat, out var copiedFormat));
		Assert.NotEqual(Format.Raw, copiedFormat);
		Assert.Equal((byte)'v', platform.ReadUInt8(APTR.FromPointer(copiedFormat), 0));

		var classRecord = MuiHeadlessObjectCore.FindClassByName(ref platform, State,
			ClassName);
		var classPointer = MuiHeadlessObjectCore.ClassPointer(ref platform,
			classRecord);
		Assert.Equal(1u, MuiClassServiceCore.ObjectLeaseCount(ref platform,
			serviceState, classPointer));
		Assert.True(MuiObjectDisposalServiceCore.DisposeObject(ref platform,
			serviceState, State, obj));
		Assert.Equal(0u, MuiClassServiceCore.ObjectLeaseCount(ref platform,
			serviceState, classPointer));
	}
}
