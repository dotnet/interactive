define(["require", "exports"], function (require, exports) {
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
