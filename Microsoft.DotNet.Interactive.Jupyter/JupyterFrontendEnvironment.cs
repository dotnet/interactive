// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System;

namespace Microsoft.DotNet.Interactive.Jupyter
{
    public class JupyterFrontendEnvironment : FrontendEnvironmentBase
    {
        public bool AllowStandardInput { get; set; }
        public Uri Host { get; set; }
    }
}