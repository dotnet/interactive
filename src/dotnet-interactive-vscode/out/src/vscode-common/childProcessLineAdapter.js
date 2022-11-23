"use strict";
// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
Object.defineProperty(exports, "__esModule", { value: true });
exports.ChildProcessLineAdapter = void 0;
const cp = require("child_process");
const lineReader_1 = require("./lineReader");
// `ChildProcessLineAdapter` spawns a new process and allows a subscription for when lines are read from STDOUT and allows lines to be written to STDIN.
class ChildProcessLineAdapter {
    constructor(command, args, workingDirectory, keepAlive, outputChannel) {
        this._lineReader = new lineReader_1.LineReader();
        function spawnProcess(lineReader, respawnCallback) {
            const process = cp.spawn(command, args, { cwd: workingDirectory });
            outputChannel.appendLine(`Started process ${process.pid}: ${command} ${args.join(' ')}`);
            process.stdout.on('data', (data) => lineReader.onData(data));
            process.stderr.on('data', (data) => outputChannel.appendLine(`process ${process.pid} stderr: ${data.toString('utf-8')}`));
            process.on('exit', (code, signal) => {
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
    subscribeToLines(callback) {
        this._lineReader.subscribe(callback);
    }
    writeLine(line) {
        this._cp.stdin.write(line + '\n');
    }
    dispose() {
        this._cp.kill();
    }
}
exports.ChildProcessLineAdapter = ChildProcessLineAdapter;
//# sourceMappingURL=childProcessLineAdapter.js.map