// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Management.Automation;
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
    [ValidateSet(
        "application/javascript",
        "application/json",
        "text/html",
        "text/markdown",
        "text/plain"
    )]
    public string MimeType { get; set; } = "text/html";

    /// <summary>
    /// If the user wants to send back a MimeType that isn't in the MimeType parameter,
    /// they can use this.
    /// </summary>
    [Parameter(Position = 1, ParameterSetName = "CustomMimeType")]
    public string CustomMimeType { get; set; }

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
        DisplayedValue displayedValue = display(obj, MimeType);
            
        if (PassThru.IsPresent)
        {
            WriteObject(displayedValue);
        }
    }
}