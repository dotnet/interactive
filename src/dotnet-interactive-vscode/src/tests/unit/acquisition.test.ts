import { expect, use } from 'chai';
import * as fs from 'fs';
import * as path from 'path';
import * as tmp from 'tmp';

use(require('chai-fs'));

import { acquireDotnetInteractive } from '../../acquisition';
import { InstallInteractiveArgs } from '../../interfaces';

describe('Acquisition tests', () => {

    function getInteractiveVersionThatReturnsNoVersionFound(dotnetPath: string, globalStoragePath: string): Promise<string | undefined> {
        return new Promise<string | undefined>((resolve, reject) => {
            resolve(undefined);
        });
    }

    function getInteractiveVersionThatReturnsSpecificValue(version: string): { (dotnetPath: string, globalStoragePath: string): Promise<string | undefined> } {
        return function(dotnetPath: string, globalStoragePath: string): Promise<string | undefined> {
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

    it("global storage doesn't exist", async () => {
        // this tests the scenario where the extension launches for the first time and nothing exists
        await withFakeGlobalStorageLocation(false, async globalStoragePath => {
            expect(globalStoragePath).to.not.be.a.path(); // sanity check that it doesn't exist
            const args = {
                dotnetPath: 'dotnet',
                toolVersion: undefined
            };
            const launchOptions = await acquireDotnetInteractive(
                args,
                globalStoragePath,
                getInteractiveVersionThatReturnsNoVersionFound, // report no version installed
                createEmptyToolManifest, // create manifest when asked
                report,
                installInteractiveToolWithSpecificVersion('42.42.42'), // 'install' this version when asked
                report);
            expect(launchOptions).to.deep.equal({
                args: [
                    'tool',
                    'run',
                    'dotnet-interactive',
                    '--'
                ],
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

    it("global storage exists, tool manifest doesn't", async () => {
        // this tests the scenario where the global storage may have been cleared, but the directory wasn't removed
        await withFakeGlobalStorageLocation(true, async globalStoragePath => {
            const args = {
                dotnetPath: 'dotnet',
                toolVersion: undefined
            };
            expect(globalStoragePath).to.be.a.directory(); // sanity check that it already exists
            const manifestPath = path.join(globalStoragePath, '.config', 'dotnet-tools.json');
            expect(manifestPath).to.not.be.a.path();
            const launchOptions = await acquireDotnetInteractive(
                args,
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

    it("global storage exsits, tool manifest exists, local tool doesn't exist", async () => {
        // this tests the scenario where an earlier attempt to install a local interactive tool may have been interrupted
        await withFakeGlobalStorageLocation(true, async globalStoragePath => {
            const args = {
                dotnetPath: 'dotnet',
                toolVersion: undefined
            };
            expect(globalStoragePath).to.be.a.directory(); // sanity check that it already exists
            // prepopulate tool manifest
            await createEmptyToolManifest(args.dotnetPath, globalStoragePath);

            const launchOptions = await acquireDotnetInteractive(
                args,
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

    it("global storage exists, tool manifest exists, local tool exists, but is out of date with unspecified version", async () => {
        // this tests the scenario where a local tool has already been installed, but is out of date
        await withFakeGlobalStorageLocation(true, async globalStoragePath => {
            const args = {
                dotnetPath: 'dotnet',
                toolVersion: undefined // install whatever you can
            };
            expect(globalStoragePath).to.be.a.directory(); // sanity check that it already exists
            // prepopulate tool manifest...
            await createEmptyToolManifest(args.dotnetPath, globalStoragePath);

            const launchOptions = await acquireDotnetInteractive(
                args,
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

    it("global storage exists, tool manifest exists, local tool exists, but is out of date with specified version", async () => {
        // this tests the scenario where a local tool has already been installed, but is out of date
        await withFakeGlobalStorageLocation(true, async globalStoragePath => {
            const args = {
                dotnetPath: 'dotnet',
                toolVersion: '42.42.42' // install exactly this
            };
            expect(globalStoragePath).to.be.a.directory(); // sanity check that it already exists
            // prepopulate tool manifest...
            await createEmptyToolManifest(args.dotnetPath, globalStoragePath);

            const launchOptions = await acquireDotnetInteractive(
                args,
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

    it("global storage exists, tool manifest exists, local tool exists and is up to date", async () => {
        // this tests the scenario where a local tool has already been installed and is ready to go
        await withFakeGlobalStorageLocation(true, async globalStoragePath => {
            const args = {
                dotnetPath: 'dotnet',
                toolVersion: '42.42.42' // request at least this version
            };
            expect(globalStoragePath).to.be.a.directory(); // sanity check that it already exists
            // prepopulate tool manifest...
            await createEmptyToolManifest(args.dotnetPath, globalStoragePath);
            // ...with existing version
            await installInteractiveTool({ dotnetPath: 'dotnet', toolVersion: '43.43.43' }, globalStoragePath);

            const launchOptions = await acquireDotnetInteractive(
                args,
                globalStoragePath,
                getInteractiveVersionThatReturnsSpecificValue('43.43.43'), // report existing version 43.43.43 is installed
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
                        version: '43.43.43',
                        commands: [
                            'dotnet-interactive'
                        ]
                    }
                }
            });
        });
    });

    function withFakeGlobalStorageLocation(createLocation: boolean, callback: {(globalStoragePath: string): Promise<void> }) {
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
