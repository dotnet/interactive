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
exports.HelpService = exports.DotNetVersion = void 0;
const path = require("path");
const vscode = require("vscode");
exports.DotNetVersion = 'DotNetVersion';
class HelpService {
    constructor(context) {
        this.context = context;
    }
    showHelpPageAndThrow(page) {
        return __awaiter(this, void 0, void 0, function* () {
            const helpPagePath = getHelpPagePath(this.context, page);
            const helpPageUri = vscode.Uri.file(helpPagePath);
            yield vscode.commands.executeCommand('markdown.showPreview', helpPageUri);
            throw new Error('Error activating extension, see the displayed help page for more details.');
        });
    }
}
exports.HelpService = HelpService;
function getHelpPagePath(context, page) {
    const basePath = path.join(context.extensionPath, 'help');
    const helpPagePath = path.join(basePath, `${page}.md`);
    return helpPagePath;
}
//# sourceMappingURL=helpService.js.map