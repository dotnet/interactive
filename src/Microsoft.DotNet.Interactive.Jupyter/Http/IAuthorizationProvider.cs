// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Jupyter.Http;

internal interface IAuthorizationProvider
{
    string GetToken();

    Task<string> GetTokenAsync();

    string AuthScheme { get; }
}
