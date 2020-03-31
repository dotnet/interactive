// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Interactive.App.CommandLine
{
    public class PortRange
    {
        public PortRange(int start, int end)
        {
            Start = start;
            End = end;
        }

        public int Start { get;  }
        public int End { get;  }
    }
}