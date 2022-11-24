// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Newtonsoft.Json;

namespace Microsoft.DotNet.Interactive.CSharpProject;

public class Buffer
{
    private readonly int offSetFromParentBuffer;

    public Buffer(BufferId id, string content, int position = 0, int offSetFromParentBuffer = 0, int order = 0)
    {
        this.offSetFromParentBuffer = offSetFromParentBuffer;
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Content = content;
        Position = position;
        Order = order;
    }

    public BufferId Id { get; }

    public string Content { get; }

    public int Position { get; internal set; }

    public int Order { get; }

    [JsonIgnore]
    public int AbsolutePosition => Position + offSetFromParentBuffer;

    public override string ToString() => $"{nameof(Buffer)}: {Id}";
}