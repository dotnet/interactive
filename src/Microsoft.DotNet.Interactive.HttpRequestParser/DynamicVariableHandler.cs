// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Interactive.HttpRequest;

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

// TODO: The replacement strategy for variables should not be a concern of the parser.
// Replacements should ideally happen at the very end at runtime / execution phase.
// The parser should just concern itself with syntax.
internal static class DynamicVariableHandler
{
    private static readonly Random s_random = new Random();
    private static readonly Regex s_projectUrlExpression = new(@"{{\$projectUrl}}", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex s_guidExpression = new(@"{{\$guid}}", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex s_randomIntExpression = new(@"{{\$randomInt (\d+) (\d+)}}", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly List<DynamicVariable> _variables = new()
    {
        new DynamicVariable(s_projectUrlExpression, GetProjectUrlHandler),
        new DynamicVariable(s_guidExpression, GetGuidHandler),
        new DynamicVariable(s_randomIntExpression, GetRandomIntHandler)
    };

    public static string ExpandVariables(HttpDocumentSnapshot httpDocumentSnapshot, string text)
    {
        foreach (DynamicVariable variable in _variables)
        {
            MatchCollection matches = variable.Expression.Matches(text);
            if (matches?.Count > 0)
            {
                text = variable.InvokeHandler(httpDocumentSnapshot, text, matches);
            }
        }
        return text;
    }

    private static string GetProjectUrlHandler(HttpDocumentSnapshot httpDocumentSnapshot, string text, MatchCollection matches)
    {
        // TODO: Support alternate mechanism whereby consumers of the parser API can supply handler / resolver for 'projectUrl'.
        //if (httpDocumentSnapshot.Document.ProjectName is not null)
        //{
        //    Debug.Assert(WebEditor.IsUIThread);

        //    if (ProjectUtilities.TryGetHierarchy(httpDocumentSnapshot.Document.FilePath!, out IVsHierarchy? hier, out _, out int isInProject)
        //        && isInProject == (int)__VSDOCINPROJECT.DOCINPROJ_DocInProject)
        //    {
        //        Project? proj;
        //        if (ErrorHandler.Succeeded(
        //                hier.GetProperty(
        //                    VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_ExtObject, out object extObject)))
        //        {
        //            proj = extObject as Project;
        //        }
        //        else
        //        {
        //            proj = null;
        //        }

        //        if (proj is not null)
        //        {
        //            string? projectUrl = (string)proj.Properties.Item("ProjectUrl").Value;
        //            if (!string.IsNullOrEmpty(projectUrl))
        //            {
        //                projectUrl = projectUrl.TrimEnd('/');

        //                for (int i = matches.Count - 1; i >= 0; i--)
        //                {
        //                    Match match = matches[i];
        //                    text = string.Concat(text.Substring(0, match.Index), projectUrl, text.Substring(match.Index + match.Value.Length));
        //                }
        //            }
        //        }
        //    }
        //}

        return text;
    }

    private static string GetGuidHandler(HttpDocumentSnapshot _, string text, MatchCollection matches)
    {
        for (int i = matches.Count - 1; i >= 0; i--)
        {
            Match match = matches[i];
            text = string.Concat(text.Substring(0, match.Index), Guid.NewGuid().ToString(), text.Substring(match.Index + match.Value.Length));
        }

        return text;
    }

    private static string GetRandomIntHandler(HttpDocumentSnapshot _, string text, MatchCollection matches)
    {
        for (int i = matches.Count - 1; i >= 0; i--)
        {
            Match match = matches[i];
            if (match.Groups.Count == 3
                && int.TryParse(match.Groups[1].Value, out int min)
                && int.TryParse(match.Groups[2].Value, out int max))
            {
                text = string.Concat(text.Substring(0, match.Index), s_random.Next(min, max).ToString(), text.Substring(match.Index + match.Value.Length));
            }
        }

        return text;
    }
}
