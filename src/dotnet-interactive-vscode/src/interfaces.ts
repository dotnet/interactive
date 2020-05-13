export interface IsValidToolVersion {
    (actualVersion: string, minSupportedVersion: string): boolean;
}

export interface RawNotebookCell {
    language: string;
    contents: Array<string>;
}

export interface DocumentWithCells {
    cells: Array<RawNotebookCell>;
}

// interactive acquisition

export interface InteractiveLaunchOptions {
    args: Array<string>;
    workingDirectory: string;
}

export interface InstallInteractiveArgs {
    dotnetPath: string;
    toolVersion?: string;
}

// acquisition callbacks

export interface CreateToolManifest {
    (dotnetPath: string, globalStoragePath: string): Promise<void>;
}

export interface GetCurrentInteractiveVersion {
    (dotnetPath: string, globalStoragePath: string): Promise<string | undefined>;
}

export interface ReportInstallationStarted {
    (): void;
}

export interface InstallInteractiveTool {
    (args: InstallInteractiveArgs, globalStoragePath: string): Promise<void>;
}

export interface ReportInstallationFinished {
    (): void;
}
