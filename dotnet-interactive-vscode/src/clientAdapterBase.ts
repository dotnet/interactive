import { Observable } from "rxjs";
import { EventEnvelope } from "./events";

export abstract class ClientAdapterBase {
    private _targetKernelName: string;

    abstract submitCommand(commandType: string, command: any): Observable<EventEnvelope>;

    constructor(targetKernelName: string) {
        this._targetKernelName = targetKernelName;
    }

    get targetKernelName(): string {
        return this._targetKernelName;
    }
}
