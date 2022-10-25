// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Based on the Microsoft DAP types https://microsoft.github.io/debug-adapter-protocol/specification 
namespace Microsoft.DotNet.Interactive.Jupyter.ValueSharing;

public static class ValueAdapterMessageType
{
    public const string Request = "request";
    public const string Response = "response";
    public const string Event = "event";
}

public static class ValueAdapterCommandTypes
{
    public const string SetVariable = "setVariable";
    public const string GetVariable = "getVariable";
    public const string Variables = "variables";
}

public static class ValueAdapterEventTypes
{
    public const string Initialized = "initialized";
}
