// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.DotNet.Interactive.Formatting
{
    internal interface IFormatterSet
    {
        void AddFormatterFactory(Func<Type, ITypeFormatter> getFormatter);

        bool TryGetFormatterForType(Type type, out ITypeFormatter formatter);
    }
}