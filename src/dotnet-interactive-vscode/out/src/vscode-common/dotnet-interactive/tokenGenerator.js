"use strict";
// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
Object.defineProperty(exports, "__esModule", { value: true });
exports.TokenGenerator = exports.Guid = void 0;
class Guid {
    constructor(guid) {
        if (!guid) {
            throw new TypeError("Invalid argument; `value` has no value.");
        }
        this.value = Guid.EMPTY;
        if (guid && Guid.isGuid(guid)) {
            this.value = guid;
        }
    }
    static isGuid(guid) {
        const value = guid.toString();
        return guid && (guid instanceof Guid || Guid.validator.test(value));
    }
    static create() {
        return new Guid([Guid.gen(2), Guid.gen(1), Guid.gen(1), Guid.gen(1), Guid.gen(3)].join("-"));
    }
    static createEmpty() {
        return new Guid("emptyguid");
    }
    static parse(guid) {
        return new Guid(guid);
    }
    static raw() {
        return [Guid.gen(2), Guid.gen(1), Guid.gen(1), Guid.gen(1), Guid.gen(3)].join("-");
    }
    static gen(count) {
        let out = "";
        for (let i = 0; i < count; i++) {
            // tslint:disable-next-line:no-bitwise
            out += (((1 + Math.random()) * 0x10000) | 0).toString(16).substring(1);
        }
        return out;
    }
    equals(other) {
        // Comparing string `value` against provided `guid` will auto-call
        // toString on `guid` for comparison
        return Guid.isGuid(other) && this.value === other.toString();
    }
    isEmpty() {
        return this.value === Guid.EMPTY;
    }
    toString() {
        return this.value;
    }
    toJSON() {
        return {
            value: this.value,
        };
    }
}
exports.Guid = Guid;
Guid.validator = new RegExp("^[a-z0-9]{8}-[a-z0-9]{4}-[a-z0-9]{4}-[a-z0-9]{4}-[a-z0-9]{12}$", "i");
Guid.EMPTY = "00000000-0000-0000-0000-000000000000";
function setToken(commandEnvelope) {
    if (!commandEnvelope.token) {
        commandEnvelope.token = Guid.create().toString();
    }
    //
}
class TokenGenerator {
    constructor() {
        this._seed = Guid.create().toString();
        this._counter = 0;
    }
    GetNewToken() {
        this._counter++;
        return `${this._seed}::${this._counter}`;
    }
}
exports.TokenGenerator = TokenGenerator;
//# sourceMappingURL=tokenGenerator.js.map