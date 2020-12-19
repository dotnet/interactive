// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.DotNet.Interactive.AspNetCore
{
    public static class InteractiveEndpointRouteBuilderExtensions
    {
        private static int _lastOrder;

        public static IEndpointConventionBuilder MapInteractive(
            this IEndpointRouteBuilder endpoints,
            string pattern,
            RequestDelegate requestDelegate)
        {
            var order = _lastOrder--;
            var builder = endpoints.Map(pattern, requestDelegate);
            builder.Add(b => ((RouteEndpointBuilder)b).Order = order);
            return builder;
        }
    }
}
