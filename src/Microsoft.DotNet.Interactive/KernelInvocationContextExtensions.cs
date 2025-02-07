// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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

        var displayedValue = new DisplayedValue(formattedValues, context);

        context.Publish(
            new DisplayedValueProduced(
                value,
                context?.CurrentlyExecutingCommand,
                formattedValues,
                displayedValue.DisplayId));

        return displayedValue;
    }

    public static DisplayedValue DisplayAs(
        this KernelInvocationContext context,
        string value,
        string mimeType)
    {
        if (context == null)
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

        var displayedValue = new DisplayedValue(formattedValues, context);

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

    public static void PublishValueProduced(
        this KernelInvocationContext context,
        RequestValue requestValue,
        object value)
    {
        // FIX: (PublishValueProduced) remove this from the public interface
        var valueType = value?.GetType();

        var requestedMimeType = requestValue.MimeType;

        var formatter = Formatter.GetPreferredFormatterFor(valueType, requestedMimeType);

        using var writer = new StringWriter(CultureInfo.InvariantCulture);
        formatter.Format(value, writer);

        var formatted = new FormattedValue(
            requestedMimeType,
            value.ToDisplayString(requestValue.MimeType));

        context.Publish(new ValueProduced(
            value,
            requestValue.Name,
            formatted,
            requestValue));
    }
}