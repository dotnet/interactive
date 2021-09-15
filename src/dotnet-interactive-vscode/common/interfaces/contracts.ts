// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Generated TypeScript interfaces and types.

// --------------------------------------------- Kernel Commands

export const AddPackageType = "AddPackage";
export const CancelType = "Cancel";
export const ChangeWorkingDirectoryType = "ChangeWorkingDirectory";
export const DisplayErrorType = "DisplayError";
export const DisplayValueType = "DisplayValue";
export const GetInputType = "GetInput";
export const ParseInteractiveDocumentType = "ParseInteractiveDocument";
export const QuitType = "Quit";
export const RequestCompletionsType = "RequestCompletions";
export const RequestDiagnosticsType = "RequestDiagnostics";
export const RequestHoverTextType = "RequestHoverText";
export const RequestSignatureHelpType = "RequestSignatureHelp";
export const RequestValueType = "RequestValue";
export const RequestValueInfosType = "RequestValueInfos";
export const SendEditableCodeType = "SendEditableCode";
export const SerializeInteractiveDocumentType = "SerializeInteractiveDocument";
export const SubmitCodeType = "SubmitCode";
export const UpdateDisplayedValueType = "UpdateDisplayedValue";

export type KernelCommandType =
      typeof AddPackageType
    | typeof CancelType
    | typeof ChangeWorkingDirectoryType
    | typeof DisplayErrorType
    | typeof DisplayValueType
    | typeof GetInputType
    | typeof ParseInteractiveDocumentType
    | typeof QuitType
    | typeof RequestCompletionsType
    | typeof RequestDiagnosticsType
    | typeof RequestHoverTextType
    | typeof RequestSignatureHelpType
    | typeof RequestValueType
    | typeof RequestValueInfosType
    | typeof SendEditableCodeType
    | typeof SerializeInteractiveDocumentType
    | typeof SubmitCodeType
    | typeof UpdateDisplayedValueType;

export interface AddPackage extends KernelCommand {
    packageReference: PackageReference;
}

export interface KernelCommand {
    targetKernelName?: string;
    id?: string;
}

export interface Cancel extends KernelCommand {
}

export interface ChangeWorkingDirectory extends KernelCommand {
    workingDirectory: string;
}

export interface DisplayError extends KernelCommand {
    message: string;
}

export interface DisplayValue extends KernelCommand {
    formattedValue: FormattedValue;
    valueId: string;
}

export interface GetInput extends KernelCommand {
    prompt: string;
    isPassword: boolean;
}

export interface ParseInteractiveDocument extends KernelCommand {
    fileName: string;
    rawData: Uint8Array;
}

export interface Quit extends KernelCommand {
}

export interface RequestCompletions extends LanguageServiceCommand {
}

export interface LanguageServiceCommand extends KernelCommand {
    code: string;
    linePosition: LinePosition;
}

export interface RequestDiagnostics extends KernelCommand {
    code: string;
}

export interface RequestHoverText extends LanguageServiceCommand {
}

export interface RequestSignatureHelp extends LanguageServiceCommand {
}

export interface RequestValue extends KernelCommand {
    name: string;
    mimeType: string;
}

export interface RequestValueInfos extends KernelCommand {
}

export interface SendEditableCode extends KernelCommand {
    language: string;
    code: string;
}

export interface SerializeInteractiveDocument extends KernelCommand {
    fileName: string;
    document: InteractiveDocument;
    newLine: string;
}

export interface SubmitCode extends KernelCommand {
    code: string;
    submissionType?: SubmissionType;
}

export interface UpdateDisplayedValue extends KernelCommand {
    formattedValue: FormattedValue;
    valueId: string;
}

export interface KernelEvent {
}

export interface DisplayElement extends InteractiveDocumentOutputElement {
    data: { [key: string]: any; };
}

export interface InteractiveDocumentOutputElement {
}

export interface TextElement extends InteractiveDocumentOutputElement {
    text: string;
}

export interface ErrorElement extends InteractiveDocumentOutputElement {
    errorName: string;
    errorValue: string;
    stackTrace: Array<string>;
}

// --------------------------------------------- Kernel events

export const CodeSubmissionReceivedType = "CodeSubmissionReceived";
export const CommandFailedType = "CommandFailed";
export const CommandSucceededType = "CommandSucceeded";
export const CompleteCodeSubmissionReceivedType = "CompleteCodeSubmissionReceived";
export const CompletionsProducedType = "CompletionsProduced";
export const DiagnosticLogEntryProducedType = "DiagnosticLogEntryProduced";
export const DiagnosticsProducedType = "DiagnosticsProduced";
export const DisplayedValueProducedType = "DisplayedValueProduced";
export const DisplayedValueUpdatedType = "DisplayedValueUpdated";
export const ErrorProducedType = "ErrorProduced";
export const HoverTextProducedType = "HoverTextProduced";
export const IncompleteCodeSubmissionReceivedType = "IncompleteCodeSubmissionReceived";
export const InputProducedType = "InputProduced";
export const InteractiveDocumentParsedType = "InteractiveDocumentParsed";
export const InteractiveDocumentSerializedType = "InteractiveDocumentSerialized";
export const KernelExtensionLoadedType = "KernelExtensionLoaded";
export const KernelReadyType = "KernelReady";
export const PackageAddedType = "PackageAdded";
export const ReturnValueProducedType = "ReturnValueProduced";
export const SignatureHelpProducedType = "SignatureHelpProduced";
export const StandardErrorValueProducedType = "StandardErrorValueProduced";
export const StandardOutputValueProducedType = "StandardOutputValueProduced";
export const ValueInfosProducedType = "ValueInfosProduced";
export const ValueProducedType = "ValueProduced";
export const WorkingDirectoryChangedType = "WorkingDirectoryChanged";

export type KernelEventType =
      typeof CodeSubmissionReceivedType
    | typeof CommandFailedType
    | typeof CommandSucceededType
    | typeof CompleteCodeSubmissionReceivedType
    | typeof CompletionsProducedType
    | typeof DiagnosticLogEntryProducedType
    | typeof DiagnosticsProducedType
    | typeof DisplayedValueProducedType
    | typeof DisplayedValueUpdatedType
    | typeof ErrorProducedType
    | typeof HoverTextProducedType
    | typeof IncompleteCodeSubmissionReceivedType
    | typeof InputProducedType
    | typeof InteractiveDocumentParsedType
    | typeof InteractiveDocumentSerializedType
    | typeof KernelExtensionLoadedType
    | typeof KernelReadyType
    | typeof PackageAddedType
    | typeof ReturnValueProducedType
    | typeof SignatureHelpProducedType
    | typeof StandardErrorValueProducedType
    | typeof StandardOutputValueProducedType
    | typeof ValueInfosProducedType
    | typeof ValueProducedType
    | typeof WorkingDirectoryChangedType;

export interface CodeSubmissionReceived extends KernelEvent {
    code: string;
}

export interface CommandFailed extends KernelEvent {
    message: string;
}

export interface CommandSucceeded extends KernelEvent {
}

export interface CompleteCodeSubmissionReceived extends KernelEvent {
    code: string;
}

export interface CompletionsProduced extends KernelEvent {
    linePositionSpan?: LinePositionSpan;
    completions: Array<CompletionItem>;
}

export interface DiagnosticLogEntryProduced extends DiagnosticEvent {
    message: string;
}

export interface DiagnosticEvent extends KernelEvent {
}

export interface DiagnosticsProduced extends KernelEvent {
    diagnostics: Array<Diagnostic>;
    formattedDiagnostics: Array<FormattedValue>;
}

export interface DisplayedValueProduced extends DisplayEvent {
}

export interface DisplayEvent extends KernelEvent {
    formattedValues: Array<FormattedValue>;
    valueId?: string;
}

export interface DisplayedValueUpdated extends DisplayEvent {
}

export interface ErrorProduced extends DisplayEvent {
    message: string;
}

export interface HoverTextProduced extends KernelEvent {
    content: Array<FormattedValue>;
    linePositionSpan?: LinePositionSpan;
}

export interface IncompleteCodeSubmissionReceived extends KernelEvent {
}

export interface InputProduced extends KernelEvent {
    value: string;
}

export interface InteractiveDocumentParsed extends KernelEvent {
    document: InteractiveDocument;
}

export interface InteractiveDocumentSerialized extends KernelEvent {
    rawData: Uint8Array;
}

export interface KernelExtensionLoaded extends KernelEvent {
}

export interface KernelReady extends KernelEvent {
}

export interface PackageAdded extends KernelEvent {
    packageReference: ResolvedPackageReference;
}

export interface ReturnValueProduced extends DisplayEvent {
}

export interface SignatureHelpProduced extends KernelEvent {
    signatures: Array<SignatureInformation>;
    activeSignatureIndex: number;
    activeParameterIndex: number;
}

export interface StandardErrorValueProduced extends DisplayEvent {
}

export interface StandardOutputValueProduced extends DisplayEvent {
}

export interface ValueInfosProduced extends KernelEvent {
    valueInfos: Array<KernelValueInfo>;
}

export interface ValueProduced extends KernelEvent {
    name: string;
    formattedValue: FormattedValue;
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

export interface Diagnostic {
    linePositionSpan: LinePositionSpan;
    severity: DiagnosticSeverity;
    code: string;
    message: string;
}

export enum DiagnosticSeverity {
    Hidden = 0,
    Info = 1,
    Warning = 2,
    Error = 3,
}

export interface LinePositionSpan {
    start: LinePosition;
    end: LinePosition;
}

export interface LinePosition {
    line: number;
    character: number;
}

export interface FormattedValue {
    mimeType: string;
    value: string;
}

export interface InteractiveDocument {
    elements: Array<InteractiveDocumentElement>;
}

export interface InteractiveDocumentElement {
    language: string;
    contents: string;
    outputs: Array<InteractiveDocumentOutputElement>;
}

export interface KernelValueInfo {
    name: string;
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

export interface SignatureInformation {
    label: string;
    documentation: FormattedValue;
    parameters: Array<ParameterInformation>;
}

export interface ParameterInformation {
    label: string;
    documentation: FormattedValue;
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

export interface KernelCommandEnvelopeHandler {
    (eventEnvelope: KernelCommandEnvelope): Promise<void>;
}

export interface Disposable {
    dispose(): void;
}

export interface DisposableSubscription extends Disposable {
}

export interface KernelTransport extends Transport {
    waitForReady(): Promise<void>;
}

export interface Transport extends Disposable {
    subscribeToKernelEvents(observer: KernelEventEnvelopeObserver): DisposableSubscription;
    setCommandHandler(handler: KernelCommandEnvelopeHandler): void;
    submitCommand(commandEnvelope: KernelCommandEnvelope): Promise<void>;
    publishKernelEvent(eventEnvelope: KernelEventEnvelope): Promise<void>;
}

