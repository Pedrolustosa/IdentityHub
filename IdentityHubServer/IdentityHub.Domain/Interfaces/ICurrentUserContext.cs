namespace IdentityHub.Domain.Interfaces;

public interface ICurrentUserContext
{
    string? UserId { get; }
}
