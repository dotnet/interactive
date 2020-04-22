// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Interactive.App.CommandLine
{
    public class HttPort
    {
        public static HttPort Auto { get; } = new HttPort();

        public HttPort(int portNumber)
        {
            PortNumber = portNumber;
        }

        private HttPort()
        {

        }

        public int? PortNumber { get; }

        public bool IsAuto => PortNumber == null;
    }
}