// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Text.Encodings.Web;
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
        public IReadOnlyList<string> InstalledPackages { get; set; }
        public IReadOnlyList<string> InstallingPackages { get; set; }
        public int Progress { get; set; }

        public InstallPackagesMessage(
                IReadOnlyList<string> restoreSources,
                IReadOnlyList<string> installedPackages,
                IReadOnlyList<string> installingPackages,
                int progress)
        {
            RestoreSources = restoreSources;
            InstalledPackages = installedPackages;
            InstallingPackages = installingPackages;
            Progress = progress;
        }

        IHtmlContent InstallMessage(string message, IEnumerable<string> items, string progress = null)
        {
            if (items.Count() > 0)
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

        private string FormatAsHtml()
        {
            string progress = new String('.', Progress);
            var items = new List<IHtmlContent>();
            items.Add(InstallMessage("Restore sources", RestoreSources));
            items.Add(InstallMessage("Installed Packages", InstalledPackages));
            items.Add(InstallMessage("Installing Packages", InstallingPackages, progress));
            return div(items).ToDisplayString();
        }

        private string FormatAsPlainText()
        {
            var result = new StringBuilder();
            if (RestoreSources.Count > 0)
            {
                result.Append("Restore sources");
                foreach (var source in RestoreSources)
                {
                    result.Append(System.Environment.NewLine);
                    result.Append(" - " + source);
                }
                result.Append(System.Environment.NewLine);
            }

            if (InstalledPackages.Count > 0)
            {
                result.Append("Installed Packages");
                foreach (var installed in InstalledPackages)
                {
                    result.Append(System.Environment.NewLine);
                    result.Append(" - " + installed);
                }
                result.Append(System.Environment.NewLine);
            }

            if (InstallingPackages.Count > 0)
            {
                result.Append("Installing Packages");
                foreach (var installing in InstallingPackages)
                {
                    result.Append(System.Environment.NewLine);
                    result.Append(" - " + installing + "   ");
                    result.Append('.', Progress);
                }
                result.Append(System.Environment.NewLine);
            }
            return result.ToString();
        }
    }

    class InstallPackagesMessageFormatterSource : ITypeFormatterSource
    {
        public IEnumerable<ITypeFormatter> CreateTypeFormatters()
        {
            return new ITypeFormatter[]
            {
                new PlainTextFormatter<InstallPackagesMessage>(m => m.FormatAsPlainText()),
                new HtmlFormatter<InstallPackagesMessage>(m => m.FormatAsHtml())
            };
        }
    }
}
