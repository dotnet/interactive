// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Microsoft.DotNet.Interactive.Commands
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    public sealed class NotSerializableAttribute : Attribute
    {
        public string Message { get; }

        public NotSerializableAttribute(string message)
        {
            Message = message;
        }
    }
    public class RequestValueNames : KernelCommand
    {
        public RequestValueNames(string targetKernelName) : base(targetKernelName)
        {
            
        }
    }

    public class RequestValue : KernelCommand
    {
        public string VariableName { get; }
        
        public IReadOnlyCollection<string> MimeType { get; }

        public RequestValue(string variableName, string targetKernelName, IReadOnlyCollection<string> mimeType = null ) : base(targetKernelName)
        {
            VariableName = variableName;
            MimeType = mimeType;
        }
    }

    public class SetFormattedValue : KernelCommand
    {
        public string FormattedValue { get; }
        public string MimeType { get; }
        public string Name { get; }

        public SetFormattedValue(string formattedValue, string mimeType, string name, string targetKernelName) : base(targetKernelName)
        {
            if (string.IsNullOrWhiteSpace(mimeType))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(mimeType));
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(name));
            }
            FormattedValue = formattedValue ?? throw new ArgumentNullException(nameof(formattedValue));

            MimeType = mimeType;
            Name = name;
        }
    }

    [NotSerializableAttribute("This command is cannot be serialized, consider using SetFormattedValue instead")]
    public class SetReferenceValue : KernelCommand
    {
        public object Value { get; }
        public string Name { get; }

        public SetReferenceValue(object value, string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(name));
            }
            Value = value ?? throw new ArgumentNullException(nameof(value));
            Name = name;
        }
    }
}
