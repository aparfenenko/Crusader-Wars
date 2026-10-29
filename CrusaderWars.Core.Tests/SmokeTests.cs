using CrusaderWars.Core.Logging;
using Xunit;

namespace CrusaderWars.Core.Tests;

public class SmokeTests
{
    [Fact]
    public void Core_assembly_is_referenced()
    {
        Assert.Equal("CrusaderWars.Core", typeof(IAppLogger).Assembly.GetName().Name);
    }
}
