using AutoFixture;
using AuthGate.Auth.Controllers;
using AuthGate.Auth.Tests.Fixtures;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace AuthGate.Auth.Tests.Controllers;

public class JwksControllerTests : BaseTestFixture
{
    [Fact]
    public void Controller_HasCorrectAttributes()
    {
        // Assert
        var controllerType = typeof(JwksController);
        
        controllerType.GetCustomAttributes(typeof(ApiControllerAttribute), false)
            .Should().NotBeEmpty();
    }

    [Fact]
    public void Controller_HasCorrectRoute()
    {
        // Arrange
        var controllerType = typeof(JwksController);
        var routeAttr = controllerType.GetCustomAttributes(typeof(RouteAttribute), false)
            .FirstOrDefault() as RouteAttribute;

        // Assert
        routeAttr.Should().NotBeNull();
        routeAttr!.Template.Should().Be(".well-known");
    }

    [Fact]
    public void GetJwks_MethodExists()
    {
        // Arrange
        var method = typeof(JwksController).GetMethod("GetJwks");

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(IActionResult));
    }

    // Note: Check if GetJwks has AllowAnonymous attribute in actual controller
}
