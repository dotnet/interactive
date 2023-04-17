// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Interactive;

internal static class LocalizationResources
{
    /// <summary>
    ///   Interpolates values into a localized string The name of the value to be created in the current kernel.
    /// </summary>
    internal static string Magics_set_name_Description()
        => GetResourceString(Resources.Magics_set_name_Description);

    /// <summary>
    ///   Interpolates values into a localized string Shares the specified value by reference if kernels are in the same process.
    /// </summary>
    internal static string Magics_set_byref_Description()
        => GetResourceString(Resources.Magics_set_byref_Description);

    /// <summary>
    ///   Interpolates values into a localized string The MIME type by which the value should be represented. This will often 
    ///   determine how an object will be formatted into a string.
    /// </summary>
    internal static string Magics_set_mime_type_Description()
        => GetResourceString(Resources.Magics_set_mime_type_Description);

    /// <summary>
    ///   Interpolates values into a localized string The --mime-type and --byref options cannot be used together.
    /// </summary>
    internal static string Magics_set_mime_type_ErrorMessageCannotBeUsed()
        => GetResourceString(Resources.Magics_set_mime_type_ErrorMessage_CannotBeUsed);

    /// <summary>
    ///   Interpolates values into a localized string Sharing by reference is not allowed when kernels are remote.
    /// </summary>
    internal static string Magics_set_ErrorMessageSharingByReference()
        => GetResourceString(Resources.Magics_set_ErrorMessage_SharingByReference);

    /// <summary>
    ///   Interpolates values into a localized string The value to be set. @input:user_prompt allows you to prompt 
    ///   the user for this value. Values can be requested from other kernels by name, for example @csharp:variableName.
    /// </summary>
    internal static string Magics_set_value_Description()
        => GetResourceString(Resources.Magics_set_value_Description);

    /// <summary>
    ///   Interpolates values into a localized string The name to give the the value in the importing kernel.
    /// </summary>
    internal static string Magics_share_as_Description()
        => GetResourceString(Resources.Magics_share_as_Description);

    /// <summary>
    ///   Interpolates values into a localized string The name of the kernel to get the value from.
    /// </summary>
    internal static string Magics_share_from_Description()
        => GetResourceString(Resources.Magics_share_from_Description);

    /// <summary>
    ///   Interpolates values into a localized string Share the value as a string formatted to the specified MIME type.
    /// </summary>
    internal static string Magics_share_mime_type_Description()
        => GetResourceString(Resources.Magics_share_mime_type_Description);

    /// <summary>
    ///   Interpolates values into a localized string The name of the value to share. (This is also the default name of 
    ///   the value created in the destination kernel, unless --as is used to specify a different one.)
    /// </summary>
    internal static string Magics_share_name_Description()
        => GetResourceString(Resources.Magics_share_name_Description);

    /// <summary>
    ///   Interpolates values into a localized string The {0} and {1} options cannot be used together.
    /// </summary>
    internal static string Magics_ErrorMessageCannotBeUsedTogether(string option1, string option2)
        => GetResourceString(Resources.Magics_ErrorMessage_CannotBeUsedTogether, option1, option2);

    /// <summary>
    ///   Interpolates values into a localized string Runs another notebook or source code file inline.
    /// </summary>
    internal static string Magics_import_Description()
        => GetResourceString(Resources.Magics_import_Description);

    /// <summary>
    ///   Interpolates values into a localized string Specifies a file whose contents will be stored.
    /// </summary>
    internal static string Magics_value_from_file_Description()
        => GetResourceString(Resources.Magics_value_from_file_Description);

    /// <summary>
    ///   Interpolates values into a localized string Specifies a URL whose content will be stored.
    /// </summary>
    internal static string Magics_value_from_url_Description()
        => GetResourceString(Resources.Magics_value_from_url_Description);

    /// <summary>
    ///   Interpolates values into a localized string Specifies a value to be stored directly.
    ///   Specifying @input:value allows you to prompt the user for this value.
    /// </summary>
    internal static string Magics_value_from_value_Description()
        => GetResourceString(Resources.Magics_value_from_value_Description);

    /// <summary>
    ///   Interpolates values into a localized string A mime type for the value. If specified, 
    ///   displays the value immediately as an output using the specified mime type.
    /// </summary>
    internal static string Magics_value_mime_type_Description()
        => GetResourceString(Resources.Magics_value_mime_type_Description);

    /// <summary>
    ///   Interpolates values into a localized string The name of the value to create.
    ///   You can use #!share to retrieve this value from another subkernel.
    /// </summary>
    internal static string Magics_value_name_Description()
        => GetResourceString(Resources.Magics_value_name_Description);

    /// <summary>
    ///   Interpolates values into a localized string Enables session logging.
    /// </summary>
    internal static string Magics_log_Description()
        => GetResourceString(Resources.Magics_log_Description);

    /// <summary>
    ///   Interpolates values into a localized string Sets a value in the current kernel
    /// </summary>
    internal static string Magics_set_Description()
        => GetResourceString(Resources.Magics_set_Description);

    /// <summary>
    ///   Interpolates values into a localized string Get a value from one kernel and create 
    ///   a copy (or a reference if the kernels are in the same process) in another.
    /// </summary>
    internal static string Magics_share_Description()
        => GetResourceString(Resources.Magics_share_Description);

    /// <summary>
    ///   Interpolates values into a localized string Display the names of the current top-level variables.
    /// </summary>
    internal static string Magics_who_Description()
        => GetResourceString(Resources.Magics_who_Description);

    /// <summary>
    ///   Interpolates values into a localized string Display the names of the current top-level variables and their values.
    /// </summary>
    internal static string Magics_whos_Description()
        => GetResourceString(Resources.Magics_whos_Description);

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
