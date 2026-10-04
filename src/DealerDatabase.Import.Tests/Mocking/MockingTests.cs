using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using DealerDatabase.Import.Abstractions;
using DealerDatabase.Import.Importing;
using Xunit;

namespace DealerDatabase.Import.Tests.Mocking;

public class MockingTests
{
    [Fact]
    public void CanReplace_ISourceDataLoader_With_Mock_And_Verify_LoadAll_Called()
    {
        var services = new ServiceCollection();
        // register import services (real implementations)
        services.AddDealerImportServices();

        // create a mock loader and replace the service registration with the mock
        var mockLoader = new Mock<ISourceDataLoader>();
        mockLoader.Setup(x => x.LoadAll()).Returns(new List<SourceDealerRecord>());

        services.AddSingleton(mockLoader.Object);

        var provider = services.BuildServiceProvider();

        var loader = provider.GetRequiredService<ISourceDataLoader>();
        var records = loader.LoadAll();

        // Verify the mock was used and LoadAll was called
        mockLoader.Verify(x => x.LoadAll(), Times.Once);
        Assert.NotNull(records);
    }
}
