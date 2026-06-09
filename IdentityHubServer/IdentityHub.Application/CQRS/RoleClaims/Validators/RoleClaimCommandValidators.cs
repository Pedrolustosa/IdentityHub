using FluentValidation;
using IdentityHub.Application.CQRS.RoleClaims.Commands;

namespace IdentityHub.Application.CQRS.RoleClaims.Validators;

public sealed class AddRoleClaimPermissionCommandValidator : AbstractValidator<AddRoleClaimPermissionCommand>
{
    public AddRoleClaimPermissionCommandValidator()
    {
        RuleFor(x => x.RoleId).NotEmpty();
        RuleFor(x => x.Permission).NotEmpty().MaximumLength(128);
    }
}

public sealed class RemoveRoleClaimPermissionCommandValidator : AbstractValidator<RemoveRoleClaimPermissionCommand>
{
    public RemoveRoleClaimPermissionCommandValidator()
    {
        RuleFor(x => x.RoleId).NotEmpty();
        RuleFor(x => x.Permission).NotEmpty().MaximumLength(128);
    }
}

public sealed class ReplaceRoleClaimPermissionsCommandValidator : AbstractValidator<ReplaceRoleClaimPermissionsCommand>
{
    public ReplaceRoleClaimPermissionsCommandValidator()
    {
        RuleFor(x => x.RoleId).NotEmpty();
        RuleFor(x => x.Permissions).NotNull();

        RuleForEach(x => x.Permissions)
            .NotEmpty()
            .MaximumLength(128);
    }
}
