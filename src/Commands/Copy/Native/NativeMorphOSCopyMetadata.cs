using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>Protection, date and comment portion of MorphOS Copy SetData.</summary>
public static class NativeMorphOSCopyMetadata
{
    public const uint Protection = 1, ProtectionX = 2, NoProtection = 4,
        Comment = 8, Dates = 16;

    /// <summary>
    /// Applies source protection, date and comment metadata when the caller selects
    /// the original SetData tail, including its QUIET error paths.
    /// NOPRO wins; normal protection clears ARCHIVE, while PROX retains only
    /// execute, pure, and script bits. DOS return values are intentionally
    /// ignored, as in the source SetData routine.
    /// </summary>
    public static void Apply(APTR name, APTR sourceFib, uint flags)
    {
        if (name.IsNull || sourceFib.IsNull)
            return;
        var target = CString.FromPointer(name.Raw);
        var protection = unchecked((uint)FileInfoBlock.GetProtection(sourceFib.Raw));
        if ((flags & NoProtection) == 0)
        {
            if ((flags & Protection) != 0)
                DOS.SetProtection(target, unchecked((int)(protection & ~(uint)FileProtection.Archive)));
            else if ((flags & ProtectionX) != 0)
                DOS.SetProtection(target, unchecked((int)(protection & ((uint)FileProtection.Execute |
                    (uint)FileProtection.Pure | (uint)FileProtection.Script))));
        }
        if ((flags & Dates) != 0)
        {
            var date = APTR.FromPointer(sourceFib.Raw + FileInfoBlock.DateDaysOffset);
            if ((FileInfoBlock.GetActualExtensionFlags(sourceFib.Raw) &
                    (byte)FileInfoExtensionFlags.PosixDate) != 0)
                DOS.SetFilePosixDate(target, date, APTR.Null);
            else
                DOS.SetFileDate(target, date);
        }
        if ((flags & Comment) != 0)
            DOS.SetComment(target, CString.FromPointer(sourceFib.Raw + FileInfoBlock.CommentOffset));
    }
}
