import { ReportChannel } from "../interfaces/vscode";

export class RecordingChannel implements ReportChannel{
    channelText: string = "";
    append(value: string): void {
        this.channelText = `${this.channelText}${value}`;
    }
    appendLine(value: string): void {
        this.channelText = `${this.channelText}${value}\n`;
    }
    clear(): void {
        this.channelText = "";
    }
    show(): void {
    }
    hide(): void {
    }
}