// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.DotNet.Interactive.App
{
    internal static class HttpApiBootstrapper
    {
        public static string GetHtmlInjection(Uri[] probingUris, string seed)
        {
            var apiCacheBuster = $"{Process.GetCurrentProcess().Id}.{seed}";
            var template = @"
<div>
    <div id='dotnet-interactive-this-cell-$CACHE_BUSTER$' style='display: none'>
        The below script needs to be able to find the current output cell; this is an easy method to get it.
    </div>
    <script type='text/javascript'>
// ensure `require` is available globally
if (typeof require !== typeof Function || typeof require.config !== typeof Function) {
    let require_script = document.createElement('script');
    require_script.setAttribute('src', 'https://cdnjs.cloudflare.com/ajax/libs/require.js/2.3.6/require.min.js');
    require_script.setAttribute('type', 'text/javascript');
    require_script.onload = function () {
        loadDotnetInteractiveApi();
    };

    document.getElementsByTagName('head')[0].appendChild(require_script);
}
else {
    loadDotnetInteractiveApi();
}

async function probeAddresses(probingAddresses) {
    function timeout(ms, promise) {
        return new Promise(function (resolve, reject) {
            setTimeout(function () {
                reject(new Error('timeout'))
            }, ms)
            promise.then(resolve, reject)
        })
    }

    if (Array.isArray(probingAddresses)) {
        for (let i = 0; i < probingAddresses.length; i++) {

            let rootUrl = probingAddresses[i];

            if (!rootUrl.endsWith('/')) {
                rootUrl = `${rootUrl}/`;
            }

            try {
                let response = await timeout(1000, fetch(`${rootUrl}discovery`, {
                    method: 'POST',
                    cache: 'no-cache',
                    mode: 'cors',
                    timeout: 1000,
                    headers: {
                        'Content-Type': 'text/plain'
                    },
                    body: probingAddresses[i]
                }));

                if (response.status == 200) {
                    return rootUrl;
                }
            }
            catch (e) { }
        }
    }
}

function loadDotnetInteractiveApi() {
    probeAddresses($ADDRESSES$)
        .then((root) => {
            // use probing to find host url and api resources
            // load interactive helpers and language services
            let dotnetInteractiveRequire = require.config({
                context: '$CACHE_BUSTER$',
                paths: {
                    'dotnet-interactive': `${root}resources`
                }
            }) || require;
            if (!window.dotnetInteractiveRequire) {
                window.dotnetInteractiveRequire = dotnetInteractiveRequire;
            }
        
            dotnetInteractiveRequire([
                    'dotnet-interactive/dotnet-interactive'
                ],
                function (dotnet) {
                    dotnet.init(window);
                },
                function (error) {
                    console.log(error);
                }
            );
        })
        .catch(error => {console.log(error);});
    }
    </script>
</div>";
            
            var jsProbingUris = $"[{ string.Join(", ", probingUris.Select(a => $"\"{a.AbsoluteUri}\"")) }]";
            return template
                .Replace("$ADDRESSES$", jsProbingUris)
                .Replace("$CACHE_BUSTER$", apiCacheBuster);
        }
    }
}
