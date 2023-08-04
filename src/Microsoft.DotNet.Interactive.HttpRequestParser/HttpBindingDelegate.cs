// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable
namespace Microsoft.DotNet.Interactive.HttpRequest;

internal delegate HttpBindingResult<object?> HttpBindingDelegate(HttpExpressionNode node);