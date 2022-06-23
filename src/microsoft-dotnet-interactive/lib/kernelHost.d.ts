import { CompositeKernel } from './compositeKernel';
import * as contracts from './contracts';
import { Kernel } from './kernel';
import { ProxyKernel } from './proxyKernel';
export declare class KernelHost {
    private readonly _kernel;
    private readonly _channel;
    private readonly _remoteUriToKernel;
    private readonly _uriToKernel;
    private readonly _kernelToKernelInfo;
    private readonly _uri;
    private readonly _scheduler;
    constructor(_kernel: CompositeKernel, _channel: contracts.KernelCommandAndEventChannel, hostUri: string);
    tryGetKernelByRemoteUri(remoteUri: string): Kernel | undefined;
    trygetKernelByOriginUri(originUri: string): Kernel | undefined;
    tryGetKernelInfo(kernel: Kernel): contracts.KernelInfo | undefined;
    addKernelInfo(kernel: Kernel, kernelInfo: contracts.KernelInfo): void;
    getKernel(kernelCommandEnvelope: contracts.KernelCommandEnvelope): Kernel;
    registerRemoteUriForProxy(proxyLocalKernelName: string, remoteUri: string): void;
    createProxyKernelOnDefaultConnector(kernelInfo: contracts.KernelInfo): ProxyKernel;
    connect(): void;
}
