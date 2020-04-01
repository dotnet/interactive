// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.DotNet.Interactive.App
{
    internal static class HttpApiBootstrapper
    {
        public static string GetHtmlInjection(Uri[] probingUris, string seed, bool enableLsp = false)
        {
            var apiCacheBuster = $"{Process.GetCurrentProcess().Id}.{seed}";
            var template = @"
<div>
    <div id='dotnet-interactive-this-cell-$SEED$' style='display: none'>
        The below script needs to be able to find the current output cell; this is an easy method to get it.
    </div>
    <script type='text/javascript'>
// ensure `requirejs` is available globally
if (typeof requirejs !== typeof Function || typeof requirejs.config !== typeof Function) {
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

            try {
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
            catch (e) {}
        }
    }
}

function loadDotnetInteractiveApi() {
    probeAddresses($ADDRESSES$)
        .then((root) => {
            // use probing to find host url and api resources
            // load interactive helpers and language services
            let dotnet_require = requirejs.config({
                context: '$CACHE_BUSTER$',
                paths: {
                    'dotnet-interactive': `${root}resources`
                }
            });
            if (!window.dotnet_require) {
                window.dotnet_require = dotnet_require;
            }
        
            dotnet_require([
                    'dotnet-interactive/dotnet-interactive',
                    'dotnet-interactive/lsp',
                    'dotnet-interactive/editor-detection'
                ],
                function (dotnet, lsp, editor) {
                    dotnet.init(window);
                    $LSP$
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

            var lspInit = @"
                    lsp.init(window);
                    editor.init(window, document, root, document.getElementById('dotnet-interactive-this-cell-$SEED$'));
";
            
            var jsProbingUris = $"[{ string.Join(", ", probingUris.Select(a => $"\"{a.AbsoluteUri}\"")) }]";
            var code = template;
            code = code.Replace("$LSP$", enableLsp ? lspInit : string.Empty);
            code = code.Replace("$ADDRESSES$", jsProbingUris);
            code = code.Replace("$CACHE_BUSTER$", apiCacheBuster);
            code = code.Replace("$SEED$", seed);
            

            return code;
        }
    }
}
