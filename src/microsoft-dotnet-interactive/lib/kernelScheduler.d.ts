export declare class KernelScheduler<T> {
    private operationQueue;
    private inFlightOperation?;
    constructor();
    runAsync(value: T, executor: (value: T) => Promise<void>): Promise<void>;
    private executeNextCommand;
}
