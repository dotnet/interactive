(function (global, factory) {
    typeof exports === 'object' && typeof module !== 'undefined' ? factory(exports) :
    typeof define === 'function' && define.amd ? define(['exports'], factory) :
    (global = global || self, factory(global.interactive = {}));
}(this, (function (exports) { 'use strict';

    var DotnetInteractiveScopeContainer = /** @class */ (function () {
        function DotnetInteractiveScopeContainer() {
        }
        return DotnetInteractiveScopeContainer;
    }());
    var DotnetInteractiveScope = /** @class */ (function () {
        function DotnetInteractiveScope() {
        }
        return DotnetInteractiveScope;
    }());

    /*! *****************************************************************************
    Copyright (c) Microsoft Corporation. All rights reserved.
    Licensed under the Apache License, Version 2.0 (the "License"); you may not use
    this file except in compliance with the License. You may obtain a copy of the
    License at http://www.apache.org/licenses/LICENSE-2.0

    THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
    KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED
    WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE,
    MERCHANTABLITY OR NON-INFRINGEMENT.

    See the Apache Version 2.0 License for specific language governing permissions
    and limitations under the License.
    ***************************************************************************** */

    function __awaiter(thisArg, _arguments, P, generator) {
        function adopt(value) { return value instanceof P ? value : new P(function (resolve) { resolve(value); }); }
        return new (P || (P = Promise))(function (resolve, reject) {
            function fulfilled(value) { try { step(generator.next(value)); } catch (e) { reject(e); } }
            function rejected(value) { try { step(generator["throw"](value)); } catch (e) { reject(e); } }
            function step(result) { result.done ? resolve(result.value) : adopt(result.value).then(fulfilled, rejected); }
            step((generator = generator.apply(thisArg, _arguments || [])).next());
        });
    }

    function __generator(thisArg, body) {
        var _ = { label: 0, sent: function() { if (t[0] & 1) throw t[1]; return t[1]; }, trys: [], ops: [] }, f, y, t, g;
        return g = { next: verb(0), "throw": verb(1), "return": verb(2) }, typeof Symbol === "function" && (g[Symbol.iterator] = function() { return this; }), g;
        function verb(n) { return function (v) { return step([n, v]); }; }
        function step(op) {
            if (f) throw new TypeError("Generator is already executing.");
            while (_) try {
                if (f = 1, y && (t = op[0] & 2 ? y["return"] : op[0] ? y["throw"] || ((t = y["return"]) && t.call(y), 0) : y.next) && !(t = t.call(y, op[1])).done) return t;
                if (y = 0, t) op = [op[0] & 2, t.value];
                switch (op[0]) {
                    case 0: case 1: t = op; break;
                    case 4: _.label++; return { value: op[1], done: false };
                    case 5: _.label++; y = op[1]; op = [0]; continue;
                    case 7: op = _.ops.pop(); _.trys.pop(); continue;
                    default:
                        if (!(t = _.trys, t = t.length > 0 && t[t.length - 1]) && (op[0] === 6 || op[0] === 2)) { _ = 0; continue; }
                        if (op[0] === 3 && (!t || (op[1] > t[0] && op[1] < t[3]))) { _.label = op[1]; break; }
                        if (op[0] === 6 && _.label < t[1]) { _.label = t[1]; t = op; break; }
                        if (t && _.label < t[2]) { _.label = t[2]; _.ops.push(op); break; }
                        if (t[2]) _.ops.pop();
                        _.trys.pop(); continue;
                }
                op = body.call(thisArg, _);
            } catch (e) { op = [6, e]; y = 0; } finally { f = t = 0; }
            if (op[0] & 5) throw op[1]; return { value: op[0] ? op[1] : void 0, done: true };
        }
    }

    // Copyright (c) .NET Foundation and contributors. All rights reserved.
    var KernelClientImpl = /** @class */ (function () {
        function KernelClientImpl(_a) {
            var clientFetch = _a.clientFetch, rootUrl = _a.rootUrl, kernelEventStream = _a.kernelEventStream;
            this._clientFetch = clientFetch;
            this._rootUrl = rootUrl;
            this._kernelEventStream = kernelEventStream;
        }
        KernelClientImpl.prototype.subscribeToKernelEvents = function (observer) {
            var subscription = this._kernelEventStream.subscribe(observer);
            return subscription;
        };
        KernelClientImpl.prototype.getVariable = function (kernelName, variableName) {
            return __awaiter(this, void 0, void 0, function () {
                var response, variable;
                return __generator(this, function (_a) {
                    switch (_a.label) {
                        case 0: return [4 /*yield*/, this._clientFetch("variables/" + kernelName + "/" + variableName, {
                                method: 'GET',
                                cache: 'no-cache',
                                mode: 'cors'
                            })];
                        case 1:
                            response = _a.sent();
                            return [4 /*yield*/, response.json()];
                        case 2:
                            variable = _a.sent();
                            return [2 /*return*/, variable];
                    }
                });
            });
        };
        KernelClientImpl.prototype.getVariables = function (variableRequest) {
            return __awaiter(this, void 0, void 0, function () {
                var response, variableBundle;
                return __generator(this, function (_a) {
                    switch (_a.label) {
                        case 0: return [4 /*yield*/, this._clientFetch("variables", {
                                method: 'POST',
                                cache: 'no-cache',
                                mode: 'cors',
                                body: JSON.stringify(variableRequest),
                                headers: {
                                    'Content-Type': 'application/json'
                                }
                            })];
                        case 1:
                            response = _a.sent();
                            return [4 /*yield*/, response.json()];
                        case 2:
                            variableBundle = _a.sent();
                            return [2 /*return*/, variableBundle];
                    }
                });
            });
        };
        KernelClientImpl.prototype.getResource = function (resource) {
            return this._clientFetch("resources/" + resource);
        };
        KernelClientImpl.prototype.getResourceUrl = function (resource) {
            var resourceUrl = this._rootUrl + "resources/" + resource;
            return resourceUrl;
        };
        KernelClientImpl.prototype.loadKernels = function () {
            return __awaiter(this, void 0, void 0, function () {
                var kernels, kernelNames, _loop_1, this_1, i;
                var _this = this;
                return __generator(this, function (_a) {
                    switch (_a.label) {
                        case 0: return [4 /*yield*/, this._clientFetch("kernels", {
                                method: "GET",
                                cache: 'no-cache',
                                mode: 'cors'
                            })];
                        case 1:
                            kernels = _a.sent();
                            return [4 /*yield*/, kernels.json()];
                        case 2:
                            kernelNames = _a.sent();
                            if (Array.isArray(kernelNames)) {
                                _loop_1 = function (i) {
                                    var kernelName = kernelNames[i];
                                    var kernelClient = {
                                        getVariable: function (variableName) {
                                            return _this.getVariable(kernelName, variableName);
                                        }
                                    };
                                    this_1[kernelName] = kernelClient;
                                };
                                this_1 = this;
                                for (i = 0; i < kernelNames.length; i++) {
                                    _loop_1(i);
                                }
                            }
                            return [2 /*return*/];
                    }
                });
            });
        };
        KernelClientImpl.prototype.submitCode = function (code, targetKernelName) {
            if (targetKernelName === void 0) { targetKernelName = null; }
            return __awaiter(this, void 0, void 0, function () {
                var token, command, response, etag;
                return __generator(this, function (_a) {
                    switch (_a.label) {
                        case 0:
                            token = null;
                            command = {
                                code: code,
                            };
                            if (targetKernelName) {
                                command.targetKernelName = targetKernelName;
                            }
                            return [4 /*yield*/, this._clientFetch("submitCode", {
                                    method: 'POST',
                                    cache: 'no-cache',
                                    mode: 'cors',
                                    body: JSON.stringify(command),
                                    headers: {
                                        'Content-Type': 'application/json'
                                    }
                                })];
                        case 1:
                            response = _a.sent();
                            etag = response.headers.get("ETag");
                            if (etag) {
                                token = etag;
                            }
                            return [2 /*return*/, token];
                    }
                });
            });
        };
        return KernelClientImpl;
    }());
    function isConfiguration(config) {
        return typeof config !== "string";
    }
    function createDotnetInteractiveClient(configuration) {
        return __awaiter(this, void 0, void 0, function () {
            function defaultClientFetch(input, requestInit) {
                if (requestInit === void 0) { requestInit = null; }
                return __awaiter(this, void 0, void 0, function () {
                    var address, response;
                    return __generator(this, function (_a) {
                        switch (_a.label) {
                            case 0:
                                address = input;
                                if (!address.startsWith("http")) {
                                    address = "" + rootUrl + address;
                                }
                                return [4 /*yield*/, fetch(address, requestInit)];
                            case 1:
                                response = _a.sent();
                                return [2 /*return*/, response];
                        }
                    });
                });
            }
            var rootUrl, clientFetch, kernelEventStreamFactory, eventStream, client;
            return __generator(this, function (_a) {
                switch (_a.label) {
                    case 0:
                        rootUrl = "";
                        clientFetch = null;
                        kernelEventStreamFactory = null;
                        if (isConfiguration(configuration)) {
                            rootUrl = configuration.address;
                            clientFetch = configuration.clientFetch;
                            kernelEventStreamFactory = configuration.kernelEventStreamFactory;
                        }
                        if (!rootUrl.endsWith("/")) {
                            rootUrl = rootUrl + "/";
                        }
                        if (!clientFetch) {
                            clientFetch = defaultClientFetch;
                        }
                        if (!kernelEventStreamFactory) {
                            kernelEventStreamFactory = kernelEventStreamFactory;
                        }
                        return [4 /*yield*/, kernelEventStreamFactory(rootUrl)];
                    case 1:
                        eventStream = _a.sent();
                        client = new KernelClientImpl({
                            clientFetch: clientFetch,
                            rootUrl: rootUrl,
                            kernelEventStream: eventStream
                        });
                        return [4 /*yield*/, client.loadKernels()];
                    case 2:
                        _a.sent();
                        return [2 /*return*/, client];
                }
            });
        });
    }

    // Copyright (c) .NET Foundation and contributors. All rights reserved.
    function init(global) {
        global.getDotnetInteractiveScope = function (key) {
            if (!global.interactiveScopes) {
                global.interactiveScopes = new DotnetInteractiveScopeContainer();
            }
            if (!global.interactiveScopes[key]) {
                global.interactiveScopes[key] = new DotnetInteractiveScope();
            }
            return global.interactiveScopes[key];
        };
        global.createDotnetInteractiveClient = createDotnetInteractiveClient;
    }

    exports.init = init;

    Object.defineProperty(exports, '__esModule', { value: true });

})));
//# sourceMappingURL=data:application/json;charset=utf-8;base64,eyJ2ZXJzaW9uIjozLCJmaWxlIjoiZG90bmV0LWludGVyYWN0aXZlLWNvZGUtbWlycm9yLmpzIiwic291cmNlcyI6W10sInNvdXJjZXNDb250ZW50IjpbXSwibmFtZXMiOltdLCJtYXBwaW5ncyI6Ijs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7OyJ9
