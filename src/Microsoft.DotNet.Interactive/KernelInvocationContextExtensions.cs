// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;

namespace Microsoft.DotNet.Interactive
{
    public static class KernelInvocationContextExtensions
    {
        public static DisplayedValue Display(
            this KernelInvocationContext context,
            object value,
            string mimeType = null)
        {
            var displayId = Guid.NewGuid().ToString();

            mimeType ??= Formatter.GetPreferredMimeTypeFor(value?.GetType());

            var formattedValue = new FormattedValue(
                mimeType,
                value.ToDisplayString(mimeType));

            context.Publish(
                new DisplayedValueProduced(
                    value,
                    context?.Command,
                    new[] { formattedValue },
                    displayId));

            var displayedValue = new DisplayedValue(displayId, mimeType, context);


            return displayedValue;
        }

        public static void DisplayStandardOut(
            this KernelInvocationContext context,
            string output,
            KernelCommand command = null)
        {
            var formattedValues = new List<FormattedValue>
            {
                new FormattedValue(
                    PlainTextFormatter.MimeType, output)
            };

            context.Publish(
                new StandardOutputValueProduced(
                    command ?? context.Command,
                    formattedValues));
        }

        public static void DisplayStandardError(
            this KernelInvocationContext context,
            string error,
            KernelCommand command = null)
        {
            var formattedValues = new List<FormattedValue>
            {
                new FormattedValue(
                    PlainTextFormatter.MimeType, error)
            };

            context.Publish(
                new StandardErrorValueProduced(
                    command ?? context.Command,
                    formattedValues));
        }
    }
}