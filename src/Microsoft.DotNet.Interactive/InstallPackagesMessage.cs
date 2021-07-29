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

        public string FormatAsHtml()
        {
            string progress = new String('.', Progress);
            var items = new List<IHtmlContent>();
            items.Add(InstallMessage("Restore sources", RestoreSources));
            items.Add(InstallMessage("Installing Packages", InstallingPackages, progress));
            items.Add(InstallMessage("Installed Packages", InstalledPackages));
            var r =  div(items).ToString();
            return r;
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
                    yield return $" - {installing}   " + new String('.', Progress);
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
            return String.Join(System.Environment.NewLine, FormatAsPlainTextLines());
        }
    }

    class InstallPackagesMessageFormatterSource : ITypeFormatterSource
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
