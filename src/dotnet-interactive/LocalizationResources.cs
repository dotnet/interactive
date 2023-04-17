// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Interactive.App;

internal static class LocalizationResources
{
    /// <summary>
    ///   Interpolates values into a localized string Show version and build information
    /// </summary>
    internal static string Magics_about_Description()
        => GetResourceString(Resources.Magics_about_Description);

    /// <summary>
    ///   Interpolates values into a localized string The default language for the kernel
    /// </summary>
    internal static string Cli_dotnet_interactive_jupyter_default_kernel_Description()
        => GetResourceString(Resources.Cli_dotnet_interactive_jupyter_default_kernel_Description);

    /// <summary>
    ///   Interpolates values into a localized string Interactive programming for .NET.
    /// </summary>
    internal static string Cli_dotnet_interactive_Description()
        => GetResourceString(Resources.Cli_dotnet_interactive_Description);

    /// <summary>
    ///   Interpolates values into a localized string The path to a connection file provided by Jupyter
    /// </summary>
    internal static string Cli_dotnet_interactive_jupyter_connection_file_Description()
        => GetResourceString(Resources.Cli_dotnet_interactive_jupyter_connection_file_Description);

    /// <summary>
    ///   Interpolates values into a localized string Specifies the range of ports to use to enable HTTP services
    /// </summary>
    internal static string Cli_dotnet_interactive_jupyter_install_http_port_range_Description()
        => GetResourceString(Resources.Cli_dotnet_interactive_jupyter_install_http_port_range_Description);

    /// <summary>
    ///   Interpolates values into a localized string Install the .NET kernel for Jupyter
    /// </summary>
    internal static string Cli_dotnet_interactive_jupyter_install_Description()
        => GetResourceString(Resources.Cli_dotnet_interactive_jupyter_install_Description);

    /// <summary>
    ///   Interpolates values into a localized string Starts dotnet-interactive as a Jupyter kernel
    /// </summary>
    internal static string Cli_dotnet_interactive_jupyter_Description()
        => GetResourceString(Resources.Cli_dotnet_interactive_jupyter_Description);

    /// <summary>
    ///   Interpolates values into a localized string Enable file logging to the specified directory
    /// </summary>
    internal static string Cli_dotnet_interactive_log_path_Description()
        => GetResourceString(Resources.Cli_dotnet_interactive_log_path_Description);

    /// <summary>
    ///   Interpolates values into a localized string Starts a process to parse and serialize notebooks.
    /// </summary>
    internal static string Cli_dotnet_interactive_notebook_parserDescription()
        => GetResourceString(Resources.Cli_dotnet_interactive_notebook_parser_Description);

    /// <summary>
    ///   Interpolates values into a localized string Must specify a port range
    /// </summary>
    internal static string Cli_ErrorMessageMustSpecifyPortRange()
        => GetResourceString(Resources.Cli_ErrorMessageMustSpecifyPortRange);

    /// <summary>
    ///   Interpolates values into a localized string Must specify a port range as StartPort-EndPort
    /// </summary>
    internal static string CliErrorMessageMustSpecifyPortRangeAsStartPortEndPort()
        => GetResourceString(Resources.Cli_ErrorMessage_MustSpecifyPortRangeAsStartPortEndPort);

    /// <summary>
    ///   Interpolates values into a localized string Start port must be lower then end port
    /// </summary>
    internal static string CliErrorMessageStartPortMustBeLower()
        => GetResourceString(Resources.Cli_ErrorMessage_StartPortMustBeLower);

    /// <summary>
    ///   Interpolates values into a localized string Installs the kernel specs to the specified directory
    /// </summary>
    internal static string Cli_dotnet_interactive_jupyter_install_path_Description()
        => GetResourceString(Resources.Cli_dotnet_interactive_jupyter_install_path_Description);

    /// <summary>
    ///   Interpolates values into a localized string Starts dotnet-interactive with kernel functionality
    ///   exposed over standard I/O
    /// </summary>
    internal static string Cli_dotnet_interactive_stdio_Description()
        => GetResourceString(Resources.Cli_dotnet_interactive_stdio_Description);

    /// <summary>
    ///   Interpolates values into a localized string Cannot specify both {0} and {1} together
    /// </summary>
    internal static string Cli_dotnet_interactive_stdio_http_port_ErrorMessageCannotSpecifyBoth(string conflictingOption, string parsedOption)
        => GetResourceString(Resources.Cli_dotnet_interactive_stdio_http_port_ErrorMessageCannotSpecifyBoth, conflictingOption, parsedOption);

    /// <summary>
    ///   Interpolates values into a localized string Must specify a port number or *.
    /// </summary>
    internal static string Cli_dotnet_interactive_stdio_http_port_ErrorMessageMustSpecifyPortNumber()
        => GetResourceString(Resources.Cli_dotnet_interactive_stdio_http_port_ErrorMessageMustSpecifyPortNumber);

    /// <summary>
    ///   Interpolates values into a localized string Specifies the port on which to enable HTTP services
    /// </summary>
    internal static string Cli_dotnet_interactive_stdio_http_port_Description()
        => GetResourceString(Resources.Cli_dotnet_interactive_stdio_http_port_Description);

    /// <summary>
    ///   Interpolates values into a localized string Specifies the range of ports to use to enable HTTP services
    /// </summary>
    internal static string Cli_dotnet_interactive_stdio_http_port_range_Description()
        => GetResourceString(Resources.Cli_dotnet_interactive_stdio_http_port_range_Description);

    /// <summary>
    ///   Interpolates values into a localized string Name of the kernel host.
    /// </summary>
    internal static string Cli_dotnet_interactive_stdio_kernel_host_Description()
        => GetResourceString(Resources.Cli_dotnet_interactive_stdio_kernel_host_Description);

    /// <summary>
    ///   Interpolates values into a localized string Enable preview kernel features.
    /// </summary>
    internal static string Cli_dotnet_interactive_stdio_preview_Description()
        => GetResourceString(Resources.Cli_dotnet_interactive_stdio_preview_Description);

    /// <summary>
    ///   Interpolates values into a localized string Working directory to which to change after launching the kernel.
    /// </summary>
    internal static string Cli_dotnet_interactive_stdio_working_directory_Description()
        => GetResourceString(Resources.Cli_dotnet_interactive_stdio_working_directory_Description);

    /// <summary>
    ///   Interpolates values into a localized string Enable verbose logging to the console
    /// </summary>
    internal static string Cli_dotnet_interactive_verbose_Description()
        => GetResourceString(Resources.Cli_dotnet_interactive_verbose_Description);

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
