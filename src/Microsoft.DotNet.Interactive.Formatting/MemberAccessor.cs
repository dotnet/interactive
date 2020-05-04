// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;

namespace Microsoft.DotNet.Interactive.Formatting
{
    internal static class MemberAccessor
    {
        public static MemberAccessor<T> CreateMemberAccessor<T>(MemberInfo memberInfo)
        {
            return new MemberAccessor<T>(memberInfo);
        }
    }
}