// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.DotNet.Interactive.Http
{
    public static class JavascriptUtilities
    {
        public static string EnsureRequireJs(Uri requireJsUri = null, string callBackName = null)
        {
            requireJsUri??= new Uri("https://cdnjs.cloudflare.com/ajax/libs/require.js/2.3.6/require.min.js");

            string GenerateOnload()
            {
                return !string.IsNullOrWhiteSpace(callBackName) 
                    ? $@"
    require_script.onload = function() {{
        {callBackName}();
    }};" 
                    : string.Empty;
            }

            string GenerateElseBranch()
            {
                return !string.IsNullOrWhiteSpace(callBackName) 
                    ? $@"else {{
    {callBackName}();
}}" 
                    : string.Empty;
            }
            return $@"// ensure `require` is available globally
if ((typeof(require) !==  typeof(Function)) || (typeof(require.config) !== typeof(Function))) {{
    let require_script = document.createElement('script');
    require_script.setAttribute('src', '{requireJsUri.AbsoluteUri}');
    require_script.setAttribute('type', 'text/javascript');
    
    {GenerateOnload()}

    document.getElementsByTagName('head')[0].appendChild(require_script);
}}
{GenerateElseBranch()}
";
        }
    }
}