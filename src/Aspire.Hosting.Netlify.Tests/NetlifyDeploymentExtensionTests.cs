using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting.Netlify.Tests;

public class NetlifyDeploymentExtensionTests
{
    [Fact]
    public void WithNetlifyDeployment_AddsAnnotation_WhenInPublishMode()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder([
            "Publishing:Publisher=manifest",
            "Publishing:OutputPath=./publish"
        ]);

        var nodeApp = builder.AddNpmApp("test-app", "./test-app");

        // Act
        nodeApp.PublishAsNetlifySite("dist", "test-site");

        using var app = builder.Build();
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        // Assert
        var nodeResource = Assert.Single(appModel.Resources.OfType<NodeAppResource>());
        Assert.True(nodeResource.TryGetAnnotationsOfType<NetlifyDeploymentAnnotation>(out var annotations));
        var annotation = Assert.Single(annotations);
        Assert.NotNull(annotation.Resource);
        Assert.Equal("test-app-netlify-deploy", annotation.Resource.Name);
    }

    [Fact]
    public void WithNetlifyDeployment_DoesNotAddAnnotation_WhenNotInPublishMode()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder();
        var nodeApp = builder.AddNpmApp("test-app", "./test-app");

        // Act
        nodeApp.PublishAsNetlifySite();

        using var app = builder.Build();
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        // Assert
        var nodeResource = Assert.Single(appModel.Resources.OfType<NodeAppResource>());
        Assert.False(nodeResource.TryGetAnnotationsOfType<NetlifyDeploymentAnnotation>(out _));
    }

    [Theory]
    [InlineData("prod")]
    [InlineData("staging")]
    [InlineData("preview")]
    [InlineData("custom-env")]
    public void WithNetlifyDeployment_ConfiguresDeploymentEnvironment_Correctly(string environment)
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder([
            "Publishing:Publisher=manifest",
            "Publishing:OutputPath=./publish"
        ]);

        var nodeApp = builder.AddNpmApp("test-app", "./test-app");

        // Act
        nodeApp.PublishAsNetlifySite(deploymentEnvironment: environment);

        using var app = builder.Build();
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        // Assert
        var nodeResource = Assert.Single(appModel.Resources.OfType<NodeAppResource>());
        Assert.True(nodeResource.TryGetAnnotationsOfType<NetlifyDeploymentAnnotation>(out var annotations));
        var annotation = Assert.Single(annotations);
        Assert.Equal(environment, annotation.Resource.DeploymentEnvironment);
    }

    [Fact]
    public void WithNetlifyDeployment_DefaultsToPreviewEnvironment()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder([
            "Publishing:Publisher=manifest",
            "Publishing:OutputPath=./publish"
        ]);

        var nodeApp = builder.AddNpmApp("test-app", "./test-app");

        // Act
        nodeApp.PublishAsNetlifySite();

        using var app = builder.Build();
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        // Assert
        var nodeResource = Assert.Single(appModel.Resources.OfType<NodeAppResource>());
        Assert.True(nodeResource.TryGetAnnotationsOfType<NetlifyDeploymentAnnotation>(out var annotations));
        var annotation = Assert.Single(annotations);
        Assert.Equal("preview", annotation.Resource.DeploymentEnvironment);
    }

    [Fact]
    public void NetlifyDeployerResource_StoresAllConfigurationCorrectly()
    {
        // Arrange & Act
        var deployer = new NetlifyDeployerResource(
            "test-deployer",
            "/path/to/working/dir",
            "build",
            "test-site",
            "staging"
        );

        // Assert
        Assert.Equal("test-deployer", deployer.Name);
        Assert.Equal("/path/to/working/dir", deployer.WorkingDirectory);
        Assert.Equal("build", deployer.BuildDirectory);
        Assert.Equal("test-site", deployer.SiteName);
        Assert.Equal("staging", deployer.DeploymentEnvironment);
        Assert.Equal("netlify", deployer.Command);
    }

    [Fact]
    public void WithNpmRunCommand_AddsAnnotation_WhenInDevelopmentMode()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder();
        var nodeApp = builder.AddNpmApp("test-app", "./test-app");

        // Act
        nodeApp.WithNpmRunCommand("build");

        using var app = builder.Build();
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        // Assert
        var nodeResource = Assert.Single(appModel.Resources.OfType<NodeAppResource>());
        Assert.True(nodeResource.TryGetAnnotationsOfType<NpmRunnerAnnotation>(out var annotations));
        var annotation = Assert.Single(annotations);
        Assert.NotNull(annotation.Resource);
        Assert.Equal("test-app-npm-run-build", annotation.Resource.Name);
        Assert.Equal("build", annotation.Resource.ScriptName);
    }

    [Fact]
    public void WithNpmRunCommand_DoesNotAddAnnotation_WhenInPublishMode()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder([
            "Publishing:Publisher=manifest",
            "Publishing:OutputPath=./publish"
        ]);
        var nodeApp = builder.AddNpmApp("test-app", "./test-app");

        // Act
        nodeApp.WithNpmRunCommand("build");

        using var app = builder.Build();
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        // Assert
        var nodeResource = Assert.Single(appModel.Resources.OfType<NodeAppResource>());
        Assert.False(nodeResource.TryGetAnnotationsOfType<NpmRunnerAnnotation>(out _));
    }

    [Theory]
    [InlineData("build")]
    [InlineData("test")]
    [InlineData("lint")]
    [InlineData("compile")]
    public void WithNpmRunCommand_ConfiguresScriptName_Correctly(string scriptName)
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder();
        var nodeApp = builder.AddNpmApp("test-app", "./test-app");

        // Act
        nodeApp.WithNpmRunCommand(scriptName);

        using var app = builder.Build();
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        // Assert
        var nodeResource = Assert.Single(appModel.Resources.OfType<NodeAppResource>());
        Assert.True(nodeResource.TryGetAnnotationsOfType<NpmRunnerAnnotation>(out var annotations));
        var annotation = Assert.Single(annotations);
        Assert.Equal(scriptName, annotation.Resource.ScriptName);
        Assert.Equal($"test-app-npm-run-{scriptName}", annotation.Resource.Name);
    }

    [Fact]
    public async Task WithNpmRunCommand_CreatesRunnerResource_WithCorrectArguments()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder();
        var nodeApp = builder.AddNpmApp("test-app", "./test-app");

        // Act
        nodeApp.WithNpmRunCommand("build");

        using var app = builder.Build();
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        // Assert
        var runnerResource = Assert.Single(appModel.Resources.OfType<NpmRunnerResource>());
        Assert.Equal("test-app-npm-run-build", runnerResource.Name);
        Assert.Equal("npm", runnerResource.Command);
        Assert.EndsWith("test-app", runnerResource.WorkingDirectory);
        Assert.Equal("build", runnerResource.ScriptName);

        // Verify arguments
        var args = await runnerResource.GetArgumentValuesAsync();
        Assert.Collection(args,
            arg => Assert.Equal("run", arg),
            arg => Assert.Equal("build", arg)
        );
    }

    [Fact]
    public async Task WithNpmRunCommand_CanAcceptAdditionalArgs()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder();
        var nodeApp = builder.AddNpmApp("test-app", "./test-app");

        // Act
        nodeApp.WithNpmRunCommand("build", configureRunner: runnerBuilder =>
        {
            runnerBuilder.WithArgs("--verbose", "--production");
        });

        using var app = builder.Build();
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        // Assert
        var runnerResource = Assert.Single(appModel.Resources.OfType<NpmRunnerResource>());

        var args = await runnerResource.GetArgumentValuesAsync();
        Assert.Collection(args,
            arg => Assert.Equal("run", arg),
            arg => Assert.Equal("build", arg),
            arg => Assert.Equal("--verbose", arg),
            arg => Assert.Equal("--production", arg)
        );
    }

    [Fact]
    public void NpmRunnerResource_StoresAllConfigurationCorrectly()
    {
        // Arrange & Act
        var runner = new NpmRunnerResource(
            "test-runner",
            "/path/to/working/dir",
            "build"
        );

        // Assert
        Assert.Equal("test-runner", runner.Name);
        Assert.Equal("/path/to/working/dir", runner.WorkingDirectory);
        Assert.Equal("build", runner.ScriptName);
        Assert.Equal("npm", runner.Command);
    }

    [Fact]
    public void WithNpmRunCommand_ThrowsArgumentNullException_ForNullBuilder()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            ((IResourceBuilder<NodeAppResource>)null!).WithNpmRunCommand("build"));
    }

    [Fact]
    public void WithNpmRunCommand_ThrowsArgumentNullException_ForNullScriptName()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder();
        var nodeApp = builder.AddNpmApp("test-app", "./test-app");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => nodeApp.WithNpmRunCommand(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void WithNpmRunCommand_ThrowsArgumentException_ForEmptyScriptName(string scriptName)
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder();
        var nodeApp = builder.AddNpmApp("test-app", "./test-app");

        // Act & Assert
        Assert.Throws<ArgumentException>(() => nodeApp.WithNpmRunCommand(scriptName));
    }
}