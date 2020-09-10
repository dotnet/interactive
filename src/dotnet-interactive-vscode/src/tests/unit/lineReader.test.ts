// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { expect } from 'chai';
import { LineReader } from '../../lineReader';

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
            let lines: Array<string> = [];
            let lineReader = new LineReader();
            lineReader.subscribe(line => lines.push(line));

            lineReader.onData(Buffer.from('line'));
            expect(lines.length).to.equal(0);
            lineReader.onData(Buffer.from(`1${separator}line2${separator}line`));
            expect(lines.length).to.equal(2);
            lineReader.onData(Buffer.from(`3${separator}`));
            expect(lines.length).to.equal(3);

            expect(lines).to.deep.equal([
                'line1',
                'line2',
                'line3'
            ]);
        });
    }
});
