using IdentityHub.Application.Common.Errors;
using IdentityHub.Application.Common.Results;
using IdentityHub.Application.DTOs;
using IdentityHub.Application.Interfaces;
using IdentityHub.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace IdentityHub.Infrastructure.Services;

public sealed class UserInvitesService : IUserInvitesService
{
    private readonly AppDbContext _dbContext;

    public UserInvitesService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<UserInvitesPagedResponse>> GetUserInvitesAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var totalCount = await _dbContext.UserInvites
            .Where(i => i.IsActive)
            .CountAsync(cancellationToken);

        var invites = await _dbContext.UserInvites
            .Where(i => i.IsActive)
            .OrderByDescending(i => i.SentAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(i => new UserInviteResponse
            {
                Id = i.Id,
                Email = i.Email,
                FullName = i.FullName,
                Status = i.Status,
                SentAt = i.SentAt,
                ExpiresAt = i.ExpiresAt,
                Roles = i.Roles
            })
            .ToListAsync(cancellationToken);

        var totalPages = (totalCount + pageSize - 1) / pageSize;

        var response = new UserInvitesPagedResponse
        {
            Items = invites,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        };

        return Result<UserInvitesPagedResponse>.Success(response);
    }

    public async Task<Result> ResendUserInviteAsync(Guid inviteId, CancellationToken cancellationToken = default)
    {
        var invite = await _dbContext.UserInvites
            .FirstOrDefaultAsync(i => i.Id == inviteId && i.IsActive, cancellationToken);

        if (invite is null)
        {
            return Result.Failure(Error.Create("Invite.NotFound", "User invite not found"));
        }

        // Reset expiration to 7 days from now
        invite.ExpiresAt = DateTime.UtcNow.AddDays(7);
        invite.SentAt = DateTime.UtcNow;

        _dbContext.UserInvites.Update(invite);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> CancelUserInviteAsync(Guid inviteId, CancellationToken cancellationToken = default)
    {
        var invite = await _dbContext.UserInvites
            .FirstOrDefaultAsync(i => i.Id == inviteId && i.IsActive, cancellationToken);

        if (invite is null)
        {
            return Result.Failure(Error.Create("Invite.NotFound", "User invite not found"));
        }

        invite.Status = "Canceled";
        invite.CanceledAt = DateTime.UtcNow;
        invite.IsActive = false;

        _dbContext.UserInvites.Update(invite);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
