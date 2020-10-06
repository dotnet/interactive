// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DotnetInteractiveScopeContainer, DotnetInteractiveScope } from "./dotnet-interactive-interfaces";
import { createDotnetInteractiveClient } from "./kernel-client-impl";

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

    global.configureRequire = (config: any) =>{        
        return (<any>require).config(config) || require;
    }

    global.createDotnetInteractiveClient = createDotnetInteractiveClient;
}