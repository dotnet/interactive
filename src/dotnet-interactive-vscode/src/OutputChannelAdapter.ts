import * as vscode from 'vscode';
import { ReportChannel } from "./interfaces/vscode";

export class OutputChannelAdapter implements ReportChannel{
    
    constructor(private channel: vscode.OutputChannel) {   
        
    }
    append(value: string): void {
        this.channel.append(value);
    }
    appendLine(value: string): void {
        this.channel.appendLine(value);
    }
    clear(): void {
        this.channel.clear();
    }
    show(): void {
        this.channel.show(true);
    }

    hide(): void {
        this.channel.hide();
    }
}