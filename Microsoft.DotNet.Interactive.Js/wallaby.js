// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


module.exports = function (wallaby) {
    return {
        files: [
            "src/**/*.ts",
            { pattern: "tests/**/*.ts", instrument: false },            
            "!tests/**/*.spec.ts*",
            {pattern: 'tests/test-main.js', instrument: false}
        ],
        tests: ["tests/**/*.spec.ts"],
        compilers: {
            '**/*.ts?(x)': wallaby.compilers.typeScript({ 
                useStandardDefaults: true })
        },
        testFramework: 'mocha',
        debug: true,
        setup: function (wallaby) {
            var mocha = wallaby.testFramework;
            mocha.timeout(10000);
        }
    };
};
