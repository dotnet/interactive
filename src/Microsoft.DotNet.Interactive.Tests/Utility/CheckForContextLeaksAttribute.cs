// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Microsoft.DotNet.Interactive.Utility;
using Pocket;
using Xunit.Sdk;
using static Pocket.Logger<Microsoft.DotNet.Interactive.Tests.Utility.CheckForContextLeaksAttribute>;

namespace Microsoft.DotNet.Interactive.Tests.Utility
{
    public class CheckForContextLeaksAttribute : BeforeAfterTestAttribute
    {
        public override void Before(MethodInfo methodUnderTest)
        {
            if (KernelInvocationContext.Current is { } context)
            {
                Log.Error(message: "KernelInvocationContext is {context} before test {methodUnderTest}", args: new object[] { context, methodUnderTest });
            }

            if (AsyncContext.Id is { } id)
            {
                Log.Error(message: "AsyncContext.Id is {id} before test {methodUnderTest}", args: new object[] { id, methodUnderTest });
            }

            if (ConsoleOutput.RefCount != 0)
            {
                Log.Info("ConsoleOutput.RefCount is {refCount} before test {methodUnderTest}", ConsoleOutput.RefCount, methodUnderTest);
            }
        }

        public override void After(MethodInfo methodUnderTest)
        {
            if (KernelInvocationContext.Current is { } context)
            {
                Log.Error(message: "KernelInvocationContext is {context} after test {methodUnderTest}", args: new object[] { context, methodUnderTest });
            }

            if (AsyncContext.Id is { } id)
            {
                Log.Error(message: "AsyncContext.Id is {id} after test {methodUnderTest}", args: new object[] { id, methodUnderTest });
            }

            if (ConsoleOutput.RefCount != 0)
            {
                Log.Info("ConsoleOutput.RefCount is {refCount} after test {methodUnderTest}", ConsoleOutput.RefCount, methodUnderTest);
            }
        }
    }
}