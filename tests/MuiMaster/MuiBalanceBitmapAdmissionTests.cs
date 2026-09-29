using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiBalanceBitmapAdmissionTests
{
	private static readonly APTR State = APTR.FromPointer(0x1000);

	[Fact]
	public void BalanceAndBitmapRecordsRoundTripThroughNamedStructs()
	{
		var platform = CreatePlatform();
		var balanceAddress = APTR.FromPointer(0x1500);
		var policyAddress = APTR.FromPointer(0x1520);
		var remappedAddress = APTR.FromPointer(0x1560);
		var balance = new MuiBalancePolicyStateRecord
		{
			Magic = MuiBalancePolicyStateRecord.Cookie,
			Quiet = uint.MaxValue,
		};
		var policy = new MuiBitmapPolicyStateRecord
		{
			Magic = MuiBitmapPolicyStateRecord.Cookie,
			Alpha = 0x11223344,
			MappingTable = 0x1800,
			Precision = 0x55667788,
			SourceColors = 0x1840,
			Transparent = 0x99AABBCC,
			UseFriend = 1,
		};
		var remapped = new MuiBitmapRemappedStateRecord
		{
			Magic = MuiBitmapRemappedStateRecord.Cookie,
			Remapped = APTR.FromPointer(0x1880),
		};

		Assert.True(MuiBalancePolicyStateRecordCodec.Write(ref platform,
			balanceAddress, balance));
		Assert.True(MuiBitmapPolicyStateRecordCodec.Write(ref platform,
			policyAddress, policy));
		Assert.True(MuiBitmapRemappedStateRecordCodec.Write(ref platform,
			remappedAddress, remapped));
		Assert.True(MuiBalancePolicyStateRecordCodec.TryRead(ref platform,
			balanceAddress, out var balanceRead));
		Assert.True(MuiBitmapPolicyStateRecordCodec.TryRead(ref platform,
			policyAddress, out var policyRead));
		Assert.True(MuiBitmapRemappedStateRecordCodec.TryRead(ref platform,
			remappedAddress, out var remappedRead));
		Assert.Equal(balance.Quiet, balanceRead.Quiet);
		Assert.Equal(policy.Alpha, policyRead.Alpha);
		Assert.Equal(policy.MappingTable, policyRead.MappingTable);
		Assert.Equal(policy.SourceColors, policyRead.SourceColors);
		Assert.Equal(policy.Transparent, policyRead.Transparent);
		Assert.Equal(policy.UseFriend, policyRead.UseFriend);
		Assert.Equal(remapped.Remapped, remappedRead.Remapped);
	}

	[Fact]
	public void BalancePolicyRecordUsesDedicatedStructMemoryAdapter()
	{
		var platform = CreatePlatform();
		var address = APTR.FromPointer(0x15A0);
		var value = new MuiBalancePolicyStateRecord
		{
			Magic = MuiBalancePolicyStateRecord.Cookie,
			Quiet = uint.MaxValue,
		};

		Assert.True(MuiBalancePolicyStateRecordCodec.Write(ref platform, address,
			value));
		Assert.True(MuiBalancePolicyStateRecordCodec.TryReadStructural(ref platform,
			address, out var structural));
		Assert.Equal(value.Magic, structural.Magic);
		Assert.Equal(value.Quiet, structural.Quiet);
		Assert.True(MuiBalancePolicyStateRecordCodec.TryRead(ref platform, address,
			out _));
		Assert.True(MuiBalancePolicyStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiBalancePolicyStateField.Quiet,
			out var typedQuietAddress));
		Assert.Equal(address.Raw + MuiBalancePolicyStateRecord.QuietOffset,
			typedQuietAddress.Raw);
		var quietCursor = new MuiBalancePolicyStateFieldCursor
		{
			Record = address,
			Field = MuiBalancePolicyStateField.Quiet,
		};
		Assert.True(MuiBalancePolicyStateFieldCursorCodec.TryGetAddress(
			ref platform, quietCursor, out var cursorQuietAddress,
			out var fieldSize));
		Assert.Equal(typedQuietAddress, cursorQuietAddress);
		Assert.Equal(MuiBalancePolicyStateRecord.FieldSize, fieldSize);
		Assert.True(MuiBalancePolicyStateRecordMemoryCodec.TryGetAddress(
			ref platform, quietCursor, out var memoryCursorAddress,
			out var memoryFieldSize));
		Assert.Equal(cursorQuietAddress, memoryCursorAddress);
		Assert.Equal(fieldSize, memoryFieldSize);
		Assert.True(MuiBalancePolicyStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiBalancePolicyStateField.Quiet, 0x13579BDF));
		Assert.True(MuiBalancePolicyStateRecordCodec.TryReadStructural(
			ref platform, address, out var typedDecoded));
		Assert.Equal(value.Magic, typedDecoded.Magic);
		Assert.Equal(0x13579BDFu, typedDecoded.Quiet);
		Assert.True(MuiBalancePolicyStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, 4u, out var lastField));
		Assert.Equal(address.Raw + 4, lastField.Raw);
		Assert.True(MuiBalancePolicyStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, 4u, out var quiet));
		Assert.Equal(0x13579BDFu, quiet);
		Assert.False(MuiBalancePolicyStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiBalancePolicyStateRecord.Size, out _));
		Assert.False(MuiBalancePolicyStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, 0u, out _));
		quietCursor.Record = APTR.Null;
		Assert.False(MuiBalancePolicyStateFieldCursorCodec.TryGetAddress(
			ref platform, quietCursor, out _, out _));
		Assert.False(MuiBalancePolicyStateRecordCodec.TryReadStructural(ref platform,
			APTR.Null, out _));
	}

	[Fact]
	public void BalancePolicySequentialRecordPreservesOpaqueLongAndBounds()
	{
		var platform = CreatePlatform();
		var address = APTR.FromPointer(0x16A0);
		var value = new MuiBalancePolicyStateRecord
		{
			Magic = MuiBalancePolicyStateRecord.Cookie,
			Quiet = uint.MaxValue,
		};

		Assert.True(MuiBalancePolicyStateRecordCodec.WriteRecord(ref platform,
			address, value));
		Assert.True(MuiBalancePolicyStateRecordCodec.TryReadRecord(ref platform,
			address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.Quiet, decoded.Quiet);

		var crossingEnd = APTR.FromPointer(0x20FFF);
		Assert.False(MuiBalancePolicyStateRecordCodec.WriteRecord(ref platform,
			crossingEnd, value));
		Assert.False(MuiBalancePolicyStateRecordCodec.TryReadRecord(ref platform,
			crossingEnd, out _));
	}

	[Fact]
	public void BitmapSourceAndRemappedRecordsUseDedicatedStructAdapters()
	{
		var platform = CreatePlatform();
		var sourceAddress = APTR.FromPointer(0x15C0);
		var remappedAddress = APTR.FromPointer(0x15E0);
		var source = new MuiBitmapSourceStateRecord
		{
			Magic = MuiBitmapSourceStateRecord.Cookie,
			Source = APTR.FromPointer(0x1800),
		};
		var remapped = new MuiBitmapRemappedStateRecord
		{
			Magic = MuiBitmapRemappedStateRecord.Cookie,
			Remapped = APTR.FromPointer(0x1880),
		};

		Assert.True(MuiBitmapSourceStateRecordCodec.Write(ref platform,
			sourceAddress, source));
		Assert.True(MuiBitmapRemappedStateRecordCodec.Write(ref platform,
			remappedAddress, remapped));
		Assert.True(MuiBitmapSourceStateRecordCodec.TryReadStructural(ref platform,
			sourceAddress, out var sourceStructural));
		Assert.True(MuiBitmapRemappedStateRecordCodec.TryReadStructural(ref platform,
			remappedAddress, out var remappedStructural));
		Assert.Equal(source.Magic, sourceStructural.Magic);
		Assert.Equal(source.Source, sourceStructural.Source);
		Assert.Equal(remapped.Magic, remappedStructural.Magic);
		Assert.Equal(remapped.Remapped, remappedStructural.Remapped);
		Assert.True(MuiBitmapSourceStateRecordCodec.TryRead(ref platform,
			sourceAddress, out _));
		Assert.True(MuiBitmapRemappedStateRecordCodec.TryRead(ref platform,
			remappedAddress, out _));
		Assert.True(MuiBitmapSourceStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, sourceAddress, MuiBitmapSourceStateField.Source,
			0x1A2B3C4Du));
		Assert.True(MuiBitmapSourceStateRecordCodec.TryReadStructural(ref platform,
			sourceAddress, out var sourceTyped));
		Assert.Equal(source.Magic, sourceTyped.Magic);
		Assert.Equal(0x1A2B3C4Du, sourceTyped.Source.Raw);
		Assert.True(MuiBitmapRemappedStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, remappedAddress, MuiBitmapRemappedStateField.Remapped,
			0x55667788u));
		Assert.True(MuiBitmapRemappedStateRecordCodec.TryReadStructural(
			ref platform, remappedAddress, out var remappedTyped));
		Assert.Equal(remapped.Magic, remappedTyped.Magic);
		Assert.Equal(0x55667788u, remappedTyped.Remapped.Raw);
		Assert.True(MuiBitmapSourceStateRecordMemoryCodec.TryGetAddress(
			ref platform, sourceAddress, MuiBitmapSourceStateField.Source,
			out var typedSourceField));
		Assert.Equal(sourceAddress.Raw + MuiBitmapSourceStateRecord.SourceOffset,
			typedSourceField.Raw);
		Assert.True(MuiBitmapSourceStateRecordMemoryCodec.TryGetAddress(
			ref platform, sourceAddress, 4u, out var sourceField));
		Assert.Equal(sourceAddress.Raw + 4, sourceField.Raw);
		var sourceCursor = new MuiBitmapSourceStateFieldCursor
		{
			Record = sourceAddress,
			Field = MuiBitmapSourceStateField.Source,
		};
		Assert.True(MuiBitmapSourceStateFieldCursorCodec.TryGetAddress(
			ref platform, sourceCursor, out var cursorSourceField,
			out var sourceFieldSize));
		Assert.Equal(typedSourceField, cursorSourceField);
		Assert.Equal(MuiBitmapSourceStateRecord.FieldSize, sourceFieldSize);
		Assert.True(MuiBitmapSourceStateRecordMemoryCodec.TryGetAddress(
			ref platform, sourceCursor, out var memorySourceField,
			out var memorySourceFieldSize));
		Assert.Equal(cursorSourceField, memorySourceField);
		Assert.Equal(sourceFieldSize, memorySourceFieldSize);
		Assert.True(MuiBitmapRemappedStateRecordMemoryCodec.TryGetAddress(
			ref platform, remappedAddress, MuiBitmapRemappedStateField.Remapped,
			out var remappedField));
		Assert.Equal(remappedAddress.Raw + MuiBitmapRemappedStateRecord.FieldSize,
			remappedField.Raw);
		var remappedCursor = new MuiBitmapRemappedStateFieldCursor
		{
			Record = remappedAddress,
			Field = MuiBitmapRemappedStateField.Remapped,
		};
		Assert.True(MuiBitmapRemappedStateFieldCursorCodec.TryGetAddress(
			ref platform, remappedCursor, out var cursorRemappedField,
			out var remappedFieldSize));
		Assert.Equal(remappedField, cursorRemappedField);
		Assert.Equal(MuiBitmapRemappedStateRecord.FieldSize, remappedFieldSize);
		Assert.True(MuiBitmapRemappedStateRecordMemoryCodec.TryGetAddress(
			ref platform, remappedCursor, out var memoryRemappedField,
			out var memoryRemappedFieldSize));
		Assert.Equal(cursorRemappedField, memoryRemappedField);
		Assert.Equal(remappedFieldSize, memoryRemappedFieldSize);
		Assert.False(MuiBitmapSourceStateRecordMemoryCodec.TryGetAddress(
			ref platform, sourceAddress, MuiBitmapSourceStateRecord.Size, out _));
		Assert.False(MuiBitmapRemappedStateRecordMemoryCodec.TryGetAddress(
			ref platform, remappedAddress, (MuiBitmapRemappedStateField)255, out _));
		Assert.False(MuiBitmapSourceStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, 0u, out _));
		sourceCursor.Record = APTR.Null;
		Assert.False(MuiBitmapSourceStateFieldCursorCodec.TryGetAddress(
			ref platform, sourceCursor, out _, out _));
		Assert.False(MuiBitmapRemappedStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiBitmapRemappedStateField.Magic, out _));
		remappedCursor.Record = APTR.Null;
		Assert.False(MuiBitmapRemappedStateFieldCursorCodec.TryGetAddress(
			ref platform, remappedCursor, out _, out _));
	}

	[Fact]
	public void BitmapSourceSequentialRecordPreservesPointerAndBounds()
	{
		var platform = CreatePlatform();
		var address = APTR.FromPointer(0x16C0);
		var value = new MuiBitmapSourceStateRecord
		{
			Magic = MuiBitmapSourceStateRecord.Cookie,
			Source = APTR.FromPointer(0xFEEDBEEF),
		};

		Assert.True(MuiBitmapSourceStateRecordCodec.WriteRecord(ref platform,
			address, value));
		Assert.True(MuiBitmapSourceStateRecordCodec.TryReadRecord(ref platform,
			address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.Source, decoded.Source);

		var crossingEnd = APTR.FromPointer(0x20FFF);
		Assert.False(MuiBitmapSourceStateRecordCodec.WriteRecord(ref platform,
			crossingEnd, value));
		Assert.False(MuiBitmapSourceStateRecordCodec.TryReadRecord(ref platform,
			crossingEnd, out _));
	}

	[Fact]
	public void BitmapSourceOffsetBridgeUsesNamedUlongCodec()
	{
		var platform = CreatePlatform();
		var address = APTR.FromPointer(0x1700);
		var initial = new MuiBitmapSourceStateRecord
		{
			Magic = MuiBitmapSourceStateRecord.Cookie,
			Source = APTR.FromPointer(0x1800),
		};

		Assert.True(MuiBitmapSourceStateRecordCodec.WriteRecord(ref platform,
			address, initial));
		Assert.True(MuiBitmapSourceStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiBitmapSourceStateRecord.SourceOffset,
			0xF1020304u));
		Assert.True(MuiBitmapSourceStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, MuiBitmapSourceStateRecord.SourceOffset,
			out var source));
		Assert.Equal(0xF1020304u, source);
		Assert.True(MuiBitmapSourceStateRecordCodec.TryReadStructural(ref platform,
			address, out var decoded));
		Assert.Equal(initial.Magic, decoded.Magic);
		Assert.Equal(0xF1020304u, decoded.Source.Raw);
		Assert.False(MuiBitmapSourceStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, MuiBitmapSourceStateRecord.Size, out _));
		Assert.False(MuiBitmapSourceStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			APTR.Null, MuiBitmapSourceStateRecord.SourceOffset, 1));
	}

	[Fact]
	public void BitmapRemappedSequentialRecordPreservesPointerAndBounds()
	{
		var platform = CreatePlatform();
		var address = APTR.FromPointer(0x16E0);
		var value = new MuiBitmapRemappedStateRecord
		{
			Magic = MuiBitmapRemappedStateRecord.Cookie,
			Remapped = APTR.FromPointer(0xCAFEBABE),
		};

		Assert.True(MuiBitmapRemappedStateRecordCodec.WriteRecord(ref platform,
			address, value));
		Assert.True(MuiBitmapRemappedStateRecordCodec.TryReadRecord(ref platform,
			address, out var decoded));
		Assert.Equal(value.Magic, decoded.Magic);
		Assert.Equal(value.Remapped, decoded.Remapped);

		var crossingEnd = APTR.FromPointer(0x20FFF);
		Assert.False(MuiBitmapRemappedStateRecordCodec.WriteRecord(ref platform,
			crossingEnd, value));
		Assert.False(MuiBitmapRemappedStateRecordCodec.TryReadRecord(ref platform,
			crossingEnd, out _));
	}

	[Fact]
	public void BitmapPolicyRecordUsesDedicatedStructMemoryAdapter()
	{
		var platform = CreatePlatform();
		var address = APTR.FromPointer(0x1600);
		var value = new MuiBitmapPolicyStateRecord
		{
			Magic = MuiBitmapPolicyStateRecord.Cookie,
			Alpha = 0x11223344,
			MappingTable = 0x1800,
			Precision = 0x55667788,
			SourceColors = 0x1840,
			Transparent = 0x99AABBCC,
			UseFriend = 1,
		};

		Assert.True(MuiBitmapPolicyStateRecordCodec.Write(ref platform, address,
			value));
		Assert.True(MuiBitmapPolicyStateRecordCodec.TryReadStructural(ref platform,
			address, out var structural));
		Assert.Equal(value.Magic, structural.Magic);
		Assert.Equal(value.Alpha, structural.Alpha);
		Assert.Equal(value.MappingTable, structural.MappingTable);
		Assert.Equal(value.Precision, structural.Precision);
		Assert.Equal(value.SourceColors, structural.SourceColors);
		Assert.Equal(value.Transparent, structural.Transparent);
		Assert.Equal(value.UseFriend, structural.UseFriend);
		Assert.True(MuiBitmapPolicyStateRecordCodec.TryRead(ref platform, address,
			out _));
		Assert.True(MuiBitmapPolicyStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiBitmapPolicyStateField.UseFriend,
			out var lastField));
		Assert.Equal(address.Raw + MuiBitmapPolicyStateRecord.UseFriendOffset,
			lastField.Raw);
		var policyCursor = new MuiBitmapPolicyStateFieldCursor
		{
			Record = address,
			Field = MuiBitmapPolicyStateField.UseFriend,
		};
		Assert.True(MuiBitmapPolicyStateFieldCursorCodec.TryGetAddress(
			ref platform, policyCursor, out var cursorLastField,
			out var policyFieldSize));
		Assert.Equal(lastField, cursorLastField);
		Assert.Equal(MuiBitmapPolicyStateRecord.FieldSize, policyFieldSize);
		Assert.True(MuiBitmapPolicyStateRecordMemoryCodec.TryGetAddress(
			ref platform, policyCursor, out var memoryCursorField,
			out var memoryPolicyFieldSize));
		Assert.Equal(cursorLastField, memoryCursorField);
		Assert.Equal(policyFieldSize, memoryPolicyFieldSize);
		Assert.True(MuiBitmapPolicyStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, MuiBitmapPolicyStateField.MappingTable,
			out var mappingTable));
		Assert.Equal(value.MappingTable, mappingTable);
		Assert.False(MuiBitmapPolicyStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, (MuiBitmapPolicyStateField)255, out _));
		Assert.False(MuiBitmapPolicyStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiBitmapPolicyStateField.Magic, out _));
		policyCursor.Record = APTR.Null;
		Assert.False(MuiBitmapPolicyStateFieldCursorCodec.TryGetAddress(
			ref platform, policyCursor, out _, out _));
		Assert.False(MuiBitmapPolicyStateRecordCodec.TryReadStructural(ref platform,
			APTR.Null, out _));
	}

	[Fact]
	public void MalformedBalanceAndBitmapMagicRemainStructuralButFailClosed()
	{
		var platform = CreatePlatform();
		var balanceAddress = APTR.FromPointer(0x1600);
		var policyAddress = APTR.FromPointer(0x1620);
		var remappedAddress = APTR.FromPointer(0x1660);
		Assert.True(MuiBalancePolicyStateRecordCodec.Write(ref platform,
			balanceAddress, new MuiBalancePolicyStateRecord
			{
				Magic = MuiBalancePolicyStateRecord.Cookie,
				Quiet = 0xFFFFFFFF,
			}));
		Assert.True(MuiBitmapPolicyStateRecordCodec.Write(ref platform,
			policyAddress, new MuiBitmapPolicyStateRecord
			{
				Magic = MuiBitmapPolicyStateRecord.Cookie,
				MappingTable = 0x1800,
				SourceColors = 0x1840,
				UseFriend = 0,
			}));
		Assert.True(MuiBitmapRemappedStateRecordCodec.Write(ref platform,
			remappedAddress, new MuiBitmapRemappedStateRecord
			{
				Magic = MuiBitmapRemappedStateRecord.Cookie,
				Remapped = APTR.FromPointer(0x1880),
			}));

		Assert.True(MuiBalancePolicyStateFieldCursorCodec.TryWriteUInt32(
			ref platform, balanceAddress, MuiBalancePolicyStateField.Magic, 0));
		Assert.True(MuiBitmapPolicyStateFieldCursorCodec.TryWriteUInt32(ref platform,
			policyAddress, MuiBitmapPolicyStateField.Magic, 0));
		Assert.True(MuiBitmapRemappedStateFieldCursorCodec.TryWriteUInt32(
			ref platform, remappedAddress, MuiBitmapRemappedStateField.Magic, 0));

		Assert.True(MuiBalancePolicyStateRecordCodec.TryReadStructural(ref platform,
			balanceAddress, out var balance));
		Assert.True(MuiBitmapPolicyStateRecordCodec.TryReadStructural(ref platform,
			policyAddress, out var policy));
		Assert.True(MuiBitmapRemappedStateRecordCodec.TryReadStructural(ref platform,
			remappedAddress, out var remapped));
		Assert.Equal(0u, balance.Magic);
		Assert.Equal(0u, policy.Magic);
		Assert.Equal(0u, remapped.Magic);
		Assert.False(MuiBalancePolicyStateRecordCodec.TryRead(ref platform,
			balanceAddress, out _));
		Assert.False(MuiBitmapPolicyStateRecordCodec.TryRead(ref platform,
			policyAddress, out _));
		Assert.False(MuiBitmapRemappedStateRecordCodec.TryRead(ref platform,
			remappedAddress, out _));
		Assert.False(MuiBalancePolicyStateAdmission.Validate(balance));
		Assert.False(MuiBitmapPolicyStateAdmission.Validate(ref platform, policy));
		Assert.False(MuiBitmapRemappedStateAdmission.Validate(ref platform,
			remapped));
	}

	private static MuiHeadlessTestPlatform CreatePlatform() =>
		new(0x1000, 0x20000, 0x4000, State);
}
