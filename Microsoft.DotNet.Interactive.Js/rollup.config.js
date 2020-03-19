// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import typescript from 'rollup-plugin-typescript2'
import resolve from 'rollup-plugin-node-resolve';
import commonjs from 'rollup-plugin-commonjs';
import pkg from './package.json';

export default {

    input: 'src/dotnet-interactive.ts',
    output: [
        {
            sourcemap: 'inline',
            file: pkg.main,
            format: 'umd',
            name: 'interactive',
        },
        {
            sourcemap: 'inline',
            file: pkg.module,
            format: 'es',
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
            jsnext: true,
            main: true,
            browser: true,
            customResolveOptions: {
                moduleDirectory: 'node_modules'
            }
        }),
        commonjs()
    ],
}