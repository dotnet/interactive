import * as cp from 'child_process';
import { Writable } from 'stream';
import { Event, ReceivedInteractiveEvent } from './interfaces';

export class InteractiveClient {
    private buffer: string = "";
    private callbacks: Map<string, {(event: Event, eventType: string): void}[]> = new Map();
    private next: number = 1;
    private stdin: Writable;

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
                    let received = <ReceivedInteractiveEvent>obj;
                    let callbacks = this.callbacks.get(received.cause.token);
                    if (callbacks) {
                        for (let callback of callbacks) {
                            callback(received.event, received.eventType);
                        }
                    }
                } catch {
                }
            }
        });

        this.stdin = childProcess.stdin;
    }

    private registerCallback(token: string, callback: {(event: Event, eventType: string): void}) {
        if (!this.callbacks.has(token)) {
            this.callbacks.set(token, []);
        }

        this.callbacks.get(token)?.push(callback);
    }

    async submitCode(code: string, callback: {(event: Event, eventType: string): void}) {
        /*
        let sampleReturnEvent = {
            eventType: "ReturnValueProduced",
            event: {
                value: 2,
                formattedValues: [
                    {
                        mimeType: "text/html",
                        value: "2"
                    }
                ],
                valueId: null
            },
            cause: {
                token: "abc",
                commandType: "SubmitCode",
                command: {
                    code: "1+1",
                    submissionType: 0,
                    targetKernelName: null
                }
            }
        };
        // */

        let token = "abc" + this.next++;
        let submit = {
            token: token,
            commandType: "SubmitCode",
            command: {
                code: code,
                submissionType: 0,
                targetKernelName: null
            }
        };

        this.registerCallback(token, callback);

        let str = JSON.stringify(submit);
        this.stdin.write(str);
        this.stdin.write("\n");
    }
}
