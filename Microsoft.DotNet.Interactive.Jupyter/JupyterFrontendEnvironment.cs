

using System;

namespace Microsoft.DotNet.Interactive.Jupyter
{
    public class JupyterFrontendEnvironment : FrontendEnvironmentBase
    {
        public bool AllowStandardInput { get; set; }
        public Uri Host { get; set; }
    }
}