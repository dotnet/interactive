// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.AspNetCore.Html;
using Microsoft.DotNet.Interactive.Formatting;
using static Microsoft.DotNet.Interactive.Formatting.PocketViewTags;

namespace Microsoft.DotNet.Interactive
{
    // Data bag containing the data to be displayed when reporting nuget resolve progress 

    [TypeFormatterSource(typeof(InstallPackagesMessageFormatterSource))]
    public class InstallPackagesMessage
    {
        public IReadOnlyList<string> RestoreSources { get; set; }
        public IReadOnlyList<string> InstallingPackages { get; set; }
        public IReadOnlyList<string> InstalledPackages { get; set; }
        public int Progress { get; set; }

        public InstallPackagesMessage(
                IReadOnlyList<string> restoreSources,
                IReadOnlyList<string> installingPackages,
                IReadOnlyList<string> installedPackages,
                int progress)
        {
            RestoreSources = restoreSources;
            InstallingPackages = installingPackages;
            InstalledPackages = installedPackages;
            Progress = progress;
        }

        private IHtmlContent InstallMessage(string message, IReadOnlyList<string> items, string progress = null)
        {
            if (items.Any())
            {
                return div(
                    strong(message),
                    ul(items.Select(s => li(span(s + progress)))));
            }
            else
            {
                return div();
            }
        }

        public string FormatAsHtml()
        {
            var items = new List<IHtmlContent>
            {
                InstallMessage("Restore sources", RestoreSources),
                InstallMessage("Installing Packages", InstallingPackages, new string('.', Progress)),
                InstallMessage("Installed Packages", InstalledPackages)
            };
            return div(items).ToString();
        }

        public IEnumerable<string> FormatAsPlainTextLines()
        {
            if (RestoreSources.Count > 0)
            {
                yield return "Restore sources";
                foreach (var source in RestoreSources)
                {
                    yield return $" - {source}";
                }
            }

            if (InstallingPackages.Count > 0)
            {
                yield return "Installing Packages";
                foreach (var installing in InstallingPackages)
                {
                    yield return $" - {installing}   " + new string('.', Progress);
                }
            }

            if (InstalledPackages.Count > 0)
            {
                yield return "Installed Packages";
                foreach (var installed in InstalledPackages)
                {
                    yield return $" - {installed}";
                }
            }
        }

        public string FormatAsPlainText()
        {
            return string.Join(Environment.NewLine, FormatAsPlainTextLines());
        }

        private class InstallPackagesMessageFormatterSource : ITypeFormatterSource
        {
            public IEnumerable<ITypeFormatter> CreateTypeFormatters()
            {
                return new ITypeFormatter[]
                {
                    new PlainTextFormatter<InstallPackagesMessage>((m, ctxt) => ctxt.Writer.Write(m.FormatAsPlainText())),
                    new HtmlFormatter<InstallPackagesMessage>((m, ctxt) => ctxt.Writer.Write(m.FormatAsHtml()))
                };
            }
        }
    }
}
