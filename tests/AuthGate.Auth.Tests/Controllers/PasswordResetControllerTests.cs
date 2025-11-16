using AutoFixture;
using AuthGate.Auth.Controllers;
using AuthGate.Auth.Tests.Fixtures;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace AuthGate.Auth.Tests.Controllers;

public class PasswordResetControllerTests : BaseTestFixture
{
    [Fact]
    public void Controller_HasCorrectAttributes()
    {
        // Assert
        var controllerType = typeof(PasswordResetController);
        
        controllerType.GetCustomAttributes(typeof(ApiControllerAttribute), false)
            .Should().NotBeEmpty();
    }

    [Fact]
    public void Controller_HasCorrectRoute()
    {
        // Arrange
        var controllerType = typeof(PasswordResetController);
        var routeAttr = controllerType.GetCustomAttributes(typeof(RouteAttribute), false)
            .FirstOrDefault() as RouteAttribute;

        // Assert
        routeAttr.Should().NotBeNull();
        routeAttr!.Template.Should().Be("api/[controller]");
    }

    // Note: Check actual method names in PasswordResetController
}
