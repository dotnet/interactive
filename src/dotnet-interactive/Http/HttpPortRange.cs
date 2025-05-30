// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Interactive.Http;

public class HttpPortRange
{
    public HttpPortRange(int start, int end)
    {
        Start = start;
        End = end;
    }

    public int Start { get;  }
        
    public int End { get;  }

    public static HttpPortRange Default { get; } = new(2048, 3000);

    public override string ToString() => $"{Start}-{End}";
}