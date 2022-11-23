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
exports.provideHover = void 0;
const utilities_1 = require("../utilities");
function provideHover(clientMapper, language, documentUri, documentText, position, languageServiceDelay, token) {
    return (0, utilities_1.debounceAndReject)(`hover-${documentUri.toString()}`, languageServiceDelay, () => __awaiter(this, void 0, void 0, function* () {
        const client = yield clientMapper.getOrAddClient(documentUri);
        const hoverText = yield client.hover(language, documentText, position.line, position.character, token);
        const content = hoverText.content.sort((a, b) => mimeTypeToPriority(a.mimeType) - mimeTypeToPriority(b.mimeType))[0];
        const hoverResult = {
            contents: content.value,
            isMarkdown: content.mimeType === 'text/markdown' || content.mimeType === 'text/x-markdown',
            range: hoverText.linePositionSpan
        };
        return hoverResult;
    }));
}
exports.provideHover = provideHover;
function mimeTypeToPriority(mimeType) {
    switch (mimeType) {
        case 'text/markdown':
        case 'text/x-markdown':
            return 1;
        case 'text/plain':
            return 2;
        default:
            return 99;
    }
}
//# sourceMappingURL=hover.js.map