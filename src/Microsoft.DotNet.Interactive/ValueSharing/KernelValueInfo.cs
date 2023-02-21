// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Microsoft.DotNet.Interactive.Formatting;

namespace Microsoft.DotNet.Interactive.ValueSharing;

public class KernelValueInfo
{
    private readonly IReadOnlyCollection<string> _preferredMimeTypes;
    private string _typeName;

    public KernelValueInfo(string name, FormattedValue formattedValue, Type type = null, string typeName = null)
    {
        Name = name;
        Type = type;
        TypeName = typeName;
        FormattedValue = formattedValue ?? throw new ArgumentNullException(nameof(formattedValue));
    }

    /// <summary>
    /// The type name of the value, as appropriate to the source kernel.
    /// </summary>
    public string TypeName
    {
        get
        {
            if (_typeName is null && Type is not null)
            {
                _typeName = Type.ToDisplayString(PlainTextSummaryFormatter.MimeType);
            }

            return _typeName;
        }
        set => _typeName = value;
    }

    /// <summary>
    /// The name of the value.
    /// </summary>
    public string Name { get; }

    public FormattedValue FormattedValue { get; }

    public IReadOnlyCollection<string> PreferredMimeTypes
    {
        get => _preferredMimeTypes ??
               (Type is not null
                    ? Formatter.GetPreferredMimeTypesFor(Type)
                    : Array.Empty<string>());
        init => _preferredMimeTypes = value;
    }

    [JsonIgnore] 
    public Type Type { get; }
}