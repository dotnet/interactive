import { DotnetInteractiveClient, KernelClientContainer } from "../src/dotnet-interactive/dotnet-interactive-interfaces";

export function asKernelClientContainer(client: DotnetInteractiveClient): KernelClientContainer {
    return <KernelClientContainer><any>client;
}