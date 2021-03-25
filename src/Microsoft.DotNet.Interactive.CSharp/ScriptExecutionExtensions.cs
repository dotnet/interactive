// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Scripting;

namespace Microsoft.DotNet.Interactive.CSharp
{
    internal static class ScriptExecutionExtensions
    {
        public static async Task<ScriptState<object>> UntilCancelled(
            this Task<ScriptState<object>> source,
            CancellationToken cancellationToken)
        {
            var completed = await Task.WhenAny(
                                source,
                                Task.Run(async () =>
                                {
                                    while (!cancellationToken.IsCancellationRequested)
                                    {
                                        try
                                        {
                                            await Task.Delay(100, cancellationToken);

                                            if (source.IsCompleted)
                                            {
                                                break;
                                            }
                                        }
                                        catch (TaskCanceledException)
                                        {
                                        }
                                        catch (OperationCanceledException)
                                        {
                                        }
                                    }

                                    return (ScriptState<object>) null;
                                }, cancellationToken));

            return completed.Result;
        }
    }
}