// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Generated TypeScript interfaces and types.

// --------------------------------------------- Kernel Commands

export const CancelType = "Cancel";
export const ChangeWorkingDirectoryType = "ChangeWorkingDirectory";
export const CompileProjectType = "CompileProject";
export const DisplayErrorType = "DisplayError";
export const DisplayValueType = "DisplayValue";
export const OpenDocumentType = "OpenDocument";
export const OpenProjectType = "OpenProject";
export const QuitType = "Quit";
export const RequestCompletionsType = "RequestCompletions";
export const RequestDiagnosticsType = "RequestDiagnostics";
export const RequestHoverTextType = "RequestHoverText";
export const RequestInputType = "RequestInput";
export const RequestKernelInfoType = "RequestKernelInfo";
export const RequestSignatureHelpType = "RequestSignatureHelp";
export const RequestValueType = "RequestValue";
export const RequestValueInfosType = "RequestValueInfos";
export const SendEditableCodeType = "SendEditableCode";
export const SendValueType = "SendValue";
export const SubmitCodeType = "SubmitCode";
export const UpdateDisplayedValueType = "UpdateDisplayedValue";

export type KernelCommandType =
      typeof CancelType
    | typeof ChangeWorkingDirectoryType
    | typeof CompileProjectType
    | typeof DisplayErrorType
    | typeof DisplayValueType
    | typeof OpenDocumentType
    | typeof OpenProjectType
    | typeof QuitType
    | typeof RequestCompletionsType
    | typeof RequestDiagnosticsType
    | typeof RequestHoverTextType
    | typeof RequestInputType
    | typeof RequestKernelInfoType
    | typeof RequestSignatureHelpType
    | typeof RequestValueType
    | typeof RequestValueInfosType
    | typeof SendEditableCodeType
    | typeof SendValueType
    | typeof SubmitCodeType
    | typeof UpdateDisplayedValueType;

export interface Cancel extends KernelCommand {
}

export interface KernelCommand {
    targetKernelName?: string;
    originUri?: string;
    destinationUri?: string;
}

export interface ChangeWorkingDirectory extends KernelCommand {
    workingDirectory: string;
}

export interface CompileProject extends KernelCommand {
}

export interface DisplayError extends KernelCommand {
    message: string;
}

export interface DisplayValue extends KernelCommand {
    formattedValue: FormattedValue;
    valueId: string;
}

export interface OpenDocument extends KernelCommand {
    relativeFilePath: string;
    regionName?: string;
}

export interface OpenProject extends KernelCommand {
    project: Project;
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

export interface RequestInput extends KernelCommand {
    prompt: string;
    isPassword: boolean;
    inputTypeHint: string;
    valueName: string;
}

export interface RequestKernelInfo extends KernelCommand {
}

export interface RequestSignatureHelp extends LanguageServiceCommand {
}

export interface RequestValue extends KernelCommand {
    name: string;
    mimeType: string;
}

export interface RequestValueInfos extends KernelCommand {
    mimeType: string;
}

export interface SendEditableCode extends KernelCommand {
    kernelName: string;
    code: string;
}

export interface SendValue extends KernelCommand {
    formattedValue: FormattedValue;
    name: string;
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
    metadata: { [key: string]: any; };
}

export interface InteractiveDocumentOutputElement {
}

export interface ReturnValueElement extends InteractiveDocumentOutputElement {
    data: { [key: string]: any; };
    executionOrder: number;
    metadata: { [key: string]: any; };
}

export interface TextElement extends InteractiveDocumentOutputElement {
    name: string;
    text: string;
}

export interface ErrorElement extends InteractiveDocumentOutputElement {
    errorName: string;
    errorValue: string;
    stackTrace: Array<string>;
}

export interface DocumentKernelInfo {
    name: string;
    languageName?: string;
    aliases: Array<string>;
}

export interface NotebookParseRequest extends NotebookParseOrSerializeRequest {
    type: RequestType;
    rawData: Uint8Array;
}

export interface NotebookParseOrSerializeRequest {
    type: RequestType;
    id: string;
    serializationType: DocumentSerializationType;
    defaultLanguage: string;
}

export interface NotebookSerializeRequest extends NotebookParseOrSerializeRequest {
    type: RequestType;
    newLine: string;
    document: InteractiveDocument;
}

export interface NotebookParseResponse extends NotebookParserServerResponse {
    document: InteractiveDocument;
}

export interface NotebookParserServerResponse {
    id: string;
}

export interface NotebookSerializeResponse extends NotebookParserServerResponse {
    rawData: Uint8Array;
}

// --------------------------------------------- Kernel events

export const AssemblyProducedType = "AssemblyProduced";
export const CodeSubmissionReceivedType = "CodeSubmissionReceived";
export const CommandFailedType = "CommandFailed";
export const CommandSucceededType = "CommandSucceeded";
export const CompleteCodeSubmissionReceivedType = "CompleteCodeSubmissionReceived";
export const CompletionsProducedType = "CompletionsProduced";
export const DiagnosticsProducedType = "DiagnosticsProduced";
export const DisplayedValueProducedType = "DisplayedValueProduced";
export const DisplayedValueUpdatedType = "DisplayedValueUpdated";
export const DocumentOpenedType = "DocumentOpened";
export const ErrorProducedType = "ErrorProduced";
export const HoverTextProducedType = "HoverTextProduced";
export const IncompleteCodeSubmissionReceivedType = "IncompleteCodeSubmissionReceived";
export const InputProducedType = "InputProduced";
export const KernelExtensionLoadedType = "KernelExtensionLoaded";
export const KernelInfoProducedType = "KernelInfoProduced";
export const KernelReadyType = "KernelReady";
export const PackageAddedType = "PackageAdded";
export const ProjectOpenedType = "ProjectOpened";
export const ReturnValueProducedType = "ReturnValueProduced";
export const SignatureHelpProducedType = "SignatureHelpProduced";
export const StandardErrorValueProducedType = "StandardErrorValueProduced";
export const StandardOutputValueProducedType = "StandardOutputValueProduced";
export const ValueInfosProducedType = "ValueInfosProduced";
export const ValueProducedType = "ValueProduced";
export const WorkingDirectoryChangedType = "WorkingDirectoryChanged";

export type KernelEventType =
      typeof AssemblyProducedType
    | typeof CodeSubmissionReceivedType
    | typeof CommandFailedType
    | typeof CommandSucceededType
    | typeof CompleteCodeSubmissionReceivedType
    | typeof CompletionsProducedType
    | typeof DiagnosticsProducedType
    | typeof DisplayedValueProducedType
    | typeof DisplayedValueUpdatedType
    | typeof DocumentOpenedType
    | typeof ErrorProducedType
    | typeof HoverTextProducedType
    | typeof IncompleteCodeSubmissionReceivedType
    | typeof InputProducedType
    | typeof KernelExtensionLoadedType
    | typeof KernelInfoProducedType
    | typeof KernelReadyType
    | typeof PackageAddedType
    | typeof ProjectOpenedType
    | typeof ReturnValueProducedType
    | typeof SignatureHelpProducedType
    | typeof StandardErrorValueProducedType
    | typeof StandardOutputValueProducedType
    | typeof ValueInfosProducedType
    | typeof ValueProducedType
    | typeof WorkingDirectoryChangedType;

export interface AssemblyProduced extends KernelEvent {
    assembly: Base64EncodedAssembly;
}

export interface CodeSubmissionReceived extends KernelEvent {
    code: string;
}

export interface CommandFailed extends KernelCommandCompletionEvent {
    message: string;
}

export interface KernelCommandCompletionEvent extends KernelEvent {
    executionOrder?: number;
}

export interface CommandSucceeded extends KernelCommandCompletionEvent {
}

export interface CompleteCodeSubmissionReceived extends KernelEvent {
    code: string;
}

export interface CompletionsProduced extends KernelEvent {
    linePositionSpan?: LinePositionSpan;
    completions: Array<CompletionItem>;
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

export interface DocumentOpened extends KernelEvent {
    relativeFilePath: string;
    regionName?: string;
    content: string;
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

export interface KernelExtensionLoaded extends KernelEvent {
}

export interface KernelInfoProduced extends KernelEvent {
    kernelInfo: KernelInfo;
}

export interface KernelReady extends KernelEvent {
    kernelInfos: Array<KernelInfo>;
}

export interface PackageAdded extends KernelEvent {
    packageReference: ResolvedPackageReference;
}

export interface ProjectOpened extends KernelEvent {
    projectItems: Array<ProjectItem>;
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

export interface Base64EncodedAssembly {
    value: string;
}

export interface CompletionItem {
    displayText: string;
    kind: string;
    filterText: string;
    sortText: string;
    insertText: string;
    insertTextFormat?: InsertTextFormat;
    documentation: string;
}

export enum InsertTextFormat {
    PlainText = "plaintext",
    Snippet = "snippet",
}

export interface Diagnostic {
    linePositionSpan: LinePositionSpan;
    severity: DiagnosticSeverity;
    code: string;
    message: string;
}

export enum DiagnosticSeverity {
    Hidden = "hidden",
    Info = "info",
    Warning = "warning",
    Error = "error",
}

export interface LinePositionSpan {
    start: LinePosition;
    end: LinePosition;
}

export interface LinePosition {
    line: number;
    character: number;
}

export enum DocumentSerializationType {
    Dib = "dib",
    Ipynb = "ipynb",
}

export interface FormattedValue {
    mimeType: string;
    value: string;
}

export interface InteractiveDocument {
    elements: Array<InteractiveDocumentElement>;
    metadata: { [key: string]: any; };
}

export interface InteractiveDocumentElement {
    id?: string;
    kernelName?: string;
    contents: string;
    outputs: Array<InteractiveDocumentOutputElement>;
    executionOrder: number;
    metadata?: { [key: string]: any; };
}

export interface KernelInfo {
    aliases: Array<string>;
    languageName?: string;
    languageVersion?: string;
    isProxy: boolean;
    isComposite: boolean;
    displayName: string;
    localName: string;
    uri: string;
    remoteUri?: string;
    supportedKernelCommands: Array<KernelCommandInfo>;
    supportedDirectives: Array<KernelDirectiveInfo>;
}

export interface KernelCommandInfo {
    name: string;
}

export interface KernelDirectiveInfo {
    name: string;
}

export interface KernelValueInfo {
    typeName: string;
    name: string;
    formattedValue: FormattedValue;
    preferredMimeTypes: Array<string>;
}

export interface Project {
    files: Array<ProjectFile>;
}

export interface ProjectFile {
    relativeFilePath: string;
    content: string;
}

export interface ProjectItem {
    relativeFilePath: string;
    regionNames: Array<string>;
    regionsContent: { [key: string]: string; };
}

export enum RequestType {
    Parse = "parse",
    Serialize = "serialize",
}

export interface ResolvedPackageReference extends PackageReference {
    assemblyPaths: Array<string>;
    probingPaths: Array<string>;
    packageRoot: string;
}

export interface PackageReference {
    packageName: string;
    packageVersion: string;
    isPackageVersionSpecified: boolean;
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
    Run = "run",
    Diagnose = "diagnose",
}

