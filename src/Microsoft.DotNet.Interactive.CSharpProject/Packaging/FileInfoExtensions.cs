// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Xml.Linq;
using System.Xml.XPath;

namespace Microsoft.DotNet.Interactive.CSharpProject.Packaging
{
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
                throw new FileNotFoundException();
            }

            var dom = XElement.Parse(File.ReadAllText(project.FullName));
            var targetFramework = dom.XPathSelectElement("//TargetFramework");
            return targetFramework?.Value ?? string.Empty;
        }

        public static string GetLanguageVersion(this FileInfo project)
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            if (!project.Exists)
            {
                throw new FileNotFoundException();
            }

            var dom = XElement.Parse(File.ReadAllText(project.FullName));
            var languageVersion = dom.XPathSelectElement("//LangVersion");
            string version;
            if (languageVersion == null || string.IsNullOrWhiteSpace(languageVersion?.Value))
            {
                version = project.SuggestedLanguageVersion();
            }
            else
            {
                version = languageVersion.Value;
            }
            return version;
        }

        public static string SuggestedLanguageVersion(this FileInfo project)
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            if (!project.Exists)
            {
                throw new FileNotFoundException();
            }

            return CSharpLanguageSelector.GetCSharpLanguageVersion(project.GetTargetFramework());
        }

        public static void SetLanguageVersion(this FileInfo project, string version)
        {
            if (project == null)
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

        public static void TrySetLanguageVersion(this FileInfo project, string version)
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            if (!project.Exists)
            {
                throw new FileNotFoundException();
            }

            var supported = CSharpLanguageSelector.GetCSharpLanguageVersion(project.GetTargetFramework());

            var canSet = StringComparer.OrdinalIgnoreCase.Equals(supported, version);
            if (canSet)
            {
                project.SetLanguageVersion(version);
            }
        }
    }
}