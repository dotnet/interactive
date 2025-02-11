// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import {
    CommandFailed,
    CommandFailedType,
    CommandSucceededType,
    CompletionsProduced,
    CompletionsProducedType,
    Diagnostic,
    DiagnosticsProduced,
    DiagnosticsProducedType,
    DisplayEvent,
    DisplayedValueProducedType,
    DisplayedValueUpdatedType,
    HoverTextProduced,
    HoverTextProducedType,
    KernelCommand,
    KernelCommandType,
    KernelEvent,
    KernelEventEnvelope,
    KernelEventEnvelopeObserver,
    KernelEventType,
    KernelInfoProduced,
    KernelInfoProducedType,
    Quit,
    QuitType,
    RequestCompletions,
    RequestCompletionsType,
    RequestDiagnostics,
    RequestDiagnosticsType,
    RequestHoverText,
    RequestHoverTextType,
    RequestSignatureHelp,
    RequestSignatureHelpType,
    RequestValue,
    RequestValueType,
    RequestValueInfos,
    RequestValueInfosType,
    ReturnValueProducedType,
    SignatureHelpProduced,
    SignatureHelpProducedType,
    StandardErrorValueProducedType,
    StandardOutputValueProducedType,
    ValueInfosProduced,
    ValueInfosProducedType,
    ValueProduced,
    ValueProducedType,
    SubmitCode,
    SubmitCodeType,
    CancelType,
    Cancel,
    ErrorProducedType,
    ErrorProduced,
    KernelInfo,
    KernelCommandEnvelope,
    CodeExpansionInfosProduced,
    CodeExpansionInfosProducedType,
    RequestCodeExpansionInfos,
    RequestCodeExpansionInfosType
} from './polyglot-notebooks/commandsAndEvents';
import { clearDebounce, createOutput } from './utilities';

import * as vscodeLike from './interfaces/vscode-like';
import { CompositeKernel } from './polyglot-notebooks/compositeKernel';
import { KernelHost } from './polyglot-notebooks/kernelHost';
import { KernelCommandAndEventChannel } from './DotnetInteractiveChannel';
import * as connection from './polyglot-notebooks/connection';
import { DisposableSubscription } from './polyglot-notebooks/disposables';
import { Logger } from './polyglot-notebooks';
import { NotebookCellMetadata } from './metadataUtilities';
import { NotebookCellExecution } from 'vscode';

export interface ErrorOutputCreator {
    (message: string, outputId?: string): vscodeLike.NotebookCellOutput;
}

export interface InteractiveClientConfiguration {
    readonly channel: KernelCommandAndEventChannel,
    readonly createErrorOutput: ErrorOutputCreator,
    readonly kernelInfos: Array<KernelInfo>
}

export interface NotebookCellSubmission {
    kernelName: string;
    index?: number;
}

export class InteractiveClient {
    private disposables: (() => void)[] = [];
    private nextExecutionCount = 1;
    private nextOutputId: number = 1;
    private nextToken: number = 1;
    private tokenEventObservers: Map<string, Array<KernelEventEnvelopeObserver>> = new Map<string, Array<KernelEventEnvelopeObserver>>();
    private deferredOutput: Array<vscodeLike.NotebookCellOutput> = [];
    private _kernel: CompositeKernel;
    private _kernelHost: KernelHost;
    constructor(readonly config: InteractiveClientConfiguration) {
        this._kernel = new CompositeKernel("vscode");
        this._kernelHost = new KernelHost(this._kernel, config.channel.sender, config.channel.receiver, "kernel://vscode");

        config.channel.receiver.subscribe({
            next: (envelope) => {
                if (connection.isKernelEventEnvelope(envelope)) {
                    this.eventListener(envelope);

                    if (envelope.eventType === KernelInfoProducedType) {
                        const kernelInfoProduced = <KernelInfoProduced>envelope.event;
                        connection.ensureOrUpdateProxyForKernelInfo(kernelInfoProduced.kernelInfo, this._kernel);
                    }
                }
            }
        });

        for (const kernelInfo of config.kernelInfos) {
            const remoteHostUri = connection.extractHostAndNomalize(kernelInfo.isProxy ? kernelInfo.remoteUri! : kernelInfo.uri);
            this._kernelHost.defaultConnector.addRemoteHostUri(remoteHostUri);
            connection.ensureOrUpdateProxyForKernelInfo(kernelInfo, this._kernel);
        }

        this._kernelHost.connect();
    }

    get kernel(): CompositeKernel {
        return this._kernel;
    }

    get kernelHost(): KernelHost {
        return this._kernelHost;
    }

    get channel(): KernelCommandAndEventChannel {
        return this.config.channel;
    }

    public tryGetProperty<T>(propertyName: string): T | null {
        try {
            return <T>((<any>this.config.channel)[propertyName]);
        }
        catch {
            return null;
        }
    }

    private clearExistingLanguageServiceRequests(requestId: string) {
        clearDebounce(requestId);
        clearDebounce(`completion-${requestId}`);
        clearDebounce(`diagnostics-${requestId}`);
        clearDebounce(`hover-${requestId}`);
        clearDebounce(`sighelp-${requestId}`);
    }

    execute(source: string,
        cell: NotebookCellSubmission,
        outputReporter: { (output: vscodeLike.NotebookCellOutput): void },
        diagnosticObserver: (diags: Array<Diagnostic>) => void,
        configuration?: { token?: string | undefined, id?: string | undefined }): Promise<boolean> {
        if (configuration !== undefined && configuration.id !== undefined) {
            this.clearExistingLanguageServiceRequests(configuration.id);
        }
        return new Promise((resolve, reject) => {
            let diagnostics: Array<Diagnostic> = [];

            let reportDiagnostics = () => {
                diagnosticObserver(diagnostics);
            };

            let failureReported = false;
            const command = new KernelCommandEnvelope(
                SubmitCodeType,
                <SubmitCode>{
                    code: source,
                    targetKernelName: cell.kernelName,
                    parameters: {
                        cellIndex: cell.index?.toString()
                    }
                }
            );

            const commandToken = command.getOrCreateToken();

            try {
                return this.submitCode(command, eventEnvelope => {
                    if (this.deferredOutput.length > 0) {
                        for (const output of this.deferredOutput) {
                            outputReporter(output);
                        }
                        this.deferredOutput = [];
                    }

                    switch (eventEnvelope.eventType) {
                        // if kernel languages were added, handle those events here
                        case CommandSucceededType:
                            if (eventEnvelope.command?.getToken() === commandToken) {
                                // only complete this promise if it's the root command
                                resolve(!failureReported);
                            }
                            break;
                        case CommandFailedType:
                            {
                                if (eventEnvelope.command?.getToken() === commandToken) {
                                    const err = <CommandFailed>eventEnvelope.event;
                                    const errorOutput = this.config.createErrorOutput(err.message, this.getNextOutputId());
                                    outputReporter(errorOutput);
                                    failureReported = true;

                                    // only complete this promise if it's the root command
                                    reject(err);
                                }
                            }
                            break;
                        case ErrorProducedType: {
                            const err = <ErrorProduced>eventEnvelope.event;
                            const errorOutput = this.config.createErrorOutput(err.message, this.getNextOutputId());
                            outputReporter(errorOutput);
                            failureReported = true;
                        }
                        case DiagnosticsProducedType:
                            {
                                const diags = <DiagnosticsProduced>eventEnvelope.event;
                                diagnostics.push(...(diags.diagnostics ?? []));
                                reportDiagnostics();
                            }
                            break;
                        case StandardErrorValueProducedType:
                        case StandardOutputValueProducedType:
                            {
                                const disp = <DisplayEvent>eventEnvelope.event;
                                const stream = eventEnvelope.eventType === StandardErrorValueProducedType ? 'stderr' : 'stdout';
                                const output = this.displayEventToCellOutput(disp, stream);
                                if (output) {
                                    outputReporter(output);
                                }
                            }
                            break;
                        case DisplayedValueProducedType:
                        case DisplayedValueUpdatedType:
                        case ReturnValueProducedType:
                            {
                                const disp = <DisplayEvent>eventEnvelope.event;
                                const output = this.displayEventToCellOutput(disp);
                                if (output) {
                                    outputReporter(output);
                                }
                            }
                            break;
                    }
                }).catch(e => {
                    // only report a failure if it's not a `CommandFailed` event from above (which has already called `reject()`)
                    if (!failureReported) {
                        const errorMessage = typeof e?.message === 'string' ? <string>e.message : '' + e;
                        const errorOutput = this.config.createErrorOutput(errorMessage, this.getNextOutputId());
                        outputReporter(errorOutput);
                        reject(e);
                    }
                });
            }
            catch (e) {
                reject(e);
            }
        });
    }

    async completion(kernelName: string, code: string, line: number, character: number): Promise<CompletionsProduced> {
        const command = new KernelCommandEnvelope(
            RequestCompletionsType,
            <RequestCompletions>{
                code: code,
                linePosition: {
                    line,
                    character
                },
                targetKernelName: kernelName
            }
        );
        let result = await this.submitCommandAndGetResult<CompletionsProduced>(command, CompletionsProducedType, true);
        if (result === undefined) {
            result = {
                completions: []
            };
        }
        return result!;
    }

    async hover(language: string, code: string, line: number, character: number): Promise<HoverTextProduced> {
        const command = new KernelCommandEnvelope(
            RequestHoverTextType,
            <RequestHoverText>{
                code: code,
                linePosition: {
                    line: line,
                    character: character,
                },
                targetKernelName: language
            }
        );
        let result = await this.submitCommandAndGetResult<HoverTextProduced>(command, HoverTextProducedType, true);
        if (result === undefined) {
            result = {
                content: []
            };
        }
        return result!;
    }

    async signatureHelp(language: string, code: string, line: number, character: number): Promise<SignatureHelpProduced> {
        const command = new KernelCommandEnvelope(
            RequestSignatureHelpType,
            <RequestSignatureHelp>{
                code,
                linePosition: {
                    line,
                    character
                },
                targetKernelName: language
            }
        );
        let result = await this.submitCommandAndGetResult<SignatureHelpProduced>(command, SignatureHelpProducedType, true);
        if (result === undefined) {
            result = {
                activeParameterIndex: 0,
                activeSignatureIndex: 0,
                signatures: []
            };
        }
        return result!;
    }

    async getDiagnostics(kernelName: string, code: string): Promise<Array<Diagnostic>> {
        const command = new KernelCommandEnvelope(
            RequestDiagnosticsType,
            <RequestDiagnostics>{
                code,
                targetKernelName: kernelName
            }
        );

        let diagsProduced = await this.submitCommandAndGetResult<DiagnosticsProduced>(command, DiagnosticsProducedType, true);
        if (diagsProduced === undefined) {
            return [];
        }
        return diagsProduced.diagnostics;
    }

    private async submitCode(
        command: KernelCommandEnvelope,
        observer: KernelEventEnvelopeObserver): Promise<DisposableSubscription> {
        if (command.commandType !== SubmitCodeType) {
            throw new Error(`Commandm ust be SubmitCpde.`);
        }
        let disposable = this.subscribeToKernelTokenEvents(command.getOrCreateToken(), observer);
        try {
            await this.submitCommand(command);
        }
        catch (error) {
            return Promise.reject(error);

        }
        return disposable;
    }

    async requestValueInfos(kernelName: string): Promise<ValueInfosProduced> {
        const command = new KernelCommandEnvelope(
            RequestValueInfosType,
            <RequestValueInfos>{
                targetKernelName: kernelName,
                mimeType: "text/plain+summary"
            }
        );
        const result = await (this.submitCommandAndGetResult<ValueInfosProduced>(command, ValueInfosProducedType));
        return result!;
    }

    async requestCodeExpansionInfos(): Promise<CodeExpansionInfosProduced> {
        const command = new KernelCommandEnvelope(
            RequestCodeExpansionInfosType,
            <RequestCodeExpansionInfos>{
                targetKernelName: ".NET", // FIX look up actual root kernel
            }
        );
        const result = await (this.submitCommandAndGetResult<CodeExpansionInfosProduced>(command, CodeExpansionInfosProducedType));
        return result!;
    }

    async requestValue(valueName: string, kernelName: string): Promise<ValueProduced> {
        const command = new KernelCommandEnvelope(
            RequestValueType,
            <RequestValue>{
                name: valueName,
                mimeType: 'text/plain',
                targetKernelName: kernelName,
            }
        );
        const result = await this.submitCommandAndGetResult<ValueProduced>(command, ValueProducedType);
        return result!;
    }

    cancel(): Promise<void> {
        const command = new KernelCommandEnvelope(
            CancelType,
            <Cancel>{}
        );
        return this.submitCommand(command);
    }

    dispose() {
        this.config.channel.sender.send(new KernelCommandEnvelope(
            QuitType,
            <Quit>{},
        ));
        this.config.channel.dispose();
        for (let disposable of this.disposables) {
            disposable();
        }
    }

    public registerForDisposal(disposable: () => void) {
        this.disposables.push(disposable);
    }

    private submitCommandAndGetResult<TEvent extends KernelEvent>(command: KernelCommandEnvelope, expectedEventType: KernelEventType, eventIsOptional = false): Promise<TEvent | undefined> {
        return new Promise<TEvent | undefined>(async (resolve, reject) => {
            let handled = false;
            const token = command.getOrCreateToken();
            const rootToken = KernelCommandEnvelope.getRootToken(token);
            let disposable = this.subscribeToKernelTokenEvents(rootToken, eventEnvelope => {
                if (eventEnvelope.command?.hasSameRootCommandAs(command)) {

                    let isRootCommand = eventEnvelope.command?.getToken() === rootToken;

                    switch (eventEnvelope.eventType) {
                        case CommandFailedType:
                            if (!handled && isRootCommand) {
                                handled = true;
                                let err = <CommandFailed>eventEnvelope.event;
                                reject(err);
                                disposable.dispose();
                            }
                            break;
                        case CommandSucceededType:
                            if (!handled && isRootCommand) {
                                handled = true;
                                if (eventIsOptional) {
                                    resolve(undefined);
                                } else {
                                    reject('Command was handled before reporting expected result.');
                                }
                                disposable.dispose();
                            }
                            break;
                        default:
                            if (eventEnvelope.eventType === expectedEventType) {
                                handled = true;
                                let event = <TEvent>eventEnvelope.event;
                                resolve(event);
                                disposable.dispose();
                            }
                            break;
                    }
                }
            });
            await this.config.channel.sender.send(command);
        });
    }

    private submitCommand(command: KernelCommandEnvelope): Promise<void> {
        return new Promise<void>((resolve, reject) => {
            let failureReported = false;
            const token = command.getOrCreateToken();

            let disposable = this.subscribeToKernelTokenEvents(token, eventEnvelope => {
                switch (eventEnvelope.eventType) {
                    case CommandFailedType:
                        let err = <CommandFailed>eventEnvelope.event;
                        failureReported = true;
                        if (eventEnvelope.command?.getToken() === token) {
                            disposable.dispose();
                            reject(err);
                        }
                        break;
                    case CommandSucceededType:
                        if (eventEnvelope.command?.getToken() === token) {
                            disposable.dispose();
                            resolve();
                        }
                        break;
                    default:
                        break;
                }
            });
            try {
                this.config.channel.sender
                    .send(command)
                    .catch(e => {
                        // only report a failure if it's not a `CommandFailed` event from above (which has already called `reject()`)
                        if (!failureReported) {
                            reject(e);
                        }
                    });
            } catch (error) {
                reject(error);
            }
        });
    }

    private subscribeToKernelTokenEvents(commandToken: string, observer: KernelEventEnvelopeObserver): DisposableSubscription {
        const token = KernelCommandEnvelope.getRootToken(commandToken);

        if (!this.tokenEventObservers.get(token)) {
            this.tokenEventObservers.set(token, []);
        }

        this.tokenEventObservers.get(token)?.push(observer);
        return {
            dispose: () => {
                let listeners = this.tokenEventObservers.get(token);
                if (listeners) {
                    let i = listeners.indexOf(observer);
                    if (i >= 0) {
                        listeners.splice(i, 1);
                    }

                    if (listeners.length === 0) {
                        this.tokenEventObservers.delete(token);
                    }
                }
            }
        };
    }

    private eventListener(eventEnvelope: KernelEventEnvelope) {
        let token = eventEnvelope.command?.getToken();
        if (token) {
            if (token.startsWith("deferredCommand::")) {
                switch (eventEnvelope.eventType) {
                    case DisplayedValueProducedType:
                    case DisplayedValueUpdatedType:
                    case ReturnValueProducedType:
                        let disp = <DisplayEvent>eventEnvelope.event;
                        let output = this.displayEventToCellOutput(disp);
                        if (output) {
                            this.deferredOutput.push(output);
                        }
                        break;
                }
            } else {
                const tokenParts = token.split('.');
                for (let i = tokenParts.length; i >= 1; i--) {
                    const candidateToken = tokenParts.slice(0, i).join('.');
                    let listeners = this.tokenEventObservers.get(candidateToken);
                    if (listeners) {
                        for (let listener of listeners) {
                            listener(eventEnvelope);
                        }
                    }
                }
            }
        }
    }

    private displayEventToCellOutput(disp: DisplayEvent, stream?: 'stdout' | 'stderr'): vscodeLike.NotebookCellOutput | null {
        const encoder = new TextEncoder();
        const outputItems: Array<vscodeLike.NotebookCellOutputItem> = [];
        if (disp.formattedValues && disp.formattedValues.length > 0) {
            for (let formatted of disp.formattedValues) {
                if (!formatted.suppressDisplay) {
                    let data = this.IsEncodedMimeType(formatted.mimeType)
                        ? Buffer.from(formatted.value, 'base64')
                        : encoder.encode(formatted.value);
                    const outputItem: vscodeLike.NotebookCellOutputItem = {
                        mime: formatted.mimeType,
                        data
                    };
                    if (stream) {
                        outputItem.stream = stream;
                    }
                    outputItems.push(outputItem);
                }
            }
        }

        if (outputItems.length === 0) {
            return null;
        } else {
            const outputId = disp.valueId ?? this.getNextOutputId();
            const output = createOutput(outputItems, outputId);
            return output;
        }
    }

    private IsEncodedMimeType(mimeType: string): boolean {
        const encdodedMimetypes = new Set<string>(["image/png", "image/jpeg", "image/gif"]);
        return encdodedMimetypes.has(mimeType);
    }

    resetExecutionCount() {
        this.nextExecutionCount = 1;
    }

    getNextExecutionCount(): number {
        const next = this.nextExecutionCount;
        this.nextExecutionCount++;
        return next;
    }

    private getNextOutputId(): string {
        return (this.nextOutputId++).toString();
    }
}
