using AutoFixture;
using AuthGate.Auth.Controllers;
using AuthGate.Auth.Tests.Fixtures;
using AuthGate.Auth.Application.DTOs.Auth;
using AuthGate.Auth.Application.Features.Auth.Commands.Login;
using AuthGate.Auth.Application.Features.Auth.Commands.Register;
using AuthGate.Auth.Domain.Common;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AuthGate.Auth.Tests.Controllers;

public class AuthControllerTests : BaseTestFixture
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<ILogger<AuthController>> _loggerMock;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _loggerMock = new Mock<ILogger<AuthController>>();
        
        // Note: UserManager and RoleManager need more complex setup
        // For now, we'll focus on testing with mocked IMediator
        _controller = null!; // TODO: Proper initialization with mocked UserManager/RoleManager
    }

    #region Register Tests

    [Fact]
    public void Register_HasCorrectAttributes()
    {
        // Assert
        var method = typeof(AuthController).GetMethod("Register");
        method.Should().NotBeNull();
        
        var allowAnonymousAttr = method!.GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute), false)
            .FirstOrDefault();
        allowAnonymousAttr.Should().NotBeNull();
    }

    [Fact]
    public void Login_MethodExists()
    {
        // Arrange
        var method = typeof(AuthController).GetMethod("Login");

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(Task<IActionResult>));
    }

    [Fact]
    public void RefreshToken_MethodExists()
    {
        // Arrange
        var method = typeof(AuthController).GetMethod("RefreshToken");

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(Task<IActionResult>));
    }

    #endregion

    #region Controller Tests

    [Fact]
    public void Controller_HasCorrectAttributes()
    {
        // Assert
        var controllerType = typeof(AuthController);
        
        controllerType.GetCustomAttributes(typeof(ApiControllerAttribute), false)
            .Should().NotBeEmpty();
    }

    [Fact]
    public void Controller_HasCorrectRoute()
    {
        // Arrange
        var controllerType = typeof(AuthController);
        var routeAttr = controllerType.GetCustomAttributes(typeof(RouteAttribute), false)
            .FirstOrDefault() as RouteAttribute;

        // Assert
        routeAttr.Should().NotBeNull();
        routeAttr!.Template.Should().Be("api/[controller]");
    }

    #endregion
}
