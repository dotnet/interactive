// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { expect, use } from 'chai';
import * as fs from 'fs';
import * as path from 'path';
import * as tmp from 'tmp';

use(require('chai-fs'));

import { acquireDotnetInteractive } from '../../acquisition';
import { InstallInteractiveArgs } from '../../interfaces';
import { RecordingChannel } from '../RecordingOutputChannel';

describe('Acquisition tests', () => {

    let acquisitionChannel: RecordingChannel;

    beforeEach(() => {
        acquisitionChannel = new RecordingChannel();
    })
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
                report, 
                acquisitionChannel);

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
                report,
                acquisitionChannel);

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
                report,
                acquisitionChannel);

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

    it("simulate local tool exists, is out of date, and is updated to the auto min version", async () => {
        await withFakeGlobalStorageLocation(true, async globalStoragePath => {
            const args = {
                dotnetPath: 'dotnet',
                toolVersion: undefined // install whatever you can
            };
            // prepopulate tool manifest...
            await createEmptyToolManifest(args.dotnetPath, globalStoragePath);

            const launchOptions = await acquireDotnetInteractive(
                args,
                '42.42.42', // min version
                globalStoragePath,
                getInteractiveVersionThatReturnsSpecificValue('0.0.0'), // report existing version 0.0.0 is installed
                createToolManifestThatThrows, // throw if acquisition tries to create another manifest
                report,
                installInteractiveToolWithSpecificVersion('42.42.42'), // 'install' this version when asked
                report,
                acquisitionChannel);

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

    it("simulate local tool exists, is out of date, and is updated to the specified min version", async () => {
        await withFakeGlobalStorageLocation(true, async globalStoragePath => {
            const args = {
                dotnetPath: 'dotnet',
                toolVersion: '42.42.42' // install exactly this
            };
            // prepopulate tool manifest...
            await createEmptyToolManifest(args.dotnetPath, globalStoragePath);

            const launchOptions = await acquireDotnetInteractive(
                args,
                '42.42.42', // min version
                globalStoragePath,
                getInteractiveVersionThatReturnsSpecificValue('0.0.0'), // report existing version 0.0.0 is installed
                createToolManifestThatThrows, // throw if acquisition tries to create another manifest
                report,
                installInteractiveToolWithSpecificVersion('42.42.42'), // 'install' this version when asked
                report,
                acquisitionChannel);

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
                toolVersion: '42.42.42' // request at least this version
            };
            // prepopulate tool manifest...
            await createEmptyToolManifest(args.dotnetPath, globalStoragePath);
            // ...with existing version
            await installInteractiveTool({ dotnetPath: 'dotnet', toolVersion: '43.43.43' }, globalStoragePath);

            const launchOptions = await acquireDotnetInteractive(
                args,
                '42.42.42', // min version
                globalStoragePath,
                getInteractiveVersionThatReturnsSpecificValue('43.43.43'), // report existing version 43.43.43 is installed
                createToolManifestThatThrows, // throw if acquisition tries to create another manifest
                report,
                installInteractiveToolThatAlwaysThrows, // throw if acquisition tries to install
                report,
                acquisitionChannel);

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

    function withFakeGlobalStorageLocation(createLocation: boolean, callback: { (globalStoragePath: string): Promise<void> }) {
        return new Promise<void>((resolve, reject) => {
            tmp.dir({ unsafeCleanup: true }, (err, dir) => {
                if (err) {
                    reject();
                    throw err;
                }

                // VS Code doesn't guarantee that the global storage path is present, so we have to go one directory deeper
                let globalStoragePath = path.join(dir, 'globalStoragePath');
                if (createLocation) {
                    fs.mkdirSync(globalStoragePath);
                }

                callback(globalStoragePath).then(() => {
                    resolve();
                });
            });
        });
    };
});
