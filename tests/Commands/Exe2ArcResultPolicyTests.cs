using Amiga;

namespace CopperOS.Commands.Tests;

public sealed class Exe2ArcResultPolicyTests
{
    [Fact]
    public void Completed_scan_reports_detection_without_claiming_saved_output()
    {
        var decision = Exe2ArcResultPolicy.ForScan(
            Exe2ArcIoStatus.Completed, 17);

        Assert.Equal(DOS.RETURN_OK, decision.ReturnLevel);
        Assert.True(decision.ReportDetection);
        Assert.False(decision.ReportSaved);
        Assert.False(decision.DeleteOutput);
        Assert.Equal(Exe2ArcCommandEvent.None, decision.Event);
        Assert.Equal(17, decision.IoError);
    }

    [Theory]
    [InlineData(Exe2ArcIoStatus.NoMatch)]
    [InlineData(Exe2ArcIoStatus.OffsetZero)]
    [InlineData(Exe2ArcIoStatus.MatchedNotPositioned)]
    [InlineData(Exe2ArcIoStatus.IoStopped)]
    public void Noncompleted_scan_is_terminal_and_never_opens_cleanup(
        Exe2ArcIoStatus status)
    {
        var decision = Exe2ArcResultPolicy.ForScan(status, 205);

        Assert.Equal(DOS.RETURN_FAIL, decision.ReturnLevel);
        Assert.False(decision.ReportDetection);
        Assert.False(decision.DeleteOutput);
        Assert.Equal(205, decision.IoError);
        Assert.Equal(status is Exe2ArcIoStatus.OffsetZero or
            Exe2ArcIoStatus.MatchedNotPositioned
            ? Exe2ArcCommandEvent.UnsafePosition
            : status == Exe2ArcIoStatus.NoMatch
                ? Exe2ArcCommandEvent.NoMatch
                : Exe2ArcCommandEvent.InputFailure, decision.Event);
    }

    [Fact]
    public void Successful_nonempty_extraction_reports_saved_warning()
    {
        var decision = Exe2ArcResultPolicy.ForExtraction(
            Exe2ArcIoStatus.Completed, 1234);

        Assert.Equal(DOS.RETURN_OK, decision.ReturnLevel);
        Assert.True(decision.ReportSaved);
        Assert.True(decision.ReportWarning);
        Assert.False(decision.DeleteOutput);
        Assert.Equal(Exe2ArcCommandEvent.Saved, decision.Event);
    }

    [Fact]
    public void Source_successful_break_keeps_output_and_break_ioerr()
    {
        var decision = Exe2ArcResultPolicy.ForExtraction(
            Exe2ArcIoStatus.Completed, 150, (int)DOS.Error.Break, true);

        Assert.Equal(DOS.RETURN_OK, decision.ReturnLevel);
        Assert.True(decision.ReportSaved);
        Assert.True(decision.ReportWarning);
        Assert.False(decision.DeleteOutput);
        Assert.Equal((int)DOS.Error.Break, decision.IoError);
        Assert.Equal(Exe2ArcCommandEvent.Saved, decision.Event);
    }

    [Theory]
    [InlineData(Exe2ArcIoStatus.Completed, 0, 0)]
    [InlineData(Exe2ArcIoStatus.IoStopped, 3, 0)]
    [InlineData(Exe2ArcIoStatus.IoStopped, 3, (int)DOS.Error.Break)]
    [InlineData(Exe2ArcIoStatus.InvalidRecord, 12, 0)]
    public void Failed_or_empty_extraction_requests_close_then_delete(
        Exe2ArcIoStatus status, uint bytes, int error)
    {
        var decision = Exe2ArcResultPolicy.ForExtraction(status, bytes, error);

        Assert.Equal(DOS.RETURN_FAIL, decision.ReturnLevel);
        Assert.True(decision.DeleteOutput);
        Assert.False(decision.ReportSaved);
        Assert.Equal(error, decision.IoError);
        Assert.Equal(status == Exe2ArcIoStatus.IoStopped &&
            error == (int)DOS.Error.Break
            ? Exe2ArcCommandEvent.Break
            : Exe2ArcCommandEvent.ExtractionFailure, decision.Event);
    }

    [Fact]
    public void Input_and_output_failures_do_not_request_deletion_before_open()
    {
        var input = Exe2ArcResultPolicy.InputFailure(205);
        var output = Exe2ArcResultPolicy.OutputFailure(218);

        Assert.False(input.DeleteOutput);
        Assert.False(output.DeleteOutput);
        Assert.Equal(Exe2ArcCommandEvent.InputFailure, input.Event);
        Assert.Equal(Exe2ArcCommandEvent.OutputFailure, output.Event);
    }
}
