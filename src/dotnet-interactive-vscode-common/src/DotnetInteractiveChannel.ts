
import { KernelCommandAndEventChannel } from './dotnet-interactive';

export interface DotnetInteractiveChannel extends KernelCommandAndEventChannel {
    waitForReady(): Promise<void>;
}
