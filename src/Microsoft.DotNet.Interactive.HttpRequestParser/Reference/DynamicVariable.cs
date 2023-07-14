﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.RegularExpressions;

namespace Microsoft.DotNet.Interactive.HttpRequest.Reference;

internal record DynamicVariable(Regex Expression, Func<HttpDocumentSnapshot, string, MatchCollection, string> InvokeHandler);