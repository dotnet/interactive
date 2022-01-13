
import { Connector } from './dotnet-interactive';

export interface KernelConnector extends Connector {
    waitForReady(): Promise<void>;
}
