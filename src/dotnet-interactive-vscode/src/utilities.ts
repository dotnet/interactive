import { ProcessStart } from "./interfaces";

export function processArguments(template: { args: Array<string>, workingDirectory: string }, dotnetPath: string, globalStoragePath: string): ProcessStart {
    let map: { [key: string]: string } = {
        'dotnet_path': dotnetPath,
        'global_storage_path': globalStoragePath
    };
    let processed = template.args.map(a => performReplacement(a, map));
    return {
        command: processed[0],
        args: [...processed.slice(1)],
        workingDirectory: performReplacement(template.workingDirectory, map)
    };
}

function performReplacement(template: string, map: { [key: string]: string }): string {
    let result = template;
    for (let key in map) {
        let fullKey = `{${key}}`;
        result = result.replace(fullKey, map[key]);
    }

    return result;
}

export function trimTrailingCarriageReturn(value: string): string {
    if (value.endsWith('\r')) {
        return value.substr(0, value.length - 1);
    }

    return value;
}
