namespace CopperOS.Desktop;

/// <summary>The frozen compact CopperStart text-provider profile, not the shipping HDF ROM.</summary>
public static class DesktopProfile
{
    public const string RequiredRomSha256 = "1211BF01AED8FC5CC2E979575DFA5779A4E578E2AFAEA720B852847E2398F7F9";
    public const string ProviderSha256 = "5900735263A90AC53626E6103FD6B232329667AE03D78105BC74D88E2BC085FD";
    public const uint TextEntry = 247686;
    public const uint StackBytes = 32768, StateBytes = 256, CleanupReserveBytes = 8192;
    public const uint FilesWorkspaceBytes = 4384, FilesEntryCapacity = 32, FilesPathBytes = 256;
    public const uint EditorCapacity = 16384, EditorBytes = 16936, EditorFontBytes = 2816;
    public const uint EditorSnapshotBytes = 16664, EditorSaveBytes = 2208;
    public const uint RecoveryBytes = 832;
}
