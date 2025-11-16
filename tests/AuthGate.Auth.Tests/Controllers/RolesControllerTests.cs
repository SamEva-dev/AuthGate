using AutoFixture;
using AuthGate.Auth.Controllers;
using AuthGate.Auth.Tests.Fixtures;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AuthGate.Auth.Tests.Controllers;

public class RolesControllerTests : BaseTestFixture
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<ILogger<RolesController>> _loggerMock;

    public RolesControllerTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _loggerMock = new Mock<ILogger<RolesController>>();
    }

    #region Controller Tests

    [Fact]
    public void Controller_HasCorrectAttributes()
    {
        // Assert
        var controllerType = typeof(RolesController);
        
        controllerType.GetCustomAttributes(typeof(ApiControllerAttribute), false)
            .Should().NotBeEmpty();
            
        controllerType.GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), false)
            .Should().NotBeEmpty();
    }

    [Fact]
    public void Controller_HasCorrectRoute()
    {
        // Arrange
        var controllerType = typeof(RolesController);
        var routeAttr = controllerType.GetCustomAttributes(typeof(RouteAttribute), false)
            .FirstOrDefault() as RouteAttribute;

        // Assert
        routeAttr.Should().NotBeNull();
        routeAttr!.Template.Should().Be("api/[controller]");
    }

    [Fact]
    public void GetRoles_MethodExists()
    {
        // Arrange
        var method = typeof(RolesController).GetMethod("GetRoles");

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(Task<IActionResult>));
    }

    [Fact]
    public void AssignPermission_MethodExists()
    {
        // Arrange
        var method = typeof(RolesController).GetMethod("AssignPermission");

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(Task<IActionResult>));
    }

    [Fact]
    public void RemovePermission_MethodExists()
    {
        // Arrange
        var method = typeof(RolesController).GetMethod("RemovePermission");

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(Task<IActionResult>));
    }

    #endregion
}
