// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

const { flattenDiagnosticMessageText } = require("typescript");

module.exports = function (wallaby) {
    return {
        env: {
            type: "node"
        },
        files: [
            "src/**/*.ts",
            { pattern: "tests/**/*.ts", instrument: false },         
            { pattern : "/node_modules/compare-versions/**.*", instrument: flattenDiagnosticMessageText},
            { pattern: "tests/Responses/**/*.json", instrument: false },     
            "!src/tests/**/*.test.ts*",
        ],
        tests: [
            "src/tests/unit/**/*.test.ts",
            "!src/tests/integration/**/*.test.ts"
        ],
        compilers: {
            '**/*.ts?(x)': wallaby.compilers.typeScript({ })
        },
        testFramework: 'mocha',
        debug: true,
        setup: function (wallaby) {
            var mocha = wallaby.testFramework;
            mocha.timeout(10000);
        }
    };
};