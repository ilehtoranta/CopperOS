namespace CopperOS.Shell;

/// <summary>Condition modes understood by the DOS-owned IF evaluator.</summary>
public enum ShellIfCondition : uint
{
    PreviousResult = 1,
    Equal = 2,
    Greater = 3,
    GreaterEqual = 4,
    /// <summary>Legacy value-mode marker; new calls carry VAL separately.</summary>
    Value = 5,
    Exists = 6,
}
