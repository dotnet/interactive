// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Formatting;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;
using static Microsoft.DotNet.Interactive.Formatting.PocketViewTags;

namespace Microsoft.DotNet.Interactive
{
    // Data bag containing the data to be displayed when reporting nuget resolve progress 
    public class InstallPackagesMessage
    {
        static InstallPackagesMessage ()
        {
            Formatter.Register<InstallPackagesMessage>(m => m.FormatAsPlainText(), PlainTextFormatter.MimeType);
            Formatter.Register<InstallPackagesMessage>(m => m.FormatAsHtml(), HtmlFormatter.MimeType);
            Formatter.SetPreferredMimeTypeFor(typeof(InstallPackagesMessage), PlainTextFormatter.MimeType);
        }

        public IEnumerable<string> RestoreSources;
        public IEnumerable<string> InstalledPackages;
        public IEnumerable<string> InstallingPackages;
        public int Progress;

        public InstallPackagesMessage(
                IEnumerable<string> restoreSources,
                IEnumerable<string> installedPackages,
                IEnumerable<string> installingPackages,
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
            return div(items).ToString();
        }

        private string FormatAsPlainText()
        {
            var result = new StringBuilder();
            if (RestoreSources.Count() > 0)
            {
                result.Append("Restore sources");
                foreach (var source in RestoreSources)
                {
                    result.Append(System.Environment.NewLine);
                    result.Append(" - " + source);
                }
                result.Append(System.Environment.NewLine);
            }

            if (InstalledPackages.Count() > 0)
            {
                result.Append("Installed Packages");
                foreach (var installed in InstalledPackages)
                {
                    result.Append(System.Environment.NewLine);
                    result.Append(" - " + installed);
                }
                result.Append(System.Environment.NewLine);
            }

            if (InstallingPackages.Count() > 0)
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
}
