// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.ValueSharing;

namespace Microsoft.DotNet.Interactive.Commands
{
    public class RequestValue : KernelCommand
    {
        public string Name { get; }

        public string MimeType { get;  }

        public RequestValue(string name, string targetKernelName, string mimeType = null ) : base(targetKernelName)
        {
            Name = name;
            MimeType = mimeType;
        }

        public override Task InvokeAsync(KernelInvocationContext context)
        {
            if (context.HandlingKernel is ISupportGetValue supportGetValuesKernel)
            {
                if (supportGetValuesKernel.TryGetValue(Name, out object value))
                {
                    if (value is { })
                    {
                        var valueType = value.GetType();
                        var mimeType = MimeType ?? Formatter.GetPreferredMimeTypeFor(valueType);
                        var formatter = Formatter.GetPreferredFormatterFor(valueType, mimeType);
                        if (formatter.MimeType != mimeType)
                        {
                            throw new InvalidOperationException($"MimeType {mimeType} is not supported");
                        }

                        using var writer = new StringWriter(CultureInfo.InvariantCulture);
                        formatter.Format(value, writer);
                        var formatted = new FormattedValue(mimeType, writer.ToString());
                        context.Publish(new ValueProduced(value, Name, formatted, this));
                    }
                    else
                    {
                        var mimeType = MimeType ?? Formatter.GetPreferredMimeTypeFor(typeof(object));
                        var formatted = new FormattedValue(mimeType, "null");

                        context.Publish(new ValueProduced(value, Name, formatted, this));
                    }

                    return Task.CompletedTask;
                }

                throw new InvalidOperationException($"Cannot find value named: {Name}");
            }

            throw new InvalidOperationException($"Kernel {context.HandlingKernel.Name} doesn't support command {nameof(RequestValue)}");
        }
    }
}