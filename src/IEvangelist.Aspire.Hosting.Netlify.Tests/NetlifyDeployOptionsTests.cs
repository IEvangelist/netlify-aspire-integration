// Copyright (c) David Pine. All rights reserved.
// Licensed under the MIT License.

namespace Aspire.Hosting.Netlify.Tests;

public class NetlifyDeployOptionsTests
{
    [Fact]
    public void ToArguments_DoesNotMutate_Message_OnRepeatedCalls()
    {
        // Arrange
        var options = new NetlifyDeployOptions();

        // Act
        var (args1, _) = options.ToArguments();
        Thread.Sleep(1100); // Force a clock tick
        var (args2, _) = options.ToArguments();

        // Assert
        Assert.Null(options.Message);
        Assert.NotEqual(ExtractMessage(args1), ExtractMessage(args2));
    }

    [Fact]
    public void ToArguments_PreservesUserSuppliedMessage()
    {
        // Arrange
        var options = new NetlifyDeployOptions { Message = "Custom deploy message" };

        // Act
        var (args, _) = options.ToArguments();

        // Assert
        Assert.Equal("Custom deploy message", ExtractMessage(args));
        Assert.Equal("Custom deploy message", options.Message);
    }

    [Theory]
    [InlineData("my-site")]
    [InlineData("ievangelist-test")]
    [InlineData("astro-demo-1234")]
    public void ToArguments_EmitsCreateSite_WithValue(string createSiteValue)
    {
        // Arrange
        var options = new NetlifyDeployOptions { CreateSite = createSiteValue };

        // Act
        var (args, _) = options.ToArguments();

        // Assert
        var idx = Array.IndexOf(args, "--create-site");
        Assert.True(idx >= 0, "Expected --create-site flag in args.");
        Assert.True(idx + 1 < args.Length, "Expected a value argument after --create-site.");
        Assert.Equal(createSiteValue, args[idx + 1]);
    }

    [Fact]
    public void ToArguments_DoesNotEmitCreateSite_WhenUnset()
    {
        // Arrange
        var options = new NetlifyDeployOptions();

        // Act
        var (args, _) = options.ToArguments();

        // Assert
        Assert.DoesNotContain("--create-site", args);
    }

    [Fact]
    public void ToArguments_RedactsSiteValue()
    {
        // Arrange
        var options = new NetlifyDeployOptions { Site = "my-secret-site-id-1234567890" };

        // Act
        var (args, redacted) = options.ToArguments();

        // Assert
        var rawIdx = Array.IndexOf(args, "--site");
        Assert.Equal("my-secret-site-id-1234567890", args[rawIdx + 1]);

        var redactedIdx = Array.IndexOf(redacted, "--site");
        Assert.NotEqual("my-secret-site-id-1234567890", redacted[redactedIdx + 1]);
    }

    [Fact]
    public void ToArguments_EmitsDeployAsFirstArg()
    {
        // Arrange
        var options = new NetlifyDeployOptions { Dir = "build" };

        // Act
        var (args, _) = options.ToArguments();

        // Assert
        Assert.Equal("deploy", args[0]);
    }

    [Fact]
    public void ToArguments_EmitsDirArg()
    {
        // Arrange
        var options = new NetlifyDeployOptions { Dir = "build" };

        // Act
        var (args, _) = options.ToArguments();

        // Assert
        var idx = Array.IndexOf(args, "--dir");
        Assert.True(idx >= 0);
        Assert.Equal("build", args[idx + 1]);
    }

    [Fact]
    public void ToArguments_DefaultsDirTo_DistFolder_WhenUnset()
    {
        // Arrange
        var options = new NetlifyDeployOptions();

        // Act
        var (args, _) = options.ToArguments();

        // Assert
        var idx = Array.IndexOf(args, "--dir");
        Assert.True(idx >= 0);
        Assert.Equal("./dist", args[idx + 1]);
    }

    [Fact]
    public void ToArguments_EmitsNoBuild_WhenSet()
    {
        // Arrange
        var options = new NetlifyDeployOptions { NoBuild = true };

        // Act
        var (args, _) = options.ToArguments();

        // Assert
        Assert.Contains("--no-build", args);
    }

    [Fact]
    public void ToArguments_EmitsProd_WhenSet()
    {
        // Arrange
        var options = new NetlifyDeployOptions { Prod = true };

        // Act
        var (args, _) = options.ToArguments();

        // Assert
        Assert.Contains("--prod", args);
    }

    [Fact]
    public void ToEnvironmentDescription_ReturnsProduction_WhenProd()
    {
        Assert.Equal("production", new NetlifyDeployOptions { Prod = true }.ToEnvironmentDescription());
    }

    [Fact]
    public void ToEnvironmentDescription_ReturnsProductionIfUnlocked_WhenSet()
    {
        Assert.Equal("production-if-unlocked",
            new NetlifyDeployOptions { ProdIfUnlocked = true }.ToEnvironmentDescription());
    }

    [Fact]
    public void ToEnvironmentDescription_ReturnsAlias_WhenAliasSet()
    {
        Assert.Equal("alias: foo",
            new NetlifyDeployOptions { Alias = "foo" }.ToEnvironmentDescription());
    }

    [Fact]
    public void ToEnvironmentDescription_DefaultsToPreview()
    {
        Assert.Equal("preview", new NetlifyDeployOptions().ToEnvironmentDescription());
    }

    private static string ExtractMessage(string[] args)
    {
        var idx = Array.IndexOf(args, "--message");
        Assert.True(idx >= 0, "Expected --message argument.");
        return args[idx + 1];
    }
}
