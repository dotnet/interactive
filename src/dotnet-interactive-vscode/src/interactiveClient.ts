import { KernelEventEnvelope, KernelTransport, RequestCompletion, RequestCompletionType, RequestHoverText, RequestHoverTextType, SubmissionType, SubmitCode, SubmitCodeType, KernelEventEvelopeObserver, CompletionRequestCompleted, DisposableSubscription, CompletionRequestCompletedType, CommandHandledType, CommandFailedType, HoverTextProduced, HoverTextProducedType } from './contracts';

export class InteractiveClient {
    private nextToken: number = 1;
    private tokenEventObservers: Map<string, Array<KernelEventEvelopeObserver>> = new Map<string, Array<KernelEventEvelopeObserver>>();

    constructor(readonly kernelTransport: KernelTransport) {
        kernelTransport.subscribeToKernelEvents(eventEnvelope => this.eventListener(eventEnvelope));
    }

    completion(language: string, code: string, line: number, character: number, token?: string | undefined): Promise<CompletionRequestCompleted> {
        return new Promise<CompletionRequestCompleted>(async (resolve, reject) => {
            let command: RequestCompletion = {
                code: code,
                position: {
                    line,
                    character
                },
                targetKernelName: language
            };
            token = token || this.getNextToken();
            let handled = false;
            let disposable = this.subscribeToKernelTokenEvents(token, eventEnvelope => {
                switch (eventEnvelope.eventType) {
                    case CompletionRequestCompletedType:
                        handled = true;
                        disposable.dispose();
                        let completion = <CompletionRequestCompleted>eventEnvelope.event;
                        resolve(completion);
                        break;
                    case CommandHandledType:
                    case CommandFailedType:
                        if (!handled) {
                            handled = true;
                            disposable.dispose();
                            reject();
                        }
                }
            });
            await this.kernelTransport.submitCommand(command, RequestCompletionType, token);
        });
    }

    hover(language: string, code: string, line: number, character: number, token?: string | undefined): Promise<HoverTextProduced> {
        return new Promise<HoverTextProduced>(async (resolve, reject) => {
            let command: RequestHoverText = {
                code: code,
                position: {
                    line: line,
                    character: character,
                },
                targetKernelName: language
            };
            token = token || this.getNextToken();
            let handled = false;
            let disposable = this.subscribeToKernelTokenEvents(token, eventEnvelope => {
                switch (eventEnvelope.eventType) {
                    case HoverTextProducedType:
                        handled = true;
                        disposable.dispose();
                        let hoverText = <HoverTextProduced>eventEnvelope.event;
                        resolve(hoverText);
                        break;
                    case CommandHandledType:
                    case CommandFailedType:
                        if (!handled) {
                            handled = true;
                            disposable.dispose();
                            reject();
                        }
                }
            });
            await this.kernelTransport.submitCommand(command, RequestHoverTextType, token);
        });
    }

    async submitCode(language: string, code: string, observer: KernelEventEvelopeObserver, token?: string | undefined): Promise<DisposableSubscription> {
        let command: SubmitCode = {
            code: code,
            submissionType: SubmissionType.Run,
            targetKernelName: language
        };
        token = token || this.getNextToken();
        let disposable = this.subscribeToKernelTokenEvents(token, observer);
        await this.kernelTransport.submitCommand(command, SubmitCodeType, token);
        return disposable;
    }

    private subscribeToKernelTokenEvents(token: string, observer: KernelEventEvelopeObserver): DisposableSubscription {
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
        let token = eventEnvelope.command?.token;
        if (token) {
            let listeners = this.tokenEventObservers.get(token);
            if (listeners) {
                for (let listener of listeners) {
                    listener(eventEnvelope);
                }
            }
        }
    }

    private getNextToken(): string {
        return (this.nextToken++).toString();
    }
}
