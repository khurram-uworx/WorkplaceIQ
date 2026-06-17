namespace WorkplaceIQ.Tests.TestDoubles;

using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

internal sealed class TestHostEnvironment(string environmentName) : IHostEnvironment
{
    public string EnvironmentName { get; set; } = environmentName;

    public string ApplicationName { get; set; } = "WorkplaceIQ.Tests";

    public string ContentRootPath { get; set; } = TestContext.CurrentContext.WorkDirectory;

    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}
