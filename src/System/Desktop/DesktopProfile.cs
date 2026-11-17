namespace CopperOS.Desktop;

/// <summary>The qualified compact A1200 CopperStart desktop module profile.</summary>
public static class DesktopProfile
{
    public const string RequiredRomSha256 = "8DED0B1EC8C1A3B6F03CDD7FFC645369AE9D464EC735588A4C147AF3248FE1B2";
    public const string ProviderSha256 = "BED9A6606772887E3FA0A8F15BE3EA8E297D330F395CFA9CB686A49F2791F615";
    public const uint TextEntry = 213360;
    public const uint StackBytes = 32768, StateBytes = 256, CleanupReserveBytes = 8192;
    public const uint FilesWorkspaceBytes = 4384, FilesEntryCapacity = 32, FilesPathBytes = 256;
    public const uint EditorCapacity = 16384, EditorBytes = 16936, EditorFontBytes = 2816;
    public const uint EditorSnapshotBytes = 16664, EditorSaveBytes = 2208;
    public const uint RecoveryBytes = 832;
}
