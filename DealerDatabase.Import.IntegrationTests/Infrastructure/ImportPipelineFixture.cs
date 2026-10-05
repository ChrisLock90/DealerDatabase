namespace DealerDatabase.Import.IntegrationTests.Infrastructure;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

internal sealed class ImportPipelineFixture : IDisposable
{
    private readonly IHost _host;

    public ImportPipelineFixture()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddDealerImportServices();
        _host = builder.Build();
    }

    public T Resolve<T>() where T : notnull => _host.Services.GetRequiredService<T>();

    public void Dispose() => _host.Dispose();
}
