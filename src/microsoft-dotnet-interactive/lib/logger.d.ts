export declare enum LogLevel {
    Info = 0,
    Warn = 1,
    Error = 2,
    None = 3
}
export declare type LogEntry = {
    logLevel: LogLevel;
    source: string;
    message: string;
};
export declare class Logger {
    private readonly source;
    readonly write: (entry: LogEntry) => void;
    private static _default;
    private constructor();
    info(message: string): void;
    warn(message: string): void;
    error(message: string): void;
    static configure(source: string, writer: (entry: LogEntry) => void): void;
    static get default(): Logger;
}
