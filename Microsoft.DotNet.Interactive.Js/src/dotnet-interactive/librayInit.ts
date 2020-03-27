// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DotnetInteractiveScopeContainer, DotnetInteractiveScope, DotnetInteractiveClient, ClientFetch } from "./dotnet-interactive-interfaces";
import { KernelClientImpl, createDotnetInteractiveClient } from "./KernelClientImpl";

export function init(global: any) {
    global.getDotnetInteractiveScope = (key: string) => {
        if (!global.interactiveScopes) {
            global.interactiveScopes = new DotnetInteractiveScopeContainer();
        }

        if (!global.interactiveScopes[key]) {
            global.interactiveScopes[key] = new DotnetInteractiveScope();
        }

        return global.interactiveScopes[key];
    }

    global.createDotnetInteractiveClient = createDotnetInteractiveClient;
}