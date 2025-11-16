using AutoFixture;
using AutoFixture.AutoMoq;

namespace AuthGate.Auth.Tests.Fixtures;

/// <summary>
/// Base test fixture providing AutoFixture with AutoMoq customization
/// for all test classes
/// </summary>
public class BaseTestFixture
{
    protected IFixture Fixture { get; }

    public BaseTestFixture()
    {
        Fixture = new Fixture()
            .Customize(new AutoMoqCustomization { ConfigureMembers = true });

        // Configure to avoid circular references
        Fixture.Behaviors.OfType<ThrowingRecursionBehavior>()
            .ToList()
            .ForEach(b => Fixture.Behaviors.Remove(b));
        
        Fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        // Register custom configurations
        CustomizeFixture();
    }

    protected virtual void CustomizeFixture()
    {
        // Override in derived classes to add custom configurations
        // Example: Fixture.Register<IMyService>(() => new MyService());
    }
}
