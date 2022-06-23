import * as contracts from "./contracts";
import * as kernel from "./kernel";
export declare class JavascriptKernel extends kernel.Kernel {
    private suppressedLocals;
    constructor(name?: string);
    private handleSubmitCode;
    private handleRequestValueInfos;
    private handleRequestValue;
    private allLocalVariableNames;
    private getLocalVariable;
}
export declare function formatValue(arg: any, mimeType: string): contracts.FormattedValue;
