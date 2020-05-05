import { Observable } from "rxjs";
import { KernelCommand, KernelCommandType, KernelEventEnvelope, KernelEventType } from "../../contracts";

// Replays all events given to it
export class TestClientTransport {
    constructor(readonly fakedEvents: { [key: string]: {eventType: KernelEventType, event: any}[] }) {
    }

    submitCommand(commandType: KernelCommandType, command: KernelCommand): Observable<KernelEventEnvelope> {
        let eventsToReturn = this.fakedEvents[commandType];
        return new Observable<KernelEventEnvelope>(subscriber => {
            for (let event of eventsToReturn) {
                subscriber.next({
                    eventType: event.eventType,
                    event: event.event,
                });
            }

            subscriber.complete();
        });
    }
}
