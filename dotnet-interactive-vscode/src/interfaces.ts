export interface CommandFailed {
    message: string;
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

export interface ReturnValueProduced {
    value: any;
    formattedValues: Array<any>;
    valueId: string;
}

export type Event = CommandFailed | HoverMarkdownProduced | HoverPlainTextProduced | ReturnValueProduced;

export interface EventEnvelope {
    eventType: string;
    event: Event;
    cause: {token: string};
}
