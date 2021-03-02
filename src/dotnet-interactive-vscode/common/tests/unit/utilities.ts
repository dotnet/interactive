// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as fs from 'fs';
import * as path from 'path';
import * as tmp from 'tmp';

export function withFakeGlobalStorageLocation(createLocation: boolean, callback: { (globalStoragePath: string): Promise<void> }) {
    return new Promise<void>((resolve, reject) => {
        tmp.dir({ unsafeCleanup: true }, (err, dir) => {
            if (err) {
                reject();
                throw err;
            }

            // VS Code doesn't guarantee that the global storage path is present, so we have to go one directory deeper
            let globalStoragePath = path.join(dir, 'globalStoragePath');
            if (createLocation) {
                fs.mkdirSync(globalStoragePath);
            }

            callback(globalStoragePath).then(() => {
                resolve();
            });
        });
    });
}
