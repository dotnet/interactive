// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Interactive.Journey;

internal static class LocalizationResources
{
    /// <summary>
    ///   Interpolates values into a localized string Specify lesson source file
    /// </summary>
    internal static string Magics_model_answer_from_file_Description()
        => GetResourceString(Resources.Magics_model_answer_from_file_Description);

    /// <summary>
    ///   Interpolates values into a localized string The {0} and {1} options cannot be used together
    /// </summary>
    internal static string Magics_model_answer_from_file_ErrorMessage(string option1, string option2)
        => GetResourceString(Resources.Magics_model_answer_from_file_ErrorMessage, option1, option2);

    /// <summary>
    ///   Interpolates values into a localized string Specify lesson source URL
    /// </summary>
    internal static string Magics_model_answer_from_url_Description()
        => GetResourceString(Resources.Magics_model_answer_from_url_Description);

    /// <summary>
    ///   Interpolates values into a localized string similar to File does not exist: {0}.
    /// </summary>
    internal static string FileDoesNotExist(string filePath) =>
        GetResourceString(Resources.FileDoesNotExist, filePath);

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
