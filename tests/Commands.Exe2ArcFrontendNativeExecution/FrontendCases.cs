namespace CopperOS.Commands.Exe2ArcFrontendNativeExecution;

internal sealed record FrontendCase(
    string Id,
    int? ReadArgsError = null,
    bool AllocationFailure = false,
    string? Type = null,
    int ExpectedResult = 20,
    int ExpectedIoError = 103);

internal static class FrontendCases
{
    public const string Suite = "exe2arc-frontend-native-smoke";

    public static IReadOnlyList<FrontendCase> All() =>
    [
        new("parser-error-bad-template", ReadArgsError: 119,
            ExpectedIoError: 119),
        new("parser-result-allocation-failure", AllocationFailure: true,
            ExpectedIoError: 103),
    ];
}
