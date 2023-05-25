// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.DotNet.Interactive.CSharpProject.Tests;

public class LogEntryList : ConcurrentQueue<(
    string MessageTemplate,
    object[] Args,
    List<(string Name, object Value)> Properties,
    byte LogLevel,
    DateTime TimestampUtc,
    Exception Exception,
    string OperationName,
    string Category,
    (string Id,
    bool IsStart,
    bool IsEnd,
    bool? IsSuccessful,
    TimeSpan? Duration) Operation)>
{
    public void Add((
        string MessageTemplate,
        object[] Args,
        List<(string Name, object Value)> Properties,
        byte LogLevel,
        DateTime TimestampUtc,
        Exception Exception,
        string OperationName,
        string Category,
        (string Id,
        bool IsStart,
        bool IsEnd,
        bool? IsSuccessful,
        TimeSpan? Duration) Operation) e) =>
            Enqueue(e);

    public (
        string MessageTemplate,
        object[] Args,
        List<(string Name, object Value)> Properties,
        byte LogLevel,
        DateTime TimestampUtc,
        Exception Exception,
        string OperationName,
        string Category,
        (string Id,
        bool IsStart,
        bool IsEnd,
        bool? IsSuccessful,
        TimeSpan? Duration) Operation) this[int index] =>
            this.ElementAt(index);
}
