export interface CommandFailed {
    message: string;
}

export interface DisplayEventBase {
    value: any;
    formattedValues: Array<any>;
    valueId: string;
}

export interface LinePosition {
    line: number;
    character: number;
}

export interface LinePositionSpan {
    start: LinePosition;
    end: LinePosition;
}

export interface HoverMarkdownProduced {
    content: string;
    range?: LinePositionSpan
}

export interface HoverPlainTextProduced {
    content: string;
    range?: LinePositionSpan;
}

export interface ReturnValueProduced extends DisplayEventBase {
}

export interface StandardOutputValueProduced extends DisplayEventBase {
}

export type Event = CommandFailed | HoverMarkdownProduced | HoverPlainTextProduced | ReturnValueProduced | StandardOutputValueProduced;

export interface EventEnvelope {
    eventType: string;
    event: Event;
    cause: {token: string};
}
