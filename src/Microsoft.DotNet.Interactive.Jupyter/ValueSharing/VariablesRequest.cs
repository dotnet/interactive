// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Interactive.Jupyter.ValueSharing;

[ValueAdapterMessageType(ValueAdapterMessageType.Request)]
[ValueAdapterCommand(ValueAdapterCommandTypes.Variables)]
public class VariablesRequest : ValueAdapterRequest<IValueAdapterRequestArguments>
{
    public VariablesRequest(): base(null)
    {
    }
}
