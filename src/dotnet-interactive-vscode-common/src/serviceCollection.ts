// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as vscode from 'vscode';
import { ClientMapper } from './clientMapper';
import { NotebookWatcherService } from './notebookWatcherService';

export class ServiceCollection {
    private notebookWatcherService: NotebookWatcherService;

    static _instance: ServiceCollection;

    static get Instance(): ServiceCollection {
        if (!ServiceCollection._instance) {
            throw new Error('ServiceCollection not yet initialized');
        }

        return ServiceCollection._instance;
    }

    constructor(context: vscode.ExtensionContext, clientMapper: ClientMapper) {
        this.notebookWatcherService = new NotebookWatcherService(context, clientMapper);
        context.subscriptions.push(this);
    }

    get NotebookWatcher() {
        return this.notebookWatcherService;
    }

    static initialize(context: vscode.ExtensionContext, clientMapper: ClientMapper) {
        ServiceCollection._instance = new ServiceCollection(context, clientMapper);
    }

    dispose() {
        this.notebookWatcherService.dispose();
    }
}
