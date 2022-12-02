// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as chai from 'chai';
chai.use(require('chai-as-promised'));
const expect = chai.expect;

import * as fs from 'fs';
import * as path from 'path';

chai.use(require('chai-fs'));

describe('package.json values tests', () => {

    it(`command line arguments contain "--preview" flag`, () => {
        const packageJsonPath = path.join(__dirname, '..', '..', 'package.json');
        const packageJsonContents = fs.readFileSync(packageJsonPath, 'utf8');
        const packageJson = JSON.parse(packageJsonContents);
        expect(packageJson.contributes.configuration.properties['dotnet-interactive.kernelTransportArgs'].default).to.contain('--preview');
    });

});
