import * as contracts from "./contracts";
import { IKernelCommandHandler, Kernel } from "./kernel";
export declare class ProxyKernel extends Kernel {
    readonly name: string;
    private readonly channel;
    constructor(name: string, channel: contracts.KernelCommandAndEventChannel);
    getCommandHandler(commandType: contracts.KernelCommandType): IKernelCommandHandler | undefined;
    private _commandHandler;
}
