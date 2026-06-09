using FluentValidation;
using IdentityHub.Application.CQRS.Roles.Commands;

namespace IdentityHub.Application.CQRS.Roles.Validators;

public sealed class CreateRoleCommandValidator : AbstractValidator<CreateRoleCommand>
{
    public CreateRoleCommandValidator()
    {
        RuleFor(x => x.Request.Name).NotEmpty().MaximumLength(64);
    }
}

public sealed class UpdateRoleCommandValidator : AbstractValidator<UpdateRoleCommand>
{
    public UpdateRoleCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Request.Name).NotEmpty().MaximumLength(64);
    }
}

public sealed class DeleteRoleCommandValidator : AbstractValidator<DeleteRoleCommand>
{
    public DeleteRoleCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public sealed class UpdateRolePermissionsCommandValidator : AbstractValidator<UpdateRolePermissionsCommand>
{
    public UpdateRolePermissionsCommandValidator()
    {
        RuleFor(x => x.RoleId).NotEmpty();

        RuleFor(x => x.Permissions)
            .NotNull()
            .Must(permissions => permissions is { Count: > 0 })
            .WithMessage("At least one permission is required.");

        RuleForEach(x => x.Permissions)
            .NotEmpty()
            .MaximumLength(128);
    }
}
