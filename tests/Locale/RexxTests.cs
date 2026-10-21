namespace CopperOS.Locale.Tests;

public class RexxTests
{
    [Theory]
    [InlineData("ISALPHA", "a", "1")]
    [InlineData("ISALPHA", "3", "0")]
    [InlineData("ISXDIGIT", "f", "1")]
    [InlineData("CONVTOUPPER", "a", "A")]
    [InlineData("CONVTOLOWER", "A", "a")]
    [InlineData("GETLOCALESTR", "15", "January")]
    public void RegisterAbiFunctions(string function, string argument, string expected)
    {
        var p = new TestPlatform(new TestState()); uint l = LocaleCore.Default(ref p), message = p.Allocate(128), head = 0;
        p.Write32(message + 28, 0x02000001); p.Write32(message + 40, p.Text(function)); p.Write32(message + 44, p.Text(argument));
        Assert.Equal(0u, RexxCore.Evaluate(ref p, l, message, ref head, out uint result, out uint temporary));
        Assert.Equal(expected, p.String(result)); p.Free(temporary);
    }
    [Theory]
    [InlineData("UNKNOWN", 1, 1)] [InlineData("ISALPHA", 2, 17)] [InlineData("ISALPHA", 1, 18)]
    public void ErrorCodes(string function, uint count, uint error)
    {
        var p = new TestPlatform(new TestState()); uint l = LocaleCore.Default(ref p), message = p.Allocate(128), head = 0;
        p.Write32(message + 28, 0x02000000 | count); p.Write32(message + 40, p.Text(function));
        Assert.Equal(error, RexxCore.Evaluate(ref p, l, message, ref head, out _, out _));
    }
}
