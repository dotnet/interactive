export function getMimeType(value: any): string {
    if (value instanceof Object) {
        return 'application/json';
    }

    return 'text/plain';
}
