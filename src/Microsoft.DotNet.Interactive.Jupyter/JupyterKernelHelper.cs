// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Interactive.Jupyter
{
    public static class TopLevelMethods
    {
        public static string input(string prompt = "")
        {
            return JupyterInteractiveHost.GetInput(prompt);
        }

        public static PasswordString password(string prompt = "")
        {
            return JupyterInteractiveHost.GetPassword(prompt);
        }
    }
}
