// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Generated TypeScript interfaces and types.

// --------------------------------------------- Kernel Commands

export const AddPackageType = "AddPackage";
export const AddPackageSourceType = "AddPackageSource";
export const CancelType = "Cancel";
export const CompileProjectType = "CompileProject";
export const DisplayErrorType = "DisplayError";
export const DisplayValueType = "DisplayValue";
export const ImportDocumentType = "ImportDocument";
export const OpenDocumentType = "OpenDocument";
export const OpenProjectType = "OpenProject";
export const QuitType = "Quit";
export const RequestCompletionsType = "RequestCompletions";
export const RequestDiagnosticsType = "RequestDiagnostics";
export const RequestHoverTextType = "RequestHoverText";
export const RequestInputType = "RequestInput";
export const RequestInputsType = "RequestInputs";
export const RequestKernelInfoType = "RequestKernelInfo";
export const RequestSignatureHelpType = "RequestSignatureHelp";
export const RequestValueType = "RequestValue";
export const RequestValueInfosType = "RequestValueInfos";
export const SendEditableCodeType = "SendEditableCode";
export const SendValueType = "SendValue";
export const SubmitCodeType = "SubmitCode";
export const UpdateDisplayedValueType = "UpdateDisplayedValue";

export type KernelCommandType =
      typeof AddPackageType
    | typeof AddPackageSourceType
    | typeof CancelType
    | typeof CompileProjectType
    | typeof DisplayErrorType
    | typeof DisplayValueType
    | typeof ImportDocumentType
    | typeof OpenDocumentType
    | typeof OpenProjectType
    | typeof QuitType
    | typeof RequestCompletionsType
    | typeof RequestDiagnosticsType
    | typeof RequestHoverTextType
    | typeof RequestInputType
    | typeof RequestInputsType
    | typeof RequestKernelInfoType
    | typeof RequestSignatureHelpType
    | typeof RequestValueType
    | typeof RequestValueInfosType
    | typeof SendEditableCodeType
    | typeof SendValueType
    | typeof SubmitCodeType
    | typeof UpdateDisplayedValueType;

export interface AddPackage extends KernelCommand {
    packageName: string;
    packageVersion: string;
}

export interface KernelCommand {
    destinationUri?: string;
    originUri?: string;
    targetKernelName?: string;
}

export interface AddPackageSource extends KernelCommand {
    packageSource: string;
}

export interface Cancel extends KernelCommand {
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

export interface ImportDocument extends KernelCommand {
    file: string;
}

export interface OpenDocument extends KernelCommand {
    regionName?: string;
    relativeFilePath: string;
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
    type: string;
    isPassword: boolean;
    parameterName: string;
    prompt: string;
    saveAs: string;
}

export interface RequestInputs extends KernelCommand {
    inputs: Array<InputDescription>;
}

export interface RequestKernelInfo extends KernelCommand {
}

export interface RequestSignatureHelp extends LanguageServiceCommand {
}

export interface RequestValue extends KernelCommand {
    mimeType: string;
    name: string;
}

export interface RequestValueInfos extends KernelCommand {
    mimeType: string;
}

export interface SendEditableCode extends KernelCommand {
    code: string;
    insertAtPosition?: number;
    kernelName: string;
}

export interface SendValue extends KernelCommand {
    formattedValue: FormattedValue;
    name: string;
}

export interface SubmitCode extends KernelCommand {
    code: string;
    parameters?: { [key: string]: string; };
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
    aliases: Array<string>;
    languageName?: string;
    name: string;
}

export interface NotebookParseRequest extends NotebookParseOrSerializeRequest {
    rawData: Uint8Array;
    type: RequestType;
}

export interface NotebookParseOrSerializeRequest {
    defaultLanguage: string;
    id: string;
    serializationType: DocumentSerializationType;
    type: RequestType;
}

export interface NotebookSerializeRequest extends NotebookParseOrSerializeRequest {
    document: InteractiveDocument;
    newLine: string;
    type: RequestType;
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
export const InputsProducedType = "InputsProduced";
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
    | typeof InputsProducedType
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
    | typeof ValueProducedType;

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
    completions: Array<CompletionItem>;
    linePositionSpan?: LinePositionSpan;
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
    content: string;
    regionName?: string;
    relativeFilePath: string;
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

export interface InputsProduced extends KernelEvent {
    values: { [key: string]: string; };
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
    activeParameterIndex: number;
    activeSignatureIndex: number;
    signatures: Array<SignatureInformation>;
}

export interface StandardErrorValueProduced extends DisplayEvent {
}

export interface StandardOutputValueProduced extends DisplayEvent {
}

export interface ValueInfosProduced extends KernelEvent {
    valueInfos: Array<KernelValueInfo>;
}

export interface ValueProduced extends KernelEvent {
    formattedValue: FormattedValue;
    name: string;
}

// --------------------------------------------- Required Types

export interface Base64EncodedAssembly {
    value: string;
}

export interface CompletionItem {
    displayText: string;
    documentation: string;
    filterText: string;
    insertText: string;
    insertTextFormat?: InsertTextFormat;
    kind: string;
    sortText: string;
}

export enum InsertTextFormat {
    PlainText = "plaintext",
    Snippet = "snippet",
}

export interface Diagnostic {
    code: string;
    linePositionSpan: LinePositionSpan;
    message: string;
    severity: DiagnosticSeverity;
}

export enum DiagnosticSeverity {
    Hidden = "hidden",
    Info = "info",
    Warning = "warning",
    Error = "error",
}

export interface LinePositionSpan {
    end: LinePosition;
    start: LinePosition;
}

export interface LinePosition {
    character: number;
    line: number;
}

export enum DocumentSerializationType {
    Dib = "dib",
    Ipynb = "ipynb",
}

export interface FormattedValue {
    mimeType: string;
    suppressDisplay: boolean;
    value: string;
}

export interface InputDescription {
    name: string;
    prompt: string;
    saveAs: string;
    type: string;
}

export interface InteractiveDocument {
    elements: Array<InteractiveDocumentElement>;
    metadata: { [key: string]: any; };
}

export interface InteractiveDocumentElement {
    contents: string;
    executionOrder: number;
    id?: string;
    kernelName?: string;
    metadata?: { [key: string]: any; };
    outputs: Array<InteractiveDocumentOutputElement>;
}

export interface KernelInfo {
    aliases: Array<string>;
    description?: string;
    displayName: string;
    isComposite: boolean;
    isProxy: boolean;
    languageName?: string;
    languageVersion?: string;
    localName: string;
    remoteUri?: string;
    supportedKernelCommands: Array<KernelCommandInfo>;
    uri: string;
}

export interface KernelCommandInfo {
    name: string;
}

export interface KernelValueInfo {
    formattedValue: FormattedValue;
    name: string;
    preferredMimeTypes: Array<string>;
    typeName: string;
}

export interface Project {
    files: Array<ProjectFile>;
}

export interface ProjectFile {
    content: string;
    relativeFilePath: string;
}

export interface ProjectItem {
    regionNames: Array<string>;
    regionsContent: { [key: string]: string; };
    relativeFilePath: string;
}

export enum RequestType {
    Parse = "parse",
    Serialize = "serialize",
}

export interface ResolvedPackageReference extends PackageReference {
    assemblyPaths: Array<string>;
    packageRoot: string;
    probingPaths: Array<string>;
}

export interface PackageReference {
    isPackageVersionSpecified: boolean;
    packageName: string;
    packageVersion: string;
}

export interface SignatureInformation {
    documentation: FormattedValue;
    label: string;
    parameters: Array<ParameterInformation>;
}

export interface ParameterInformation {
    documentation: FormattedValue;
    label: string;
}

