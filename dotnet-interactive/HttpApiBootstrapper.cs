// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.DotNet.Interactive.App
{
    internal static class HttpApiBootstrapper
    {
        public static string GetJSCode(Uri apiRoot)
        {
            var template = @"#!javascript
if ((typeof(requirejs) !==  typeof(Function)) || (typeof(requirejs.config) !== typeof(Function))) { 
    let script = document.createElement(""script""); 
    script.setAttribute(""src"", ""$API_URL$""); 
    script.onload = function(){
        window.dotnetInteractive = createDotnetInteractiveClient(""$HOST$"");
    };
    document.getElementsByTagName(""head"")[0].appendChild(script); 
}
else {
    let apiRequire = requirejs.config({context:""dotnet-interactive"",paths:{dotnetInteractive:""$API_URL$""}});
    apiRequire(['dotnetInteractive'], function(api) {
        window.dotnetInteractive = createDotnetInteractiveClient(""$HOST$"");
    });
}";
            var jsUri = new Uri(apiRoot, "/resources/dotnet-interactive.js");
            var code = template.Replace("$HOST$", apiRoot.ToString());
            code = code.Replace("$API_URL$", jsUri.ToString());

            return code;
        }
    }
}