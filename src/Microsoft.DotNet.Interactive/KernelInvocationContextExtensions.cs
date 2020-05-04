// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;

namespace Microsoft.DotNet.Interactive
{
    public static class KernelInvocationContextExtensions
    {
        public static Task<DisplayedValue> DisplayAsync(
            this KernelInvocationContext context,
            object value,
            string mimeType = null)
        {
            DisplayedValue result = Display(context, value, mimeType);
            return Task.FromResult(result);
        }

        public static DisplayedValue Display(
            this KernelInvocationContext context,
            object value,
            string mimeType = null)
        {
            var displayId = Kernel.DisplayIdGenerator?.Invoke() ??
                            Guid.NewGuid().ToString();

            mimeType ??= Formatter.PreferredMimeTypeFor(value.GetType());

            var formattedValue = new FormattedValue(
                mimeType,
                value.ToDisplayString(mimeType));

            context.Publish(
                new DisplayedValueProduced(
                    value,
                    context?.Command,
                    new[] { formattedValue },
                    displayId));

            return new DisplayedValue(displayId, mimeType, context);
        }

        public static void DisplayStandardOut(
            this KernelInvocationContext context,
            string output,
            IKernelCommand command = null)
        {
            var formattedValues = new List<FormattedValue>
            {
                new FormattedValue(
                    PlainTextFormatter.MimeType, output)
            };

            context.Publish(
                new StandardOutputValueProduced(
                    output,
                    command ?? context.Command,
                    formattedValues));
        }

        public static void DisplayStandardError(
            this KernelInvocationContext context,
            string error,
            IKernelCommand command = null)
        {
            var formattedValues = new List<FormattedValue>
            {
                new FormattedValue(
                    PlainTextFormatter.MimeType, error)
            };

            context.Publish(
                new StandardErrorValueProduced(
                    error,
                    command ?? context.Command,
                    formattedValues));
        }
    }
}