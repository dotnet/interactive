// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as util from "util";
import * as contracts from "./commandsAndEvents";
import { KernelInvocationContext } from "./kernelInvocationContext";
import * as disposables from "./disposables";

export class ConsoleCapture implements disposables.Disposable {
    private originalConsole: Console;
    private _kernelInvocationContext: KernelInvocationContext | undefined;

    constructor() {
        this.originalConsole = console;
        console = <Console><any>this;
    }

    set kernelInvocationContext(value: KernelInvocationContext | undefined) {
        this._kernelInvocationContext = value;
    }

    assert(value: any, message?: string, ...optionalParams: any[]): void {
        this.originalConsole.assert(value, message, optionalParams);
    }
    clear(): void {
        this.originalConsole.clear();
    }
    count(label?: any): void {
        this.originalConsole.count(label);
    }
    countReset(label?: string): void {
        this.originalConsole.countReset(label);
    }
    debug(message?: any, ...optionalParams: any[]): void {
        this.originalConsole.debug(message, optionalParams);
    }
    dir(obj: any, options?: util.InspectOptions): void {
        this.originalConsole.dir(obj, options);
    }
    dirxml(...data: any[]): void {
        this.originalConsole.dirxml(data);
    }
    error(message?: any, ...optionalParams: any[]): void {
        this.redirectAndPublish(this.originalConsole.error, ...[message, ...optionalParams]);
    }

    group(...label: any[]): void {
        this.originalConsole.group(label);
    }
    groupCollapsed(...label: any[]): void {
        this.originalConsole.groupCollapsed(label);
    }
    groupEnd(): void {
        this.originalConsole.groupEnd();
    }
    info(message?: any, ...optionalParams: any[]): void {
        this.redirectAndPublish(this.originalConsole.info, ...[message, ...optionalParams]);
    }
    log(message?: any, ...optionalParams: any[]): void {
        this.redirectAndPublish(this.originalConsole.log, ...[message, ...optionalParams]);
    }

    table(tabularData: any, properties?: string[]): void {
        this.originalConsole.table(tabularData, properties);
    }
    time(label?: string): void {
        this.originalConsole.time(label);
    }
    timeEnd(label?: string): void {
        this.originalConsole.timeEnd(label);
    }
    timeLog(label?: string, ...data: any[]): void {
        this.originalConsole.timeLog(label, data);
    }
    timeStamp(label?: string): void {
        this.originalConsole.timeStamp(label);
    }
    trace(message?: any, ...optionalParams: any[]): void {
        this.redirectAndPublish(this.originalConsole.trace, ...[message, ...optionalParams]);
    }
    warn(message?: any, ...optionalParams: any[]): void {
        this.originalConsole.warn(message, optionalParams);
    }

    profile(label?: string): void {
        this.originalConsole.profile(label);
    }
    profileEnd(label?: string): void {
        this.originalConsole.profileEnd(label);
    }

    dispose(): void {
        console = this.originalConsole;
    }

    private redirectAndPublish(target: (...args: any[]) => void, ...args: any[]) {
        if (this._kernelInvocationContext) {
            for (const arg of args) {
                let mimeType: string;
                let value: string;
                if (typeof arg !== 'object' && !Array.isArray(arg)) {
                    mimeType = 'text/plain';
                    value = arg?.toString();
                } else {
                    mimeType = 'application/json';
                    value = JSON.stringify(arg);
                }

                const displayedValue: contracts.DisplayedValueProduced = {
                    formattedValues: [
                        {
                            mimeType,
                            value,
                        }
                    ]
                };
                const eventEnvelope: contracts.KernelEventEnvelope = {
                    eventType: contracts.DisplayedValueProducedType,
                    event: displayedValue,
                    command: this._kernelInvocationContext.commandEnvelope
                };

                this._kernelInvocationContext.publish(eventEnvelope);
            }
        }
        if (target) {
            target(...args);
        }
    }
}