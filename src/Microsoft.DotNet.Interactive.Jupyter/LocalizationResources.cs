// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Interactive.Jupyter;

/// <summary>
/// Provides localizable strings for help and error messages.
/// </summary>
internal static class LocalizationResources
{
    /// <summary>
    ///   Gets a localized message like: List the available magic commands / directives.
    /// </summary>
    internal static string Magics_lsmagic_Description() =>
        GetResourceString(Resources.Magics_lsmagic_Description);

    /// <summary>
    ///   Gets a localized message like: Convert the code that follows from Markdown into HTML
    /// </summary>
    internal static string Magics_markdown_Description() =>
        GetResourceString(Resources.Magics_markdown_Description);

    /// <summary>
    ///   Gets a localized message like: Time the execution of the following code in the submission.
    /// </summary>
    internal static string Magics_time_Description() =>
        GetResourceString(Resources.Magics_time_Description);

    /// <summary>
    /// Interpolates values into a localized string.
    /// </summary>
    /// <param name="resourceString">The string template into which values will be interpolated.</param>
    /// <param name="formatArguments">The values to interpolate.</param>
    /// <returns>The final string after interpolation.</returns>
    private static string GetResourceString(string resourceString, params object[] formatArguments)
    {
        if (resourceString is null)
        {
            return string.Empty;
        }
        if (formatArguments.Length > 0)
        {
            return string.Format(resourceString, formatArguments);
        }
        return resourceString;
    }
}
