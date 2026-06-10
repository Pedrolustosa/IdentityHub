namespace IdentityHub.Domain.Constants;

public static class SecurityAlertEventTypes
{
    public const string SuspiciousLogin = "Security.Alert.SuspiciousLogin";
    public const string CriticalAction = "Security.Alert.CriticalAction";

    public static IReadOnlyList<string> All() =>
    [
        SuspiciousLogin,
        CriticalAction
    ];
}