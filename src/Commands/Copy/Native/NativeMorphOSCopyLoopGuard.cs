using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>Bounded MorphOS Copy TestLoop ancestor check.</summary>
public static class NativeMorphOSCopyLoopGuard
{
    /// <summary>
    /// Returns true when destination is at or below source on the same device.
    /// It never releases the caller-owned destination lock; parent locks it
    /// acquires are released as the source does.
    /// </summary>
    public static bool HasLoop(BPTR sourceDirectory, BPTR destinationDirectory)
    {
        var current = destinationDirectory;
        var loop = false;
        if (DOS.SameDevice(sourceDirectory, destinationDirectory) != 0)
        {
            do
            {
                if (DOS.SameLock(sourceDirectory, current) != 0)
                {
                    var parent = DOS.ParentDirRaw(current);
                    if (current.Raw != destinationDirectory.Raw)
                        DOS.UnLock(current);
                    current = parent;
                }
                else
                {
                    loop = true;
                }
            } while (!loop && current.IsNotNull);
        }
        if (current.Raw != destinationDirectory.Raw)
            DOS.UnLock(current);
        return loop;
    }
}
