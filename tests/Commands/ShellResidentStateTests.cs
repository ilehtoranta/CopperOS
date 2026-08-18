using Amiga;
using CopperOS.Shell;

namespace CopperOS.Commands.Tests;

public sealed class ShellResidentStateTests
{
    [Fact]
    public void Codec_tracks_use_count_and_blocks_acquire_after_removal()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        ShellResidentEntryState initial = new()
        {
            Name = new APTR(400),
            NameLength = 4,
            Segment = BPTR.FromRaw(1),
            SegmentOwner = new APTR(500),
            Flags = ShellResidentEntryFlags.VerifiedPure |
                ShellResidentEntryFlags.Loaded,
        };
        APTR entry = new(3000);

        Assert.True(ShellResidentEntryCodec.Initialize(
            ref platform, entry, in initial));
        Assert.True(ShellResidentEntryCodec.TryAcquire(
            ref platform, entry));
        Assert.True(ShellResidentEntryCodec.TryRead(
            ref platform, entry, out var acquired));
        Assert.Equal(1u, acquired.UseCount);
        Assert.False(ShellResidentEntryCodec.TryMarkRemoval(
            ref platform, entry));
        Assert.True(ShellResidentEntryCodec.TryRelease(
            ref platform, entry));
        Assert.True(ShellResidentEntryCodec.TryMarkRemoval(
            ref platform, entry));
        Assert.False(ShellResidentEntryCodec.TryAcquire(
            ref platform, entry));
    }

    [Fact]
    public void Forced_unverified_admission_is_unsafe_not_verified_pure()
    {
        ShellResidentEntryState state = new()
        {
            Name = new APTR(400),
            NameLength = 4,
        };

        Assert.True(ShellResidentPolicy.TryAdmit(
            ref state, 0, 1, 0, 0, BPTR.FromRaw(1),
            new APTR(500)));
        Assert.True((state.Flags & ShellResidentEntryFlags.Unsafe) != 0);
        Assert.False((state.Flags & ShellResidentEntryFlags.VerifiedPure) != 0);
        Assert.True(ShellResidentPolicy.CanAcquire(in state));
    }

    [Fact]
    public void Deferred_admission_has_no_loaded_segment_and_cannot_run()
    {
        ShellResidentEntryState state = new()
        {
            Name = new APTR(400),
            NameLength = 4,
        };

        Assert.True(ShellResidentPolicy.TryAdmit(
            ref state, 1, 0, 0, 1, BPTR.Null, APTR.Null));
        Assert.True((state.Flags & ShellResidentEntryFlags.Deferred) != 0);
        Assert.False((state.Flags & ShellResidentEntryFlags.Loaded) != 0);
        Assert.False(ShellResidentPolicy.CanAcquire(in state));
    }

    [Fact]
    public void Admission_rejects_conflicting_pure_force_and_segment_state()
    {
        ShellResidentEntryState state = new()
        {
            Name = new APTR(400),
            NameLength = 4,
        };

        Assert.False(ShellResidentPolicy.TryAdmit(
            ref state, 1, 1, 0, 0, BPTR.FromRaw(1),
            new APTR(500)));
        Assert.False(ShellResidentPolicy.TryAdmit(
            ref state, 0, 0, 0, 0, BPTR.Null, APTR.Null));
    }

    [Fact]
    public void Codec_rejects_unmapped_resident_name_span()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        ShellResidentEntryState state = new()
        {
            Name = APTR.FromPointer(0xFFFF_FFF0),
            NameLength = 8,
            Segment = BPTR.FromRaw(1),
            SegmentOwner = new APTR(500),
            Flags = ShellResidentEntryFlags.Loaded,
        };

        Assert.False(ShellResidentEntryCodec.Initialize(
            ref platform, new APTR(3000), in state));
    }
}
