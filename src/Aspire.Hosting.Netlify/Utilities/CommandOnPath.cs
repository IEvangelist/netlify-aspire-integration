// Copyright (c) David Pine. All rights reserved.
// Licensed under the MIT License.

namespace Aspire.Hosting;

internal sealed record class CommandOnPath(bool IsFound, string? Path)
{
    public static implicit operator CommandOnPath(string path) => new(true, path);
    public static implicit operator CommandOnPath(bool isFound) => new(isFound, null);
}