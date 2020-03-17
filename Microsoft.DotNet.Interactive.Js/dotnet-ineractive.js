define("KernelClient", ["require", "exports"], function (require, exports) {
    "use strict";
    Object.defineProperty(exports, "__esModule", { value: true });
    function createClient() {
        return {
            GetVariable: function (variableName) {
                return 1;
            }
        };
    }
    exports.createClient = createClient;
});
define("dotnet-interactive", ["require", "exports", "KernelClient"], function (require, exports, KernelClient_1) {
    "use strict";
    function __export(m) {
        for (var p in m) if (!exports.hasOwnProperty(p)) exports[p] = m[p];
    }
    Object.defineProperty(exports, "__esModule", { value: true });
    __export(KernelClient_1);
});
