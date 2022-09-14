﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Microsoft.DotNet.Interactive.Documents.ParserServer;

namespace Microsoft.DotNet.Interactive.Documents;

[JsonConverter(typeof(KernelInfoConverter))]
public class KernelInfo
{
    public KernelInfo(
        string name, 
        IReadOnlyCollection<string>? aliases = null)
    {
        Validate(name);
        Name = name;

        if (aliases is not null)
        {
            foreach (var alias in aliases)
            {
                Validate(alias);
            }

            Aliases = aliases;
        }
        else
        {
            Aliases = new string[] { };
        }
    }

    public string Name { get; }

    public IReadOnlyCollection<string> Aliases { get; }

    public override string ToString()
    {
        return Name;
    }

    private static void Validate(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("KernelInfo name cannot be null or consist entirely of whitespace.");
        }

        if (name.StartsWith("#"))
        {
            throw new ArgumentException("Kernel names or aliases cannot begin with \"#\"");
        }
    }
}