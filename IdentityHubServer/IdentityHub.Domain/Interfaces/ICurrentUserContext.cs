namespace IdentityHub.Domain.Interfaces;

public interface ICurrentUserContext
{
    string? UserId { get; }
}

public interface IClientDeviceInfoProvider
{
    (string IpAddress, string Browser, string OperatingSystem) GetCurrent();
}
