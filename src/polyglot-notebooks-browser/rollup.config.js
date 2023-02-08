// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import typescript from 'rollup-plugin-typescript2'
import resolve from 'rollup-plugin-node-resolve';
import commonjs from 'rollup-plugin-commonjs';
import pkg from './package.json';

export default {

    output: [
        {
            sourcemap: 'inline',
            format: 'umd',
            name: 'interactive',
        }
    ],
    external: [
        //  ...Object.keys(pkg.dependencies || {}),
        ...Object.keys(pkg.peerDependencies || {}),
        ...Object.keys(pkg.devDependencies || {}),
    ],
    plugins: [
        typescript({
            typescript: require('typescript'),
            tsconfigOverride: {
                compilerOptions: {
                    "module": "ES2015"
                }
            },
        }),
        resolve({    
            mainFields: ['browser', 'esnext', 'module', 'main'],        
            browser: true,
            customResolveOptions: {
                moduleDirectory: 'node_modules'
            }
        }),
        commonjs()
    ],
}