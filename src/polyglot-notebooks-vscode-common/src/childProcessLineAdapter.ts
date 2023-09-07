// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as cp from 'child_process';
import { ReportChannel } from './interfaces/vscode-like';
import { LineReader } from './lineReader';

// A `LineAdapter` allows lines to be submitted for writing as well as allows for callbacks for when a line is read.
export interface LineAdapter {
    subscribeToLines(callback: (line: string) => void): void;
    writeLine(line: string): void;
}

// `ChildProcessLineAdapter` spawns a new process and allows a subscription for when lines are read from STDOUT and allows lines to be written to STDIN.
export class ChildProcessLineAdapter implements LineAdapter {
    private _lineReader: LineReader;
    private _cp: cp.ChildProcessWithoutNullStreams;

    constructor(command: string, args: string[], workingDirectory: string, keepAlive: boolean, outputChannel: ReportChannel) {
        this._lineReader = new LineReader();

        function spawnProcess(lineReader: LineReader, respawnCallback?: (process: cp.ChildProcessWithoutNullStreams) => void): cp.ChildProcessWithoutNullStreams {
            const process = cp.spawn(command, args, { cwd: workingDirectory });
            outputChannel.appendLine(`Started process ${process.pid}: ${command} ${args.join(' ')}`);
            process.stdout.on('data', (data: Buffer) => lineReader.onData(data));
            process.stderr.on('data', (data: Buffer) => outputChannel.appendLine(`process ${process.pid} stderr: ${data.toString('utf-8')}`));
            process.on('exit', (code: number, signal: string) => {
                outputChannel.appendLine(`Process '${command}' with PID ${process.pid} exited with code ${code} and signal ${signal}`);
                if (keepAlive && respawnCallback) {
                    const reSpawned = spawnProcess(lineReader, respawnCallback);
                    respawnCallback(reSpawned);
                }
            });
            return process;
        }

        this._cp = spawnProcess(this._lineReader, respawned => { this._cp = respawned; });
    }

    subscribeToLines(callback: (line: string) => void) {
        this._lineReader.subscribe(callback);
    }

    writeLine(line: string) {
        this._cp.stdin.write(line + '\n');
    }

    dispose() {
        this._cp.kill();
    }
}
