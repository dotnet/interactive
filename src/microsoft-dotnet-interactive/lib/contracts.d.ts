export declare const AddPackageType = "AddPackage";
export declare const CancelType = "Cancel";
export declare const ChangeWorkingDirectoryType = "ChangeWorkingDirectory";
export declare const CompileProjectType = "CompileProject";
export declare const DisplayErrorType = "DisplayError";
export declare const DisplayValueType = "DisplayValue";
export declare const OpenDocumentType = "OpenDocument";
export declare const OpenProjectType = "OpenProject";
export declare const QuitType = "Quit";
export declare const RequestCompletionsType = "RequestCompletions";
export declare const RequestDiagnosticsType = "RequestDiagnostics";
export declare const RequestHoverTextType = "RequestHoverText";
export declare const RequestInputType = "RequestInput";
export declare const RequestKernelInfoType = "RequestKernelInfo";
export declare const RequestSignatureHelpType = "RequestSignatureHelp";
export declare const RequestValueType = "RequestValue";
export declare const RequestValueInfosType = "RequestValueInfos";
export declare const SendEditableCodeType = "SendEditableCode";
export declare const SubmitCodeType = "SubmitCode";
export declare const UpdateDisplayedValueType = "UpdateDisplayedValue";
export declare type KernelCommandType = typeof AddPackageType | typeof CancelType | typeof ChangeWorkingDirectoryType | typeof CompileProjectType | typeof DisplayErrorType | typeof DisplayValueType | typeof OpenDocumentType | typeof OpenProjectType | typeof QuitType | typeof RequestCompletionsType | typeof RequestDiagnosticsType | typeof RequestHoverTextType | typeof RequestInputType | typeof RequestKernelInfoType | typeof RequestSignatureHelpType | typeof RequestValueType | typeof RequestValueInfosType | typeof SendEditableCodeType | typeof SubmitCodeType | typeof UpdateDisplayedValueType;
export interface AddPackage extends KernelCommand {
    packageReference: PackageReference;
}
export interface KernelCommand {
    targetKernelName?: string;
    originUri?: string;
    destinationUri?: string;
}
export interface Cancel extends KernelCommand {
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
}
export interface SendEditableCode extends KernelCommand {
    language: string;
    code: string;
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
    data: {
        [key: string]: any;
    };
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
export interface NotebookErrorResponse extends NotebookParserServerResponse {
    errorMessage: string;
}
export declare const AssemblyProducedType = "AssemblyProduced";
export declare const CodeSubmissionReceivedType = "CodeSubmissionReceived";
export declare const CommandCancelledType = "CommandCancelled";
export declare const CommandFailedType = "CommandFailed";
export declare const CommandSucceededType = "CommandSucceeded";
export declare const CompleteCodeSubmissionReceivedType = "CompleteCodeSubmissionReceived";
export declare const CompletionsProducedType = "CompletionsProduced";
export declare const DiagnosticLogEntryProducedType = "DiagnosticLogEntryProduced";
export declare const DiagnosticsProducedType = "DiagnosticsProduced";
export declare const DisplayedValueProducedType = "DisplayedValueProduced";
export declare const DisplayedValueUpdatedType = "DisplayedValueUpdated";
export declare const DocumentOpenedType = "DocumentOpened";
export declare const ErrorProducedType = "ErrorProduced";
export declare const HoverTextProducedType = "HoverTextProduced";
export declare const IncompleteCodeSubmissionReceivedType = "IncompleteCodeSubmissionReceived";
export declare const InputProducedType = "InputProduced";
export declare const KernelExtensionLoadedType = "KernelExtensionLoaded";
export declare const KernelInfoProducedType = "KernelInfoProduced";
export declare const KernelReadyType = "KernelReady";
export declare const PackageAddedType = "PackageAdded";
export declare const ProjectOpenedType = "ProjectOpened";
export declare const ReturnValueProducedType = "ReturnValueProduced";
export declare const SignatureHelpProducedType = "SignatureHelpProduced";
export declare const StandardErrorValueProducedType = "StandardErrorValueProduced";
export declare const StandardOutputValueProducedType = "StandardOutputValueProduced";
export declare const ValueInfosProducedType = "ValueInfosProduced";
export declare const ValueProducedType = "ValueProduced";
export declare const WorkingDirectoryChangedType = "WorkingDirectoryChanged";
export declare type KernelEventType = typeof AssemblyProducedType | typeof CodeSubmissionReceivedType | typeof CommandCancelledType | typeof CommandFailedType | typeof CommandSucceededType | typeof CompleteCodeSubmissionReceivedType | typeof CompletionsProducedType | typeof DiagnosticLogEntryProducedType | typeof DiagnosticsProducedType | typeof DisplayedValueProducedType | typeof DisplayedValueUpdatedType | typeof DocumentOpenedType | typeof ErrorProducedType | typeof HoverTextProducedType | typeof IncompleteCodeSubmissionReceivedType | typeof InputProducedType | typeof KernelExtensionLoadedType | typeof KernelInfoProducedType | typeof KernelReadyType | typeof PackageAddedType | typeof ProjectOpenedType | typeof ReturnValueProducedType | typeof SignatureHelpProducedType | typeof StandardErrorValueProducedType | typeof StandardOutputValueProducedType | typeof ValueInfosProducedType | typeof ValueProducedType | typeof WorkingDirectoryChangedType;
export interface AssemblyProduced extends KernelEvent {
    assembly: Base64EncodedAssembly;
}
export interface CodeSubmissionReceived extends KernelEvent {
    code: string;
}
export interface CommandCancelled extends KernelEvent {
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
export declare enum InsertTextFormat {
    PlainText = "plaintext",
    Snippet = "snippet"
}
export interface Diagnostic {
    linePositionSpan: LinePositionSpan;
    severity: DiagnosticSeverity;
    code: string;
    message: string;
}
export declare enum DiagnosticSeverity {
    Hidden = "hidden",
    Info = "info",
    Warning = "warning",
    Error = "error"
}
export interface LinePositionSpan {
    start: LinePosition;
    end: LinePosition;
}
export interface LinePosition {
    line: number;
    character: number;
}
export declare enum DocumentSerializationType {
    Dib = "dib",
    Ipynb = "ipynb"
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
export interface KernelInfo {
    aliases: Array<string>;
    languageName?: string;
    languageVersion?: string;
    localName: string;
    uri?: string;
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
    name: string;
}
export interface PackageReference {
    packageName: string;
    packageVersion: string;
    isPackageVersionSpecified: boolean;
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
    regionsContent: {
        [key: string]: string;
    };
}
export declare enum RequestType {
    Parse = "parse",
    Serialize = "serialize"
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
export declare enum SubmissionType {
    Run = "run",
    Diagnose = "diagnose"
}
export interface KernelEventEnvelope {
    eventType: KernelEventType;
    event: KernelEvent;
    command?: KernelCommandEnvelope;
}
export interface KernelCommandEnvelope {
    token?: string;
    id?: string;
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
export interface KernelCommandAndEventSender {
    submitCommand(commandEnvelope: KernelCommandEnvelope): Promise<void>;
    publishKernelEvent(eventEnvelope: KernelEventEnvelope): Promise<void>;
}
export interface KernelCommandAndEventReceiver {
    subscribeToKernelEvents(observer: KernelEventEnvelopeObserver): DisposableSubscription;
    setCommandHandler(handler: KernelCommandEnvelopeHandler): void;
}
export interface KernelCommandAndEventChannel extends KernelCommandAndEventSender, KernelCommandAndEventReceiver, Disposable {
}
