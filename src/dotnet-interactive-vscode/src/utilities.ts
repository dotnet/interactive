export function getMimeType(value: any): string {
    if (value instanceof Object) {
        return 'application/json';
    }

    return 'text/plain';
}

export function trimTrailingCarriageReturn(value: string): string {
    if (value.endsWith('\r')) {
        return value.substr(0, value.length - 1);
    }

    return value;
}
