namespace IdentityHub.Domain.Constants;

public static class SecurityAlertEventTypes
{
    public const string SuspiciousLogin = "Security.Alert.SuspiciousLogin";
    public const string CriticalAction = "Security.Alert.CriticalAction";
    public const string RefreshTokenReuse = "Security.Alert.RefreshTokenReuse";

    public static IReadOnlyList<string> All() =>
    [
        SuspiciousLogin,
        CriticalAction,
        RefreshTokenReuse
    ];
}

public static class SecurityEventSeverity
{
    public const string Low = "Low";
    public const string Medium = "Medium";
    public const string High = "High";
    public const string Critical = "Critical";

    public static IReadOnlyList<string> All() =>
    [
        Low,
        Medium,
        High,
        Critical
    ];
}

public static class SecurityEventStatus
{
    public const string Open = "Open";
    public const string Reviewed = "Reviewed";
    public const string Ignored = "Ignored";
    public const string Resolved = "Resolved";

    public static IReadOnlyList<string> All() =>
    [
        Open,
        Reviewed,
        Ignored,
        Resolved
    ];
}