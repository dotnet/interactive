// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;

namespace Microsoft.DotNet.Interactive.App
{
    internal static class HttpApiBootstrapper
    {
        public static string GetJSCode(Uri apiRoot)
        {
            var id = $"{Process.GetCurrentProcess().Id}.{apiRoot.Port}";
            var template = @"#!javascript
// ensure `requirejs` is available
if ((typeof(requirejs) !==  typeof(Function)) || (typeof(requirejs.config) !== typeof(Function))) {
    let script = document.createElement('script');
    script.setAttribute('src', 'https://cdnjs.cloudflare.com/ajax/libs/require.js/2.3.6/require.min.js');
    script.onload = function() {
        loadDotnetInteractiveApi();
    };

    document.getElementsByTagName('head')[0].appendChild(script);
}
else {
    loadDotnetInteractiveApi();
}

function loadDotnetInteractiveApi() {
    // ensure interactive helpers are loaded
    let interactiveHelperElement = document.getElementById('dotnet-interactive-script-loaded');
    if (!interactiveHelperElement) {
        let apiRequire = requirejs.config({
                context: 'dotnet-interactive.$SEED$',
                paths: {
                    dotnetInteractive: '$API_URL$'
                }
            });
            apiRequire(['dotnetInteractive'],
                function(api) {
                    api.init(window);
                    let sentinelElement = document.createElement('div');
                    sentinelElement.setAttribute('id', 'dotnet-interactive-script-loaded');
                    sentinelElement.setAttribute('style', 'display: none');
                    document.getElementsByTagName('head')[0].appendChild(sentinelElement);
                },
                function(error) {
                    console.log(error);
                }
            );
    }
}";
            var jsUri = new Uri(apiRoot, "/resources/dotnet-interactive");
            var code = template;
            code = code.Replace("$API_URL$", jsUri.ToString());
            code = code.Replace("$SEED$", id);

            return code;
        }
    }
}
