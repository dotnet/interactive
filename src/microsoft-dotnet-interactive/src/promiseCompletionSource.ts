// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

export function isPromiseCompletionSource<T>(obj: any): obj is PromiseCompletionSource<T> {
    return obj.promise
        && obj.resolve
        && obj.reject;
}

export class PromiseCompletionSource<T> {
    private _resolve: (value: T) => void = () => { };
    private _reject: (reason: any) => void = () => { };
    readonly promise: Promise<T>;

    constructor() {
        this.promise = new Promise<T>((resolve, reject) => {
            this._resolve = resolve;
            this._reject = reject;
        });
    }

    resolve(value: T) {
        this._resolve(value);
    }

    reject(reason: any) {
        this._reject(reason);
    }
}
