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
    KernelCommandEnvelope
} from './polyglot-notebooks/commandsAndEvents';
import { clearDebounce, createOutput } from './utilities';

import * as vscodeLike from './interfaces/vscode-like';
import { CompositeKernel } from './polyglot-notebooks/compositeKernel';
import { KernelHost } from './polyglot-notebooks/kernelHost';
import { KernelCommandAndEventChannel } from './DotnetInteractiveChannel';
import * as connection from './polyglot-notebooks/connection';
import { DisposableSubscription } from './polyglot-notebooks/disposables';

export interface ErrorOutputCreator {
    (message: string, outputId?: string): vscodeLike.NotebookCellOutput;
}

export interface InteractiveClientConfiguration {
    readonly channel: KernelCommandAndEventChannel,
    readonly createErrorOutput: ErrorOutputCreator,
    readonly kernelInfos: Array<KernelInfo>
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

    execute(source: string, language: string, outputReporter: { (output: vscodeLike.NotebookCellOutput): void }, diagnosticObserver: (diags: Array<Diagnostic>) => void, configuration?: { token?: string | undefined, id?: string | undefined }): Promise<boolean> {
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
                    targetKernelName: language
                }
            );
            if (configuration !== undefined && configuration.id !== undefined) {
                command.setId(configuration.id);
            }
            const commandId = command.id;
            try {
                return this.submitCode(command, language, eventEnvelope => {
                    if (this.deferredOutput.length > 0) {
                        for (const output of this.deferredOutput) {
                            outputReporter(output);
                        }
                        this.deferredOutput = [];
                    }

                    switch (eventEnvelope.eventType) {
                        // if kernel languages were added, handle those events here
                        case CommandSucceededType:
                            if (eventEnvelope.command?.id === commandId) {
                                // only complete this promise if it's the root command
                                resolve(!failureReported);
                            }
                            break;
                        case CommandFailedType:
                            {
                                const err = <CommandFailed>eventEnvelope.event;
                                const errorOutput = this.config.createErrorOutput(err.message, this.getNextOutputId());
                                outputReporter(errorOutput);
                                failureReported = true;
                                if (eventEnvelope.command?.id === commandId) {
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

    completion(kernelName: string, code: string, line: number, character: number): Promise<CompletionsProduced> {

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
        return this.submitCommandAndGetResult<CompletionsProduced>(command, CompletionsProducedType);
    }

    hover(language: string, code: string, line: number, character: number): Promise<HoverTextProduced> {
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
        return this.submitCommandAndGetResult<HoverTextProduced>(command, HoverTextProducedType);
    }

    signatureHelp(language: string, code: string, line: number, character: number): Promise<SignatureHelpProduced> {
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
        return this.submitCommandAndGetResult<SignatureHelpProduced>(command, SignatureHelpProducedType);
    }

    async getDiagnostics(kernelName: string, code: string): Promise<Array<Diagnostic>> {
        const command = new KernelCommandEnvelope(
            RequestDiagnosticsType,
            <RequestDiagnostics>{
                code,
                targetKernelName: kernelName
            }
        );

        const diagsProduced = await this.submitCommandAndGetResult<DiagnosticsProduced>(command, DiagnosticsProducedType);
        return diagsProduced.diagnostics;
    }

    private async submitCode(command: KernelCommandEnvelope, language: string, observer: KernelEventEnvelopeObserver): Promise<DisposableSubscription> {
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

    requestValueInfos(kernelName: string): Promise<ValueInfosProduced> {
        const command = new KernelCommandEnvelope(
            RequestValueInfosType,
            <RequestValueInfos>{
                targetKernelName: kernelName,
                mimeType: "text/plain+summary"
            }
        );
        return this.submitCommandAndGetResult(command, ValueInfosProducedType);
    }

    requestValue(valueName: string, kernelName: string): Promise<ValueProduced> {
        const command = new KernelCommandEnvelope(
            RequestValueType,
            <RequestValue>{
                name: valueName,
                mimeType: 'text/plain',
                targetKernelName: kernelName,
            }
        );
        return this.submitCommandAndGetResult(command, ValueProducedType);
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

    private submitCommandAndGetResult<TEvent extends KernelEvent>(command: KernelCommandEnvelope, expectedEventType: KernelEventType): Promise<TEvent> {
        return new Promise<TEvent>(async (resolve, reject) => {
            let handled = false;
            const token = command.getOrCreateToken();
            let disposable = this.subscribeToKernelTokenEvents(token, eventEnvelope => {
                if (eventEnvelope.command?.hasSameRootCommandAs(command) && eventEnvelope.eventType === expectedEventType) {
                    switch (eventEnvelope.eventType) {
                        case CommandFailedType:
                            if (!handled) {
                                handled = true;
                                disposable.dispose();
                                let err = <CommandFailed>eventEnvelope.event;
                                reject(err);
                            }
                            break;
                        case CommandSucceededType:
                            if (!handled) {
                                handled = true;
                                disposable.dispose();
                                reject('Command was handled before reporting expected result.');
                            }
                            break;
                        default:
                            if (eventEnvelope.eventType === expectedEventType) {
                                handled = true;
                                disposable.dispose();
                                let event = <TEvent>eventEnvelope.event;
                                resolve(event);
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
            const id = command.id;
            const commandType = command.commandType;
            let disposable = this.subscribeToKernelTokenEvents(token, eventEnvelope => {
                switch (eventEnvelope.eventType) {
                    case CommandFailedType:
                        let err = <CommandFailed>eventEnvelope.event;
                        failureReported = true;
                        if (eventEnvelope.command?.id === id) {
                            disposable.dispose();
                            reject(err);
                        }
                        break;
                    case CommandSucceededType:
                        if (eventEnvelope.command?.id === id) {
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

    private subscribeToKernelTokenEvents(token: string, observer: KernelEventEnvelopeObserver): DisposableSubscription {
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
        let token = eventEnvelope.command?.getOrCreateToken();
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
