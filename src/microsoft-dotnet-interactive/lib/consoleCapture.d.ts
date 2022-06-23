/// <reference types="node" />
import { InspectOptions } from "util";
import * as contracts from "./contracts";
import { KernelInvocationContext } from "./kernelInvocationContext";
export declare class ConsoleCapture implements contracts.Disposable {
    private kernelInvocationContext;
    private originalConsole;
    constructor(kernelInvocationContext: KernelInvocationContext);
    assert(value: any, message?: string, ...optionalParams: any[]): void;
    clear(): void;
    count(label?: any): void;
    countReset(label?: string): void;
    debug(message?: any, ...optionalParams: any[]): void;
    dir(obj: any, options?: InspectOptions): void;
    dirxml(...data: any[]): void;
    error(message?: any, ...optionalParams: any[]): void;
    group(...label: any[]): void;
    groupCollapsed(...label: any[]): void;
    groupEnd(): void;
    info(message?: any, ...optionalParams: any[]): void;
    log(message?: any, ...optionalParams: any[]): void;
    table(tabularData: any, properties?: string[]): void;
    time(label?: string): void;
    timeEnd(label?: string): void;
    timeLog(label?: string, ...data: any[]): void;
    timeStamp(label?: string): void;
    trace(message?: any, ...optionalParams: any[]): void;
    warn(message?: any, ...optionalParams: any[]): void;
    profile(label?: string): void;
    profileEnd(label?: string): void;
    dispose(): void;
    private redirectAndPublish;
    private publishArgsAsEvents;
}
