using AuthGate.Auth.Application.Features.Auth.Commands.Register;
using AuthGate.Auth.Application.Tests.Fixtures;
using AuthGate.Auth.Domain.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AuthGate.Auth.Application.Tests.Features.Auth.Commands;

public class RegisterCommandHandlerTests : BaseApplicationTestFixture
{
    private readonly Mock<UserManager<User>> _userManagerMock;
    private readonly Mock<ILogger<RegisterCommandHandler>> _loggerMock;
    private readonly RegisterCommandHandler _handler;

    public RegisterCommandHandlerTests()
    {
        var userStore = new Mock<IUserStore<User>>();
        _userManagerMock = new Mock<UserManager<User>>(
            userStore.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        _loggerMock = new Mock<ILogger<RegisterCommandHandler>>();

        _handler = new RegisterCommandHandler(
            _userManagerMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidCommand_CreatesUserSuccessfully()
    {
        // Arrange
        var command = new RegisterCommand
        {
            Email = "test@test.com",
            Password = "Test@1234",
            ConfirmPassword = "Test@1234",
            FirstName = "Test",
            LastName = "User"
        };

        _userManagerMock
            .Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);

        _userManagerMock
            .Setup(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Email.Should().Be(command.Email);
        
        _userManagerMock.Verify(
            x => x.CreateAsync(It.Is<User>(u => u.Email == command.Email), command.Password),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithExistingEmail_ReturnsFailure()
    {
        // Arrange
        var command = new RegisterCommand
        {
            Email = "existing@test.com",
            Password = "Test@1234",
            ConfirmPassword = "Test@1234"
        };

        var existingUser = new User { Email = command.Email };
        _userManagerMock
            .Setup(x => x.FindByEmailAsync(command.Email))
            .ReturnsAsync(existingUser);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already exists");
    }

    [Fact]
    public async Task Handle_WhenUserCreationFails_ReturnsFailure()
    {
        // Arrange
        var command = new RegisterCommand
        {
            Email = "test@test.com",
            Password = "Test@1234",
            ConfirmPassword = "Test@1234"
        };

        _userManagerMock
            .Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);

        _userManagerMock
            .Setup(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Password too weak" }));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Password too weak");
    }
}
