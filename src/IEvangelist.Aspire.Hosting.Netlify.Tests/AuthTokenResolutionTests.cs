// Copyright (c) David Pine. All rights reserved.
// Licensed under the MIT License.

namespace Aspire.Hosting.Netlify.Tests;

public class AuthTokenResolutionTests
{
    [Fact]
    public void ResolveAuthToken_PrefersParameterToken_OverEnvVar()
    {
        var resolved = NetlifyDeploymentPipelineSteps.ResolveAuthToken(
            parameterToken: "param-token",
            envToken: "env-token");

        Assert.Equal("param-token", resolved);
    }

    [Fact]
    public void ResolveAuthToken_FallsBackToEnvVar_WhenParameterIsNull()
    {
        var resolved = NetlifyDeploymentPipelineSteps.ResolveAuthToken(
            parameterToken: null,
            envToken: "env-token");

        Assert.Equal("env-token", resolved);
    }

    [Fact]
    public void ResolveAuthToken_FallsBackToEnvVar_WhenParameterIsWhitespace()
    {
        var resolved = NetlifyDeploymentPipelineSteps.ResolveAuthToken(
            parameterToken: "   ",
            envToken: "env-token");

        Assert.Equal("env-token", resolved);
    }

    [Fact]
    public void ResolveAuthToken_ReturnsNull_WhenBothUnset()
    {
        var resolved = NetlifyDeploymentPipelineSteps.ResolveAuthToken(
            parameterToken: null,
            envToken: null);

        Assert.Null(resolved);
    }

    [Fact]
    public void ResolveAuthToken_ReturnsNull_WhenBothWhitespace()
    {
        var resolved = NetlifyDeploymentPipelineSteps.ResolveAuthToken(
            parameterToken: "",
            envToken: "  ");

        Assert.Null(resolved);
    }

    [Fact]
    public void ResolveAuthToken_ReturnsParameter_WhenEnvVarUnset()
    {
        var resolved = NetlifyDeploymentPipelineSteps.ResolveAuthToken(
            parameterToken: "param-token",
            envToken: null);

        Assert.Equal("param-token", resolved);
    }
}
