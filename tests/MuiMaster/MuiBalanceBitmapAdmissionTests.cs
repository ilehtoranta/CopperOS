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
			ref platform, address, 4, out var lastField));
		Assert.Equal(address.Raw + 4, lastField.Raw);
		Assert.True(MuiBalancePolicyStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, 4, out var quiet));
		Assert.Equal(value.Quiet, quiet);
		Assert.False(MuiBalancePolicyStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, MuiBalancePolicyStateRecord.Size, out _));
		Assert.False(MuiBalancePolicyStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, 0, out _));
		Assert.False(MuiBalancePolicyStateRecordCodec.TryReadStructural(ref platform,
			APTR.Null, out _));
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
		Assert.True(MuiBitmapSourceStateRecordMemoryCodec.TryGetAddress(
			ref platform, sourceAddress, 4, out var sourceField));
		Assert.Equal(sourceAddress.Raw + 4, sourceField.Raw);
		Assert.True(MuiBitmapRemappedStateRecordMemoryCodec.TryGetAddress(
			ref platform, remappedAddress, MuiBitmapRemappedStateField.Remapped,
			out var remappedField));
		Assert.Equal(remappedAddress.Raw + MuiBitmapRemappedStateRecord.RemappedOffset,
			remappedField.Raw);
		Assert.False(MuiBitmapSourceStateRecordMemoryCodec.TryGetAddress(
			ref platform, sourceAddress, MuiBitmapSourceStateRecord.Size, out _));
		Assert.False(MuiBitmapRemappedStateRecordMemoryCodec.TryGetAddress(
			ref platform, remappedAddress, (MuiBitmapRemappedStateField)255, out _));
		Assert.False(MuiBitmapSourceStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, 0, out _));
		Assert.False(MuiBitmapRemappedStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiBitmapRemappedStateField.Magic, out _));
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
		Assert.True(MuiBitmapPolicyStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, MuiBitmapPolicyStateField.MappingTable,
			out var mappingTable));
		Assert.Equal(value.MappingTable, mappingTable);
		Assert.False(MuiBitmapPolicyStateRecordMemoryCodec.TryGetAddress(
			ref platform, address, (MuiBitmapPolicyStateField)255, out _));
		Assert.False(MuiBitmapPolicyStateRecordMemoryCodec.TryGetAddress(
			ref platform, APTR.Null, MuiBitmapPolicyStateField.Magic, out _));
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
