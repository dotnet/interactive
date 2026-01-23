// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as fs from 'fs';
import * as path from 'path';
import { InstallInteractiveTool, InstallInteractiveArgs, CreateToolManifest, GetCurrentInteractiveVersion, InteractiveLaunchOptions, ReportInstallationStarted, ReportInstallationFinished } from './interfaces';

import { isVersionExactlyEqual } from './utilities';

// The acquisition function.  Uses predefined callbacks for external command invocations to make testing easier.
export async function acquireDotnetInteractive(
    args: InstallInteractiveArgs,
    requiredDotNetInteractiveVersion: string,
    globalStoragePath: string,
    getInteractiveVersion: GetCurrentInteractiveVersion,
    createToolManifest: CreateToolManifest,
    reportInstallationStarted: ReportInstallationStarted,
    installInteractive: InstallInteractiveTool,
    reportInstallationFinished: ReportInstallationFinished
): Promise<InteractiveLaunchOptions> {
    // Ensure `globalStoragePath` exists.  This prevents a bunch of issues with spawned processes and working directories.
    if (!fs.existsSync(globalStoragePath)) {
        fs.mkdirSync(globalStoragePath);
    }

    // create tool manifest if necessary
    const toolManifestFile = path.join(globalStoragePath, '.config', 'dotnet-tools.json');
    const alternativeToolManifestFile = path.join(globalStoragePath, 'dotnet-tools.json');
    
    const exsistAtAny = [toolManifestFile, alternativeToolManifestFile]
        .some(item => fs.existsSync(toolManifestFile));
    
    if (!exsistAtAny) {
        await createToolManifest(args.dotnetPath, globalStoragePath);
    }

    const launchOptions: InteractiveLaunchOptions = {
        workingDirectory: globalStoragePath
    };

    // determine if acquisition is necessary
    const requiredVersion = args.toolVersion ?? requiredDotNetInteractiveVersion;
    const currentVersion = await getInteractiveVersion(args.dotnetPath, globalStoragePath);
    if (currentVersion && isVersionExactlyEqual(currentVersion, requiredVersion)) {
        // current is acceptable
        return launchOptions;
    }

    // no current version installed or it's incorrect
    reportInstallationStarted(requiredVersion);
    await installInteractive({
        dotnetPath: args.dotnetPath,
        toolVersion: requiredVersion
    },
        globalStoragePath);
    reportInstallationFinished();

    return launchOptions;
}
