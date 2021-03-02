// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

declare module 'vscode' {
    // A type by this name is required to implement `DotNetInteractiveNotebookContentProvider` against VS Code stable.
    // It's not used by insiders, so we declare it here to make the compiler happy.
    export interface NotebookDocumentEditEvent {
    }
}
