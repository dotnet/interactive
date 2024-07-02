// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Parsing;

internal delegate Task<DirectiveBindingResult<object?>> DirectiveBindingDelegate(DirectiveExpressionNode expressionNode);