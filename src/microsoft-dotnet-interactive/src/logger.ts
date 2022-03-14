// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

export enum LogLevel {
    Info = 0,
    Warn = 1,
    Error = 2,
    None = 3,
}

export type LogEntry = {
    logLevel: LogLevel;
    source: string;
    message: string;
};

export class Logger {

    private static _default: Logger = new Logger('default', (_entry: LogEntry) => { });

    private constructor(private readonly source: string, readonly write: (entry: LogEntry) => void) {
    }

    public info(message: string): void {
        this.write({ logLevel: LogLevel.Info, source: this.source, message });
    }

    public warn(message: string): void {
        this.write({ logLevel: LogLevel.Warn, source: this.source, message });
    }

    public error(message: string): void {
        this.write({ logLevel: LogLevel.Error, source: this.source, message });
    }

    public static configure(source: string, writer: (entry: LogEntry) => void) {
        const logger = new Logger(source, writer);
        Logger._default = logger;
    }

    public static get default(): Logger {
        if (Logger._default) {
            return Logger._default;
        }

        throw new Error('No logger has been configured for this context');
    }
}
