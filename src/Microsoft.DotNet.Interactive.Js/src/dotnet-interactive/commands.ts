// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


export const SubmitCodeCommandType = "SubmitCode";
export const UpdateDisplayedValueCommandType = "UpdateDisplayedValue";
export const RequestDiagnosticsCommandType = "RequestDiagnostics";
export const RequestCompletionCommandType = "RequestCompletion";
export const DisplayValueCommandType = "DisplayValue";
export const DisplayErrorCommandType = "DisplayError";
export const CancelCurrentCommandCommandType = "CancelCurrentCommand";
export const AddPackageCommandType = "AddPackage";

export type KernelCommandType =
    typeof UpdateDisplayedValueCommandType
    | typeof RequestDiagnosticsCommandType
    | typeof RequestCompletionCommandType
    | typeof DisplayValueCommandType
    | typeof DisplayErrorCommandType
    | typeof CancelCurrentCommandCommandType
    | typeof AddPackageCommandType;

export interface KernelCommand {
    targetKernelName?: string
}

export interface AddPackage extends KernelCommand {
    packageReference: {
        packageName: string;
        packageVersion?: string;
    };
}

export interface CancelCurrentCommand extends KernelCommand {

}

export interface SubmitCode extends KernelCommand {
    code: string;
    submissionType?: number;
}


export interface DisplayValue extends KernelCommand {
    valueId?: string;
    value: any;
    formattedValue: {
        mimeType: string;
        value: string;
    }
}

export interface UpdateDisplayedValue extends KernelCommand {
    valueId: string;
    value: any;
    formattedValue: {
        mimeType: string;
        value: string;
    }
}

export interface DisplayError extends KernelCommand {
    message: string;
}

export interface RequestDiagnostics extends KernelCommand {

}

export interface RequestCompletion extends KernelCommand {
    code: string;
    cursorPosition: number;
}