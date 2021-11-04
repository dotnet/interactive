// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Journey
{
    public static class Main
    { 
        public static Task OnLoadAsync(Kernel kernel, HttpClient httpClient = null)
        {
            Lesson.Clear();
            if (kernel is CompositeKernel compositeKernel)
            {
                Lesson.ResetChallenge();
                compositeKernel.UseProgressiveLearning(httpClient)
                    .UseProgressiveLearningMiddleware()
                    .UseModelAnswerValidation();
            }
            else
            {
                throw new ArgumentException("Not composite kernel");
            }

            if (KernelInvocationContext.Current is { } context)
            {
                context.DisplayAs("Interactive.Journey has loaded!", "text/markdown");
            }

            return Task.CompletedTask;
        }
    }
}
