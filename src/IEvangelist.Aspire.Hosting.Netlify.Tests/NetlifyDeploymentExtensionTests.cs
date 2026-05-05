// Copyright (c) David Pine. All rights reserved.
// Licensed under the MIT License.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.JavaScript;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting.Netlify.Tests;

public class NetlifyDeploymentExtensionTests
{
    private static IDistributedApplicationBuilder CreatePublishBuilder() =>
        DistributedApplication.CreateBuilder([
            "Publishing:Publisher=manifest",
            "Publishing:OutputPath=./publish"
        ]);

    [Fact]
    public void PublishAsNetlifySite_AddsDeploymentResource_WhenInPublishMode()
    {
        // Arrange
        var builder = CreatePublishBuilder();
        var jsApp = builder.AddJavaScriptApp("test-app", "./test-app");

        // Act
        jsApp.PublishAsNetlifySite(new NetlifyDeployOptions
        {
            Dir = "dist",
            Alias = "test-site"
        });

        using var app = builder.Build();
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        // Assert
        var deployment = Assert.Single(appModel.Resources.OfType<NetlifyDeploymentResource>());
        Assert.Equal("test-app-netlify-deploy", deployment.Name);
        Assert.Equal("dist", deployment.BuildDirectory);
    }

    [Fact]
    public void PublishAsNetlifySite_DoesNotAddDeploymentResource_WhenNotInPublishMode()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder();
        var jsApp = builder.AddJavaScriptApp("test-app", "./test-app");

        // Act
        jsApp.PublishAsNetlifySite(new NetlifyDeployOptions());

        using var app = builder.Build();
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        // Assert
        Assert.Empty(appModel.Resources.OfType<NetlifyDeploymentResource>());
    }

    [Theory]
    [InlineData(true, "prod")]
    [InlineData(false, "preview")]
    public void PublishAsNetlifySite_ConfiguresDeploymentEnvironment_Correctly(bool isProd, string expectedEnvironment)
    {
        // Arrange
        var builder = CreatePublishBuilder();
        var jsApp = builder.AddJavaScriptApp("test-app", "./test-app");

        // Act
        jsApp.PublishAsNetlifySite(new NetlifyDeployOptions
        {
            Prod = isProd
        });

        using var app = builder.Build();
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        // Assert
        var deployment = Assert.Single(appModel.Resources.OfType<NetlifyDeploymentResource>());
        Assert.Equal(expectedEnvironment, deployment.DeploymentEnvironment);
    }

    [Fact]
    public void PublishAsNetlifySite_DefaultsToPreviewEnvironment()
    {
        // Arrange
        var builder = CreatePublishBuilder();
        var jsApp = builder.AddJavaScriptApp("test-app", "./test-app");

        // Act
        jsApp.PublishAsNetlifySite(new NetlifyDeployOptions());

        using var app = builder.Build();
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        // Assert
        var deployment = Assert.Single(appModel.Resources.OfType<NetlifyDeploymentResource>());
        Assert.Equal("preview", deployment.DeploymentEnvironment);
    }

    [Fact]
    public void NetlifyDeploymentResource_StoresAllConfigurationCorrectly()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder();
        var jsApp = builder.AddJavaScriptApp("test-app", "test-app-dir");
        var options = new NetlifyDeployOptions
        {
            Dir = "build",
            Site = "test-site",
            Prod = false
        };

        // Act
        var deployer = new NetlifyDeploymentResource(
            "test-deployer",
            jsApp.Resource,
            options
        );

        // Assert
        Assert.Equal("test-deployer", deployer.Name);
        Assert.EndsWith("test-app-dir", deployer.WorkingDirectory);
        Assert.Equal("build", deployer.BuildDirectory);
        Assert.Equal("test-site", deployer.SiteName);
        Assert.Equal("preview", deployer.DeploymentEnvironment);
    }

    [Fact]
    public void WithNpmRunCommand_AddsRunnerResource()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder();
        var jsApp = builder.AddJavaScriptApp("test-app", "./test-app");

        // Act
        jsApp.WithNpmRunCommand("build");

        using var app = builder.Build();
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        // Assert
        var runner = Assert.Single(appModel.Resources.OfType<NpmCommandResource>());
        Assert.Equal("test-app-npm-run-build", runner.Name);
        Assert.Equal("build", runner.ScriptName);
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
        var jsApp = builder.AddJavaScriptApp("test-app", "./test-app");

        // Act
        jsApp.WithNpmRunCommand(scriptName);

        using var app = builder.Build();
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        // Assert
        var runner = Assert.Single(appModel.Resources.OfType<NpmCommandResource>());
        Assert.Equal(scriptName, runner.ScriptName);
        Assert.Equal($"test-app-npm-run-{scriptName}", runner.Name);
    }

    [Fact]
    public void WithNpmRunCommand_CanAcceptAdditionalArgs()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder();
        var jsApp = builder.AddJavaScriptApp("test-app", "./test-app");

        // Act
        jsApp.WithNpmRunCommand("build", configureRunner: runnerBuilder =>
        {
            runnerBuilder.WithArgs("--verbose", "--production");
        });

        using var app = builder.Build();
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        // Assert
        var runner = Assert.Single(appModel.Resources.OfType<NpmCommandResource>());
        Assert.Equal("npm", runner.Command);
        Assert.EndsWith("test-app", runner.WorkingDirectory);
    }

    [Fact]
    public void NpmCommandResource_StoresAllConfigurationCorrectly()
    {
        // Arrange & Act
        var runner = new NpmCommandResource(
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
            ((IResourceBuilder<JavaScriptAppResource>)null!).WithNpmRunCommand("build"));
    }

    [Fact]
    public void WithNpmRunCommand_ThrowsArgumentNullException_ForNullScriptName()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder();
        var jsApp = builder.AddJavaScriptApp("test-app", "./test-app");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => jsApp.WithNpmRunCommand(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void WithNpmRunCommand_ThrowsArgumentException_ForEmptyScriptName(string scriptName)
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder();
        var jsApp = builder.AddJavaScriptApp("test-app", "./test-app");

        // Act & Assert
        Assert.Throws<ArgumentException>(() => jsApp.WithNpmRunCommand(scriptName));
    }

    // -- PublishAsNetlifySite(string dir) convenience overload --

    [Fact]
    public void PublishAsNetlifySite_DirectoryOverload_AddsDeploymentResource_WhenInPublishMode()
    {
        // Arrange
        var builder = CreatePublishBuilder();
        var jsApp = builder.AddJavaScriptApp("test-app", "./test-app");

        // Act
        jsApp.PublishAsNetlifySite("dist");

        using var app = builder.Build();
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        // Assert
        var deployment = Assert.Single(appModel.Resources.OfType<NetlifyDeploymentResource>());
        Assert.Equal("test-app-netlify-deploy", deployment.Name);
        Assert.Equal("dist", deployment.BuildDirectory);
        Assert.True(deployment.Options.NoBuild,
            "Directory overload should set NoBuild = true (the JS app is built separately).");
    }

    [Fact]
    public void PublishAsNetlifySite_DirectoryOverload_DoesNotAddDeploymentResource_WhenNotInPublishMode()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder();
        var jsApp = builder.AddJavaScriptApp("test-app", "./test-app");

        // Act
        jsApp.PublishAsNetlifySite("dist");

        using var app = builder.Build();
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        // Assert
        Assert.Empty(appModel.Resources.OfType<NetlifyDeploymentResource>());
    }

    [Theory]
    [InlineData("dist")]
    [InlineData("build")]
    [InlineData(".next")]
    [InlineData("public")]
    public void PublishAsNetlifySite_DirectoryOverload_PassesThroughDirectory(string dir)
    {
        // Arrange
        var builder = CreatePublishBuilder();
        var jsApp = builder.AddJavaScriptApp("test-app", "./test-app");

        // Act
        jsApp.PublishAsNetlifySite(dir);

        using var app = builder.Build();
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        // Assert
        var deployment = Assert.Single(appModel.Resources.OfType<NetlifyDeploymentResource>());
        Assert.Equal(dir, deployment.BuildDirectory);
    }

    [Fact]
    public void PublishAsNetlifySite_DirectoryOverload_DefaultsToPreviewEnvironment()
    {
        // Arrange
        var builder = CreatePublishBuilder();
        var jsApp = builder.AddJavaScriptApp("test-app", "./test-app");

        // Act
        jsApp.PublishAsNetlifySite("dist");

        using var app = builder.Build();
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        // Assert
        var deployment = Assert.Single(appModel.Resources.OfType<NetlifyDeploymentResource>());
        Assert.Equal("preview", deployment.DeploymentEnvironment);
    }

    [Fact]
    public void PublishAsNetlifySite_DirectoryOverload_ThrowsArgumentNullException_ForNullBuilder()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ((IResourceBuilder<JavaScriptAppResource>)null!).PublishAsNetlifySite("dist"));
    }

    [Fact]
    public void PublishAsNetlifySite_DirectoryOverload_ThrowsArgumentNullException_ForNullDirectory()
    {
        // Arrange
        var builder = CreatePublishBuilder();
        var jsApp = builder.AddJavaScriptApp("test-app", "./test-app");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => jsApp.PublishAsNetlifySite((string)null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void PublishAsNetlifySite_DirectoryOverload_ThrowsArgumentException_ForEmptyOrWhitespaceDirectory(string dir)
    {
        // Arrange
        var builder = CreatePublishBuilder();
        var jsApp = builder.AddJavaScriptApp("test-app", "./test-app");

        // Act & Assert
        Assert.Throws<ArgumentException>(() => jsApp.PublishAsNetlifySite(dir));
    }
}
