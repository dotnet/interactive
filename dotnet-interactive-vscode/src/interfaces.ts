export interface CommandFailed {
    message: string;
}

export interface ReturnValueProduced {
    value: any;
    formattedValues: any[];
    valueId: string;
}

export type Event = CommandFailed | ReturnValueProduced;

export interface ReceivedInteractiveEvent {
    eventType: string;
    event: Event;
    cause: {token: string};
}
