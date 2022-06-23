import * as contracts from "./contracts";
import { IKernelCommandInvocation, Kernel } from "./kernel";
import { KernelHost } from "./kernelHost";
export declare class CompositeKernel extends Kernel {
    private _host;
    private readonly _namesTokernelMap;
    private readonly _kernelToNamesMap;
    defaultKernelName: string | undefined;
    constructor(name: string);
    get childKernels(): Kernel[];
    get host(): KernelHost | null;
    set host(host: KernelHost | null);
    protected handleRequestKernelInfo(invocation: IKernelCommandInvocation): Promise<void>;
    add(kernel: Kernel, aliases?: string[]): void;
    findKernelByName(kernelName: string): Kernel | undefined;
    findKernelByUri(uri: string): Kernel | undefined;
    handleCommand(commandEnvelope: contracts.KernelCommandEnvelope): Promise<void>;
    getHandlingKernel(commandEnvelope: contracts.KernelCommandEnvelope): Kernel | undefined;
}
