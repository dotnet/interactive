// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.LanguageService;

namespace Microsoft.DotNet.Interactive.Sql
{

    public class ToolsServiceClient: IDisposable, IOb
    {

        private Process process;
        private Subject<Notification> notifications = new Subject<Notification>();

        public void startProcessAndRedirectIO()
        {

            process = CommandLine.StartProcess("/Users/chlafren/azuredatastudio/extensions/mssql/sqltoolsservice/OSX/2.0.0-release.70/MicrosoftSqlToolsServiceLayer", output: OnOutput, error: onError );

            // var startInfo = new ProcessStartInfo("/Users/chlafren/azuredatastudio/extensions/mssql/sqltoolsservice/OSX/2.0.0-release.70/MicrosoftSqlToolsServiceLayer")
            // {
            //     UseShellExecute = false,
            //     RedirectStandardInput = true,
            //     RedirectStandardOutput = true,
            //     RedirectStandardError = true
            // };
            // ToolsServiceProces = new Process
            // {
            //     StartInfo = startInfo
            // };
            // ToolsServiceProces.Start();
        }

        private void OnOutput(string json)
        {
            var notification = JsonConvert.DeserializeObject<Notification>(json);
            notifications.OnNext(notification);
        }

        private void OnError(string value)
        {
            
        }

        public async Task SendRequestAsync(Request request)
        {
            var text = request.ToJson();
            await process.StandardInput.WriteLineAsync(text);
        }

        public void Dispose()
        {

        }
    }

}