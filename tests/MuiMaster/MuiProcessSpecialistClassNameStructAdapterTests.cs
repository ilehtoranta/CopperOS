/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.CompilerServices;
using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiProcessSpecialistClassNameStructAdapterTests
{
	[Fact]
	public void ProcessAndSlaveClassNamesUseExactNamedRecords()
	{
		var state = APTR.FromPointer(0x1000);
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			state);
		var processAddress = APTR.FromPointer(0x2400);
		var slaveAddress = APTR.FromPointer(0x2420);
		var process = new MuiProcessSpecialistProcessClassNameRecord
		{
			Word0 = 0x50726F63,
			Word1 = 0x6573732E,
			Word2 = 0x6D756900,
		};
		var slave = new MuiProcessSpecialistSlaveClassNameRecord
		{
			Word0 = 0x536C6176,
			Word1 = 0x652E6D75,
			Character = (byte)'i',
			Terminator = 0,
		};

		Assert.Equal(12,
			Unsafe.SizeOf<MuiProcessSpecialistProcessClassNameRecord>());
		Assert.Equal(10, Unsafe.SizeOf<MuiProcessSpecialistSlaveClassNameRecord>());
		Assert.True(MuiProcessSpecialistProcessClassNameRecordCodec.WriteRecord(ref platform,
			processAddress, process));
		Assert.True(MuiProcessSpecialistSlaveClassNameRecordCodec.WriteRecord(ref platform,
			slaveAddress, slave));
		Assert.True(MuiProcessSpecialistProcessClassNameRecordCodec.TryMatch(
			ref platform, processAddress));
		Assert.True(MuiProcessSpecialistSlaveClassNameRecordCodec.TryMatch(ref platform,
			slaveAddress));
		Assert.Equal(MuiProcessSpecialistClass.Process,
			MuiProcessSpecialistCore.ClassifyName(ref platform, processAddress));
		Assert.Equal(MuiProcessSpecialistClass.Slave,
			MuiProcessSpecialistCore.ClassifyName(ref platform, slaveAddress));
		Assert.False(MuiProcessSpecialistProcessClassNameRecordCodec.TryMatch(
			ref platform, APTR.FromPointer(0x20FF5)));
	}

	[Fact]
	public void ProcessAndSlaveSequentialRecordsPreserveMixedWidthsAndBounds()
	{
		var state = APTR.FromPointer(0x1000);
		var platform = new MuiHeadlessTestPlatform(0x1000, 0x20000, 0x4000,
			state);
		var processAddress = APTR.FromPointer(0x2480);
		var slaveAddress = APTR.FromPointer(0x24A0);
		var process = new MuiProcessSpecialistProcessClassNameRecord
		{
			Word0 = 0xFFFFFFFFu, Word1 = 0xCAFEBABEu, Word2 = 0x01020304u,
		};
		var slave = new MuiProcessSpecialistSlaveClassNameRecord
		{
			Word0 = 0x11223344u, Word1 = 0x55667788u,
			Character = 0xA5, Terminator = 0x5A,
		};

		Assert.True(MuiProcessSpecialistProcessClassNameRecordCodec.WriteRecord(
			ref platform, processAddress, process));
		Assert.True(MuiProcessSpecialistProcessClassNameRecordCodec.TryReadRecord(
			ref platform, processAddress, out var decodedProcess));
		Assert.Equal(process.Word0, decodedProcess.Word0);
		Assert.Equal(process.Word1, decodedProcess.Word1);
		Assert.Equal(process.Word2, decodedProcess.Word2);
		Assert.True(MuiProcessSpecialistSlaveClassNameRecordCodec.WriteRecord(
			ref platform, slaveAddress, slave));
		Assert.True(MuiProcessSpecialistSlaveClassNameRecordCodec.TryReadRecord(
			ref platform, slaveAddress, out var decodedSlave));
		Assert.Equal(slave.Word0, decodedSlave.Word0);
		Assert.Equal(slave.Word1, decodedSlave.Word1);
		Assert.Equal(slave.Character, decodedSlave.Character);
		Assert.Equal(slave.Terminator, decodedSlave.Terminator);
		Assert.False(MuiProcessSpecialistProcessClassNameRecordCodec.WriteRecord(
			ref platform, APTR.FromPointer(0x2FFFF), process));
		Assert.False(MuiProcessSpecialistSlaveClassNameRecordCodec.TryReadRecord(
			ref platform, APTR.FromPointer(0x2FFFF), out _));
	}
}
