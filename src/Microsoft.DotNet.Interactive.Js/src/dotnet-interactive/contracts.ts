// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Generated TypeScript interfaces and types.

// --------------------------------------------- Kernel Commands

export const AddPackageType = "AddPackage";
export const ChangeWorkingDirectoryType = "ChangeWorkingDirectory";
export const DisplayErrorType = "DisplayError";
export const DisplayValueType = "DisplayValue";
export const RequestCompletionType = "RequestCompletion";
export const RequestDiagnosticsType = "RequestDiagnostics";
export const RequestHoverTextType = "RequestHoverText";
export const SubmitCodeType = "SubmitCode";
export const UpdateDisplayedValueType = "UpdateDisplayedValue";

export type KernelCommandType =
      typeof AddPackageType
    | typeof ChangeWorkingDirectoryType
    | typeof DisplayErrorType
    | typeof DisplayValueType
    | typeof RequestCompletionType
    | typeof RequestDiagnosticsType
    | typeof RequestHoverTextType
    | typeof SubmitCodeType
    | typeof UpdateDisplayedValueType;

export interface AddPackage extends KernelCommand {
    packageReference: PackageReference;
}

export interface KernelCommand {
    targetKernelName?: string;
}

export interface ChangeWorkingDirectory extends KernelCommand {
    workingDirectory: string;
}

export interface DisplayError extends KernelCommand {
    message: string;
}

export interface DisplayValue extends KernelCommand {
    value: any;
    formattedValue: FormattedValue;
    valueId: string;
}

export interface RequestCompletion extends LanguageServiceCommand {
}

export interface LanguageServiceCommand extends KernelCommand {
    code: string;
    linePosition: LinePosition;
}

export interface RequestDiagnostics extends KernelCommand {
}

export interface RequestHoverText extends LanguageServiceCommand {
}

export interface SubmitCode extends KernelCommand {
    code: string;
    submissionType?: SubmissionType;
}

export interface UpdateDisplayedValue extends KernelCommand {
    value: any;
    formattedValue: FormattedValue;
    valueId: string;
}

// --------------------------------------------- Kernel events

export const CodeSubmissionReceivedType = "CodeSubmissionReceived";
export const CommandFailedType = "CommandFailed";
export const CommandSucceededType = "CommandSucceeded";
export const CompleteCodeSubmissionReceivedType = "CompleteCodeSubmissionReceived";
export const CompletionRequestCompletedType = "CompletionRequestCompleted";
export const CompletionRequestReceivedType = "CompletionRequestReceived";
export const DiagnosticLogEntryProducedType = "DiagnosticLogEntryProduced";
export const DisplayedValueProducedType = "DisplayedValueProduced";
export const DisplayedValueUpdatedType = "DisplayedValueUpdated";
export const ErrorProducedType = "ErrorProduced";
export const HoverTextProducedType = "HoverTextProduced";
export const IncompleteCodeSubmissionReceivedType = "IncompleteCodeSubmissionReceived";
export const InputRequestedType = "InputRequested";
export const PackageAddedType = "PackageAdded";
export const PasswordRequestedType = "PasswordRequested";
export const ReturnValueProducedType = "ReturnValueProduced";
export const StandardErrorValueProducedType = "StandardErrorValueProduced";
export const StandardOutputValueProducedType = "StandardOutputValueProduced";
export const WorkingDirectoryChangedType = "WorkingDirectoryChanged";

export type KernelEventType =
      typeof CodeSubmissionReceivedType
    | typeof CommandFailedType
    | typeof CommandSucceededType
    | typeof CompleteCodeSubmissionReceivedType
    | typeof CompletionRequestCompletedType
    | typeof CompletionRequestReceivedType
    | typeof DiagnosticLogEntryProducedType
    | typeof DisplayedValueProducedType
    | typeof DisplayedValueUpdatedType
    | typeof ErrorProducedType
    | typeof HoverTextProducedType
    | typeof IncompleteCodeSubmissionReceivedType
    | typeof InputRequestedType
    | typeof PackageAddedType
    | typeof PasswordRequestedType
    | typeof ReturnValueProducedType
    | typeof StandardErrorValueProducedType
    | typeof StandardOutputValueProducedType
    | typeof WorkingDirectoryChangedType;

export interface CodeSubmissionReceived extends KernelEvent {
    code: string;
}

export interface KernelEvent {
}

export interface CommandFailed extends KernelEvent {
    message: string;
}

export interface CommandSucceeded extends KernelEvent {
}

export interface CompleteCodeSubmissionReceived extends KernelEvent {
    code: string;
}

export interface CompletionRequestCompleted extends KernelEvent {
    linePositionSpan?: LinePositionSpan;
    completionList: Array<CompletionItem>;
}

export interface CompletionRequestReceived extends KernelEvent {
}

export interface DiagnosticLogEntryProduced extends DiagnosticEvent {
    message: string;
}

export interface DiagnosticEvent extends KernelEvent {
}

export interface DisplayedValueProduced extends DisplayEvent {
}

export interface DisplayEvent extends KernelEvent {
    value: any;
    formattedValues: Array<FormattedValue>;
    valueId: string;
}

export interface DisplayedValueUpdated extends DisplayEvent {
}

export interface ErrorProduced extends DisplayEvent {
    message: string;
}

export interface HoverTextProduced extends KernelEvent {
    content: Array<FormattedValue>;
    range?: LinePositionSpan;
}

export interface IncompleteCodeSubmissionReceived extends KernelEvent {
}

export interface InputRequested extends KernelEvent {
    prompt: string;
}

export interface PackageAdded extends KernelEvent {
    packageReference: ResolvedPackageReference;
}

export interface PasswordRequested extends KernelEvent {
    prompt: string;
}

export interface ReturnValueProduced extends DisplayEvent {
}

export interface StandardErrorValueProduced extends DisplayEvent {
}

export interface StandardOutputValueProduced extends DisplayEvent {
}

export interface WorkingDirectoryChanged extends KernelEvent {
    workingDirectory: string;
}

// --------------------------------------------- Required Types

export interface CompletionItem {
    displayText: string;
    kind: string;
    filterText: string;
    sortText: string;
    insertText: string;
    documentation: string;
}

export interface FormattedValue {
    mimeType: string;
    value: string;
}

export interface LinePosition {
    line: number;
    character: number;
}

export interface LinePositionSpan {
    start: LinePosition;
    end: LinePosition;
}

export interface PackageReference {
    packageName: string;
    packageVersion: string;
    isPackageVersionSpecified: boolean;
}

export interface ResolvedPackageReference extends PackageReference {
    assemblyPaths: Array<string>;
    probingPaths: Array<string>;
    packageRoot: string;
}

export enum SubmissionType {
    Run = 0,
    Diagnose = 1,
}

export interface KernelEventEnvelope {
    eventType: KernelEventType;
    event: KernelEvent;
    command?: KernelCommandEnvelope;
}

export interface KernelCommandEnvelope {
    token?: string;
    commandType: KernelCommandType;
    command: KernelCommand;
}

export interface KernelEventEnvelopeObserver {
    (eventEnvelope: KernelEventEnvelope): void;
}

export interface Disposable {
    dispose(): void;
}

export interface DisposableSubscription extends Disposable {
}

export interface KernelTransport extends Disposable {
    subscribeToKernelEvents(observer: KernelEventEnvelopeObserver): DisposableSubscription;
    submitCommand(command: KernelCommand, commandType: KernelCommandType, token: string): Promise<void>;
}
