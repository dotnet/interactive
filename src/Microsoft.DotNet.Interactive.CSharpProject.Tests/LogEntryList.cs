// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Linq;

namespace Microsoft.DotNet.Interactive.CSharpProject.Tests
{
    public class LogEntryList : ConcurrentQueue<(
        byte LogLevel,
        DateTime TimestampUtc,
        Func<(string Message, (string Name, object Value)[] Properties)> Evaluate,
        Exception Exception,
        string OperationName,
        string Category,
        (string Id,
        bool IsStart,
        bool IsEnd,
        bool? IsSuccessful,
        TimeSpan? Duration) Operation)>
    {
        public void Add(
            (
                byte LogLevel,
                DateTime TimestampUtc,
                Func<(string Message, (string Name, object Value)[] Properties)> Evaluate,
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
            byte LogLevel,
            DateTime TimestampUtc,
            Func<(string Message, (string Name, object Value)[] Properties)> Evaluate,
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
}