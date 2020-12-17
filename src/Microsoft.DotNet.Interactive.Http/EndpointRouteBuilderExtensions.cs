// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.DotNet.Interactive.Http
{
    public static class EndpointRouteBuilderExtensions
    {
        private static int __AspNet_NextEndpointOrder;

        public static IEndpointConventionBuilder MapAction(
            this IEndpointRouteBuilder endpoints,
            string pattern,
            RequestDelegate requestDelegate)
        {
            var order = __AspNet_NextEndpointOrder--;
            var builder = endpoints.MapGet(pattern, requestDelegate);
            builder.Add(b => ((RouteEndpointBuilder)b).Order = order);
            return builder;
        }
    }
}
