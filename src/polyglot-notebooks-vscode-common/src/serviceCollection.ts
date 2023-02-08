// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as vscode from 'vscode';
import { ClientMapper } from './clientMapper';
import { DynamicGrammarSemanticTokenProvider } from './dynamicGrammarSemanticTokenProvider';
import { KernelInfoUpdaterService } from './kernelInfoUpdaterService';
import { LanguageConfigurationManager } from './languageConfigurationManager';
import { NotebookWatcherService } from './notebookWatcherService';

export class ServiceCollection {
    private kernelInfoUpdaterService: KernelInfoUpdaterService;
    private languageConfigurationManager: LanguageConfigurationManager;
    private notebookWatcherService: NotebookWatcherService;

    static _instance: ServiceCollection;

    static get Instance(): ServiceCollection {
        if (!ServiceCollection._instance) {
            throw new Error('ServiceCollection not yet initialized');
        }

        return ServiceCollection._instance;
    }

    constructor(context: vscode.ExtensionContext, clientMapper: ClientMapper, dynamicTokensProvider: DynamicGrammarSemanticTokenProvider) {
        this.kernelInfoUpdaterService = new KernelInfoUpdaterService(clientMapper);
        this.languageConfigurationManager = new LanguageConfigurationManager(dynamicTokensProvider);
        this.notebookWatcherService = new NotebookWatcherService(context, clientMapper);
        context.subscriptions.push(this);
    }

    get KernelInfoUpdaterService() {
        return this.kernelInfoUpdaterService;
    }

    get LanguageConfigurationManager() {
        return this.languageConfigurationManager;
    }

    get NotebookWatcher() {
        return this.notebookWatcherService;
    }

    static initialize(context: vscode.ExtensionContext, clientMapper: ClientMapper, dynamicTokensProvider: DynamicGrammarSemanticTokenProvider) {
        ServiceCollection._instance = new ServiceCollection(context, clientMapper, dynamicTokensProvider);
    }

    dispose() {
        this.languageConfigurationManager.dispose();
        this.notebookWatcherService.dispose();
    }
}
