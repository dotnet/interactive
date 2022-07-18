// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

export interface Disposable {
    dispose(): void;
}

export interface DisposableSubscription extends Disposable {
}