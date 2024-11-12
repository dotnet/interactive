// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Management.Automation;
using Microsoft.DotNet.Interactive.Formatting;
using static Microsoft.DotNet.Interactive.Kernel;

namespace Microsoft.DotNet.Interactive.PowerShell.Commands;

/// <summary>
/// Takes the the input and displays it on the client using .NET Interactive's formatters.
/// This returns a DisplayedValue that can then be updated by calling `Update` on the object.
/// </summary>
[Cmdlet(VerbsData.Out, "Display", DefaultParameterSetName = "MimeType")]
[OutputType("Interactive.Events.DisplayedValue")]
[Alias("od")]
public sealed class OutDisplayCommand : PSCmdlet
{
    /// <summary>
    /// The object from pipeline.
    /// </summary>
    [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true)]
    [AllowNull]
    public object InputObject { get; set; }

    /// <summary>
    /// The MimeType to use.
    /// </summary>
    [Parameter(Position = 1, ParameterSetName = "MimeType")]
    public string MimeType { get; set; } = "text/html";

    /// <summary>
    /// Determines whether the DisplayedValue should get written to the pipeline.
    /// </summary>
    [Parameter()]
    public SwitchParameter PassThru { get; set; }

    /// <summary>
    /// ProcessRecord override.
    /// </summary>
    protected override void ProcessRecord()
    {
        object obj = InputObject is PSObject psObj ? psObj.Unwrap() : InputObject;

        DisplayedValue displayedValue = default;

        if (MimeType is "application/javascript" or
                        "application/json" or
                        "text/html" or
                        "text/markdown" or
                        "text/plain")
        {
            displayedValue = display(obj, MimeType);
        }
        else
        {
            displayedValue = Formatter.ToDisplayString(obj).DisplayAs(MimeType);
        }

        if (PassThru.IsPresent)
        {
            WriteObject(displayedValue);
        }
    }
}