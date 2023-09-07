// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

export class LineReader {
    private current: string = '';
    private subscribers: Array<(line: string) => void> = [];

    subscribe(listener: (line: string) => void) {
        this.subscribers.push(listener);
    }

    onData(data: Buffer) {
        let str = data.toString('utf-8');
        this.current += str;

        let i = this.current.indexOf('\n');
        while (i >= 0) {
            let line = this.current.substr(0, i);
            if (line.endsWith('\r')) {
                line = line.substr(0, line.length - 1);
            }

            this.current = this.current.substr(i + 1);
            for (let listener of this.subscribers) {
                try {
                    listener(line);
                } catch {
                }
            }

            i = this.current.indexOf('\n');
        }
    }
}
