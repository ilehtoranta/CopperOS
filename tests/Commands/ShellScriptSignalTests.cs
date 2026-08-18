using Amiga;
using CopperOS.Shell;

namespace CopperOS.Commands.Tests;

public sealed class ShellScriptSignalTests
{
    [Fact]
    public void Signal_codec_records_and_acknowledges_one_event()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        APTR record = new(3000);
        ShellScriptSignalState initial = default;
        Assert.True(ShellScriptSignalCodec.Initialize(
            ref platform, record, in initial));
        ShellScriptSignalEvent signal = new(
            ShellScriptSignalFlags.Break | ShellScriptSignalFlags.CtrlC,
            130,
            9);

        Assert.True(ShellScriptSignalCodec.TryRecord(
            ref platform, record, in signal));
        Assert.True(ShellScriptSignalCodec.TryAcknowledge(
            ref platform, record, signal.Sequence));
        Assert.True(ShellScriptSignalCodec.TryRead(
            ref platform, record, out var state));
        Assert.Equal(ShellScriptSignalFlags.None, state.Pending);
        Assert.Equal(130, state.Result);
        Assert.Equal((uint)9, state.Sequence);
        Assert.Equal((uint)9, state.AcknowledgedSequence);
    }

    [Fact]
    public void Signal_codec_rejects_unknown_flags_and_wrong_acknowledgement()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        APTR record = new(3000);
        ShellScriptSignalState initial = default;
        Assert.True(ShellScriptSignalCodec.Initialize(
            ref platform, record, in initial));
        ShellScriptSignalEvent invalid = new(
            (ShellScriptSignalFlags)0x80,
            1,
            1);
        Assert.False(ShellScriptSignalCodec.TryRecord(
            ref platform, record, in invalid));
        ShellScriptSignalEvent valid = new(
            ShellScriptSignalFlags.CtrlD,
            0,
            2);
        Assert.True(ShellScriptSignalCodec.TryRecord(
            ref platform, record, in valid));
        Assert.False(ShellScriptSignalCodec.TryAcknowledge(
            ref platform, record, 3));
    }
}
