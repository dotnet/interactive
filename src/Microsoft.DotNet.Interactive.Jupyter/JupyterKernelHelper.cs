// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.DotNet.Interactive.Jupyter.Protocol;

namespace Microsoft.DotNet.Interactive.Jupyter
{
    public static class TopLevelMethods
    {
        public static string input(string prompt = "")
        {
            var context = JupyterRequestContext.Current;

            if (context.JupyterRequestMessageEnvelope.Content is ExecuteRequest { AllowStdin: false })
            {
                throw new NotSupportedException("Input prompt is not supported.");
            }

            var inputReq = new InputRequest(prompt, password: false);

            var result = context.JupyterMessageSender.Send(inputReq);
            return result;
        }

        public static PasswordString password(string prompt = "")
        {
            var context = JupyterRequestContext.Current;

            if (context.JupyterRequestMessageEnvelope.Content is ExecuteRequest { AllowStdin: false })
            {
                throw new NotSupportedException("Password prompt is not supported.");
            }

            var inputReq = new InputRequest(prompt, password: true);
            var password1 = context.JupyterMessageSender.Send(inputReq);
            var result = new PasswordString(password1);
            return result;
        }
    }
}