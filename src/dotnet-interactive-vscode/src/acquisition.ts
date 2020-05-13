import * as fs from 'fs';
import * as path from 'path';
import { InstallInteractiveTool, InstallInteractiveArgs, CreateToolManifest, GetCurrentInteractiveVersion, InteractiveLaunchOptions, ReportInstallationStarted, ReportInstallationFinished } from './interfaces';

import compareVersions = require("../node_modules/compare-versions");

// The acquisition function.  Uses predefined callbacks for external command invocations to make testing easier.
export async function acquireDotnetInteractive(
    args: InstallInteractiveArgs,
    minDotNetInteractiveVersion: string,
    globalStoragePath: string,
    getInteractiveVersion: GetCurrentInteractiveVersion,
    createToolManifest: CreateToolManifest,
    reportInstallationStarted: ReportInstallationStarted,
    installInteractive: InstallInteractiveTool,
    reportInstallationFinished: ReportInstallationFinished
    ): Promise<InteractiveLaunchOptions | undefined>
{
    // Ensure `globalStoragePath` exists.  This prevents a bunch of issues with spawned processes and working directories.
    if (!fs.existsSync(globalStoragePath)) {
        fs.mkdirSync(globalStoragePath);
    }

    // create tool manifest if necessary
    const toolManifestFile = path.join(globalStoragePath, '.config', 'dotnet-tools.json');
    if (!fs.existsSync(toolManifestFile)) {
        await createToolManifest(args.dotnetPath, globalStoragePath);
    }

    const launchOptions: InteractiveLaunchOptions = {
        workingDirectory: globalStoragePath
    };

    // determine if acquisition is necessary
    const requiredVersion = args.toolVersion ?? minDotNetInteractiveVersion;
    const currentVersion = await getInteractiveVersion(args.dotnetPath, globalStoragePath);
    if (currentVersion && compareVersions.compare(currentVersion, requiredVersion, '>=')) {
        // current is acceptable
        return launchOptions;
    }

    // no current version installed or it's out of date
    reportInstallationStarted(requiredVersion);
    await installInteractive({
            dotnetPath: args.dotnetPath,
            toolVersion: requiredVersion
        },
        globalStoragePath);
    reportInstallationFinished();

    return launchOptions;
}
