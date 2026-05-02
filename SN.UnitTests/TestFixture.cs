using AutoFixture;
using AutoFixture.AutoNSubstitute;

namespace SN.UnitTests;

public abstract class TestFixture
{
    protected Fixture Fixture;
    protected string randomText = string.Empty;

    protected TestFixture()
    {
        Fixture = new Fixture();
        Fixture.Customize(new AutoNSubstituteCustomization());
        randomText = Fixture.Create<string>();
    }
}
