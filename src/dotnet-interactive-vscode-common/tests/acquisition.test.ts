// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as chai from 'chai';
chai.use(require('chai-as-promised'));
const expect = chai.expect;

import * as fs from 'fs';
import * as path from 'path';

chai.use(require('chai-fs'));

import { acquireDotnetInteractive } from '../../src/vscode-common/acquisition';
import { InstallInteractiveArgs } from '../../src/vscode-common/interfaces';
import { computeToolInstallArguments } from '../../src/vscode-common/utilities';
import { withFakeGlobalStorageLocation } from './utilities';

describe('Acquisition tests', () => {

    function getInteractiveVersionThatReturnsNoVersionFound(dotnetPath: string, globalStoragePath: string): Promise<string | undefined> {
        return new Promise<string | undefined>((resolve, reject) => {
            resolve(undefined);
        });
    }

    function getInteractiveVersionThatReturnsSpecificValue(version: string): { (dotnetPath: string, globalStoragePath: string): Promise<string | undefined> } {
        return function (dotnetPath: string, globalStoragePath: string): Promise<string | undefined> {
            return new Promise<string | undefined>((resolve, reject) => {
                resolve(version);
            });
        };
    }

    function createEmptyToolManifest(dotnetPath: string, globalStoragePath: string): Promise<void> {
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

    function createToolManifestThatThrows(dotnetPath: string, globalStoragePath: string): Promise<void> {
        throw new Error('This function should have never been called.');
    }

    function installInteractiveTool(args: InstallInteractiveArgs, globalStoragePath: string): Promise<void> {
        return new Promise((resolve, reject) => {
            const manifestPath = path.join(globalStoragePath, '.config', 'dotnet-tools.json');
            fs.readFile(manifestPath, (err, data) => {
                let manifestContent = JSON.parse(data.toString());
                manifestContent.tools['microsoft.dotnet-interactive'] = {
                    version: args!.toolVersion,
                    commands: [
                        'dotnet-interactive'
                    ]
                };
                fs.writeFile(manifestPath, JSON.stringify(manifestContent), () => resolve());
            });
        });
    }

    function installInteractiveToolWithSpecificVersion(version: string): { (args: InstallInteractiveArgs, globalStoragePath: string): Promise<void> } {
        const overrideArgs = {
            dotnetPath: 'dotnet',
            toolVersion: version
        };
        return (args: InstallInteractiveArgs, globalStoragePath: string) => {
            return installInteractiveTool(overrideArgs, globalStoragePath);
        };
    }

    function installInteractiveToolThatAlwaysThrows(args: InstallInteractiveArgs, globalStoragePath: string): Promise<void> {
        throw new Error('This function should have never been called.');
    }

    function report() {
        // noop
    }

    it("simulate first launch when global storage doesn't exist", async () => {
        await withFakeGlobalStorageLocation(false, async globalStoragePath => {
            const args = {
                dotnetPath: 'dotnet',
                toolVersion: undefined
            };
            const launchOptions = await acquireDotnetInteractive(
                args,
                '42.42.42', // min version
                globalStoragePath,
                getInteractiveVersionThatReturnsNoVersionFound, // report no version installed
                createEmptyToolManifest, // create manifest when asked
                report,
                installInteractiveToolWithSpecificVersion('42.42.42'), // 'install' this version when asked
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
        });
    });

    it("simulate global storage existing, but empty", async () => {
        await withFakeGlobalStorageLocation(true, async globalStoragePath => {
            const args = {
                dotnetPath: 'dotnet',
                toolVersion: undefined
            };
            const manifestPath = path.join(globalStoragePath, '.config', 'dotnet-tools.json');
            expect(manifestPath).to.not.be.a.path();
            const launchOptions = await acquireDotnetInteractive(
                args,
                '42.42.42', // min version
                globalStoragePath,
                getInteractiveVersionThatReturnsNoVersionFound, // report no version installed
                createEmptyToolManifest, // create manifest when asked
                report,
                installInteractiveToolWithSpecificVersion('42.42.42'), // 'install' this version when asked
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
        });
    });

    it("simulate global storage and tool manifest exist; local tool doesn't exist, but is added to the manifest", async () => {
        await withFakeGlobalStorageLocation(true, async globalStoragePath => {
            const args = {
                dotnetPath: 'dotnet',
                toolVersion: undefined
            };
            // prepopulate tool manifest
            await createEmptyToolManifest(args.dotnetPath, globalStoragePath);

            const launchOptions = await acquireDotnetInteractive(
                args,
                '42.42.42', // min version
                globalStoragePath,
                getInteractiveVersionThatReturnsNoVersionFound, // report no version found
                createToolManifestThatThrows, // throw if acquisition tries to create another manifest
                report,
                installInteractiveToolWithSpecificVersion('42.42.42'), // 'install' this version when asked
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
        });
    });

    it("simulate local tool exists, is out of date, and is updated to the auto version", async () => {
        await withFakeGlobalStorageLocation(true, async globalStoragePath => {
            const args = {
                dotnetPath: 'dotnet',
                toolVersion: undefined // install whatever you can
            };
            // prepopulate tool manifest...
            await createEmptyToolManifest(args.dotnetPath, globalStoragePath);

            const launchOptions = await acquireDotnetInteractive(
                args,
                '42.42.42', // required version
                globalStoragePath,
                getInteractiveVersionThatReturnsSpecificValue('0.0.0'), // report existing version 0.0.0 is installed
                createToolManifestThatThrows, // throw if acquisition tries to create another manifest
                report,
                installInteractiveToolWithSpecificVersion('42.42.42'), // 'install' this version when asked
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
        });
    });

    it("simulate local tool exists, is out of date, and is updated to the specified version", async () => {
        await withFakeGlobalStorageLocation(true, async globalStoragePath => {
            const args = {
                dotnetPath: 'dotnet',
                toolVersion: '42.42.42' // install exactly this
            };
            // prepopulate tool manifest...
            await createEmptyToolManifest(args.dotnetPath, globalStoragePath);

            const launchOptions = await acquireDotnetInteractive(
                args,
                '42.42.42', // required version
                globalStoragePath,
                getInteractiveVersionThatReturnsSpecificValue('0.0.0'), // report existing version 0.0.0 is installed
                createToolManifestThatThrows, // throw if acquisition tries to create another manifest
                report,
                installInteractiveToolWithSpecificVersion('42.42.42'), // 'install' this version when asked
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
        });
    });

    it("simulate local tool exists and is already up to date", async () => {
        await withFakeGlobalStorageLocation(true, async globalStoragePath => {
            const args = {
                dotnetPath: 'dotnet',
                toolVersion: '42.42.42' // install exactly this
            };
            // prepopulate tool manifest...
            await createEmptyToolManifest(args.dotnetPath, globalStoragePath);
            // ...with existing version
            await installInteractiveTool({ dotnetPath: 'dotnet', toolVersion: '42.42.42' }, globalStoragePath);

            const launchOptions = await acquireDotnetInteractive(
                args,
                '42.42.42', // required version
                globalStoragePath,
                getInteractiveVersionThatReturnsSpecificValue('42.42.42'), // report existing version 42.42.42 is installed
                createToolManifestThatThrows, // throw if acquisition tries to create another manifest
                report,
                installInteractiveToolThatAlwaysThrows, // throw if acquisition tries to install
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
        });
    });

    it("simulate local tool exists and is newer than required; version is downgraded", async () => {
        await withFakeGlobalStorageLocation(true, async globalStoragePath => {
            const args = {
                dotnetPath: 'dotnet',
                toolVersion: '42.42.42', // install exactly this
            };
            // prepopulate tool manifest...
            await createEmptyToolManifest(args.dotnetPath, globalStoragePath);
            // ...with existing version
            await installInteractiveTool({ dotnetPath: 'dotnet', toolVersion: '43.43.43' }, globalStoragePath);

            const launchOptions = await acquireDotnetInteractive(
                args,
                '42.42.42', // required version
                globalStoragePath,
                getInteractiveVersionThatReturnsSpecificValue('43.43.43'), // report existing version 43.43.43 is installed
                createToolManifestThatThrows, // throw if acquisition tries to create another manifest
                report,
                installInteractiveToolWithSpecificVersion('42.42.42'), // 'install' this version when asked
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
                        version: '42.42.42', // 43.43.43 was downgraded to 42.42.42
                        commands: [
                            'dotnet-interactive'
                        ]
                    }
                }
            });
        });
    });

    it("helper method doesn't create global storage location when it shouldn't", async () => {
        await withFakeGlobalStorageLocation(false, globalStoragePath => {
            return new Promise((resolve, reject) => {
                expect(globalStoragePath).to.not.be.a.path();
                resolve();
            });
        });
    });

    it("helper method creates global storage location when it should", async () => {
        await withFakeGlobalStorageLocation(true, globalStoragePath => {
            return new Promise((resolve, reject) => {
                expect(globalStoragePath).to.be.a.directory();
                resolve();
            });
        });
    });

    it('install arguments are computed from `undefined`', () => {
        const installArgs = computeToolInstallArguments(undefined);
        expect(installArgs).to.deep.equal({
            dotnetPath: 'dotnet',
            toolVersion: undefined,
        });
    });

    it('install arguments are computed from `string`', () => {
        const installArgs = computeToolInstallArguments('some/path/to/dotnet');
        expect(installArgs).to.deep.equal({
            dotnetPath: 'some/path/to/dotnet',
            toolVersion: undefined,
        });
    });

    it('install arguments are computed from an existing install arguments object', () => {
        const installArgs = computeToolInstallArguments({
            dotnetPath: 'some/path/to/dotnet',
            toolVersion: 'some-tool-version',
        });
        expect(installArgs).to.deep.equal({
            dotnetPath: 'some/path/to/dotnet',
            toolVersion: 'some-tool-version',
        });
    });

    it('getting existing tool version failure is properly forwarded', done => {
        withFakeGlobalStorageLocation(true, async globalStoragePath => {
            expect(acquireDotnetInteractive(
                { dotnetPath: 'dotnet' },
                '42.42.42', // minimum version necessary
                globalStoragePath,
                () => { throw new Error('simulated tool version failure'); },
                () => Promise.resolve(), // create tool manifest
                () => { }, // report started
                () => Promise.resolve(), // install tool
                () => { } // report complete
            )).eventually.rejectedWith('simulated tool version failure').notify(done);
        });
    });

    it('creating tool manifest failure is properly forwarded', done => {
        withFakeGlobalStorageLocation(true, async globalStoragePath => {
            expect(acquireDotnetInteractive(
                { dotnetPath: 'dotnet' },
                '42.42.42', // minimum version necessary
                globalStoragePath,
                () => Promise.resolve(undefined), // no tool version found
                () => { throw new Error('simulated tool manifest creation failure'); },
                () => { }, // report started
                () => Promise.resolve(), // install tool
                () => { } // report complete
            )).eventually.rejectedWith('simulated tool manifest creation failure').notify(done);
        });
    });

    it('tool install failure is properly forwarded', done => {
        withFakeGlobalStorageLocation(true, async globalStoragePath => {
            expect(acquireDotnetInteractive(
                { dotnetPath: 'dotnet' },
                '42.42.42', // minimum version necessary
                globalStoragePath,
                () => Promise.resolve(undefined), // no tool version found
                () => Promise.resolve(), // create tool manifest
                () => { }, // report started
                () => { throw new Error('simulated tool install failure'); },
                () => { } // report complete
            )).eventually.rejectedWith('simulated tool install failure').notify(done);
        });
    });
});
