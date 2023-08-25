// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.DotNet.Interactive.PowerShell.Host;

internal class ConsoleColorProxy
{
    private readonly PSKernelHostUserInterface _ui;

    public ConsoleColorProxy(PSKernelHostUserInterface ui)
    {
        _ui = ui ?? throw new ArgumentNullException(nameof(ui));
    }

    public ConsoleColor FormatAccentColor
    {
        get => _ui.FormatAccentColor;
        set => _ui.FormatAccentColor = value;
    }

    public ConsoleColor ErrorAccentColor
    {
        get => _ui.ErrorAccentColor;
        set => _ui.ErrorAccentColor = value;
    }

    public ConsoleColor ErrorForegroundColor
    {
        get => _ui.ErrorForegroundColor;
        set => _ui.ErrorForegroundColor = value;
    }

    public ConsoleColor ErrorBackgroundColor
    {
        get => _ui.ErrorBackgroundColor;
        set => _ui.ErrorBackgroundColor = value;
    }

    public ConsoleColor WarningForegroundColor
    {
        get => _ui.WarningForegroundColor;
        set => _ui.WarningForegroundColor = value;
    }

    public ConsoleColor WarningBackgroundColor
    {
        get => _ui.WarningBackgroundColor;
        set => _ui.WarningBackgroundColor = value;
    }

    public ConsoleColor DebugForegroundColor
    {
        get => _ui.DebugForegroundColor;
        set => _ui.DebugForegroundColor = value;
    }

    public ConsoleColor DebugBackgroundColor
    {
        get => _ui.DebugBackgroundColor;
        set => _ui.DebugBackgroundColor = value;
    }

    public ConsoleColor VerboseForegroundColor
    {
        get => _ui.VerboseForegroundColor;
        set => _ui.VerboseForegroundColor = value;
    }

    public ConsoleColor VerboseBackgroundColor
    {
        get => _ui.VerboseBackgroundColor;
        set => _ui.VerboseBackgroundColor = value;
    }

    public ConsoleColor ProgressForegroundColor
    {
        get => _ui.ProgressForegroundColor;
        set => _ui.ProgressForegroundColor = value;
    }

    public ConsoleColor ProgressBackgroundColor
    {
        get => _ui.ProgressBackgroundColor;
        set => _ui.ProgressBackgroundColor = value;
    }
}