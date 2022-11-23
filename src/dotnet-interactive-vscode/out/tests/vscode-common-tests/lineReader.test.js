"use strict";
// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
Object.defineProperty(exports, "__esModule", { value: true });
const chai_1 = require("chai");
const lineReader_1 = require("../../src/vscode-common/lineReader");
describe('line reader tests', () => {
    const lineSeparators = [
        {
            separator: '\n',
            description: 'LF'
        },
        {
            separator: '\r\n',
            description: 'CRLF'
        }
    ];
    for (const { separator, description } of lineSeparators) {
        it(`can separate lines ending with ${description}`, () => {
            let lines = [];
            let lineReader = new lineReader_1.LineReader();
            lineReader.subscribe(line => lines.push(line));
            lineReader.onData(Buffer.from('line'));
            (0, chai_1.expect)(lines.length).to.equal(0);
            lineReader.onData(Buffer.from(`1${separator}line2${separator}line`));
            (0, chai_1.expect)(lines.length).to.equal(2);
            lineReader.onData(Buffer.from(`3${separator}`));
            (0, chai_1.expect)(lines.length).to.equal(3);
            (0, chai_1.expect)(lines).to.deep.equal([
                'line1',
                'line2',
                'line3'
            ]);
        });
    }
});
//# sourceMappingURL=lineReader.test.js.map