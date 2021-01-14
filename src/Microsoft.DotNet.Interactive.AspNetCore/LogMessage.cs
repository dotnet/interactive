// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Interactive.AspNetCore
{
    internal class LogMessage
    {
        public LogLevel LogLevel { get; set; }
        public string Category { get; set; }
        public EventId EventId { get; set; }
        public string Message { get; set; }
        public Exception Exception { get; set; }
    }
}
