import * as cp from 'child_process';
import { DisposableSubscription, KernelCommand, KernelCommandType, KernelEventEnvelope, KernelEventEnvelopeObserver } from "./contracts";
import { ProcessStart } from './interfaces';

export class StdioKernelTransport {
    private buffer: string = '';
    private childProcess: cp.ChildProcessWithoutNullStreams;
    private subscribers: Array<KernelEventEnvelopeObserver> = [];

    constructor(processStart: ProcessStart) {
        this.childProcess = cp.spawn(processStart.command, processStart.args, { cwd: processStart.workingDirectory });
        this.childProcess.on('exit', (_code: number, _signal: string) => {
            //
            let x = 1;
        });
        this.childProcess.stdout.on('data', (data) => {
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
            this.childProcess.stdin.write(str);
            this.childProcess.stdin.write('\n');
            resolve();
        });
    }

    dispose() {
        this.childProcess.kill();
    }
}
