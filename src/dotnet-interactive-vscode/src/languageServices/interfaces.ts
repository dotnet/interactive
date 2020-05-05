import { LinePositionSpan } from './../contracts';

export interface CancellationTokenLike {
    isCancellationRequested: boolean;
    onCancellationRequested: {(arg: any): any};
}

export interface DocumentLike {
    uri: {path: string};
    getText: {(): string};
}

export interface HoverResult {
    contents: string,
    isMarkdown: boolean;
    range: LinePositionSpan | undefined,
}

export interface PositionLike {
    line: number;
    character: number;
}
