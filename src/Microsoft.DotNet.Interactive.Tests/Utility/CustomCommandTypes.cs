// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive.Tests.Utility
{
    public class CustomCommandTypes
    {
        // Multiple definitions of MyCommand simulate a user experimentally developing
        // the command type in a notebook over time.

        public class FirstSubmission
        {
            public class MyCommand : KernelCommand
            {
                public MyCommand(string info)
                {
                    Info = info;
                }
                public string Info { get; }
            }
        }

        public class SecondSubmission
        {
            public class MyCommand : KernelCommand
            {
                public MyCommand(string info, int additionalProperty)
                {
                    Info = info;
                    AdditionalProperty = additionalProperty;
                }
                public string Info { get; }
                public int AdditionalProperty { get; }
            }
        }
    }
}