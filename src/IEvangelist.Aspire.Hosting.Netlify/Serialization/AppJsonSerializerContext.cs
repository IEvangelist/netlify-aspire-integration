// Copyright (c) David Pine. All rights reserved.
// Licensed under the MIT License.

namespace Aspire.Hosting;

[JsonSourceGenerationOptions(
    defaults: JsonSerializerDefaults.Web,
    WriteIndented = true)]
[JsonSerializable(typeof(NetlifyDeployState))]
internal sealed partial class NetlifyWebJsonContext : JsonSerializerContext;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    WriteIndented = true
)]
[JsonSerializable(typeof(NetlifySite))]
public partial class NetlifySnakeCaseContext : JsonSerializerContext;