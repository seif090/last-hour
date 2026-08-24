using FluentAssertions;
using LastHour.BuildingBlocks.SharedKernel.Results;
using LastHour.Modules.Identity.Application.Commands;
using LastHour.Modules.Identity.Domain.Roles;
using LastHour.Modules.Identity.Tests.Application.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;

namespace LastHour.Modules.Identity.Tests.Application.Commands;

public class CreateRoleCommandHandlerTests
{
    private static CreateRoleCommandHandler CreateHandler(FakeRoleRepository roles)
        => new CreateRoleCommandHandler(roles, NullLogger<CreateRoleCommandHandler>.Instance);

    [Fact]
    public async Task Handle_ValidCommand_CreatesRoleWithNormalizedName()
    {
        var roles = new FakeRoleRepository();
        CreateRoleCommandHandler handler = CreateHandler(roles);

        Result<RoleId> result = await handler.Handle(
            new CreateRoleCommand("Administrator", "Manages the platform"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        Role created = roles.All.Should().ContainSingle().Subject;
        created.Id.Should().Be(result.Value);
        created.Name.Value.Should().Be("Administrator");
        created.Name.NormalizedValue.Should().Be("administrator");
        created.Description.Should().Be("Manages the platform");
    }

    [Fact]
    public async Task Handle_DuplicateNormalizedName_ReturnsConflictAndDoesNotAdd()
    {
        var roles = new FakeRoleRepository();
        roles.AddRole("Administrator");
        CreateRoleCommandHandler handler = CreateHandler(roles);

        Result<RoleId> result = await handler.Handle(
            new CreateRoleCommand("administrator", null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Type.Should().Be(ErrorType.Conflict);
        result.FirstError!.Code.Should().Be("Roles.NameAlreadyExists");
        roles.All.Should().ContainSingle();
    }

    [Fact]
    public async Task Handle_EmptyName_ReturnsValidationFailure()
    {
        CreateRoleCommandHandler handler = CreateHandler(new FakeRoleRepository());

        Result<RoleId> result = await handler.Handle(
            new CreateRoleCommand("   ", null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Type.Should().Be(ErrorType.Validation);
        result.FirstError!.Code.Should().Be("InvalidRoleName");
    }

    [Fact]
    public async Task Handle_OverlongDescription_ReturnsValidationFailure()
    {
        CreateRoleCommandHandler handler = CreateHandler(new FakeRoleRepository());

        Result<RoleId> result = await handler.Handle(
            new CreateRoleCommand("Viewer", new string('a', Role.MaxDescriptionLength + 1)),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Type.Should().Be(ErrorType.Validation);
    }
}
