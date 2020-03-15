// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reflection;
using Pocket;
using Xunit.Sdk;
using static Pocket.Logger;

namespace Microsoft.DotNet.Interactive.Tests.Utility
{
    internal class LogTestNamesToPocketLoggerAttribute : BeforeAfterTestAttribute
    {
        private static readonly ConcurrentDictionary<MethodInfo, OperationLogger> _operations = new ConcurrentDictionary<MethodInfo, OperationLogger>();

        public override void Before(MethodInfo methodUnderTest)
        {
            if (methodUnderTest == null)
            {
                return;
            }

            _operations.TryAdd(
                methodUnderTest,
                Log.OnEnterAndExit(name: $"{methodUnderTest?.DeclaringType?.Name}.{methodUnderTest.Name}"));
        }

        public override void After(MethodInfo methodUnderTest)
        {
            if (_operations.TryRemove(methodUnderTest, out var operation))
            {
                operation.Dispose();
            }
        }
    }
}