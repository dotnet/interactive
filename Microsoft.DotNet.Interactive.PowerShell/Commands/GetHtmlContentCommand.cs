// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using static Microsoft.DotNet.Interactive.Kernel;

namespace Microsoft.DotNet.Interactive.PowerShell.Commands
{
    using System.Management.Automation;

    /// <summary>
    /// Takes the the string input and turns it into an IHtmlContent that can be passed in to
    /// Out-Display to render Html in a Notebook cell's output.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "HtmlContent")]
    [OutputType("AspNetCore.Html.IHtmlContent")]
    [Alias("ghc")]
    public sealed class GetHtmlContentCommand : PSCmdlet
    {
        /// <summary>
        /// The Html string to convert into an HtmlContent object.
        /// </summary>
        [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true)]
        public string HtmlString { get; set; }

        /// <summary>
        /// ProcessRecord override.
        /// </summary>
        protected override void ProcessRecord()
        {
            WriteObject(HTML(HtmlString));
        }
    }
}
