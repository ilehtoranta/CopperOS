using Amiga;

namespace CopperOS.Shell;

/// <summary>
/// Dispatches one resolved internal command through the fixed Shell command
/// boundary. Name lookup and command semantics remain separate: this type
/// only selects an already implemented command and supplies its workspace
/// buffers.
/// </summary>
public static class ShellCommandDispatcher
{
    public static int DispatchByName<TPlatform>(
        ref TPlatform platform,
        in CommandInvocation invocation,
        APTR commandName,
        uint commandNameLength,
        in ShellCommandWorkspace workspace)
        where TPlatform : struct, IShellPlatform
    {
        var command = ShellInternalCommandResolver.Resolve(
            ref platform, commandName, commandNameLength);
        return command == ShellInternalCommand.Unknown
            ? (int)ShellCommandResult.Error
            : Dispatch(ref platform, in invocation, command, in workspace);
    }

    public static int Dispatch<TPlatform>(
        ref TPlatform platform,
        in CommandInvocation invocation,
        ShellInternalCommand command,
        in ShellCommandWorkspace workspace)
        where TPlatform : struct, IShellPlatform
    {
        switch (command)
        {
            case ShellInternalCommand.Alias:
                return AliasCommand.Execute(ref platform, in invocation,
                    workspace.First, workspace.FirstCapacity,
                    workspace.Second, workspace.SecondCapacity);
            case ShellInternalCommand.Ask:
                return AskCommand.Execute(ref platform, in invocation,
                    workspace.First, workspace.FirstCapacity);
            case ShellInternalCommand.CD:
                return CdCommand.Execute(ref platform, in invocation,
                    workspace.Token, workspace.TokenCapacity,
                    workspace.First, workspace.FirstCapacity);
            case ShellInternalCommand.Cls:
                return ClsCommand.Execute(ref platform, in invocation,
                    workspace.Token, workspace.TokenCapacity);
            case ShellInternalCommand.Echo:
                return EchoCommand.ParseAndExecute(ref platform, in invocation,
                    workspace.First, workspace.FirstCapacity,
                    workspace.Token, workspace.TokenCapacity,
                    workspace.Second, workspace.SecondCapacity);
            case ShellInternalCommand.Else:
                return ElseCommand.Execute(ref platform, in invocation,
                    workspace.Token, workspace.TokenCapacity);
            case ShellInternalCommand.EndCLI:
                return EndCliCommand.Execute(ref platform, in invocation,
                    workspace.Token, workspace.TokenCapacity);
            case ShellInternalCommand.EndIf:
                return EndIfCommand.Execute(ref platform, in invocation,
                    workspace.Token, workspace.TokenCapacity);
            case ShellInternalCommand.EndShell:
                return EndShellCommand.Execute(ref platform, in invocation,
                    workspace.Token, workspace.TokenCapacity);
            case ShellInternalCommand.EndSkip:
                return EndSkipCommand.Execute(ref platform, in invocation,
                    workspace.Token, workspace.TokenCapacity);
            case ShellInternalCommand.Failat:
                return FailatCommand.Execute(ref platform, in invocation,
                    workspace.Token, workspace.TokenCapacity);
            case ShellInternalCommand.Fault:
                return FaultCommand.Execute(ref platform, in invocation,
                    workspace.Token, workspace.TokenCapacity,
                    workspace.ErrorCodes, workspace.ErrorCodeCapacity);
            case ShellInternalCommand.Get:
                return GetCommand.Execute(ref platform, in invocation,
                    workspace.Token, workspace.TokenCapacity,
                    workspace.First, workspace.FirstCapacity);
            case ShellInternalCommand.Getenv:
                return GetenvCommand.Execute(ref platform, in invocation,
                    workspace.Token, workspace.TokenCapacity,
                    workspace.First, workspace.FirstCapacity);
            case ShellInternalCommand.If:
                return IfCommand.Execute(ref platform, in invocation,
                    workspace.Token, workspace.TokenCapacity,
                    workspace.First, workspace.FirstCapacity,
                    workspace.Second, workspace.SecondCapacity);
            case ShellInternalCommand.Lab:
                return LabCommand.Execute(ref platform, in invocation,
                    workspace.Token, workspace.TokenCapacity,
                    workspace.First, workspace.FirstCapacity);
            case ShellInternalCommand.NewCLI:
                return NewCliCommand.Execute(ref platform, in invocation,
                    workspace.Token, workspace.TokenCapacity,
                    workspace.First, workspace.FirstCapacity,
                    workspace.Second, workspace.SecondCapacity);
            case ShellInternalCommand.NewShell:
                return NewShellCommand.Execute(ref platform, in invocation,
                    workspace.Token, workspace.TokenCapacity,
                    workspace.First, workspace.FirstCapacity,
                    workspace.Second, workspace.SecondCapacity);
            case ShellInternalCommand.Path:
                return PathCommand.Execute(ref platform, in invocation,
                    workspace.Token, workspace.TokenCapacity,
                    workspace.First, workspace.FirstCapacity);
            case ShellInternalCommand.Prompt:
                return PromptCommand.Execute(ref platform, in invocation,
                    workspace.First, workspace.FirstCapacity);
            case ShellInternalCommand.Quit:
                return QuitCommand.Execute(ref platform, in invocation,
                    workspace.Token, workspace.TokenCapacity);
            case ShellInternalCommand.Resident:
                return ResidentCommand.Execute(ref platform, in invocation,
                    workspace.Token, workspace.TokenCapacity,
                    workspace.First, workspace.FirstCapacity,
                    workspace.Second, workspace.SecondCapacity,
                    workspace.Third, workspace.ThirdCapacity);
            case ShellInternalCommand.Run:
                return RunCommand.Execute(ref platform, in invocation,
                    workspace.Token, workspace.TokenCapacity,
                    workspace.First, workspace.FirstCapacity);
            case ShellInternalCommand.Set:
                return SetCommand.Execute(ref platform, in invocation,
                    workspace.First, workspace.FirstCapacity,
                    workspace.Second, workspace.SecondCapacity);
            case ShellInternalCommand.Setenv:
                return SetenvCommand.Execute(ref platform, in invocation,
                    workspace.First, workspace.FirstCapacity,
                    workspace.Second, workspace.SecondCapacity,
                    workspace.Third, workspace.ThirdCapacity);
            case ShellInternalCommand.Skip:
                return SkipCommand.Execute(ref platform, in invocation,
                    workspace.Token, workspace.TokenCapacity,
                    workspace.First, workspace.FirstCapacity);
            case ShellInternalCommand.Stack:
                return StackCommand.Execute(ref platform, in invocation,
                    workspace.Token, workspace.TokenCapacity);
            case ShellInternalCommand.Unalias:
                return UnaliasCommand.Execute(ref platform, in invocation,
                    workspace.Token, workspace.TokenCapacity,
                    workspace.First, workspace.FirstCapacity);
            case ShellInternalCommand.Unset:
                return UnsetCommand.Execute(ref platform, in invocation,
                    workspace.First, workspace.FirstCapacity,
                    workspace.Second, workspace.SecondCapacity);
            case ShellInternalCommand.Unsetenv:
                return UnsetenvCommand.Execute(ref platform, in invocation,
                    workspace.First, workspace.FirstCapacity,
                    workspace.Second, workspace.SecondCapacity);
            case ShellInternalCommand.Why:
                return WhyCommand.Execute(ref platform, in invocation,
                    workspace.Token, workspace.TokenCapacity);
            default:
                return (int)ShellCommandResult.Error;
        }
    }
}
