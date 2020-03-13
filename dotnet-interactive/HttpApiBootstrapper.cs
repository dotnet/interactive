// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.DotNet.Interactive.App
{
    internal static class HttpApiBootstrapper
    {
        public static string GetJSCode(Uri[] probingUris, string seed)
        {
            var apiCacheBuster = $"{Process.GetCurrentProcess().Id}.{seed}";
            var template = @"// ensure `requirejs` is available
if ((typeof (requirejs) !== typeof (Function)) || (typeof (requirejs.config) !== typeof (Function))) 
{
    let requirejs_script = document.createElement('script');
    requirejs_script.setAttribute('src', 'https://cdnjs.cloudflare.com/ajax/libs/require.js/2.3.6/require.min.js');
    requirejs_script.setAttribute('type', 'text/javascript');
    requirejs_script.onload = function () {
        loadDotnetInteractiveApi();
    };

    document.getElementsByTagName('head')[0].appendChild(requirejs_script);
}
else {
    loadDotnetInteractiveApi();
}

async function probeAddresses(probingAddresses) {
    if (Array.isArray(probingAddresses)) {
        for (let i = 0; i < probingAddresses.length; i++) {
            
            let rootUrl = probingAddresses[i];

            if (!rootUrl.endsWith('/')) {
                rootUrl = `${rootUrl}/`;
            }

            let response = await fetch(`${rootUrl}discovery`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'text/plain'
                },
                body: probingAddresses[i]
            });

            if (response.status == 200) {
                return rootUrl;
            }
        }
    }
}

function loadDotnetInteractiveApi() {
    probeAddresses($ADDRESSES$)
        .then((root) => {
            // use probing to find host url and api resources
            // ensure interactive helpers are loaded
            let interactiveHelperElement = document.getElementById('dotnet-interactive-script-loaded');
            if (!interactiveHelperElement) {
                let apiRequire = requirejs.config({
                    context: 'dotnet-interactive.$CACHE_BUSTER$',
                    paths: {
                        dotnetInteractive: `${root}resources/dotnet-interactive`
                    }
                });
                apiRequire(['dotnetInteractive'],
                    function (api) {
                        api.init(window);
                        let sentinelElement = document.createElement('script');
                        sentinelElement.setAttribute('id', 'dotnet-interactive-script-loaded');
                        sentinelElement.setAttribute('type', 'text/javascript');
                        document.getElementsByTagName('head')[0].appendChild(sentinelElement);
                    },
                    function (error) {
                        console.log(error);
                    }
                );
            }
        })
        .catch(error => {console.log(error);});
}";
           
            var jsProbingUris = $"[{ string.Join(", ",probingUris.Select(a => $"\"{a.AbsoluteUri}\"")) }]";
            var code = template;
            code = code.Replace("$CACHE_BUSTER$", apiCacheBuster);
            code = code.Replace("$ADDRESSES$", jsProbingUris);

            return code;
        }
    }
}
