import { LinePosition } from "./events";

export interface RequestCompletion {
    code: string;
    position: LinePosition;
}

export interface RequestHoverText {
    code: string;
    position: LinePosition;
}

export enum SubmissionType {
    run = 0,
    diagnose,
}

export interface SubmitCode {
    code: string;
    submissionType: SubmissionType
}

export type Command = RequestCompletion | RequestHoverText | SubmitCode;
