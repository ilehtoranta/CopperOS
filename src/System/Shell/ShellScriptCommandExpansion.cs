using Amiga;

namespace CopperOS.Shell;

public static partial class ShellScriptEngine
{
    private static bool HasCommandSubstitution<TPlatform>(ref TPlatform platform,
        APTR text, uint length) where TPlatform : struct, IShellPlatform
    {
        uint quoted = 0;
        for (uint i = 0; i < length; i++)
        {
            var ch = platform.ReadUInt8(text, (int)i);
            if (ch == (byte)'*') { i++; continue; }
            if (ch == (byte)'"') quoted ^= 1;
            if (ch == (byte)';' && quoted == 0) return false;
            if (ch == (byte)'`') return true;
        }
        return false;
    }

    private static bool StartCommandExpansion<TPlatform>(ref TPlatform platform,
        APTR frame, APTR cli, in ShellScriptStepWorkspace workspace, uint length,
        uint nextLine, uint nextOffset)
        where TPlatform : struct, IShellPlatform, IShellScriptPlatform
    {
        var templateBuffer = workspace.PromptTemplate;
        var templateCapacity = workspace.PromptTemplateCapacity;
        if (templateBuffer.IsNull && !platform.TryEnsureScriptPromptTemplate(cli, out templateBuffer, out templateCapacity)) return false;
        uint quoted = 0, inCommand = 0;
        for (uint i = 0; i < length; i++)
        {
            var ch = platform.ReadUInt8(workspace.Line, (int)i);
            if (ch == (byte)'*') { i++; continue; }
            if (ch == (byte)'`') inCommand ^= 1;
            if (ch == (byte)'"' && inCommand == 0) quoted ^= 1;
            if (ch == (byte)';' && quoted == 0 && inCommand == 0) { length = i; break; }
        }
        // The first word of this runner-owned buffer retains the start of the
        // current capture, so trimming its newline cannot eat a literal prefix.
        if (templateBuffer.IsNull || templateCapacity < 5 ||
            length >= templateCapacity - 4 ||
            !platform.IsMapped(templateBuffer, templateCapacity))
            return false;
        var templateText = APTR.FromPointer(templateBuffer.Raw + 4);
        platform.Copy(workspace.Line, templateText, length);
        platform.WriteUInt8(templateText, (int)length, 0);
        var template = new ShellScriptPromptTemplate { Text = templateText, Length = length };
        var cursor = default(ShellScriptPromptCursor);
        while (true)
        {
            if (!ShellScriptPromptParser.TryNextCommand(ref platform, in template, in cursor, out var segment))
                return false;
            if (segment.Kind == ShellScriptPromptSegmentKind.End) break;
            cursor = segment.Next;
        }
        var deferred = new ShellScriptDeferredCommandState
        {
            Kind = ShellScriptDeferredCommandKind.CommandExpansion,
            Text = new ShellScriptTextSlice(templateText, length),
            NextLine = nextLine, NextOffset = nextOffset,
        };
        var expansion = new ShellScriptPromptExpansionState
        {
            Phase = ShellScriptPromptExpansionPhase.Expanding,
            TemplateLength = length,
        };
        if (!ShellScriptFrameCodec.TrySetDeferredCommand(ref platform, frame, in deferred) ||
            !ShellScriptFrameCodec.TrySetPromptExpansion(ref platform, frame, in expansion))
            return false;
        platform.WriteUInt8(workspace.Line, 0, 0);
        platform.WriteUInt32(templateBuffer, 0, 0);
        return true;
    }

    private static ShellScriptStepStatus StepCommandExpansion<TPlatform>(ref TPlatform platform,
        APTR frame, in ShellScriptFrameState state, in ShellScriptStepWorkspace suppliedWorkspace,
        out ShellScriptStepResult step)
        where TPlatform : struct, IShellPlatform, IShellScriptPlatform
    {
        var workspace = suppliedWorkspace;
        if (workspace.PromptTemplate.IsNull)
        {
            if (!platform.TryEnsureScriptPromptTemplate(state.Cli, out var buffer, out var capacity))
            {
                step = MakeStep(ShellScriptStepStatus.PlatformFailure, ShellInternalCommand.Unknown,
                    (int)ShellCommandResult.Error, state.CurrentLine, state.CurrentOffset, 0);
                return step.Status;
            }
            workspace.PromptTemplate = buffer; workspace.PromptTemplateCapacity = capacity;
        }
        var expansion = state.PromptExpansion;
        var command = ShellInternalCommand.Unknown;
        var result = state.LastResult;
        var progress = ShellScriptPromptProgress.Failed;
        if (expansion.Phase == ShellScriptPromptExpansionPhase.ReadingCapture)
            progress = ReadPromptCapture(ref platform, frame, in state, in workspace, ref expansion, out result);
        else if (expansion.Phase == ShellScriptPromptExpansionPhase.Expanding)
        {
            var template = new ShellScriptPromptTemplate
            { Text = state.DeferredCommand.Text.Data, Length = expansion.TemplateLength };
            var cursor = expansion.Cursor;
            if (ShellScriptPromptParser.TryNextCommand(ref platform, in template, in cursor, out var segment))
            {
                if (segment.Kind == ShellScriptPromptSegmentKind.End)
                {
                    if (CommandBufferLength(ref platform, in workspace, out var length))
                    {
                        var deferred = state.DeferredCommand;
                        deferred.Kind = ShellScriptDeferredCommandKind.ExpandedLine;
                        deferred.Text = new ShellScriptTextSlice(workspace.Line, length);
                        if (ShellScriptFrameCodec.TrySetDeferredCommand(ref platform, frame, in deferred) &&
                            ShellScriptFrameCodec.TrySetPromptExpansion(ref platform, frame, default))
                            progress = ShellScriptPromptProgress.Ready;
                    }
                }
                else if (segment.Kind == ShellScriptPromptSegmentKind.Literal)
                {
                    if (AppendCommandText(ref platform, in workspace, segment.Text, segment.Length, 0))
                    {
                        expansion.Cursor = segment.Next;
                        if (ShellScriptFrameCodec.TrySetPromptExpansion(ref platform, frame, in expansion))
                            progress = ShellScriptPromptProgress.Continue;
                    }
                }
                else if (CommandBufferLength(ref platform, in workspace, out var length))
                {
                    platform.WriteUInt32(workspace.PromptTemplate, 0, length);
                    progress = ExecutePromptCommand(ref platform, frame, in state, in workspace,
                        in segment, ref expansion, out command, out result);
                }
            }
        }
        var status = progress == ShellScriptPromptProgress.Failed ? ShellScriptStepStatus.PlatformFailure :
            progress == ShellScriptPromptProgress.Waiting ? ShellScriptStepStatus.Waiting : ShellScriptStepStatus.Empty;
        if (progress == ShellScriptPromptProgress.Failed) result = (int)ShellCommandResult.Error;
        step = MakeStep(status, command, result, state.CurrentLine, state.CurrentOffset, 0);
        return status;
    }

    private static bool CommandBufferLength<TPlatform>(ref TPlatform platform,
        in ShellScriptStepWorkspace workspace, out uint length)
        where TPlatform : struct, IShellPlatform
    {
        length = 0;
        while (length < workspace.LineCapacity)
        {
            if (platform.ReadUInt8(workspace.Line, (int)length) == 0) return true;
            length++;
        }
        return false;
    }

    private static bool AppendCommandCapture<TPlatform>(ref TPlatform platform,
        in ShellScriptStepWorkspace workspace, APTR text, uint count)
        where TPlatform : struct, IShellPlatform =>
        AppendCommandText(ref platform, in workspace, text, count, 1);

    private static bool AppendCommandText<TPlatform>(ref TPlatform platform,
        in ShellScriptStepWorkspace workspace, APTR text, uint count, uint capture)
        where TPlatform : struct, IShellPlatform
    {
        if (!CommandBufferLength(ref platform, in workspace, out var length) ||
            count >= workspace.LineCapacity - length) return false;
        for (uint i = 0; i < count; i++)
        {
            var ch = platform.ReadUInt8(text, (int)i);
            if (capture != 0 && (ch == 10 || ch == 13)) ch = (byte)' ';
            platform.WriteUInt8(workspace.Line, (int)(length + i), ch);
        }
        platform.WriteUInt8(workspace.Line, (int)(length + count), 0);
        return true;
    }

    private static bool TrimCommandCapture<TPlatform>(ref TPlatform platform,
        in ShellScriptStepWorkspace workspace)
        where TPlatform : struct, IShellPlatform
    {
        if (!CommandBufferLength(ref platform, in workspace, out var length)) return false;
        var start = platform.ReadUInt32(workspace.PromptTemplate, 0);
        if (start > length) return false;
        while (length > start)
        {
            var ch = platform.ReadUInt8(workspace.Line, (int)(length - 1));
            if (ch != (byte)' ' && ch != (byte)'\t') break;
            length--;
        }
        platform.WriteUInt8(workspace.Line, (int)length, 0);
        return true;
    }
}
