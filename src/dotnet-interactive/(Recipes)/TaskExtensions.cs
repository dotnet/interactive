// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

namespace Recipes
{
    internal static class TaskExtensions
    {
        /// <summary>
        /// Provides a way to specify the intention to fire and forget a task and suppress the compiler warning that results from unawaited tasks.
        /// </summary>
        internal static void DontAwait(this Task task)
        {
        }

        /// <summary>
        /// Throws TimeoutException if the source task does not complete within the specified time.
        /// </summary>
        /// <param name="source">The task being given a limited amount of time to complete.</param>
        /// <param name="timeout">The amount of time before a TimeoutException will be thrown.</param>
        public static async Task Timeout(
            this Task source,
            TimeSpan timeout)
        {
            if (await Task.WhenAny(
                    source,
                    Task.Delay(timeout)) != source)
            {
                throw new TimeoutException();
            }
        }
    }
}