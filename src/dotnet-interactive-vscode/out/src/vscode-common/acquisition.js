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
exports.acquireDotnetInteractive = void 0;
const fs = require("fs");
const path = require("path");
const utilities_1 = require("./utilities");
// The acquisition function.  Uses predefined callbacks for external command invocations to make testing easier.
function acquireDotnetInteractive(args, minDotNetInteractiveVersion, globalStoragePath, getInteractiveVersion, createToolManifest, reportInstallationStarted, installInteractive, reportInstallationFinished) {
    var _a;
    return __awaiter(this, void 0, void 0, function* () {
        // Ensure `globalStoragePath` exists.  This prevents a bunch of issues with spawned processes and working directories.
        if (!fs.existsSync(globalStoragePath)) {
            fs.mkdirSync(globalStoragePath);
        }
        // create tool manifest if necessary
        const toolManifestFile = path.join(globalStoragePath, '.config', 'dotnet-tools.json');
        if (!fs.existsSync(toolManifestFile)) {
            yield createToolManifest(args.dotnetPath, globalStoragePath);
        }
        const launchOptions = {
            workingDirectory: globalStoragePath
        };
        // determine if acquisition is necessary
        const requiredVersion = (_a = args.toolVersion) !== null && _a !== void 0 ? _a : minDotNetInteractiveVersion;
        const currentVersion = yield getInteractiveVersion(args.dotnetPath, globalStoragePath);
        if (currentVersion && (0, utilities_1.isVersionSufficient)(currentVersion, requiredVersion)) {
            // current is acceptable
            return launchOptions;
        }
        // no current version installed or it's out of date
        reportInstallationStarted(requiredVersion);
        yield installInteractive({
            dotnetPath: args.dotnetPath,
            toolVersion: requiredVersion
        }, globalStoragePath);
        reportInstallationFinished();
        return launchOptions;
    });
}
exports.acquireDotnetInteractive = acquireDotnetInteractive;
//# sourceMappingURL=acquisition.js.map