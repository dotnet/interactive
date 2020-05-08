import * as cp from 'child_process';
import { Writable } from 'stream';
import { DisposableSubscription, KernelCommand, KernelCommandType, KernelEventEnvelope, KernelEventEnvelopeObserver } from "./contracts";

export class StdioKernelTransport {
    private buffer: string = '';
    private nextToken: number = 1;
    private stdin: Writable;
    private subscribers: Array<KernelEventEnvelopeObserver> = [];

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
                    for (let i = this.subscribers.length - 1; i >= 0; i--) {
                        this.subscribers[i](envelope);
                    }
                } catch {
                }
            }
        });

        this.stdin = childProcess.stdin;
    }

    subscribeToKernelEvents(observer: KernelEventEnvelopeObserver): DisposableSubscription {
        this.subscribers.push(observer);
        return {
            dispose: () => {
                let i = this.subscribers.indexOf(observer);
                if (i >= 0) {
                    this.subscribers.splice(i, 1);
                }
            }
        };
    }

    submitCommand(command: KernelCommand, commandType: KernelCommandType, token: string): Promise<void> {
        return new Promise((resolve, reject) => {
            let submit = {
                token,
                commandType,
                command
            };

            let str = JSON.stringify(submit);
            this.stdin.write(str);
            this.stdin.write('\n');
            resolve();
        });
    }
}
