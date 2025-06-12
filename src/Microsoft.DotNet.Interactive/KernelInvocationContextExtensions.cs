﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;

namespace Microsoft.DotNet.Interactive;

public static class KernelInvocationContextExtensions
{
    public static DisplayedValue Display(
        this KernelInvocationContext context,
        object value,
        params string[] mimeTypes)
    {
        var formattedValues = FormattedValue.CreateManyFromObject(value, mimeTypes).ToArray();

        var displayedValue = new DisplayedValue(value, formattedValues);

        context.Display(displayedValue);

        return displayedValue;
    }

    internal static void Display(
        this KernelInvocationContext context,
        DisplayedValue displayedValue)
    {
        if (!displayedValue.IsUpdated)
        {
            context.Publish(
                new DisplayedValueProduced(
                    displayedValue.Value,
                    context.CurrentlyExecutingCommand,
                    displayedValue.FormattedValues,
                    displayedValue.DisplayId));
        }
        else
        {
            context.Publish(
                new DisplayedValueUpdated(
                    displayedValue.Value,
                    displayedValue.DisplayId,
                    context.CurrentlyExecutingCommand,
                    displayedValue.FormattedValues));
        }
    }

    public static DisplayedValue DisplayAs(
        this KernelInvocationContext context,
        string value,
        string mimeType)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (string.IsNullOrWhiteSpace(mimeType))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(mimeType));
        }

        var formattedValue = new FormattedValue(
            mimeType,
            value);

        var formattedValues = new[] { formattedValue };

        var displayedValue = new DisplayedValue(value, formattedValues);

        context.Publish(
            new DisplayedValueProduced(
                value,
                context.Command,
                formattedValues,
                displayedValue.DisplayId));

        return displayedValue;
    }

    internal static void DisplayStandardOut(
        this KernelInvocationContext context,
        string output,
        KernelCommand command = null)
    {
        var formattedValues = new List<FormattedValue>
        {
            new(PlainTextFormatter.MimeType, output)
        };

        context.Publish(
            new StandardOutputValueProduced(
                command ?? context.Command,
                formattedValues));
    }

    internal static void DisplayStandardError(
        this KernelInvocationContext context,
        string error,
        KernelCommand command = null)
    {
        var formattedValues = new List<FormattedValue>
        {
            new(PlainTextFormatter.MimeType, error)
        };

        context.Publish(
            new StandardErrorValueProduced(
                command ?? context.Command,
                formattedValues));
    }
}