// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


import { KernelCommandType, KernelCommand } from "./commands";

export const CodeSubmissionReceivedEventType = "CodeSubmissionReceived";
export const CommandFailedEventType = "CommandFailed";
export const CommandHandledEventType = "CommandHandled";
export const CompleteCodeSubmissionReceivedEventType = "CompleteCodeSubmissionReceived";
export const CompletionRequestCompletedEventType = "CompletionRequestCompleted";
export const CompletionRequestReceivedEventType = "CompletionRequestReceived";
export const DiagnosticLogEventProducedEventType = "DiagnosticLogEventProduced";
export const DisplayedValueProducedEventType = "DisplayedValueProduced";
export const DisplayedValueUpdatedEventType = "DisplayedValueUpdated";
export const ErrorProducedEventType = "ErrorProduced";
export const IncompleteCodeSubmissionReceivedEventType = "IncompleteCodeSubmissionReceived";
export const InputRequestedEventType = "InputRequested";
export const PackageAddedEventType = "PackageAdded";
export const PasswordRequestedEventType = "PasswordRequested";
export const ReturnValueProducedEventType = "ReturnValueProduced";
export const StandardErrorValueProducedEventType = "StandardErrorValueProduced";
export const StandardOutputValueProducedEventType = "StandardOutputValueProduced";

export type KernelEventType =
    typeof CodeSubmissionReceivedEventType
    | typeof CommandFailedEventType
    | typeof CommandHandledEventType
    | typeof CompleteCodeSubmissionReceivedEventType
    | typeof CompletionRequestCompletedEventType
    | typeof CompletionRequestReceivedEventType
    | typeof DiagnosticLogEventProducedEventType
    | typeof DisplayedValueProducedEventType
    | typeof ErrorProducedEventType
    | typeof InputRequestedEventType
    | typeof ReturnValueProducedEventType
    | typeof PasswordRequestedEventType
    | typeof StandardErrorValueProducedEventType
    | typeof StandardOutputValueProducedEventType;

export interface KernelEventEnvelope {
    eventType: KernelEventType;
    event: KernelEvent;
    casue?: {
        token: string;
        commandType: KernelCommandType;
        command: KernelCommand;
    }
}

export interface KernelEvent {

}

export interface CodeSubmissionReceived extends KernelEvent {
    code: string;
}

export interface CommandFailed extends KernelEvent {
    message: string;
}

export interface CommandHandled extends KernelEvent {

}

export interface CompleteCodeSubmissionReceived extends KernelEvent {
    code: string;
}

export interface CompletionRequestCompleted extends KernelEvent {
    replacementStartIndex?: number;
    replacementEndIndex?: number;
    completionList: Array<{
        displayText: string;
        kind: string;
        filterText?: string;
        sortText?: string;
        insertText?: string;
        documentation?: string;
    }>;
}


export interface CompletionRequestReceived extends KernelEvent {

}

export interface DiagnosticLogEventProduced extends KernelEvent {
    message: string;
}

export interface DisplayedValueProduced extends KernelEvent {
    value: any;
    formattedValues: Array<{
        mimeType: string;
        value: string;
    }>;
    valueId: string;
}

export interface DisplayedValueUpdated extends KernelEvent {
    value: any;
    formattedValues: Array<{
        mimeType: string;
        value: string;
    }>;
    valueId: string;
}

export interface ErrorProduced extends KernelEvent {
    message: string;
}

export interface IncompleteCodeSubmissionReceived extends KernelEvent {

}

export interface InputRequested extends KernelEvent {
    prompt : string;
}

export interface PackageAdded extends KernelEvent {
    packageReference: {
        packageName: string;
        packageVersion: string;
        packageRoot: string;
        assemblyPaths: Array<string>;
        probingPaths:  Array<string>;
    }
}

export interface PasswordRequested extends KernelEvent{
    prompt: string;
}

export interface ReturnValueProduced extends KernelEvent{
    value: any;
    formattedValues: Array<{
        mimeType: string;
        value: string;
    }>;
}

export interface StandardErrorValueProduced extends KernelEvent{
    value: any;
    formattedValues: Array<{
        mimeType: string;
        value: string;
    }>;
}

export interface StandardOutputValueProduced extends KernelEvent{
    value: any;
    formattedValues: Array<{
        mimeType: string;
        value: string;
    }>;
}