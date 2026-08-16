using Amiga;

namespace CopperOS.Shell;

/// <summary>
/// Caller-owned guest buffers shared by one Shell command dispatch.
///
/// The workspace is a value type so a native Shell frame can keep all
/// temporary storage explicit. Commands never retain these pointers after
/// their entry returns.
/// </summary>
public struct ShellCommandWorkspace
{
    public ShellCommandWorkspace(
        APTR token,
        uint tokenCapacity,
        APTR first,
        uint firstCapacity,
        APTR second,
        uint secondCapacity,
        APTR third,
        uint thirdCapacity,
        APTR fourth,
        uint fourthCapacity,
        APTR errorCodes,
        uint errorCodeCapacity)
    {
        Token = token;
        TokenCapacity = tokenCapacity;
        First = first;
        FirstCapacity = firstCapacity;
        Second = second;
        SecondCapacity = secondCapacity;
        Third = third;
        ThirdCapacity = thirdCapacity;
        Fourth = fourth;
        FourthCapacity = fourthCapacity;
        ErrorCodes = errorCodes;
        ErrorCodeCapacity = errorCodeCapacity;
    }

	public APTR Token { get; set; }
	public uint TokenCapacity { get; set; }
	public APTR First { get; set; }
	public uint FirstCapacity { get; set; }
	public APTR Second { get; set; }
	public uint SecondCapacity { get; set; }
	public APTR Third { get; set; }
	public uint ThirdCapacity { get; set; }
	public APTR Fourth { get; set; }
	public uint FourthCapacity { get; set; }
	public APTR ErrorCodes { get; set; }
	public uint ErrorCodeCapacity { get; set; }
}
