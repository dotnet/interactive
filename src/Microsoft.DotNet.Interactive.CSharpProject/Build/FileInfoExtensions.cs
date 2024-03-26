// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Xml.Linq;
using System.Xml.XPath;

namespace Microsoft.DotNet.Interactive.CSharpProject.Build;

internal static class FileInfoExtensions
{
    public static string GetTargetFramework(this FileInfo project)
    {
        if (project == null)
        {
            throw new ArgumentNullException(nameof(project));
        }

        if (!project.Exists)
        {
            throw new FileNotFoundException("Project file not found", project.FullName);
        }

        var dom = XElement.Parse(File.ReadAllText(project.FullName));
        var targetFramework = dom.XPathSelectElement("//TargetFramework");
        return targetFramework?.Value ?? string.Empty;
    }

    public static void SetLanguageVersion(this FileInfo project, string version)
    {
        if (project is null)
        {
            throw new ArgumentNullException(nameof(project));
        }

        if (!project.Exists)
        {
            throw new FileNotFoundException();
        }

        var dom = XElement.Parse(File.ReadAllText(project.FullName));
        var langElement = dom.XPathSelectElement("//LangVersion");

        if (langElement != null)
        {
            langElement.Value = version;
        }
        else
        {
            var propertyGroup = dom.XPathSelectElement("//PropertyGroup");
            propertyGroup?.Add(new XElement("LangVersion", version));
        }

        File.WriteAllText(project.FullName, dom.ToString());
    }
}