// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import typescript from 'rollup-plugin-typescript2';
import { nodeResolve } from '@rollup/plugin-node-resolve';
import commonjs from '@rollup/plugin-commonjs';
import pkg from './package.json';

export default {

    output: [
        {
            sourcemap: 'inline',
            format: 'umd',
            name: 'polyglotNotebooks'
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
        nodeResolve({
            mainFields: ['browser', 'esnext', 'module', 'main'],
            browser: true,
            customResolveOptions: {
                moduleDirectories: ['node_modules']
            }
        }),
        commonjs()
    ],
    onwarn: function (warning, warn) {
        switch (warning.code) {
            case 'EVAL':
            case 'MISSING_NAME_OPTION_FOR_IIFE_EXPORT':
                // don't care about these
                return;
            default:
                warn(warning);
                break;
        }
    }
};
