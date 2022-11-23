"use strict";
// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
var __awaiter = (this && this.__awaiter) || function (thisArg, _arguments, P, generator) {
    function adopt(value) { return value instanceof P ? value : new P(function (resolve) { resolve(value); }); }
    return new (P || (P = Promise))(function (resolve, reject) {
        function fulfilled(value) { try { step(generator.next(value)); } catch (e) { reject(e); } }
        function rejected(value) { try { step(generator["throw"](value)); } catch (e) { reject(e); } }
        function step(result) { result.done ? resolve(result.value) : adopt(result.value).then(fulfilled, rejected); }
        step((generator = generator.apply(thisArg, _arguments || [])).next());
    });
};
Object.defineProperty(exports, "__esModule", { value: true });
const chai = require("chai");
chai.use(require('chai-as-promised'));
const expect = chai.expect;
const fs = require("fs");
const path = require("path");
chai.use(require('chai-fs'));
const acquisition_1 = require("../../src/vscode-common/acquisition");
const utilities_1 = require("../../src/vscode-common/utilities");
const utilities_2 = require("./utilities");
describe('Acquisition tests', () => {
    function getInteractiveVersionThatReturnsNoVersionFound(dotnetPath, globalStoragePath) {
        return new Promise((resolve, reject) => {
            resolve(undefined);
        });
    }
    function getInteractiveVersionThatReturnsSpecificValue(version) {
        return function (dotnetPath, globalStoragePath) {
            return new Promise((resolve, reject) => {
                resolve(version);
            });
        };
    }
    function createEmptyToolManifest(dotnetPath, globalStoragePath) {
        return new Promise((resolve, reject) => {
            const manifestDir = path.join(globalStoragePath, '.config');
            fs.mkdirSync(manifestDir);
            const manifestPath = path.join(manifestDir, 'dotnet-tools.json');
            const manfiestContent = {
                version: 1,
                isRoot: true,
                tools: {}
            };
            fs.writeFile(manifestPath, JSON.stringify(manfiestContent), () => resolve());
        });
    }
    function createToolManifestThatThrows(dotnetPath, globalStoragePath) {
        throw new Error('This function should have never been called.');
    }
    function installInteractiveTool(args, globalStoragePath) {
        return new Promise((resolve, reject) => {
            const manifestPath = path.join(globalStoragePath, '.config', 'dotnet-tools.json');
            fs.readFile(manifestPath, (err, data) => {
                let manifestContent = JSON.parse(data.toString());
                manifestContent.tools['microsoft.dotnet-interactive'] = {
                    version: args.toolVersion,
                    commands: [
                        'dotnet-interactive'
                    ]
                };
                fs.writeFile(manifestPath, JSON.stringify(manifestContent), () => resolve());
            });
        });
    }
    function installInteractiveToolWithSpecificVersion(version) {
        const overrideArgs = {
            dotnetPath: 'dotnet',
            toolVersion: version
        };
        return (args, globalStoragePath) => {
            return installInteractiveTool(overrideArgs, globalStoragePath);
        };
    }
    function installInteractiveToolThatAlwaysThrows(args, globalStoragePath) {
        throw new Error('This function should have never been called.');
    }
    function report() {
        // noop
    }
    it("simulate first launch when global storage doesn't exist", () => __awaiter(void 0, void 0, void 0, function* () {
        yield (0, utilities_2.withFakeGlobalStorageLocation)(false, (globalStoragePath) => __awaiter(void 0, void 0, void 0, function* () {
            const args = {
                dotnetPath: 'dotnet',
                toolVersion: undefined
            };
            const launchOptions = yield (0, acquisition_1.acquireDotnetInteractive)(args, '42.42.42', // min version
            globalStoragePath, getInteractiveVersionThatReturnsNoVersionFound, // report no version installed
            createEmptyToolManifest, // create manifest when asked
            report, installInteractiveToolWithSpecificVersion('42.42.42'), // 'install' this version when asked
            report);
            expect(launchOptions).to.deep.equal({
                workingDirectory: globalStoragePath
            });
            expect(globalStoragePath).to.be.a.directory();
            const manifestPath = path.join(globalStoragePath, '.config', 'dotnet-tools.json');
            expect(manifestPath).to.be.file().with.json;
            const jsonContent = JSON.parse(fs.readFileSync(manifestPath).toString());
            expect(jsonContent).to.deep.equal({
                version: 1,
                isRoot: true,
                tools: {
                    'microsoft.dotnet-interactive': {
                        version: '42.42.42',
                        commands: [
                            'dotnet-interactive'
                        ]
                    }
                }
            });
        }));
    }));
    it("simulate global storage existing, but empty", () => __awaiter(void 0, void 0, void 0, function* () {
        yield (0, utilities_2.withFakeGlobalStorageLocation)(true, (globalStoragePath) => __awaiter(void 0, void 0, void 0, function* () {
            const args = {
                dotnetPath: 'dotnet',
                toolVersion: undefined
            };
            const manifestPath = path.join(globalStoragePath, '.config', 'dotnet-tools.json');
            expect(manifestPath).to.not.be.a.path();
            const launchOptions = yield (0, acquisition_1.acquireDotnetInteractive)(args, '42.42.42', // min version
            globalStoragePath, getInteractiveVersionThatReturnsNoVersionFound, // report no version installed
            createEmptyToolManifest, // create manifest when asked
            report, installInteractiveToolWithSpecificVersion('42.42.42'), // 'install' this version when asked
            report);
            expect(globalStoragePath).to.be.a.directory();
            expect(manifestPath).to.be.file().with.json;
            const jsonContent = JSON.parse(fs.readFileSync(manifestPath).toString());
            expect(jsonContent).to.deep.equal({
                version: 1,
                isRoot: true,
                tools: {
                    'microsoft.dotnet-interactive': {
                        version: '42.42.42',
                        commands: [
                            'dotnet-interactive'
                        ]
                    }
                }
            });
        }));
    }));
    it("simulate global storage and tool manifest exist; local tool doesn't exist, but is added to the manifest", () => __awaiter(void 0, void 0, void 0, function* () {
        yield (0, utilities_2.withFakeGlobalStorageLocation)(true, (globalStoragePath) => __awaiter(void 0, void 0, void 0, function* () {
            const args = {
                dotnetPath: 'dotnet',
                toolVersion: undefined
            };
            // prepopulate tool manifest
            yield createEmptyToolManifest(args.dotnetPath, globalStoragePath);
            const launchOptions = yield (0, acquisition_1.acquireDotnetInteractive)(args, '42.42.42', // min version
            globalStoragePath, getInteractiveVersionThatReturnsNoVersionFound, // report no version found
            createToolManifestThatThrows, // throw if acquisition tries to create another manifest
            report, installInteractiveToolWithSpecificVersion('42.42.42'), // 'install' this version when asked
            report);
            expect(globalStoragePath).to.be.a.directory();
            const manifestPath = path.join(globalStoragePath, '.config', 'dotnet-tools.json');
            expect(manifestPath).to.be.file().with.json;
            const jsonContent = JSON.parse(fs.readFileSync(manifestPath).toString());
            expect(jsonContent).to.deep.equal({
                version: 1,
                isRoot: true,
                tools: {
                    'microsoft.dotnet-interactive': {
                        version: '42.42.42',
                        commands: [
                            'dotnet-interactive'
                        ]
                    }
                }
            });
        }));
    }));
    it("simulate local tool exists, is out of date, and is updated to the auto min version", () => __awaiter(void 0, void 0, void 0, function* () {
        yield (0, utilities_2.withFakeGlobalStorageLocation)(true, (globalStoragePath) => __awaiter(void 0, void 0, void 0, function* () {
            const args = {
                dotnetPath: 'dotnet',
                toolVersion: undefined // install whatever you can
            };
            // prepopulate tool manifest...
            yield createEmptyToolManifest(args.dotnetPath, globalStoragePath);
            const launchOptions = yield (0, acquisition_1.acquireDotnetInteractive)(args, '42.42.42', // min version
            globalStoragePath, getInteractiveVersionThatReturnsSpecificValue('0.0.0'), // report existing version 0.0.0 is installed
            createToolManifestThatThrows, // throw if acquisition tries to create another manifest
            report, installInteractiveToolWithSpecificVersion('42.42.42'), // 'install' this version when asked
            report);
            expect(globalStoragePath).to.be.a.directory();
            const manifestPath = path.join(globalStoragePath, '.config', 'dotnet-tools.json');
            expect(manifestPath).to.be.file().with.json;
            const jsonContent = JSON.parse(fs.readFileSync(manifestPath).toString());
            expect(jsonContent).to.deep.equal({
                version: 1,
                isRoot: true,
                tools: {
                    'microsoft.dotnet-interactive': {
                        version: '42.42.42',
                        commands: [
                            'dotnet-interactive'
                        ]
                    }
                }
            });
        }));
    }));
    it("simulate local tool exists, is out of date, and is updated to the specified min version", () => __awaiter(void 0, void 0, void 0, function* () {
        yield (0, utilities_2.withFakeGlobalStorageLocation)(true, (globalStoragePath) => __awaiter(void 0, void 0, void 0, function* () {
            const args = {
                dotnetPath: 'dotnet',
                toolVersion: '42.42.42' // install exactly this
            };
            // prepopulate tool manifest...
            yield createEmptyToolManifest(args.dotnetPath, globalStoragePath);
            const launchOptions = yield (0, acquisition_1.acquireDotnetInteractive)(args, '42.42.42', // min version
            globalStoragePath, getInteractiveVersionThatReturnsSpecificValue('0.0.0'), // report existing version 0.0.0 is installed
            createToolManifestThatThrows, // throw if acquisition tries to create another manifest
            report, installInteractiveToolWithSpecificVersion('42.42.42'), // 'install' this version when asked
            report);
            expect(globalStoragePath).to.be.a.directory();
            const manifestPath = path.join(globalStoragePath, '.config', 'dotnet-tools.json');
            expect(manifestPath).to.be.file().with.json;
            const jsonContent = JSON.parse(fs.readFileSync(manifestPath).toString());
            expect(jsonContent).to.deep.equal({
                version: 1,
                isRoot: true,
                tools: {
                    'microsoft.dotnet-interactive': {
                        version: '42.42.42',
                        commands: [
                            'dotnet-interactive'
                        ]
                    }
                }
            });
        }));
    }));
    it("simulate local tool exists and is already up to date", () => __awaiter(void 0, void 0, void 0, function* () {
        yield (0, utilities_2.withFakeGlobalStorageLocation)(true, (globalStoragePath) => __awaiter(void 0, void 0, void 0, function* () {
            const args = {
                dotnetPath: 'dotnet',
                toolVersion: '42.42.42' // request at least this version
            };
            // prepopulate tool manifest...
            yield createEmptyToolManifest(args.dotnetPath, globalStoragePath);
            // ...with existing version
            yield installInteractiveTool({ dotnetPath: 'dotnet', toolVersion: '43.43.43' }, globalStoragePath);
            const launchOptions = yield (0, acquisition_1.acquireDotnetInteractive)(args, '42.42.42', // min version
            globalStoragePath, getInteractiveVersionThatReturnsSpecificValue('43.43.43'), // report existing version 43.43.43 is installed
            createToolManifestThatThrows, // throw if acquisition tries to create another manifest
            report, installInteractiveToolThatAlwaysThrows, // throw if acquisition tries to install
            report);
            expect(globalStoragePath).to.be.a.directory();
            const manifestPath = path.join(globalStoragePath, '.config', 'dotnet-tools.json');
            expect(manifestPath).to.be.file().with.json;
            const jsonContent = JSON.parse(fs.readFileSync(manifestPath).toString());
            expect(jsonContent).to.deep.equal({
                version: 1,
                isRoot: true,
                tools: {
                    'microsoft.dotnet-interactive': {
                        version: '43.43.43',
                        commands: [
                            'dotnet-interactive'
                        ]
                    }
                }
            });
        }));
    }));
    it("helper method doesn't create global storage location when it shouldn't", () => __awaiter(void 0, void 0, void 0, function* () {
        yield (0, utilities_2.withFakeGlobalStorageLocation)(false, globalStoragePath => {
            return new Promise((resolve, reject) => {
                expect(globalStoragePath).to.not.be.a.path();
                resolve();
            });
        });
    }));
    it("helper method creates global storage location when it should", () => __awaiter(void 0, void 0, void 0, function* () {
        yield (0, utilities_2.withFakeGlobalStorageLocation)(true, globalStoragePath => {
            return new Promise((resolve, reject) => {
                expect(globalStoragePath).to.be.a.directory();
                resolve();
            });
        });
    }));
    it('install arguments are computed from `undefined`', () => {
        const installArgs = (0, utilities_1.computeToolInstallArguments)(undefined);
        expect(installArgs).to.deep.equal({
            dotnetPath: 'dotnet',
            toolVersion: undefined,
        });
    });
    it('install arguments are computed from `string`', () => {
        const installArgs = (0, utilities_1.computeToolInstallArguments)('some/path/to/dotnet');
        expect(installArgs).to.deep.equal({
            dotnetPath: 'some/path/to/dotnet',
            toolVersion: undefined,
        });
    });
    it('install arguments are computed from an existing install arguments object', () => {
        const installArgs = (0, utilities_1.computeToolInstallArguments)({
            dotnetPath: 'some/path/to/dotnet',
            toolVersion: 'some-tool-version',
        });
        expect(installArgs).to.deep.equal({
            dotnetPath: 'some/path/to/dotnet',
            toolVersion: 'some-tool-version',
        });
    });
    it('getting existing tool version failure is properly forwarded', done => {
        (0, utilities_2.withFakeGlobalStorageLocation)(true, (globalStoragePath) => __awaiter(void 0, void 0, void 0, function* () {
            expect((0, acquisition_1.acquireDotnetInteractive)({ dotnetPath: 'dotnet' }, '42.42.42', // minimum version necessary
            globalStoragePath, () => { throw new Error('simulated tool version failure'); }, () => Promise.resolve(), // create tool manifest
            () => { }, // report started
            () => Promise.resolve(), // install tool
            () => { } // report complete
            )).eventually.rejectedWith('simulated tool version failure').notify(done);
        }));
    });
    it('creating tool manifest failure is properly forwarded', done => {
        (0, utilities_2.withFakeGlobalStorageLocation)(true, (globalStoragePath) => __awaiter(void 0, void 0, void 0, function* () {
            expect((0, acquisition_1.acquireDotnetInteractive)({ dotnetPath: 'dotnet' }, '42.42.42', // minimum version necessary
            globalStoragePath, () => Promise.resolve(undefined), // no tool version found
            () => { throw new Error('simulated tool manifest creation failure'); }, () => { }, // report started
            () => Promise.resolve(), // install tool
            () => { } // report complete
            )).eventually.rejectedWith('simulated tool manifest creation failure').notify(done);
        }));
    });
    it('tool install failure is properly forwarded', done => {
        (0, utilities_2.withFakeGlobalStorageLocation)(true, (globalStoragePath) => __awaiter(void 0, void 0, void 0, function* () {
            expect((0, acquisition_1.acquireDotnetInteractive)({ dotnetPath: 'dotnet' }, '42.42.42', // minimum version necessary
            globalStoragePath, () => Promise.resolve(undefined), // no tool version found
            () => Promise.resolve(), // create tool manifest
            () => { }, // report started
            () => { throw new Error('simulated tool install failure'); }, () => { } // report complete
            )).eventually.rejectedWith('simulated tool install failure').notify(done);
        }));
    });
});
//# sourceMappingURL=acquisition.test.js.map