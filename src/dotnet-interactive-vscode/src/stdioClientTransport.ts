import * as cp from 'child_process';
import { Observable, Subscriber } from "rxjs";
import { Writable } from 'stream';
import { KernelCommand, KernelCommandType, KernelEventEnvelope } from "./contracts";

export class StdioClientTransport {
    private buffer: string = '';
    private next: number = 1;
    private stdin: Writable;
    private subscribers: Map<string, Array<Subscriber<KernelEventEnvelope>>> = new Map();

    constructor() {
        let childProcess = cp.spawn('dotnet', ['interactive', 'stdio']);
        childProcess.on('exit', (code: number, _signal: string) => {
            //
            let x = 1;
        });
        childProcess.stdout.on('data', (data) => {
            let str: string = data.toString();
            this.buffer += str;

            let i = this.buffer.indexOf('\n');
            while (i >= 0) {
                let temp = this.buffer.substr(0, i + 1);
                this.buffer = this.buffer.substr(i + 1);
                i = this.buffer.indexOf('\n');
                let obj = JSON.parse(temp);
                try {
                    let envelope = <KernelEventEnvelope>obj;
                    let token = envelope.command?.token;
                    if (token) {
                        let subscribers = this.subscribers.get(token);
                        if (subscribers) {
                            for (let i = subscribers.length - 1; i >= 0; i--) {
                                subscribers[i].next(envelope);

                                // TODO: is this correct?
                                if (envelope.eventType === 'CommandHandled' || envelope.eventType === 'CommandFailed') {
                                    subscribers[i].complete();
                                    delete subscribers[i];
                                }
                            }
                        }
                    }
                } catch {
                    let x = 1;
                }
            }
        });

        this.stdin = childProcess.stdin;
    }

    private registerSubscriber(token: string, subscriber: Subscriber<KernelEventEnvelope>) {
        if (!this.subscribers.has(token)) {
            this.subscribers.set(token, []);
        }

        this.subscribers.get(token)?.push(subscriber);
    }

    submitCommand(commandType: KernelCommandType, command: KernelCommand, targetKernelName: string | undefined): Observable<KernelEventEnvelope> {
        return new Observable<KernelEventEnvelope>(subscriber => {
            let token = 'abc' + this.next++;
            if (targetKernelName) {
                command.targetKernelName = targetKernelName;
            }
            let submit = {
                token: token,
                commandType: commandType,
                command: command
            };

            this.registerSubscriber(token, subscriber);

            let str = JSON.stringify(submit);
            this.stdin.write(str);
            this.stdin.write('\n');
        });
    }
}
