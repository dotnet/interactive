// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Interactive.FSharp

open Microsoft.DotNet.Interactive
open Microsoft.AspNetCore.Html

module FSharpKernelHelpers =
    type internal IMarker = interface end
    let display (value: obj) =
        Kernel.display value
    let HTML (value: string) =
        HtmlString value
    let Javascript (content: string) =
        Kernel.Javascript content
