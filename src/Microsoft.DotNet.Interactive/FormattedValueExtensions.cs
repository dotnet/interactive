// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.DotNet.Interactive
{
    public static class FormattedValueExtensions
    {
        public static (object value, Type type) ToDotNetValue(this FormattedValue formattedValue)
        {
            return (formattedValue.Value, typeof(string));
        }
    }
}