"use strict";
// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
Object.defineProperty(exports, "__esModule", { value: true });
exports.Logger = exports.LogLevel = void 0;
var LogLevel;
(function (LogLevel) {
    LogLevel[LogLevel["Info"] = 0] = "Info";
    LogLevel[LogLevel["Warn"] = 1] = "Warn";
    LogLevel[LogLevel["Error"] = 2] = "Error";
    LogLevel[LogLevel["None"] = 3] = "None";
})(LogLevel = exports.LogLevel || (exports.LogLevel = {}));
class Logger {
    constructor(source, write) {
        this.source = source;
        this.write = write;
    }
    info(message) {
        this.write({ logLevel: LogLevel.Info, source: this.source, message });
    }
    warn(message) {
        this.write({ logLevel: LogLevel.Warn, source: this.source, message });
    }
    error(message) {
        this.write({ logLevel: LogLevel.Error, source: this.source, message });
    }
    static configure(source, writer) {
        const logger = new Logger(source, writer);
        Logger._default = logger;
    }
    static get default() {
        if (Logger._default) {
            return Logger._default;
        }
        throw new Error('No logger has been configured for this context');
    }
}
exports.Logger = Logger;
Logger._default = new Logger('default', (_entry) => { });
//# sourceMappingURL=logger.js.map