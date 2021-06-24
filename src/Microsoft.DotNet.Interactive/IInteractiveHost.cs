// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive
{
    public interface IInteractiveHost
    {
        /// <summary>
        /// Add a cell to the end notebook.
        /// </summary>
        /// <param name="language">The language of the new cell.</param>
        /// <param name="contents">The contents of the new cell.</param>
        Task AddCellAsync(string language, string contents, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets input from the user.
        /// </summary>
        /// <param name="prompt">The prompt to show.</param>
        /// <param name="isPassword">Whether the input should be treated as a password.</param>
        /// <returns>The value.</returns>
        Task<string> GetInputAsync(string prompt = "", bool isPassword = false, CancellationToken cancellationToken = default);
    }
}
