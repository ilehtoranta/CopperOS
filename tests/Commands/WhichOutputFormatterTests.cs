using Amiga;
using CopperOS.Commands;

namespace CopperOS.Commands.Tests;

public sealed class WhichOutputFormatterTests
{
    [Theory]
    [InlineData(WhichOutputKind.Path, "Execute", "Workbench3.1:C/Execute",
        "Workbench3.1:C/Execute\n")]
    [InlineData(WhichOutputKind.Internal, "CD", "unused",
        "INTERNAL CD\n")]
    [InlineData(WhichOutputKind.Resident, "Execute", "unused",
        "RES Execute\n")]
    public void Formats_reference_observed_classic_result_lines(
        WhichOutputKind kind, string nameText, string pathText, string expected)
    {
        EchoCommandTests.TestShellPlatform platform = new();
        APTR name = platform.Store.PutAt(500, nameText);
        APTR path = platform.Store.PutAt(600, pathText);

        Assert.True(WhichOutputFormatter.TryWrite(ref platform, new BPTR(1), kind,
            name, (uint)nameText.Length, path, (uint)pathText.Length,
            new APTR(800), 128));

        Assert.Equal(expected, platform.Store.OutputText);
    }

    [Fact]
    public void Rejects_short_writes_and_overlapping_source_storage()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        APTR name = platform.Store.PutAt(500, "CD");
        APTR path = platform.Store.PutAt(600, "Workbench3.1:C/Execute");
        platform.Store.ShortWrite = true;

        Assert.False(WhichOutputFormatter.TryWrite(ref platform, new BPTR(1),
            WhichOutputKind.Path, name, 2, path, 23, new APTR(800), 128));
        Assert.Empty(platform.Store.Output);

        platform.Store.ShortWrite = false;
        Assert.False(WhichOutputFormatter.TryWrite(ref platform, new BPTR(1),
            WhichOutputKind.Internal, name, 2, path, 23, name, 64));
        Assert.Empty(platform.Store.Output);
    }
}
