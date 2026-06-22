namespace IdentityHub.Application.DTOs;

public sealed class UserInviteResponse
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string Roles { get; set; } = string.Empty;
}

public sealed class UserInvitesPagedResponse
{
    public List<UserInviteResponse> Items { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}

public sealed class ResendInviteRequest
{
    public Guid InviteId { get; set; }
}

public sealed class CancelInviteRequest
{
    public Guid InviteId { get; set; }
}
