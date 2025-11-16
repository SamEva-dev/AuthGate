using AutoFixture;
using AuthGate.Auth.Controllers;
using AuthGate.Auth.Tests.Fixtures;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace AuthGate.Auth.Tests.Controllers;

public class RegisterControllerTests : BaseTestFixture
{
    [Fact]
    public void Controller_HasCorrectAttributes()
    {
        // Assert
        var controllerType = typeof(RegisterController);
        
        controllerType.GetCustomAttributes(typeof(ApiControllerAttribute), false)
            .Should().NotBeEmpty();
    }

    [Fact]
    public void Controller_HasCorrectRoute()
    {
        // Arrange
        var controllerType = typeof(RegisterController);
        var routeAttr = controllerType.GetCustomAttributes(typeof(RouteAttribute), false)
            .FirstOrDefault() as RouteAttribute;

        // Assert
        routeAttr.Should().NotBeNull();
        routeAttr!.Template.Should().Be("api/[controller]");
    }

    [Fact]
    public void Register_HasAllowAnonymousAttribute()
    {
        // Arrange
        var method = typeof(RegisterController).GetMethod("Register");

        // Assert
        method.Should().NotBeNull();
        var allowAnonymousAttr = method!.GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute), false)
            .FirstOrDefault();
        allowAnonymousAttr.Should().NotBeNull();
    }

    [Fact]
    public void Register_MethodExists()
    {
        // Arrange
        var method = typeof(RegisterController).GetMethod("Register");

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(Task<IActionResult>));
    }
}
