using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace CopperOS.Commands.RenameNativeExecution;

internal static class PhysicalFilePath
{
    // This host fixture is run on Windows. Fail closed on another platform
    // until its equivalent physical-file boundary is separately implemented.
    public static string Resolve(string path)
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Reference boundary requires the Windows physical-file guard.");
        using var handle = File.OpenHandle(Path.GetFullPath(path), FileMode.Open, FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete);
        var buffer = new StringBuilder(32768);
        var length = GetFinalPathNameByHandleW(handle, buffer, (uint)buffer.Capacity, 0);
        if (length == 0) throw new Win32Exception(Marshal.GetLastPInvokeError());
        if (length >= buffer.Capacity) throw new PathTooLongException("Physical file path exceeds the guarded bound.");
        var result = buffer.ToString();
        if (result.StartsWith(@"\\?\UNC\", StringComparison.OrdinalIgnoreCase)) result = @"\\" + result[8..];
        else if (result.StartsWith(@"\\?\", StringComparison.Ordinal)) result = result[4..];
        return Path.GetFullPath(result);
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
    private static extern uint GetFinalPathNameByHandleW(SafeFileHandle file,
        [Out] StringBuilder path, uint capacity, uint flags);
}
