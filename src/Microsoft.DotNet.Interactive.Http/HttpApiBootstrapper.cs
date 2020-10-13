﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Html;

namespace Microsoft.DotNet.Interactive.Http
{
    internal static class HttpApiBootstrapper
    {
        public static IHtmlContent GetHtmlInjection(Uri[] probingUris, string seed)
        {
            var apiCacheBuster = $"{Process.GetCurrentProcess().Id}.{seed}";
            var template = 
                $@"
<div>
    <div id='dotnet-interactive-this-cell-$CACHE_BUSTER$' style='display: none'>
        The below script needs to be able to find the current output cell; this is an easy method to get it.
    </div>
    <script type='text/javascript'>
async function probeAddresses(probingAddresses) {{
    function timeout(ms, promise) {{
        return new Promise(function (resolve, reject) {{
            setTimeout(function () {{
                reject(new Error('timeout'))
            }}, ms)
            promise.then(resolve, reject)
        }})
    }}

    if (Array.isArray(probingAddresses)) {{
        for (let i = 0; i < probingAddresses.length; i++) {{

            let rootUrl = probingAddresses[i];

            if (!rootUrl.endsWith('/')) {{
                rootUrl = `${{rootUrl}}/`;
            }}

            try {{
                let response = await timeout(1000, fetch(`${{rootUrl}}discovery`, {{
                    method: 'POST',
                    cache: 'no-cache',
                    mode: 'cors',
                    timeout: 1000,
                    headers: {{
                        'Content-Type': 'text/plain'
                    }},
                    body: probingAddresses[i]
                }}));

                if (response.status == 200) {{
                    return rootUrl;
                }}
            }}
            catch (e) {{ }}
        }}
    }}
}}

function loadDotnetInteractiveApi() {{
    probeAddresses($ADDRESSES$)
        .then((root) => {{
        // use probing to find host url and api resources
        // load interactive helpers and language services
        let dotnetInteractiveRequire = require.config({{
        context: '$CACHE_BUSTER$',
                paths:
            {{
                'dotnet-interactive': `${{root}}
                resources`
                }}
        }}) || require;

            window.dotnetInteractiveRequire = dotnetInteractiveRequire;

            window.configureRequireFromExtension = function(extensionName, extensionCacheBuster) {{
                let paths = {{}};
                paths[extensionName] = `${{root}}extensions/${{extensionName}}/resources/`;
                
                let internalRequire = require.config({{
                    context: extensionCacheBuster,
                    paths: paths,
                    urlArgs: `cacheBuster=${{extensionCacheBuster}}`
                    }}) || require;

                return internalRequire
            }};
        
            dotnetInteractiveRequire([
                    'dotnet-interactive/dotnet-interactive'
                ],
                function (dotnet) {{
                    dotnet.init(window);
                }},
                function (error) {{
                    console.log(error);
                }}
            );
        }})
        .catch(error => {{console.log(error);}});
    }}

{JavascriptUtilities.GetCodeForEnsureRequireJs(onRequirejsLoadedCallBackName: "loadDotnetInteractiveApi")}
    </script>
</div>";
            
            var jsProbingUris = $"[{ string.Join(", ", probingUris.Select(a => $"\"{a.AbsoluteUri}\"")) }]";
            var html =  template
                .Replace("$ADDRESSES$", jsProbingUris)
                .Replace("$CACHE_BUSTER$", apiCacheBuster);
            return new HtmlString(html);
        }
    }
}
