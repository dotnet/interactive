import { Observable } from "rxjs";
import { EventEnvelope } from "./events";

export abstract class ClientAdapterBase {
    abstract submitCommand(commandType: string, command: any, targetKernelName: string): Observable<EventEnvelope>;
}
