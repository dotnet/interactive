// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

export interface IsValidToolVersion {
    (actualVersion: string, minSupportedVersion: string): boolean;
}

export interface ProcessStart {
    command: string;
    args: Array<string>;
    workingDirectory: string;
}

// interactive acquisition

export interface InteractiveLaunchOptions {
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
    (version: string): void;
}

export interface InstallInteractiveTool {
    (args: InstallInteractiveArgs, globalStoragePath: string): Promise<void>;
}

export interface ReportInstallationFinished {
    (): void;
}
