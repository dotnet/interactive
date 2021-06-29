import { InspectOptions } from "util";
import { IKernelInvocationContext } from "../common/interfaces/kernel";

export class ConsoleCapture implements Console {
    private _global: Console = null;
    constructor(private kernelInvocationContext: IKernelInvocationContext) {
       this._global = global.console;
       global.console = this;
        
    }
    memory: any;   

    Console: NodeJS.ConsoleConstructor;

     assert(value: any, message?: string, ...optionalParams: any[]): void{
        this._global.assert(value,message,optionalParams);
    }
    clear(): void {
        this._global.clear();
    }
    count(label?: any): void {
        this._global.count(label);
    }
    countReset(label?: string): void {
        this._global.countReset(label);
    }
    debug(message?: any, ...optionalParams: any[]): void {
        this._global.debug(message, optionalParams);
    }
    dir(obj: any, options?: InspectOptions): void {
        this._global.dir(obj, options);
    }
    dirxml(...data: any[]): void {
        this._global.dirxml(data);
    }
    error(message?: any, ...optionalParams: any[]): void {
        this._global.error(message,optionalParams);
    }
    exception(message?: string, ...optionalParams: any[]): void {
       this._global.exception(message,optionalParams);
    }
    group(...label: any[]): void {
        this._global.group(label);
    }
    groupCollapsed(...label: any[]): void {
        this._global.groupCollapsed(label);
    }
    groupEnd(): void {
        this._global.groupEnd();
    }
    info(message?: any, ...optionalParams: any[]): void {
        this._global.info(message, optionalParams);
    }
    log(message?: any, ...optionalParams: any[]): void {
        this._global.log(message, optionalParams);
    }
    table(tabularData: any, properties?: ReadonlyArray<string>): void {
        this._global.table(tabularData, properties);
    }
    time(label?: string): void {
        this._global.time(label);
    }
    timeEnd(label?: string): void {
        this._global.timeEnd(label);
    }
    timeLog(label?: string, ...data: any[]): void {
        this._global.timeLog(label,data);
    }
    timeStamp(label?: string): void {
        this._global.timeStamp(label);
    }
    trace(message?: any, ...optionalParams: any[]): void {
        this._global.trace(message,optionalParams);
    }
    warn(message?: any, ...optionalParams: any[]): void{
        this._global.warn(message,optionalParams);
    }
   
    profile(label?: string): void {
        this._global.profile(label);
    }
    profileEnd(label?: string): void {
        this._global.profileEnd(label);
    }

    dispose(): void{       
       global.console = this._global;
       this._global = global.console;
    }
}