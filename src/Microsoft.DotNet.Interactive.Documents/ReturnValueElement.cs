// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.DotNet.Interactive.Documents;

public class ReturnValueElement : InteractiveDocumentOutputElement, IDataElement
{
    public IDictionary<string, object> Data { get; } = new Dictionary<string, object>();

    public int ExecutionOrder { get; set; }

    public IDictionary<string, object> Metadata { get; } = new Dictionary<string, object>();
}