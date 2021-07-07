// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive
{
    public interface IInteractiveHost
    {
        /// <summary>
        /// Gets input from the user.
        /// </summary>
        /// <param name="prompt">The prompt to show.</param>
        /// <param name="isPassword">Whether the input should be treated as a password.</param>
        /// <returns>The value.</returns>
        Task<string> GetInputAsync(string prompt = "", bool isPassword = false, CancellationToken cancellationToken = default);
    }
}
