(function (exports) {
    'use strict';

    /******************************************************************************
    Copyright (c) Microsoft Corporation.

    Permission to use, copy, modify, and/or distribute this software for any
    purpose with or without fee is hereby granted.

    THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES WITH
    REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF MERCHANTABILITY
    AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY SPECIAL, DIRECT,
    INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING FROM
    LOSS OF USE, DATA OR PROFITS, WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR
    OTHER TORTIOUS ACTION, ARISING OUT OF OR IN CONNECTION WITH THE USE OR
    PERFORMANCE OF THIS SOFTWARE.
    ***************************************************************************** */
    /* global Reflect, Promise */

    var extendStatics = function(d, b) {
        extendStatics = Object.setPrototypeOf ||
            ({ __proto__: [] } instanceof Array && function (d, b) { d.__proto__ = b; }) ||
            function (d, b) { for (var p in b) if (Object.prototype.hasOwnProperty.call(b, p)) d[p] = b[p]; };
        return extendStatics(d, b);
    };

    function __extends(d, b) {
        if (typeof b !== "function" && b !== null)
            throw new TypeError("Class extends value " + String(b) + " is not a constructor or null");
        extendStatics(d, b);
        function __() { this.constructor = d; }
        d.prototype = b === null ? Object.create(b) : (__.prototype = b.prototype, new __());
    }

    function __awaiter(thisArg, _arguments, P, generator) {
        function adopt(value) { return value instanceof P ? value : new P(function (resolve) { resolve(value); }); }
        return new (P || (P = Promise))(function (resolve, reject) {
            function fulfilled(value) { try { step(generator.next(value)); } catch (e) { reject(e); } }
            function rejected(value) { try { step(generator["throw"](value)); } catch (e) { reject(e); } }
            function step(result) { result.done ? resolve(result.value) : adopt(result.value).then(fulfilled, rejected); }
            step((generator = generator.apply(thisArg, _arguments || [])).next());
        });
    }

    function __values(o) {
        var s = typeof Symbol === "function" && Symbol.iterator, m = s && o[s], i = 0;
        if (m) return m.call(o);
        if (o && typeof o.length === "number") return {
            next: function () {
                if (o && i >= o.length) o = void 0;
                return { value: o && o[i++], done: !o };
            }
        };
        throw new TypeError(s ? "Object is not iterable." : "Symbol.iterator is not defined.");
    }

    function __read(o, n) {
        var m = typeof Symbol === "function" && o[Symbol.iterator];
        if (!m) return o;
        var i = m.call(o), r, ar = [], e;
        try {
            while ((n === void 0 || n-- > 0) && !(r = i.next()).done) ar.push(r.value);
        }
        catch (error) { e = { error: error }; }
        finally {
            try {
                if (r && !r.done && (m = i["return"])) m.call(i);
            }
            finally { if (e) throw e.error; }
        }
        return ar;
    }

    function __spreadArray(to, from, pack) {
        if (pack || arguments.length === 2) for (var i = 0, l = from.length, ar; i < l; i++) {
            if (ar || !(i in from)) {
                if (!ar) ar = Array.prototype.slice.call(from, 0, i);
                ar[i] = from[i];
            }
        }
        return to.concat(ar || Array.prototype.slice.call(from));
    }

    var LIB;(()=>{var t={470:t=>{function e(t){if("string"!=typeof t)throw new TypeError("Path must be a string. Received "+JSON.stringify(t))}function r(t,e){for(var r,n="",o=0,i=-1,a=0,h=0;h<=t.length;++h){if(h<t.length)r=t.charCodeAt(h);else {if(47===r)break;r=47;}if(47===r){if(i===h-1||1===a);else if(i!==h-1&&2===a){if(n.length<2||2!==o||46!==n.charCodeAt(n.length-1)||46!==n.charCodeAt(n.length-2))if(n.length>2){var s=n.lastIndexOf("/");if(s!==n.length-1){-1===s?(n="",o=0):o=(n=n.slice(0,s)).length-1-n.lastIndexOf("/"),i=h,a=0;continue}}else if(2===n.length||1===n.length){n="",o=0,i=h,a=0;continue}e&&(n.length>0?n+="/..":n="..",o=2);}else n.length>0?n+="/"+t.slice(i+1,h):n=t.slice(i+1,h),o=h-i-1;i=h,a=0;}else 46===r&&-1!==a?++a:a=-1;}return n}var n={resolve:function(){for(var t,n="",o=!1,i=arguments.length-1;i>=-1&&!o;i--){var a;i>=0?a=arguments[i]:(void 0===t&&(t=process.cwd()),a=t),e(a),0!==a.length&&(n=a+"/"+n,o=47===a.charCodeAt(0));}return n=r(n,!o),o?n.length>0?"/"+n:"/":n.length>0?n:"."},normalize:function(t){if(e(t),0===t.length)return ".";var n=47===t.charCodeAt(0),o=47===t.charCodeAt(t.length-1);return 0!==(t=r(t,!n)).length||n||(t="."),t.length>0&&o&&(t+="/"),n?"/"+t:t},isAbsolute:function(t){return e(t),t.length>0&&47===t.charCodeAt(0)},join:function(){if(0===arguments.length)return ".";for(var t,r=0;r<arguments.length;++r){var o=arguments[r];e(o),o.length>0&&(void 0===t?t=o:t+="/"+o);}return void 0===t?".":n.normalize(t)},relative:function(t,r){if(e(t),e(r),t===r)return "";if((t=n.resolve(t))===(r=n.resolve(r)))return "";for(var o=1;o<t.length&&47===t.charCodeAt(o);++o);for(var i=t.length,a=i-o,h=1;h<r.length&&47===r.charCodeAt(h);++h);for(var s=r.length-h,c=a<s?a:s,f=-1,u=0;u<=c;++u){if(u===c){if(s>c){if(47===r.charCodeAt(h+u))return r.slice(h+u+1);if(0===u)return r.slice(h+u)}else a>c&&(47===t.charCodeAt(o+u)?f=u:0===u&&(f=0));break}var l=t.charCodeAt(o+u);if(l!==r.charCodeAt(h+u))break;47===l&&(f=u);}var p="";for(u=o+f+1;u<=i;++u)u!==i&&47!==t.charCodeAt(u)||(0===p.length?p+="..":p+="/..");return p.length>0?p+r.slice(h+f):(h+=f,47===r.charCodeAt(h)&&++h,r.slice(h))},_makeLong:function(t){return t},dirname:function(t){if(e(t),0===t.length)return ".";for(var r=t.charCodeAt(0),n=47===r,o=-1,i=!0,a=t.length-1;a>=1;--a)if(47===(r=t.charCodeAt(a))){if(!i){o=a;break}}else i=!1;return -1===o?n?"/":".":n&&1===o?"//":t.slice(0,o)},basename:function(t,r){if(void 0!==r&&"string"!=typeof r)throw new TypeError('"ext" argument must be a string');e(t);var n,o=0,i=-1,a=!0;if(void 0!==r&&r.length>0&&r.length<=t.length){if(r.length===t.length&&r===t)return "";var h=r.length-1,s=-1;for(n=t.length-1;n>=0;--n){var c=t.charCodeAt(n);if(47===c){if(!a){o=n+1;break}}else -1===s&&(a=!1,s=n+1),h>=0&&(c===r.charCodeAt(h)?-1==--h&&(i=n):(h=-1,i=s));}return o===i?i=s:-1===i&&(i=t.length),t.slice(o,i)}for(n=t.length-1;n>=0;--n)if(47===t.charCodeAt(n)){if(!a){o=n+1;break}}else -1===i&&(a=!1,i=n+1);return -1===i?"":t.slice(o,i)},extname:function(t){e(t);for(var r=-1,n=0,o=-1,i=!0,a=0,h=t.length-1;h>=0;--h){var s=t.charCodeAt(h);if(47!==s)-1===o&&(i=!1,o=h+1),46===s?-1===r?r=h:1!==a&&(a=1):-1!==r&&(a=-1);else if(!i){n=h+1;break}}return -1===r||-1===o||0===a||1===a&&r===o-1&&r===n+1?"":t.slice(r,o)},format:function(t){if(null===t||"object"!=typeof t)throw new TypeError('The "pathObject" argument must be of type Object. Received type '+typeof t);return function(t,e){var r=e.dir||e.root,n=e.base||(e.name||"")+(e.ext||"");return r?r===e.root?r+n:r+"/"+n:n}(0,t)},parse:function(t){e(t);var r={root:"",dir:"",base:"",ext:"",name:""};if(0===t.length)return r;var n,o=t.charCodeAt(0),i=47===o;i?(r.root="/",n=1):n=0;for(var a=-1,h=0,s=-1,c=!0,f=t.length-1,u=0;f>=n;--f)if(47!==(o=t.charCodeAt(f)))-1===s&&(c=!1,s=f+1),46===o?-1===a?a=f:1!==u&&(u=1):-1!==a&&(u=-1);else if(!c){h=f+1;break}return -1===a||-1===s||0===u||1===u&&a===s-1&&a===h+1?-1!==s&&(r.base=r.name=0===h&&i?t.slice(1,s):t.slice(h,s)):(0===h&&i?(r.name=t.slice(1,a),r.base=t.slice(1,s)):(r.name=t.slice(h,a),r.base=t.slice(h,s)),r.ext=t.slice(a,s)),h>0?r.dir=t.slice(0,h-1):i&&(r.dir="/"),r},sep:"/",delimiter:":",win32:null,posix:null};n.posix=n,t.exports=n;}},e={};function r(n){var o=e[n];if(void 0!==o)return o.exports;var i=e[n]={exports:{}};return t[n](i,i.exports,r),i.exports}r.d=(t,e)=>{for(var n in e)r.o(e,n)&&!r.o(t,n)&&Object.defineProperty(t,n,{enumerable:!0,get:e[n]});},r.o=(t,e)=>Object.prototype.hasOwnProperty.call(t,e),r.r=t=>{"undefined"!=typeof Symbol&&Symbol.toStringTag&&Object.defineProperty(t,Symbol.toStringTag,{value:"Module"}),Object.defineProperty(t,"__esModule",{value:!0});};var n={};(()=>{var t;if(r.r(n),r.d(n,{URI:()=>p,Utils:()=>_}),"object"==typeof process)t="win32"===process.platform;else if("object"==typeof navigator){var e=navigator.userAgent;t=e.indexOf("Windows")>=0;}var o,i,a=(o=function(t,e){return o=Object.setPrototypeOf||{__proto__:[]}instanceof Array&&function(t,e){t.__proto__=e;}||function(t,e){for(var r in e)Object.prototype.hasOwnProperty.call(e,r)&&(t[r]=e[r]);},o(t,e)},function(t,e){if("function"!=typeof e&&null!==e)throw new TypeError("Class extends value "+String(e)+" is not a constructor or null");function r(){this.constructor=t;}o(t,e),t.prototype=null===e?Object.create(e):(r.prototype=e.prototype,new r);}),h=/^\w[\w\d+.-]*$/,s=/^\//,c=/^\/\//,f="",u="/",l=/^(([^:/?#]+?):)?(\/\/([^/?#]*))?([^?#]*)(\?([^#]*))?(#(.*))?/,p=function(){function e(t,e,r,n,o,i){void 0===i&&(i=!1),"object"==typeof t?(this.scheme=t.scheme||f,this.authority=t.authority||f,this.path=t.path||f,this.query=t.query||f,this.fragment=t.fragment||f):(this.scheme=function(t,e){return t||e?t:"file"}(t,i),this.authority=e||f,this.path=function(t,e){switch(t){case"https":case"http":case"file":e?e[0]!==u&&(e=u+e):e=u;}return e}(this.scheme,r||f),this.query=n||f,this.fragment=o||f,function(t,e){if(!t.scheme&&e)throw new Error('[UriError]: Scheme is missing: {scheme: "", authority: "'.concat(t.authority,'", path: "').concat(t.path,'", query: "').concat(t.query,'", fragment: "').concat(t.fragment,'"}'));if(t.scheme&&!h.test(t.scheme))throw new Error("[UriError]: Scheme contains illegal characters.");if(t.path)if(t.authority){if(!s.test(t.path))throw new Error('[UriError]: If a URI contains an authority component, then the path component must either be empty or begin with a slash ("/") character')}else if(c.test(t.path))throw new Error('[UriError]: If a URI does not contain an authority component, then the path cannot begin with two slash characters ("//")')}(this,i));}return e.isUri=function(t){return t instanceof e||!!t&&"string"==typeof t.authority&&"string"==typeof t.fragment&&"string"==typeof t.path&&"string"==typeof t.query&&"string"==typeof t.scheme&&"string"==typeof t.fsPath&&"function"==typeof t.with&&"function"==typeof t.toString},Object.defineProperty(e.prototype,"fsPath",{get:function(){return b(this,!1)},enumerable:!1,configurable:!0}),e.prototype.with=function(t){if(!t)return this;var e=t.scheme,r=t.authority,n=t.path,o=t.query,i=t.fragment;return void 0===e?e=this.scheme:null===e&&(e=f),void 0===r?r=this.authority:null===r&&(r=f),void 0===n?n=this.path:null===n&&(n=f),void 0===o?o=this.query:null===o&&(o=f),void 0===i?i=this.fragment:null===i&&(i=f),e===this.scheme&&r===this.authority&&n===this.path&&o===this.query&&i===this.fragment?this:new d(e,r,n,o,i)},e.parse=function(t,e){void 0===e&&(e=!1);var r=l.exec(t);return r?new d(r[2]||f,x(r[4]||f),x(r[5]||f),x(r[7]||f),x(r[9]||f),e):new d(f,f,f,f,f)},e.file=function(e){var r=f;if(t&&(e=e.replace(/\\/g,u)),e[0]===u&&e[1]===u){var n=e.indexOf(u,2);-1===n?(r=e.substring(2),e=u):(r=e.substring(2,n),e=e.substring(n)||u);}return new d("file",r,e,f,f)},e.from=function(t){return new d(t.scheme,t.authority,t.path,t.query,t.fragment)},e.prototype.toString=function(t){return void 0===t&&(t=!1),C(this,t)},e.prototype.toJSON=function(){return this},e.revive=function(t){if(t){if(t instanceof e)return t;var r=new d(t);return r._formatted=t.external,r._fsPath=t._sep===g?t.fsPath:null,r}return t},e}(),g=t?1:void 0,d=function(t){function e(){var e=null!==t&&t.apply(this,arguments)||this;return e._formatted=null,e._fsPath=null,e}return a(e,t),Object.defineProperty(e.prototype,"fsPath",{get:function(){return this._fsPath||(this._fsPath=b(this,!1)),this._fsPath},enumerable:!1,configurable:!0}),e.prototype.toString=function(t){return void 0===t&&(t=!1),t?C(this,!0):(this._formatted||(this._formatted=C(this,!1)),this._formatted)},e.prototype.toJSON=function(){var t={$mid:1};return this._fsPath&&(t.fsPath=this._fsPath,t._sep=g),this._formatted&&(t.external=this._formatted),this.path&&(t.path=this.path),this.scheme&&(t.scheme=this.scheme),this.authority&&(t.authority=this.authority),this.query&&(t.query=this.query),this.fragment&&(t.fragment=this.fragment),t},e}(p),v=((i={})[58]="%3A",i[47]="%2F",i[63]="%3F",i[35]="%23",i[91]="%5B",i[93]="%5D",i[64]="%40",i[33]="%21",i[36]="%24",i[38]="%26",i[39]="%27",i[40]="%28",i[41]="%29",i[42]="%2A",i[43]="%2B",i[44]="%2C",i[59]="%3B",i[61]="%3D",i[32]="%20",i);function y(t,e){for(var r=void 0,n=-1,o=0;o<t.length;o++){var i=t.charCodeAt(o);if(i>=97&&i<=122||i>=65&&i<=90||i>=48&&i<=57||45===i||46===i||95===i||126===i||e&&47===i)-1!==n&&(r+=encodeURIComponent(t.substring(n,o)),n=-1),void 0!==r&&(r+=t.charAt(o));else {void 0===r&&(r=t.substr(0,o));var a=v[i];void 0!==a?(-1!==n&&(r+=encodeURIComponent(t.substring(n,o)),n=-1),r+=a):-1===n&&(n=o);}}return -1!==n&&(r+=encodeURIComponent(t.substring(n))),void 0!==r?r:t}function m(t){for(var e=void 0,r=0;r<t.length;r++){var n=t.charCodeAt(r);35===n||63===n?(void 0===e&&(e=t.substr(0,r)),e+=v[n]):void 0!==e&&(e+=t[r]);}return void 0!==e?e:t}function b(e,r){var n;return n=e.authority&&e.path.length>1&&"file"===e.scheme?"//".concat(e.authority).concat(e.path):47===e.path.charCodeAt(0)&&(e.path.charCodeAt(1)>=65&&e.path.charCodeAt(1)<=90||e.path.charCodeAt(1)>=97&&e.path.charCodeAt(1)<=122)&&58===e.path.charCodeAt(2)?r?e.path.substr(1):e.path[1].toLowerCase()+e.path.substr(2):e.path,t&&(n=n.replace(/\//g,"\\")),n}function C(t,e){var r=e?m:y,n="",o=t.scheme,i=t.authority,a=t.path,h=t.query,s=t.fragment;if(o&&(n+=o,n+=":"),(i||"file"===o)&&(n+=u,n+=u),i){var c=i.indexOf("@");if(-1!==c){var f=i.substr(0,c);i=i.substr(c+1),-1===(c=f.indexOf(":"))?n+=r(f,!1):(n+=r(f.substr(0,c),!1),n+=":",n+=r(f.substr(c+1),!1)),n+="@";}-1===(c=(i=i.toLowerCase()).indexOf(":"))?n+=r(i,!1):(n+=r(i.substr(0,c),!1),n+=i.substr(c));}if(a){if(a.length>=3&&47===a.charCodeAt(0)&&58===a.charCodeAt(2))(l=a.charCodeAt(1))>=65&&l<=90&&(a="/".concat(String.fromCharCode(l+32),":").concat(a.substr(3)));else if(a.length>=2&&58===a.charCodeAt(1)){var l;(l=a.charCodeAt(0))>=65&&l<=90&&(a="".concat(String.fromCharCode(l+32),":").concat(a.substr(2)));}n+=r(a,!0);}return h&&(n+="?",n+=r(h,!1)),s&&(n+="#",n+=e?s:y(s,!1)),n}function A(t){try{return decodeURIComponent(t)}catch(e){return t.length>3?t.substr(0,3)+A(t.substr(3)):t}}var w=/(%[0-9A-Za-z][0-9A-Za-z])+/g;function x(t){return t.match(w)?t.replace(w,(function(t){return A(t)})):t}var _,O=r(470),P=function(t,e,r){if(r||2===arguments.length)for(var n,o=0,i=e.length;o<i;o++)!n&&o in e||(n||(n=Array.prototype.slice.call(e,0,o)),n[o]=e[o]);return t.concat(n||Array.prototype.slice.call(e))},j=O.posix||O,U="/";!function(t){t.joinPath=function(t){for(var e=[],r=1;r<arguments.length;r++)e[r-1]=arguments[r];return t.with({path:j.join.apply(j,P([t.path],e,!1))})},t.resolvePath=function(t){for(var e=[],r=1;r<arguments.length;r++)e[r-1]=arguments[r];var n=t.path,o=!1;n[0]!==U&&(n=U+n,o=!0);var i=j.resolve.apply(j,P([n],e,!1));return o&&i[0]===U&&!t.authority&&(i=i.substring(1)),t.with({path:i})},t.dirname=function(t){if(0===t.path.length||t.path===U)return t;var e=j.dirname(t.path);return 1===e.length&&46===e.charCodeAt(0)&&(e=""),t.with({path:e})},t.basename=function(t){return j.basename(t.path)},t.extname=function(t){return j.extname(t.path)};}(_||(_={}));})(),LIB=n;})();const{URI,Utils}=LIB;

    // Copyright (c) .NET Foundation and contributors. All rights reserved.
    function createKernelUri(kernelUri) {
        const uri = URI.parse(kernelUri);
        uri.authority; //?
        uri.path; //?
        let absoluteUri = `${uri.scheme}://${uri.authority}${uri.path || "/"}`;
        return absoluteUri; //?
    }
    function createKernelUriWithQuery(kernelUri) {
        const uri = URI.parse(kernelUri);
        uri.authority; //?
        uri.path; //?
        let absoluteUri = `${uri.scheme}://${uri.authority}${uri.path || "/"}`;
        if (uri.query) {
            absoluteUri += `?${uri.query}`;
        }
        return absoluteUri; //?
    }
    function stampCommandRoutingSlipAsArrived(kernelCommandEnvelope, kernelUri) {
        stampCommandRoutingSlipAs(kernelCommandEnvelope, kernelUri, "arrived");
    }
    function stampCommandRoutingSlip(kernelCommandEnvelope, kernelUri) {
        if (kernelCommandEnvelope.routingSlip === undefined || kernelCommandEnvelope.routingSlip === null) {
            throw new Error("The command does not have a routing slip");
        }
        kernelCommandEnvelope.routingSlip; //?
        let absoluteUri = createKernelUri(kernelUri); //?
        if (kernelCommandEnvelope.routingSlip.find(e => e === absoluteUri)) {
            throw Error(`The uri ${absoluteUri} is already in the routing slip [${kernelCommandEnvelope.routingSlip}]`);
        }
        else if (kernelCommandEnvelope.routingSlip.find(e => e.startsWith(absoluteUri))) {
            kernelCommandEnvelope.routingSlip.push(absoluteUri);
        }
        else {
            throw new Error(`The uri ${absoluteUri} is not in the routing slip [${kernelCommandEnvelope.routingSlip}]`);
        }
    }
    function stampEventRoutingSlip(kernelEventEnvelope, kernelUri) {
        stampRoutingSlip(kernelEventEnvelope, kernelUri);
    }
    function stampCommandRoutingSlipAs(kernelCommandOrEventEnvelope, kernelUri, tag) {
        const absoluteUri = `${createKernelUri(kernelUri)}?tag=${tag}`; //?
        stampRoutingSlip(kernelCommandOrEventEnvelope, absoluteUri);
    }
    function stampRoutingSlip(kernelCommandOrEventEnvelope, kernelUri) {
        if (kernelCommandOrEventEnvelope.routingSlip === undefined || kernelCommandOrEventEnvelope.routingSlip === null) {
            kernelCommandOrEventEnvelope.routingSlip = [];
        }
        const normalizedUri = createKernelUriWithQuery(kernelUri);
        const canAdd = !kernelCommandOrEventEnvelope.routingSlip.find(e => createKernelUriWithQuery(e) === normalizedUri);
        if (canAdd) {
            kernelCommandOrEventEnvelope.routingSlip.push(normalizedUri);
            kernelCommandOrEventEnvelope.routingSlip; //?
        }
        else {
            throw new Error(`The uri ${normalizedUri} is already in the routing slip [${kernelCommandOrEventEnvelope.routingSlip}]`);
        }
    }
    function continueRoutingSlip(kernelCommandOrEventEnvelope, kernelUris) {
        if (kernelCommandOrEventEnvelope.routingSlip === undefined || kernelCommandOrEventEnvelope.routingSlip === null) {
            kernelCommandOrEventEnvelope.routingSlip = [];
        }
        let toContinue = createRoutingSlip(kernelUris);
        if (routingSlipStartsWith(toContinue, kernelCommandOrEventEnvelope.routingSlip)) {
            toContinue = toContinue.slice(kernelCommandOrEventEnvelope.routingSlip.length);
        }
        const original = [...kernelCommandOrEventEnvelope.routingSlip];
        for (let i = 0; i < toContinue.length; i++) {
            const normalizedUri = toContinue[i]; //?
            const canAdd = !kernelCommandOrEventEnvelope.routingSlip.find(e => createKernelUri(e) === normalizedUri);
            if (canAdd) {
                kernelCommandOrEventEnvelope.routingSlip.push(normalizedUri);
            }
            else {
                throw new Error(`The uri ${normalizedUri} is already in the routing slip [${original}], cannot continue with routing slip [${kernelUris.map(e => createKernelUri(e))}]`);
            }
        }
    }
    function continueCommandRoutingSlip(kernelCommandEnvelope, kernelUris) {
        continueRoutingSlip(kernelCommandEnvelope, kernelUris);
    }
    function createRoutingSlip(kernelUris) {
        return Array.from(new Set(kernelUris.map(e => createKernelUri(e))));
    }
    function routingSlipStartsWith(thisKernelUris, otherKernelUris) {
        let startsWith = true;
        if (otherKernelUris.length > 0 && thisKernelUris.length >= otherKernelUris.length) {
            for (let i = 0; i < otherKernelUris.length; i++) {
                if (createKernelUri(otherKernelUris[i]) !== createKernelUri(thisKernelUris[i])) {
                    startsWith = false;
                    break;
                }
            }
        }
        else {
            startsWith = false;
        }
        return startsWith;
    }
    function eventRoutingSlipContains(kernlEvent, kernelUri, ignoreQuery = false) {
        return routingSlipContains(kernlEvent, kernelUri, ignoreQuery);
    }
    function commandRoutingSlipContains(kernlEvent, kernelUri, ignoreQuery = false) {
        return routingSlipContains(kernlEvent, kernelUri, ignoreQuery);
    }
    function routingSlipContains(kernelCommandOrEventEnvelope, kernelUri, ignoreQuery = false) {
        var _a;
        const normalizedUri = ignoreQuery ? createKernelUri(kernelUri) : createKernelUriWithQuery(kernelUri);
        return ((_a = kernelCommandOrEventEnvelope === null || kernelCommandOrEventEnvelope === void 0 ? void 0 : kernelCommandOrEventEnvelope.routingSlip) === null || _a === void 0 ? void 0 : _a.find(e => normalizedUri === (!ignoreQuery ? createKernelUriWithQuery(e) : createKernelUri(e)))) !== undefined;
    }

    // Copyright (c) .NET Foundation and contributors. All rights reserved.
    const RequestKernelInfoType = "RequestKernelInfo";
    const RequestValueType = "RequestValue";
    const RequestValueInfosType = "RequestValueInfos";
    const SendValueType = "SendValue";
    const SubmitCodeType = "SubmitCode";
    const CodeSubmissionReceivedType = "CodeSubmissionReceived";
    const CommandCancelledType = "CommandCancelled";
    const CommandFailedType = "CommandFailed";
    const CommandSucceededType = "CommandSucceeded";
    const DisplayedValueProducedType = "DisplayedValueProduced";
    const KernelInfoProducedType = "KernelInfoProduced";
    const KernelReadyType = "KernelReady";
    const ReturnValueProducedType = "ReturnValueProduced";
    const ValueInfosProducedType = "ValueInfosProduced";
    const ValueProducedType = "ValueProduced";
    var InsertTextFormat;
    (function (InsertTextFormat) {
        InsertTextFormat["PlainText"] = "plaintext";
        InsertTextFormat["Snippet"] = "snippet";
    })(InsertTextFormat || (InsertTextFormat = {}));
    var DiagnosticSeverity;
    (function (DiagnosticSeverity) {
        DiagnosticSeverity["Hidden"] = "hidden";
        DiagnosticSeverity["Info"] = "info";
        DiagnosticSeverity["Warning"] = "warning";
        DiagnosticSeverity["Error"] = "error";
    })(DiagnosticSeverity || (DiagnosticSeverity = {}));
    var DocumentSerializationType;
    (function (DocumentSerializationType) {
        DocumentSerializationType["Dib"] = "dib";
        DocumentSerializationType["Ipynb"] = "ipynb";
    })(DocumentSerializationType || (DocumentSerializationType = {}));
    var RequestType;
    (function (RequestType) {
        RequestType["Parse"] = "parse";
        RequestType["Serialize"] = "serialize";
    })(RequestType || (RequestType = {}));
    var SubmissionType;
    (function (SubmissionType) {
        SubmissionType["Run"] = "run";
        SubmissionType["Diagnose"] = "diagnose";
    })(SubmissionType || (SubmissionType = {}));

    function isFunction(value) {
        return typeof value === 'function';
    }

    function createErrorClass(createImpl) {
        var _super = function (instance) {
            Error.call(instance);
            instance.stack = new Error().stack;
        };
        var ctorFunc = createImpl(_super);
        ctorFunc.prototype = Object.create(Error.prototype);
        ctorFunc.prototype.constructor = ctorFunc;
        return ctorFunc;
    }

    var UnsubscriptionError = createErrorClass(function (_super) {
        return function UnsubscriptionErrorImpl(errors) {
            _super(this);
            this.message = errors
                ? errors.length + " errors occurred during unsubscription:\n" + errors.map(function (err, i) { return i + 1 + ") " + err.toString(); }).join('\n  ')
                : '';
            this.name = 'UnsubscriptionError';
            this.errors = errors;
        };
    });

    function arrRemove(arr, item) {
        if (arr) {
            var index = arr.indexOf(item);
            0 <= index && arr.splice(index, 1);
        }
    }

    var Subscription = (function () {
        function Subscription(initialTeardown) {
            this.initialTeardown = initialTeardown;
            this.closed = false;
            this._parentage = null;
            this._finalizers = null;
        }
        Subscription.prototype.unsubscribe = function () {
            var e_1, _a, e_2, _b;
            var errors;
            if (!this.closed) {
                this.closed = true;
                var _parentage = this._parentage;
                if (_parentage) {
                    this._parentage = null;
                    if (Array.isArray(_parentage)) {
                        try {
                            for (var _parentage_1 = __values(_parentage), _parentage_1_1 = _parentage_1.next(); !_parentage_1_1.done; _parentage_1_1 = _parentage_1.next()) {
                                var parent_1 = _parentage_1_1.value;
                                parent_1.remove(this);
                            }
                        }
                        catch (e_1_1) { e_1 = { error: e_1_1 }; }
                        finally {
                            try {
                                if (_parentage_1_1 && !_parentage_1_1.done && (_a = _parentage_1.return)) _a.call(_parentage_1);
                            }
                            finally { if (e_1) throw e_1.error; }
                        }
                    }
                    else {
                        _parentage.remove(this);
                    }
                }
                var initialFinalizer = this.initialTeardown;
                if (isFunction(initialFinalizer)) {
                    try {
                        initialFinalizer();
                    }
                    catch (e) {
                        errors = e instanceof UnsubscriptionError ? e.errors : [e];
                    }
                }
                var _finalizers = this._finalizers;
                if (_finalizers) {
                    this._finalizers = null;
                    try {
                        for (var _finalizers_1 = __values(_finalizers), _finalizers_1_1 = _finalizers_1.next(); !_finalizers_1_1.done; _finalizers_1_1 = _finalizers_1.next()) {
                            var finalizer = _finalizers_1_1.value;
                            try {
                                execFinalizer(finalizer);
                            }
                            catch (err) {
                                errors = errors !== null && errors !== void 0 ? errors : [];
                                if (err instanceof UnsubscriptionError) {
                                    errors = __spreadArray(__spreadArray([], __read(errors)), __read(err.errors));
                                }
                                else {
                                    errors.push(err);
                                }
                            }
                        }
                    }
                    catch (e_2_1) { e_2 = { error: e_2_1 }; }
                    finally {
                        try {
                            if (_finalizers_1_1 && !_finalizers_1_1.done && (_b = _finalizers_1.return)) _b.call(_finalizers_1);
                        }
                        finally { if (e_2) throw e_2.error; }
                    }
                }
                if (errors) {
                    throw new UnsubscriptionError(errors);
                }
            }
        };
        Subscription.prototype.add = function (teardown) {
            var _a;
            if (teardown && teardown !== this) {
                if (this.closed) {
                    execFinalizer(teardown);
                }
                else {
                    if (teardown instanceof Subscription) {
                        if (teardown.closed || teardown._hasParent(this)) {
                            return;
                        }
                        teardown._addParent(this);
                    }
                    (this._finalizers = (_a = this._finalizers) !== null && _a !== void 0 ? _a : []).push(teardown);
                }
            }
        };
        Subscription.prototype._hasParent = function (parent) {
            var _parentage = this._parentage;
            return _parentage === parent || (Array.isArray(_parentage) && _parentage.includes(parent));
        };
        Subscription.prototype._addParent = function (parent) {
            var _parentage = this._parentage;
            this._parentage = Array.isArray(_parentage) ? (_parentage.push(parent), _parentage) : _parentage ? [_parentage, parent] : parent;
        };
        Subscription.prototype._removeParent = function (parent) {
            var _parentage = this._parentage;
            if (_parentage === parent) {
                this._parentage = null;
            }
            else if (Array.isArray(_parentage)) {
                arrRemove(_parentage, parent);
            }
        };
        Subscription.prototype.remove = function (teardown) {
            var _finalizers = this._finalizers;
            _finalizers && arrRemove(_finalizers, teardown);
            if (teardown instanceof Subscription) {
                teardown._removeParent(this);
            }
        };
        Subscription.EMPTY = (function () {
            var empty = new Subscription();
            empty.closed = true;
            return empty;
        })();
        return Subscription;
    }());
    var EMPTY_SUBSCRIPTION = Subscription.EMPTY;
    function isSubscription(value) {
        return (value instanceof Subscription ||
            (value && 'closed' in value && isFunction(value.remove) && isFunction(value.add) && isFunction(value.unsubscribe)));
    }
    function execFinalizer(finalizer) {
        if (isFunction(finalizer)) {
            finalizer();
        }
        else {
            finalizer.unsubscribe();
        }
    }

    var config = {
        onUnhandledError: null,
        onStoppedNotification: null,
        Promise: undefined,
        useDeprecatedSynchronousErrorHandling: false,
        useDeprecatedNextContext: false,
    };

    var timeoutProvider = {
        setTimeout: function (handler, timeout) {
            var args = [];
            for (var _i = 2; _i < arguments.length; _i++) {
                args[_i - 2] = arguments[_i];
            }
            var delegate = timeoutProvider.delegate;
            if (delegate === null || delegate === void 0 ? void 0 : delegate.setTimeout) {
                return delegate.setTimeout.apply(delegate, __spreadArray([handler, timeout], __read(args)));
            }
            return setTimeout.apply(void 0, __spreadArray([handler, timeout], __read(args)));
        },
        clearTimeout: function (handle) {
            var delegate = timeoutProvider.delegate;
            return ((delegate === null || delegate === void 0 ? void 0 : delegate.clearTimeout) || clearTimeout)(handle);
        },
        delegate: undefined,
    };

    function reportUnhandledError(err) {
        timeoutProvider.setTimeout(function () {
            {
                throw err;
            }
        });
    }

    function noop() { }

    var context = null;
    function errorContext(cb) {
        if (config.useDeprecatedSynchronousErrorHandling) {
            var isRoot = !context;
            if (isRoot) {
                context = { errorThrown: false, error: null };
            }
            cb();
            if (isRoot) {
                var _a = context, errorThrown = _a.errorThrown, error = _a.error;
                context = null;
                if (errorThrown) {
                    throw error;
                }
            }
        }
        else {
            cb();
        }
    }

    var Subscriber = (function (_super) {
        __extends(Subscriber, _super);
        function Subscriber(destination) {
            var _this = _super.call(this) || this;
            _this.isStopped = false;
            if (destination) {
                _this.destination = destination;
                if (isSubscription(destination)) {
                    destination.add(_this);
                }
            }
            else {
                _this.destination = EMPTY_OBSERVER;
            }
            return _this;
        }
        Subscriber.create = function (next, error, complete) {
            return new SafeSubscriber(next, error, complete);
        };
        Subscriber.prototype.next = function (value) {
            if (this.isStopped) ;
            else {
                this._next(value);
            }
        };
        Subscriber.prototype.error = function (err) {
            if (this.isStopped) ;
            else {
                this.isStopped = true;
                this._error(err);
            }
        };
        Subscriber.prototype.complete = function () {
            if (this.isStopped) ;
            else {
                this.isStopped = true;
                this._complete();
            }
        };
        Subscriber.prototype.unsubscribe = function () {
            if (!this.closed) {
                this.isStopped = true;
                _super.prototype.unsubscribe.call(this);
                this.destination = null;
            }
        };
        Subscriber.prototype._next = function (value) {
            this.destination.next(value);
        };
        Subscriber.prototype._error = function (err) {
            try {
                this.destination.error(err);
            }
            finally {
                this.unsubscribe();
            }
        };
        Subscriber.prototype._complete = function () {
            try {
                this.destination.complete();
            }
            finally {
                this.unsubscribe();
            }
        };
        return Subscriber;
    }(Subscription));
    var _bind = Function.prototype.bind;
    function bind(fn, thisArg) {
        return _bind.call(fn, thisArg);
    }
    var ConsumerObserver = (function () {
        function ConsumerObserver(partialObserver) {
            this.partialObserver = partialObserver;
        }
        ConsumerObserver.prototype.next = function (value) {
            var partialObserver = this.partialObserver;
            if (partialObserver.next) {
                try {
                    partialObserver.next(value);
                }
                catch (error) {
                    handleUnhandledError(error);
                }
            }
        };
        ConsumerObserver.prototype.error = function (err) {
            var partialObserver = this.partialObserver;
            if (partialObserver.error) {
                try {
                    partialObserver.error(err);
                }
                catch (error) {
                    handleUnhandledError(error);
                }
            }
            else {
                handleUnhandledError(err);
            }
        };
        ConsumerObserver.prototype.complete = function () {
            var partialObserver = this.partialObserver;
            if (partialObserver.complete) {
                try {
                    partialObserver.complete();
                }
                catch (error) {
                    handleUnhandledError(error);
                }
            }
        };
        return ConsumerObserver;
    }());
    var SafeSubscriber = (function (_super) {
        __extends(SafeSubscriber, _super);
        function SafeSubscriber(observerOrNext, error, complete) {
            var _this = _super.call(this) || this;
            var partialObserver;
            if (isFunction(observerOrNext) || !observerOrNext) {
                partialObserver = {
                    next: (observerOrNext !== null && observerOrNext !== void 0 ? observerOrNext : undefined),
                    error: error !== null && error !== void 0 ? error : undefined,
                    complete: complete !== null && complete !== void 0 ? complete : undefined,
                };
            }
            else {
                var context_1;
                if (_this && config.useDeprecatedNextContext) {
                    context_1 = Object.create(observerOrNext);
                    context_1.unsubscribe = function () { return _this.unsubscribe(); };
                    partialObserver = {
                        next: observerOrNext.next && bind(observerOrNext.next, context_1),
                        error: observerOrNext.error && bind(observerOrNext.error, context_1),
                        complete: observerOrNext.complete && bind(observerOrNext.complete, context_1),
                    };
                }
                else {
                    partialObserver = observerOrNext;
                }
            }
            _this.destination = new ConsumerObserver(partialObserver);
            return _this;
        }
        return SafeSubscriber;
    }(Subscriber));
    function handleUnhandledError(error) {
        {
            reportUnhandledError(error);
        }
    }
    function defaultErrorHandler(err) {
        throw err;
    }
    var EMPTY_OBSERVER = {
        closed: true,
        next: noop,
        error: defaultErrorHandler,
        complete: noop,
    };

    var observable = (function () { return (typeof Symbol === 'function' && Symbol.observable) || '@@observable'; })();

    function identity(x) {
        return x;
    }

    function pipeFromArray(fns) {
        if (fns.length === 0) {
            return identity;
        }
        if (fns.length === 1) {
            return fns[0];
        }
        return function piped(input) {
            return fns.reduce(function (prev, fn) { return fn(prev); }, input);
        };
    }

    var Observable = (function () {
        function Observable(subscribe) {
            if (subscribe) {
                this._subscribe = subscribe;
            }
        }
        Observable.prototype.lift = function (operator) {
            var observable = new Observable();
            observable.source = this;
            observable.operator = operator;
            return observable;
        };
        Observable.prototype.subscribe = function (observerOrNext, error, complete) {
            var _this = this;
            var subscriber = isSubscriber(observerOrNext) ? observerOrNext : new SafeSubscriber(observerOrNext, error, complete);
            errorContext(function () {
                var _a = _this, operator = _a.operator, source = _a.source;
                subscriber.add(operator
                    ?
                        operator.call(subscriber, source)
                    : source
                        ?
                            _this._subscribe(subscriber)
                        :
                            _this._trySubscribe(subscriber));
            });
            return subscriber;
        };
        Observable.prototype._trySubscribe = function (sink) {
            try {
                return this._subscribe(sink);
            }
            catch (err) {
                sink.error(err);
            }
        };
        Observable.prototype.forEach = function (next, promiseCtor) {
            var _this = this;
            promiseCtor = getPromiseCtor(promiseCtor);
            return new promiseCtor(function (resolve, reject) {
                var subscriber = new SafeSubscriber({
                    next: function (value) {
                        try {
                            next(value);
                        }
                        catch (err) {
                            reject(err);
                            subscriber.unsubscribe();
                        }
                    },
                    error: reject,
                    complete: resolve,
                });
                _this.subscribe(subscriber);
            });
        };
        Observable.prototype._subscribe = function (subscriber) {
            var _a;
            return (_a = this.source) === null || _a === void 0 ? void 0 : _a.subscribe(subscriber);
        };
        Observable.prototype[observable] = function () {
            return this;
        };
        Observable.prototype.pipe = function () {
            var operations = [];
            for (var _i = 0; _i < arguments.length; _i++) {
                operations[_i] = arguments[_i];
            }
            return pipeFromArray(operations)(this);
        };
        Observable.prototype.toPromise = function (promiseCtor) {
            var _this = this;
            promiseCtor = getPromiseCtor(promiseCtor);
            return new promiseCtor(function (resolve, reject) {
                var value;
                _this.subscribe(function (x) { return (value = x); }, function (err) { return reject(err); }, function () { return resolve(value); });
            });
        };
        Observable.create = function (subscribe) {
            return new Observable(subscribe);
        };
        return Observable;
    }());
    function getPromiseCtor(promiseCtor) {
        var _a;
        return (_a = promiseCtor !== null && promiseCtor !== void 0 ? promiseCtor : config.Promise) !== null && _a !== void 0 ? _a : Promise;
    }
    function isObserver(value) {
        return value && isFunction(value.next) && isFunction(value.error) && isFunction(value.complete);
    }
    function isSubscriber(value) {
        return (value && value instanceof Subscriber) || (isObserver(value) && isSubscription(value));
    }

    function hasLift(source) {
        return isFunction(source === null || source === void 0 ? void 0 : source.lift);
    }
    function operate(init) {
        return function (source) {
            if (hasLift(source)) {
                return source.lift(function (liftedSource) {
                    try {
                        return init(liftedSource, this);
                    }
                    catch (err) {
                        this.error(err);
                    }
                });
            }
            throw new TypeError('Unable to lift unknown Observable type');
        };
    }

    function createOperatorSubscriber(destination, onNext, onComplete, onError, onFinalize) {
        return new OperatorSubscriber(destination, onNext, onComplete, onError, onFinalize);
    }
    var OperatorSubscriber = (function (_super) {
        __extends(OperatorSubscriber, _super);
        function OperatorSubscriber(destination, onNext, onComplete, onError, onFinalize, shouldUnsubscribe) {
            var _this = _super.call(this, destination) || this;
            _this.onFinalize = onFinalize;
            _this.shouldUnsubscribe = shouldUnsubscribe;
            _this._next = onNext
                ? function (value) {
                    try {
                        onNext(value);
                    }
                    catch (err) {
                        destination.error(err);
                    }
                }
                : _super.prototype._next;
            _this._error = onError
                ? function (err) {
                    try {
                        onError(err);
                    }
                    catch (err) {
                        destination.error(err);
                    }
                    finally {
                        this.unsubscribe();
                    }
                }
                : _super.prototype._error;
            _this._complete = onComplete
                ? function () {
                    try {
                        onComplete();
                    }
                    catch (err) {
                        destination.error(err);
                    }
                    finally {
                        this.unsubscribe();
                    }
                }
                : _super.prototype._complete;
            return _this;
        }
        OperatorSubscriber.prototype.unsubscribe = function () {
            var _a;
            if (!this.shouldUnsubscribe || this.shouldUnsubscribe()) {
                var closed_1 = this.closed;
                _super.prototype.unsubscribe.call(this);
                !closed_1 && ((_a = this.onFinalize) === null || _a === void 0 ? void 0 : _a.call(this));
            }
        };
        return OperatorSubscriber;
    }(Subscriber));

    var ObjectUnsubscribedError = createErrorClass(function (_super) {
        return function ObjectUnsubscribedErrorImpl() {
            _super(this);
            this.name = 'ObjectUnsubscribedError';
            this.message = 'object unsubscribed';
        };
    });

    var Subject = (function (_super) {
        __extends(Subject, _super);
        function Subject() {
            var _this = _super.call(this) || this;
            _this.closed = false;
            _this.currentObservers = null;
            _this.observers = [];
            _this.isStopped = false;
            _this.hasError = false;
            _this.thrownError = null;
            return _this;
        }
        Subject.prototype.lift = function (operator) {
            var subject = new AnonymousSubject(this, this);
            subject.operator = operator;
            return subject;
        };
        Subject.prototype._throwIfClosed = function () {
            if (this.closed) {
                throw new ObjectUnsubscribedError();
            }
        };
        Subject.prototype.next = function (value) {
            var _this = this;
            errorContext(function () {
                var e_1, _a;
                _this._throwIfClosed();
                if (!_this.isStopped) {
                    if (!_this.currentObservers) {
                        _this.currentObservers = Array.from(_this.observers);
                    }
                    try {
                        for (var _b = __values(_this.currentObservers), _c = _b.next(); !_c.done; _c = _b.next()) {
                            var observer = _c.value;
                            observer.next(value);
                        }
                    }
                    catch (e_1_1) { e_1 = { error: e_1_1 }; }
                    finally {
                        try {
                            if (_c && !_c.done && (_a = _b.return)) _a.call(_b);
                        }
                        finally { if (e_1) throw e_1.error; }
                    }
                }
            });
        };
        Subject.prototype.error = function (err) {
            var _this = this;
            errorContext(function () {
                _this._throwIfClosed();
                if (!_this.isStopped) {
                    _this.hasError = _this.isStopped = true;
                    _this.thrownError = err;
                    var observers = _this.observers;
                    while (observers.length) {
                        observers.shift().error(err);
                    }
                }
            });
        };
        Subject.prototype.complete = function () {
            var _this = this;
            errorContext(function () {
                _this._throwIfClosed();
                if (!_this.isStopped) {
                    _this.isStopped = true;
                    var observers = _this.observers;
                    while (observers.length) {
                        observers.shift().complete();
                    }
                }
            });
        };
        Subject.prototype.unsubscribe = function () {
            this.isStopped = this.closed = true;
            this.observers = this.currentObservers = null;
        };
        Object.defineProperty(Subject.prototype, "observed", {
            get: function () {
                var _a;
                return ((_a = this.observers) === null || _a === void 0 ? void 0 : _a.length) > 0;
            },
            enumerable: false,
            configurable: true
        });
        Subject.prototype._trySubscribe = function (subscriber) {
            this._throwIfClosed();
            return _super.prototype._trySubscribe.call(this, subscriber);
        };
        Subject.prototype._subscribe = function (subscriber) {
            this._throwIfClosed();
            this._checkFinalizedStatuses(subscriber);
            return this._innerSubscribe(subscriber);
        };
        Subject.prototype._innerSubscribe = function (subscriber) {
            var _this = this;
            var _a = this, hasError = _a.hasError, isStopped = _a.isStopped, observers = _a.observers;
            if (hasError || isStopped) {
                return EMPTY_SUBSCRIPTION;
            }
            this.currentObservers = null;
            observers.push(subscriber);
            return new Subscription(function () {
                _this.currentObservers = null;
                arrRemove(observers, subscriber);
            });
        };
        Subject.prototype._checkFinalizedStatuses = function (subscriber) {
            var _a = this, hasError = _a.hasError, thrownError = _a.thrownError, isStopped = _a.isStopped;
            if (hasError) {
                subscriber.error(thrownError);
            }
            else if (isStopped) {
                subscriber.complete();
            }
        };
        Subject.prototype.asObservable = function () {
            var observable = new Observable();
            observable.source = this;
            return observable;
        };
        Subject.create = function (destination, source) {
            return new AnonymousSubject(destination, source);
        };
        return Subject;
    }(Observable));
    var AnonymousSubject = (function (_super) {
        __extends(AnonymousSubject, _super);
        function AnonymousSubject(destination, source) {
            var _this = _super.call(this) || this;
            _this.destination = destination;
            _this.source = source;
            return _this;
        }
        AnonymousSubject.prototype.next = function (value) {
            var _a, _b;
            (_b = (_a = this.destination) === null || _a === void 0 ? void 0 : _a.next) === null || _b === void 0 ? void 0 : _b.call(_a, value);
        };
        AnonymousSubject.prototype.error = function (err) {
            var _a, _b;
            (_b = (_a = this.destination) === null || _a === void 0 ? void 0 : _a.error) === null || _b === void 0 ? void 0 : _b.call(_a, err);
        };
        AnonymousSubject.prototype.complete = function () {
            var _a, _b;
            (_b = (_a = this.destination) === null || _a === void 0 ? void 0 : _a.complete) === null || _b === void 0 ? void 0 : _b.call(_a);
        };
        AnonymousSubject.prototype._subscribe = function (subscriber) {
            var _a, _b;
            return (_b = (_a = this.source) === null || _a === void 0 ? void 0 : _a.subscribe(subscriber)) !== null && _b !== void 0 ? _b : EMPTY_SUBSCRIPTION;
        };
        return AnonymousSubject;
    }(Subject));

    function map(project, thisArg) {
        return operate(function (source, subscriber) {
            var index = 0;
            source.subscribe(createOperatorSubscriber(subscriber, function (value) {
                subscriber.next(project.call(thisArg, value, index++));
            }));
        });
    }

    // Copyright (c) .NET Foundation and contributors. All rights reserved.
    class PromiseCompletionSource {
        constructor() {
            this._resolve = () => { };
            this._reject = () => { };
            this.promise = new Promise((resolve, reject) => {
                this._resolve = resolve;
                this._reject = reject;
            });
        }
        resolve(value) {
            this._resolve(value);
        }
        reject(reason) {
            this._reject(reason);
        }
    }

    // Copyright (c) .NET Foundation and contributors. All rights reserved.
    class KernelInvocationContext {
        constructor(kernelCommandInvocation) {
            this._childCommands = [];
            this._eventSubject = new Subject();
            this._isComplete = false;
            this._handlingKernel = null;
            this.completionSource = new PromiseCompletionSource();
            this._commandEnvelope = kernelCommandInvocation;
        }
        get promise() {
            return this.completionSource.promise;
        }
        get handlingKernel() {
            return this._handlingKernel;
        }
        ;
        get kernelEvents() {
            return this._eventSubject.asObservable();
        }
        ;
        set handlingKernel(value) {
            this._handlingKernel = value;
        }
        static establish(kernelCommandInvocation) {
            let current = KernelInvocationContext._current;
            if (!current || current._isComplete) {
                KernelInvocationContext._current = new KernelInvocationContext(kernelCommandInvocation);
            }
            else {
                if (!areCommandsTheSame(kernelCommandInvocation, current._commandEnvelope)) {
                    const found = current._childCommands.includes(kernelCommandInvocation);
                    if (!found) {
                        current._childCommands.push(kernelCommandInvocation);
                    }
                }
            }
            return KernelInvocationContext._current;
        }
        static get current() { return this._current; }
        get command() { return this._commandEnvelope.command; }
        get commandEnvelope() { return this._commandEnvelope; }
        complete(command) {
            if (areCommandsTheSame(command, this._commandEnvelope)) {
                this._isComplete = true;
                let succeeded = {};
                let eventEnvelope = {
                    command: this._commandEnvelope,
                    eventType: CommandSucceededType,
                    event: succeeded
                };
                this.internalPublish(eventEnvelope);
                this.completionSource.resolve();
                // TODO: C# version has completion callbacks - do we need these?
                // if (!_events.IsDisposed)
                // {
                //     _events.OnCompleted();
                // }
            }
            else {
                let pos = this._childCommands.indexOf(command);
                delete this._childCommands[pos];
            }
        }
        fail(message) {
            // TODO:
            // The C# code accepts a message and/or an exception. Do we need to add support
            // for exceptions? (The TS CommandFailed interface doesn't have a place for it right now.)
            this._isComplete = true;
            let failed = { message: message !== null && message !== void 0 ? message : "Command Failed" };
            let eventEnvelope = {
                command: this._commandEnvelope,
                eventType: CommandFailedType,
                event: failed
            };
            this.internalPublish(eventEnvelope);
            this.completionSource.resolve();
        }
        publish(kernelEvent) {
            if (!this._isComplete) {
                this.internalPublish(kernelEvent);
            }
        }
        internalPublish(kernelEvent) {
            if (!kernelEvent.command) {
                kernelEvent.command = this._commandEnvelope;
            }
            let command = kernelEvent.command;
            if (this.handlingKernel) {
                const kernelUri = getKernelUri(this.handlingKernel);
                if (!eventRoutingSlipContains(kernelEvent, kernelUri)) {
                    stampEventRoutingSlip(kernelEvent, kernelUri);
                    kernelEvent.routingSlip; //?
                }
            }
            this._commandEnvelope; //?
            if (command === null ||
                command === undefined ||
                areCommandsTheSame(command, this._commandEnvelope) ||
                this._childCommands.includes(command)) {
                this._eventSubject.next(kernelEvent);
            }
        }
        isParentOfCommand(commandEnvelope) {
            const childFound = this._childCommands.includes(commandEnvelope);
            return childFound;
        }
        dispose() {
            if (!this._isComplete) {
                this.complete(this._commandEnvelope);
            }
            KernelInvocationContext._current = null;
        }
    }
    KernelInvocationContext._current = null;
    function areCommandsTheSame(envelope1, envelope2) {
        if (envelope1 === envelope2) {
            return true;
        }
        const sameCommandType = (envelope1 === null || envelope1 === void 0 ? void 0 : envelope1.commandType) === (envelope2 === null || envelope2 === void 0 ? void 0 : envelope2.commandType); //?
        const sameToken = (envelope1 === null || envelope1 === void 0 ? void 0 : envelope1.token) === (envelope2 === null || envelope2 === void 0 ? void 0 : envelope2.token); //?
        const sameCommandId = (envelope1 === null || envelope1 === void 0 ? void 0 : envelope1.id) === (envelope2 === null || envelope2 === void 0 ? void 0 : envelope2.id); //?
        if (sameCommandType && sameToken && sameCommandId) {
            return true;
        }
        return false;
    }

    // Copyright (c) .NET Foundation and contributors. All rights reserved.
    // Licensed under the MIT license. See LICENSE file in the project root for full license information.
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
    Guid.validator = new RegExp("^[a-z0-9]{8}-[a-z0-9]{4}-[a-z0-9]{4}-[a-z0-9]{4}-[a-z0-9]{12}$", "i");
    Guid.EMPTY = "00000000-0000-0000-0000-000000000000";
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

    // Copyright (c) .NET Foundation and contributors. All rights reserved.
    // Licensed under the MIT license. See LICENSE file in the project root for full license information.
    var LogLevel;
    (function (LogLevel) {
        LogLevel[LogLevel["Info"] = 0] = "Info";
        LogLevel[LogLevel["Warn"] = 1] = "Warn";
        LogLevel[LogLevel["Error"] = 2] = "Error";
        LogLevel[LogLevel["None"] = 3] = "None";
    })(LogLevel || (LogLevel = {}));
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
    Logger._default = new Logger('default', (_entry) => { });

    // Copyright (c) .NET Foundation and contributors. All rights reserved.
    class KernelScheduler {
        constructor() {
            this._operationQueue = [];
        }
        cancelCurrentOperation() {
            var _a;
            (_a = this._inFlightOperation) === null || _a === void 0 ? void 0 : _a.promiseCompletionSource.reject(new Error("Operation cancelled"));
        }
        runAsync(value, executor) {
            const operation = {
                value,
                executor,
                promiseCompletionSource: new PromiseCompletionSource(),
            };
            if (this._inFlightOperation) {
                Logger.default.info(`kernelScheduler: starting immediate execution of ${JSON.stringify(operation.value)}`);
                // invoke immediately
                return operation.executor(operation.value)
                    .then(() => {
                    Logger.default.info(`kernelScheduler: immediate execution completed: ${JSON.stringify(operation.value)}`);
                    operation.promiseCompletionSource.resolve();
                })
                    .catch(e => {
                    Logger.default.info(`kernelScheduler: immediate execution failed: ${JSON.stringify(e)} - ${JSON.stringify(operation.value)}`);
                    operation.promiseCompletionSource.reject(e);
                });
            }
            Logger.default.info(`kernelScheduler: scheduling execution of ${JSON.stringify(operation.value)}`);
            this._operationQueue.push(operation);
            if (this._operationQueue.length === 1) {
                this.executeNextCommand();
            }
            return operation.promiseCompletionSource.promise;
        }
        executeNextCommand() {
            const nextOperation = this._operationQueue.length > 0 ? this._operationQueue[0] : undefined;
            if (nextOperation) {
                this._inFlightOperation = nextOperation;
                Logger.default.info(`kernelScheduler: starting scheduled execution of ${JSON.stringify(nextOperation.value)}`);
                nextOperation.executor(nextOperation.value)
                    .then(() => {
                    this._inFlightOperation = undefined;
                    Logger.default.info(`kernelScheduler: completing inflight operation: success ${JSON.stringify(nextOperation.value)}`);
                    nextOperation.promiseCompletionSource.resolve();
                })
                    .catch(e => {
                    this._inFlightOperation = undefined;
                    Logger.default.info(`kernelScheduler: completing inflight operation: failure ${JSON.stringify(e)} - ${JSON.stringify(nextOperation.value)}`);
                    nextOperation.promiseCompletionSource.reject(e);
                })
                    .finally(() => {
                    this._operationQueue.shift();
                    this.executeNextCommand();
                });
            }
        }
    }

    // Copyright (c) .NET Foundation and contributors. All rights reserved.
    var KernelType;
    (function (KernelType) {
        KernelType[KernelType["composite"] = 0] = "composite";
        KernelType[KernelType["proxy"] = 1] = "proxy";
        KernelType[KernelType["default"] = 2] = "default";
    })(KernelType || (KernelType = {}));
    class Kernel {
        constructor(name, languageName, languageVersion, displayName) {
            this.name = name;
            this._commandHandlers = new Map();
            this._eventSubject = new Subject();
            this._tokenGenerator = new TokenGenerator();
            this.rootKernel = this;
            this.parentKernel = null;
            this._scheduler = null;
            this._kernelType = KernelType.default;
            this._kernelInfo = {
                localName: name,
                languageName: languageName,
                aliases: [],
                uri: createKernelUri(`kernel://local/${name}`),
                languageVersion: languageVersion,
                displayName: displayName !== null && displayName !== void 0 ? displayName : name,
                supportedDirectives: [],
                supportedKernelCommands: []
            };
            this._internalRegisterCommandHandler({
                commandType: RequestKernelInfoType, handle: (invocation) => __awaiter(this, void 0, void 0, function* () {
                    yield this.handleRequestKernelInfo(invocation);
                })
            });
        }
        get kernelInfo() {
            return this._kernelInfo;
        }
        get kernelType() {
            return this._kernelType;
        }
        set kernelType(value) {
            this._kernelType = value;
        }
        get kernelEvents() {
            return this._eventSubject.asObservable();
        }
        handleRequestKernelInfo(invocation) {
            return __awaiter(this, void 0, void 0, function* () {
                const eventEnvelope = {
                    eventType: KernelInfoProducedType,
                    command: invocation.commandEnvelope,
                    event: { kernelInfo: this._kernelInfo }
                }; //?
                invocation.context.publish(eventEnvelope);
                return Promise.resolve();
            });
        }
        getScheduler() {
            var _a, _b;
            if (!this._scheduler) {
                this._scheduler = (_b = (_a = this.parentKernel) === null || _a === void 0 ? void 0 : _a.getScheduler()) !== null && _b !== void 0 ? _b : new KernelScheduler();
            }
            return this._scheduler;
        }
        ensureCommandTokenAndId(commandEnvelope) {
            var _a;
            if (!commandEnvelope.token) {
                let nextToken = this._tokenGenerator.GetNewToken();
                if ((_a = KernelInvocationContext.current) === null || _a === void 0 ? void 0 : _a.commandEnvelope) {
                    // a parent command exists, create a token hierarchy
                    nextToken = KernelInvocationContext.current.commandEnvelope.token;
                }
                commandEnvelope.token = nextToken;
            }
            if (!commandEnvelope.id) {
                commandEnvelope.id = Guid.create().toString();
            }
        }
        static get current() {
            if (KernelInvocationContext.current) {
                return KernelInvocationContext.current.handlingKernel;
            }
            return null;
        }
        static get root() {
            if (Kernel.current) {
                return Kernel.current.rootKernel;
            }
            return null;
        }
        // Is it worth us going to efforts to ensure that the Promise returned here accurately reflects
        // the command's progress? The only thing that actually calls this is the kernel channel, through
        // the callback set up by attachKernelToChannel, and the callback is expected to return void, so
        // nothing is ever going to look at the promise we return here.
        send(commandEnvelope) {
            return __awaiter(this, void 0, void 0, function* () {
                this.ensureCommandTokenAndId(commandEnvelope);
                const kernelUri = getKernelUri(this);
                if (!commandRoutingSlipContains(commandEnvelope, kernelUri)) {
                    stampCommandRoutingSlipAsArrived(commandEnvelope, kernelUri);
                }
                commandEnvelope.routingSlip; //?
                KernelInvocationContext.establish(commandEnvelope);
                return this.getScheduler().runAsync(commandEnvelope, (value) => this.executeCommand(value).finally(() => {
                    stampCommandRoutingSlip(commandEnvelope, kernelUri);
                }));
            });
        }
        executeCommand(commandEnvelope) {
            return __awaiter(this, void 0, void 0, function* () {
                let context = KernelInvocationContext.establish(commandEnvelope);
                let previousHandlingKernel = context.handlingKernel;
                try {
                    yield this.handleCommand(commandEnvelope);
                }
                catch (e) {
                    context.fail((e === null || e === void 0 ? void 0 : e.message) || JSON.stringify(e));
                }
                finally {
                    context.handlingKernel = previousHandlingKernel;
                }
            });
        }
        getCommandHandler(commandType) {
            return this._commandHandlers.get(commandType);
        }
        handleCommand(commandEnvelope) {
            return new Promise((resolve, reject) => __awaiter(this, void 0, void 0, function* () {
                let context = KernelInvocationContext.establish(commandEnvelope);
                const previoudHendlingKernel = context.handlingKernel;
                context.handlingKernel = this;
                let isRootCommand = areCommandsTheSame(context.commandEnvelope, commandEnvelope);
                let eventSubscription = undefined; //?
                if (isRootCommand) {
                    this.name; //?
                    Logger.default.info(`kernel ${this.name} of type ${KernelType[this.kernelType]} subscribing to context events`);
                    eventSubscription = context.kernelEvents.pipe(map(e => {
                        var _a;
                        const message = `kernel ${this.name} of type ${KernelType[this.kernelType]} saw event ${e.eventType} with token ${(_a = e.command) === null || _a === void 0 ? void 0 : _a.token}`;
                        Logger.default.info(message);
                        const kernelUri = getKernelUri(this);
                        if (!eventRoutingSlipContains(e, kernelUri)) {
                            stampEventRoutingSlip(e, kernelUri);
                        }
                        return e;
                    }))
                        .subscribe(this.publishEvent.bind(this));
                }
                let handler = this.getCommandHandler(commandEnvelope.commandType);
                if (handler) {
                    try {
                        Logger.default.info(`kernel ${this.name} about to handle command: ${JSON.stringify(commandEnvelope)}`);
                        yield handler.handle({ commandEnvelope: commandEnvelope, context });
                        context.complete(commandEnvelope);
                        context.handlingKernel = previoudHendlingKernel;
                        if (isRootCommand) {
                            eventSubscription === null || eventSubscription === void 0 ? void 0 : eventSubscription.unsubscribe();
                            context.dispose();
                        }
                        Logger.default.info(`kernel ${this.name} done handling command: ${JSON.stringify(commandEnvelope)}`);
                        resolve();
                    }
                    catch (e) {
                        context.fail((e === null || e === void 0 ? void 0 : e.message) || JSON.stringify(e));
                        context.handlingKernel = previoudHendlingKernel;
                        if (isRootCommand) {
                            eventSubscription === null || eventSubscription === void 0 ? void 0 : eventSubscription.unsubscribe();
                            context.dispose();
                        }
                        reject(e);
                    }
                }
                else {
                    context.handlingKernel = previoudHendlingKernel;
                    if (isRootCommand) {
                        eventSubscription === null || eventSubscription === void 0 ? void 0 : eventSubscription.unsubscribe();
                        context.dispose();
                    }
                    reject(new Error(`No handler found for command type ${commandEnvelope.commandType}`));
                }
            }));
        }
        subscribeToKernelEvents(observer) {
            const sub = this._eventSubject.subscribe(observer);
            return {
                dispose: () => { sub.unsubscribe(); }
            };
        }
        canHandle(commandEnvelope) {
            if (commandEnvelope.command.targetKernelName && commandEnvelope.command.targetKernelName !== this.name) {
                return false;
            }
            if (commandEnvelope.command.destinationUri) {
                const normalizedUri = createKernelUri(commandEnvelope.command.destinationUri);
                if (this.kernelInfo.uri !== normalizedUri) {
                    return false;
                }
            }
            return this.supportsCommand(commandEnvelope.commandType);
        }
        supportsCommand(commandType) {
            return this._commandHandlers.has(commandType);
        }
        registerCommandHandler(handler) {
            // When a registration already existed, we want to overwrite it because we want users to
            // be able to develop handlers iteratively, and it would be unhelpful for handler registration
            // for any particular command to be cumulative.
            const shouldNotify = !this._commandHandlers.has(handler.commandType);
            this._internalRegisterCommandHandler(handler);
            if (shouldNotify) {
                const event = {
                    kernelInfo: this._kernelInfo,
                };
                const envelope = {
                    eventType: KernelInfoProducedType,
                    event: event
                };
                stampEventRoutingSlip(envelope, getKernelUri(this));
                const context = KernelInvocationContext.current;
                if (context) {
                    envelope.command = context.commandEnvelope;
                    context.publish(envelope);
                }
                else {
                    this.publishEvent(envelope);
                }
            }
        }
        _internalRegisterCommandHandler(handler) {
            this._commandHandlers.set(handler.commandType, handler);
            this._kernelInfo.supportedKernelCommands = Array.from(this._commandHandlers.keys()).map(commandName => ({ name: commandName }));
        }
        getHandlingKernel(commandEnvelope, context) {
            if (this.canHandle(commandEnvelope)) {
                return this;
            }
            else {
                context === null || context === void 0 ? void 0 : context.fail(`Command ${commandEnvelope.commandType} is not supported by Kernel ${this.name}`);
                return null;
            }
        }
        publishEvent(kernelEvent) {
            this._eventSubject.next(kernelEvent);
        }
    }
    function getKernelUri(kernel) {
        var _a;
        return (_a = kernel.kernelInfo.uri) !== null && _a !== void 0 ? _a : `kernel://local/${kernel.kernelInfo.localName}`;
    }

    // Copyright (c) .NET Foundation and contributors. All rights reserved.
    class CompositeKernel extends Kernel {
        constructor(name) {
            super(name);
            this._host = null;
            this._defaultKernelNamesByCommandType = new Map();
            this.kernelType = KernelType.composite;
            this._childKernels = new KernelCollection(this);
        }
        get childKernels() {
            return Array.from(this._childKernels);
        }
        get host() {
            return this._host;
        }
        set host(host) {
            this._host = host;
            if (this._host) {
                this.kernelInfo.uri = this._host.uri;
                this._childKernels.notifyThatHostWasSet();
            }
        }
        handleRequestKernelInfo(invocation) {
            return __awaiter(this, void 0, void 0, function* () {
                const eventEnvelope = {
                    eventType: KernelInfoProducedType,
                    command: invocation.commandEnvelope,
                    event: { kernelInfo: this.kernelInfo }
                }; //?
                invocation.context.publish(eventEnvelope);
                for (let kernel of this._childKernels) {
                    if (kernel.supportsCommand(invocation.commandEnvelope.commandType)) {
                        const childCommand = {
                            commandType: RequestKernelInfoType,
                            command: {
                                targetKernelName: kernel.kernelInfo.localName
                            },
                            routingSlip: []
                        };
                        continueCommandRoutingSlip(childCommand, invocation.commandEnvelope.routingSlip || []);
                        yield kernel.handleCommand(childCommand);
                    }
                }
            });
        }
        add(kernel, aliases) {
            if (!kernel) {
                throw new Error("kernel cannot be null or undefined");
            }
            if (!this.defaultKernelName) {
                // default to first kernel
                this.defaultKernelName = kernel.name;
            }
            kernel.parentKernel = this;
            kernel.rootKernel = this.rootKernel;
            kernel.kernelEvents.subscribe({
                next: (event) => {
                    const kernelUri = getKernelUri(this);
                    if (!eventRoutingSlipContains(event, kernelUri)) {
                        stampEventRoutingSlip(event, kernelUri);
                    }
                    this.publishEvent(event);
                }
            });
            if (aliases) {
                let set = new Set(aliases);
                if (kernel.kernelInfo.aliases) {
                    for (let alias in kernel.kernelInfo.aliases) {
                        set.add(alias);
                    }
                }
                kernel.kernelInfo.aliases = Array.from(set);
            }
            this._childKernels.add(kernel, aliases);
            const invocationContext = KernelInvocationContext.current;
            if (invocationContext) {
                invocationContext.commandEnvelope; //?
                invocationContext.publish({
                    eventType: KernelInfoProducedType,
                    event: {
                        kernelInfo: kernel.kernelInfo
                    },
                    command: invocationContext.commandEnvelope
                });
            }
            else {
                this.publishEvent({
                    eventType: KernelInfoProducedType,
                    event: {
                        kernelInfo: kernel.kernelInfo
                    }
                });
            }
        }
        findKernelByUri(uri) {
            const normalized = createKernelUri(uri);
            if (this.kernelInfo.uri === normalized) {
                return this;
            }
            return this._childKernels.tryGetByUri(normalized);
        }
        findKernelByName(name) {
            if (this.kernelInfo.localName === name || this.kernelInfo.aliases.find(a => a === name)) {
                return this;
            }
            return this._childKernels.tryGetByAlias(name);
        }
        findKernels(predicate) {
            var results = [];
            if (predicate(this)) {
                results.push(this);
            }
            for (let kernel of this.childKernels) {
                if (predicate(kernel)) {
                    results.push(kernel);
                }
            }
            return results;
        }
        findKernel(predicate) {
            if (predicate(this)) {
                return this;
            }
            return this.childKernels.find(predicate);
        }
        setDefaultTargetKernelNameForCommand(commandType, kernelName) {
            this._defaultKernelNamesByCommandType.set(commandType, kernelName);
        }
        handleCommand(commandEnvelope) {
            var _a;
            const invocationContext = KernelInvocationContext.current;
            let kernel = commandEnvelope.command.targetKernelName === this.name
                ? this
                : this.getHandlingKernel(commandEnvelope, invocationContext);
            const previusoHandlingKernel = (_a = invocationContext === null || invocationContext === void 0 ? void 0 : invocationContext.handlingKernel) !== null && _a !== void 0 ? _a : null;
            if (kernel === this) {
                if (invocationContext !== null) {
                    invocationContext.handlingKernel = kernel;
                }
                return super.handleCommand(commandEnvelope).finally(() => {
                    if (invocationContext !== null) {
                        invocationContext.handlingKernel = previusoHandlingKernel;
                    }
                });
            }
            else if (kernel) {
                if (invocationContext !== null) {
                    invocationContext.handlingKernel = kernel;
                }
                const kernelUri = getKernelUri(kernel);
                if (!commandRoutingSlipContains(commandEnvelope, kernelUri)) {
                    stampCommandRoutingSlipAsArrived(commandEnvelope, kernelUri);
                }
                return kernel.handleCommand(commandEnvelope).finally(() => {
                    if (invocationContext !== null) {
                        invocationContext.handlingKernel = previusoHandlingKernel;
                    }
                    if (!commandRoutingSlipContains(commandEnvelope, kernelUri)) {
                        stampCommandRoutingSlip(commandEnvelope, kernelUri);
                    }
                });
            }
            if (invocationContext !== null) {
                invocationContext.handlingKernel = previusoHandlingKernel;
            }
            return Promise.reject(new Error("Kernel not found: " + commandEnvelope.command.targetKernelName));
        }
        getHandlingKernel(commandEnvelope, context) {
            var _a, _b, _c, _d, _e;
            let kernel = null;
            if (commandEnvelope.command.destinationUri) {
                const normalized = createKernelUri(commandEnvelope.command.destinationUri);
                kernel = (_a = this._childKernels.tryGetByUri(normalized)) !== null && _a !== void 0 ? _a : null;
                if (kernel) {
                    return kernel;
                }
            }
            let targetKernelName = commandEnvelope.command.targetKernelName;
            if (targetKernelName === undefined || targetKernelName === null) {
                if (this.canHandle(commandEnvelope)) {
                    return this;
                }
                targetKernelName = (_b = this._defaultKernelNamesByCommandType.get(commandEnvelope.commandType)) !== null && _b !== void 0 ? _b : this.defaultKernelName;
            }
            if (targetKernelName !== undefined && targetKernelName !== null) {
                kernel = (_c = this._childKernels.tryGetByAlias(targetKernelName)) !== null && _c !== void 0 ? _c : null;
            }
            if (targetKernelName && !kernel) {
                const errorMessage = `Kernel not found: ${targetKernelName}`;
                Logger.default.error(errorMessage);
                throw new Error(errorMessage);
            }
            if (!kernel) {
                if (this._childKernels.count === 1) {
                    kernel = (_d = this._childKernels.single()) !== null && _d !== void 0 ? _d : null;
                }
            }
            if (!kernel) {
                kernel = (_e = context === null || context === void 0 ? void 0 : context.handlingKernel) !== null && _e !== void 0 ? _e : null;
            }
            return kernel !== null && kernel !== void 0 ? kernel : this;
        }
    }
    class KernelCollection {
        constructor(compositeKernel) {
            this._kernels = [];
            this._nameAndAliasesByKernel = new Map();
            this._kernelsByNameOrAlias = new Map();
            this._kernelsByLocalUri = new Map();
            this._kernelsByRemoteUri = new Map();
            this._compositeKernel = compositeKernel;
        }
        [Symbol.iterator]() {
            let counter = 0;
            return {
                next: () => {
                    return {
                        value: this._kernels[counter++],
                        done: counter > this._kernels.length //?
                    };
                }
            };
        }
        single() {
            return this._kernels.length === 1 ? this._kernels[0] : undefined;
        }
        add(kernel, aliases) {
            if (this._kernelsByNameOrAlias.has(kernel.name)) {
                throw new Error(`kernel with name ${kernel.name} already exists`);
            }
            this.updateKernelInfoAndIndex(kernel, aliases);
            this._kernels.push(kernel);
        }
        get count() {
            return this._kernels.length;
        }
        updateKernelInfoAndIndex(kernel, aliases) {
            var _a, _b;
            if (aliases) {
                for (let alias of aliases) {
                    if (this._kernelsByNameOrAlias.has(alias)) {
                        throw new Error(`kernel with alias ${alias} already exists`);
                    }
                }
            }
            if (!this._nameAndAliasesByKernel.has(kernel)) {
                let set = new Set();
                for (let alias of kernel.kernelInfo.aliases) {
                    set.add(alias);
                }
                kernel.kernelInfo.aliases = Array.from(set);
                set.add(kernel.kernelInfo.localName);
                this._nameAndAliasesByKernel.set(kernel, set);
            }
            if (aliases) {
                for (let alias of aliases) {
                    this._nameAndAliasesByKernel.get(kernel).add(alias);
                }
            }
            (_a = this._nameAndAliasesByKernel.get(kernel)) === null || _a === void 0 ? void 0 : _a.forEach(alias => {
                this._kernelsByNameOrAlias.set(alias, kernel);
            });
            let baseUri = ((_b = this._compositeKernel.host) === null || _b === void 0 ? void 0 : _b.uri) || this._compositeKernel.kernelInfo.uri;
            if (!baseUri.endsWith("/")) {
                baseUri += "/";
            }
            kernel.kernelInfo.uri = createKernelUri(`${baseUri}${kernel.kernelInfo.localName}`); //?
            this._kernelsByLocalUri.set(kernel.kernelInfo.uri, kernel);
            if (kernel.kernelType === KernelType.proxy) {
                this._kernelsByRemoteUri.set(kernel.kernelInfo.remoteUri, kernel);
            }
        }
        tryGetByAlias(alias) {
            return this._kernelsByNameOrAlias.get(alias);
        }
        tryGetByUri(uri) {
            let kernel = this._kernelsByLocalUri.get(uri) || this._kernelsByRemoteUri.get(uri);
            return kernel;
        }
        notifyThatHostWasSet() {
            for (let kernel of this._kernels) {
                this.updateKernelInfoAndIndex(kernel);
            }
        }
    }

    // Copyright (c) .NET Foundation and contributors. All rights reserved.
    class ConsoleCapture {
        constructor() {
            this.originalConsole = console;
            console = this;
        }
        set kernelInvocationContext(value) {
            this._kernelInvocationContext = value;
        }
        assert(value, message, ...optionalParams) {
            this.originalConsole.assert(value, message, optionalParams);
        }
        clear() {
            this.originalConsole.clear();
        }
        count(label) {
            this.originalConsole.count(label);
        }
        countReset(label) {
            this.originalConsole.countReset(label);
        }
        debug(message, ...optionalParams) {
            this.originalConsole.debug(message, optionalParams);
        }
        dir(obj, options) {
            this.originalConsole.dir(obj, options);
        }
        dirxml(...data) {
            this.originalConsole.dirxml(data);
        }
        error(message, ...optionalParams) {
            this.redirectAndPublish(this.originalConsole.error, ...[message, ...optionalParams]);
        }
        group(...label) {
            this.originalConsole.group(label);
        }
        groupCollapsed(...label) {
            this.originalConsole.groupCollapsed(label);
        }
        groupEnd() {
            this.originalConsole.groupEnd();
        }
        info(message, ...optionalParams) {
            this.redirectAndPublish(this.originalConsole.info, ...[message, ...optionalParams]);
        }
        log(message, ...optionalParams) {
            this.redirectAndPublish(this.originalConsole.log, ...[message, ...optionalParams]);
        }
        table(tabularData, properties) {
            this.originalConsole.table(tabularData, properties);
        }
        time(label) {
            this.originalConsole.time(label);
        }
        timeEnd(label) {
            this.originalConsole.timeEnd(label);
        }
        timeLog(label, ...data) {
            this.originalConsole.timeLog(label, data);
        }
        timeStamp(label) {
            this.originalConsole.timeStamp(label);
        }
        trace(message, ...optionalParams) {
            this.redirectAndPublish(this.originalConsole.trace, ...[message, ...optionalParams]);
        }
        warn(message, ...optionalParams) {
            this.originalConsole.warn(message, optionalParams);
        }
        profile(label) {
            this.originalConsole.profile(label);
        }
        profileEnd(label) {
            this.originalConsole.profileEnd(label);
        }
        dispose() {
            console = this.originalConsole;
        }
        redirectAndPublish(target, ...args) {
            if (this._kernelInvocationContext) {
                for (const arg of args) {
                    let mimeType;
                    let value;
                    if (typeof arg !== 'object' && !Array.isArray(arg)) {
                        mimeType = 'text/plain';
                        value = arg === null || arg === void 0 ? void 0 : arg.toString();
                    }
                    else {
                        mimeType = 'application/json';
                        value = JSON.stringify(arg);
                    }
                    const displayedValue = {
                        formattedValues: [
                            {
                                mimeType,
                                value,
                            }
                        ]
                    };
                    const eventEnvelope = {
                        eventType: DisplayedValueProducedType,
                        event: displayedValue,
                        command: this._kernelInvocationContext.commandEnvelope
                    };
                    this._kernelInvocationContext.publish(eventEnvelope);
                }
            }
            if (target) {
                target(...args);
            }
        }
    }

    // Copyright (c) .NET Foundation and contributors. All rights reserved.
    class JavascriptKernel extends Kernel {
        constructor(name) {
            super(name !== null && name !== void 0 ? name : "javascript", "JavaScript");
            this.suppressedLocals = new Set(this.allLocalVariableNames());
            this.registerCommandHandler({ commandType: SubmitCodeType, handle: invocation => this.handleSubmitCode(invocation) });
            this.registerCommandHandler({ commandType: RequestValueInfosType, handle: invocation => this.handleRequestValueInfos(invocation) });
            this.registerCommandHandler({ commandType: RequestValueType, handle: invocation => this.handleRequestValue(invocation) });
            this.registerCommandHandler({ commandType: SendValueType, handle: invocation => this.handleSendValue(invocation) });
            this.capture = new ConsoleCapture();
        }
        handleSendValue(invocation) {
            const sendValue = invocation.commandEnvelope.command;
            if (sendValue.formattedValue) {
                switch (sendValue.formattedValue.mimeType) {
                    case 'application/json':
                        globalThis[sendValue.name] = JSON.parse(sendValue.formattedValue.value);
                        break;
                    default:
                        throw new Error(`mimetype ${sendValue.formattedValue.mimeType} not supported`);
                }
                return Promise.resolve();
            }
            throw new Error("formattedValue is required");
        }
        handleSubmitCode(invocation) {
            const _super = Object.create(null, {
                kernelInfo: { get: () => super.kernelInfo }
            });
            return __awaiter(this, void 0, void 0, function* () {
                const submitCode = invocation.commandEnvelope.command;
                const code = submitCode.code;
                _super.kernelInfo.localName; //?
                _super.kernelInfo.uri; //?
                _super.kernelInfo.remoteUri; //?
                invocation.context.publish({ eventType: CodeSubmissionReceivedType, event: { code }, command: invocation.commandEnvelope });
                invocation.context.commandEnvelope.routingSlip; //?
                this.capture.kernelInvocationContext = invocation.context;
                let result = undefined;
                try {
                    const AsyncFunction = eval(`Object.getPrototypeOf(async function(){}).constructor`);
                    const evaluator = AsyncFunction("console", code);
                    result = yield evaluator(this.capture);
                    if (result !== undefined) {
                        const formattedValue = formatValue(result, 'application/json');
                        const event = {
                            formattedValues: [formattedValue]
                        };
                        invocation.context.publish({ eventType: ReturnValueProducedType, event, command: invocation.commandEnvelope });
                    }
                }
                catch (e) {
                    throw e; //?
                }
                finally {
                    this.capture.kernelInvocationContext = undefined;
                }
            });
        }
        handleRequestValueInfos(invocation) {
            const valueInfos = this.allLocalVariableNames().filter(v => !this.suppressedLocals.has(v)).map(v => ({ name: v, preferredMimeTypes: [] }));
            const event = {
                valueInfos
            };
            invocation.context.publish({ eventType: ValueInfosProducedType, event, command: invocation.commandEnvelope });
            return Promise.resolve();
        }
        handleRequestValue(invocation) {
            const requestValue = invocation.commandEnvelope.command;
            const rawValue = this.getLocalVariable(requestValue.name);
            const formattedValue = formatValue(rawValue, requestValue.mimeType || 'application/json');
            Logger.default.info(`returning ${JSON.stringify(formattedValue)} for ${requestValue.name}`);
            const event = {
                name: requestValue.name,
                formattedValue
            };
            invocation.context.publish({ eventType: ValueProducedType, event, command: invocation.commandEnvelope });
            return Promise.resolve();
        }
        allLocalVariableNames() {
            const result = [];
            try {
                for (const key in globalThis) {
                    try {
                        if (typeof globalThis[key] !== 'function') {
                            result.push(key);
                        }
                    }
                    catch (e) {
                        Logger.default.error(`error getting value for ${key} : ${e}`);
                    }
                }
            }
            catch (e) {
                Logger.default.error(`error scanning globla variables : ${e}`);
            }
            return result;
        }
        getLocalVariable(name) {
            return globalThis[name];
        }
    }
    function formatValue(arg, mimeType) {
        let value;
        switch (mimeType) {
            case 'text/plain':
                value = (arg === null || arg === void 0 ? void 0 : arg.toString()) || 'undefined';
                break;
            case 'application/json':
                value = JSON.stringify(arg);
                break;
            default:
                throw new Error(`unsupported mime type: ${mimeType}`);
        }
        return {
            mimeType,
            value,
        };
    }

    // Copyright (c) .NET Foundation and contributors. All rights reserved.
    function isKernelCommandEnvelope(commandOrEvent) {
        return commandOrEvent.commandType !== undefined;
    }
    function isKernelEventEnvelope(commandOrEvent) {
        return commandOrEvent.eventType !== undefined;
    }
    class KernelCommandAndEventReceiver {
        constructor(observer) {
            this._disposables = [];
            this._observable = observer;
        }
        subscribe(observer) {
            return this._observable.subscribe(observer);
        }
        dispose() {
            for (let disposable of this._disposables) {
                disposable.dispose();
            }
        }
        static FromObservable(observable) {
            return new KernelCommandAndEventReceiver(observable);
        }
        static FromEventListener(args) {
            let subject = new Subject();
            const listener = (e) => {
                let mapped = args.map(e);
                subject.next(mapped);
            };
            args.eventTarget.addEventListener(args.event, listener);
            const ret = new KernelCommandAndEventReceiver(subject);
            ret._disposables.push({
                dispose: () => {
                    args.eventTarget.removeEventListener(args.event, listener);
                }
            });
            args.eventTarget.removeEventListener(args.event, listener);
            return ret;
        }
    }
    function isObservable(source) {
        return source.next !== undefined;
    }
    class KernelCommandAndEventSender {
        constructor() {
        }
        send(kernelCommandOrEventEnvelope) {
            if (this._sender) {
                try {
                    const serislized = JSON.parse(JSON.stringify(kernelCommandOrEventEnvelope));
                    if (typeof this._sender === "function") {
                        this._sender(serislized);
                    }
                    else if (isObservable(this._sender)) {
                        this._sender.next(serislized);
                    }
                    else {
                        return Promise.reject(new Error("Sender is not set"));
                    }
                }
                catch (error) {
                    return Promise.reject(error);
                }
                return Promise.resolve();
            }
            return Promise.reject(new Error("Sender is not set"));
        }
        static FromObserver(observer) {
            const sender = new KernelCommandAndEventSender();
            sender._sender = observer;
            return sender;
        }
        static FromFunction(send) {
            const sender = new KernelCommandAndEventSender();
            sender._sender = send;
            return sender;
        }
    }
    const onKernelInfoUpdates = [];
    function ensureOrUpdateProxyForKernelInfo(kernelInfoProduced, compositeKernel) {
        var _a;
        const uriToLookup = (_a = kernelInfoProduced.kernelInfo.uri) !== null && _a !== void 0 ? _a : kernelInfoProduced.kernelInfo.remoteUri;
        if (uriToLookup) {
            let kernel = compositeKernel.findKernelByUri(uriToLookup);
            if (!kernel) {
                // add
                if (compositeKernel.host) {
                    Logger.default.info(`creating proxy for uri[${uriToLookup}]with info ${JSON.stringify(kernelInfoProduced)}`);
                    // check for clash with `kernelInfo.localName`
                    kernel = compositeKernel.host.connectProxyKernel(kernelInfoProduced.kernelInfo.localName, uriToLookup, kernelInfoProduced.kernelInfo.aliases);
                }
                else {
                    throw new Error('no kernel host found');
                }
            }
            else {
                Logger.default.info(`patching proxy for uri[${uriToLookup}]with info ${JSON.stringify(kernelInfoProduced)} `);
            }
            if (kernel.kernelType === KernelType.proxy) {
                // patch
                updateKernelInfo(kernel.kernelInfo, kernelInfoProduced.kernelInfo);
            }
            for (const updater of onKernelInfoUpdates) {
                updater(compositeKernel);
            }
        }
    }
    function updateKernelInfo(destination, incoming) {
        var _a, _b;
        destination.languageName = (_a = incoming.languageName) !== null && _a !== void 0 ? _a : destination.languageName;
        destination.languageVersion = (_b = incoming.languageVersion) !== null && _b !== void 0 ? _b : destination.languageVersion;
        destination.displayName = incoming.displayName;
        const supportedDirectives = new Set();
        const supportedCommands = new Set();
        if (!destination.supportedDirectives) {
            destination.supportedDirectives = [];
        }
        if (!destination.supportedKernelCommands) {
            destination.supportedKernelCommands = [];
        }
        for (const supportedDirective of destination.supportedDirectives) {
            supportedDirectives.add(supportedDirective.name);
        }
        for (const supportedCommand of destination.supportedKernelCommands) {
            supportedCommands.add(supportedCommand.name);
        }
        for (const supportedDirective of incoming.supportedDirectives) {
            if (!supportedDirectives.has(supportedDirective.name)) {
                supportedDirectives.add(supportedDirective.name);
                destination.supportedDirectives.push(supportedDirective);
            }
        }
        for (const supportedCommand of incoming.supportedKernelCommands) {
            if (!supportedCommands.has(supportedCommand.name)) {
                supportedCommands.add(supportedCommand.name);
                destination.supportedKernelCommands.push(supportedCommand);
            }
        }
    }
    class Connector {
        constructor(configuration) {
            this._remoteUris = new Set();
            this._receiver = configuration.receiver;
            this._sender = configuration.sender;
            if (configuration.remoteUris) {
                for (const remoteUri of configuration.remoteUris) {
                    const uri = extractHostAndNomalize(remoteUri);
                    if (uri) {
                        this._remoteUris.add(uri);
                    }
                }
            }
            this._listener = this._receiver.subscribe({
                next: (kernelCommandOrEventEnvelope) => {
                    var _a, _b;
                    if (isKernelEventEnvelope(kernelCommandOrEventEnvelope)) {
                        if (kernelCommandOrEventEnvelope.eventType === KernelInfoProducedType) {
                            const event = kernelCommandOrEventEnvelope.event;
                            if (!event.kernelInfo.remoteUri) {
                                const uri = extractHostAndNomalize(event.kernelInfo.uri);
                                if (uri) {
                                    this._remoteUris.add(uri);
                                }
                            }
                        }
                        if (((_b = (_a = kernelCommandOrEventEnvelope.routingSlip) === null || _a === void 0 ? void 0 : _a.length) !== null && _b !== void 0 ? _b : 0) > 0) {
                            const eventOrigin = kernelCommandOrEventEnvelope.routingSlip[0];
                            const uri = extractHostAndNomalize(eventOrigin);
                            if (uri) {
                                this._remoteUris.add(uri);
                            }
                        }
                    }
                }
            });
        }
        get remoteHostUris() {
            return Array.from(this._remoteUris.values());
        }
        get sender() {
            return this._sender;
        }
        get receiver() {
            return this._receiver;
        }
        canReach(remoteUri) {
            const host = extractHostAndNomalize(remoteUri); //?
            if (host) {
                return this._remoteUris.has(host);
            }
            return false;
        }
        dispose() {
            this._listener.unsubscribe();
        }
    }
    function extractHostAndNomalize(kernelUri) {
        var _a;
        const filter = /(?<host>.+:\/\/[^\/]+)(\/[^\/])*/gi;
        const match = filter.exec(kernelUri); //?
        if ((_a = match === null || match === void 0 ? void 0 : match.groups) === null || _a === void 0 ? void 0 : _a.host) {
            const host = match.groups.host;
            return host; //?
        }
        return "";
    }

    // Copyright (c) .NET Foundation and contributors. All rights reserved.
    class ProxyKernel extends Kernel {
        constructor(name, _sender, _receiver, languageName, languageVersion) {
            super(name, languageName, languageVersion);
            this.name = name;
            this._sender = _sender;
            this._receiver = _receiver;
            this.kernelType = KernelType.proxy;
        }
        getCommandHandler(commandType) {
            return {
                commandType,
                handle: (invocation) => {
                    return this._commandHandler(invocation);
                }
            };
        }
        delegatePublication(envelope, invocationContext) {
            let alreadyBeenSeen = false;
            const kernelUri = getKernelUri(this);
            if (kernelUri && !eventRoutingSlipContains(envelope, kernelUri)) {
                stampEventRoutingSlip(envelope, kernelUri);
            }
            else {
                alreadyBeenSeen = true;
            }
            if (this.hasSameOrigin(envelope)) {
                if (!alreadyBeenSeen) {
                    invocationContext.publish(envelope);
                }
            }
        }
        hasSameOrigin(envelope) {
            var _a, _b, _c;
            let commandOriginUri = (_c = (_b = (_a = envelope.command) === null || _a === void 0 ? void 0 : _a.command) === null || _b === void 0 ? void 0 : _b.originUri) !== null && _c !== void 0 ? _c : this.kernelInfo.uri;
            if (commandOriginUri === this.kernelInfo.uri) {
                return true;
            }
            return commandOriginUri === null;
        }
        updateKernelInfoFromEvent(kernelInfoProduced) {
            updateKernelInfo(this.kernelInfo, kernelInfoProduced.kernelInfo);
        }
        _commandHandler(commandInvocation) {
            var _a, _b;
            var _c, _d;
            return __awaiter(this, void 0, void 0, function* () {
                this.ensureCommandTokenAndId(commandInvocation.commandEnvelope);
                const commandToken = commandInvocation.commandEnvelope.token;
                const commandId = commandInvocation.commandEnvelope.id;
                const completionSource = new PromiseCompletionSource();
                // fix : is this the right way? We are trying to avoid forwarding events we just did forward
                let eventSubscription = this._receiver.subscribe({
                    next: (envelope) => {
                        var _a, _b, _c, _d;
                        if (isKernelEventEnvelope(envelope)) {
                            if (envelope.eventType === KernelInfoProducedType &&
                                (envelope.command === null || envelope.command === undefined)) {
                                const kernelInfoProduced = envelope.event;
                                kernelInfoProduced.kernelInfo; //?
                                this.kernelInfo; //?
                                if (kernelInfoProduced.kernelInfo.uri === this.kernelInfo.remoteUri) {
                                    this.updateKernelInfoFromEvent(kernelInfoProduced);
                                    this.publishEvent({
                                        eventType: KernelInfoProducedType,
                                        event: { kernelInfo: this.kernelInfo }
                                    });
                                }
                            }
                            else if (envelope.command.token === commandToken) {
                                Logger.default.info(`proxy name=${this.name}[local uri:${this.kernelInfo.uri}, remote uri:${this.kernelInfo.remoteUri}] processing event, envelopeid=${envelope.command.id}, commandid=${commandId}`);
                                Logger.default.info(`proxy name=${this.name}[local uri:${this.kernelInfo.uri}, remote uri:${this.kernelInfo.remoteUri}] processing event, ${JSON.stringify(envelope)}`);
                                try {
                                    const original = [...(_b = (_a = commandInvocation.commandEnvelope) === null || _a === void 0 ? void 0 : _a.routingSlip) !== null && _b !== void 0 ? _b : []];
                                    continueCommandRoutingSlip(commandInvocation.commandEnvelope, envelope.command.routingSlip);
                                    envelope.command.routingSlip = [...(_c = commandInvocation.commandEnvelope.routingSlip) !== null && _c !== void 0 ? _c : []]; //?
                                    Logger.default.warn(`proxy name=${this.name}[local uri:${this.kernelInfo.uri}, command routingSlip :${original}] has changed to: ${JSON.stringify((_d = commandInvocation.commandEnvelope.routingSlip) !== null && _d !== void 0 ? _d : [])}`);
                                }
                                catch (e) {
                                    Logger.default.error(`proxy name=${this.name}[local uri:${this.kernelInfo.uri}, error ${e === null || e === void 0 ? void 0 : e.message}`);
                                }
                                switch (envelope.eventType) {
                                    case KernelInfoProducedType:
                                        {
                                            const kernelInfoProduced = envelope.event;
                                            if (kernelInfoProduced.kernelInfo.uri === this.kernelInfo.remoteUri) {
                                                this.updateKernelInfoFromEvent(kernelInfoProduced);
                                                this.delegatePublication({
                                                    eventType: KernelInfoProducedType,
                                                    event: { kernelInfo: this.kernelInfo },
                                                    routingSlip: envelope.routingSlip,
                                                    command: commandInvocation.commandEnvelope
                                                }, commandInvocation.context);
                                                this.delegatePublication(envelope, commandInvocation.context);
                                            }
                                            else {
                                                this.delegatePublication(envelope, commandInvocation.context);
                                            }
                                        }
                                        break;
                                    case CommandCancelledType:
                                    case CommandFailedType:
                                    case CommandSucceededType:
                                        Logger.default.info(`proxy name=${this.name}[local uri:${this.kernelInfo.uri}, remote uri:${this.kernelInfo.remoteUri}] finished, envelopeid=${envelope.command.id}, commandid=${commandId}`);
                                        if (envelope.command.id === commandId) {
                                            Logger.default.info(`proxy name=${this.name}[local uri:${this.kernelInfo.uri}, remote uri:${this.kernelInfo.remoteUri}] resolving promise, envelopeid=${envelope.command.id}, commandid=${commandId}`);
                                            completionSource.resolve(envelope);
                                        }
                                        else {
                                            Logger.default.info(`proxy name=${this.name}[local uri:${this.kernelInfo.uri}, remote uri:${this.kernelInfo.remoteUri}] not resolving promise, envelopeid=${envelope.command.id}, commandid=${commandId}`);
                                            this.delegatePublication(envelope, commandInvocation.context);
                                        }
                                        break;
                                    default:
                                        this.delegatePublication(envelope, commandInvocation.context);
                                        break;
                                }
                            }
                        }
                    }
                });
                try {
                    if (!commandInvocation.commandEnvelope.command.destinationUri || !commandInvocation.commandEnvelope.command.originUri) {
                        (_a = (_c = commandInvocation.commandEnvelope.command).originUri) !== null && _a !== void 0 ? _a : (_c.originUri = this.kernelInfo.uri);
                        (_b = (_d = commandInvocation.commandEnvelope.command).destinationUri) !== null && _b !== void 0 ? _b : (_d.destinationUri = this.kernelInfo.remoteUri);
                    }
                    commandInvocation.commandEnvelope.routingSlip; //?
                    if (commandInvocation.commandEnvelope.commandType === RequestKernelInfoType) {
                        const destinationUri = this.kernelInfo.remoteUri;
                        if (commandRoutingSlipContains(commandInvocation.commandEnvelope, destinationUri, true)) {
                            return Promise.resolve();
                        }
                    }
                    Logger.default.info(`proxy ${this.name}[local uri:${this.kernelInfo.uri}, remote uri:${this.kernelInfo.remoteUri}] forwarding command ${commandInvocation.commandEnvelope.commandType} to ${commandInvocation.commandEnvelope.command.destinationUri}`);
                    this._sender.send(commandInvocation.commandEnvelope);
                    Logger.default.info(`proxy ${this.name}[local uri:${this.kernelInfo.uri}, remote uri:${this.kernelInfo.remoteUri}] about to await with token ${commandToken} and  commandid ${commandId}`);
                    const enventEnvelope = yield completionSource.promise;
                    if (enventEnvelope.eventType === CommandFailedType) {
                        commandInvocation.context.fail(enventEnvelope.event.message);
                    }
                    Logger.default.info(`proxy ${this.name}[local uri:${this.kernelInfo.uri}, remote uri:${this.kernelInfo.remoteUri}] done awaiting with token ${commandToken}} and  commandid ${commandId}`);
                }
                catch (e) {
                    commandInvocation.context.fail(e.message);
                }
                finally {
                    eventSubscription.unsubscribe();
                }
            });
        }
    }

    // Copyright (c) .NET Foundation and contributors. All rights reserved.
    class KernelHost {
        constructor(kernel, sender, receiver, hostUri) {
            this._remoteUriToKernel = new Map();
            this._uriToKernel = new Map();
            this._kernelToKernelInfo = new Map();
            this._connectors = [];
            this._kernel = kernel;
            this._uri = createKernelUri(hostUri || "kernel://vscode");
            this._kernel.host = this;
            this._scheduler = new KernelScheduler();
            this._defaultConnector = new Connector({ sender, receiver });
            this._connectors.push(this._defaultConnector);
        }
        get defaultConnector() {
            return this._defaultConnector;
        }
        get uri() {
            return this._uri;
        }
        tryGetKernelByRemoteUri(remoteUri) {
            return this._remoteUriToKernel.get(remoteUri);
        }
        trygetKernelByOriginUri(originUri) {
            return this._uriToKernel.get(originUri);
        }
        tryGetKernelInfo(kernel) {
            return this._kernelToKernelInfo.get(kernel);
        }
        addKernelInfo(kernel, kernelInfo) {
            kernelInfo.uri = createKernelUri(`${this._uri}${kernel.name}`);
            this._kernelToKernelInfo.set(kernel, kernelInfo);
            this._uriToKernel.set(kernelInfo.uri, kernel);
        }
        getKernel(kernelCommandEnvelope) {
            var _a;
            const uriToLookup = (_a = kernelCommandEnvelope.command.destinationUri) !== null && _a !== void 0 ? _a : kernelCommandEnvelope.command.originUri;
            let kernel = undefined;
            if (uriToLookup) {
                kernel = this._kernel.findKernelByUri(uriToLookup);
            }
            if (!kernel) {
                if (kernelCommandEnvelope.command.targetKernelName) {
                    kernel = this._kernel.findKernelByName(kernelCommandEnvelope.command.targetKernelName);
                }
            }
            kernel !== null && kernel !== void 0 ? kernel : (kernel = this._kernel);
            Logger.default.info(`Using Kernel ${kernel.name}`);
            return kernel;
        }
        connectProxyKernelOnDefaultConnector(localName, remoteKernelUri, aliases) {
            return this.connectProxyKernelOnConnector(localName, this._defaultConnector.sender, this._defaultConnector.receiver, remoteKernelUri, aliases);
        }
        tryAddConnector(connector) {
            if (!connector.remoteUris) {
                this._connectors.push(new Connector(connector));
                return true;
            }
            else {
                const found = connector.remoteUris.find(uri => this._connectors.find(c => c.canReach(uri)));
                if (!found) {
                    this._connectors.push(new Connector(connector));
                    return true;
                }
                return false;
            }
        }
        tryRemoveConnector(connector) {
            if (!connector.remoteUris) {
                for (let uri of connector.remoteUris) {
                    const index = this._connectors.findIndex(c => c.canReach(uri));
                    if (index >= 0) {
                        this._connectors.splice(index, 1);
                    }
                }
                return true;
            }
            else {
                return false;
            }
        }
        connectProxyKernel(localName, remoteKernelUri, aliases) {
            this._connectors; //?
            const connector = this._connectors.find(c => c.canReach(remoteKernelUri));
            if (!connector) {
                throw new Error(`Cannot find connector to reach ${remoteKernelUri}`);
            }
            let kernel = new ProxyKernel(localName, connector.sender, connector.receiver);
            kernel.kernelInfo.remoteUri = remoteKernelUri;
            this._kernel.add(kernel, aliases);
            return kernel;
        }
        connectProxyKernelOnConnector(localName, sender, receiver, remoteKernelUri, aliases) {
            let kernel = new ProxyKernel(localName, sender, receiver);
            kernel.kernelInfo.remoteUri = remoteKernelUri;
            this._kernel.add(kernel, aliases);
            return kernel;
        }
        tryGetConnector(remoteUri) {
            return this._connectors.find(c => c.canReach(remoteUri));
        }
        connect() {
            this._kernel.subscribeToKernelEvents(e => {
                Logger.default.info(`KernelHost forwarding event: ${JSON.stringify(e)}`);
                this._defaultConnector.sender.send(e);
            });
            this._defaultConnector.receiver.subscribe({
                next: (kernelCommandOrEventEnvelope) => {
                    if (isKernelCommandEnvelope(kernelCommandOrEventEnvelope)) {
                        Logger.default.info(`KernelHost dispacthing command: ${JSON.stringify(kernelCommandOrEventEnvelope)}`);
                        this._scheduler.runAsync(kernelCommandOrEventEnvelope, commandEnvelope => {
                            const kernel = this._kernel;
                            return kernel.send(commandEnvelope);
                        });
                    }
                }
            });
            this._defaultConnector.sender.send({ eventType: KernelReadyType, event: {}, routingSlip: [this._kernel.kernelInfo.uri] });
            this.publishKerneInfo();
        }
        publishKerneInfo() {
            const events = this.getKernelInfoProduced();
            for (const event of events) {
                this._defaultConnector.sender.send(event);
            }
        }
        getKernelInfoProduced() {
            let events = [];
            events.push({ eventType: KernelInfoProducedType, event: { kernelInfo: this._kernel.kernelInfo }, routingSlip: [this._kernel.kernelInfo.uri] });
            for (let kernel of this._kernel.childKernels) {
                events.push({ eventType: KernelInfoProducedType, event: { kernelInfo: kernel.kernelInfo }, routingSlip: [kernel.kernelInfo.uri] });
            }
            return events;
        }
    }

    // Copyright (c) .NET Foundation and contributors. All rights reserved.
    function createHost(global, compositeKernelName, configureRequire, logMessage, localToRemote, remoteToLocal, onReady) {
        Logger.configure(compositeKernelName, logMessage);
        global.interactive = {};
        configureRequire(global.interactive);
        const compositeKernel = new CompositeKernel(compositeKernelName);
        const kernelHost = new KernelHost(compositeKernel, KernelCommandAndEventSender.FromObserver(localToRemote), KernelCommandAndEventReceiver.FromObservable(remoteToLocal), `kernel://${compositeKernelName}`);
        kernelHost.defaultConnector.receiver.subscribe({
            next: (envelope) => {
                if (isKernelEventEnvelope(envelope) && envelope.eventType === KernelInfoProducedType) {
                    const kernelInfoProduced = envelope.event;
                    ensureOrUpdateProxyForKernelInfo(kernelInfoProduced, compositeKernel);
                }
            }
        });
        // use composite kernel as root
        global.kernel = {
            get root() {
                return compositeKernel;
            }
        };
        global[compositeKernelName] = {
            compositeKernel,
            kernelHost,
        };
        const jsKernel = new JavascriptKernel();
        compositeKernel.add(jsKernel, ["js"]);
        kernelHost.connect();
        onReady();
    }

    // Copyright (c) .NET Foundation and contributors. All rights reserved.
    function configure(global) {
        if (!global) {
            global = window;
        }
        const remoteToLocal = new Subject();
        const localToRemote = new Subject();
        localToRemote.subscribe({
            next: envelope => {
                // @ts-ignore
                postKernelMessage({ envelope });
            }
        });
        // @ts-ignore
        onDidReceiveKernelMessage((arg) => {
            var _a, _b;
            if (arg.envelope) {
                const envelope = (arg.envelope);
                if (isKernelEventEnvelope(envelope)) {
                    Logger.default.info(`channel got ${envelope.eventType} with token ${(_a = envelope.command) === null || _a === void 0 ? void 0 : _a.token} and id ${(_b = envelope.command) === null || _b === void 0 ? void 0 : _b.id}`);
                }
                remoteToLocal.next(envelope);
            }
        });
        createHost(global, 'webview', configureRequire, entry => {
            // @ts-ignore
            postKernelMessage({ logEntry: entry });
        }, localToRemote, remoteToLocal, () => {
            const kernelInfoProduced = (global['webview'].kernelHost).getKernelInfoProduced();
            const hostUri = (global['webview'].kernelHost).uri;
            // @ts-ignore
            postKernelMessage({ preloadCommand: '#!connect', kernelInfoProduced, hostUri });
        });
    }
    function configureRequire(interactive) {
        if ((typeof (require) !== typeof (Function)) || (typeof (require.config) !== typeof (Function))) {
            let require_script = document.createElement('script');
            require_script.setAttribute('src', 'https://cdnjs.cloudflare.com/ajax/libs/require.js/2.3.6/require.min.js');
            require_script.setAttribute('type', 'text/javascript');
            require_script.onload = function () {
                interactive.configureRequire = (confing) => {
                    return require.config(confing) || require;
                };
            };
            document.getElementsByTagName('head')[0].appendChild(require_script);
        }
        else {
            interactive.configureRequire = (confing) => {
                return require.config(confing) || require;
            };
        }
    }
    Logger.default.info(`setting up 'webview' host`);
    configure(window);
    Logger.default.info(`set up 'webview' host complete`);

    exports.configure = configure;

    Object.defineProperty(exports, '__esModule', { value: true });

    return exports;

})({});
//# sourceMappingURL=data:application/json;charset=utf-8;base64,eyJ2ZXJzaW9uIjozLCJmaWxlIjoia2VybmVsQXBpQm9vdHN0cmFwcGVyLmpzIiwic291cmNlcyI6WyIuLi9ub2RlX21vZHVsZXMvdnNjb2RlLXVyaS9saWIvZXNtL2luZGV4LmpzIiwiLi4vc3JjL3JvdXRpbmdzbGlwLnRzIiwiLi4vc3JjL2NvbnRyYWN0cy50cyIsIi4uL25vZGVfbW9kdWxlcy9yeGpzL2Rpc3QvZXNtNS9pbnRlcm5hbC91dGlsL2lzRnVuY3Rpb24uanMiLCIuLi9ub2RlX21vZHVsZXMvcnhqcy9kaXN0L2VzbTUvaW50ZXJuYWwvdXRpbC9jcmVhdGVFcnJvckNsYXNzLmpzIiwiLi4vbm9kZV9tb2R1bGVzL3J4anMvZGlzdC9lc201L2ludGVybmFsL3V0aWwvVW5zdWJzY3JpcHRpb25FcnJvci5qcyIsIi4uL25vZGVfbW9kdWxlcy9yeGpzL2Rpc3QvZXNtNS9pbnRlcm5hbC91dGlsL2FyclJlbW92ZS5qcyIsIi4uL25vZGVfbW9kdWxlcy9yeGpzL2Rpc3QvZXNtNS9pbnRlcm5hbC9TdWJzY3JpcHRpb24uanMiLCIuLi9ub2RlX21vZHVsZXMvcnhqcy9kaXN0L2VzbTUvaW50ZXJuYWwvY29uZmlnLmpzIiwiLi4vbm9kZV9tb2R1bGVzL3J4anMvZGlzdC9lc201L2ludGVybmFsL3NjaGVkdWxlci90aW1lb3V0UHJvdmlkZXIuanMiLCIuLi9ub2RlX21vZHVsZXMvcnhqcy9kaXN0L2VzbTUvaW50ZXJuYWwvdXRpbC9yZXBvcnRVbmhhbmRsZWRFcnJvci5qcyIsIi4uL25vZGVfbW9kdWxlcy9yeGpzL2Rpc3QvZXNtNS9pbnRlcm5hbC91dGlsL25vb3AuanMiLCIuLi9ub2RlX21vZHVsZXMvcnhqcy9kaXN0L2VzbTUvaW50ZXJuYWwvdXRpbC9lcnJvckNvbnRleHQuanMiLCIuLi9ub2RlX21vZHVsZXMvcnhqcy9kaXN0L2VzbTUvaW50ZXJuYWwvU3Vic2NyaWJlci5qcyIsIi4uL25vZGVfbW9kdWxlcy9yeGpzL2Rpc3QvZXNtNS9pbnRlcm5hbC9zeW1ib2wvb2JzZXJ2YWJsZS5qcyIsIi4uL25vZGVfbW9kdWxlcy9yeGpzL2Rpc3QvZXNtNS9pbnRlcm5hbC91dGlsL2lkZW50aXR5LmpzIiwiLi4vbm9kZV9tb2R1bGVzL3J4anMvZGlzdC9lc201L2ludGVybmFsL3V0aWwvcGlwZS5qcyIsIi4uL25vZGVfbW9kdWxlcy9yeGpzL2Rpc3QvZXNtNS9pbnRlcm5hbC9PYnNlcnZhYmxlLmpzIiwiLi4vbm9kZV9tb2R1bGVzL3J4anMvZGlzdC9lc201L2ludGVybmFsL3V0aWwvbGlmdC5qcyIsIi4uL25vZGVfbW9kdWxlcy9yeGpzL2Rpc3QvZXNtNS9pbnRlcm5hbC9vcGVyYXRvcnMvT3BlcmF0b3JTdWJzY3JpYmVyLmpzIiwiLi4vbm9kZV9tb2R1bGVzL3J4anMvZGlzdC9lc201L2ludGVybmFsL3V0aWwvT2JqZWN0VW5zdWJzY3JpYmVkRXJyb3IuanMiLCIuLi9ub2RlX21vZHVsZXMvcnhqcy9kaXN0L2VzbTUvaW50ZXJuYWwvU3ViamVjdC5qcyIsIi4uL25vZGVfbW9kdWxlcy9yeGpzL2Rpc3QvZXNtNS9pbnRlcm5hbC9vcGVyYXRvcnMvbWFwLmpzIiwiLi4vc3JjL3Byb21pc2VDb21wbGV0aW9uU291cmNlLnRzIiwiLi4vc3JjL2tlcm5lbEludm9jYXRpb25Db250ZXh0LnRzIiwiLi4vc3JjL3Rva2VuR2VuZXJhdG9yLnRzIiwiLi4vc3JjL2xvZ2dlci50cyIsIi4uL3NyYy9rZXJuZWxTY2hlZHVsZXIudHMiLCIuLi9zcmMva2VybmVsLnRzIiwiLi4vc3JjL2NvbXBvc2l0ZUtlcm5lbC50cyIsIi4uL3NyYy9jb25zb2xlQ2FwdHVyZS50cyIsIi4uL3NyYy9qYXZhc2NyaXB0S2VybmVsLnRzIiwiLi4vc3JjL2Nvbm5lY3Rpb24udHMiLCIuLi9zcmMvcHJveHlLZXJuZWwudHMiLCIuLi9zcmMva2VybmVsSG9zdC50cyIsIi4uL3NyYy93ZWJ2aWV3L2Zyb250RW5kSG9zdC50cyIsIi4uL3NyYy93ZWJ2aWV3L2tlcm5lbEFwaUJvb3RzdHJhcHBlci50cyJdLCJzb3VyY2VzQ29udGVudCI6WyJ2YXIgTElCOygoKT0+e1widXNlIHN0cmljdFwiO3ZhciB0PXs0NzA6dD0+e2Z1bmN0aW9uIGUodCl7aWYoXCJzdHJpbmdcIiE9dHlwZW9mIHQpdGhyb3cgbmV3IFR5cGVFcnJvcihcIlBhdGggbXVzdCBiZSBhIHN0cmluZy4gUmVjZWl2ZWQgXCIrSlNPTi5zdHJpbmdpZnkodCkpfWZ1bmN0aW9uIHIodCxlKXtmb3IodmFyIHIsbj1cIlwiLG89MCxpPS0xLGE9MCxoPTA7aDw9dC5sZW5ndGg7KytoKXtpZihoPHQubGVuZ3RoKXI9dC5jaGFyQ29kZUF0KGgpO2Vsc2V7aWYoNDc9PT1yKWJyZWFrO3I9NDd9aWYoNDc9PT1yKXtpZihpPT09aC0xfHwxPT09YSk7ZWxzZSBpZihpIT09aC0xJiYyPT09YSl7aWYobi5sZW5ndGg8Mnx8MiE9PW98fDQ2IT09bi5jaGFyQ29kZUF0KG4ubGVuZ3RoLTEpfHw0NiE9PW4uY2hhckNvZGVBdChuLmxlbmd0aC0yKSlpZihuLmxlbmd0aD4yKXt2YXIgcz1uLmxhc3RJbmRleE9mKFwiL1wiKTtpZihzIT09bi5sZW5ndGgtMSl7LTE9PT1zPyhuPVwiXCIsbz0wKTpvPShuPW4uc2xpY2UoMCxzKSkubGVuZ3RoLTEtbi5sYXN0SW5kZXhPZihcIi9cIiksaT1oLGE9MDtjb250aW51ZX19ZWxzZSBpZigyPT09bi5sZW5ndGh8fDE9PT1uLmxlbmd0aCl7bj1cIlwiLG89MCxpPWgsYT0wO2NvbnRpbnVlfWUmJihuLmxlbmd0aD4wP24rPVwiLy4uXCI6bj1cIi4uXCIsbz0yKX1lbHNlIG4ubGVuZ3RoPjA/bis9XCIvXCIrdC5zbGljZShpKzEsaCk6bj10LnNsaWNlKGkrMSxoKSxvPWgtaS0xO2k9aCxhPTB9ZWxzZSA0Nj09PXImJi0xIT09YT8rK2E6YT0tMX1yZXR1cm4gbn12YXIgbj17cmVzb2x2ZTpmdW5jdGlvbigpe2Zvcih2YXIgdCxuPVwiXCIsbz0hMSxpPWFyZ3VtZW50cy5sZW5ndGgtMTtpPj0tMSYmIW87aS0tKXt2YXIgYTtpPj0wP2E9YXJndW1lbnRzW2ldOih2b2lkIDA9PT10JiYodD1wcm9jZXNzLmN3ZCgpKSxhPXQpLGUoYSksMCE9PWEubGVuZ3RoJiYobj1hK1wiL1wiK24sbz00Nz09PWEuY2hhckNvZGVBdCgwKSl9cmV0dXJuIG49cihuLCFvKSxvP24ubGVuZ3RoPjA/XCIvXCIrbjpcIi9cIjpuLmxlbmd0aD4wP246XCIuXCJ9LG5vcm1hbGl6ZTpmdW5jdGlvbih0KXtpZihlKHQpLDA9PT10Lmxlbmd0aClyZXR1cm5cIi5cIjt2YXIgbj00Nz09PXQuY2hhckNvZGVBdCgwKSxvPTQ3PT09dC5jaGFyQ29kZUF0KHQubGVuZ3RoLTEpO3JldHVybiAwIT09KHQ9cih0LCFuKSkubGVuZ3RofHxufHwodD1cIi5cIiksdC5sZW5ndGg+MCYmbyYmKHQrPVwiL1wiKSxuP1wiL1wiK3Q6dH0saXNBYnNvbHV0ZTpmdW5jdGlvbih0KXtyZXR1cm4gZSh0KSx0Lmxlbmd0aD4wJiY0Nz09PXQuY2hhckNvZGVBdCgwKX0sam9pbjpmdW5jdGlvbigpe2lmKDA9PT1hcmd1bWVudHMubGVuZ3RoKXJldHVyblwiLlwiO2Zvcih2YXIgdCxyPTA7cjxhcmd1bWVudHMubGVuZ3RoOysrcil7dmFyIG89YXJndW1lbnRzW3JdO2Uobyksby5sZW5ndGg+MCYmKHZvaWQgMD09PXQ/dD1vOnQrPVwiL1wiK28pfXJldHVybiB2b2lkIDA9PT10P1wiLlwiOm4ubm9ybWFsaXplKHQpfSxyZWxhdGl2ZTpmdW5jdGlvbih0LHIpe2lmKGUodCksZShyKSx0PT09cilyZXR1cm5cIlwiO2lmKCh0PW4ucmVzb2x2ZSh0KSk9PT0ocj1uLnJlc29sdmUocikpKXJldHVyblwiXCI7Zm9yKHZhciBvPTE7bzx0Lmxlbmd0aCYmNDc9PT10LmNoYXJDb2RlQXQobyk7KytvKTtmb3IodmFyIGk9dC5sZW5ndGgsYT1pLW8saD0xO2g8ci5sZW5ndGgmJjQ3PT09ci5jaGFyQ29kZUF0KGgpOysraCk7Zm9yKHZhciBzPXIubGVuZ3RoLWgsYz1hPHM/YTpzLGY9LTEsdT0wO3U8PWM7Kyt1KXtpZih1PT09Yyl7aWYocz5jKXtpZig0Nz09PXIuY2hhckNvZGVBdChoK3UpKXJldHVybiByLnNsaWNlKGgrdSsxKTtpZigwPT09dSlyZXR1cm4gci5zbGljZShoK3UpfWVsc2UgYT5jJiYoNDc9PT10LmNoYXJDb2RlQXQobyt1KT9mPXU6MD09PXUmJihmPTApKTticmVha312YXIgbD10LmNoYXJDb2RlQXQobyt1KTtpZihsIT09ci5jaGFyQ29kZUF0KGgrdSkpYnJlYWs7NDc9PT1sJiYoZj11KX12YXIgcD1cIlwiO2Zvcih1PW8rZisxO3U8PWk7Kyt1KXUhPT1pJiY0NyE9PXQuY2hhckNvZGVBdCh1KXx8KDA9PT1wLmxlbmd0aD9wKz1cIi4uXCI6cCs9XCIvLi5cIik7cmV0dXJuIHAubGVuZ3RoPjA/cCtyLnNsaWNlKGgrZik6KGgrPWYsNDc9PT1yLmNoYXJDb2RlQXQoaCkmJisraCxyLnNsaWNlKGgpKX0sX21ha2VMb25nOmZ1bmN0aW9uKHQpe3JldHVybiB0fSxkaXJuYW1lOmZ1bmN0aW9uKHQpe2lmKGUodCksMD09PXQubGVuZ3RoKXJldHVyblwiLlwiO2Zvcih2YXIgcj10LmNoYXJDb2RlQXQoMCksbj00Nz09PXIsbz0tMSxpPSEwLGE9dC5sZW5ndGgtMTthPj0xOy0tYSlpZig0Nz09PShyPXQuY2hhckNvZGVBdChhKSkpe2lmKCFpKXtvPWE7YnJlYWt9fWVsc2UgaT0hMTtyZXR1cm4tMT09PW8/bj9cIi9cIjpcIi5cIjpuJiYxPT09bz9cIi8vXCI6dC5zbGljZSgwLG8pfSxiYXNlbmFtZTpmdW5jdGlvbih0LHIpe2lmKHZvaWQgMCE9PXImJlwic3RyaW5nXCIhPXR5cGVvZiByKXRocm93IG5ldyBUeXBlRXJyb3IoJ1wiZXh0XCIgYXJndW1lbnQgbXVzdCBiZSBhIHN0cmluZycpO2UodCk7dmFyIG4sbz0wLGk9LTEsYT0hMDtpZih2b2lkIDAhPT1yJiZyLmxlbmd0aD4wJiZyLmxlbmd0aDw9dC5sZW5ndGgpe2lmKHIubGVuZ3RoPT09dC5sZW5ndGgmJnI9PT10KXJldHVyblwiXCI7dmFyIGg9ci5sZW5ndGgtMSxzPS0xO2ZvcihuPXQubGVuZ3RoLTE7bj49MDstLW4pe3ZhciBjPXQuY2hhckNvZGVBdChuKTtpZig0Nz09PWMpe2lmKCFhKXtvPW4rMTticmVha319ZWxzZS0xPT09cyYmKGE9ITEscz1uKzEpLGg+PTAmJihjPT09ci5jaGFyQ29kZUF0KGgpPy0xPT0tLWgmJihpPW4pOihoPS0xLGk9cykpfXJldHVybiBvPT09aT9pPXM6LTE9PT1pJiYoaT10Lmxlbmd0aCksdC5zbGljZShvLGkpfWZvcihuPXQubGVuZ3RoLTE7bj49MDstLW4paWYoNDc9PT10LmNoYXJDb2RlQXQobikpe2lmKCFhKXtvPW4rMTticmVha319ZWxzZS0xPT09aSYmKGE9ITEsaT1uKzEpO3JldHVybi0xPT09aT9cIlwiOnQuc2xpY2UobyxpKX0sZXh0bmFtZTpmdW5jdGlvbih0KXtlKHQpO2Zvcih2YXIgcj0tMSxuPTAsbz0tMSxpPSEwLGE9MCxoPXQubGVuZ3RoLTE7aD49MDstLWgpe3ZhciBzPXQuY2hhckNvZGVBdChoKTtpZig0NyE9PXMpLTE9PT1vJiYoaT0hMSxvPWgrMSksNDY9PT1zPy0xPT09cj9yPWg6MSE9PWEmJihhPTEpOi0xIT09ciYmKGE9LTEpO2Vsc2UgaWYoIWkpe249aCsxO2JyZWFrfX1yZXR1cm4tMT09PXJ8fC0xPT09b3x8MD09PWF8fDE9PT1hJiZyPT09by0xJiZyPT09bisxP1wiXCI6dC5zbGljZShyLG8pfSxmb3JtYXQ6ZnVuY3Rpb24odCl7aWYobnVsbD09PXR8fFwib2JqZWN0XCIhPXR5cGVvZiB0KXRocm93IG5ldyBUeXBlRXJyb3IoJ1RoZSBcInBhdGhPYmplY3RcIiBhcmd1bWVudCBtdXN0IGJlIG9mIHR5cGUgT2JqZWN0LiBSZWNlaXZlZCB0eXBlICcrdHlwZW9mIHQpO3JldHVybiBmdW5jdGlvbih0LGUpe3ZhciByPWUuZGlyfHxlLnJvb3Qsbj1lLmJhc2V8fChlLm5hbWV8fFwiXCIpKyhlLmV4dHx8XCJcIik7cmV0dXJuIHI/cj09PWUucm9vdD9yK246citcIi9cIituOm59KDAsdCl9LHBhcnNlOmZ1bmN0aW9uKHQpe2UodCk7dmFyIHI9e3Jvb3Q6XCJcIixkaXI6XCJcIixiYXNlOlwiXCIsZXh0OlwiXCIsbmFtZTpcIlwifTtpZigwPT09dC5sZW5ndGgpcmV0dXJuIHI7dmFyIG4sbz10LmNoYXJDb2RlQXQoMCksaT00Nz09PW87aT8oci5yb290PVwiL1wiLG49MSk6bj0wO2Zvcih2YXIgYT0tMSxoPTAscz0tMSxjPSEwLGY9dC5sZW5ndGgtMSx1PTA7Zj49bjstLWYpaWYoNDchPT0obz10LmNoYXJDb2RlQXQoZikpKS0xPT09cyYmKGM9ITEscz1mKzEpLDQ2PT09bz8tMT09PWE/YT1mOjEhPT11JiYodT0xKTotMSE9PWEmJih1PS0xKTtlbHNlIGlmKCFjKXtoPWYrMTticmVha31yZXR1cm4tMT09PWF8fC0xPT09c3x8MD09PXV8fDE9PT11JiZhPT09cy0xJiZhPT09aCsxPy0xIT09cyYmKHIuYmFzZT1yLm5hbWU9MD09PWgmJmk/dC5zbGljZSgxLHMpOnQuc2xpY2UoaCxzKSk6KDA9PT1oJiZpPyhyLm5hbWU9dC5zbGljZSgxLGEpLHIuYmFzZT10LnNsaWNlKDEscykpOihyLm5hbWU9dC5zbGljZShoLGEpLHIuYmFzZT10LnNsaWNlKGgscykpLHIuZXh0PXQuc2xpY2UoYSxzKSksaD4wP3IuZGlyPXQuc2xpY2UoMCxoLTEpOmkmJihyLmRpcj1cIi9cIikscn0sc2VwOlwiL1wiLGRlbGltaXRlcjpcIjpcIix3aW4zMjpudWxsLHBvc2l4Om51bGx9O24ucG9zaXg9bix0LmV4cG9ydHM9bn19LGU9e307ZnVuY3Rpb24gcihuKXt2YXIgbz1lW25dO2lmKHZvaWQgMCE9PW8pcmV0dXJuIG8uZXhwb3J0czt2YXIgaT1lW25dPXtleHBvcnRzOnt9fTtyZXR1cm4gdFtuXShpLGkuZXhwb3J0cyxyKSxpLmV4cG9ydHN9ci5kPSh0LGUpPT57Zm9yKHZhciBuIGluIGUpci5vKGUsbikmJiFyLm8odCxuKSYmT2JqZWN0LmRlZmluZVByb3BlcnR5KHQsbix7ZW51bWVyYWJsZTohMCxnZXQ6ZVtuXX0pfSxyLm89KHQsZSk9Pk9iamVjdC5wcm90b3R5cGUuaGFzT3duUHJvcGVydHkuY2FsbCh0LGUpLHIucj10PT57XCJ1bmRlZmluZWRcIiE9dHlwZW9mIFN5bWJvbCYmU3ltYm9sLnRvU3RyaW5nVGFnJiZPYmplY3QuZGVmaW5lUHJvcGVydHkodCxTeW1ib2wudG9TdHJpbmdUYWcse3ZhbHVlOlwiTW9kdWxlXCJ9KSxPYmplY3QuZGVmaW5lUHJvcGVydHkodCxcIl9fZXNNb2R1bGVcIix7dmFsdWU6ITB9KX07dmFyIG49e307KCgpPT57dmFyIHQ7aWYoci5yKG4pLHIuZChuLHtVUkk6KCk9PnAsVXRpbHM6KCk9Pl99KSxcIm9iamVjdFwiPT10eXBlb2YgcHJvY2Vzcyl0PVwid2luMzJcIj09PXByb2Nlc3MucGxhdGZvcm07ZWxzZSBpZihcIm9iamVjdFwiPT10eXBlb2YgbmF2aWdhdG9yKXt2YXIgZT1uYXZpZ2F0b3IudXNlckFnZW50O3Q9ZS5pbmRleE9mKFwiV2luZG93c1wiKT49MH12YXIgbyxpLGE9KG89ZnVuY3Rpb24odCxlKXtyZXR1cm4gbz1PYmplY3Quc2V0UHJvdG90eXBlT2Z8fHtfX3Byb3RvX186W119aW5zdGFuY2VvZiBBcnJheSYmZnVuY3Rpb24odCxlKXt0Ll9fcHJvdG9fXz1lfXx8ZnVuY3Rpb24odCxlKXtmb3IodmFyIHIgaW4gZSlPYmplY3QucHJvdG90eXBlLmhhc093blByb3BlcnR5LmNhbGwoZSxyKSYmKHRbcl09ZVtyXSl9LG8odCxlKX0sZnVuY3Rpb24odCxlKXtpZihcImZ1bmN0aW9uXCIhPXR5cGVvZiBlJiZudWxsIT09ZSl0aHJvdyBuZXcgVHlwZUVycm9yKFwiQ2xhc3MgZXh0ZW5kcyB2YWx1ZSBcIitTdHJpbmcoZSkrXCIgaXMgbm90IGEgY29uc3RydWN0b3Igb3IgbnVsbFwiKTtmdW5jdGlvbiByKCl7dGhpcy5jb25zdHJ1Y3Rvcj10fW8odCxlKSx0LnByb3RvdHlwZT1udWxsPT09ZT9PYmplY3QuY3JlYXRlKGUpOihyLnByb3RvdHlwZT1lLnByb3RvdHlwZSxuZXcgcil9KSxoPS9eXFx3W1xcd1xcZCsuLV0qJC8scz0vXlxcLy8sYz0vXlxcL1xcLy8sZj1cIlwiLHU9XCIvXCIsbD0vXigoW146Lz8jXSs/KTopPyhcXC9cXC8oW14vPyNdKikpPyhbXj8jXSopKFxcPyhbXiNdKikpPygjKC4qKSk/LyxwPWZ1bmN0aW9uKCl7ZnVuY3Rpb24gZSh0LGUscixuLG8saSl7dm9pZCAwPT09aSYmKGk9ITEpLFwib2JqZWN0XCI9PXR5cGVvZiB0Pyh0aGlzLnNjaGVtZT10LnNjaGVtZXx8Zix0aGlzLmF1dGhvcml0eT10LmF1dGhvcml0eXx8Zix0aGlzLnBhdGg9dC5wYXRofHxmLHRoaXMucXVlcnk9dC5xdWVyeXx8Zix0aGlzLmZyYWdtZW50PXQuZnJhZ21lbnR8fGYpOih0aGlzLnNjaGVtZT1mdW5jdGlvbih0LGUpe3JldHVybiB0fHxlP3Q6XCJmaWxlXCJ9KHQsaSksdGhpcy5hdXRob3JpdHk9ZXx8Zix0aGlzLnBhdGg9ZnVuY3Rpb24odCxlKXtzd2l0Y2godCl7Y2FzZVwiaHR0cHNcIjpjYXNlXCJodHRwXCI6Y2FzZVwiZmlsZVwiOmU/ZVswXSE9PXUmJihlPXUrZSk6ZT11fXJldHVybiBlfSh0aGlzLnNjaGVtZSxyfHxmKSx0aGlzLnF1ZXJ5PW58fGYsdGhpcy5mcmFnbWVudD1vfHxmLGZ1bmN0aW9uKHQsZSl7aWYoIXQuc2NoZW1lJiZlKXRocm93IG5ldyBFcnJvcignW1VyaUVycm9yXTogU2NoZW1lIGlzIG1pc3Npbmc6IHtzY2hlbWU6IFwiXCIsIGF1dGhvcml0eTogXCInLmNvbmNhdCh0LmF1dGhvcml0eSwnXCIsIHBhdGg6IFwiJykuY29uY2F0KHQucGF0aCwnXCIsIHF1ZXJ5OiBcIicpLmNvbmNhdCh0LnF1ZXJ5LCdcIiwgZnJhZ21lbnQ6IFwiJykuY29uY2F0KHQuZnJhZ21lbnQsJ1wifScpKTtpZih0LnNjaGVtZSYmIWgudGVzdCh0LnNjaGVtZSkpdGhyb3cgbmV3IEVycm9yKFwiW1VyaUVycm9yXTogU2NoZW1lIGNvbnRhaW5zIGlsbGVnYWwgY2hhcmFjdGVycy5cIik7aWYodC5wYXRoKWlmKHQuYXV0aG9yaXR5KXtpZighcy50ZXN0KHQucGF0aCkpdGhyb3cgbmV3IEVycm9yKCdbVXJpRXJyb3JdOiBJZiBhIFVSSSBjb250YWlucyBhbiBhdXRob3JpdHkgY29tcG9uZW50LCB0aGVuIHRoZSBwYXRoIGNvbXBvbmVudCBtdXN0IGVpdGhlciBiZSBlbXB0eSBvciBiZWdpbiB3aXRoIGEgc2xhc2ggKFwiL1wiKSBjaGFyYWN0ZXInKX1lbHNlIGlmKGMudGVzdCh0LnBhdGgpKXRocm93IG5ldyBFcnJvcignW1VyaUVycm9yXTogSWYgYSBVUkkgZG9lcyBub3QgY29udGFpbiBhbiBhdXRob3JpdHkgY29tcG9uZW50LCB0aGVuIHRoZSBwYXRoIGNhbm5vdCBiZWdpbiB3aXRoIHR3byBzbGFzaCBjaGFyYWN0ZXJzIChcIi8vXCIpJyl9KHRoaXMsaSkpfXJldHVybiBlLmlzVXJpPWZ1bmN0aW9uKHQpe3JldHVybiB0IGluc3RhbmNlb2YgZXx8ISF0JiZcInN0cmluZ1wiPT10eXBlb2YgdC5hdXRob3JpdHkmJlwic3RyaW5nXCI9PXR5cGVvZiB0LmZyYWdtZW50JiZcInN0cmluZ1wiPT10eXBlb2YgdC5wYXRoJiZcInN0cmluZ1wiPT10eXBlb2YgdC5xdWVyeSYmXCJzdHJpbmdcIj09dHlwZW9mIHQuc2NoZW1lJiZcInN0cmluZ1wiPT10eXBlb2YgdC5mc1BhdGgmJlwiZnVuY3Rpb25cIj09dHlwZW9mIHQud2l0aCYmXCJmdW5jdGlvblwiPT10eXBlb2YgdC50b1N0cmluZ30sT2JqZWN0LmRlZmluZVByb3BlcnR5KGUucHJvdG90eXBlLFwiZnNQYXRoXCIse2dldDpmdW5jdGlvbigpe3JldHVybiBiKHRoaXMsITEpfSxlbnVtZXJhYmxlOiExLGNvbmZpZ3VyYWJsZTohMH0pLGUucHJvdG90eXBlLndpdGg9ZnVuY3Rpb24odCl7aWYoIXQpcmV0dXJuIHRoaXM7dmFyIGU9dC5zY2hlbWUscj10LmF1dGhvcml0eSxuPXQucGF0aCxvPXQucXVlcnksaT10LmZyYWdtZW50O3JldHVybiB2b2lkIDA9PT1lP2U9dGhpcy5zY2hlbWU6bnVsbD09PWUmJihlPWYpLHZvaWQgMD09PXI/cj10aGlzLmF1dGhvcml0eTpudWxsPT09ciYmKHI9Ziksdm9pZCAwPT09bj9uPXRoaXMucGF0aDpudWxsPT09biYmKG49Ziksdm9pZCAwPT09bz9vPXRoaXMucXVlcnk6bnVsbD09PW8mJihvPWYpLHZvaWQgMD09PWk/aT10aGlzLmZyYWdtZW50Om51bGw9PT1pJiYoaT1mKSxlPT09dGhpcy5zY2hlbWUmJnI9PT10aGlzLmF1dGhvcml0eSYmbj09PXRoaXMucGF0aCYmbz09PXRoaXMucXVlcnkmJmk9PT10aGlzLmZyYWdtZW50P3RoaXM6bmV3IGQoZSxyLG4sbyxpKX0sZS5wYXJzZT1mdW5jdGlvbih0LGUpe3ZvaWQgMD09PWUmJihlPSExKTt2YXIgcj1sLmV4ZWModCk7cmV0dXJuIHI/bmV3IGQoclsyXXx8Zix4KHJbNF18fGYpLHgocls1XXx8ZikseChyWzddfHxmKSx4KHJbOV18fGYpLGUpOm5ldyBkKGYsZixmLGYsZil9LGUuZmlsZT1mdW5jdGlvbihlKXt2YXIgcj1mO2lmKHQmJihlPWUucmVwbGFjZSgvXFxcXC9nLHUpKSxlWzBdPT09dSYmZVsxXT09PXUpe3ZhciBuPWUuaW5kZXhPZih1LDIpOy0xPT09bj8ocj1lLnN1YnN0cmluZygyKSxlPXUpOihyPWUuc3Vic3RyaW5nKDIsbiksZT1lLnN1YnN0cmluZyhuKXx8dSl9cmV0dXJuIG5ldyBkKFwiZmlsZVwiLHIsZSxmLGYpfSxlLmZyb209ZnVuY3Rpb24odCl7cmV0dXJuIG5ldyBkKHQuc2NoZW1lLHQuYXV0aG9yaXR5LHQucGF0aCx0LnF1ZXJ5LHQuZnJhZ21lbnQpfSxlLnByb3RvdHlwZS50b1N0cmluZz1mdW5jdGlvbih0KXtyZXR1cm4gdm9pZCAwPT09dCYmKHQ9ITEpLEModGhpcyx0KX0sZS5wcm90b3R5cGUudG9KU09OPWZ1bmN0aW9uKCl7cmV0dXJuIHRoaXN9LGUucmV2aXZlPWZ1bmN0aW9uKHQpe2lmKHQpe2lmKHQgaW5zdGFuY2VvZiBlKXJldHVybiB0O3ZhciByPW5ldyBkKHQpO3JldHVybiByLl9mb3JtYXR0ZWQ9dC5leHRlcm5hbCxyLl9mc1BhdGg9dC5fc2VwPT09Zz90LmZzUGF0aDpudWxsLHJ9cmV0dXJuIHR9LGV9KCksZz10PzE6dm9pZCAwLGQ9ZnVuY3Rpb24odCl7ZnVuY3Rpb24gZSgpe3ZhciBlPW51bGwhPT10JiZ0LmFwcGx5KHRoaXMsYXJndW1lbnRzKXx8dGhpcztyZXR1cm4gZS5fZm9ybWF0dGVkPW51bGwsZS5fZnNQYXRoPW51bGwsZX1yZXR1cm4gYShlLHQpLE9iamVjdC5kZWZpbmVQcm9wZXJ0eShlLnByb3RvdHlwZSxcImZzUGF0aFwiLHtnZXQ6ZnVuY3Rpb24oKXtyZXR1cm4gdGhpcy5fZnNQYXRofHwodGhpcy5fZnNQYXRoPWIodGhpcywhMSkpLHRoaXMuX2ZzUGF0aH0sZW51bWVyYWJsZTohMSxjb25maWd1cmFibGU6ITB9KSxlLnByb3RvdHlwZS50b1N0cmluZz1mdW5jdGlvbih0KXtyZXR1cm4gdm9pZCAwPT09dCYmKHQ9ITEpLHQ/Qyh0aGlzLCEwKToodGhpcy5fZm9ybWF0dGVkfHwodGhpcy5fZm9ybWF0dGVkPUModGhpcywhMSkpLHRoaXMuX2Zvcm1hdHRlZCl9LGUucHJvdG90eXBlLnRvSlNPTj1mdW5jdGlvbigpe3ZhciB0PXskbWlkOjF9O3JldHVybiB0aGlzLl9mc1BhdGgmJih0LmZzUGF0aD10aGlzLl9mc1BhdGgsdC5fc2VwPWcpLHRoaXMuX2Zvcm1hdHRlZCYmKHQuZXh0ZXJuYWw9dGhpcy5fZm9ybWF0dGVkKSx0aGlzLnBhdGgmJih0LnBhdGg9dGhpcy5wYXRoKSx0aGlzLnNjaGVtZSYmKHQuc2NoZW1lPXRoaXMuc2NoZW1lKSx0aGlzLmF1dGhvcml0eSYmKHQuYXV0aG9yaXR5PXRoaXMuYXV0aG9yaXR5KSx0aGlzLnF1ZXJ5JiYodC5xdWVyeT10aGlzLnF1ZXJ5KSx0aGlzLmZyYWdtZW50JiYodC5mcmFnbWVudD10aGlzLmZyYWdtZW50KSx0fSxlfShwKSx2PSgoaT17fSlbNThdPVwiJTNBXCIsaVs0N109XCIlMkZcIixpWzYzXT1cIiUzRlwiLGlbMzVdPVwiJTIzXCIsaVs5MV09XCIlNUJcIixpWzkzXT1cIiU1RFwiLGlbNjRdPVwiJTQwXCIsaVszM109XCIlMjFcIixpWzM2XT1cIiUyNFwiLGlbMzhdPVwiJTI2XCIsaVszOV09XCIlMjdcIixpWzQwXT1cIiUyOFwiLGlbNDFdPVwiJTI5XCIsaVs0Ml09XCIlMkFcIixpWzQzXT1cIiUyQlwiLGlbNDRdPVwiJTJDXCIsaVs1OV09XCIlM0JcIixpWzYxXT1cIiUzRFwiLGlbMzJdPVwiJTIwXCIsaSk7ZnVuY3Rpb24geSh0LGUpe2Zvcih2YXIgcj12b2lkIDAsbj0tMSxvPTA7bzx0Lmxlbmd0aDtvKyspe3ZhciBpPXQuY2hhckNvZGVBdChvKTtpZihpPj05NyYmaTw9MTIyfHxpPj02NSYmaTw9OTB8fGk+PTQ4JiZpPD01N3x8NDU9PT1pfHw0Nj09PWl8fDk1PT09aXx8MTI2PT09aXx8ZSYmNDc9PT1pKS0xIT09biYmKHIrPWVuY29kZVVSSUNvbXBvbmVudCh0LnN1YnN0cmluZyhuLG8pKSxuPS0xKSx2b2lkIDAhPT1yJiYocis9dC5jaGFyQXQobykpO2Vsc2V7dm9pZCAwPT09ciYmKHI9dC5zdWJzdHIoMCxvKSk7dmFyIGE9dltpXTt2b2lkIDAhPT1hPygtMSE9PW4mJihyKz1lbmNvZGVVUklDb21wb25lbnQodC5zdWJzdHJpbmcobixvKSksbj0tMSkscis9YSk6LTE9PT1uJiYobj1vKX19cmV0dXJuLTEhPT1uJiYocis9ZW5jb2RlVVJJQ29tcG9uZW50KHQuc3Vic3RyaW5nKG4pKSksdm9pZCAwIT09cj9yOnR9ZnVuY3Rpb24gbSh0KXtmb3IodmFyIGU9dm9pZCAwLHI9MDtyPHQubGVuZ3RoO3IrKyl7dmFyIG49dC5jaGFyQ29kZUF0KHIpOzM1PT09bnx8NjM9PT1uPyh2b2lkIDA9PT1lJiYoZT10LnN1YnN0cigwLHIpKSxlKz12W25dKTp2b2lkIDAhPT1lJiYoZSs9dFtyXSl9cmV0dXJuIHZvaWQgMCE9PWU/ZTp0fWZ1bmN0aW9uIGIoZSxyKXt2YXIgbjtyZXR1cm4gbj1lLmF1dGhvcml0eSYmZS5wYXRoLmxlbmd0aD4xJiZcImZpbGVcIj09PWUuc2NoZW1lP1wiLy9cIi5jb25jYXQoZS5hdXRob3JpdHkpLmNvbmNhdChlLnBhdGgpOjQ3PT09ZS5wYXRoLmNoYXJDb2RlQXQoMCkmJihlLnBhdGguY2hhckNvZGVBdCgxKT49NjUmJmUucGF0aC5jaGFyQ29kZUF0KDEpPD05MHx8ZS5wYXRoLmNoYXJDb2RlQXQoMSk+PTk3JiZlLnBhdGguY2hhckNvZGVBdCgxKTw9MTIyKSYmNTg9PT1lLnBhdGguY2hhckNvZGVBdCgyKT9yP2UucGF0aC5zdWJzdHIoMSk6ZS5wYXRoWzFdLnRvTG93ZXJDYXNlKCkrZS5wYXRoLnN1YnN0cigyKTplLnBhdGgsdCYmKG49bi5yZXBsYWNlKC9cXC8vZyxcIlxcXFxcIikpLG59ZnVuY3Rpb24gQyh0LGUpe3ZhciByPWU/bTp5LG49XCJcIixvPXQuc2NoZW1lLGk9dC5hdXRob3JpdHksYT10LnBhdGgsaD10LnF1ZXJ5LHM9dC5mcmFnbWVudDtpZihvJiYobis9byxuKz1cIjpcIiksKGl8fFwiZmlsZVwiPT09bykmJihuKz11LG4rPXUpLGkpe3ZhciBjPWkuaW5kZXhPZihcIkBcIik7aWYoLTEhPT1jKXt2YXIgZj1pLnN1YnN0cigwLGMpO2k9aS5zdWJzdHIoYysxKSwtMT09PShjPWYuaW5kZXhPZihcIjpcIikpP24rPXIoZiwhMSk6KG4rPXIoZi5zdWJzdHIoMCxjKSwhMSksbis9XCI6XCIsbis9cihmLnN1YnN0cihjKzEpLCExKSksbis9XCJAXCJ9LTE9PT0oYz0oaT1pLnRvTG93ZXJDYXNlKCkpLmluZGV4T2YoXCI6XCIpKT9uKz1yKGksITEpOihuKz1yKGkuc3Vic3RyKDAsYyksITEpLG4rPWkuc3Vic3RyKGMpKX1pZihhKXtpZihhLmxlbmd0aD49MyYmNDc9PT1hLmNoYXJDb2RlQXQoMCkmJjU4PT09YS5jaGFyQ29kZUF0KDIpKShsPWEuY2hhckNvZGVBdCgxKSk+PTY1JiZsPD05MCYmKGE9XCIvXCIuY29uY2F0KFN0cmluZy5mcm9tQ2hhckNvZGUobCszMiksXCI6XCIpLmNvbmNhdChhLnN1YnN0cigzKSkpO2Vsc2UgaWYoYS5sZW5ndGg+PTImJjU4PT09YS5jaGFyQ29kZUF0KDEpKXt2YXIgbDsobD1hLmNoYXJDb2RlQXQoMCkpPj02NSYmbDw9OTAmJihhPVwiXCIuY29uY2F0KFN0cmluZy5mcm9tQ2hhckNvZGUobCszMiksXCI6XCIpLmNvbmNhdChhLnN1YnN0cigyKSkpfW4rPXIoYSwhMCl9cmV0dXJuIGgmJihuKz1cIj9cIixuKz1yKGgsITEpKSxzJiYobis9XCIjXCIsbis9ZT9zOnkocywhMSkpLG59ZnVuY3Rpb24gQSh0KXt0cnl7cmV0dXJuIGRlY29kZVVSSUNvbXBvbmVudCh0KX1jYXRjaChlKXtyZXR1cm4gdC5sZW5ndGg+Mz90LnN1YnN0cigwLDMpK0EodC5zdWJzdHIoMykpOnR9fXZhciB3PS8oJVswLTlBLVphLXpdWzAtOUEtWmEtel0pKy9nO2Z1bmN0aW9uIHgodCl7cmV0dXJuIHQubWF0Y2godyk/dC5yZXBsYWNlKHcsKGZ1bmN0aW9uKHQpe3JldHVybiBBKHQpfSkpOnR9dmFyIF8sTz1yKDQ3MCksUD1mdW5jdGlvbih0LGUscil7aWYocnx8Mj09PWFyZ3VtZW50cy5sZW5ndGgpZm9yKHZhciBuLG89MCxpPWUubGVuZ3RoO288aTtvKyspIW4mJm8gaW4gZXx8KG58fChuPUFycmF5LnByb3RvdHlwZS5zbGljZS5jYWxsKGUsMCxvKSksbltvXT1lW29dKTtyZXR1cm4gdC5jb25jYXQobnx8QXJyYXkucHJvdG90eXBlLnNsaWNlLmNhbGwoZSkpfSxqPU8ucG9zaXh8fE8sVT1cIi9cIjshZnVuY3Rpb24odCl7dC5qb2luUGF0aD1mdW5jdGlvbih0KXtmb3IodmFyIGU9W10scj0xO3I8YXJndW1lbnRzLmxlbmd0aDtyKyspZVtyLTFdPWFyZ3VtZW50c1tyXTtyZXR1cm4gdC53aXRoKHtwYXRoOmouam9pbi5hcHBseShqLFAoW3QucGF0aF0sZSwhMSkpfSl9LHQucmVzb2x2ZVBhdGg9ZnVuY3Rpb24odCl7Zm9yKHZhciBlPVtdLHI9MTtyPGFyZ3VtZW50cy5sZW5ndGg7cisrKWVbci0xXT1hcmd1bWVudHNbcl07dmFyIG49dC5wYXRoLG89ITE7blswXSE9PVUmJihuPVUrbixvPSEwKTt2YXIgaT1qLnJlc29sdmUuYXBwbHkoaixQKFtuXSxlLCExKSk7cmV0dXJuIG8mJmlbMF09PT1VJiYhdC5hdXRob3JpdHkmJihpPWkuc3Vic3RyaW5nKDEpKSx0LndpdGgoe3BhdGg6aX0pfSx0LmRpcm5hbWU9ZnVuY3Rpb24odCl7aWYoMD09PXQucGF0aC5sZW5ndGh8fHQucGF0aD09PVUpcmV0dXJuIHQ7dmFyIGU9ai5kaXJuYW1lKHQucGF0aCk7cmV0dXJuIDE9PT1lLmxlbmd0aCYmNDY9PT1lLmNoYXJDb2RlQXQoMCkmJihlPVwiXCIpLHQud2l0aCh7cGF0aDplfSl9LHQuYmFzZW5hbWU9ZnVuY3Rpb24odCl7cmV0dXJuIGouYmFzZW5hbWUodC5wYXRoKX0sdC5leHRuYW1lPWZ1bmN0aW9uKHQpe3JldHVybiBqLmV4dG5hbWUodC5wYXRoKX19KF98fChfPXt9KSl9KSgpLExJQj1ufSkoKTtleHBvcnQgY29uc3R7VVJJLFV0aWxzfT1MSUI7XG4vLyMgc291cmNlTWFwcGluZ1VSTD1pbmRleC5qcy5tYXAiLCIvLyBDb3B5cmlnaHQgKGMpIC5ORVQgRm91bmRhdGlvbiBhbmQgY29udHJpYnV0b3JzLiBBbGwgcmlnaHRzIHJlc2VydmVkLlxyXG4vLyBMaWNlbnNlZCB1bmRlciB0aGUgTUlUIGxpY2Vuc2UuIFNlZSBMSUNFTlNFIGZpbGUgaW4gdGhlIHByb2plY3Qgcm9vdCBmb3IgZnVsbCBsaWNlbnNlIGluZm9ybWF0aW9uLlxyXG5cclxuaW1wb3J0ICogYXMgY29udHJhY3RzIGZyb20gJy4vY29udHJhY3RzJztcclxuaW1wb3J0IHsgVVJJIH0gZnJvbSAndnNjb2RlLXVyaSc7XHJcbmltcG9ydCB7IEtlcm5lbENvbW1hbmRPckV2ZW50RW52ZWxvcGUgfSBmcm9tICcuL2Nvbm5lY3Rpb24nO1xyXG5pbXBvcnQgeyB0aHJvd0Vycm9yIH0gZnJvbSAncnhqcyc7XHJcblxyXG5cclxuZXhwb3J0IGZ1bmN0aW9uIGNyZWF0ZUtlcm5lbFVyaShrZXJuZWxVcmk6IHN0cmluZyk6IHN0cmluZyB7XHJcbiAgICBrZXJuZWxVcmk7Ly8/XHJcbiAgICBjb25zdCB1cmkgPSBVUkkucGFyc2Uoa2VybmVsVXJpKTtcclxuICAgIHVyaS5hdXRob3JpdHk7Ly8/XHJcbiAgICB1cmkucGF0aDsvLz9cclxuICAgIGxldCBhYnNvbHV0ZVVyaSA9IGAke3VyaS5zY2hlbWV9Oi8vJHt1cmkuYXV0aG9yaXR5fSR7dXJpLnBhdGggfHwgXCIvXCJ9YDtcclxuICAgIHJldHVybiBhYnNvbHV0ZVVyaTsvLz9cclxufVxyXG5cclxuZXhwb3J0IGZ1bmN0aW9uIGNyZWF0ZUtlcm5lbFVyaVdpdGhRdWVyeShrZXJuZWxVcmk6IHN0cmluZyk6IHN0cmluZyB7XHJcbiAgICBrZXJuZWxVcmk7Ly8/XHJcbiAgICBjb25zdCB1cmkgPSBVUkkucGFyc2Uoa2VybmVsVXJpKTtcclxuICAgIHVyaS5hdXRob3JpdHk7Ly8/XHJcbiAgICB1cmkucGF0aDsvLz9cclxuICAgIGxldCBhYnNvbHV0ZVVyaSA9IGAke3VyaS5zY2hlbWV9Oi8vJHt1cmkuYXV0aG9yaXR5fSR7dXJpLnBhdGggfHwgXCIvXCJ9YDtcclxuICAgIGlmICh1cmkucXVlcnkpIHtcclxuICAgICAgICBhYnNvbHV0ZVVyaSArPSBgPyR7dXJpLnF1ZXJ5fWA7XHJcbiAgICB9XHJcbiAgICByZXR1cm4gYWJzb2x1dGVVcmk7Ly8/XHJcbn1cclxuXHJcbmV4cG9ydCBmdW5jdGlvbiBzdGFtcENvbW1hbmRSb3V0aW5nU2xpcEFzQXJyaXZlZChrZXJuZWxDb21tYW5kRW52ZWxvcGU6IGNvbnRyYWN0cy5LZXJuZWxDb21tYW5kRW52ZWxvcGUsIGtlcm5lbFVyaTogc3RyaW5nKSB7XHJcbiAgICBzdGFtcENvbW1hbmRSb3V0aW5nU2xpcEFzKGtlcm5lbENvbW1hbmRFbnZlbG9wZSwga2VybmVsVXJpLCBcImFycml2ZWRcIik7XHJcbn1cclxuXHJcbmV4cG9ydCBmdW5jdGlvbiBzdGFtcENvbW1hbmRSb3V0aW5nU2xpcChrZXJuZWxDb21tYW5kRW52ZWxvcGU6IGNvbnRyYWN0cy5LZXJuZWxDb21tYW5kRW52ZWxvcGUsIGtlcm5lbFVyaTogc3RyaW5nKSB7XHJcbiAgICBpZiAoa2VybmVsQ29tbWFuZEVudmVsb3BlLnJvdXRpbmdTbGlwID09PSB1bmRlZmluZWQgfHwga2VybmVsQ29tbWFuZEVudmVsb3BlLnJvdXRpbmdTbGlwID09PSBudWxsKSB7XHJcbiAgICAgICAgdGhyb3cgbmV3IEVycm9yKFwiVGhlIGNvbW1hbmQgZG9lcyBub3QgaGF2ZSBhIHJvdXRpbmcgc2xpcFwiKTtcclxuICAgIH1cclxuICAgIGtlcm5lbENvbW1hbmRFbnZlbG9wZS5yb3V0aW5nU2xpcDsvLz9cclxuICAgIGtlcm5lbFVyaTsvLz9cclxuICAgIGxldCBhYnNvbHV0ZVVyaSA9IGNyZWF0ZUtlcm5lbFVyaShrZXJuZWxVcmkpOyAvLz9cclxuICAgIGlmIChrZXJuZWxDb21tYW5kRW52ZWxvcGUucm91dGluZ1NsaXAuZmluZChlID0+IGUgPT09IGFic29sdXRlVXJpKSkge1xyXG4gICAgICAgIHRocm93IEVycm9yKGBUaGUgdXJpICR7YWJzb2x1dGVVcml9IGlzIGFscmVhZHkgaW4gdGhlIHJvdXRpbmcgc2xpcCBbJHtrZXJuZWxDb21tYW5kRW52ZWxvcGUucm91dGluZ1NsaXB9XWApO1xyXG4gICAgfSBlbHNlIGlmIChrZXJuZWxDb21tYW5kRW52ZWxvcGUucm91dGluZ1NsaXAuZmluZChlID0+IGUuc3RhcnRzV2l0aChhYnNvbHV0ZVVyaSkpKSB7XHJcbiAgICAgICAga2VybmVsQ29tbWFuZEVudmVsb3BlLnJvdXRpbmdTbGlwLnB1c2goYWJzb2x1dGVVcmkpO1xyXG4gICAgfVxyXG4gICAgZWxzZSB7XHJcbiAgICAgICAgdGhyb3cgbmV3IEVycm9yKGBUaGUgdXJpICR7YWJzb2x1dGVVcml9IGlzIG5vdCBpbiB0aGUgcm91dGluZyBzbGlwIFske2tlcm5lbENvbW1hbmRFbnZlbG9wZS5yb3V0aW5nU2xpcH1dYCk7XHJcbiAgICB9XHJcbn1cclxuXHJcbmV4cG9ydCBmdW5jdGlvbiBzdGFtcEV2ZW50Um91dGluZ1NsaXAoa2VybmVsRXZlbnRFbnZlbG9wZTogY29udHJhY3RzLktlcm5lbEV2ZW50RW52ZWxvcGUsIGtlcm5lbFVyaTogc3RyaW5nKSB7XHJcbiAgICBzdGFtcFJvdXRpbmdTbGlwKGtlcm5lbEV2ZW50RW52ZWxvcGUsIGtlcm5lbFVyaSk7XHJcbn1cclxuXHJcbmZ1bmN0aW9uIHN0YW1wQ29tbWFuZFJvdXRpbmdTbGlwQXMoa2VybmVsQ29tbWFuZE9yRXZlbnRFbnZlbG9wZTogS2VybmVsQ29tbWFuZE9yRXZlbnRFbnZlbG9wZSwga2VybmVsVXJpOiBzdHJpbmcsIHRhZzogc3RyaW5nKSB7XHJcbiAgICBjb25zdCBhYnNvbHV0ZVVyaSA9IGAke2NyZWF0ZUtlcm5lbFVyaShrZXJuZWxVcmkpfT90YWc9JHt0YWd9YDsvLz9cclxuICAgIHN0YW1wUm91dGluZ1NsaXAoa2VybmVsQ29tbWFuZE9yRXZlbnRFbnZlbG9wZSwgYWJzb2x1dGVVcmkpO1xyXG59XHJcblxyXG5cclxuZnVuY3Rpb24gc3RhbXBSb3V0aW5nU2xpcChrZXJuZWxDb21tYW5kT3JFdmVudEVudmVsb3BlOiBLZXJuZWxDb21tYW5kT3JFdmVudEVudmVsb3BlLCBrZXJuZWxVcmk6IHN0cmluZykge1xyXG4gICAgaWYgKGtlcm5lbENvbW1hbmRPckV2ZW50RW52ZWxvcGUucm91dGluZ1NsaXAgPT09IHVuZGVmaW5lZCB8fCBrZXJuZWxDb21tYW5kT3JFdmVudEVudmVsb3BlLnJvdXRpbmdTbGlwID09PSBudWxsKSB7XHJcbiAgICAgICAga2VybmVsQ29tbWFuZE9yRXZlbnRFbnZlbG9wZS5yb3V0aW5nU2xpcCA9IFtdO1xyXG4gICAgfVxyXG4gICAgY29uc3Qgbm9ybWFsaXplZFVyaSA9IGNyZWF0ZUtlcm5lbFVyaVdpdGhRdWVyeShrZXJuZWxVcmkpO1xyXG4gICAgY29uc3QgY2FuQWRkID0gIWtlcm5lbENvbW1hbmRPckV2ZW50RW52ZWxvcGUucm91dGluZ1NsaXAuZmluZChlID0+IGNyZWF0ZUtlcm5lbFVyaVdpdGhRdWVyeShlKSA9PT0gbm9ybWFsaXplZFVyaSk7XHJcbiAgICBpZiAoY2FuQWRkKSB7XHJcbiAgICAgICAga2VybmVsQ29tbWFuZE9yRXZlbnRFbnZlbG9wZS5yb3V0aW5nU2xpcC5wdXNoKG5vcm1hbGl6ZWRVcmkpO1xyXG4gICAgICAgIGtlcm5lbENvbW1hbmRPckV2ZW50RW52ZWxvcGUucm91dGluZ1NsaXA7Ly8/XHJcbiAgICB9IGVsc2Uge1xyXG4gICAgICAgIHRocm93IG5ldyBFcnJvcihgVGhlIHVyaSAke25vcm1hbGl6ZWRVcml9IGlzIGFscmVhZHkgaW4gdGhlIHJvdXRpbmcgc2xpcCBbJHtrZXJuZWxDb21tYW5kT3JFdmVudEVudmVsb3BlLnJvdXRpbmdTbGlwfV1gKTtcclxuICAgIH1cclxufVxyXG5cclxuZnVuY3Rpb24gY29udGludWVSb3V0aW5nU2xpcChrZXJuZWxDb21tYW5kT3JFdmVudEVudmVsb3BlOiBLZXJuZWxDb21tYW5kT3JFdmVudEVudmVsb3BlLCBrZXJuZWxVcmlzOiBzdHJpbmdbXSk6IHZvaWQge1xyXG4gICAgaWYgKGtlcm5lbENvbW1hbmRPckV2ZW50RW52ZWxvcGUucm91dGluZ1NsaXAgPT09IHVuZGVmaW5lZCB8fCBrZXJuZWxDb21tYW5kT3JFdmVudEVudmVsb3BlLnJvdXRpbmdTbGlwID09PSBudWxsKSB7XHJcbiAgICAgICAga2VybmVsQ29tbWFuZE9yRXZlbnRFbnZlbG9wZS5yb3V0aW5nU2xpcCA9IFtdO1xyXG4gICAgfVxyXG5cclxuICAgIGxldCB0b0NvbnRpbnVlID0gY3JlYXRlUm91dGluZ1NsaXAoa2VybmVsVXJpcyk7XHJcblxyXG4gICAgaWYgKHJvdXRpbmdTbGlwU3RhcnRzV2l0aCh0b0NvbnRpbnVlLCBrZXJuZWxDb21tYW5kT3JFdmVudEVudmVsb3BlLnJvdXRpbmdTbGlwKSkge1xyXG4gICAgICAgIHRvQ29udGludWUgPSB0b0NvbnRpbnVlLnNsaWNlKGtlcm5lbENvbW1hbmRPckV2ZW50RW52ZWxvcGUucm91dGluZ1NsaXAubGVuZ3RoKTtcclxuICAgIH1cclxuXHJcbiAgICBjb25zdCBvcmlnaW5hbCA9IFsuLi5rZXJuZWxDb21tYW5kT3JFdmVudEVudmVsb3BlLnJvdXRpbmdTbGlwXTtcclxuICAgIGZvciAobGV0IGkgPSAwOyBpIDwgdG9Db250aW51ZS5sZW5ndGg7IGkrKykge1xyXG4gICAgICAgIGNvbnN0IG5vcm1hbGl6ZWRVcmkgPSB0b0NvbnRpbnVlW2ldOy8vP1xyXG4gICAgICAgIGNvbnN0IGNhbkFkZCA9ICFrZXJuZWxDb21tYW5kT3JFdmVudEVudmVsb3BlLnJvdXRpbmdTbGlwLmZpbmQoZSA9PiBjcmVhdGVLZXJuZWxVcmkoZSkgPT09IG5vcm1hbGl6ZWRVcmkpO1xyXG4gICAgICAgIGlmIChjYW5BZGQpIHtcclxuICAgICAgICAgICAga2VybmVsQ29tbWFuZE9yRXZlbnRFbnZlbG9wZS5yb3V0aW5nU2xpcC5wdXNoKG5vcm1hbGl6ZWRVcmkpO1xyXG4gICAgICAgIH0gZWxzZSB7XHJcbiAgICAgICAgICAgIHRocm93IG5ldyBFcnJvcihgVGhlIHVyaSAke25vcm1hbGl6ZWRVcml9IGlzIGFscmVhZHkgaW4gdGhlIHJvdXRpbmcgc2xpcCBbJHtvcmlnaW5hbH1dLCBjYW5ub3QgY29udGludWUgd2l0aCByb3V0aW5nIHNsaXAgWyR7a2VybmVsVXJpcy5tYXAoZSA9PiBjcmVhdGVLZXJuZWxVcmkoZSkpfV1gKTtcclxuICAgICAgICB9XHJcbiAgICB9XHJcbn1cclxuXHJcbmV4cG9ydCBmdW5jdGlvbiBjb250aW51ZUNvbW1hbmRSb3V0aW5nU2xpcChrZXJuZWxDb21tYW5kRW52ZWxvcGU6IGNvbnRyYWN0cy5LZXJuZWxDb21tYW5kRW52ZWxvcGUsIGtlcm5lbFVyaXM6IHN0cmluZ1tdKTogdm9pZCB7XHJcbiAgICBjb250aW51ZVJvdXRpbmdTbGlwKGtlcm5lbENvbW1hbmRFbnZlbG9wZSwga2VybmVsVXJpcyk7XHJcbn1cclxuXHJcbmV4cG9ydCBmdW5jdGlvbiBjb250aW51ZUV2ZW50Um91dGluZ1NsaXAoa2VybmVsRXZlbnRFbnZlbG9wZTogY29udHJhY3RzLktlcm5lbEV2ZW50RW52ZWxvcGUsIGtlcm5lbFVyaXM6IHN0cmluZ1tdKTogdm9pZCB7XHJcbiAgICBjb250aW51ZVJvdXRpbmdTbGlwKGtlcm5lbEV2ZW50RW52ZWxvcGUsIGtlcm5lbFVyaXMpO1xyXG59XHJcblxyXG5leHBvcnQgZnVuY3Rpb24gY3JlYXRlUm91dGluZ1NsaXAoa2VybmVsVXJpczogc3RyaW5nW10pOiBzdHJpbmdbXSB7XHJcbiAgICByZXR1cm4gQXJyYXkuZnJvbShuZXcgU2V0KGtlcm5lbFVyaXMubWFwKGUgPT4gY3JlYXRlS2VybmVsVXJpKGUpKSkpO1xyXG59XHJcblxyXG5leHBvcnQgZnVuY3Rpb24gZXZlbnRSb3V0aW5nU2xpcFN0YXJ0c1dpdGgodGhpc0V2ZW50OiBjb250cmFjdHMuS2VybmVsRXZlbnRFbnZlbG9wZSwgb3RoZXI6IHN0cmluZ1tdIHwgY29udHJhY3RzLktlcm5lbEV2ZW50RW52ZWxvcGUpOiBib29sZWFuIHtcclxuICAgIGNvbnN0IHRoaXNLZXJuZWxVcmlzID0gdGhpc0V2ZW50LnJvdXRpbmdTbGlwID8/IFtdO1xyXG4gICAgY29uc3Qgb3RoZXJLZXJuZWxVcmlzID0gKG90aGVyIGluc3RhbmNlb2YgQXJyYXkgPyBvdGhlciA6IG90aGVyPy5yb3V0aW5nU2xpcCkgPz8gW107XHJcblxyXG4gICAgcmV0dXJuIHJvdXRpbmdTbGlwU3RhcnRzV2l0aCh0aGlzS2VybmVsVXJpcywgb3RoZXJLZXJuZWxVcmlzKTtcclxufVxyXG5cclxuZXhwb3J0IGZ1bmN0aW9uIGNvbW1hbmRSb3V0aW5nU2xpcFN0YXJ0c1dpdGgodGhpc0NvbW1hbmQ6IGNvbnRyYWN0cy5LZXJuZWxDb21tYW5kRW52ZWxvcGUsIG90aGVyOiBzdHJpbmdbXSB8IGNvbnRyYWN0cy5LZXJuZWxDb21tYW5kRW52ZWxvcGUpOiBib29sZWFuIHtcclxuICAgIGNvbnN0IHRoaXNLZXJuZWxVcmlzID0gdGhpc0NvbW1hbmQucm91dGluZ1NsaXAgPz8gW107XHJcbiAgICBjb25zdCBvdGhlcktlcm5lbFVyaXMgPSAob3RoZXIgaW5zdGFuY2VvZiBBcnJheSA/IG90aGVyIDogb3RoZXI/LnJvdXRpbmdTbGlwKSA/PyBbXTtcclxuXHJcbiAgICByZXR1cm4gcm91dGluZ1NsaXBTdGFydHNXaXRoKHRoaXNLZXJuZWxVcmlzLCBvdGhlcktlcm5lbFVyaXMpO1xyXG59XHJcblxyXG5mdW5jdGlvbiByb3V0aW5nU2xpcFN0YXJ0c1dpdGgodGhpc0tlcm5lbFVyaXM6IHN0cmluZ1tdLCBvdGhlcktlcm5lbFVyaXM6IHN0cmluZ1tdKTogYm9vbGVhbiB7XHJcbiAgICBsZXQgc3RhcnRzV2l0aCA9IHRydWU7XHJcblxyXG4gICAgaWYgKG90aGVyS2VybmVsVXJpcy5sZW5ndGggPiAwICYmIHRoaXNLZXJuZWxVcmlzLmxlbmd0aCA+PSBvdGhlcktlcm5lbFVyaXMubGVuZ3RoKSB7XHJcbiAgICAgICAgZm9yIChsZXQgaSA9IDA7IGkgPCBvdGhlcktlcm5lbFVyaXMubGVuZ3RoOyBpKyspIHtcclxuICAgICAgICAgICAgaWYgKGNyZWF0ZUtlcm5lbFVyaShvdGhlcktlcm5lbFVyaXNbaV0pICE9PSBjcmVhdGVLZXJuZWxVcmkodGhpc0tlcm5lbFVyaXNbaV0pKSB7XHJcbiAgICAgICAgICAgICAgICBzdGFydHNXaXRoID0gZmFsc2U7XHJcbiAgICAgICAgICAgICAgICBicmVhaztcclxuICAgICAgICAgICAgfVxyXG4gICAgICAgIH1cclxuICAgIH1cclxuICAgIGVsc2Uge1xyXG4gICAgICAgIHN0YXJ0c1dpdGggPSBmYWxzZTtcclxuICAgIH1cclxuXHJcbiAgICByZXR1cm4gc3RhcnRzV2l0aDtcclxufVxyXG5cclxuZXhwb3J0IGZ1bmN0aW9uIGV2ZW50Um91dGluZ1NsaXBDb250YWlucyhrZXJubEV2ZW50OiBjb250cmFjdHMuS2VybmVsRXZlbnRFbnZlbG9wZSwga2VybmVsVXJpOiBzdHJpbmcsIGlnbm9yZVF1ZXJ5OiBib29sZWFuID0gZmFsc2UpOiBib29sZWFuIHtcclxuICAgIHJldHVybiByb3V0aW5nU2xpcENvbnRhaW5zKGtlcm5sRXZlbnQsIGtlcm5lbFVyaSwgaWdub3JlUXVlcnkpO1xyXG59XHJcblxyXG5leHBvcnQgZnVuY3Rpb24gY29tbWFuZFJvdXRpbmdTbGlwQ29udGFpbnMoa2VybmxFdmVudDogY29udHJhY3RzLktlcm5lbENvbW1hbmRFbnZlbG9wZSwga2VybmVsVXJpOiBzdHJpbmcsIGlnbm9yZVF1ZXJ5OiBib29sZWFuID0gZmFsc2UpOiBib29sZWFuIHtcclxuICAgIHJldHVybiByb3V0aW5nU2xpcENvbnRhaW5zKGtlcm5sRXZlbnQsIGtlcm5lbFVyaSwgaWdub3JlUXVlcnkpO1xyXG59XHJcblxyXG5mdW5jdGlvbiByb3V0aW5nU2xpcENvbnRhaW5zKGtlcm5lbENvbW1hbmRPckV2ZW50RW52ZWxvcGU6IEtlcm5lbENvbW1hbmRPckV2ZW50RW52ZWxvcGUsIGtlcm5lbFVyaTogc3RyaW5nLCBpZ25vcmVRdWVyeTogYm9vbGVhbiA9IGZhbHNlKTogYm9vbGVhbiB7XHJcbiAgICBjb25zdCBub3JtYWxpemVkVXJpID0gaWdub3JlUXVlcnkgPyBjcmVhdGVLZXJuZWxVcmkoa2VybmVsVXJpKSA6IGNyZWF0ZUtlcm5lbFVyaVdpdGhRdWVyeShrZXJuZWxVcmkpO1xyXG4gICAgcmV0dXJuIGtlcm5lbENvbW1hbmRPckV2ZW50RW52ZWxvcGU/LnJvdXRpbmdTbGlwPy5maW5kKGUgPT4gbm9ybWFsaXplZFVyaSA9PT0gKCFpZ25vcmVRdWVyeSA/IGNyZWF0ZUtlcm5lbFVyaVdpdGhRdWVyeShlKSA6IGNyZWF0ZUtlcm5lbFVyaShlKSkpICE9PSB1bmRlZmluZWQ7XHJcbn0iLCIvLyBDb3B5cmlnaHQgKGMpIC5ORVQgRm91bmRhdGlvbiBhbmQgY29udHJpYnV0b3JzLiBBbGwgcmlnaHRzIHJlc2VydmVkLlxuLy8gTGljZW5zZWQgdW5kZXIgdGhlIE1JVCBsaWNlbnNlLiBTZWUgTElDRU5TRSBmaWxlIGluIHRoZSBwcm9qZWN0IHJvb3QgZm9yIGZ1bGwgbGljZW5zZSBpbmZvcm1hdGlvbi5cblxuLy8gR2VuZXJhdGVkIFR5cGVTY3JpcHQgaW50ZXJmYWNlcyBhbmQgdHlwZXMuXHJcblxyXG4vLyAtLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0gS2VybmVsIENvbW1hbmRzXHJcblxyXG5leHBvcnQgY29uc3QgQ2FuY2VsVHlwZSA9IFwiQ2FuY2VsXCI7XHJcbmV4cG9ydCBjb25zdCBDaGFuZ2VXb3JraW5nRGlyZWN0b3J5VHlwZSA9IFwiQ2hhbmdlV29ya2luZ0RpcmVjdG9yeVwiO1xyXG5leHBvcnQgY29uc3QgQ29tcGlsZVByb2plY3RUeXBlID0gXCJDb21waWxlUHJvamVjdFwiO1xyXG5leHBvcnQgY29uc3QgRGlzcGxheUVycm9yVHlwZSA9IFwiRGlzcGxheUVycm9yXCI7XHJcbmV4cG9ydCBjb25zdCBEaXNwbGF5VmFsdWVUeXBlID0gXCJEaXNwbGF5VmFsdWVcIjtcclxuZXhwb3J0IGNvbnN0IE9wZW5Eb2N1bWVudFR5cGUgPSBcIk9wZW5Eb2N1bWVudFwiO1xyXG5leHBvcnQgY29uc3QgT3BlblByb2plY3RUeXBlID0gXCJPcGVuUHJvamVjdFwiO1xyXG5leHBvcnQgY29uc3QgUXVpdFR5cGUgPSBcIlF1aXRcIjtcclxuZXhwb3J0IGNvbnN0IFJlcXVlc3RDb21wbGV0aW9uc1R5cGUgPSBcIlJlcXVlc3RDb21wbGV0aW9uc1wiO1xyXG5leHBvcnQgY29uc3QgUmVxdWVzdERpYWdub3N0aWNzVHlwZSA9IFwiUmVxdWVzdERpYWdub3N0aWNzXCI7XHJcbmV4cG9ydCBjb25zdCBSZXF1ZXN0SG92ZXJUZXh0VHlwZSA9IFwiUmVxdWVzdEhvdmVyVGV4dFwiO1xyXG5leHBvcnQgY29uc3QgUmVxdWVzdElucHV0VHlwZSA9IFwiUmVxdWVzdElucHV0XCI7XHJcbmV4cG9ydCBjb25zdCBSZXF1ZXN0S2VybmVsSW5mb1R5cGUgPSBcIlJlcXVlc3RLZXJuZWxJbmZvXCI7XHJcbmV4cG9ydCBjb25zdCBSZXF1ZXN0U2lnbmF0dXJlSGVscFR5cGUgPSBcIlJlcXVlc3RTaWduYXR1cmVIZWxwXCI7XHJcbmV4cG9ydCBjb25zdCBSZXF1ZXN0VmFsdWVUeXBlID0gXCJSZXF1ZXN0VmFsdWVcIjtcclxuZXhwb3J0IGNvbnN0IFJlcXVlc3RWYWx1ZUluZm9zVHlwZSA9IFwiUmVxdWVzdFZhbHVlSW5mb3NcIjtcclxuZXhwb3J0IGNvbnN0IFNlbmRFZGl0YWJsZUNvZGVUeXBlID0gXCJTZW5kRWRpdGFibGVDb2RlXCI7XHJcbmV4cG9ydCBjb25zdCBTZW5kVmFsdWVUeXBlID0gXCJTZW5kVmFsdWVcIjtcclxuZXhwb3J0IGNvbnN0IFN1Ym1pdENvZGVUeXBlID0gXCJTdWJtaXRDb2RlXCI7XHJcbmV4cG9ydCBjb25zdCBVcGRhdGVEaXNwbGF5ZWRWYWx1ZVR5cGUgPSBcIlVwZGF0ZURpc3BsYXllZFZhbHVlXCI7XHJcblxyXG5leHBvcnQgdHlwZSBLZXJuZWxDb21tYW5kVHlwZSA9XHJcbiAgICAgIHR5cGVvZiBDYW5jZWxUeXBlXHJcbiAgICB8IHR5cGVvZiBDaGFuZ2VXb3JraW5nRGlyZWN0b3J5VHlwZVxyXG4gICAgfCB0eXBlb2YgQ29tcGlsZVByb2plY3RUeXBlXHJcbiAgICB8IHR5cGVvZiBEaXNwbGF5RXJyb3JUeXBlXHJcbiAgICB8IHR5cGVvZiBEaXNwbGF5VmFsdWVUeXBlXHJcbiAgICB8IHR5cGVvZiBPcGVuRG9jdW1lbnRUeXBlXHJcbiAgICB8IHR5cGVvZiBPcGVuUHJvamVjdFR5cGVcclxuICAgIHwgdHlwZW9mIFF1aXRUeXBlXHJcbiAgICB8IHR5cGVvZiBSZXF1ZXN0Q29tcGxldGlvbnNUeXBlXHJcbiAgICB8IHR5cGVvZiBSZXF1ZXN0RGlhZ25vc3RpY3NUeXBlXHJcbiAgICB8IHR5cGVvZiBSZXF1ZXN0SG92ZXJUZXh0VHlwZVxyXG4gICAgfCB0eXBlb2YgUmVxdWVzdElucHV0VHlwZVxyXG4gICAgfCB0eXBlb2YgUmVxdWVzdEtlcm5lbEluZm9UeXBlXHJcbiAgICB8IHR5cGVvZiBSZXF1ZXN0U2lnbmF0dXJlSGVscFR5cGVcclxuICAgIHwgdHlwZW9mIFJlcXVlc3RWYWx1ZVR5cGVcclxuICAgIHwgdHlwZW9mIFJlcXVlc3RWYWx1ZUluZm9zVHlwZVxyXG4gICAgfCB0eXBlb2YgU2VuZEVkaXRhYmxlQ29kZVR5cGVcclxuICAgIHwgdHlwZW9mIFNlbmRWYWx1ZVR5cGVcclxuICAgIHwgdHlwZW9mIFN1Ym1pdENvZGVUeXBlXHJcbiAgICB8IHR5cGVvZiBVcGRhdGVEaXNwbGF5ZWRWYWx1ZVR5cGU7XHJcblxyXG5leHBvcnQgaW50ZXJmYWNlIENhbmNlbCBleHRlbmRzIEtlcm5lbENvbW1hbmQge1xyXG59XHJcblxyXG5leHBvcnQgaW50ZXJmYWNlIEtlcm5lbENvbW1hbmQge1xyXG4gICAgdGFyZ2V0S2VybmVsTmFtZT86IHN0cmluZztcclxuICAgIG9yaWdpblVyaT86IHN0cmluZztcclxuICAgIGRlc3RpbmF0aW9uVXJpPzogc3RyaW5nO1xyXG59XHJcblxyXG5leHBvcnQgaW50ZXJmYWNlIENoYW5nZVdvcmtpbmdEaXJlY3RvcnkgZXh0ZW5kcyBLZXJuZWxDb21tYW5kIHtcclxuICAgIHdvcmtpbmdEaXJlY3Rvcnk6IHN0cmluZztcclxufVxyXG5cclxuZXhwb3J0IGludGVyZmFjZSBDb21waWxlUHJvamVjdCBleHRlbmRzIEtlcm5lbENvbW1hbmQge1xyXG59XHJcblxyXG5leHBvcnQgaW50ZXJmYWNlIERpc3BsYXlFcnJvciBleHRlbmRzIEtlcm5lbENvbW1hbmQge1xyXG4gICAgbWVzc2FnZTogc3RyaW5nO1xyXG59XHJcblxyXG5leHBvcnQgaW50ZXJmYWNlIERpc3BsYXlWYWx1ZSBleHRlbmRzIEtlcm5lbENvbW1hbmQge1xyXG4gICAgZm9ybWF0dGVkVmFsdWU6IEZvcm1hdHRlZFZhbHVlO1xyXG4gICAgdmFsdWVJZDogc3RyaW5nO1xyXG59XHJcblxyXG5leHBvcnQgaW50ZXJmYWNlIE9wZW5Eb2N1bWVudCBleHRlbmRzIEtlcm5lbENvbW1hbmQge1xyXG4gICAgcmVsYXRpdmVGaWxlUGF0aDogc3RyaW5nO1xyXG4gICAgcmVnaW9uTmFtZT86IHN0cmluZztcclxufVxyXG5cclxuZXhwb3J0IGludGVyZmFjZSBPcGVuUHJvamVjdCBleHRlbmRzIEtlcm5lbENvbW1hbmQge1xyXG4gICAgcHJvamVjdDogUHJvamVjdDtcclxufVxyXG5cclxuZXhwb3J0IGludGVyZmFjZSBRdWl0IGV4dGVuZHMgS2VybmVsQ29tbWFuZCB7XHJcbn1cclxuXHJcbmV4cG9ydCBpbnRlcmZhY2UgUmVxdWVzdENvbXBsZXRpb25zIGV4dGVuZHMgTGFuZ3VhZ2VTZXJ2aWNlQ29tbWFuZCB7XHJcbn1cclxuXHJcbmV4cG9ydCBpbnRlcmZhY2UgTGFuZ3VhZ2VTZXJ2aWNlQ29tbWFuZCBleHRlbmRzIEtlcm5lbENvbW1hbmQge1xyXG4gICAgY29kZTogc3RyaW5nO1xyXG4gICAgbGluZVBvc2l0aW9uOiBMaW5lUG9zaXRpb247XHJcbn1cclxuXHJcbmV4cG9ydCBpbnRlcmZhY2UgUmVxdWVzdERpYWdub3N0aWNzIGV4dGVuZHMgS2VybmVsQ29tbWFuZCB7XHJcbiAgICBjb2RlOiBzdHJpbmc7XHJcbn1cclxuXHJcbmV4cG9ydCBpbnRlcmZhY2UgUmVxdWVzdEhvdmVyVGV4dCBleHRlbmRzIExhbmd1YWdlU2VydmljZUNvbW1hbmQge1xyXG59XHJcblxyXG5leHBvcnQgaW50ZXJmYWNlIFJlcXVlc3RJbnB1dCBleHRlbmRzIEtlcm5lbENvbW1hbmQge1xyXG4gICAgcHJvbXB0OiBzdHJpbmc7XHJcbiAgICBpc1Bhc3N3b3JkOiBib29sZWFuO1xyXG4gICAgaW5wdXRUeXBlSGludDogc3RyaW5nO1xyXG4gICAgdmFsdWVOYW1lOiBzdHJpbmc7XHJcbn1cclxuXHJcbmV4cG9ydCBpbnRlcmZhY2UgUmVxdWVzdEtlcm5lbEluZm8gZXh0ZW5kcyBLZXJuZWxDb21tYW5kIHtcclxufVxyXG5cclxuZXhwb3J0IGludGVyZmFjZSBSZXF1ZXN0U2lnbmF0dXJlSGVscCBleHRlbmRzIExhbmd1YWdlU2VydmljZUNvbW1hbmQge1xyXG59XHJcblxyXG5leHBvcnQgaW50ZXJmYWNlIFJlcXVlc3RWYWx1ZSBleHRlbmRzIEtlcm5lbENvbW1hbmQge1xyXG4gICAgbmFtZTogc3RyaW5nO1xyXG4gICAgbWltZVR5cGU6IHN0cmluZztcclxufVxyXG5cclxuZXhwb3J0IGludGVyZmFjZSBSZXF1ZXN0VmFsdWVJbmZvcyBleHRlbmRzIEtlcm5lbENvbW1hbmQge1xyXG59XHJcblxyXG5leHBvcnQgaW50ZXJmYWNlIFNlbmRFZGl0YWJsZUNvZGUgZXh0ZW5kcyBLZXJuZWxDb21tYW5kIHtcclxuICAgIGtlcm5lbE5hbWU6IHN0cmluZztcclxuICAgIGNvZGU6IHN0cmluZztcclxufVxyXG5cclxuZXhwb3J0IGludGVyZmFjZSBTZW5kVmFsdWUgZXh0ZW5kcyBLZXJuZWxDb21tYW5kIHtcclxuICAgIGZvcm1hdHRlZFZhbHVlOiBGb3JtYXR0ZWRWYWx1ZTtcclxuICAgIG5hbWU6IHN0cmluZztcclxufVxyXG5cclxuZXhwb3J0IGludGVyZmFjZSBTdWJtaXRDb2RlIGV4dGVuZHMgS2VybmVsQ29tbWFuZCB7XHJcbiAgICBjb2RlOiBzdHJpbmc7XHJcbiAgICBzdWJtaXNzaW9uVHlwZT86IFN1Ym1pc3Npb25UeXBlO1xyXG59XHJcblxyXG5leHBvcnQgaW50ZXJmYWNlIFVwZGF0ZURpc3BsYXllZFZhbHVlIGV4dGVuZHMgS2VybmVsQ29tbWFuZCB7XHJcbiAgICBmb3JtYXR0ZWRWYWx1ZTogRm9ybWF0dGVkVmFsdWU7XHJcbiAgICB2YWx1ZUlkOiBzdHJpbmc7XHJcbn1cclxuXHJcbmV4cG9ydCBpbnRlcmZhY2UgS2VybmVsRXZlbnQge1xyXG59XHJcblxyXG5leHBvcnQgaW50ZXJmYWNlIERpc3BsYXlFbGVtZW50IGV4dGVuZHMgSW50ZXJhY3RpdmVEb2N1bWVudE91dHB1dEVsZW1lbnQge1xyXG4gICAgZGF0YTogeyBba2V5OiBzdHJpbmddOiBhbnk7IH07XHJcbiAgICBtZXRhZGF0YTogeyBba2V5OiBzdHJpbmddOiBhbnk7IH07XHJcbn1cclxuXHJcbmV4cG9ydCBpbnRlcmZhY2UgSW50ZXJhY3RpdmVEb2N1bWVudE91dHB1dEVsZW1lbnQge1xyXG59XHJcblxyXG5leHBvcnQgaW50ZXJmYWNlIFJldHVyblZhbHVlRWxlbWVudCBleHRlbmRzIEludGVyYWN0aXZlRG9jdW1lbnRPdXRwdXRFbGVtZW50IHtcclxuICAgIGRhdGE6IHsgW2tleTogc3RyaW5nXTogYW55OyB9O1xyXG4gICAgZXhlY3V0aW9uT3JkZXI6IG51bWJlcjtcclxuICAgIG1ldGFkYXRhOiB7IFtrZXk6IHN0cmluZ106IGFueTsgfTtcclxufVxyXG5cclxuZXhwb3J0IGludGVyZmFjZSBUZXh0RWxlbWVudCBleHRlbmRzIEludGVyYWN0aXZlRG9jdW1lbnRPdXRwdXRFbGVtZW50IHtcclxuICAgIG5hbWU6IHN0cmluZztcclxuICAgIHRleHQ6IHN0cmluZztcclxufVxyXG5cclxuZXhwb3J0IGludGVyZmFjZSBFcnJvckVsZW1lbnQgZXh0ZW5kcyBJbnRlcmFjdGl2ZURvY3VtZW50T3V0cHV0RWxlbWVudCB7XHJcbiAgICBlcnJvck5hbWU6IHN0cmluZztcclxuICAgIGVycm9yVmFsdWU6IHN0cmluZztcclxuICAgIHN0YWNrVHJhY2U6IEFycmF5PHN0cmluZz47XHJcbn1cclxuXHJcbmV4cG9ydCBpbnRlcmZhY2UgRG9jdW1lbnRLZXJuZWxJbmZvIHtcclxuICAgIG5hbWU6IHN0cmluZztcclxuICAgIGxhbmd1YWdlTmFtZT86IHN0cmluZztcclxuICAgIGFsaWFzZXM6IEFycmF5PHN0cmluZz47XHJcbn1cclxuXHJcbmV4cG9ydCBpbnRlcmZhY2UgTm90ZWJvb2tQYXJzZVJlcXVlc3QgZXh0ZW5kcyBOb3RlYm9va1BhcnNlT3JTZXJpYWxpemVSZXF1ZXN0IHtcclxuICAgIHR5cGU6IFJlcXVlc3RUeXBlO1xyXG4gICAgcmF3RGF0YTogVWludDhBcnJheTtcclxufVxyXG5cclxuZXhwb3J0IGludGVyZmFjZSBOb3RlYm9va1BhcnNlT3JTZXJpYWxpemVSZXF1ZXN0IHtcclxuICAgIHR5cGU6IFJlcXVlc3RUeXBlO1xyXG4gICAgaWQ6IHN0cmluZztcclxuICAgIHNlcmlhbGl6YXRpb25UeXBlOiBEb2N1bWVudFNlcmlhbGl6YXRpb25UeXBlO1xyXG4gICAgZGVmYXVsdExhbmd1YWdlOiBzdHJpbmc7XHJcbn1cclxuXHJcbmV4cG9ydCBpbnRlcmZhY2UgTm90ZWJvb2tTZXJpYWxpemVSZXF1ZXN0IGV4dGVuZHMgTm90ZWJvb2tQYXJzZU9yU2VyaWFsaXplUmVxdWVzdCB7XHJcbiAgICB0eXBlOiBSZXF1ZXN0VHlwZTtcclxuICAgIG5ld0xpbmU6IHN0cmluZztcclxuICAgIGRvY3VtZW50OiBJbnRlcmFjdGl2ZURvY3VtZW50O1xyXG59XHJcblxyXG5leHBvcnQgaW50ZXJmYWNlIE5vdGVib29rUGFyc2VSZXNwb25zZSBleHRlbmRzIE5vdGVib29rUGFyc2VyU2VydmVyUmVzcG9uc2Uge1xyXG4gICAgZG9jdW1lbnQ6IEludGVyYWN0aXZlRG9jdW1lbnQ7XHJcbn1cclxuXHJcbmV4cG9ydCBpbnRlcmZhY2UgTm90ZWJvb2tQYXJzZXJTZXJ2ZXJSZXNwb25zZSB7XHJcbiAgICBpZDogc3RyaW5nO1xyXG59XHJcblxyXG5leHBvcnQgaW50ZXJmYWNlIE5vdGVib29rU2VyaWFsaXplUmVzcG9uc2UgZXh0ZW5kcyBOb3RlYm9va1BhcnNlclNlcnZlclJlc3BvbnNlIHtcclxuICAgIHJhd0RhdGE6IFVpbnQ4QXJyYXk7XHJcbn1cclxuXHJcbmV4cG9ydCBpbnRlcmZhY2UgTm90ZWJvb2tFcnJvclJlc3BvbnNlIGV4dGVuZHMgTm90ZWJvb2tQYXJzZXJTZXJ2ZXJSZXNwb25zZSB7XHJcbiAgICBlcnJvck1lc3NhZ2U6IHN0cmluZztcclxufVxyXG5cclxuLy8gLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tIEtlcm5lbCBldmVudHNcclxuXHJcbmV4cG9ydCBjb25zdCBBc3NlbWJseVByb2R1Y2VkVHlwZSA9IFwiQXNzZW1ibHlQcm9kdWNlZFwiO1xyXG5leHBvcnQgY29uc3QgQ29kZVN1Ym1pc3Npb25SZWNlaXZlZFR5cGUgPSBcIkNvZGVTdWJtaXNzaW9uUmVjZWl2ZWRcIjtcclxuZXhwb3J0IGNvbnN0IENvbW1hbmRDYW5jZWxsZWRUeXBlID0gXCJDb21tYW5kQ2FuY2VsbGVkXCI7XHJcbmV4cG9ydCBjb25zdCBDb21tYW5kRmFpbGVkVHlwZSA9IFwiQ29tbWFuZEZhaWxlZFwiO1xyXG5leHBvcnQgY29uc3QgQ29tbWFuZFN1Y2NlZWRlZFR5cGUgPSBcIkNvbW1hbmRTdWNjZWVkZWRcIjtcclxuZXhwb3J0IGNvbnN0IENvbXBsZXRlQ29kZVN1Ym1pc3Npb25SZWNlaXZlZFR5cGUgPSBcIkNvbXBsZXRlQ29kZVN1Ym1pc3Npb25SZWNlaXZlZFwiO1xyXG5leHBvcnQgY29uc3QgQ29tcGxldGlvbnNQcm9kdWNlZFR5cGUgPSBcIkNvbXBsZXRpb25zUHJvZHVjZWRcIjtcclxuZXhwb3J0IGNvbnN0IERpYWdub3N0aWNMb2dFbnRyeVByb2R1Y2VkVHlwZSA9IFwiRGlhZ25vc3RpY0xvZ0VudHJ5UHJvZHVjZWRcIjtcclxuZXhwb3J0IGNvbnN0IERpYWdub3N0aWNzUHJvZHVjZWRUeXBlID0gXCJEaWFnbm9zdGljc1Byb2R1Y2VkXCI7XHJcbmV4cG9ydCBjb25zdCBEaXNwbGF5ZWRWYWx1ZVByb2R1Y2VkVHlwZSA9IFwiRGlzcGxheWVkVmFsdWVQcm9kdWNlZFwiO1xyXG5leHBvcnQgY29uc3QgRGlzcGxheWVkVmFsdWVVcGRhdGVkVHlwZSA9IFwiRGlzcGxheWVkVmFsdWVVcGRhdGVkXCI7XHJcbmV4cG9ydCBjb25zdCBEb2N1bWVudE9wZW5lZFR5cGUgPSBcIkRvY3VtZW50T3BlbmVkXCI7XHJcbmV4cG9ydCBjb25zdCBFcnJvclByb2R1Y2VkVHlwZSA9IFwiRXJyb3JQcm9kdWNlZFwiO1xyXG5leHBvcnQgY29uc3QgSG92ZXJUZXh0UHJvZHVjZWRUeXBlID0gXCJIb3ZlclRleHRQcm9kdWNlZFwiO1xyXG5leHBvcnQgY29uc3QgSW5jb21wbGV0ZUNvZGVTdWJtaXNzaW9uUmVjZWl2ZWRUeXBlID0gXCJJbmNvbXBsZXRlQ29kZVN1Ym1pc3Npb25SZWNlaXZlZFwiO1xyXG5leHBvcnQgY29uc3QgSW5wdXRQcm9kdWNlZFR5cGUgPSBcIklucHV0UHJvZHVjZWRcIjtcclxuZXhwb3J0IGNvbnN0IEtlcm5lbEV4dGVuc2lvbkxvYWRlZFR5cGUgPSBcIktlcm5lbEV4dGVuc2lvbkxvYWRlZFwiO1xyXG5leHBvcnQgY29uc3QgS2VybmVsSW5mb1Byb2R1Y2VkVHlwZSA9IFwiS2VybmVsSW5mb1Byb2R1Y2VkXCI7XHJcbmV4cG9ydCBjb25zdCBLZXJuZWxSZWFkeVR5cGUgPSBcIktlcm5lbFJlYWR5XCI7XHJcbmV4cG9ydCBjb25zdCBQYWNrYWdlQWRkZWRUeXBlID0gXCJQYWNrYWdlQWRkZWRcIjtcclxuZXhwb3J0IGNvbnN0IFByb2plY3RPcGVuZWRUeXBlID0gXCJQcm9qZWN0T3BlbmVkXCI7XHJcbmV4cG9ydCBjb25zdCBSZXR1cm5WYWx1ZVByb2R1Y2VkVHlwZSA9IFwiUmV0dXJuVmFsdWVQcm9kdWNlZFwiO1xyXG5leHBvcnQgY29uc3QgU2lnbmF0dXJlSGVscFByb2R1Y2VkVHlwZSA9IFwiU2lnbmF0dXJlSGVscFByb2R1Y2VkXCI7XHJcbmV4cG9ydCBjb25zdCBTdGFuZGFyZEVycm9yVmFsdWVQcm9kdWNlZFR5cGUgPSBcIlN0YW5kYXJkRXJyb3JWYWx1ZVByb2R1Y2VkXCI7XHJcbmV4cG9ydCBjb25zdCBTdGFuZGFyZE91dHB1dFZhbHVlUHJvZHVjZWRUeXBlID0gXCJTdGFuZGFyZE91dHB1dFZhbHVlUHJvZHVjZWRcIjtcclxuZXhwb3J0IGNvbnN0IFZhbHVlSW5mb3NQcm9kdWNlZFR5cGUgPSBcIlZhbHVlSW5mb3NQcm9kdWNlZFwiO1xyXG5leHBvcnQgY29uc3QgVmFsdWVQcm9kdWNlZFR5cGUgPSBcIlZhbHVlUHJvZHVjZWRcIjtcclxuZXhwb3J0IGNvbnN0IFdvcmtpbmdEaXJlY3RvcnlDaGFuZ2VkVHlwZSA9IFwiV29ya2luZ0RpcmVjdG9yeUNoYW5nZWRcIjtcclxuXHJcbmV4cG9ydCB0eXBlIEtlcm5lbEV2ZW50VHlwZSA9XHJcbiAgICAgIHR5cGVvZiBBc3NlbWJseVByb2R1Y2VkVHlwZVxyXG4gICAgfCB0eXBlb2YgQ29kZVN1Ym1pc3Npb25SZWNlaXZlZFR5cGVcclxuICAgIHwgdHlwZW9mIENvbW1hbmRDYW5jZWxsZWRUeXBlXHJcbiAgICB8IHR5cGVvZiBDb21tYW5kRmFpbGVkVHlwZVxyXG4gICAgfCB0eXBlb2YgQ29tbWFuZFN1Y2NlZWRlZFR5cGVcclxuICAgIHwgdHlwZW9mIENvbXBsZXRlQ29kZVN1Ym1pc3Npb25SZWNlaXZlZFR5cGVcclxuICAgIHwgdHlwZW9mIENvbXBsZXRpb25zUHJvZHVjZWRUeXBlXHJcbiAgICB8IHR5cGVvZiBEaWFnbm9zdGljTG9nRW50cnlQcm9kdWNlZFR5cGVcclxuICAgIHwgdHlwZW9mIERpYWdub3N0aWNzUHJvZHVjZWRUeXBlXHJcbiAgICB8IHR5cGVvZiBEaXNwbGF5ZWRWYWx1ZVByb2R1Y2VkVHlwZVxyXG4gICAgfCB0eXBlb2YgRGlzcGxheWVkVmFsdWVVcGRhdGVkVHlwZVxyXG4gICAgfCB0eXBlb2YgRG9jdW1lbnRPcGVuZWRUeXBlXHJcbiAgICB8IHR5cGVvZiBFcnJvclByb2R1Y2VkVHlwZVxyXG4gICAgfCB0eXBlb2YgSG92ZXJUZXh0UHJvZHVjZWRUeXBlXHJcbiAgICB8IHR5cGVvZiBJbmNvbXBsZXRlQ29kZVN1Ym1pc3Npb25SZWNlaXZlZFR5cGVcclxuICAgIHwgdHlwZW9mIElucHV0UHJvZHVjZWRUeXBlXHJcbiAgICB8IHR5cGVvZiBLZXJuZWxFeHRlbnNpb25Mb2FkZWRUeXBlXHJcbiAgICB8IHR5cGVvZiBLZXJuZWxJbmZvUHJvZHVjZWRUeXBlXHJcbiAgICB8IHR5cGVvZiBLZXJuZWxSZWFkeVR5cGVcclxuICAgIHwgdHlwZW9mIFBhY2thZ2VBZGRlZFR5cGVcclxuICAgIHwgdHlwZW9mIFByb2plY3RPcGVuZWRUeXBlXHJcbiAgICB8IHR5cGVvZiBSZXR1cm5WYWx1ZVByb2R1Y2VkVHlwZVxyXG4gICAgfCB0eXBlb2YgU2lnbmF0dXJlSGVscFByb2R1Y2VkVHlwZVxyXG4gICAgfCB0eXBlb2YgU3RhbmRhcmRFcnJvclZhbHVlUHJvZHVjZWRUeXBlXHJcbiAgICB8IHR5cGVvZiBTdGFuZGFyZE91dHB1dFZhbHVlUHJvZHVjZWRUeXBlXHJcbiAgICB8IHR5cGVvZiBWYWx1ZUluZm9zUHJvZHVjZWRUeXBlXHJcbiAgICB8IHR5cGVvZiBWYWx1ZVByb2R1Y2VkVHlwZVxyXG4gICAgfCB0eXBlb2YgV29ya2luZ0RpcmVjdG9yeUNoYW5nZWRUeXBlO1xyXG5cclxuZXhwb3J0IGludGVyZmFjZSBBc3NlbWJseVByb2R1Y2VkIGV4dGVuZHMgS2VybmVsRXZlbnQge1xyXG4gICAgYXNzZW1ibHk6IEJhc2U2NEVuY29kZWRBc3NlbWJseTtcclxufVxyXG5cclxuZXhwb3J0IGludGVyZmFjZSBDb2RlU3VibWlzc2lvblJlY2VpdmVkIGV4dGVuZHMgS2VybmVsRXZlbnQge1xyXG4gICAgY29kZTogc3RyaW5nO1xyXG59XHJcblxyXG5leHBvcnQgaW50ZXJmYWNlIENvbW1hbmRDYW5jZWxsZWQgZXh0ZW5kcyBLZXJuZWxFdmVudCB7XHJcbn1cclxuXHJcbmV4cG9ydCBpbnRlcmZhY2UgQ29tbWFuZEZhaWxlZCBleHRlbmRzIEtlcm5lbENvbW1hbmRDb21wbGV0aW9uRXZlbnQge1xyXG4gICAgbWVzc2FnZTogc3RyaW5nO1xyXG59XHJcblxyXG5leHBvcnQgaW50ZXJmYWNlIEtlcm5lbENvbW1hbmRDb21wbGV0aW9uRXZlbnQgZXh0ZW5kcyBLZXJuZWxFdmVudCB7XHJcbiAgICBleGVjdXRpb25PcmRlcj86IG51bWJlcjtcclxufVxyXG5cclxuZXhwb3J0IGludGVyZmFjZSBDb21tYW5kU3VjY2VlZGVkIGV4dGVuZHMgS2VybmVsQ29tbWFuZENvbXBsZXRpb25FdmVudCB7XHJcbn1cclxuXHJcbmV4cG9ydCBpbnRlcmZhY2UgQ29tcGxldGVDb2RlU3VibWlzc2lvblJlY2VpdmVkIGV4dGVuZHMgS2VybmVsRXZlbnQge1xyXG4gICAgY29kZTogc3RyaW5nO1xyXG59XHJcblxyXG5leHBvcnQgaW50ZXJmYWNlIENvbXBsZXRpb25zUHJvZHVjZWQgZXh0ZW5kcyBLZXJuZWxFdmVudCB7XHJcbiAgICBsaW5lUG9zaXRpb25TcGFuPzogTGluZVBvc2l0aW9uU3BhbjtcclxuICAgIGNvbXBsZXRpb25zOiBBcnJheTxDb21wbGV0aW9uSXRlbT47XHJcbn1cclxuXHJcbmV4cG9ydCBpbnRlcmZhY2UgRGlhZ25vc3RpY0xvZ0VudHJ5UHJvZHVjZWQgZXh0ZW5kcyBEaWFnbm9zdGljRXZlbnQge1xyXG4gICAgbWVzc2FnZTogc3RyaW5nO1xyXG59XHJcblxyXG5leHBvcnQgaW50ZXJmYWNlIERpYWdub3N0aWNFdmVudCBleHRlbmRzIEtlcm5lbEV2ZW50IHtcclxufVxyXG5cclxuZXhwb3J0IGludGVyZmFjZSBEaWFnbm9zdGljc1Byb2R1Y2VkIGV4dGVuZHMgS2VybmVsRXZlbnQge1xyXG4gICAgZGlhZ25vc3RpY3M6IEFycmF5PERpYWdub3N0aWM+O1xyXG4gICAgZm9ybWF0dGVkRGlhZ25vc3RpY3M6IEFycmF5PEZvcm1hdHRlZFZhbHVlPjtcclxufVxyXG5cclxuZXhwb3J0IGludGVyZmFjZSBEaXNwbGF5ZWRWYWx1ZVByb2R1Y2VkIGV4dGVuZHMgRGlzcGxheUV2ZW50IHtcclxufVxyXG5cclxuZXhwb3J0IGludGVyZmFjZSBEaXNwbGF5RXZlbnQgZXh0ZW5kcyBLZXJuZWxFdmVudCB7XHJcbiAgICBmb3JtYXR0ZWRWYWx1ZXM6IEFycmF5PEZvcm1hdHRlZFZhbHVlPjtcclxuICAgIHZhbHVlSWQ/OiBzdHJpbmc7XHJcbn1cclxuXHJcbmV4cG9ydCBpbnRlcmZhY2UgRGlzcGxheWVkVmFsdWVVcGRhdGVkIGV4dGVuZHMgRGlzcGxheUV2ZW50IHtcclxufVxyXG5cclxuZXhwb3J0IGludGVyZmFjZSBEb2N1bWVudE9wZW5lZCBleHRlbmRzIEtlcm5lbEV2ZW50IHtcclxuICAgIHJlbGF0aXZlRmlsZVBhdGg6IHN0cmluZztcclxuICAgIHJlZ2lvbk5hbWU/OiBzdHJpbmc7XHJcbiAgICBjb250ZW50OiBzdHJpbmc7XHJcbn1cclxuXHJcbmV4cG9ydCBpbnRlcmZhY2UgRXJyb3JQcm9kdWNlZCBleHRlbmRzIERpc3BsYXlFdmVudCB7XHJcbiAgICBtZXNzYWdlOiBzdHJpbmc7XHJcbn1cclxuXHJcbmV4cG9ydCBpbnRlcmZhY2UgSG92ZXJUZXh0UHJvZHVjZWQgZXh0ZW5kcyBLZXJuZWxFdmVudCB7XHJcbiAgICBjb250ZW50OiBBcnJheTxGb3JtYXR0ZWRWYWx1ZT47XHJcbiAgICBsaW5lUG9zaXRpb25TcGFuPzogTGluZVBvc2l0aW9uU3BhbjtcclxufVxyXG5cclxuZXhwb3J0IGludGVyZmFjZSBJbmNvbXBsZXRlQ29kZVN1Ym1pc3Npb25SZWNlaXZlZCBleHRlbmRzIEtlcm5lbEV2ZW50IHtcclxufVxyXG5cclxuZXhwb3J0IGludGVyZmFjZSBJbnB1dFByb2R1Y2VkIGV4dGVuZHMgS2VybmVsRXZlbnQge1xyXG4gICAgdmFsdWU6IHN0cmluZztcclxufVxyXG5cclxuZXhwb3J0IGludGVyZmFjZSBLZXJuZWxFeHRlbnNpb25Mb2FkZWQgZXh0ZW5kcyBLZXJuZWxFdmVudCB7XHJcbn1cclxuXHJcbmV4cG9ydCBpbnRlcmZhY2UgS2VybmVsSW5mb1Byb2R1Y2VkIGV4dGVuZHMgS2VybmVsRXZlbnQge1xyXG4gICAga2VybmVsSW5mbzogS2VybmVsSW5mbztcclxufVxyXG5cclxuZXhwb3J0IGludGVyZmFjZSBLZXJuZWxSZWFkeSBleHRlbmRzIEtlcm5lbEV2ZW50IHtcclxufVxyXG5cclxuZXhwb3J0IGludGVyZmFjZSBQYWNrYWdlQWRkZWQgZXh0ZW5kcyBLZXJuZWxFdmVudCB7XHJcbiAgICBwYWNrYWdlUmVmZXJlbmNlOiBSZXNvbHZlZFBhY2thZ2VSZWZlcmVuY2U7XHJcbn1cclxuXHJcbmV4cG9ydCBpbnRlcmZhY2UgUHJvamVjdE9wZW5lZCBleHRlbmRzIEtlcm5lbEV2ZW50IHtcclxuICAgIHByb2plY3RJdGVtczogQXJyYXk8UHJvamVjdEl0ZW0+O1xyXG59XHJcblxyXG5leHBvcnQgaW50ZXJmYWNlIFJldHVyblZhbHVlUHJvZHVjZWQgZXh0ZW5kcyBEaXNwbGF5RXZlbnQge1xyXG59XHJcblxyXG5leHBvcnQgaW50ZXJmYWNlIFNpZ25hdHVyZUhlbHBQcm9kdWNlZCBleHRlbmRzIEtlcm5lbEV2ZW50IHtcclxuICAgIHNpZ25hdHVyZXM6IEFycmF5PFNpZ25hdHVyZUluZm9ybWF0aW9uPjtcclxuICAgIGFjdGl2ZVNpZ25hdHVyZUluZGV4OiBudW1iZXI7XHJcbiAgICBhY3RpdmVQYXJhbWV0ZXJJbmRleDogbnVtYmVyO1xyXG59XHJcblxyXG5leHBvcnQgaW50ZXJmYWNlIFN0YW5kYXJkRXJyb3JWYWx1ZVByb2R1Y2VkIGV4dGVuZHMgRGlzcGxheUV2ZW50IHtcclxufVxyXG5cclxuZXhwb3J0IGludGVyZmFjZSBTdGFuZGFyZE91dHB1dFZhbHVlUHJvZHVjZWQgZXh0ZW5kcyBEaXNwbGF5RXZlbnQge1xyXG59XHJcblxyXG5leHBvcnQgaW50ZXJmYWNlIFZhbHVlSW5mb3NQcm9kdWNlZCBleHRlbmRzIEtlcm5lbEV2ZW50IHtcclxuICAgIHZhbHVlSW5mb3M6IEFycmF5PEtlcm5lbFZhbHVlSW5mbz47XHJcbn1cclxuXHJcbmV4cG9ydCBpbnRlcmZhY2UgVmFsdWVQcm9kdWNlZCBleHRlbmRzIEtlcm5lbEV2ZW50IHtcclxuICAgIG5hbWU6IHN0cmluZztcclxuICAgIGZvcm1hdHRlZFZhbHVlOiBGb3JtYXR0ZWRWYWx1ZTtcclxufVxyXG5cclxuZXhwb3J0IGludGVyZmFjZSBXb3JraW5nRGlyZWN0b3J5Q2hhbmdlZCBleHRlbmRzIEtlcm5lbEV2ZW50IHtcclxuICAgIHdvcmtpbmdEaXJlY3Rvcnk6IHN0cmluZztcclxufVxyXG5cclxuLy8gLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tIFJlcXVpcmVkIFR5cGVzXHJcblxyXG5leHBvcnQgaW50ZXJmYWNlIEJhc2U2NEVuY29kZWRBc3NlbWJseSB7XHJcbiAgICB2YWx1ZTogc3RyaW5nO1xyXG59XHJcblxyXG5leHBvcnQgaW50ZXJmYWNlIENvbXBsZXRpb25JdGVtIHtcclxuICAgIGRpc3BsYXlUZXh0OiBzdHJpbmc7XHJcbiAgICBraW5kOiBzdHJpbmc7XHJcbiAgICBmaWx0ZXJUZXh0OiBzdHJpbmc7XHJcbiAgICBzb3J0VGV4dDogc3RyaW5nO1xyXG4gICAgaW5zZXJ0VGV4dDogc3RyaW5nO1xyXG4gICAgaW5zZXJ0VGV4dEZvcm1hdD86IEluc2VydFRleHRGb3JtYXQ7XHJcbiAgICBkb2N1bWVudGF0aW9uOiBzdHJpbmc7XHJcbn1cclxuXHJcbmV4cG9ydCBlbnVtIEluc2VydFRleHRGb3JtYXQge1xyXG4gICAgUGxhaW5UZXh0ID0gXCJwbGFpbnRleHRcIixcclxuICAgIFNuaXBwZXQgPSBcInNuaXBwZXRcIixcclxufVxyXG5cclxuZXhwb3J0IGludGVyZmFjZSBEaWFnbm9zdGljIHtcclxuICAgIGxpbmVQb3NpdGlvblNwYW46IExpbmVQb3NpdGlvblNwYW47XHJcbiAgICBzZXZlcml0eTogRGlhZ25vc3RpY1NldmVyaXR5O1xyXG4gICAgY29kZTogc3RyaW5nO1xyXG4gICAgbWVzc2FnZTogc3RyaW5nO1xyXG59XHJcblxyXG5leHBvcnQgZW51bSBEaWFnbm9zdGljU2V2ZXJpdHkge1xyXG4gICAgSGlkZGVuID0gXCJoaWRkZW5cIixcclxuICAgIEluZm8gPSBcImluZm9cIixcclxuICAgIFdhcm5pbmcgPSBcIndhcm5pbmdcIixcclxuICAgIEVycm9yID0gXCJlcnJvclwiLFxyXG59XHJcblxyXG5leHBvcnQgaW50ZXJmYWNlIExpbmVQb3NpdGlvblNwYW4ge1xyXG4gICAgc3RhcnQ6IExpbmVQb3NpdGlvbjtcclxuICAgIGVuZDogTGluZVBvc2l0aW9uO1xyXG59XHJcblxyXG5leHBvcnQgaW50ZXJmYWNlIExpbmVQb3NpdGlvbiB7XHJcbiAgICBsaW5lOiBudW1iZXI7XHJcbiAgICBjaGFyYWN0ZXI6IG51bWJlcjtcclxufVxyXG5cclxuZXhwb3J0IGVudW0gRG9jdW1lbnRTZXJpYWxpemF0aW9uVHlwZSB7XHJcbiAgICBEaWIgPSBcImRpYlwiLFxyXG4gICAgSXB5bmIgPSBcImlweW5iXCIsXHJcbn1cclxuXHJcbmV4cG9ydCBpbnRlcmZhY2UgRm9ybWF0dGVkVmFsdWUge1xyXG4gICAgbWltZVR5cGU6IHN0cmluZztcclxuICAgIHZhbHVlOiBzdHJpbmc7XHJcbn1cclxuXHJcbmV4cG9ydCBpbnRlcmZhY2UgSW50ZXJhY3RpdmVEb2N1bWVudCB7XHJcbiAgICBlbGVtZW50czogQXJyYXk8SW50ZXJhY3RpdmVEb2N1bWVudEVsZW1lbnQ+O1xyXG4gICAgbWV0YWRhdGE6IHsgW2tleTogc3RyaW5nXTogYW55OyB9O1xyXG59XHJcblxyXG5leHBvcnQgaW50ZXJmYWNlIEludGVyYWN0aXZlRG9jdW1lbnRFbGVtZW50IHtcclxuICAgIGlkPzogc3RyaW5nO1xyXG4gICAga2VybmVsTmFtZT86IHN0cmluZztcclxuICAgIGNvbnRlbnRzOiBzdHJpbmc7XHJcbiAgICBvdXRwdXRzOiBBcnJheTxJbnRlcmFjdGl2ZURvY3VtZW50T3V0cHV0RWxlbWVudD47XHJcbiAgICBleGVjdXRpb25PcmRlcjogbnVtYmVyO1xyXG4gICAgbWV0YWRhdGE/OiB7IFtrZXk6IHN0cmluZ106IGFueTsgfTtcclxufVxyXG5cclxuZXhwb3J0IGludGVyZmFjZSBLZXJuZWxJbmZvIHtcclxuICAgIGFsaWFzZXM6IEFycmF5PHN0cmluZz47XHJcbiAgICBsYW5ndWFnZU5hbWU/OiBzdHJpbmc7XHJcbiAgICBsYW5ndWFnZVZlcnNpb24/OiBzdHJpbmc7XHJcbiAgICBkaXNwbGF5TmFtZTogc3RyaW5nO1xyXG4gICAgbG9jYWxOYW1lOiBzdHJpbmc7XHJcbiAgICB1cmk6IHN0cmluZztcclxuICAgIHJlbW90ZVVyaT86IHN0cmluZztcclxuICAgIHN1cHBvcnRlZEtlcm5lbENvbW1hbmRzOiBBcnJheTxLZXJuZWxDb21tYW5kSW5mbz47XHJcbiAgICBzdXBwb3J0ZWREaXJlY3RpdmVzOiBBcnJheTxLZXJuZWxEaXJlY3RpdmVJbmZvPjtcclxufVxyXG5cclxuZXhwb3J0IGludGVyZmFjZSBLZXJuZWxDb21tYW5kSW5mbyB7XHJcbiAgICBuYW1lOiBzdHJpbmc7XHJcbn1cclxuXHJcbmV4cG9ydCBpbnRlcmZhY2UgS2VybmVsRGlyZWN0aXZlSW5mbyB7XHJcbiAgICBuYW1lOiBzdHJpbmc7XHJcbn1cclxuXHJcbmV4cG9ydCBpbnRlcmZhY2UgS2VybmVsVmFsdWVJbmZvIHtcclxuICAgIG5hbWU6IHN0cmluZztcclxuICAgIHByZWZlcnJlZE1pbWVUeXBlczogQXJyYXk8c3RyaW5nPjtcclxufVxyXG5cclxuZXhwb3J0IGludGVyZmFjZSBQcm9qZWN0IHtcclxuICAgIGZpbGVzOiBBcnJheTxQcm9qZWN0RmlsZT47XHJcbn1cclxuXHJcbmV4cG9ydCBpbnRlcmZhY2UgUHJvamVjdEZpbGUge1xyXG4gICAgcmVsYXRpdmVGaWxlUGF0aDogc3RyaW5nO1xyXG4gICAgY29udGVudDogc3RyaW5nO1xyXG59XHJcblxyXG5leHBvcnQgaW50ZXJmYWNlIFByb2plY3RJdGVtIHtcclxuICAgIHJlbGF0aXZlRmlsZVBhdGg6IHN0cmluZztcclxuICAgIHJlZ2lvbk5hbWVzOiBBcnJheTxzdHJpbmc+O1xyXG4gICAgcmVnaW9uc0NvbnRlbnQ6IHsgW2tleTogc3RyaW5nXTogc3RyaW5nOyB9O1xyXG59XHJcblxyXG5leHBvcnQgZW51bSBSZXF1ZXN0VHlwZSB7XHJcbiAgICBQYXJzZSA9IFwicGFyc2VcIixcclxuICAgIFNlcmlhbGl6ZSA9IFwic2VyaWFsaXplXCIsXHJcbn1cclxuXHJcbmV4cG9ydCBpbnRlcmZhY2UgUmVzb2x2ZWRQYWNrYWdlUmVmZXJlbmNlIGV4dGVuZHMgUGFja2FnZVJlZmVyZW5jZSB7XHJcbiAgICBhc3NlbWJseVBhdGhzOiBBcnJheTxzdHJpbmc+O1xyXG4gICAgcHJvYmluZ1BhdGhzOiBBcnJheTxzdHJpbmc+O1xyXG4gICAgcGFja2FnZVJvb3Q6IHN0cmluZztcclxufVxyXG5cclxuZXhwb3J0IGludGVyZmFjZSBQYWNrYWdlUmVmZXJlbmNlIHtcclxuICAgIHBhY2thZ2VOYW1lOiBzdHJpbmc7XHJcbiAgICBwYWNrYWdlVmVyc2lvbjogc3RyaW5nO1xyXG4gICAgaXNQYWNrYWdlVmVyc2lvblNwZWNpZmllZDogYm9vbGVhbjtcclxufVxyXG5cclxuZXhwb3J0IGludGVyZmFjZSBTaWduYXR1cmVJbmZvcm1hdGlvbiB7XHJcbiAgICBsYWJlbDogc3RyaW5nO1xyXG4gICAgZG9jdW1lbnRhdGlvbjogRm9ybWF0dGVkVmFsdWU7XHJcbiAgICBwYXJhbWV0ZXJzOiBBcnJheTxQYXJhbWV0ZXJJbmZvcm1hdGlvbj47XHJcbn1cclxuXHJcbmV4cG9ydCBpbnRlcmZhY2UgUGFyYW1ldGVySW5mb3JtYXRpb24ge1xyXG4gICAgbGFiZWw6IHN0cmluZztcclxuICAgIGRvY3VtZW50YXRpb246IEZvcm1hdHRlZFZhbHVlO1xyXG59XHJcblxyXG5leHBvcnQgZW51bSBTdWJtaXNzaW9uVHlwZSB7XHJcbiAgICBSdW4gPSBcInJ1blwiLFxyXG4gICAgRGlhZ25vc2UgPSBcImRpYWdub3NlXCIsXHJcbn1cclxuXHJcbmV4cG9ydCBpbnRlcmZhY2UgRG9jdW1lbnRLZXJuZWxJbmZvQ29sbGVjdGlvbiB7XG4gICAgZGVmYXVsdEtlcm5lbE5hbWU6IHN0cmluZztcbiAgICBpdGVtczogRG9jdW1lbnRLZXJuZWxJbmZvW107XG59XG5cbmV4cG9ydCBpbnRlcmZhY2UgS2VybmVsRXZlbnRFbnZlbG9wZSB7XG4gICAgZXZlbnRUeXBlOiBLZXJuZWxFdmVudFR5cGU7XG4gICAgZXZlbnQ6IEtlcm5lbEV2ZW50O1xuICAgIGNvbW1hbmQ/OiBLZXJuZWxDb21tYW5kRW52ZWxvcGU7XG4gICAgcm91dGluZ1NsaXA/OiBzdHJpbmdbXTtcbn1cblxuZXhwb3J0IGludGVyZmFjZSBLZXJuZWxDb21tYW5kRW52ZWxvcGUge1xuICAgIHRva2VuPzogc3RyaW5nO1xuICAgIGlkPzogc3RyaW5nO1xuICAgIGNvbW1hbmRUeXBlOiBLZXJuZWxDb21tYW5kVHlwZTtcbiAgICBjb21tYW5kOiBLZXJuZWxDb21tYW5kO1xuICAgIHJvdXRpbmdTbGlwPzogc3RyaW5nW107XG59XG5cbmV4cG9ydCBpbnRlcmZhY2UgS2VybmVsRXZlbnRFbnZlbG9wZU9ic2VydmVyIHtcbiAgICAoZXZlbnRFbnZlbG9wZTogS2VybmVsRXZlbnRFbnZlbG9wZSk6IHZvaWQ7XG59XG5cbmV4cG9ydCBpbnRlcmZhY2UgS2VybmVsQ29tbWFuZEVudmVsb3BlSGFuZGxlciB7XG4gICAgKGV2ZW50RW52ZWxvcGU6IEtlcm5lbENvbW1hbmRFbnZlbG9wZSk6IFByb21pc2U8dm9pZD47XG59XG4iLCJleHBvcnQgZnVuY3Rpb24gaXNGdW5jdGlvbih2YWx1ZSkge1xuICAgIHJldHVybiB0eXBlb2YgdmFsdWUgPT09ICdmdW5jdGlvbic7XG59XG4vLyMgc291cmNlTWFwcGluZ1VSTD1pc0Z1bmN0aW9uLmpzLm1hcCIsImV4cG9ydCBmdW5jdGlvbiBjcmVhdGVFcnJvckNsYXNzKGNyZWF0ZUltcGwpIHtcbiAgICB2YXIgX3N1cGVyID0gZnVuY3Rpb24gKGluc3RhbmNlKSB7XG4gICAgICAgIEVycm9yLmNhbGwoaW5zdGFuY2UpO1xuICAgICAgICBpbnN0YW5jZS5zdGFjayA9IG5ldyBFcnJvcigpLnN0YWNrO1xuICAgIH07XG4gICAgdmFyIGN0b3JGdW5jID0gY3JlYXRlSW1wbChfc3VwZXIpO1xuICAgIGN0b3JGdW5jLnByb3RvdHlwZSA9IE9iamVjdC5jcmVhdGUoRXJyb3IucHJvdG90eXBlKTtcbiAgICBjdG9yRnVuYy5wcm90b3R5cGUuY29uc3RydWN0b3IgPSBjdG9yRnVuYztcbiAgICByZXR1cm4gY3RvckZ1bmM7XG59XG4vLyMgc291cmNlTWFwcGluZ1VSTD1jcmVhdGVFcnJvckNsYXNzLmpzLm1hcCIsImltcG9ydCB7IGNyZWF0ZUVycm9yQ2xhc3MgfSBmcm9tICcuL2NyZWF0ZUVycm9yQ2xhc3MnO1xuZXhwb3J0IHZhciBVbnN1YnNjcmlwdGlvbkVycm9yID0gY3JlYXRlRXJyb3JDbGFzcyhmdW5jdGlvbiAoX3N1cGVyKSB7XG4gICAgcmV0dXJuIGZ1bmN0aW9uIFVuc3Vic2NyaXB0aW9uRXJyb3JJbXBsKGVycm9ycykge1xuICAgICAgICBfc3VwZXIodGhpcyk7XG4gICAgICAgIHRoaXMubWVzc2FnZSA9IGVycm9yc1xuICAgICAgICAgICAgPyBlcnJvcnMubGVuZ3RoICsgXCIgZXJyb3JzIG9jY3VycmVkIGR1cmluZyB1bnN1YnNjcmlwdGlvbjpcXG5cIiArIGVycm9ycy5tYXAoZnVuY3Rpb24gKGVyciwgaSkgeyByZXR1cm4gaSArIDEgKyBcIikgXCIgKyBlcnIudG9TdHJpbmcoKTsgfSkuam9pbignXFxuICAnKVxuICAgICAgICAgICAgOiAnJztcbiAgICAgICAgdGhpcy5uYW1lID0gJ1Vuc3Vic2NyaXB0aW9uRXJyb3InO1xuICAgICAgICB0aGlzLmVycm9ycyA9IGVycm9ycztcbiAgICB9O1xufSk7XG4vLyMgc291cmNlTWFwcGluZ1VSTD1VbnN1YnNjcmlwdGlvbkVycm9yLmpzLm1hcCIsImV4cG9ydCBmdW5jdGlvbiBhcnJSZW1vdmUoYXJyLCBpdGVtKSB7XG4gICAgaWYgKGFycikge1xuICAgICAgICB2YXIgaW5kZXggPSBhcnIuaW5kZXhPZihpdGVtKTtcbiAgICAgICAgMCA8PSBpbmRleCAmJiBhcnIuc3BsaWNlKGluZGV4LCAxKTtcbiAgICB9XG59XG4vLyMgc291cmNlTWFwcGluZ1VSTD1hcnJSZW1vdmUuanMubWFwIiwiaW1wb3J0IHsgX19yZWFkLCBfX3NwcmVhZEFycmF5LCBfX3ZhbHVlcyB9IGZyb20gXCJ0c2xpYlwiO1xuaW1wb3J0IHsgaXNGdW5jdGlvbiB9IGZyb20gJy4vdXRpbC9pc0Z1bmN0aW9uJztcbmltcG9ydCB7IFVuc3Vic2NyaXB0aW9uRXJyb3IgfSBmcm9tICcuL3V0aWwvVW5zdWJzY3JpcHRpb25FcnJvcic7XG5pbXBvcnQgeyBhcnJSZW1vdmUgfSBmcm9tICcuL3V0aWwvYXJyUmVtb3ZlJztcbnZhciBTdWJzY3JpcHRpb24gPSAoZnVuY3Rpb24gKCkge1xuICAgIGZ1bmN0aW9uIFN1YnNjcmlwdGlvbihpbml0aWFsVGVhcmRvd24pIHtcbiAgICAgICAgdGhpcy5pbml0aWFsVGVhcmRvd24gPSBpbml0aWFsVGVhcmRvd247XG4gICAgICAgIHRoaXMuY2xvc2VkID0gZmFsc2U7XG4gICAgICAgIHRoaXMuX3BhcmVudGFnZSA9IG51bGw7XG4gICAgICAgIHRoaXMuX2ZpbmFsaXplcnMgPSBudWxsO1xuICAgIH1cbiAgICBTdWJzY3JpcHRpb24ucHJvdG90eXBlLnVuc3Vic2NyaWJlID0gZnVuY3Rpb24gKCkge1xuICAgICAgICB2YXIgZV8xLCBfYSwgZV8yLCBfYjtcbiAgICAgICAgdmFyIGVycm9ycztcbiAgICAgICAgaWYgKCF0aGlzLmNsb3NlZCkge1xuICAgICAgICAgICAgdGhpcy5jbG9zZWQgPSB0cnVlO1xuICAgICAgICAgICAgdmFyIF9wYXJlbnRhZ2UgPSB0aGlzLl9wYXJlbnRhZ2U7XG4gICAgICAgICAgICBpZiAoX3BhcmVudGFnZSkge1xuICAgICAgICAgICAgICAgIHRoaXMuX3BhcmVudGFnZSA9IG51bGw7XG4gICAgICAgICAgICAgICAgaWYgKEFycmF5LmlzQXJyYXkoX3BhcmVudGFnZSkpIHtcbiAgICAgICAgICAgICAgICAgICAgdHJ5IHtcbiAgICAgICAgICAgICAgICAgICAgICAgIGZvciAodmFyIF9wYXJlbnRhZ2VfMSA9IF9fdmFsdWVzKF9wYXJlbnRhZ2UpLCBfcGFyZW50YWdlXzFfMSA9IF9wYXJlbnRhZ2VfMS5uZXh0KCk7ICFfcGFyZW50YWdlXzFfMS5kb25lOyBfcGFyZW50YWdlXzFfMSA9IF9wYXJlbnRhZ2VfMS5uZXh0KCkpIHtcbiAgICAgICAgICAgICAgICAgICAgICAgICAgICB2YXIgcGFyZW50XzEgPSBfcGFyZW50YWdlXzFfMS52YWx1ZTtcbiAgICAgICAgICAgICAgICAgICAgICAgICAgICBwYXJlbnRfMS5yZW1vdmUodGhpcyk7XG4gICAgICAgICAgICAgICAgICAgICAgICB9XG4gICAgICAgICAgICAgICAgICAgIH1cbiAgICAgICAgICAgICAgICAgICAgY2F0Y2ggKGVfMV8xKSB7IGVfMSA9IHsgZXJyb3I6IGVfMV8xIH07IH1cbiAgICAgICAgICAgICAgICAgICAgZmluYWxseSB7XG4gICAgICAgICAgICAgICAgICAgICAgICB0cnkge1xuICAgICAgICAgICAgICAgICAgICAgICAgICAgIGlmIChfcGFyZW50YWdlXzFfMSAmJiAhX3BhcmVudGFnZV8xXzEuZG9uZSAmJiAoX2EgPSBfcGFyZW50YWdlXzEucmV0dXJuKSkgX2EuY2FsbChfcGFyZW50YWdlXzEpO1xuICAgICAgICAgICAgICAgICAgICAgICAgfVxuICAgICAgICAgICAgICAgICAgICAgICAgZmluYWxseSB7IGlmIChlXzEpIHRocm93IGVfMS5lcnJvcjsgfVxuICAgICAgICAgICAgICAgICAgICB9XG4gICAgICAgICAgICAgICAgfVxuICAgICAgICAgICAgICAgIGVsc2Uge1xuICAgICAgICAgICAgICAgICAgICBfcGFyZW50YWdlLnJlbW92ZSh0aGlzKTtcbiAgICAgICAgICAgICAgICB9XG4gICAgICAgICAgICB9XG4gICAgICAgICAgICB2YXIgaW5pdGlhbEZpbmFsaXplciA9IHRoaXMuaW5pdGlhbFRlYXJkb3duO1xuICAgICAgICAgICAgaWYgKGlzRnVuY3Rpb24oaW5pdGlhbEZpbmFsaXplcikpIHtcbiAgICAgICAgICAgICAgICB0cnkge1xuICAgICAgICAgICAgICAgICAgICBpbml0aWFsRmluYWxpemVyKCk7XG4gICAgICAgICAgICAgICAgfVxuICAgICAgICAgICAgICAgIGNhdGNoIChlKSB7XG4gICAgICAgICAgICAgICAgICAgIGVycm9ycyA9IGUgaW5zdGFuY2VvZiBVbnN1YnNjcmlwdGlvbkVycm9yID8gZS5lcnJvcnMgOiBbZV07XG4gICAgICAgICAgICAgICAgfVxuICAgICAgICAgICAgfVxuICAgICAgICAgICAgdmFyIF9maW5hbGl6ZXJzID0gdGhpcy5fZmluYWxpemVycztcbiAgICAgICAgICAgIGlmIChfZmluYWxpemVycykge1xuICAgICAgICAgICAgICAgIHRoaXMuX2ZpbmFsaXplcnMgPSBudWxsO1xuICAgICAgICAgICAgICAgIHRyeSB7XG4gICAgICAgICAgICAgICAgICAgIGZvciAodmFyIF9maW5hbGl6ZXJzXzEgPSBfX3ZhbHVlcyhfZmluYWxpemVycyksIF9maW5hbGl6ZXJzXzFfMSA9IF9maW5hbGl6ZXJzXzEubmV4dCgpOyAhX2ZpbmFsaXplcnNfMV8xLmRvbmU7IF9maW5hbGl6ZXJzXzFfMSA9IF9maW5hbGl6ZXJzXzEubmV4dCgpKSB7XG4gICAgICAgICAgICAgICAgICAgICAgICB2YXIgZmluYWxpemVyID0gX2ZpbmFsaXplcnNfMV8xLnZhbHVlO1xuICAgICAgICAgICAgICAgICAgICAgICAgdHJ5IHtcbiAgICAgICAgICAgICAgICAgICAgICAgICAgICBleGVjRmluYWxpemVyKGZpbmFsaXplcik7XG4gICAgICAgICAgICAgICAgICAgICAgICB9XG4gICAgICAgICAgICAgICAgICAgICAgICBjYXRjaCAoZXJyKSB7XG4gICAgICAgICAgICAgICAgICAgICAgICAgICAgZXJyb3JzID0gZXJyb3JzICE9PSBudWxsICYmIGVycm9ycyAhPT0gdm9pZCAwID8gZXJyb3JzIDogW107XG4gICAgICAgICAgICAgICAgICAgICAgICAgICAgaWYgKGVyciBpbnN0YW5jZW9mIFVuc3Vic2NyaXB0aW9uRXJyb3IpIHtcbiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgZXJyb3JzID0gX19zcHJlYWRBcnJheShfX3NwcmVhZEFycmF5KFtdLCBfX3JlYWQoZXJyb3JzKSksIF9fcmVhZChlcnIuZXJyb3JzKSk7XG4gICAgICAgICAgICAgICAgICAgICAgICAgICAgfVxuICAgICAgICAgICAgICAgICAgICAgICAgICAgIGVsc2Uge1xuICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICBlcnJvcnMucHVzaChlcnIpO1xuICAgICAgICAgICAgICAgICAgICAgICAgICAgIH1cbiAgICAgICAgICAgICAgICAgICAgICAgIH1cbiAgICAgICAgICAgICAgICAgICAgfVxuICAgICAgICAgICAgICAgIH1cbiAgICAgICAgICAgICAgICBjYXRjaCAoZV8yXzEpIHsgZV8yID0geyBlcnJvcjogZV8yXzEgfTsgfVxuICAgICAgICAgICAgICAgIGZpbmFsbHkge1xuICAgICAgICAgICAgICAgICAgICB0cnkge1xuICAgICAgICAgICAgICAgICAgICAgICAgaWYgKF9maW5hbGl6ZXJzXzFfMSAmJiAhX2ZpbmFsaXplcnNfMV8xLmRvbmUgJiYgKF9iID0gX2ZpbmFsaXplcnNfMS5yZXR1cm4pKSBfYi5jYWxsKF9maW5hbGl6ZXJzXzEpO1xuICAgICAgICAgICAgICAgICAgICB9XG4gICAgICAgICAgICAgICAgICAgIGZpbmFsbHkgeyBpZiAoZV8yKSB0aHJvdyBlXzIuZXJyb3I7IH1cbiAgICAgICAgICAgICAgICB9XG4gICAgICAgICAgICB9XG4gICAgICAgICAgICBpZiAoZXJyb3JzKSB7XG4gICAgICAgICAgICAgICAgdGhyb3cgbmV3IFVuc3Vic2NyaXB0aW9uRXJyb3IoZXJyb3JzKTtcbiAgICAgICAgICAgIH1cbiAgICAgICAgfVxuICAgIH07XG4gICAgU3Vic2NyaXB0aW9uLnByb3RvdHlwZS5hZGQgPSBmdW5jdGlvbiAodGVhcmRvd24pIHtcbiAgICAgICAgdmFyIF9hO1xuICAgICAgICBpZiAodGVhcmRvd24gJiYgdGVhcmRvd24gIT09IHRoaXMpIHtcbiAgICAgICAgICAgIGlmICh0aGlzLmNsb3NlZCkge1xuICAgICAgICAgICAgICAgIGV4ZWNGaW5hbGl6ZXIodGVhcmRvd24pO1xuICAgICAgICAgICAgfVxuICAgICAgICAgICAgZWxzZSB7XG4gICAgICAgICAgICAgICAgaWYgKHRlYXJkb3duIGluc3RhbmNlb2YgU3Vic2NyaXB0aW9uKSB7XG4gICAgICAgICAgICAgICAgICAgIGlmICh0ZWFyZG93bi5jbG9zZWQgfHwgdGVhcmRvd24uX2hhc1BhcmVudCh0aGlzKSkge1xuICAgICAgICAgICAgICAgICAgICAgICAgcmV0dXJuO1xuICAgICAgICAgICAgICAgICAgICB9XG4gICAgICAgICAgICAgICAgICAgIHRlYXJkb3duLl9hZGRQYXJlbnQodGhpcyk7XG4gICAgICAgICAgICAgICAgfVxuICAgICAgICAgICAgICAgICh0aGlzLl9maW5hbGl6ZXJzID0gKF9hID0gdGhpcy5fZmluYWxpemVycykgIT09IG51bGwgJiYgX2EgIT09IHZvaWQgMCA/IF9hIDogW10pLnB1c2godGVhcmRvd24pO1xuICAgICAgICAgICAgfVxuICAgICAgICB9XG4gICAgfTtcbiAgICBTdWJzY3JpcHRpb24ucHJvdG90eXBlLl9oYXNQYXJlbnQgPSBmdW5jdGlvbiAocGFyZW50KSB7XG4gICAgICAgIHZhciBfcGFyZW50YWdlID0gdGhpcy5fcGFyZW50YWdlO1xuICAgICAgICByZXR1cm4gX3BhcmVudGFnZSA9PT0gcGFyZW50IHx8IChBcnJheS5pc0FycmF5KF9wYXJlbnRhZ2UpICYmIF9wYXJlbnRhZ2UuaW5jbHVkZXMocGFyZW50KSk7XG4gICAgfTtcbiAgICBTdWJzY3JpcHRpb24ucHJvdG90eXBlLl9hZGRQYXJlbnQgPSBmdW5jdGlvbiAocGFyZW50KSB7XG4gICAgICAgIHZhciBfcGFyZW50YWdlID0gdGhpcy5fcGFyZW50YWdlO1xuICAgICAgICB0aGlzLl9wYXJlbnRhZ2UgPSBBcnJheS5pc0FycmF5KF9wYXJlbnRhZ2UpID8gKF9wYXJlbnRhZ2UucHVzaChwYXJlbnQpLCBfcGFyZW50YWdlKSA6IF9wYXJlbnRhZ2UgPyBbX3BhcmVudGFnZSwgcGFyZW50XSA6IHBhcmVudDtcbiAgICB9O1xuICAgIFN1YnNjcmlwdGlvbi5wcm90b3R5cGUuX3JlbW92ZVBhcmVudCA9IGZ1bmN0aW9uIChwYXJlbnQpIHtcbiAgICAgICAgdmFyIF9wYXJlbnRhZ2UgPSB0aGlzLl9wYXJlbnRhZ2U7XG4gICAgICAgIGlmIChfcGFyZW50YWdlID09PSBwYXJlbnQpIHtcbiAgICAgICAgICAgIHRoaXMuX3BhcmVudGFnZSA9IG51bGw7XG4gICAgICAgIH1cbiAgICAgICAgZWxzZSBpZiAoQXJyYXkuaXNBcnJheShfcGFyZW50YWdlKSkge1xuICAgICAgICAgICAgYXJyUmVtb3ZlKF9wYXJlbnRhZ2UsIHBhcmVudCk7XG4gICAgICAgIH1cbiAgICB9O1xuICAgIFN1YnNjcmlwdGlvbi5wcm90b3R5cGUucmVtb3ZlID0gZnVuY3Rpb24gKHRlYXJkb3duKSB7XG4gICAgICAgIHZhciBfZmluYWxpemVycyA9IHRoaXMuX2ZpbmFsaXplcnM7XG4gICAgICAgIF9maW5hbGl6ZXJzICYmIGFyclJlbW92ZShfZmluYWxpemVycywgdGVhcmRvd24pO1xuICAgICAgICBpZiAodGVhcmRvd24gaW5zdGFuY2VvZiBTdWJzY3JpcHRpb24pIHtcbiAgICAgICAgICAgIHRlYXJkb3duLl9yZW1vdmVQYXJlbnQodGhpcyk7XG4gICAgICAgIH1cbiAgICB9O1xuICAgIFN1YnNjcmlwdGlvbi5FTVBUWSA9IChmdW5jdGlvbiAoKSB7XG4gICAgICAgIHZhciBlbXB0eSA9IG5ldyBTdWJzY3JpcHRpb24oKTtcbiAgICAgICAgZW1wdHkuY2xvc2VkID0gdHJ1ZTtcbiAgICAgICAgcmV0dXJuIGVtcHR5O1xuICAgIH0pKCk7XG4gICAgcmV0dXJuIFN1YnNjcmlwdGlvbjtcbn0oKSk7XG5leHBvcnQgeyBTdWJzY3JpcHRpb24gfTtcbmV4cG9ydCB2YXIgRU1QVFlfU1VCU0NSSVBUSU9OID0gU3Vic2NyaXB0aW9uLkVNUFRZO1xuZXhwb3J0IGZ1bmN0aW9uIGlzU3Vic2NyaXB0aW9uKHZhbHVlKSB7XG4gICAgcmV0dXJuICh2YWx1ZSBpbnN0YW5jZW9mIFN1YnNjcmlwdGlvbiB8fFxuICAgICAgICAodmFsdWUgJiYgJ2Nsb3NlZCcgaW4gdmFsdWUgJiYgaXNGdW5jdGlvbih2YWx1ZS5yZW1vdmUpICYmIGlzRnVuY3Rpb24odmFsdWUuYWRkKSAmJiBpc0Z1bmN0aW9uKHZhbHVlLnVuc3Vic2NyaWJlKSkpO1xufVxuZnVuY3Rpb24gZXhlY0ZpbmFsaXplcihmaW5hbGl6ZXIpIHtcbiAgICBpZiAoaXNGdW5jdGlvbihmaW5hbGl6ZXIpKSB7XG4gICAgICAgIGZpbmFsaXplcigpO1xuICAgIH1cbiAgICBlbHNlIHtcbiAgICAgICAgZmluYWxpemVyLnVuc3Vic2NyaWJlKCk7XG4gICAgfVxufVxuLy8jIHNvdXJjZU1hcHBpbmdVUkw9U3Vic2NyaXB0aW9uLmpzLm1hcCIsImV4cG9ydCB2YXIgY29uZmlnID0ge1xuICAgIG9uVW5oYW5kbGVkRXJyb3I6IG51bGwsXG4gICAgb25TdG9wcGVkTm90aWZpY2F0aW9uOiBudWxsLFxuICAgIFByb21pc2U6IHVuZGVmaW5lZCxcbiAgICB1c2VEZXByZWNhdGVkU3luY2hyb25vdXNFcnJvckhhbmRsaW5nOiBmYWxzZSxcbiAgICB1c2VEZXByZWNhdGVkTmV4dENvbnRleHQ6IGZhbHNlLFxufTtcbi8vIyBzb3VyY2VNYXBwaW5nVVJMPWNvbmZpZy5qcy5tYXAiLCJpbXBvcnQgeyBfX3JlYWQsIF9fc3ByZWFkQXJyYXkgfSBmcm9tIFwidHNsaWJcIjtcbmV4cG9ydCB2YXIgdGltZW91dFByb3ZpZGVyID0ge1xuICAgIHNldFRpbWVvdXQ6IGZ1bmN0aW9uIChoYW5kbGVyLCB0aW1lb3V0KSB7XG4gICAgICAgIHZhciBhcmdzID0gW107XG4gICAgICAgIGZvciAodmFyIF9pID0gMjsgX2kgPCBhcmd1bWVudHMubGVuZ3RoOyBfaSsrKSB7XG4gICAgICAgICAgICBhcmdzW19pIC0gMl0gPSBhcmd1bWVudHNbX2ldO1xuICAgICAgICB9XG4gICAgICAgIHZhciBkZWxlZ2F0ZSA9IHRpbWVvdXRQcm92aWRlci5kZWxlZ2F0ZTtcbiAgICAgICAgaWYgKGRlbGVnYXRlID09PSBudWxsIHx8IGRlbGVnYXRlID09PSB2b2lkIDAgPyB2b2lkIDAgOiBkZWxlZ2F0ZS5zZXRUaW1lb3V0KSB7XG4gICAgICAgICAgICByZXR1cm4gZGVsZWdhdGUuc2V0VGltZW91dC5hcHBseShkZWxlZ2F0ZSwgX19zcHJlYWRBcnJheShbaGFuZGxlciwgdGltZW91dF0sIF9fcmVhZChhcmdzKSkpO1xuICAgICAgICB9XG4gICAgICAgIHJldHVybiBzZXRUaW1lb3V0LmFwcGx5KHZvaWQgMCwgX19zcHJlYWRBcnJheShbaGFuZGxlciwgdGltZW91dF0sIF9fcmVhZChhcmdzKSkpO1xuICAgIH0sXG4gICAgY2xlYXJUaW1lb3V0OiBmdW5jdGlvbiAoaGFuZGxlKSB7XG4gICAgICAgIHZhciBkZWxlZ2F0ZSA9IHRpbWVvdXRQcm92aWRlci5kZWxlZ2F0ZTtcbiAgICAgICAgcmV0dXJuICgoZGVsZWdhdGUgPT09IG51bGwgfHwgZGVsZWdhdGUgPT09IHZvaWQgMCA/IHZvaWQgMCA6IGRlbGVnYXRlLmNsZWFyVGltZW91dCkgfHwgY2xlYXJUaW1lb3V0KShoYW5kbGUpO1xuICAgIH0sXG4gICAgZGVsZWdhdGU6IHVuZGVmaW5lZCxcbn07XG4vLyMgc291cmNlTWFwcGluZ1VSTD10aW1lb3V0UHJvdmlkZXIuanMubWFwIiwiaW1wb3J0IHsgY29uZmlnIH0gZnJvbSAnLi4vY29uZmlnJztcbmltcG9ydCB7IHRpbWVvdXRQcm92aWRlciB9IGZyb20gJy4uL3NjaGVkdWxlci90aW1lb3V0UHJvdmlkZXInO1xuZXhwb3J0IGZ1bmN0aW9uIHJlcG9ydFVuaGFuZGxlZEVycm9yKGVycikge1xuICAgIHRpbWVvdXRQcm92aWRlci5zZXRUaW1lb3V0KGZ1bmN0aW9uICgpIHtcbiAgICAgICAgdmFyIG9uVW5oYW5kbGVkRXJyb3IgPSBjb25maWcub25VbmhhbmRsZWRFcnJvcjtcbiAgICAgICAgaWYgKG9uVW5oYW5kbGVkRXJyb3IpIHtcbiAgICAgICAgICAgIG9uVW5oYW5kbGVkRXJyb3IoZXJyKTtcbiAgICAgICAgfVxuICAgICAgICBlbHNlIHtcbiAgICAgICAgICAgIHRocm93IGVycjtcbiAgICAgICAgfVxuICAgIH0pO1xufVxuLy8jIHNvdXJjZU1hcHBpbmdVUkw9cmVwb3J0VW5oYW5kbGVkRXJyb3IuanMubWFwIiwiZXhwb3J0IGZ1bmN0aW9uIG5vb3AoKSB7IH1cbi8vIyBzb3VyY2VNYXBwaW5nVVJMPW5vb3AuanMubWFwIiwiaW1wb3J0IHsgY29uZmlnIH0gZnJvbSAnLi4vY29uZmlnJztcbnZhciBjb250ZXh0ID0gbnVsbDtcbmV4cG9ydCBmdW5jdGlvbiBlcnJvckNvbnRleHQoY2IpIHtcbiAgICBpZiAoY29uZmlnLnVzZURlcHJlY2F0ZWRTeW5jaHJvbm91c0Vycm9ySGFuZGxpbmcpIHtcbiAgICAgICAgdmFyIGlzUm9vdCA9ICFjb250ZXh0O1xuICAgICAgICBpZiAoaXNSb290KSB7XG4gICAgICAgICAgICBjb250ZXh0ID0geyBlcnJvclRocm93bjogZmFsc2UsIGVycm9yOiBudWxsIH07XG4gICAgICAgIH1cbiAgICAgICAgY2IoKTtcbiAgICAgICAgaWYgKGlzUm9vdCkge1xuICAgICAgICAgICAgdmFyIF9hID0gY29udGV4dCwgZXJyb3JUaHJvd24gPSBfYS5lcnJvclRocm93biwgZXJyb3IgPSBfYS5lcnJvcjtcbiAgICAgICAgICAgIGNvbnRleHQgPSBudWxsO1xuICAgICAgICAgICAgaWYgKGVycm9yVGhyb3duKSB7XG4gICAgICAgICAgICAgICAgdGhyb3cgZXJyb3I7XG4gICAgICAgICAgICB9XG4gICAgICAgIH1cbiAgICB9XG4gICAgZWxzZSB7XG4gICAgICAgIGNiKCk7XG4gICAgfVxufVxuZXhwb3J0IGZ1bmN0aW9uIGNhcHR1cmVFcnJvcihlcnIpIHtcbiAgICBpZiAoY29uZmlnLnVzZURlcHJlY2F0ZWRTeW5jaHJvbm91c0Vycm9ySGFuZGxpbmcgJiYgY29udGV4dCkge1xuICAgICAgICBjb250ZXh0LmVycm9yVGhyb3duID0gdHJ1ZTtcbiAgICAgICAgY29udGV4dC5lcnJvciA9IGVycjtcbiAgICB9XG59XG4vLyMgc291cmNlTWFwcGluZ1VSTD1lcnJvckNvbnRleHQuanMubWFwIiwiaW1wb3J0IHsgX19leHRlbmRzIH0gZnJvbSBcInRzbGliXCI7XG5pbXBvcnQgeyBpc0Z1bmN0aW9uIH0gZnJvbSAnLi91dGlsL2lzRnVuY3Rpb24nO1xuaW1wb3J0IHsgaXNTdWJzY3JpcHRpb24sIFN1YnNjcmlwdGlvbiB9IGZyb20gJy4vU3Vic2NyaXB0aW9uJztcbmltcG9ydCB7IGNvbmZpZyB9IGZyb20gJy4vY29uZmlnJztcbmltcG9ydCB7IHJlcG9ydFVuaGFuZGxlZEVycm9yIH0gZnJvbSAnLi91dGlsL3JlcG9ydFVuaGFuZGxlZEVycm9yJztcbmltcG9ydCB7IG5vb3AgfSBmcm9tICcuL3V0aWwvbm9vcCc7XG5pbXBvcnQgeyBuZXh0Tm90aWZpY2F0aW9uLCBlcnJvck5vdGlmaWNhdGlvbiwgQ09NUExFVEVfTk9USUZJQ0FUSU9OIH0gZnJvbSAnLi9Ob3RpZmljYXRpb25GYWN0b3JpZXMnO1xuaW1wb3J0IHsgdGltZW91dFByb3ZpZGVyIH0gZnJvbSAnLi9zY2hlZHVsZXIvdGltZW91dFByb3ZpZGVyJztcbmltcG9ydCB7IGNhcHR1cmVFcnJvciB9IGZyb20gJy4vdXRpbC9lcnJvckNvbnRleHQnO1xudmFyIFN1YnNjcmliZXIgPSAoZnVuY3Rpb24gKF9zdXBlcikge1xuICAgIF9fZXh0ZW5kcyhTdWJzY3JpYmVyLCBfc3VwZXIpO1xuICAgIGZ1bmN0aW9uIFN1YnNjcmliZXIoZGVzdGluYXRpb24pIHtcbiAgICAgICAgdmFyIF90aGlzID0gX3N1cGVyLmNhbGwodGhpcykgfHwgdGhpcztcbiAgICAgICAgX3RoaXMuaXNTdG9wcGVkID0gZmFsc2U7XG4gICAgICAgIGlmIChkZXN0aW5hdGlvbikge1xuICAgICAgICAgICAgX3RoaXMuZGVzdGluYXRpb24gPSBkZXN0aW5hdGlvbjtcbiAgICAgICAgICAgIGlmIChpc1N1YnNjcmlwdGlvbihkZXN0aW5hdGlvbikpIHtcbiAgICAgICAgICAgICAgICBkZXN0aW5hdGlvbi5hZGQoX3RoaXMpO1xuICAgICAgICAgICAgfVxuICAgICAgICB9XG4gICAgICAgIGVsc2Uge1xuICAgICAgICAgICAgX3RoaXMuZGVzdGluYXRpb24gPSBFTVBUWV9PQlNFUlZFUjtcbiAgICAgICAgfVxuICAgICAgICByZXR1cm4gX3RoaXM7XG4gICAgfVxuICAgIFN1YnNjcmliZXIuY3JlYXRlID0gZnVuY3Rpb24gKG5leHQsIGVycm9yLCBjb21wbGV0ZSkge1xuICAgICAgICByZXR1cm4gbmV3IFNhZmVTdWJzY3JpYmVyKG5leHQsIGVycm9yLCBjb21wbGV0ZSk7XG4gICAgfTtcbiAgICBTdWJzY3JpYmVyLnByb3RvdHlwZS5uZXh0ID0gZnVuY3Rpb24gKHZhbHVlKSB7XG4gICAgICAgIGlmICh0aGlzLmlzU3RvcHBlZCkge1xuICAgICAgICAgICAgaGFuZGxlU3RvcHBlZE5vdGlmaWNhdGlvbihuZXh0Tm90aWZpY2F0aW9uKHZhbHVlKSwgdGhpcyk7XG4gICAgICAgIH1cbiAgICAgICAgZWxzZSB7XG4gICAgICAgICAgICB0aGlzLl9uZXh0KHZhbHVlKTtcbiAgICAgICAgfVxuICAgIH07XG4gICAgU3Vic2NyaWJlci5wcm90b3R5cGUuZXJyb3IgPSBmdW5jdGlvbiAoZXJyKSB7XG4gICAgICAgIGlmICh0aGlzLmlzU3RvcHBlZCkge1xuICAgICAgICAgICAgaGFuZGxlU3RvcHBlZE5vdGlmaWNhdGlvbihlcnJvck5vdGlmaWNhdGlvbihlcnIpLCB0aGlzKTtcbiAgICAgICAgfVxuICAgICAgICBlbHNlIHtcbiAgICAgICAgICAgIHRoaXMuaXNTdG9wcGVkID0gdHJ1ZTtcbiAgICAgICAgICAgIHRoaXMuX2Vycm9yKGVycik7XG4gICAgICAgIH1cbiAgICB9O1xuICAgIFN1YnNjcmliZXIucHJvdG90eXBlLmNvbXBsZXRlID0gZnVuY3Rpb24gKCkge1xuICAgICAgICBpZiAodGhpcy5pc1N0b3BwZWQpIHtcbiAgICAgICAgICAgIGhhbmRsZVN0b3BwZWROb3RpZmljYXRpb24oQ09NUExFVEVfTk9USUZJQ0FUSU9OLCB0aGlzKTtcbiAgICAgICAgfVxuICAgICAgICBlbHNlIHtcbiAgICAgICAgICAgIHRoaXMuaXNTdG9wcGVkID0gdHJ1ZTtcbiAgICAgICAgICAgIHRoaXMuX2NvbXBsZXRlKCk7XG4gICAgICAgIH1cbiAgICB9O1xuICAgIFN1YnNjcmliZXIucHJvdG90eXBlLnVuc3Vic2NyaWJlID0gZnVuY3Rpb24gKCkge1xuICAgICAgICBpZiAoIXRoaXMuY2xvc2VkKSB7XG4gICAgICAgICAgICB0aGlzLmlzU3RvcHBlZCA9IHRydWU7XG4gICAgICAgICAgICBfc3VwZXIucHJvdG90eXBlLnVuc3Vic2NyaWJlLmNhbGwodGhpcyk7XG4gICAgICAgICAgICB0aGlzLmRlc3RpbmF0aW9uID0gbnVsbDtcbiAgICAgICAgfVxuICAgIH07XG4gICAgU3Vic2NyaWJlci5wcm90b3R5cGUuX25leHQgPSBmdW5jdGlvbiAodmFsdWUpIHtcbiAgICAgICAgdGhpcy5kZXN0aW5hdGlvbi5uZXh0KHZhbHVlKTtcbiAgICB9O1xuICAgIFN1YnNjcmliZXIucHJvdG90eXBlLl9lcnJvciA9IGZ1bmN0aW9uIChlcnIpIHtcbiAgICAgICAgdHJ5IHtcbiAgICAgICAgICAgIHRoaXMuZGVzdGluYXRpb24uZXJyb3IoZXJyKTtcbiAgICAgICAgfVxuICAgICAgICBmaW5hbGx5IHtcbiAgICAgICAgICAgIHRoaXMudW5zdWJzY3JpYmUoKTtcbiAgICAgICAgfVxuICAgIH07XG4gICAgU3Vic2NyaWJlci5wcm90b3R5cGUuX2NvbXBsZXRlID0gZnVuY3Rpb24gKCkge1xuICAgICAgICB0cnkge1xuICAgICAgICAgICAgdGhpcy5kZXN0aW5hdGlvbi5jb21wbGV0ZSgpO1xuICAgICAgICB9XG4gICAgICAgIGZpbmFsbHkge1xuICAgICAgICAgICAgdGhpcy51bnN1YnNjcmliZSgpO1xuICAgICAgICB9XG4gICAgfTtcbiAgICByZXR1cm4gU3Vic2NyaWJlcjtcbn0oU3Vic2NyaXB0aW9uKSk7XG5leHBvcnQgeyBTdWJzY3JpYmVyIH07XG52YXIgX2JpbmQgPSBGdW5jdGlvbi5wcm90b3R5cGUuYmluZDtcbmZ1bmN0aW9uIGJpbmQoZm4sIHRoaXNBcmcpIHtcbiAgICByZXR1cm4gX2JpbmQuY2FsbChmbiwgdGhpc0FyZyk7XG59XG52YXIgQ29uc3VtZXJPYnNlcnZlciA9IChmdW5jdGlvbiAoKSB7XG4gICAgZnVuY3Rpb24gQ29uc3VtZXJPYnNlcnZlcihwYXJ0aWFsT2JzZXJ2ZXIpIHtcbiAgICAgICAgdGhpcy5wYXJ0aWFsT2JzZXJ2ZXIgPSBwYXJ0aWFsT2JzZXJ2ZXI7XG4gICAgfVxuICAgIENvbnN1bWVyT2JzZXJ2ZXIucHJvdG90eXBlLm5leHQgPSBmdW5jdGlvbiAodmFsdWUpIHtcbiAgICAgICAgdmFyIHBhcnRpYWxPYnNlcnZlciA9IHRoaXMucGFydGlhbE9ic2VydmVyO1xuICAgICAgICBpZiAocGFydGlhbE9ic2VydmVyLm5leHQpIHtcbiAgICAgICAgICAgIHRyeSB7XG4gICAgICAgICAgICAgICAgcGFydGlhbE9ic2VydmVyLm5leHQodmFsdWUpO1xuICAgICAgICAgICAgfVxuICAgICAgICAgICAgY2F0Y2ggKGVycm9yKSB7XG4gICAgICAgICAgICAgICAgaGFuZGxlVW5oYW5kbGVkRXJyb3IoZXJyb3IpO1xuICAgICAgICAgICAgfVxuICAgICAgICB9XG4gICAgfTtcbiAgICBDb25zdW1lck9ic2VydmVyLnByb3RvdHlwZS5lcnJvciA9IGZ1bmN0aW9uIChlcnIpIHtcbiAgICAgICAgdmFyIHBhcnRpYWxPYnNlcnZlciA9IHRoaXMucGFydGlhbE9ic2VydmVyO1xuICAgICAgICBpZiAocGFydGlhbE9ic2VydmVyLmVycm9yKSB7XG4gICAgICAgICAgICB0cnkge1xuICAgICAgICAgICAgICAgIHBhcnRpYWxPYnNlcnZlci5lcnJvcihlcnIpO1xuICAgICAgICAgICAgfVxuICAgICAgICAgICAgY2F0Y2ggKGVycm9yKSB7XG4gICAgICAgICAgICAgICAgaGFuZGxlVW5oYW5kbGVkRXJyb3IoZXJyb3IpO1xuICAgICAgICAgICAgfVxuICAgICAgICB9XG4gICAgICAgIGVsc2Uge1xuICAgICAgICAgICAgaGFuZGxlVW5oYW5kbGVkRXJyb3IoZXJyKTtcbiAgICAgICAgfVxuICAgIH07XG4gICAgQ29uc3VtZXJPYnNlcnZlci5wcm90b3R5cGUuY29tcGxldGUgPSBmdW5jdGlvbiAoKSB7XG4gICAgICAgIHZhciBwYXJ0aWFsT2JzZXJ2ZXIgPSB0aGlzLnBhcnRpYWxPYnNlcnZlcjtcbiAgICAgICAgaWYgKHBhcnRpYWxPYnNlcnZlci5jb21wbGV0ZSkge1xuICAgICAgICAgICAgdHJ5IHtcbiAgICAgICAgICAgICAgICBwYXJ0aWFsT2JzZXJ2ZXIuY29tcGxldGUoKTtcbiAgICAgICAgICAgIH1cbiAgICAgICAgICAgIGNhdGNoIChlcnJvcikge1xuICAgICAgICAgICAgICAgIGhhbmRsZVVuaGFuZGxlZEVycm9yKGVycm9yKTtcbiAgICAgICAgICAgIH1cbiAgICAgICAgfVxuICAgIH07XG4gICAgcmV0dXJuIENvbnN1bWVyT2JzZXJ2ZXI7XG59KCkpO1xudmFyIFNhZmVTdWJzY3JpYmVyID0gKGZ1bmN0aW9uIChfc3VwZXIpIHtcbiAgICBfX2V4dGVuZHMoU2FmZVN1YnNjcmliZXIsIF9zdXBlcik7XG4gICAgZnVuY3Rpb24gU2FmZVN1YnNjcmliZXIob2JzZXJ2ZXJPck5leHQsIGVycm9yLCBjb21wbGV0ZSkge1xuICAgICAgICB2YXIgX3RoaXMgPSBfc3VwZXIuY2FsbCh0aGlzKSB8fCB0aGlzO1xuICAgICAgICB2YXIgcGFydGlhbE9ic2VydmVyO1xuICAgICAgICBpZiAoaXNGdW5jdGlvbihvYnNlcnZlck9yTmV4dCkgfHwgIW9ic2VydmVyT3JOZXh0KSB7XG4gICAgICAgICAgICBwYXJ0aWFsT2JzZXJ2ZXIgPSB7XG4gICAgICAgICAgICAgICAgbmV4dDogKG9ic2VydmVyT3JOZXh0ICE9PSBudWxsICYmIG9ic2VydmVyT3JOZXh0ICE9PSB2b2lkIDAgPyBvYnNlcnZlck9yTmV4dCA6IHVuZGVmaW5lZCksXG4gICAgICAgICAgICAgICAgZXJyb3I6IGVycm9yICE9PSBudWxsICYmIGVycm9yICE9PSB2b2lkIDAgPyBlcnJvciA6IHVuZGVmaW5lZCxcbiAgICAgICAgICAgICAgICBjb21wbGV0ZTogY29tcGxldGUgIT09IG51bGwgJiYgY29tcGxldGUgIT09IHZvaWQgMCA/IGNvbXBsZXRlIDogdW5kZWZpbmVkLFxuICAgICAgICAgICAgfTtcbiAgICAgICAgfVxuICAgICAgICBlbHNlIHtcbiAgICAgICAgICAgIHZhciBjb250ZXh0XzE7XG4gICAgICAgICAgICBpZiAoX3RoaXMgJiYgY29uZmlnLnVzZURlcHJlY2F0ZWROZXh0Q29udGV4dCkge1xuICAgICAgICAgICAgICAgIGNvbnRleHRfMSA9IE9iamVjdC5jcmVhdGUob2JzZXJ2ZXJPck5leHQpO1xuICAgICAgICAgICAgICAgIGNvbnRleHRfMS51bnN1YnNjcmliZSA9IGZ1bmN0aW9uICgpIHsgcmV0dXJuIF90aGlzLnVuc3Vic2NyaWJlKCk7IH07XG4gICAgICAgICAgICAgICAgcGFydGlhbE9ic2VydmVyID0ge1xuICAgICAgICAgICAgICAgICAgICBuZXh0OiBvYnNlcnZlck9yTmV4dC5uZXh0ICYmIGJpbmQob2JzZXJ2ZXJPck5leHQubmV4dCwgY29udGV4dF8xKSxcbiAgICAgICAgICAgICAgICAgICAgZXJyb3I6IG9ic2VydmVyT3JOZXh0LmVycm9yICYmIGJpbmQob2JzZXJ2ZXJPck5leHQuZXJyb3IsIGNvbnRleHRfMSksXG4gICAgICAgICAgICAgICAgICAgIGNvbXBsZXRlOiBvYnNlcnZlck9yTmV4dC5jb21wbGV0ZSAmJiBiaW5kKG9ic2VydmVyT3JOZXh0LmNvbXBsZXRlLCBjb250ZXh0XzEpLFxuICAgICAgICAgICAgICAgIH07XG4gICAgICAgICAgICB9XG4gICAgICAgICAgICBlbHNlIHtcbiAgICAgICAgICAgICAgICBwYXJ0aWFsT2JzZXJ2ZXIgPSBvYnNlcnZlck9yTmV4dDtcbiAgICAgICAgICAgIH1cbiAgICAgICAgfVxuICAgICAgICBfdGhpcy5kZXN0aW5hdGlvbiA9IG5ldyBDb25zdW1lck9ic2VydmVyKHBhcnRpYWxPYnNlcnZlcik7XG4gICAgICAgIHJldHVybiBfdGhpcztcbiAgICB9XG4gICAgcmV0dXJuIFNhZmVTdWJzY3JpYmVyO1xufShTdWJzY3JpYmVyKSk7XG5leHBvcnQgeyBTYWZlU3Vic2NyaWJlciB9O1xuZnVuY3Rpb24gaGFuZGxlVW5oYW5kbGVkRXJyb3IoZXJyb3IpIHtcbiAgICBpZiAoY29uZmlnLnVzZURlcHJlY2F0ZWRTeW5jaHJvbm91c0Vycm9ySGFuZGxpbmcpIHtcbiAgICAgICAgY2FwdHVyZUVycm9yKGVycm9yKTtcbiAgICB9XG4gICAgZWxzZSB7XG4gICAgICAgIHJlcG9ydFVuaGFuZGxlZEVycm9yKGVycm9yKTtcbiAgICB9XG59XG5mdW5jdGlvbiBkZWZhdWx0RXJyb3JIYW5kbGVyKGVycikge1xuICAgIHRocm93IGVycjtcbn1cbmZ1bmN0aW9uIGhhbmRsZVN0b3BwZWROb3RpZmljYXRpb24obm90aWZpY2F0aW9uLCBzdWJzY3JpYmVyKSB7XG4gICAgdmFyIG9uU3RvcHBlZE5vdGlmaWNhdGlvbiA9IGNvbmZpZy5vblN0b3BwZWROb3RpZmljYXRpb247XG4gICAgb25TdG9wcGVkTm90aWZpY2F0aW9uICYmIHRpbWVvdXRQcm92aWRlci5zZXRUaW1lb3V0KGZ1bmN0aW9uICgpIHsgcmV0dXJuIG9uU3RvcHBlZE5vdGlmaWNhdGlvbihub3RpZmljYXRpb24sIHN1YnNjcmliZXIpOyB9KTtcbn1cbmV4cG9ydCB2YXIgRU1QVFlfT0JTRVJWRVIgPSB7XG4gICAgY2xvc2VkOiB0cnVlLFxuICAgIG5leHQ6IG5vb3AsXG4gICAgZXJyb3I6IGRlZmF1bHRFcnJvckhhbmRsZXIsXG4gICAgY29tcGxldGU6IG5vb3AsXG59O1xuLy8jIHNvdXJjZU1hcHBpbmdVUkw9U3Vic2NyaWJlci5qcy5tYXAiLCJleHBvcnQgdmFyIG9ic2VydmFibGUgPSAoZnVuY3Rpb24gKCkgeyByZXR1cm4gKHR5cGVvZiBTeW1ib2wgPT09ICdmdW5jdGlvbicgJiYgU3ltYm9sLm9ic2VydmFibGUpIHx8ICdAQG9ic2VydmFibGUnOyB9KSgpO1xuLy8jIHNvdXJjZU1hcHBpbmdVUkw9b2JzZXJ2YWJsZS5qcy5tYXAiLCJleHBvcnQgZnVuY3Rpb24gaWRlbnRpdHkoeCkge1xuICAgIHJldHVybiB4O1xufVxuLy8jIHNvdXJjZU1hcHBpbmdVUkw9aWRlbnRpdHkuanMubWFwIiwiaW1wb3J0IHsgaWRlbnRpdHkgfSBmcm9tICcuL2lkZW50aXR5JztcbmV4cG9ydCBmdW5jdGlvbiBwaXBlKCkge1xuICAgIHZhciBmbnMgPSBbXTtcbiAgICBmb3IgKHZhciBfaSA9IDA7IF9pIDwgYXJndW1lbnRzLmxlbmd0aDsgX2krKykge1xuICAgICAgICBmbnNbX2ldID0gYXJndW1lbnRzW19pXTtcbiAgICB9XG4gICAgcmV0dXJuIHBpcGVGcm9tQXJyYXkoZm5zKTtcbn1cbmV4cG9ydCBmdW5jdGlvbiBwaXBlRnJvbUFycmF5KGZucykge1xuICAgIGlmIChmbnMubGVuZ3RoID09PSAwKSB7XG4gICAgICAgIHJldHVybiBpZGVudGl0eTtcbiAgICB9XG4gICAgaWYgKGZucy5sZW5ndGggPT09IDEpIHtcbiAgICAgICAgcmV0dXJuIGZuc1swXTtcbiAgICB9XG4gICAgcmV0dXJuIGZ1bmN0aW9uIHBpcGVkKGlucHV0KSB7XG4gICAgICAgIHJldHVybiBmbnMucmVkdWNlKGZ1bmN0aW9uIChwcmV2LCBmbikgeyByZXR1cm4gZm4ocHJldik7IH0sIGlucHV0KTtcbiAgICB9O1xufVxuLy8jIHNvdXJjZU1hcHBpbmdVUkw9cGlwZS5qcy5tYXAiLCJpbXBvcnQgeyBTYWZlU3Vic2NyaWJlciwgU3Vic2NyaWJlciB9IGZyb20gJy4vU3Vic2NyaWJlcic7XG5pbXBvcnQgeyBpc1N1YnNjcmlwdGlvbiB9IGZyb20gJy4vU3Vic2NyaXB0aW9uJztcbmltcG9ydCB7IG9ic2VydmFibGUgYXMgU3ltYm9sX29ic2VydmFibGUgfSBmcm9tICcuL3N5bWJvbC9vYnNlcnZhYmxlJztcbmltcG9ydCB7IHBpcGVGcm9tQXJyYXkgfSBmcm9tICcuL3V0aWwvcGlwZSc7XG5pbXBvcnQgeyBjb25maWcgfSBmcm9tICcuL2NvbmZpZyc7XG5pbXBvcnQgeyBpc0Z1bmN0aW9uIH0gZnJvbSAnLi91dGlsL2lzRnVuY3Rpb24nO1xuaW1wb3J0IHsgZXJyb3JDb250ZXh0IH0gZnJvbSAnLi91dGlsL2Vycm9yQ29udGV4dCc7XG52YXIgT2JzZXJ2YWJsZSA9IChmdW5jdGlvbiAoKSB7XG4gICAgZnVuY3Rpb24gT2JzZXJ2YWJsZShzdWJzY3JpYmUpIHtcbiAgICAgICAgaWYgKHN1YnNjcmliZSkge1xuICAgICAgICAgICAgdGhpcy5fc3Vic2NyaWJlID0gc3Vic2NyaWJlO1xuICAgICAgICB9XG4gICAgfVxuICAgIE9ic2VydmFibGUucHJvdG90eXBlLmxpZnQgPSBmdW5jdGlvbiAob3BlcmF0b3IpIHtcbiAgICAgICAgdmFyIG9ic2VydmFibGUgPSBuZXcgT2JzZXJ2YWJsZSgpO1xuICAgICAgICBvYnNlcnZhYmxlLnNvdXJjZSA9IHRoaXM7XG4gICAgICAgIG9ic2VydmFibGUub3BlcmF0b3IgPSBvcGVyYXRvcjtcbiAgICAgICAgcmV0dXJuIG9ic2VydmFibGU7XG4gICAgfTtcbiAgICBPYnNlcnZhYmxlLnByb3RvdHlwZS5zdWJzY3JpYmUgPSBmdW5jdGlvbiAob2JzZXJ2ZXJPck5leHQsIGVycm9yLCBjb21wbGV0ZSkge1xuICAgICAgICB2YXIgX3RoaXMgPSB0aGlzO1xuICAgICAgICB2YXIgc3Vic2NyaWJlciA9IGlzU3Vic2NyaWJlcihvYnNlcnZlck9yTmV4dCkgPyBvYnNlcnZlck9yTmV4dCA6IG5ldyBTYWZlU3Vic2NyaWJlcihvYnNlcnZlck9yTmV4dCwgZXJyb3IsIGNvbXBsZXRlKTtcbiAgICAgICAgZXJyb3JDb250ZXh0KGZ1bmN0aW9uICgpIHtcbiAgICAgICAgICAgIHZhciBfYSA9IF90aGlzLCBvcGVyYXRvciA9IF9hLm9wZXJhdG9yLCBzb3VyY2UgPSBfYS5zb3VyY2U7XG4gICAgICAgICAgICBzdWJzY3JpYmVyLmFkZChvcGVyYXRvclxuICAgICAgICAgICAgICAgID9cbiAgICAgICAgICAgICAgICAgICAgb3BlcmF0b3IuY2FsbChzdWJzY3JpYmVyLCBzb3VyY2UpXG4gICAgICAgICAgICAgICAgOiBzb3VyY2VcbiAgICAgICAgICAgICAgICAgICAgP1xuICAgICAgICAgICAgICAgICAgICAgICAgX3RoaXMuX3N1YnNjcmliZShzdWJzY3JpYmVyKVxuICAgICAgICAgICAgICAgICAgICA6XG4gICAgICAgICAgICAgICAgICAgICAgICBfdGhpcy5fdHJ5U3Vic2NyaWJlKHN1YnNjcmliZXIpKTtcbiAgICAgICAgfSk7XG4gICAgICAgIHJldHVybiBzdWJzY3JpYmVyO1xuICAgIH07XG4gICAgT2JzZXJ2YWJsZS5wcm90b3R5cGUuX3RyeVN1YnNjcmliZSA9IGZ1bmN0aW9uIChzaW5rKSB7XG4gICAgICAgIHRyeSB7XG4gICAgICAgICAgICByZXR1cm4gdGhpcy5fc3Vic2NyaWJlKHNpbmspO1xuICAgICAgICB9XG4gICAgICAgIGNhdGNoIChlcnIpIHtcbiAgICAgICAgICAgIHNpbmsuZXJyb3IoZXJyKTtcbiAgICAgICAgfVxuICAgIH07XG4gICAgT2JzZXJ2YWJsZS5wcm90b3R5cGUuZm9yRWFjaCA9IGZ1bmN0aW9uIChuZXh0LCBwcm9taXNlQ3Rvcikge1xuICAgICAgICB2YXIgX3RoaXMgPSB0aGlzO1xuICAgICAgICBwcm9taXNlQ3RvciA9IGdldFByb21pc2VDdG9yKHByb21pc2VDdG9yKTtcbiAgICAgICAgcmV0dXJuIG5ldyBwcm9taXNlQ3RvcihmdW5jdGlvbiAocmVzb2x2ZSwgcmVqZWN0KSB7XG4gICAgICAgICAgICB2YXIgc3Vic2NyaWJlciA9IG5ldyBTYWZlU3Vic2NyaWJlcih7XG4gICAgICAgICAgICAgICAgbmV4dDogZnVuY3Rpb24gKHZhbHVlKSB7XG4gICAgICAgICAgICAgICAgICAgIHRyeSB7XG4gICAgICAgICAgICAgICAgICAgICAgICBuZXh0KHZhbHVlKTtcbiAgICAgICAgICAgICAgICAgICAgfVxuICAgICAgICAgICAgICAgICAgICBjYXRjaCAoZXJyKSB7XG4gICAgICAgICAgICAgICAgICAgICAgICByZWplY3QoZXJyKTtcbiAgICAgICAgICAgICAgICAgICAgICAgIHN1YnNjcmliZXIudW5zdWJzY3JpYmUoKTtcbiAgICAgICAgICAgICAgICAgICAgfVxuICAgICAgICAgICAgICAgIH0sXG4gICAgICAgICAgICAgICAgZXJyb3I6IHJlamVjdCxcbiAgICAgICAgICAgICAgICBjb21wbGV0ZTogcmVzb2x2ZSxcbiAgICAgICAgICAgIH0pO1xuICAgICAgICAgICAgX3RoaXMuc3Vic2NyaWJlKHN1YnNjcmliZXIpO1xuICAgICAgICB9KTtcbiAgICB9O1xuICAgIE9ic2VydmFibGUucHJvdG90eXBlLl9zdWJzY3JpYmUgPSBmdW5jdGlvbiAoc3Vic2NyaWJlcikge1xuICAgICAgICB2YXIgX2E7XG4gICAgICAgIHJldHVybiAoX2EgPSB0aGlzLnNvdXJjZSkgPT09IG51bGwgfHwgX2EgPT09IHZvaWQgMCA/IHZvaWQgMCA6IF9hLnN1YnNjcmliZShzdWJzY3JpYmVyKTtcbiAgICB9O1xuICAgIE9ic2VydmFibGUucHJvdG90eXBlW1N5bWJvbF9vYnNlcnZhYmxlXSA9IGZ1bmN0aW9uICgpIHtcbiAgICAgICAgcmV0dXJuIHRoaXM7XG4gICAgfTtcbiAgICBPYnNlcnZhYmxlLnByb3RvdHlwZS5waXBlID0gZnVuY3Rpb24gKCkge1xuICAgICAgICB2YXIgb3BlcmF0aW9ucyA9IFtdO1xuICAgICAgICBmb3IgKHZhciBfaSA9IDA7IF9pIDwgYXJndW1lbnRzLmxlbmd0aDsgX2krKykge1xuICAgICAgICAgICAgb3BlcmF0aW9uc1tfaV0gPSBhcmd1bWVudHNbX2ldO1xuICAgICAgICB9XG4gICAgICAgIHJldHVybiBwaXBlRnJvbUFycmF5KG9wZXJhdGlvbnMpKHRoaXMpO1xuICAgIH07XG4gICAgT2JzZXJ2YWJsZS5wcm90b3R5cGUudG9Qcm9taXNlID0gZnVuY3Rpb24gKHByb21pc2VDdG9yKSB7XG4gICAgICAgIHZhciBfdGhpcyA9IHRoaXM7XG4gICAgICAgIHByb21pc2VDdG9yID0gZ2V0UHJvbWlzZUN0b3IocHJvbWlzZUN0b3IpO1xuICAgICAgICByZXR1cm4gbmV3IHByb21pc2VDdG9yKGZ1bmN0aW9uIChyZXNvbHZlLCByZWplY3QpIHtcbiAgICAgICAgICAgIHZhciB2YWx1ZTtcbiAgICAgICAgICAgIF90aGlzLnN1YnNjcmliZShmdW5jdGlvbiAoeCkgeyByZXR1cm4gKHZhbHVlID0geCk7IH0sIGZ1bmN0aW9uIChlcnIpIHsgcmV0dXJuIHJlamVjdChlcnIpOyB9LCBmdW5jdGlvbiAoKSB7IHJldHVybiByZXNvbHZlKHZhbHVlKTsgfSk7XG4gICAgICAgIH0pO1xuICAgIH07XG4gICAgT2JzZXJ2YWJsZS5jcmVhdGUgPSBmdW5jdGlvbiAoc3Vic2NyaWJlKSB7XG4gICAgICAgIHJldHVybiBuZXcgT2JzZXJ2YWJsZShzdWJzY3JpYmUpO1xuICAgIH07XG4gICAgcmV0dXJuIE9ic2VydmFibGU7XG59KCkpO1xuZXhwb3J0IHsgT2JzZXJ2YWJsZSB9O1xuZnVuY3Rpb24gZ2V0UHJvbWlzZUN0b3IocHJvbWlzZUN0b3IpIHtcbiAgICB2YXIgX2E7XG4gICAgcmV0dXJuIChfYSA9IHByb21pc2VDdG9yICE9PSBudWxsICYmIHByb21pc2VDdG9yICE9PSB2b2lkIDAgPyBwcm9taXNlQ3RvciA6IGNvbmZpZy5Qcm9taXNlKSAhPT0gbnVsbCAmJiBfYSAhPT0gdm9pZCAwID8gX2EgOiBQcm9taXNlO1xufVxuZnVuY3Rpb24gaXNPYnNlcnZlcih2YWx1ZSkge1xuICAgIHJldHVybiB2YWx1ZSAmJiBpc0Z1bmN0aW9uKHZhbHVlLm5leHQpICYmIGlzRnVuY3Rpb24odmFsdWUuZXJyb3IpICYmIGlzRnVuY3Rpb24odmFsdWUuY29tcGxldGUpO1xufVxuZnVuY3Rpb24gaXNTdWJzY3JpYmVyKHZhbHVlKSB7XG4gICAgcmV0dXJuICh2YWx1ZSAmJiB2YWx1ZSBpbnN0YW5jZW9mIFN1YnNjcmliZXIpIHx8IChpc09ic2VydmVyKHZhbHVlKSAmJiBpc1N1YnNjcmlwdGlvbih2YWx1ZSkpO1xufVxuLy8jIHNvdXJjZU1hcHBpbmdVUkw9T2JzZXJ2YWJsZS5qcy5tYXAiLCJpbXBvcnQgeyBpc0Z1bmN0aW9uIH0gZnJvbSAnLi9pc0Z1bmN0aW9uJztcbmV4cG9ydCBmdW5jdGlvbiBoYXNMaWZ0KHNvdXJjZSkge1xuICAgIHJldHVybiBpc0Z1bmN0aW9uKHNvdXJjZSA9PT0gbnVsbCB8fCBzb3VyY2UgPT09IHZvaWQgMCA/IHZvaWQgMCA6IHNvdXJjZS5saWZ0KTtcbn1cbmV4cG9ydCBmdW5jdGlvbiBvcGVyYXRlKGluaXQpIHtcbiAgICByZXR1cm4gZnVuY3Rpb24gKHNvdXJjZSkge1xuICAgICAgICBpZiAoaGFzTGlmdChzb3VyY2UpKSB7XG4gICAgICAgICAgICByZXR1cm4gc291cmNlLmxpZnQoZnVuY3Rpb24gKGxpZnRlZFNvdXJjZSkge1xuICAgICAgICAgICAgICAgIHRyeSB7XG4gICAgICAgICAgICAgICAgICAgIHJldHVybiBpbml0KGxpZnRlZFNvdXJjZSwgdGhpcyk7XG4gICAgICAgICAgICAgICAgfVxuICAgICAgICAgICAgICAgIGNhdGNoIChlcnIpIHtcbiAgICAgICAgICAgICAgICAgICAgdGhpcy5lcnJvcihlcnIpO1xuICAgICAgICAgICAgICAgIH1cbiAgICAgICAgICAgIH0pO1xuICAgICAgICB9XG4gICAgICAgIHRocm93IG5ldyBUeXBlRXJyb3IoJ1VuYWJsZSB0byBsaWZ0IHVua25vd24gT2JzZXJ2YWJsZSB0eXBlJyk7XG4gICAgfTtcbn1cbi8vIyBzb3VyY2VNYXBwaW5nVVJMPWxpZnQuanMubWFwIiwiaW1wb3J0IHsgX19leHRlbmRzIH0gZnJvbSBcInRzbGliXCI7XG5pbXBvcnQgeyBTdWJzY3JpYmVyIH0gZnJvbSAnLi4vU3Vic2NyaWJlcic7XG5leHBvcnQgZnVuY3Rpb24gY3JlYXRlT3BlcmF0b3JTdWJzY3JpYmVyKGRlc3RpbmF0aW9uLCBvbk5leHQsIG9uQ29tcGxldGUsIG9uRXJyb3IsIG9uRmluYWxpemUpIHtcbiAgICByZXR1cm4gbmV3IE9wZXJhdG9yU3Vic2NyaWJlcihkZXN0aW5hdGlvbiwgb25OZXh0LCBvbkNvbXBsZXRlLCBvbkVycm9yLCBvbkZpbmFsaXplKTtcbn1cbnZhciBPcGVyYXRvclN1YnNjcmliZXIgPSAoZnVuY3Rpb24gKF9zdXBlcikge1xuICAgIF9fZXh0ZW5kcyhPcGVyYXRvclN1YnNjcmliZXIsIF9zdXBlcik7XG4gICAgZnVuY3Rpb24gT3BlcmF0b3JTdWJzY3JpYmVyKGRlc3RpbmF0aW9uLCBvbk5leHQsIG9uQ29tcGxldGUsIG9uRXJyb3IsIG9uRmluYWxpemUsIHNob3VsZFVuc3Vic2NyaWJlKSB7XG4gICAgICAgIHZhciBfdGhpcyA9IF9zdXBlci5jYWxsKHRoaXMsIGRlc3RpbmF0aW9uKSB8fCB0aGlzO1xuICAgICAgICBfdGhpcy5vbkZpbmFsaXplID0gb25GaW5hbGl6ZTtcbiAgICAgICAgX3RoaXMuc2hvdWxkVW5zdWJzY3JpYmUgPSBzaG91bGRVbnN1YnNjcmliZTtcbiAgICAgICAgX3RoaXMuX25leHQgPSBvbk5leHRcbiAgICAgICAgICAgID8gZnVuY3Rpb24gKHZhbHVlKSB7XG4gICAgICAgICAgICAgICAgdHJ5IHtcbiAgICAgICAgICAgICAgICAgICAgb25OZXh0KHZhbHVlKTtcbiAgICAgICAgICAgICAgICB9XG4gICAgICAgICAgICAgICAgY2F0Y2ggKGVycikge1xuICAgICAgICAgICAgICAgICAgICBkZXN0aW5hdGlvbi5lcnJvcihlcnIpO1xuICAgICAgICAgICAgICAgIH1cbiAgICAgICAgICAgIH1cbiAgICAgICAgICAgIDogX3N1cGVyLnByb3RvdHlwZS5fbmV4dDtcbiAgICAgICAgX3RoaXMuX2Vycm9yID0gb25FcnJvclxuICAgICAgICAgICAgPyBmdW5jdGlvbiAoZXJyKSB7XG4gICAgICAgICAgICAgICAgdHJ5IHtcbiAgICAgICAgICAgICAgICAgICAgb25FcnJvcihlcnIpO1xuICAgICAgICAgICAgICAgIH1cbiAgICAgICAgICAgICAgICBjYXRjaCAoZXJyKSB7XG4gICAgICAgICAgICAgICAgICAgIGRlc3RpbmF0aW9uLmVycm9yKGVycik7XG4gICAgICAgICAgICAgICAgfVxuICAgICAgICAgICAgICAgIGZpbmFsbHkge1xuICAgICAgICAgICAgICAgICAgICB0aGlzLnVuc3Vic2NyaWJlKCk7XG4gICAgICAgICAgICAgICAgfVxuICAgICAgICAgICAgfVxuICAgICAgICAgICAgOiBfc3VwZXIucHJvdG90eXBlLl9lcnJvcjtcbiAgICAgICAgX3RoaXMuX2NvbXBsZXRlID0gb25Db21wbGV0ZVxuICAgICAgICAgICAgPyBmdW5jdGlvbiAoKSB7XG4gICAgICAgICAgICAgICAgdHJ5IHtcbiAgICAgICAgICAgICAgICAgICAgb25Db21wbGV0ZSgpO1xuICAgICAgICAgICAgICAgIH1cbiAgICAgICAgICAgICAgICBjYXRjaCAoZXJyKSB7XG4gICAgICAgICAgICAgICAgICAgIGRlc3RpbmF0aW9uLmVycm9yKGVycik7XG4gICAgICAgICAgICAgICAgfVxuICAgICAgICAgICAgICAgIGZpbmFsbHkge1xuICAgICAgICAgICAgICAgICAgICB0aGlzLnVuc3Vic2NyaWJlKCk7XG4gICAgICAgICAgICAgICAgfVxuICAgICAgICAgICAgfVxuICAgICAgICAgICAgOiBfc3VwZXIucHJvdG90eXBlLl9jb21wbGV0ZTtcbiAgICAgICAgcmV0dXJuIF90aGlzO1xuICAgIH1cbiAgICBPcGVyYXRvclN1YnNjcmliZXIucHJvdG90eXBlLnVuc3Vic2NyaWJlID0gZnVuY3Rpb24gKCkge1xuICAgICAgICB2YXIgX2E7XG4gICAgICAgIGlmICghdGhpcy5zaG91bGRVbnN1YnNjcmliZSB8fCB0aGlzLnNob3VsZFVuc3Vic2NyaWJlKCkpIHtcbiAgICAgICAgICAgIHZhciBjbG9zZWRfMSA9IHRoaXMuY2xvc2VkO1xuICAgICAgICAgICAgX3N1cGVyLnByb3RvdHlwZS51bnN1YnNjcmliZS5jYWxsKHRoaXMpO1xuICAgICAgICAgICAgIWNsb3NlZF8xICYmICgoX2EgPSB0aGlzLm9uRmluYWxpemUpID09PSBudWxsIHx8IF9hID09PSB2b2lkIDAgPyB2b2lkIDAgOiBfYS5jYWxsKHRoaXMpKTtcbiAgICAgICAgfVxuICAgIH07XG4gICAgcmV0dXJuIE9wZXJhdG9yU3Vic2NyaWJlcjtcbn0oU3Vic2NyaWJlcikpO1xuZXhwb3J0IHsgT3BlcmF0b3JTdWJzY3JpYmVyIH07XG4vLyMgc291cmNlTWFwcGluZ1VSTD1PcGVyYXRvclN1YnNjcmliZXIuanMubWFwIiwiaW1wb3J0IHsgY3JlYXRlRXJyb3JDbGFzcyB9IGZyb20gJy4vY3JlYXRlRXJyb3JDbGFzcyc7XG5leHBvcnQgdmFyIE9iamVjdFVuc3Vic2NyaWJlZEVycm9yID0gY3JlYXRlRXJyb3JDbGFzcyhmdW5jdGlvbiAoX3N1cGVyKSB7XG4gICAgcmV0dXJuIGZ1bmN0aW9uIE9iamVjdFVuc3Vic2NyaWJlZEVycm9ySW1wbCgpIHtcbiAgICAgICAgX3N1cGVyKHRoaXMpO1xuICAgICAgICB0aGlzLm5hbWUgPSAnT2JqZWN0VW5zdWJzY3JpYmVkRXJyb3InO1xuICAgICAgICB0aGlzLm1lc3NhZ2UgPSAnb2JqZWN0IHVuc3Vic2NyaWJlZCc7XG4gICAgfTtcbn0pO1xuLy8jIHNvdXJjZU1hcHBpbmdVUkw9T2JqZWN0VW5zdWJzY3JpYmVkRXJyb3IuanMubWFwIiwiaW1wb3J0IHsgX19leHRlbmRzLCBfX3ZhbHVlcyB9IGZyb20gXCJ0c2xpYlwiO1xuaW1wb3J0IHsgT2JzZXJ2YWJsZSB9IGZyb20gJy4vT2JzZXJ2YWJsZSc7XG5pbXBvcnQgeyBTdWJzY3JpcHRpb24sIEVNUFRZX1NVQlNDUklQVElPTiB9IGZyb20gJy4vU3Vic2NyaXB0aW9uJztcbmltcG9ydCB7IE9iamVjdFVuc3Vic2NyaWJlZEVycm9yIH0gZnJvbSAnLi91dGlsL09iamVjdFVuc3Vic2NyaWJlZEVycm9yJztcbmltcG9ydCB7IGFyclJlbW92ZSB9IGZyb20gJy4vdXRpbC9hcnJSZW1vdmUnO1xuaW1wb3J0IHsgZXJyb3JDb250ZXh0IH0gZnJvbSAnLi91dGlsL2Vycm9yQ29udGV4dCc7XG52YXIgU3ViamVjdCA9IChmdW5jdGlvbiAoX3N1cGVyKSB7XG4gICAgX19leHRlbmRzKFN1YmplY3QsIF9zdXBlcik7XG4gICAgZnVuY3Rpb24gU3ViamVjdCgpIHtcbiAgICAgICAgdmFyIF90aGlzID0gX3N1cGVyLmNhbGwodGhpcykgfHwgdGhpcztcbiAgICAgICAgX3RoaXMuY2xvc2VkID0gZmFsc2U7XG4gICAgICAgIF90aGlzLmN1cnJlbnRPYnNlcnZlcnMgPSBudWxsO1xuICAgICAgICBfdGhpcy5vYnNlcnZlcnMgPSBbXTtcbiAgICAgICAgX3RoaXMuaXNTdG9wcGVkID0gZmFsc2U7XG4gICAgICAgIF90aGlzLmhhc0Vycm9yID0gZmFsc2U7XG4gICAgICAgIF90aGlzLnRocm93bkVycm9yID0gbnVsbDtcbiAgICAgICAgcmV0dXJuIF90aGlzO1xuICAgIH1cbiAgICBTdWJqZWN0LnByb3RvdHlwZS5saWZ0ID0gZnVuY3Rpb24gKG9wZXJhdG9yKSB7XG4gICAgICAgIHZhciBzdWJqZWN0ID0gbmV3IEFub255bW91c1N1YmplY3QodGhpcywgdGhpcyk7XG4gICAgICAgIHN1YmplY3Qub3BlcmF0b3IgPSBvcGVyYXRvcjtcbiAgICAgICAgcmV0dXJuIHN1YmplY3Q7XG4gICAgfTtcbiAgICBTdWJqZWN0LnByb3RvdHlwZS5fdGhyb3dJZkNsb3NlZCA9IGZ1bmN0aW9uICgpIHtcbiAgICAgICAgaWYgKHRoaXMuY2xvc2VkKSB7XG4gICAgICAgICAgICB0aHJvdyBuZXcgT2JqZWN0VW5zdWJzY3JpYmVkRXJyb3IoKTtcbiAgICAgICAgfVxuICAgIH07XG4gICAgU3ViamVjdC5wcm90b3R5cGUubmV4dCA9IGZ1bmN0aW9uICh2YWx1ZSkge1xuICAgICAgICB2YXIgX3RoaXMgPSB0aGlzO1xuICAgICAgICBlcnJvckNvbnRleHQoZnVuY3Rpb24gKCkge1xuICAgICAgICAgICAgdmFyIGVfMSwgX2E7XG4gICAgICAgICAgICBfdGhpcy5fdGhyb3dJZkNsb3NlZCgpO1xuICAgICAgICAgICAgaWYgKCFfdGhpcy5pc1N0b3BwZWQpIHtcbiAgICAgICAgICAgICAgICBpZiAoIV90aGlzLmN1cnJlbnRPYnNlcnZlcnMpIHtcbiAgICAgICAgICAgICAgICAgICAgX3RoaXMuY3VycmVudE9ic2VydmVycyA9IEFycmF5LmZyb20oX3RoaXMub2JzZXJ2ZXJzKTtcbiAgICAgICAgICAgICAgICB9XG4gICAgICAgICAgICAgICAgdHJ5IHtcbiAgICAgICAgICAgICAgICAgICAgZm9yICh2YXIgX2IgPSBfX3ZhbHVlcyhfdGhpcy5jdXJyZW50T2JzZXJ2ZXJzKSwgX2MgPSBfYi5uZXh0KCk7ICFfYy5kb25lOyBfYyA9IF9iLm5leHQoKSkge1xuICAgICAgICAgICAgICAgICAgICAgICAgdmFyIG9ic2VydmVyID0gX2MudmFsdWU7XG4gICAgICAgICAgICAgICAgICAgICAgICBvYnNlcnZlci5uZXh0KHZhbHVlKTtcbiAgICAgICAgICAgICAgICAgICAgfVxuICAgICAgICAgICAgICAgIH1cbiAgICAgICAgICAgICAgICBjYXRjaCAoZV8xXzEpIHsgZV8xID0geyBlcnJvcjogZV8xXzEgfTsgfVxuICAgICAgICAgICAgICAgIGZpbmFsbHkge1xuICAgICAgICAgICAgICAgICAgICB0cnkge1xuICAgICAgICAgICAgICAgICAgICAgICAgaWYgKF9jICYmICFfYy5kb25lICYmIChfYSA9IF9iLnJldHVybikpIF9hLmNhbGwoX2IpO1xuICAgICAgICAgICAgICAgICAgICB9XG4gICAgICAgICAgICAgICAgICAgIGZpbmFsbHkgeyBpZiAoZV8xKSB0aHJvdyBlXzEuZXJyb3I7IH1cbiAgICAgICAgICAgICAgICB9XG4gICAgICAgICAgICB9XG4gICAgICAgIH0pO1xuICAgIH07XG4gICAgU3ViamVjdC5wcm90b3R5cGUuZXJyb3IgPSBmdW5jdGlvbiAoZXJyKSB7XG4gICAgICAgIHZhciBfdGhpcyA9IHRoaXM7XG4gICAgICAgIGVycm9yQ29udGV4dChmdW5jdGlvbiAoKSB7XG4gICAgICAgICAgICBfdGhpcy5fdGhyb3dJZkNsb3NlZCgpO1xuICAgICAgICAgICAgaWYgKCFfdGhpcy5pc1N0b3BwZWQpIHtcbiAgICAgICAgICAgICAgICBfdGhpcy5oYXNFcnJvciA9IF90aGlzLmlzU3RvcHBlZCA9IHRydWU7XG4gICAgICAgICAgICAgICAgX3RoaXMudGhyb3duRXJyb3IgPSBlcnI7XG4gICAgICAgICAgICAgICAgdmFyIG9ic2VydmVycyA9IF90aGlzLm9ic2VydmVycztcbiAgICAgICAgICAgICAgICB3aGlsZSAob2JzZXJ2ZXJzLmxlbmd0aCkge1xuICAgICAgICAgICAgICAgICAgICBvYnNlcnZlcnMuc2hpZnQoKS5lcnJvcihlcnIpO1xuICAgICAgICAgICAgICAgIH1cbiAgICAgICAgICAgIH1cbiAgICAgICAgfSk7XG4gICAgfTtcbiAgICBTdWJqZWN0LnByb3RvdHlwZS5jb21wbGV0ZSA9IGZ1bmN0aW9uICgpIHtcbiAgICAgICAgdmFyIF90aGlzID0gdGhpcztcbiAgICAgICAgZXJyb3JDb250ZXh0KGZ1bmN0aW9uICgpIHtcbiAgICAgICAgICAgIF90aGlzLl90aHJvd0lmQ2xvc2VkKCk7XG4gICAgICAgICAgICBpZiAoIV90aGlzLmlzU3RvcHBlZCkge1xuICAgICAgICAgICAgICAgIF90aGlzLmlzU3RvcHBlZCA9IHRydWU7XG4gICAgICAgICAgICAgICAgdmFyIG9ic2VydmVycyA9IF90aGlzLm9ic2VydmVycztcbiAgICAgICAgICAgICAgICB3aGlsZSAob2JzZXJ2ZXJzLmxlbmd0aCkge1xuICAgICAgICAgICAgICAgICAgICBvYnNlcnZlcnMuc2hpZnQoKS5jb21wbGV0ZSgpO1xuICAgICAgICAgICAgICAgIH1cbiAgICAgICAgICAgIH1cbiAgICAgICAgfSk7XG4gICAgfTtcbiAgICBTdWJqZWN0LnByb3RvdHlwZS51bnN1YnNjcmliZSA9IGZ1bmN0aW9uICgpIHtcbiAgICAgICAgdGhpcy5pc1N0b3BwZWQgPSB0aGlzLmNsb3NlZCA9IHRydWU7XG4gICAgICAgIHRoaXMub2JzZXJ2ZXJzID0gdGhpcy5jdXJyZW50T2JzZXJ2ZXJzID0gbnVsbDtcbiAgICB9O1xuICAgIE9iamVjdC5kZWZpbmVQcm9wZXJ0eShTdWJqZWN0LnByb3RvdHlwZSwgXCJvYnNlcnZlZFwiLCB7XG4gICAgICAgIGdldDogZnVuY3Rpb24gKCkge1xuICAgICAgICAgICAgdmFyIF9hO1xuICAgICAgICAgICAgcmV0dXJuICgoX2EgPSB0aGlzLm9ic2VydmVycykgPT09IG51bGwgfHwgX2EgPT09IHZvaWQgMCA/IHZvaWQgMCA6IF9hLmxlbmd0aCkgPiAwO1xuICAgICAgICB9LFxuICAgICAgICBlbnVtZXJhYmxlOiBmYWxzZSxcbiAgICAgICAgY29uZmlndXJhYmxlOiB0cnVlXG4gICAgfSk7XG4gICAgU3ViamVjdC5wcm90b3R5cGUuX3RyeVN1YnNjcmliZSA9IGZ1bmN0aW9uIChzdWJzY3JpYmVyKSB7XG4gICAgICAgIHRoaXMuX3Rocm93SWZDbG9zZWQoKTtcbiAgICAgICAgcmV0dXJuIF9zdXBlci5wcm90b3R5cGUuX3RyeVN1YnNjcmliZS5jYWxsKHRoaXMsIHN1YnNjcmliZXIpO1xuICAgIH07XG4gICAgU3ViamVjdC5wcm90b3R5cGUuX3N1YnNjcmliZSA9IGZ1bmN0aW9uIChzdWJzY3JpYmVyKSB7XG4gICAgICAgIHRoaXMuX3Rocm93SWZDbG9zZWQoKTtcbiAgICAgICAgdGhpcy5fY2hlY2tGaW5hbGl6ZWRTdGF0dXNlcyhzdWJzY3JpYmVyKTtcbiAgICAgICAgcmV0dXJuIHRoaXMuX2lubmVyU3Vic2NyaWJlKHN1YnNjcmliZXIpO1xuICAgIH07XG4gICAgU3ViamVjdC5wcm90b3R5cGUuX2lubmVyU3Vic2NyaWJlID0gZnVuY3Rpb24gKHN1YnNjcmliZXIpIHtcbiAgICAgICAgdmFyIF90aGlzID0gdGhpcztcbiAgICAgICAgdmFyIF9hID0gdGhpcywgaGFzRXJyb3IgPSBfYS5oYXNFcnJvciwgaXNTdG9wcGVkID0gX2EuaXNTdG9wcGVkLCBvYnNlcnZlcnMgPSBfYS5vYnNlcnZlcnM7XG4gICAgICAgIGlmIChoYXNFcnJvciB8fCBpc1N0b3BwZWQpIHtcbiAgICAgICAgICAgIHJldHVybiBFTVBUWV9TVUJTQ1JJUFRJT047XG4gICAgICAgIH1cbiAgICAgICAgdGhpcy5jdXJyZW50T2JzZXJ2ZXJzID0gbnVsbDtcbiAgICAgICAgb2JzZXJ2ZXJzLnB1c2goc3Vic2NyaWJlcik7XG4gICAgICAgIHJldHVybiBuZXcgU3Vic2NyaXB0aW9uKGZ1bmN0aW9uICgpIHtcbiAgICAgICAgICAgIF90aGlzLmN1cnJlbnRPYnNlcnZlcnMgPSBudWxsO1xuICAgICAgICAgICAgYXJyUmVtb3ZlKG9ic2VydmVycywgc3Vic2NyaWJlcik7XG4gICAgICAgIH0pO1xuICAgIH07XG4gICAgU3ViamVjdC5wcm90b3R5cGUuX2NoZWNrRmluYWxpemVkU3RhdHVzZXMgPSBmdW5jdGlvbiAoc3Vic2NyaWJlcikge1xuICAgICAgICB2YXIgX2EgPSB0aGlzLCBoYXNFcnJvciA9IF9hLmhhc0Vycm9yLCB0aHJvd25FcnJvciA9IF9hLnRocm93bkVycm9yLCBpc1N0b3BwZWQgPSBfYS5pc1N0b3BwZWQ7XG4gICAgICAgIGlmIChoYXNFcnJvcikge1xuICAgICAgICAgICAgc3Vic2NyaWJlci5lcnJvcih0aHJvd25FcnJvcik7XG4gICAgICAgIH1cbiAgICAgICAgZWxzZSBpZiAoaXNTdG9wcGVkKSB7XG4gICAgICAgICAgICBzdWJzY3JpYmVyLmNvbXBsZXRlKCk7XG4gICAgICAgIH1cbiAgICB9O1xuICAgIFN1YmplY3QucHJvdG90eXBlLmFzT2JzZXJ2YWJsZSA9IGZ1bmN0aW9uICgpIHtcbiAgICAgICAgdmFyIG9ic2VydmFibGUgPSBuZXcgT2JzZXJ2YWJsZSgpO1xuICAgICAgICBvYnNlcnZhYmxlLnNvdXJjZSA9IHRoaXM7XG4gICAgICAgIHJldHVybiBvYnNlcnZhYmxlO1xuICAgIH07XG4gICAgU3ViamVjdC5jcmVhdGUgPSBmdW5jdGlvbiAoZGVzdGluYXRpb24sIHNvdXJjZSkge1xuICAgICAgICByZXR1cm4gbmV3IEFub255bW91c1N1YmplY3QoZGVzdGluYXRpb24sIHNvdXJjZSk7XG4gICAgfTtcbiAgICByZXR1cm4gU3ViamVjdDtcbn0oT2JzZXJ2YWJsZSkpO1xuZXhwb3J0IHsgU3ViamVjdCB9O1xudmFyIEFub255bW91c1N1YmplY3QgPSAoZnVuY3Rpb24gKF9zdXBlcikge1xuICAgIF9fZXh0ZW5kcyhBbm9ueW1vdXNTdWJqZWN0LCBfc3VwZXIpO1xuICAgIGZ1bmN0aW9uIEFub255bW91c1N1YmplY3QoZGVzdGluYXRpb24sIHNvdXJjZSkge1xuICAgICAgICB2YXIgX3RoaXMgPSBfc3VwZXIuY2FsbCh0aGlzKSB8fCB0aGlzO1xuICAgICAgICBfdGhpcy5kZXN0aW5hdGlvbiA9IGRlc3RpbmF0aW9uO1xuICAgICAgICBfdGhpcy5zb3VyY2UgPSBzb3VyY2U7XG4gICAgICAgIHJldHVybiBfdGhpcztcbiAgICB9XG4gICAgQW5vbnltb3VzU3ViamVjdC5wcm90b3R5cGUubmV4dCA9IGZ1bmN0aW9uICh2YWx1ZSkge1xuICAgICAgICB2YXIgX2EsIF9iO1xuICAgICAgICAoX2IgPSAoX2EgPSB0aGlzLmRlc3RpbmF0aW9uKSA9PT0gbnVsbCB8fCBfYSA9PT0gdm9pZCAwID8gdm9pZCAwIDogX2EubmV4dCkgPT09IG51bGwgfHwgX2IgPT09IHZvaWQgMCA/IHZvaWQgMCA6IF9iLmNhbGwoX2EsIHZhbHVlKTtcbiAgICB9O1xuICAgIEFub255bW91c1N1YmplY3QucHJvdG90eXBlLmVycm9yID0gZnVuY3Rpb24gKGVycikge1xuICAgICAgICB2YXIgX2EsIF9iO1xuICAgICAgICAoX2IgPSAoX2EgPSB0aGlzLmRlc3RpbmF0aW9uKSA9PT0gbnVsbCB8fCBfYSA9PT0gdm9pZCAwID8gdm9pZCAwIDogX2EuZXJyb3IpID09PSBudWxsIHx8IF9iID09PSB2b2lkIDAgPyB2b2lkIDAgOiBfYi5jYWxsKF9hLCBlcnIpO1xuICAgIH07XG4gICAgQW5vbnltb3VzU3ViamVjdC5wcm90b3R5cGUuY29tcGxldGUgPSBmdW5jdGlvbiAoKSB7XG4gICAgICAgIHZhciBfYSwgX2I7XG4gICAgICAgIChfYiA9IChfYSA9IHRoaXMuZGVzdGluYXRpb24pID09PSBudWxsIHx8IF9hID09PSB2b2lkIDAgPyB2b2lkIDAgOiBfYS5jb21wbGV0ZSkgPT09IG51bGwgfHwgX2IgPT09IHZvaWQgMCA/IHZvaWQgMCA6IF9iLmNhbGwoX2EpO1xuICAgIH07XG4gICAgQW5vbnltb3VzU3ViamVjdC5wcm90b3R5cGUuX3N1YnNjcmliZSA9IGZ1bmN0aW9uIChzdWJzY3JpYmVyKSB7XG4gICAgICAgIHZhciBfYSwgX2I7XG4gICAgICAgIHJldHVybiAoX2IgPSAoX2EgPSB0aGlzLnNvdXJjZSkgPT09IG51bGwgfHwgX2EgPT09IHZvaWQgMCA/IHZvaWQgMCA6IF9hLnN1YnNjcmliZShzdWJzY3JpYmVyKSkgIT09IG51bGwgJiYgX2IgIT09IHZvaWQgMCA/IF9iIDogRU1QVFlfU1VCU0NSSVBUSU9OO1xuICAgIH07XG4gICAgcmV0dXJuIEFub255bW91c1N1YmplY3Q7XG59KFN1YmplY3QpKTtcbmV4cG9ydCB7IEFub255bW91c1N1YmplY3QgfTtcbi8vIyBzb3VyY2VNYXBwaW5nVVJMPVN1YmplY3QuanMubWFwIiwiaW1wb3J0IHsgb3BlcmF0ZSB9IGZyb20gJy4uL3V0aWwvbGlmdCc7XG5pbXBvcnQgeyBjcmVhdGVPcGVyYXRvclN1YnNjcmliZXIgfSBmcm9tICcuL09wZXJhdG9yU3Vic2NyaWJlcic7XG5leHBvcnQgZnVuY3Rpb24gbWFwKHByb2plY3QsIHRoaXNBcmcpIHtcbiAgICByZXR1cm4gb3BlcmF0ZShmdW5jdGlvbiAoc291cmNlLCBzdWJzY3JpYmVyKSB7XG4gICAgICAgIHZhciBpbmRleCA9IDA7XG4gICAgICAgIHNvdXJjZS5zdWJzY3JpYmUoY3JlYXRlT3BlcmF0b3JTdWJzY3JpYmVyKHN1YnNjcmliZXIsIGZ1bmN0aW9uICh2YWx1ZSkge1xuICAgICAgICAgICAgc3Vic2NyaWJlci5uZXh0KHByb2plY3QuY2FsbCh0aGlzQXJnLCB2YWx1ZSwgaW5kZXgrKykpO1xuICAgICAgICB9KSk7XG4gICAgfSk7XG59XG4vLyMgc291cmNlTWFwcGluZ1VSTD1tYXAuanMubWFwIiwiLy8gQ29weXJpZ2h0IChjKSAuTkVUIEZvdW5kYXRpb24gYW5kIGNvbnRyaWJ1dG9ycy4gQWxsIHJpZ2h0cyByZXNlcnZlZC5cclxuLy8gTGljZW5zZWQgdW5kZXIgdGhlIE1JVCBsaWNlbnNlLiBTZWUgTElDRU5TRSBmaWxlIGluIHRoZSBwcm9qZWN0IHJvb3QgZm9yIGZ1bGwgbGljZW5zZSBpbmZvcm1hdGlvbi5cclxuXHJcbmV4cG9ydCBmdW5jdGlvbiBpc1Byb21pc2VDb21wbGV0aW9uU291cmNlPFQ+KG9iajogYW55KTogb2JqIGlzIFByb21pc2VDb21wbGV0aW9uU291cmNlPFQ+IHtcclxuICAgIHJldHVybiBvYmoucHJvbWlzZVxyXG4gICAgICAgICYmIG9iai5yZXNvbHZlXHJcbiAgICAgICAgJiYgb2JqLnJlamVjdDtcclxufVxyXG5cclxuZXhwb3J0IGNsYXNzIFByb21pc2VDb21wbGV0aW9uU291cmNlPFQ+IHtcclxuICAgIHByaXZhdGUgX3Jlc29sdmU6ICh2YWx1ZTogVCkgPT4gdm9pZCA9ICgpID0+IHsgfTtcclxuICAgIHByaXZhdGUgX3JlamVjdDogKHJlYXNvbjogYW55KSA9PiB2b2lkID0gKCkgPT4geyB9O1xyXG4gICAgcmVhZG9ubHkgcHJvbWlzZTogUHJvbWlzZTxUPjtcclxuXHJcbiAgICBjb25zdHJ1Y3RvcigpIHtcclxuICAgICAgICB0aGlzLnByb21pc2UgPSBuZXcgUHJvbWlzZTxUPigocmVzb2x2ZSwgcmVqZWN0KSA9PiB7XHJcbiAgICAgICAgICAgIHRoaXMuX3Jlc29sdmUgPSByZXNvbHZlO1xyXG4gICAgICAgICAgICB0aGlzLl9yZWplY3QgPSByZWplY3Q7XHJcbiAgICAgICAgfSk7XHJcbiAgICB9XHJcblxyXG4gICAgcmVzb2x2ZSh2YWx1ZTogVCkge1xyXG4gICAgICAgIHRoaXMuX3Jlc29sdmUodmFsdWUpO1xyXG4gICAgfVxyXG5cclxuICAgIHJlamVjdChyZWFzb246IGFueSkge1xyXG4gICAgICAgIHRoaXMuX3JlamVjdChyZWFzb24pO1xyXG4gICAgfVxyXG59XHJcbiIsIi8vIENvcHlyaWdodCAoYykgLk5FVCBGb3VuZGF0aW9uIGFuZCBjb250cmlidXRvcnMuIEFsbCByaWdodHMgcmVzZXJ2ZWQuXHJcbi8vIExpY2Vuc2VkIHVuZGVyIHRoZSBNSVQgbGljZW5zZS4gU2VlIExJQ0VOU0UgZmlsZSBpbiB0aGUgcHJvamVjdCByb290IGZvciBmdWxsIGxpY2Vuc2UgaW5mb3JtYXRpb24uXHJcblxyXG5pbXBvcnQgKiBhcyByeGpzIGZyb20gXCJyeGpzXCI7XHJcbmltcG9ydCAqIGFzIHJvdXRpbmdzbGlwIGZyb20gXCIuL3JvdXRpbmdzbGlwXCI7XHJcbmltcG9ydCAqIGFzIGNvbnRyYWN0cyBmcm9tIFwiLi9jb250cmFjdHNcIjtcclxuaW1wb3J0IHsgRGlzcG9zYWJsZSB9IGZyb20gXCIuL2Rpc3Bvc2FibGVzXCI7XHJcbmltcG9ydCB7IGdldEtlcm5lbFVyaSwgS2VybmVsIH0gZnJvbSBcIi4va2VybmVsXCI7XHJcbmltcG9ydCB7IFByb21pc2VDb21wbGV0aW9uU291cmNlIH0gZnJvbSBcIi4vcHJvbWlzZUNvbXBsZXRpb25Tb3VyY2VcIjtcclxuXHJcblxyXG5leHBvcnQgY2xhc3MgS2VybmVsSW52b2NhdGlvbkNvbnRleHQgaW1wbGVtZW50cyBEaXNwb3NhYmxlIHtcclxuICAgIHB1YmxpYyBnZXQgcHJvbWlzZSgpOiB2b2lkIHwgUHJvbWlzZUxpa2U8dm9pZD4ge1xyXG4gICAgICAgIHJldHVybiB0aGlzLmNvbXBsZXRpb25Tb3VyY2UucHJvbWlzZTtcclxuICAgIH1cclxuICAgIHByaXZhdGUgc3RhdGljIF9jdXJyZW50OiBLZXJuZWxJbnZvY2F0aW9uQ29udGV4dCB8IG51bGwgPSBudWxsO1xyXG4gICAgcHJpdmF0ZSByZWFkb25seSBfY29tbWFuZEVudmVsb3BlOiBjb250cmFjdHMuS2VybmVsQ29tbWFuZEVudmVsb3BlO1xyXG4gICAgcHJpdmF0ZSByZWFkb25seSBfY2hpbGRDb21tYW5kczogY29udHJhY3RzLktlcm5lbENvbW1hbmRFbnZlbG9wZVtdID0gW107XHJcbiAgICBwcml2YXRlIHJlYWRvbmx5IF9ldmVudFN1YmplY3Q6IHJ4anMuU3ViamVjdDxjb250cmFjdHMuS2VybmVsRXZlbnRFbnZlbG9wZT4gPSBuZXcgcnhqcy5TdWJqZWN0PGNvbnRyYWN0cy5LZXJuZWxFdmVudEVudmVsb3BlPigpO1xyXG5cclxuICAgIHByaXZhdGUgX2lzQ29tcGxldGUgPSBmYWxzZTtcclxuICAgIHByaXZhdGUgX2hhbmRsaW5nS2VybmVsOiBLZXJuZWwgfCBudWxsID0gbnVsbDtcclxuXHJcbiAgICBwdWJsaWMgZ2V0IGhhbmRsaW5nS2VybmVsKCkge1xyXG4gICAgICAgIHJldHVybiB0aGlzLl9oYW5kbGluZ0tlcm5lbDtcclxuICAgIH07XHJcblxyXG4gICAgcHVibGljIGdldCBrZXJuZWxFdmVudHMoKTogcnhqcy5PYnNlcnZhYmxlPGNvbnRyYWN0cy5LZXJuZWxFdmVudEVudmVsb3BlPiB7XHJcbiAgICAgICAgcmV0dXJuIHRoaXMuX2V2ZW50U3ViamVjdC5hc09ic2VydmFibGUoKTtcclxuICAgIH07XHJcblxyXG4gICAgcHVibGljIHNldCBoYW5kbGluZ0tlcm5lbCh2YWx1ZTogS2VybmVsIHwgbnVsbCkge1xyXG4gICAgICAgIHRoaXMuX2hhbmRsaW5nS2VybmVsID0gdmFsdWU7XHJcbiAgICB9XHJcblxyXG4gICAgcHJpdmF0ZSBjb21wbGV0aW9uU291cmNlID0gbmV3IFByb21pc2VDb21wbGV0aW9uU291cmNlPHZvaWQ+KCk7XHJcbiAgICBzdGF0aWMgZXN0YWJsaXNoKGtlcm5lbENvbW1hbmRJbnZvY2F0aW9uOiBjb250cmFjdHMuS2VybmVsQ29tbWFuZEVudmVsb3BlKTogS2VybmVsSW52b2NhdGlvbkNvbnRleHQge1xyXG4gICAgICAgIGxldCBjdXJyZW50ID0gS2VybmVsSW52b2NhdGlvbkNvbnRleHQuX2N1cnJlbnQ7XHJcbiAgICAgICAgaWYgKCFjdXJyZW50IHx8IGN1cnJlbnQuX2lzQ29tcGxldGUpIHtcclxuICAgICAgICAgICAgS2VybmVsSW52b2NhdGlvbkNvbnRleHQuX2N1cnJlbnQgPSBuZXcgS2VybmVsSW52b2NhdGlvbkNvbnRleHQoa2VybmVsQ29tbWFuZEludm9jYXRpb24pO1xyXG4gICAgICAgIH0gZWxzZSB7XHJcbiAgICAgICAgICAgIGlmICghYXJlQ29tbWFuZHNUaGVTYW1lKGtlcm5lbENvbW1hbmRJbnZvY2F0aW9uLCBjdXJyZW50Ll9jb21tYW5kRW52ZWxvcGUpKSB7XHJcbiAgICAgICAgICAgICAgICBjb25zdCBmb3VuZCA9IGN1cnJlbnQuX2NoaWxkQ29tbWFuZHMuaW5jbHVkZXMoa2VybmVsQ29tbWFuZEludm9jYXRpb24pO1xyXG4gICAgICAgICAgICAgICAgaWYgKCFmb3VuZCkge1xyXG4gICAgICAgICAgICAgICAgICAgIGN1cnJlbnQuX2NoaWxkQ29tbWFuZHMucHVzaChrZXJuZWxDb21tYW5kSW52b2NhdGlvbik7XHJcbiAgICAgICAgICAgICAgICB9XHJcbiAgICAgICAgICAgIH1cclxuICAgICAgICB9XHJcblxyXG4gICAgICAgIHJldHVybiBLZXJuZWxJbnZvY2F0aW9uQ29udGV4dC5fY3VycmVudCE7XHJcbiAgICB9XHJcblxyXG4gICAgc3RhdGljIGdldCBjdXJyZW50KCk6IEtlcm5lbEludm9jYXRpb25Db250ZXh0IHwgbnVsbCB7IHJldHVybiB0aGlzLl9jdXJyZW50OyB9XHJcbiAgICBnZXQgY29tbWFuZCgpOiBjb250cmFjdHMuS2VybmVsQ29tbWFuZCB7IHJldHVybiB0aGlzLl9jb21tYW5kRW52ZWxvcGUuY29tbWFuZDsgfVxyXG4gICAgZ2V0IGNvbW1hbmRFbnZlbG9wZSgpOiBjb250cmFjdHMuS2VybmVsQ29tbWFuZEVudmVsb3BlIHsgcmV0dXJuIHRoaXMuX2NvbW1hbmRFbnZlbG9wZTsgfVxyXG4gICAgY29uc3RydWN0b3Ioa2VybmVsQ29tbWFuZEludm9jYXRpb246IGNvbnRyYWN0cy5LZXJuZWxDb21tYW5kRW52ZWxvcGUpIHtcclxuICAgICAgICB0aGlzLl9jb21tYW5kRW52ZWxvcGUgPSBrZXJuZWxDb21tYW5kSW52b2NhdGlvbjtcclxuICAgIH1cclxuXHJcbiAgICBjb21wbGV0ZShjb21tYW5kOiBjb250cmFjdHMuS2VybmVsQ29tbWFuZEVudmVsb3BlKSB7XHJcbiAgICAgICAgaWYgKGFyZUNvbW1hbmRzVGhlU2FtZShjb21tYW5kLCB0aGlzLl9jb21tYW5kRW52ZWxvcGUpKSB7XHJcbiAgICAgICAgICAgIHRoaXMuX2lzQ29tcGxldGUgPSB0cnVlO1xyXG4gICAgICAgICAgICBsZXQgc3VjY2VlZGVkOiBjb250cmFjdHMuQ29tbWFuZFN1Y2NlZWRlZCA9IHt9O1xyXG4gICAgICAgICAgICBsZXQgZXZlbnRFbnZlbG9wZTogY29udHJhY3RzLktlcm5lbEV2ZW50RW52ZWxvcGUgPSB7XHJcbiAgICAgICAgICAgICAgICBjb21tYW5kOiB0aGlzLl9jb21tYW5kRW52ZWxvcGUsXHJcbiAgICAgICAgICAgICAgICBldmVudFR5cGU6IGNvbnRyYWN0cy5Db21tYW5kU3VjY2VlZGVkVHlwZSxcclxuICAgICAgICAgICAgICAgIGV2ZW50OiBzdWNjZWVkZWRcclxuICAgICAgICAgICAgfTtcclxuICAgICAgICAgICAgdGhpcy5pbnRlcm5hbFB1Ymxpc2goZXZlbnRFbnZlbG9wZSk7XHJcbiAgICAgICAgICAgIHRoaXMuY29tcGxldGlvblNvdXJjZS5yZXNvbHZlKCk7XHJcbiAgICAgICAgICAgIC8vIFRPRE86IEMjIHZlcnNpb24gaGFzIGNvbXBsZXRpb24gY2FsbGJhY2tzIC0gZG8gd2UgbmVlZCB0aGVzZT9cclxuICAgICAgICAgICAgLy8gaWYgKCFfZXZlbnRzLklzRGlzcG9zZWQpXHJcbiAgICAgICAgICAgIC8vIHtcclxuICAgICAgICAgICAgLy8gICAgIF9ldmVudHMuT25Db21wbGV0ZWQoKTtcclxuICAgICAgICAgICAgLy8gfVxyXG5cclxuICAgICAgICB9XHJcbiAgICAgICAgZWxzZSB7XHJcbiAgICAgICAgICAgIGxldCBwb3MgPSB0aGlzLl9jaGlsZENvbW1hbmRzLmluZGV4T2YoY29tbWFuZCk7XHJcbiAgICAgICAgICAgIGRlbGV0ZSB0aGlzLl9jaGlsZENvbW1hbmRzW3Bvc107XHJcbiAgICAgICAgfVxyXG4gICAgfVxyXG5cclxuICAgIGZhaWwobWVzc2FnZT86IHN0cmluZykge1xyXG4gICAgICAgIC8vIFRPRE86XHJcbiAgICAgICAgLy8gVGhlIEMjIGNvZGUgYWNjZXB0cyBhIG1lc3NhZ2UgYW5kL29yIGFuIGV4Y2VwdGlvbi4gRG8gd2UgbmVlZCB0byBhZGQgc3VwcG9ydFxyXG4gICAgICAgIC8vIGZvciBleGNlcHRpb25zPyAoVGhlIFRTIENvbW1hbmRGYWlsZWQgaW50ZXJmYWNlIGRvZXNuJ3QgaGF2ZSBhIHBsYWNlIGZvciBpdCByaWdodCBub3cuKVxyXG4gICAgICAgIHRoaXMuX2lzQ29tcGxldGUgPSB0cnVlO1xyXG4gICAgICAgIGxldCBmYWlsZWQ6IGNvbnRyYWN0cy5Db21tYW5kRmFpbGVkID0geyBtZXNzYWdlOiBtZXNzYWdlID8/IFwiQ29tbWFuZCBGYWlsZWRcIiB9O1xyXG4gICAgICAgIGxldCBldmVudEVudmVsb3BlOiBjb250cmFjdHMuS2VybmVsRXZlbnRFbnZlbG9wZSA9IHtcclxuICAgICAgICAgICAgY29tbWFuZDogdGhpcy5fY29tbWFuZEVudmVsb3BlLFxyXG4gICAgICAgICAgICBldmVudFR5cGU6IGNvbnRyYWN0cy5Db21tYW5kRmFpbGVkVHlwZSxcclxuICAgICAgICAgICAgZXZlbnQ6IGZhaWxlZFxyXG4gICAgICAgIH07XHJcblxyXG4gICAgICAgIHRoaXMuaW50ZXJuYWxQdWJsaXNoKGV2ZW50RW52ZWxvcGUpO1xyXG4gICAgICAgIHRoaXMuY29tcGxldGlvblNvdXJjZS5yZXNvbHZlKCk7XHJcbiAgICB9XHJcblxyXG4gICAgcHVibGlzaChrZXJuZWxFdmVudDogY29udHJhY3RzLktlcm5lbEV2ZW50RW52ZWxvcGUpIHtcclxuICAgICAgICBpZiAoIXRoaXMuX2lzQ29tcGxldGUpIHtcclxuICAgICAgICAgICAgdGhpcy5pbnRlcm5hbFB1Ymxpc2goa2VybmVsRXZlbnQpO1xyXG4gICAgICAgIH1cclxuICAgIH1cclxuXHJcbiAgICBwcml2YXRlIGludGVybmFsUHVibGlzaChrZXJuZWxFdmVudDogY29udHJhY3RzLktlcm5lbEV2ZW50RW52ZWxvcGUpIHtcclxuICAgICAgICBpZiAoIWtlcm5lbEV2ZW50LmNvbW1hbmQpIHtcclxuICAgICAgICAgICAga2VybmVsRXZlbnQuY29tbWFuZCA9IHRoaXMuX2NvbW1hbmRFbnZlbG9wZTtcclxuICAgICAgICB9XHJcblxyXG4gICAgICAgIGxldCBjb21tYW5kID0ga2VybmVsRXZlbnQuY29tbWFuZDtcclxuXHJcbiAgICAgICAgaWYgKHRoaXMuaGFuZGxpbmdLZXJuZWwpIHtcclxuICAgICAgICAgICAgY29uc3Qga2VybmVsVXJpID0gZ2V0S2VybmVsVXJpKHRoaXMuaGFuZGxpbmdLZXJuZWwpO1xyXG4gICAgICAgICAgICBpZiAoIXJvdXRpbmdzbGlwLmV2ZW50Um91dGluZ1NsaXBDb250YWlucyhrZXJuZWxFdmVudCwga2VybmVsVXJpKSkge1xyXG4gICAgICAgICAgICAgICAgcm91dGluZ3NsaXAuc3RhbXBFdmVudFJvdXRpbmdTbGlwKGtlcm5lbEV2ZW50LCBrZXJuZWxVcmkpO1xyXG4gICAgICAgICAgICAgICAga2VybmVsRXZlbnQucm91dGluZ1NsaXA7Ly8/XHJcbiAgICAgICAgICAgIH0gZWxzZSB7XHJcbiAgICAgICAgICAgICAgICBcInNob3VsZCBub3QgYmUgaGVyZVwiOy8vP1xyXG4gICAgICAgICAgICB9XHJcblxyXG4gICAgICAgIH0gZWxzZSB7XHJcbiAgICAgICAgICAgIGtlcm5lbEV2ZW50Oy8vP1xyXG4gICAgICAgIH1cclxuICAgICAgICB0aGlzLl9jb21tYW5kRW52ZWxvcGU7Ly8/XHJcbiAgICAgICAgaWYgKGNvbW1hbmQgPT09IG51bGwgfHxcclxuICAgICAgICAgICAgY29tbWFuZCA9PT0gdW5kZWZpbmVkIHx8XHJcbiAgICAgICAgICAgIGFyZUNvbW1hbmRzVGhlU2FtZShjb21tYW5kISwgdGhpcy5fY29tbWFuZEVudmVsb3BlKSB8fFxyXG4gICAgICAgICAgICB0aGlzLl9jaGlsZENvbW1hbmRzLmluY2x1ZGVzKGNvbW1hbmQhKSkge1xyXG4gICAgICAgICAgICB0aGlzLl9ldmVudFN1YmplY3QubmV4dChrZXJuZWxFdmVudCk7XHJcbiAgICAgICAgfVxyXG4gICAgfVxyXG5cclxuICAgIGlzUGFyZW50T2ZDb21tYW5kKGNvbW1hbmRFbnZlbG9wZTogY29udHJhY3RzLktlcm5lbENvbW1hbmRFbnZlbG9wZSk6IGJvb2xlYW4ge1xyXG4gICAgICAgIGNvbnN0IGNoaWxkRm91bmQgPSB0aGlzLl9jaGlsZENvbW1hbmRzLmluY2x1ZGVzKGNvbW1hbmRFbnZlbG9wZSk7XHJcbiAgICAgICAgcmV0dXJuIGNoaWxkRm91bmQ7XHJcbiAgICB9XHJcblxyXG4gICAgZGlzcG9zZSgpIHtcclxuICAgICAgICBpZiAoIXRoaXMuX2lzQ29tcGxldGUpIHtcclxuICAgICAgICAgICAgdGhpcy5jb21wbGV0ZSh0aGlzLl9jb21tYW5kRW52ZWxvcGUpO1xyXG4gICAgICAgIH1cclxuICAgICAgICBLZXJuZWxJbnZvY2F0aW9uQ29udGV4dC5fY3VycmVudCA9IG51bGw7XHJcbiAgICB9XHJcbn1cclxuXHJcbmV4cG9ydCBmdW5jdGlvbiBhcmVDb21tYW5kc1RoZVNhbWUoZW52ZWxvcGUxOiBjb250cmFjdHMuS2VybmVsQ29tbWFuZEVudmVsb3BlLCBlbnZlbG9wZTI6IGNvbnRyYWN0cy5LZXJuZWxDb21tYW5kRW52ZWxvcGUpOiBib29sZWFuIHtcclxuICAgIGVudmVsb3BlMTsvLz9cclxuICAgIGVudmVsb3BlMjsvLz9cclxuICAgIGVudmVsb3BlMSA9PT0gZW52ZWxvcGUyOy8vP1xyXG4gICAgaWYgKGVudmVsb3BlMSA9PT0gZW52ZWxvcGUyKSB7XHJcbiAgICAgICAgcmV0dXJuIHRydWU7XHJcbiAgICB9XHJcblxyXG4gICAgY29uc3Qgc2FtZUNvbW1hbmRUeXBlID0gZW52ZWxvcGUxPy5jb21tYW5kVHlwZSA9PT0gZW52ZWxvcGUyPy5jb21tYW5kVHlwZTsgLy8/XHJcbiAgICBjb25zdCBzYW1lVG9rZW4gPSBlbnZlbG9wZTE/LnRva2VuID09PSBlbnZlbG9wZTI/LnRva2VuOyAvLz9cclxuICAgIGNvbnN0IHNhbWVDb21tYW5kSWQgPSBlbnZlbG9wZTE/LmlkID09PSBlbnZlbG9wZTI/LmlkOyAvLz9cclxuICAgIGlmIChzYW1lQ29tbWFuZFR5cGUgJiYgc2FtZVRva2VuICYmIHNhbWVDb21tYW5kSWQpIHtcclxuICAgICAgICByZXR1cm4gdHJ1ZTtcclxuICAgIH1cclxuICAgIHJldHVybiBmYWxzZTtcclxufVxyXG4iLCIvLyBDb3B5cmlnaHQgKGMpIC5ORVQgRm91bmRhdGlvbiBhbmQgY29udHJpYnV0b3JzLiBBbGwgcmlnaHRzIHJlc2VydmVkLlxyXG4vLyBMaWNlbnNlZCB1bmRlciB0aGUgTUlUIGxpY2Vuc2UuIFNlZSBMSUNFTlNFIGZpbGUgaW4gdGhlIHByb2plY3Qgcm9vdCBmb3IgZnVsbCBsaWNlbnNlIGluZm9ybWF0aW9uLlxyXG5cclxuaW1wb3J0IHsgS2VybmVsQ29tbWFuZEVudmVsb3BlIH0gZnJvbSBcIi4vY29udHJhY3RzXCI7XHJcblxyXG5leHBvcnQgY2xhc3MgR3VpZCB7XHJcblxyXG4gICAgcHVibGljIHN0YXRpYyB2YWxpZGF0b3IgPSBuZXcgUmVnRXhwKFwiXlthLXowLTldezh9LVthLXowLTldezR9LVthLXowLTldezR9LVthLXowLTldezR9LVthLXowLTldezEyfSRcIiwgXCJpXCIpO1xyXG5cclxuICAgIHB1YmxpYyBzdGF0aWMgRU1QVFkgPSBcIjAwMDAwMDAwLTAwMDAtMDAwMC0wMDAwLTAwMDAwMDAwMDAwMFwiO1xyXG5cclxuICAgIHB1YmxpYyBzdGF0aWMgaXNHdWlkKGd1aWQ6IGFueSkge1xyXG4gICAgICAgIGNvbnN0IHZhbHVlOiBzdHJpbmcgPSBndWlkLnRvU3RyaW5nKCk7XHJcbiAgICAgICAgcmV0dXJuIGd1aWQgJiYgKGd1aWQgaW5zdGFuY2VvZiBHdWlkIHx8IEd1aWQudmFsaWRhdG9yLnRlc3QodmFsdWUpKTtcclxuICAgIH1cclxuXHJcbiAgICBwdWJsaWMgc3RhdGljIGNyZWF0ZSgpOiBHdWlkIHtcclxuICAgICAgICByZXR1cm4gbmV3IEd1aWQoW0d1aWQuZ2VuKDIpLCBHdWlkLmdlbigxKSwgR3VpZC5nZW4oMSksIEd1aWQuZ2VuKDEpLCBHdWlkLmdlbigzKV0uam9pbihcIi1cIikpO1xyXG4gICAgfVxyXG5cclxuICAgIHB1YmxpYyBzdGF0aWMgY3JlYXRlRW1wdHkoKTogR3VpZCB7XHJcbiAgICAgICAgcmV0dXJuIG5ldyBHdWlkKFwiZW1wdHlndWlkXCIpO1xyXG4gICAgfVxyXG5cclxuICAgIHB1YmxpYyBzdGF0aWMgcGFyc2UoZ3VpZDogc3RyaW5nKTogR3VpZCB7XHJcbiAgICAgICAgcmV0dXJuIG5ldyBHdWlkKGd1aWQpO1xyXG4gICAgfVxyXG5cclxuICAgIHB1YmxpYyBzdGF0aWMgcmF3KCk6IHN0cmluZyB7XHJcbiAgICAgICAgcmV0dXJuIFtHdWlkLmdlbigyKSwgR3VpZC5nZW4oMSksIEd1aWQuZ2VuKDEpLCBHdWlkLmdlbigxKSwgR3VpZC5nZW4oMyldLmpvaW4oXCItXCIpO1xyXG4gICAgfVxyXG5cclxuICAgIHByaXZhdGUgc3RhdGljIGdlbihjb3VudDogbnVtYmVyKSB7XHJcbiAgICAgICAgbGV0IG91dDogc3RyaW5nID0gXCJcIjtcclxuICAgICAgICBmb3IgKGxldCBpOiBudW1iZXIgPSAwOyBpIDwgY291bnQ7IGkrKykge1xyXG4gICAgICAgICAgICAvLyB0c2xpbnQ6ZGlzYWJsZS1uZXh0LWxpbmU6bm8tYml0d2lzZVxyXG4gICAgICAgICAgICBvdXQgKz0gKCgoMSArIE1hdGgucmFuZG9tKCkpICogMHgxMDAwMCkgfCAwKS50b1N0cmluZygxNikuc3Vic3RyaW5nKDEpO1xyXG4gICAgICAgIH1cclxuICAgICAgICByZXR1cm4gb3V0O1xyXG4gICAgfVxyXG5cclxuICAgIHByaXZhdGUgdmFsdWU6IHN0cmluZztcclxuXHJcbiAgICBwcml2YXRlIGNvbnN0cnVjdG9yKGd1aWQ6IHN0cmluZykge1xyXG4gICAgICAgIGlmICghZ3VpZCkgeyB0aHJvdyBuZXcgVHlwZUVycm9yKFwiSW52YWxpZCBhcmd1bWVudDsgYHZhbHVlYCBoYXMgbm8gdmFsdWUuXCIpOyB9XHJcblxyXG4gICAgICAgIHRoaXMudmFsdWUgPSBHdWlkLkVNUFRZO1xyXG5cclxuICAgICAgICBpZiAoZ3VpZCAmJiBHdWlkLmlzR3VpZChndWlkKSkge1xyXG4gICAgICAgICAgICB0aGlzLnZhbHVlID0gZ3VpZDtcclxuICAgICAgICB9XHJcbiAgICB9XHJcblxyXG4gICAgcHVibGljIGVxdWFscyhvdGhlcjogR3VpZCk6IGJvb2xlYW4ge1xyXG4gICAgICAgIC8vIENvbXBhcmluZyBzdHJpbmcgYHZhbHVlYCBhZ2FpbnN0IHByb3ZpZGVkIGBndWlkYCB3aWxsIGF1dG8tY2FsbFxyXG4gICAgICAgIC8vIHRvU3RyaW5nIG9uIGBndWlkYCBmb3IgY29tcGFyaXNvblxyXG4gICAgICAgIHJldHVybiBHdWlkLmlzR3VpZChvdGhlcikgJiYgdGhpcy52YWx1ZSA9PT0gb3RoZXIudG9TdHJpbmcoKTtcclxuICAgIH1cclxuXHJcbiAgICBwdWJsaWMgaXNFbXB0eSgpOiBib29sZWFuIHtcclxuICAgICAgICByZXR1cm4gdGhpcy52YWx1ZSA9PT0gR3VpZC5FTVBUWTtcclxuICAgIH1cclxuXHJcbiAgICBwdWJsaWMgdG9TdHJpbmcoKTogc3RyaW5nIHtcclxuICAgICAgICByZXR1cm4gdGhpcy52YWx1ZTtcclxuICAgIH1cclxuXHJcbiAgICBwdWJsaWMgdG9KU09OKCk6IGFueSB7XHJcbiAgICAgICAgcmV0dXJuIHtcclxuICAgICAgICAgICAgdmFsdWU6IHRoaXMudmFsdWUsXHJcbiAgICAgICAgfTtcclxuICAgIH1cclxufVxyXG5cclxuZnVuY3Rpb24gc2V0VG9rZW4oY29tbWFuZEVudmVsb3BlOiBLZXJuZWxDb21tYW5kRW52ZWxvcGUpIHtcclxuICAgIGlmICghY29tbWFuZEVudmVsb3BlLnRva2VuKSB7XHJcbiAgICAgICAgY29tbWFuZEVudmVsb3BlLnRva2VuID0gR3VpZC5jcmVhdGUoKS50b1N0cmluZygpO1xyXG4gICAgfVxyXG5cclxuICAgIC8vXHJcbn1cclxuXHJcbmV4cG9ydCBjbGFzcyBUb2tlbkdlbmVyYXRvciB7XHJcbiAgICBwcml2YXRlIF9zZWVkOiBzdHJpbmc7XHJcbiAgICBwcml2YXRlIF9jb3VudGVyOiBudW1iZXI7XHJcblxyXG4gICAgY29uc3RydWN0b3IoKSB7XHJcbiAgICAgICAgdGhpcy5fc2VlZCA9IEd1aWQuY3JlYXRlKCkudG9TdHJpbmcoKTtcclxuICAgICAgICB0aGlzLl9jb3VudGVyID0gMDtcclxuICAgIH1cclxuXHJcbiAgICBwdWJsaWMgR2V0TmV3VG9rZW4oKTogc3RyaW5nIHtcclxuICAgICAgICB0aGlzLl9jb3VudGVyKys7XHJcbiAgICAgICAgcmV0dXJuIGAke3RoaXMuX3NlZWR9Ojoke3RoaXMuX2NvdW50ZXJ9YDtcclxuICAgIH1cclxufVxyXG4iLCIvLyBDb3B5cmlnaHQgKGMpIC5ORVQgRm91bmRhdGlvbiBhbmQgY29udHJpYnV0b3JzLiBBbGwgcmlnaHRzIHJlc2VydmVkLlxyXG4vLyBMaWNlbnNlZCB1bmRlciB0aGUgTUlUIGxpY2Vuc2UuIFNlZSBMSUNFTlNFIGZpbGUgaW4gdGhlIHByb2plY3Qgcm9vdCBmb3IgZnVsbCBsaWNlbnNlIGluZm9ybWF0aW9uLlxyXG5cclxuZXhwb3J0IGVudW0gTG9nTGV2ZWwge1xyXG4gICAgSW5mbyA9IDAsXHJcbiAgICBXYXJuID0gMSxcclxuICAgIEVycm9yID0gMixcclxuICAgIE5vbmUgPSAzLFxyXG59XHJcblxyXG5leHBvcnQgdHlwZSBMb2dFbnRyeSA9IHtcclxuICAgIGxvZ0xldmVsOiBMb2dMZXZlbDtcclxuICAgIHNvdXJjZTogc3RyaW5nO1xyXG4gICAgbWVzc2FnZTogc3RyaW5nO1xyXG59O1xyXG5cclxuZXhwb3J0IGNsYXNzIExvZ2dlciB7XHJcblxyXG4gICAgcHJpdmF0ZSBzdGF0aWMgX2RlZmF1bHQ6IExvZ2dlciA9IG5ldyBMb2dnZXIoJ2RlZmF1bHQnLCAoX2VudHJ5OiBMb2dFbnRyeSkgPT4geyB9KTtcclxuXHJcbiAgICBwcml2YXRlIGNvbnN0cnVjdG9yKHByaXZhdGUgcmVhZG9ubHkgc291cmNlOiBzdHJpbmcsIHJlYWRvbmx5IHdyaXRlOiAoZW50cnk6IExvZ0VudHJ5KSA9PiB2b2lkKSB7XHJcbiAgICB9XHJcblxyXG4gICAgcHVibGljIGluZm8obWVzc2FnZTogc3RyaW5nKTogdm9pZCB7XHJcbiAgICAgICAgdGhpcy53cml0ZSh7IGxvZ0xldmVsOiBMb2dMZXZlbC5JbmZvLCBzb3VyY2U6IHRoaXMuc291cmNlLCBtZXNzYWdlIH0pO1xyXG4gICAgfVxyXG5cclxuICAgIHB1YmxpYyB3YXJuKG1lc3NhZ2U6IHN0cmluZyk6IHZvaWQge1xyXG4gICAgICAgIHRoaXMud3JpdGUoeyBsb2dMZXZlbDogTG9nTGV2ZWwuV2Fybiwgc291cmNlOiB0aGlzLnNvdXJjZSwgbWVzc2FnZSB9KTtcclxuICAgIH1cclxuXHJcbiAgICBwdWJsaWMgZXJyb3IobWVzc2FnZTogc3RyaW5nKTogdm9pZCB7XHJcbiAgICAgICAgdGhpcy53cml0ZSh7IGxvZ0xldmVsOiBMb2dMZXZlbC5FcnJvciwgc291cmNlOiB0aGlzLnNvdXJjZSwgbWVzc2FnZSB9KTtcclxuICAgIH1cclxuXHJcbiAgICBwdWJsaWMgc3RhdGljIGNvbmZpZ3VyZShzb3VyY2U6IHN0cmluZywgd3JpdGVyOiAoZW50cnk6IExvZ0VudHJ5KSA9PiB2b2lkKSB7XHJcbiAgICAgICAgY29uc3QgbG9nZ2VyID0gbmV3IExvZ2dlcihzb3VyY2UsIHdyaXRlcik7XHJcbiAgICAgICAgTG9nZ2VyLl9kZWZhdWx0ID0gbG9nZ2VyO1xyXG4gICAgfVxyXG5cclxuICAgIHB1YmxpYyBzdGF0aWMgZ2V0IGRlZmF1bHQoKTogTG9nZ2VyIHtcclxuICAgICAgICBpZiAoTG9nZ2VyLl9kZWZhdWx0KSB7XHJcbiAgICAgICAgICAgIHJldHVybiBMb2dnZXIuX2RlZmF1bHQ7XHJcbiAgICAgICAgfVxyXG5cclxuICAgICAgICB0aHJvdyBuZXcgRXJyb3IoJ05vIGxvZ2dlciBoYXMgYmVlbiBjb25maWd1cmVkIGZvciB0aGlzIGNvbnRleHQnKTtcclxuICAgIH1cclxufVxyXG4iLCIvLyBDb3B5cmlnaHQgKGMpIC5ORVQgRm91bmRhdGlvbiBhbmQgY29udHJpYnV0b3JzLiBBbGwgcmlnaHRzIHJlc2VydmVkLlxyXG4vLyBMaWNlbnNlZCB1bmRlciB0aGUgTUlUIGxpY2Vuc2UuIFNlZSBMSUNFTlNFIGZpbGUgaW4gdGhlIHByb2plY3Qgcm9vdCBmb3IgZnVsbCBsaWNlbnNlIGluZm9ybWF0aW9uLlxyXG5cclxuaW1wb3J0IHsgTG9nZ2VyIH0gZnJvbSBcIi4vbG9nZ2VyXCI7XHJcbmltcG9ydCB7IFByb21pc2VDb21wbGV0aW9uU291cmNlIH0gZnJvbSBcIi4vcHJvbWlzZUNvbXBsZXRpb25Tb3VyY2VcIjtcclxuXHJcbmludGVyZmFjZSBTY2hlZHVsZXJPcGVyYXRpb248VD4ge1xyXG4gICAgdmFsdWU6IFQ7XHJcbiAgICBleGVjdXRvcjogKHZhbHVlOiBUKSA9PiBQcm9taXNlPHZvaWQ+O1xyXG4gICAgcHJvbWlzZUNvbXBsZXRpb25Tb3VyY2U6IFByb21pc2VDb21wbGV0aW9uU291cmNlPHZvaWQ+O1xyXG59XHJcbmV4cG9ydCBjbGFzcyBLZXJuZWxTY2hlZHVsZXI8VD4ge1xyXG4gICAgcHJpdmF0ZSBfb3BlcmF0aW9uUXVldWU6IEFycmF5PFNjaGVkdWxlck9wZXJhdGlvbjxUPj4gPSBbXTtcclxuICAgIHByaXZhdGUgX2luRmxpZ2h0T3BlcmF0aW9uPzogU2NoZWR1bGVyT3BlcmF0aW9uPFQ+O1xyXG5cclxuICAgIGNvbnN0cnVjdG9yKCkge1xyXG4gICAgfVxyXG5cclxuICAgIHB1YmxpYyBjYW5jZWxDdXJyZW50T3BlcmF0aW9uKCk6IHZvaWQge1xyXG4gICAgICAgIHRoaXMuX2luRmxpZ2h0T3BlcmF0aW9uPy5wcm9taXNlQ29tcGxldGlvblNvdXJjZS5yZWplY3QobmV3IEVycm9yKFwiT3BlcmF0aW9uIGNhbmNlbGxlZFwiKSk7XHJcbiAgICB9XHJcblxyXG4gICAgcnVuQXN5bmModmFsdWU6IFQsIGV4ZWN1dG9yOiAodmFsdWU6IFQpID0+IFByb21pc2U8dm9pZD4pOiBQcm9taXNlPHZvaWQ+IHtcclxuICAgICAgICBjb25zdCBvcGVyYXRpb24gPSB7XHJcbiAgICAgICAgICAgIHZhbHVlLFxyXG4gICAgICAgICAgICBleGVjdXRvcixcclxuICAgICAgICAgICAgcHJvbWlzZUNvbXBsZXRpb25Tb3VyY2U6IG5ldyBQcm9taXNlQ29tcGxldGlvblNvdXJjZTx2b2lkPigpLFxyXG4gICAgICAgIH07XHJcblxyXG4gICAgICAgIGlmICh0aGlzLl9pbkZsaWdodE9wZXJhdGlvbikge1xyXG4gICAgICAgICAgICBMb2dnZXIuZGVmYXVsdC5pbmZvKGBrZXJuZWxTY2hlZHVsZXI6IHN0YXJ0aW5nIGltbWVkaWF0ZSBleGVjdXRpb24gb2YgJHtKU09OLnN0cmluZ2lmeShvcGVyYXRpb24udmFsdWUpfWApO1xyXG5cclxuICAgICAgICAgICAgLy8gaW52b2tlIGltbWVkaWF0ZWx5XHJcbiAgICAgICAgICAgIHJldHVybiBvcGVyYXRpb24uZXhlY3V0b3Iob3BlcmF0aW9uLnZhbHVlKVxyXG4gICAgICAgICAgICAgICAgLnRoZW4oKCkgPT4ge1xyXG4gICAgICAgICAgICAgICAgICAgIExvZ2dlci5kZWZhdWx0LmluZm8oYGtlcm5lbFNjaGVkdWxlcjogaW1tZWRpYXRlIGV4ZWN1dGlvbiBjb21wbGV0ZWQ6ICR7SlNPTi5zdHJpbmdpZnkob3BlcmF0aW9uLnZhbHVlKX1gKTtcclxuICAgICAgICAgICAgICAgICAgICBvcGVyYXRpb24ucHJvbWlzZUNvbXBsZXRpb25Tb3VyY2UucmVzb2x2ZSgpO1xyXG4gICAgICAgICAgICAgICAgfSlcclxuICAgICAgICAgICAgICAgIC5jYXRjaChlID0+IHtcclxuICAgICAgICAgICAgICAgICAgICBMb2dnZXIuZGVmYXVsdC5pbmZvKGBrZXJuZWxTY2hlZHVsZXI6IGltbWVkaWF0ZSBleGVjdXRpb24gZmFpbGVkOiAke0pTT04uc3RyaW5naWZ5KGUpfSAtICR7SlNPTi5zdHJpbmdpZnkob3BlcmF0aW9uLnZhbHVlKX1gKTtcclxuICAgICAgICAgICAgICAgICAgICBvcGVyYXRpb24ucHJvbWlzZUNvbXBsZXRpb25Tb3VyY2UucmVqZWN0KGUpO1xyXG4gICAgICAgICAgICAgICAgfSk7XHJcbiAgICAgICAgfVxyXG5cclxuICAgICAgICBMb2dnZXIuZGVmYXVsdC5pbmZvKGBrZXJuZWxTY2hlZHVsZXI6IHNjaGVkdWxpbmcgZXhlY3V0aW9uIG9mICR7SlNPTi5zdHJpbmdpZnkob3BlcmF0aW9uLnZhbHVlKX1gKTtcclxuICAgICAgICB0aGlzLl9vcGVyYXRpb25RdWV1ZS5wdXNoKG9wZXJhdGlvbik7XHJcbiAgICAgICAgaWYgKHRoaXMuX29wZXJhdGlvblF1ZXVlLmxlbmd0aCA9PT0gMSkge1xyXG4gICAgICAgICAgICB0aGlzLmV4ZWN1dGVOZXh0Q29tbWFuZCgpO1xyXG4gICAgICAgIH1cclxuXHJcbiAgICAgICAgcmV0dXJuIG9wZXJhdGlvbi5wcm9taXNlQ29tcGxldGlvblNvdXJjZS5wcm9taXNlO1xyXG4gICAgfVxyXG5cclxuICAgIHByaXZhdGUgZXhlY3V0ZU5leHRDb21tYW5kKCk6IHZvaWQge1xyXG4gICAgICAgIGNvbnN0IG5leHRPcGVyYXRpb24gPSB0aGlzLl9vcGVyYXRpb25RdWV1ZS5sZW5ndGggPiAwID8gdGhpcy5fb3BlcmF0aW9uUXVldWVbMF0gOiB1bmRlZmluZWQ7XHJcbiAgICAgICAgaWYgKG5leHRPcGVyYXRpb24pIHtcclxuICAgICAgICAgICAgdGhpcy5faW5GbGlnaHRPcGVyYXRpb24gPSBuZXh0T3BlcmF0aW9uO1xyXG4gICAgICAgICAgICBMb2dnZXIuZGVmYXVsdC5pbmZvKGBrZXJuZWxTY2hlZHVsZXI6IHN0YXJ0aW5nIHNjaGVkdWxlZCBleGVjdXRpb24gb2YgJHtKU09OLnN0cmluZ2lmeShuZXh0T3BlcmF0aW9uLnZhbHVlKX1gKTtcclxuICAgICAgICAgICAgbmV4dE9wZXJhdGlvbi5leGVjdXRvcihuZXh0T3BlcmF0aW9uLnZhbHVlKVxyXG4gICAgICAgICAgICAgICAgLnRoZW4oKCkgPT4ge1xyXG4gICAgICAgICAgICAgICAgICAgIHRoaXMuX2luRmxpZ2h0T3BlcmF0aW9uID0gdW5kZWZpbmVkO1xyXG4gICAgICAgICAgICAgICAgICAgIExvZ2dlci5kZWZhdWx0LmluZm8oYGtlcm5lbFNjaGVkdWxlcjogY29tcGxldGluZyBpbmZsaWdodCBvcGVyYXRpb246IHN1Y2Nlc3MgJHtKU09OLnN0cmluZ2lmeShuZXh0T3BlcmF0aW9uLnZhbHVlKX1gKTtcclxuICAgICAgICAgICAgICAgICAgICBuZXh0T3BlcmF0aW9uLnByb21pc2VDb21wbGV0aW9uU291cmNlLnJlc29sdmUoKTtcclxuICAgICAgICAgICAgICAgIH0pXHJcbiAgICAgICAgICAgICAgICAuY2F0Y2goZSA9PiB7XHJcbiAgICAgICAgICAgICAgICAgICAgdGhpcy5faW5GbGlnaHRPcGVyYXRpb24gPSB1bmRlZmluZWQ7XHJcbiAgICAgICAgICAgICAgICAgICAgTG9nZ2VyLmRlZmF1bHQuaW5mbyhga2VybmVsU2NoZWR1bGVyOiBjb21wbGV0aW5nIGluZmxpZ2h0IG9wZXJhdGlvbjogZmFpbHVyZSAke0pTT04uc3RyaW5naWZ5KGUpfSAtICR7SlNPTi5zdHJpbmdpZnkobmV4dE9wZXJhdGlvbi52YWx1ZSl9YCk7XHJcbiAgICAgICAgICAgICAgICAgICAgbmV4dE9wZXJhdGlvbi5wcm9taXNlQ29tcGxldGlvblNvdXJjZS5yZWplY3QoZSk7XHJcbiAgICAgICAgICAgICAgICB9KVxyXG4gICAgICAgICAgICAgICAgLmZpbmFsbHkoKCkgPT4ge1xyXG4gICAgICAgICAgICAgICAgICAgIHRoaXMuX29wZXJhdGlvblF1ZXVlLnNoaWZ0KCk7XHJcbiAgICAgICAgICAgICAgICAgICAgdGhpcy5leGVjdXRlTmV4dENvbW1hbmQoKTtcclxuICAgICAgICAgICAgICAgIH0pO1xyXG4gICAgICAgIH1cclxuICAgIH1cclxufVxyXG4iLCIvLyBDb3B5cmlnaHQgKGMpIC5ORVQgRm91bmRhdGlvbiBhbmQgY29udHJpYnV0b3JzLiBBbGwgcmlnaHRzIHJlc2VydmVkLlxyXG4vLyBMaWNlbnNlZCB1bmRlciB0aGUgTUlUIGxpY2Vuc2UuIFNlZSBMSUNFTlNFIGZpbGUgaW4gdGhlIHByb2plY3Qgcm9vdCBmb3IgZnVsbCBsaWNlbnNlIGluZm9ybWF0aW9uLlxyXG5cclxuaW1wb3J0IHsgS2VybmVsSW52b2NhdGlvbkNvbnRleHQsIGFyZUNvbW1hbmRzVGhlU2FtZSB9IGZyb20gXCIuL2tlcm5lbEludm9jYXRpb25Db250ZXh0XCI7XHJcbmltcG9ydCB7IFRva2VuR2VuZXJhdG9yLCBHdWlkIH0gZnJvbSBcIi4vdG9rZW5HZW5lcmF0b3JcIjtcclxuaW1wb3J0ICogYXMgY29udHJhY3RzIGZyb20gXCIuL2NvbnRyYWN0c1wiO1xyXG5pbXBvcnQgeyBMb2dnZXIgfSBmcm9tIFwiLi9sb2dnZXJcIjtcclxuaW1wb3J0IHsgQ29tcG9zaXRlS2VybmVsIH0gZnJvbSBcIi4vY29tcG9zaXRlS2VybmVsXCI7XHJcbmltcG9ydCB7IEtlcm5lbFNjaGVkdWxlciB9IGZyb20gXCIuL2tlcm5lbFNjaGVkdWxlclwiO1xyXG5pbXBvcnQgeyBQcm9taXNlQ29tcGxldGlvblNvdXJjZSB9IGZyb20gXCIuL3Byb21pc2VDb21wbGV0aW9uU291cmNlXCI7XHJcbmltcG9ydCAqIGFzIGRpc3Bvc2FibGVzIGZyb20gXCIuL2Rpc3Bvc2FibGVzXCI7XHJcbmltcG9ydCAqIGFzIHJvdXRpbmdzbGlwIGZyb20gXCIuL3JvdXRpbmdzbGlwXCI7XHJcbmltcG9ydCAqIGFzIHJ4anMgZnJvbSBcInJ4anNcIjtcclxuXHJcbmV4cG9ydCBpbnRlcmZhY2UgSUtlcm5lbENvbW1hbmRJbnZvY2F0aW9uIHtcclxuICAgIGNvbW1hbmRFbnZlbG9wZTogY29udHJhY3RzLktlcm5lbENvbW1hbmRFbnZlbG9wZTtcclxuICAgIGNvbnRleHQ6IEtlcm5lbEludm9jYXRpb25Db250ZXh0O1xyXG59XHJcblxyXG5leHBvcnQgaW50ZXJmYWNlIElLZXJuZWxDb21tYW5kSGFuZGxlciB7XHJcbiAgICBjb21tYW5kVHlwZTogc3RyaW5nO1xyXG4gICAgaGFuZGxlOiAoY29tbWFuZEludm9jYXRpb246IElLZXJuZWxDb21tYW5kSW52b2NhdGlvbikgPT4gUHJvbWlzZTx2b2lkPjtcclxufVxyXG5cclxuZXhwb3J0IGludGVyZmFjZSBJS2VybmVsRXZlbnRPYnNlcnZlciB7XHJcbiAgICAoa2VybmVsRXZlbnQ6IGNvbnRyYWN0cy5LZXJuZWxFdmVudEVudmVsb3BlKTogdm9pZDtcclxufVxyXG5cclxuZXhwb3J0IGVudW0gS2VybmVsVHlwZSB7XHJcbiAgICBjb21wb3NpdGUsXHJcbiAgICBwcm94eSxcclxuICAgIGRlZmF1bHRcclxufTtcclxuXHJcbmV4cG9ydCBjbGFzcyBLZXJuZWwge1xyXG4gICAgcHJpdmF0ZSBfa2VybmVsSW5mbzogY29udHJhY3RzLktlcm5lbEluZm87XHJcbiAgICBwcml2YXRlIF9jb21tYW5kSGFuZGxlcnMgPSBuZXcgTWFwPHN0cmluZywgSUtlcm5lbENvbW1hbmRIYW5kbGVyPigpO1xyXG4gICAgcHJpdmF0ZSBfZXZlbnRTdWJqZWN0ID0gbmV3IHJ4anMuU3ViamVjdDxjb250cmFjdHMuS2VybmVsRXZlbnRFbnZlbG9wZT4oKTtcclxuICAgIHByaXZhdGUgcmVhZG9ubHkgX3Rva2VuR2VuZXJhdG9yOiBUb2tlbkdlbmVyYXRvciA9IG5ldyBUb2tlbkdlbmVyYXRvcigpO1xyXG4gICAgcHVibGljIHJvb3RLZXJuZWw6IEtlcm5lbCA9IHRoaXM7XHJcbiAgICBwdWJsaWMgcGFyZW50S2VybmVsOiBDb21wb3NpdGVLZXJuZWwgfCBudWxsID0gbnVsbDtcclxuICAgIHByaXZhdGUgX3NjaGVkdWxlcj86IEtlcm5lbFNjaGVkdWxlcjxjb250cmFjdHMuS2VybmVsQ29tbWFuZEVudmVsb3BlPiB8IG51bGwgPSBudWxsO1xyXG4gICAgcHJpdmF0ZSBfa2VybmVsVHlwZTogS2VybmVsVHlwZSA9IEtlcm5lbFR5cGUuZGVmYXVsdDtcclxuXHJcbiAgICBwdWJsaWMgZ2V0IGtlcm5lbEluZm8oKTogY29udHJhY3RzLktlcm5lbEluZm8ge1xyXG5cclxuICAgICAgICByZXR1cm4gdGhpcy5fa2VybmVsSW5mbztcclxuICAgIH1cclxuXHJcbiAgICBwdWJsaWMgZ2V0IGtlcm5lbFR5cGUoKTogS2VybmVsVHlwZSB7XHJcbiAgICAgICAgcmV0dXJuIHRoaXMuX2tlcm5lbFR5cGU7XHJcbiAgICB9XHJcblxyXG4gICAgcHJvdGVjdGVkIHNldCBrZXJuZWxUeXBlKHZhbHVlOiBLZXJuZWxUeXBlKSB7XHJcbiAgICAgICAgdGhpcy5fa2VybmVsVHlwZSA9IHZhbHVlO1xyXG4gICAgfVxyXG5cclxuICAgIHB1YmxpYyBnZXQga2VybmVsRXZlbnRzKCk6IHJ4anMuT2JzZXJ2YWJsZTxjb250cmFjdHMuS2VybmVsRXZlbnRFbnZlbG9wZT4ge1xyXG4gICAgICAgIHJldHVybiB0aGlzLl9ldmVudFN1YmplY3QuYXNPYnNlcnZhYmxlKCk7XHJcbiAgICB9XHJcblxyXG4gICAgY29uc3RydWN0b3IocmVhZG9ubHkgbmFtZTogc3RyaW5nLCBsYW5ndWFnZU5hbWU/OiBzdHJpbmcsIGxhbmd1YWdlVmVyc2lvbj86IHN0cmluZywgZGlzcGxheU5hbWU/OiBzdHJpbmcpIHtcclxuICAgICAgICB0aGlzLl9rZXJuZWxJbmZvID0ge1xyXG4gICAgICAgICAgICBsb2NhbE5hbWU6IG5hbWUsXHJcbiAgICAgICAgICAgIGxhbmd1YWdlTmFtZTogbGFuZ3VhZ2VOYW1lLFxyXG4gICAgICAgICAgICBhbGlhc2VzOiBbXSxcclxuICAgICAgICAgICAgdXJpOiByb3V0aW5nc2xpcC5jcmVhdGVLZXJuZWxVcmkoYGtlcm5lbDovL2xvY2FsLyR7bmFtZX1gKSxcclxuICAgICAgICAgICAgbGFuZ3VhZ2VWZXJzaW9uOiBsYW5ndWFnZVZlcnNpb24sXHJcbiAgICAgICAgICAgIGRpc3BsYXlOYW1lOiBkaXNwbGF5TmFtZSA/PyBuYW1lLFxyXG4gICAgICAgICAgICBzdXBwb3J0ZWREaXJlY3RpdmVzOiBbXSxcclxuICAgICAgICAgICAgc3VwcG9ydGVkS2VybmVsQ29tbWFuZHM6IFtdXHJcbiAgICAgICAgfTtcclxuICAgICAgICB0aGlzLl9pbnRlcm5hbFJlZ2lzdGVyQ29tbWFuZEhhbmRsZXIoe1xyXG4gICAgICAgICAgICBjb21tYW5kVHlwZTogY29udHJhY3RzLlJlcXVlc3RLZXJuZWxJbmZvVHlwZSwgaGFuZGxlOiBhc3luYyBpbnZvY2F0aW9uID0+IHtcclxuICAgICAgICAgICAgICAgIGF3YWl0IHRoaXMuaGFuZGxlUmVxdWVzdEtlcm5lbEluZm8oaW52b2NhdGlvbik7XHJcbiAgICAgICAgICAgIH1cclxuICAgICAgICB9KTtcclxuICAgIH1cclxuXHJcbiAgICBwcm90ZWN0ZWQgYXN5bmMgaGFuZGxlUmVxdWVzdEtlcm5lbEluZm8oaW52b2NhdGlvbjogSUtlcm5lbENvbW1hbmRJbnZvY2F0aW9uKTogUHJvbWlzZTx2b2lkPiB7XHJcbiAgICAgICAgY29uc3QgZXZlbnRFbnZlbG9wZTogY29udHJhY3RzLktlcm5lbEV2ZW50RW52ZWxvcGUgPSB7XHJcbiAgICAgICAgICAgIGV2ZW50VHlwZTogY29udHJhY3RzLktlcm5lbEluZm9Qcm9kdWNlZFR5cGUsXHJcbiAgICAgICAgICAgIGNvbW1hbmQ6IGludm9jYXRpb24uY29tbWFuZEVudmVsb3BlLFxyXG4gICAgICAgICAgICBldmVudDogPGNvbnRyYWN0cy5LZXJuZWxJbmZvUHJvZHVjZWQ+eyBrZXJuZWxJbmZvOiB0aGlzLl9rZXJuZWxJbmZvIH1cclxuICAgICAgICB9Oy8vP1xyXG5cclxuICAgICAgICBpbnZvY2F0aW9uLmNvbnRleHQucHVibGlzaChldmVudEVudmVsb3BlKTtcclxuICAgICAgICByZXR1cm4gUHJvbWlzZS5yZXNvbHZlKCk7XHJcbiAgICB9XHJcblxyXG4gICAgcHJpdmF0ZSBnZXRTY2hlZHVsZXIoKTogS2VybmVsU2NoZWR1bGVyPGNvbnRyYWN0cy5LZXJuZWxDb21tYW5kRW52ZWxvcGU+IHtcclxuICAgICAgICBpZiAoIXRoaXMuX3NjaGVkdWxlcikge1xyXG4gICAgICAgICAgICB0aGlzLl9zY2hlZHVsZXIgPSB0aGlzLnBhcmVudEtlcm5lbD8uZ2V0U2NoZWR1bGVyKCkgPz8gbmV3IEtlcm5lbFNjaGVkdWxlcjxjb250cmFjdHMuS2VybmVsQ29tbWFuZEVudmVsb3BlPigpO1xyXG4gICAgICAgIH1cclxuXHJcbiAgICAgICAgcmV0dXJuIHRoaXMuX3NjaGVkdWxlcjtcclxuICAgIH1cclxuXHJcbiAgICBwcm90ZWN0ZWQgZW5zdXJlQ29tbWFuZFRva2VuQW5kSWQoY29tbWFuZEVudmVsb3BlOiBjb250cmFjdHMuS2VybmVsQ29tbWFuZEVudmVsb3BlKSB7XHJcbiAgICAgICAgaWYgKCFjb21tYW5kRW52ZWxvcGUudG9rZW4pIHtcclxuICAgICAgICAgICAgbGV0IG5leHRUb2tlbiA9IHRoaXMuX3Rva2VuR2VuZXJhdG9yLkdldE5ld1Rva2VuKCk7XHJcbiAgICAgICAgICAgIGlmIChLZXJuZWxJbnZvY2F0aW9uQ29udGV4dC5jdXJyZW50Py5jb21tYW5kRW52ZWxvcGUpIHtcclxuICAgICAgICAgICAgICAgIC8vIGEgcGFyZW50IGNvbW1hbmQgZXhpc3RzLCBjcmVhdGUgYSB0b2tlbiBoaWVyYXJjaHlcclxuICAgICAgICAgICAgICAgIG5leHRUb2tlbiA9IEtlcm5lbEludm9jYXRpb25Db250ZXh0LmN1cnJlbnQuY29tbWFuZEVudmVsb3BlLnRva2VuITtcclxuICAgICAgICAgICAgfVxyXG4gICAgICAgICAgICBjb21tYW5kRW52ZWxvcGUudG9rZW4gPSBuZXh0VG9rZW47XHJcbiAgICAgICAgfVxyXG5cclxuICAgICAgICBpZiAoIWNvbW1hbmRFbnZlbG9wZS5pZCkge1xyXG4gICAgICAgICAgICBjb21tYW5kRW52ZWxvcGUuaWQgPSBHdWlkLmNyZWF0ZSgpLnRvU3RyaW5nKCk7XHJcbiAgICAgICAgfVxyXG4gICAgfVxyXG5cclxuICAgIHN0YXRpYyBnZXQgY3VycmVudCgpOiBLZXJuZWwgfCBudWxsIHtcclxuICAgICAgICBpZiAoS2VybmVsSW52b2NhdGlvbkNvbnRleHQuY3VycmVudCkge1xyXG4gICAgICAgICAgICByZXR1cm4gS2VybmVsSW52b2NhdGlvbkNvbnRleHQuY3VycmVudC5oYW5kbGluZ0tlcm5lbDtcclxuICAgICAgICB9XHJcbiAgICAgICAgcmV0dXJuIG51bGw7XHJcbiAgICB9XHJcblxyXG4gICAgc3RhdGljIGdldCByb290KCk6IEtlcm5lbCB8IG51bGwge1xyXG4gICAgICAgIGlmIChLZXJuZWwuY3VycmVudCkge1xyXG4gICAgICAgICAgICByZXR1cm4gS2VybmVsLmN1cnJlbnQucm9vdEtlcm5lbDtcclxuICAgICAgICB9XHJcbiAgICAgICAgcmV0dXJuIG51bGw7XHJcbiAgICB9XHJcblxyXG4gICAgLy8gSXMgaXQgd29ydGggdXMgZ29pbmcgdG8gZWZmb3J0cyB0byBlbnN1cmUgdGhhdCB0aGUgUHJvbWlzZSByZXR1cm5lZCBoZXJlIGFjY3VyYXRlbHkgcmVmbGVjdHNcclxuICAgIC8vIHRoZSBjb21tYW5kJ3MgcHJvZ3Jlc3M/IFRoZSBvbmx5IHRoaW5nIHRoYXQgYWN0dWFsbHkgY2FsbHMgdGhpcyBpcyB0aGUga2VybmVsIGNoYW5uZWwsIHRocm91Z2hcclxuICAgIC8vIHRoZSBjYWxsYmFjayBzZXQgdXAgYnkgYXR0YWNoS2VybmVsVG9DaGFubmVsLCBhbmQgdGhlIGNhbGxiYWNrIGlzIGV4cGVjdGVkIHRvIHJldHVybiB2b2lkLCBzb1xyXG4gICAgLy8gbm90aGluZyBpcyBldmVyIGdvaW5nIHRvIGxvb2sgYXQgdGhlIHByb21pc2Ugd2UgcmV0dXJuIGhlcmUuXHJcbiAgICBhc3luYyBzZW5kKGNvbW1hbmRFbnZlbG9wZTogY29udHJhY3RzLktlcm5lbENvbW1hbmRFbnZlbG9wZSk6IFByb21pc2U8dm9pZD4ge1xyXG4gICAgICAgIHRoaXMuZW5zdXJlQ29tbWFuZFRva2VuQW5kSWQoY29tbWFuZEVudmVsb3BlKTtcclxuICAgICAgICBjb25zdCBrZXJuZWxVcmkgPSBnZXRLZXJuZWxVcmkodGhpcyk7XHJcbiAgICAgICAgaWYgKCFyb3V0aW5nc2xpcC5jb21tYW5kUm91dGluZ1NsaXBDb250YWlucyhjb21tYW5kRW52ZWxvcGUsIGtlcm5lbFVyaSkpIHtcclxuICAgICAgICAgICAgcm91dGluZ3NsaXAuc3RhbXBDb21tYW5kUm91dGluZ1NsaXBBc0Fycml2ZWQoY29tbWFuZEVudmVsb3BlLCBrZXJuZWxVcmkpO1xyXG4gICAgICAgIH0gZWxzZSB7XHJcbiAgICAgICAgICAgIFwic2hvdWxkIG5vdCBiZSBoZXJlXCI7Ly8/XHJcbiAgICAgICAgfVxyXG4gICAgICAgIGNvbW1hbmRFbnZlbG9wZS5yb3V0aW5nU2xpcDsvLz9cclxuICAgICAgICBLZXJuZWxJbnZvY2F0aW9uQ29udGV4dC5lc3RhYmxpc2goY29tbWFuZEVudmVsb3BlKTtcclxuICAgICAgICByZXR1cm4gdGhpcy5nZXRTY2hlZHVsZXIoKS5ydW5Bc3luYyhjb21tYW5kRW52ZWxvcGUsICh2YWx1ZSkgPT4gdGhpcy5leGVjdXRlQ29tbWFuZCh2YWx1ZSkuZmluYWxseSgoKSA9PiB7XHJcbiAgICAgICAgICAgIHJvdXRpbmdzbGlwLnN0YW1wQ29tbWFuZFJvdXRpbmdTbGlwKGNvbW1hbmRFbnZlbG9wZSwga2VybmVsVXJpKTtcclxuICAgICAgICB9KSk7XHJcbiAgICB9XHJcblxyXG4gICAgcHJpdmF0ZSBhc3luYyBleGVjdXRlQ29tbWFuZChjb21tYW5kRW52ZWxvcGU6IGNvbnRyYWN0cy5LZXJuZWxDb21tYW5kRW52ZWxvcGUpOiBQcm9taXNlPHZvaWQ+IHtcclxuICAgICAgICBsZXQgY29udGV4dCA9IEtlcm5lbEludm9jYXRpb25Db250ZXh0LmVzdGFibGlzaChjb21tYW5kRW52ZWxvcGUpO1xyXG4gICAgICAgIGxldCBwcmV2aW91c0hhbmRsaW5nS2VybmVsID0gY29udGV4dC5oYW5kbGluZ0tlcm5lbDtcclxuXHJcbiAgICAgICAgdHJ5IHtcclxuICAgICAgICAgICAgYXdhaXQgdGhpcy5oYW5kbGVDb21tYW5kKGNvbW1hbmRFbnZlbG9wZSk7XHJcbiAgICAgICAgfVxyXG4gICAgICAgIGNhdGNoIChlKSB7XHJcbiAgICAgICAgICAgIGNvbnRleHQuZmFpbCgoPGFueT5lKT8ubWVzc2FnZSB8fCBKU09OLnN0cmluZ2lmeShlKSk7XHJcbiAgICAgICAgfVxyXG4gICAgICAgIGZpbmFsbHkge1xyXG4gICAgICAgICAgICBjb250ZXh0LmhhbmRsaW5nS2VybmVsID0gcHJldmlvdXNIYW5kbGluZ0tlcm5lbDtcclxuICAgICAgICB9XHJcbiAgICB9XHJcblxyXG4gICAgZ2V0Q29tbWFuZEhhbmRsZXIoY29tbWFuZFR5cGU6IGNvbnRyYWN0cy5LZXJuZWxDb21tYW5kVHlwZSk6IElLZXJuZWxDb21tYW5kSGFuZGxlciB8IHVuZGVmaW5lZCB7XHJcbiAgICAgICAgcmV0dXJuIHRoaXMuX2NvbW1hbmRIYW5kbGVycy5nZXQoY29tbWFuZFR5cGUpO1xyXG4gICAgfVxyXG5cclxuICAgIGhhbmRsZUNvbW1hbmQoY29tbWFuZEVudmVsb3BlOiBjb250cmFjdHMuS2VybmVsQ29tbWFuZEVudmVsb3BlKTogUHJvbWlzZTx2b2lkPiB7XHJcbiAgICAgICAgcmV0dXJuIG5ldyBQcm9taXNlPHZvaWQ+KGFzeW5jIChyZXNvbHZlLCByZWplY3QpID0+IHtcclxuICAgICAgICAgICAgbGV0IGNvbnRleHQgPSBLZXJuZWxJbnZvY2F0aW9uQ29udGV4dC5lc3RhYmxpc2goY29tbWFuZEVudmVsb3BlKTtcclxuXHJcbiAgICAgICAgICAgIGNvbnN0IHByZXZpb3VkSGVuZGxpbmdLZXJuZWwgPSBjb250ZXh0LmhhbmRsaW5nS2VybmVsO1xyXG4gICAgICAgICAgICBjb250ZXh0LmhhbmRsaW5nS2VybmVsID0gdGhpcztcclxuICAgICAgICAgICAgbGV0IGlzUm9vdENvbW1hbmQgPSBhcmVDb21tYW5kc1RoZVNhbWUoY29udGV4dC5jb21tYW5kRW52ZWxvcGUsIGNvbW1hbmRFbnZlbG9wZSk7XHJcblxyXG4gICAgICAgICAgICBsZXQgZXZlbnRTdWJzY3JpcHRpb246IHJ4anMuU3Vic2NyaXB0aW9uIHwgdW5kZWZpbmVkID0gdW5kZWZpbmVkOy8vP1xyXG5cclxuICAgICAgICAgICAgaWYgKGlzUm9vdENvbW1hbmQpIHtcclxuICAgICAgICAgICAgICAgIHRoaXMubmFtZTsvLz9cclxuICAgICAgICAgICAgICAgIExvZ2dlci5kZWZhdWx0LmluZm8oYGtlcm5lbCAke3RoaXMubmFtZX0gb2YgdHlwZSAke0tlcm5lbFR5cGVbdGhpcy5rZXJuZWxUeXBlXX0gc3Vic2NyaWJpbmcgdG8gY29udGV4dCBldmVudHNgKTtcclxuICAgICAgICAgICAgICAgIGV2ZW50U3Vic2NyaXB0aW9uID0gY29udGV4dC5rZXJuZWxFdmVudHMucGlwZShyeGpzLm1hcChlID0+IHtcclxuICAgICAgICAgICAgICAgICAgICBjb25zdCBtZXNzYWdlID0gYGtlcm5lbCAke3RoaXMubmFtZX0gb2YgdHlwZSAke0tlcm5lbFR5cGVbdGhpcy5rZXJuZWxUeXBlXX0gc2F3IGV2ZW50ICR7ZS5ldmVudFR5cGV9IHdpdGggdG9rZW4gJHtlLmNvbW1hbmQ/LnRva2VufWA7XHJcbiAgICAgICAgICAgICAgICAgICAgbWVzc2FnZTsvLz9cclxuICAgICAgICAgICAgICAgICAgICBMb2dnZXIuZGVmYXVsdC5pbmZvKG1lc3NhZ2UpO1xyXG4gICAgICAgICAgICAgICAgICAgIGNvbnN0IGtlcm5lbFVyaSA9IGdldEtlcm5lbFVyaSh0aGlzKTtcclxuICAgICAgICAgICAgICAgICAgICBpZiAoIXJvdXRpbmdzbGlwLmV2ZW50Um91dGluZ1NsaXBDb250YWlucyhlLCBrZXJuZWxVcmkpKSB7XHJcbiAgICAgICAgICAgICAgICAgICAgICAgIHJvdXRpbmdzbGlwLnN0YW1wRXZlbnRSb3V0aW5nU2xpcChlLCBrZXJuZWxVcmkpO1xyXG4gICAgICAgICAgICAgICAgICAgIH0gZWxzZSB7XHJcbiAgICAgICAgICAgICAgICAgICAgICAgIFwic2hvdWxkIG5vdCBnZXQgaGVyZVwiOy8vP1xyXG4gICAgICAgICAgICAgICAgICAgIH1cclxuICAgICAgICAgICAgICAgICAgICByZXR1cm4gZTtcclxuICAgICAgICAgICAgICAgIH0pKVxyXG4gICAgICAgICAgICAgICAgICAgIC5zdWJzY3JpYmUodGhpcy5wdWJsaXNoRXZlbnQuYmluZCh0aGlzKSk7XHJcbiAgICAgICAgICAgIH1cclxuXHJcbiAgICAgICAgICAgIGxldCBoYW5kbGVyID0gdGhpcy5nZXRDb21tYW5kSGFuZGxlcihjb21tYW5kRW52ZWxvcGUuY29tbWFuZFR5cGUpO1xyXG4gICAgICAgICAgICBpZiAoaGFuZGxlcikge1xyXG4gICAgICAgICAgICAgICAgdHJ5IHtcclxuICAgICAgICAgICAgICAgICAgICBMb2dnZXIuZGVmYXVsdC5pbmZvKGBrZXJuZWwgJHt0aGlzLm5hbWV9IGFib3V0IHRvIGhhbmRsZSBjb21tYW5kOiAke0pTT04uc3RyaW5naWZ5KGNvbW1hbmRFbnZlbG9wZSl9YCk7XHJcbiAgICAgICAgICAgICAgICAgICAgYXdhaXQgaGFuZGxlci5oYW5kbGUoeyBjb21tYW5kRW52ZWxvcGU6IGNvbW1hbmRFbnZlbG9wZSwgY29udGV4dCB9KTtcclxuICAgICAgICAgICAgICAgICAgICBjb250ZXh0LmNvbXBsZXRlKGNvbW1hbmRFbnZlbG9wZSk7XHJcbiAgICAgICAgICAgICAgICAgICAgY29udGV4dC5oYW5kbGluZ0tlcm5lbCA9IHByZXZpb3VkSGVuZGxpbmdLZXJuZWw7XHJcbiAgICAgICAgICAgICAgICAgICAgaWYgKGlzUm9vdENvbW1hbmQpIHtcclxuICAgICAgICAgICAgICAgICAgICAgICAgZXZlbnRTdWJzY3JpcHRpb24/LnVuc3Vic2NyaWJlKCk7XHJcbiAgICAgICAgICAgICAgICAgICAgICAgIGNvbnRleHQuZGlzcG9zZSgpO1xyXG4gICAgICAgICAgICAgICAgICAgIH1cclxuICAgICAgICAgICAgICAgICAgICBMb2dnZXIuZGVmYXVsdC5pbmZvKGBrZXJuZWwgJHt0aGlzLm5hbWV9IGRvbmUgaGFuZGxpbmcgY29tbWFuZDogJHtKU09OLnN0cmluZ2lmeShjb21tYW5kRW52ZWxvcGUpfWApO1xyXG4gICAgICAgICAgICAgICAgICAgIHJlc29sdmUoKTtcclxuICAgICAgICAgICAgICAgIH1cclxuICAgICAgICAgICAgICAgIGNhdGNoIChlKSB7XHJcbiAgICAgICAgICAgICAgICAgICAgY29udGV4dC5mYWlsKCg8YW55PmUpPy5tZXNzYWdlIHx8IEpTT04uc3RyaW5naWZ5KGUpKTtcclxuICAgICAgICAgICAgICAgICAgICBjb250ZXh0LmhhbmRsaW5nS2VybmVsID0gcHJldmlvdWRIZW5kbGluZ0tlcm5lbDtcclxuICAgICAgICAgICAgICAgICAgICBpZiAoaXNSb290Q29tbWFuZCkge1xyXG4gICAgICAgICAgICAgICAgICAgICAgICBldmVudFN1YnNjcmlwdGlvbj8udW5zdWJzY3JpYmUoKTtcclxuICAgICAgICAgICAgICAgICAgICAgICAgY29udGV4dC5kaXNwb3NlKCk7XHJcbiAgICAgICAgICAgICAgICAgICAgfVxyXG4gICAgICAgICAgICAgICAgICAgIHJlamVjdChlKTtcclxuICAgICAgICAgICAgICAgIH1cclxuICAgICAgICAgICAgfSBlbHNlIHtcclxuICAgICAgICAgICAgICAgIGNvbnRleHQuaGFuZGxpbmdLZXJuZWwgPSBwcmV2aW91ZEhlbmRsaW5nS2VybmVsO1xyXG4gICAgICAgICAgICAgICAgaWYgKGlzUm9vdENvbW1hbmQpIHtcclxuICAgICAgICAgICAgICAgICAgICBldmVudFN1YnNjcmlwdGlvbj8udW5zdWJzY3JpYmUoKTtcclxuICAgICAgICAgICAgICAgICAgICBjb250ZXh0LmRpc3Bvc2UoKTtcclxuICAgICAgICAgICAgICAgIH1cclxuICAgICAgICAgICAgICAgIHJlamVjdChuZXcgRXJyb3IoYE5vIGhhbmRsZXIgZm91bmQgZm9yIGNvbW1hbmQgdHlwZSAke2NvbW1hbmRFbnZlbG9wZS5jb21tYW5kVHlwZX1gKSk7XHJcbiAgICAgICAgICAgIH1cclxuICAgICAgICB9KTtcclxuICAgIH1cclxuXHJcbiAgICBzdWJzY3JpYmVUb0tlcm5lbEV2ZW50cyhvYnNlcnZlcjogY29udHJhY3RzLktlcm5lbEV2ZW50RW52ZWxvcGVPYnNlcnZlcik6IGRpc3Bvc2FibGVzLkRpc3Bvc2FibGVTdWJzY3JpcHRpb24ge1xyXG4gICAgICAgIGNvbnN0IHN1YiA9IHRoaXMuX2V2ZW50U3ViamVjdC5zdWJzY3JpYmUob2JzZXJ2ZXIpO1xyXG5cclxuICAgICAgICByZXR1cm4ge1xyXG4gICAgICAgICAgICBkaXNwb3NlOiAoKSA9PiB7IHN1Yi51bnN1YnNjcmliZSgpOyB9XHJcbiAgICAgICAgfTtcclxuICAgIH1cclxuXHJcbiAgICBwcm90ZWN0ZWQgY2FuSGFuZGxlKGNvbW1hbmRFbnZlbG9wZTogY29udHJhY3RzLktlcm5lbENvbW1hbmRFbnZlbG9wZSkge1xyXG4gICAgICAgIGlmIChjb21tYW5kRW52ZWxvcGUuY29tbWFuZC50YXJnZXRLZXJuZWxOYW1lICYmIGNvbW1hbmRFbnZlbG9wZS5jb21tYW5kLnRhcmdldEtlcm5lbE5hbWUgIT09IHRoaXMubmFtZSkge1xyXG4gICAgICAgICAgICByZXR1cm4gZmFsc2U7XHJcblxyXG4gICAgICAgIH1cclxuXHJcbiAgICAgICAgaWYgKGNvbW1hbmRFbnZlbG9wZS5jb21tYW5kLmRlc3RpbmF0aW9uVXJpKSB7XHJcbiAgICAgICAgICAgIGNvbnN0IG5vcm1hbGl6ZWRVcmkgPSByb3V0aW5nc2xpcC5jcmVhdGVLZXJuZWxVcmkoY29tbWFuZEVudmVsb3BlLmNvbW1hbmQuZGVzdGluYXRpb25VcmkpO1xyXG4gICAgICAgICAgICBpZiAodGhpcy5rZXJuZWxJbmZvLnVyaSAhPT0gbm9ybWFsaXplZFVyaSkge1xyXG4gICAgICAgICAgICAgICAgcmV0dXJuIGZhbHNlO1xyXG4gICAgICAgICAgICB9XHJcbiAgICAgICAgfVxyXG5cclxuICAgICAgICByZXR1cm4gdGhpcy5zdXBwb3J0c0NvbW1hbmQoY29tbWFuZEVudmVsb3BlLmNvbW1hbmRUeXBlKTtcclxuICAgIH1cclxuXHJcbiAgICBzdXBwb3J0c0NvbW1hbmQoY29tbWFuZFR5cGU6IGNvbnRyYWN0cy5LZXJuZWxDb21tYW5kVHlwZSk6IGJvb2xlYW4ge1xyXG4gICAgICAgIHJldHVybiB0aGlzLl9jb21tYW5kSGFuZGxlcnMuaGFzKGNvbW1hbmRUeXBlKTtcclxuICAgIH1cclxuXHJcbiAgICByZWdpc3RlckNvbW1hbmRIYW5kbGVyKGhhbmRsZXI6IElLZXJuZWxDb21tYW5kSGFuZGxlcik6IHZvaWQge1xyXG4gICAgICAgIC8vIFdoZW4gYSByZWdpc3RyYXRpb24gYWxyZWFkeSBleGlzdGVkLCB3ZSB3YW50IHRvIG92ZXJ3cml0ZSBpdCBiZWNhdXNlIHdlIHdhbnQgdXNlcnMgdG9cclxuICAgICAgICAvLyBiZSBhYmxlIHRvIGRldmVsb3AgaGFuZGxlcnMgaXRlcmF0aXZlbHksIGFuZCBpdCB3b3VsZCBiZSB1bmhlbHBmdWwgZm9yIGhhbmRsZXIgcmVnaXN0cmF0aW9uXHJcbiAgICAgICAgLy8gZm9yIGFueSBwYXJ0aWN1bGFyIGNvbW1hbmQgdG8gYmUgY3VtdWxhdGl2ZS5cclxuXHJcbiAgICAgICAgY29uc3Qgc2hvdWxkTm90aWZ5ID0gIXRoaXMuX2NvbW1hbmRIYW5kbGVycy5oYXMoaGFuZGxlci5jb21tYW5kVHlwZSk7XHJcbiAgICAgICAgdGhpcy5faW50ZXJuYWxSZWdpc3RlckNvbW1hbmRIYW5kbGVyKGhhbmRsZXIpO1xyXG4gICAgICAgIGlmIChzaG91bGROb3RpZnkpIHtcclxuICAgICAgICAgICAgY29uc3QgZXZlbnQ6IGNvbnRyYWN0cy5LZXJuZWxJbmZvUHJvZHVjZWQgPSB7XHJcbiAgICAgICAgICAgICAgICBrZXJuZWxJbmZvOiB0aGlzLl9rZXJuZWxJbmZvLFxyXG4gICAgICAgICAgICB9O1xyXG4gICAgICAgICAgICBjb25zdCBlbnZlbG9wZTogY29udHJhY3RzLktlcm5lbEV2ZW50RW52ZWxvcGUgPSB7XHJcbiAgICAgICAgICAgICAgICBldmVudFR5cGU6IGNvbnRyYWN0cy5LZXJuZWxJbmZvUHJvZHVjZWRUeXBlLFxyXG4gICAgICAgICAgICAgICAgZXZlbnQ6IGV2ZW50XHJcbiAgICAgICAgICAgIH07XHJcbiAgICAgICAgICAgIHJvdXRpbmdzbGlwLnN0YW1wRXZlbnRSb3V0aW5nU2xpcChlbnZlbG9wZSwgZ2V0S2VybmVsVXJpKHRoaXMpKTtcclxuICAgICAgICAgICAgY29uc3QgY29udGV4dCA9IEtlcm5lbEludm9jYXRpb25Db250ZXh0LmN1cnJlbnQ7XHJcblxyXG4gICAgICAgICAgICBpZiAoY29udGV4dCkge1xyXG4gICAgICAgICAgICAgICAgZW52ZWxvcGUuY29tbWFuZCA9IGNvbnRleHQuY29tbWFuZEVudmVsb3BlO1xyXG5cclxuICAgICAgICAgICAgICAgIGNvbnRleHQucHVibGlzaChlbnZlbG9wZSk7XHJcbiAgICAgICAgICAgIH0gZWxzZSB7XHJcbiAgICAgICAgICAgICAgICB0aGlzLnB1Ymxpc2hFdmVudChlbnZlbG9wZSk7XHJcbiAgICAgICAgICAgIH1cclxuICAgICAgICB9XHJcbiAgICB9XHJcblxyXG4gICAgcHJpdmF0ZSBfaW50ZXJuYWxSZWdpc3RlckNvbW1hbmRIYW5kbGVyKGhhbmRsZXI6IElLZXJuZWxDb21tYW5kSGFuZGxlcik6IHZvaWQge1xyXG4gICAgICAgIHRoaXMuX2NvbW1hbmRIYW5kbGVycy5zZXQoaGFuZGxlci5jb21tYW5kVHlwZSwgaGFuZGxlcik7XHJcbiAgICAgICAgdGhpcy5fa2VybmVsSW5mby5zdXBwb3J0ZWRLZXJuZWxDb21tYW5kcyA9IEFycmF5LmZyb20odGhpcy5fY29tbWFuZEhhbmRsZXJzLmtleXMoKSkubWFwKGNvbW1hbmROYW1lID0+ICh7IG5hbWU6IGNvbW1hbmROYW1lIH0pKTtcclxuICAgIH1cclxuXHJcbiAgICBwcm90ZWN0ZWQgZ2V0SGFuZGxpbmdLZXJuZWwoY29tbWFuZEVudmVsb3BlOiBjb250cmFjdHMuS2VybmVsQ29tbWFuZEVudmVsb3BlLCBjb250ZXh0PzogS2VybmVsSW52b2NhdGlvbkNvbnRleHQgfCBudWxsKTogS2VybmVsIHwgbnVsbCB7XHJcbiAgICAgICAgaWYgKHRoaXMuY2FuSGFuZGxlKGNvbW1hbmRFbnZlbG9wZSkpIHtcclxuICAgICAgICAgICAgcmV0dXJuIHRoaXM7XHJcbiAgICAgICAgfSBlbHNlIHtcclxuICAgICAgICAgICAgY29udGV4dD8uZmFpbChgQ29tbWFuZCAke2NvbW1hbmRFbnZlbG9wZS5jb21tYW5kVHlwZX0gaXMgbm90IHN1cHBvcnRlZCBieSBLZXJuZWwgJHt0aGlzLm5hbWV9YCk7XHJcbiAgICAgICAgICAgIHJldHVybiBudWxsO1xyXG4gICAgICAgIH1cclxuICAgIH1cclxuXHJcbiAgICBwcm90ZWN0ZWQgcHVibGlzaEV2ZW50KGtlcm5lbEV2ZW50OiBjb250cmFjdHMuS2VybmVsRXZlbnRFbnZlbG9wZSkge1xyXG4gICAgICAgIHRoaXMuX2V2ZW50U3ViamVjdC5uZXh0KGtlcm5lbEV2ZW50KTtcclxuICAgIH1cclxufVxyXG5cclxuZXhwb3J0IGFzeW5jIGZ1bmN0aW9uIHN1Ym1pdENvbW1hbmRBbmRHZXRSZXN1bHQ8VEV2ZW50IGV4dGVuZHMgY29udHJhY3RzLktlcm5lbEV2ZW50PihrZXJuZWw6IEtlcm5lbCwgY29tbWFuZEVudmVsb3BlOiBjb250cmFjdHMuS2VybmVsQ29tbWFuZEVudmVsb3BlLCBleHBlY3RlZEV2ZW50VHlwZTogY29udHJhY3RzLktlcm5lbEV2ZW50VHlwZSk6IFByb21pc2U8VEV2ZW50PiB7XHJcbiAgICBsZXQgY29tcGxldGlvblNvdXJjZSA9IG5ldyBQcm9taXNlQ29tcGxldGlvblNvdXJjZTxURXZlbnQ+KCk7XHJcbiAgICBsZXQgaGFuZGxlZCA9IGZhbHNlO1xyXG4gICAgbGV0IGRpc3Bvc2FibGUgPSBrZXJuZWwuc3Vic2NyaWJlVG9LZXJuZWxFdmVudHMoZXZlbnRFbnZlbG9wZSA9PiB7XHJcbiAgICAgICAgaWYgKGV2ZW50RW52ZWxvcGUuY29tbWFuZD8udG9rZW4gPT09IGNvbW1hbmRFbnZlbG9wZS50b2tlbikge1xyXG4gICAgICAgICAgICBzd2l0Y2ggKGV2ZW50RW52ZWxvcGUuZXZlbnRUeXBlKSB7XHJcbiAgICAgICAgICAgICAgICBjYXNlIGNvbnRyYWN0cy5Db21tYW5kRmFpbGVkVHlwZTpcclxuICAgICAgICAgICAgICAgICAgICBpZiAoIWhhbmRsZWQpIHtcclxuICAgICAgICAgICAgICAgICAgICAgICAgaGFuZGxlZCA9IHRydWU7XHJcbiAgICAgICAgICAgICAgICAgICAgICAgIGxldCBlcnIgPSA8Y29udHJhY3RzLkNvbW1hbmRGYWlsZWQ+ZXZlbnRFbnZlbG9wZS5ldmVudDsvLz9cclxuICAgICAgICAgICAgICAgICAgICAgICAgY29tcGxldGlvblNvdXJjZS5yZWplY3QoZXJyKTtcclxuICAgICAgICAgICAgICAgICAgICB9XHJcbiAgICAgICAgICAgICAgICAgICAgYnJlYWs7XHJcbiAgICAgICAgICAgICAgICBjYXNlIGNvbnRyYWN0cy5Db21tYW5kU3VjY2VlZGVkVHlwZTpcclxuICAgICAgICAgICAgICAgICAgICBpZiAoYXJlQ29tbWFuZHNUaGVTYW1lKGV2ZW50RW52ZWxvcGUuY29tbWFuZCEsIGNvbW1hbmRFbnZlbG9wZSlcclxuICAgICAgICAgICAgICAgICAgICAgICAgJiYgKGV2ZW50RW52ZWxvcGUuY29tbWFuZD8uaWQgPT09IGNvbW1hbmRFbnZlbG9wZS5pZCkpIHtcclxuICAgICAgICAgICAgICAgICAgICAgICAgaWYgKCFoYW5kbGVkKSB7Ly8/ICgkID8gZXZlbnRFbnZlbG9wZSA6IHt9KVxyXG4gICAgICAgICAgICAgICAgICAgICAgICAgICAgaGFuZGxlZCA9IHRydWU7XHJcbiAgICAgICAgICAgICAgICAgICAgICAgICAgICBjb21wbGV0aW9uU291cmNlLnJlamVjdCgnQ29tbWFuZCB3YXMgaGFuZGxlZCBiZWZvcmUgcmVwb3J0aW5nIGV4cGVjdGVkIHJlc3VsdC4nKTtcclxuICAgICAgICAgICAgICAgICAgICAgICAgfVxyXG4gICAgICAgICAgICAgICAgICAgICAgICBicmVhaztcclxuICAgICAgICAgICAgICAgICAgICB9XHJcbiAgICAgICAgICAgICAgICBkZWZhdWx0OlxyXG4gICAgICAgICAgICAgICAgICAgIGlmIChldmVudEVudmVsb3BlLmV2ZW50VHlwZSA9PT0gZXhwZWN0ZWRFdmVudFR5cGUpIHtcclxuICAgICAgICAgICAgICAgICAgICAgICAgaGFuZGxlZCA9IHRydWU7XHJcbiAgICAgICAgICAgICAgICAgICAgICAgIGxldCBldmVudCA9IDxURXZlbnQ+ZXZlbnRFbnZlbG9wZS5ldmVudDsvLz8gKCQgPyBldmVudEVudmVsb3BlIDoge30pXHJcbiAgICAgICAgICAgICAgICAgICAgICAgIGNvbXBsZXRpb25Tb3VyY2UucmVzb2x2ZShldmVudCk7XHJcbiAgICAgICAgICAgICAgICAgICAgfVxyXG4gICAgICAgICAgICAgICAgICAgIGJyZWFrO1xyXG4gICAgICAgICAgICB9XHJcbiAgICAgICAgfVxyXG4gICAgfSk7XHJcblxyXG4gICAgdHJ5IHtcclxuICAgICAgICBhd2FpdCBrZXJuZWwuc2VuZChjb21tYW5kRW52ZWxvcGUpO1xyXG4gICAgfVxyXG4gICAgZmluYWxseSB7XHJcbiAgICAgICAgZGlzcG9zYWJsZS5kaXNwb3NlKCk7XHJcbiAgICB9XHJcblxyXG4gICAgcmV0dXJuIGNvbXBsZXRpb25Tb3VyY2UucHJvbWlzZTtcclxufVxyXG5cclxuZXhwb3J0IGZ1bmN0aW9uIGdldEtlcm5lbFVyaShrZXJuZWw6IEtlcm5lbCk6IHN0cmluZyB7XHJcbiAgICByZXR1cm4ga2VybmVsLmtlcm5lbEluZm8udXJpID8/IGBrZXJuZWw6Ly9sb2NhbC8ke2tlcm5lbC5rZXJuZWxJbmZvLmxvY2FsTmFtZX1gO1xyXG59IiwiLy8gQ29weXJpZ2h0IChjKSAuTkVUIEZvdW5kYXRpb24gYW5kIGNvbnRyaWJ1dG9ycy4gQWxsIHJpZ2h0cyByZXNlcnZlZC5cclxuLy8gTGljZW5zZWQgdW5kZXIgdGhlIE1JVCBsaWNlbnNlLiBTZWUgTElDRU5TRSBmaWxlIGluIHRoZSBwcm9qZWN0IHJvb3QgZm9yIGZ1bGwgbGljZW5zZSBpbmZvcm1hdGlvbi5cclxuXHJcbmltcG9ydCAqIGFzIHJvdXRpbmdzbGlwIGZyb20gXCIuL3JvdXRpbmdzbGlwXCI7XHJcbmltcG9ydCAqIGFzIGNvbnRyYWN0cyBmcm9tIFwiLi9jb250cmFjdHNcIjtcclxuaW1wb3J0IHsgZ2V0S2VybmVsVXJpLCBJS2VybmVsQ29tbWFuZEludm9jYXRpb24sIEtlcm5lbCwgS2VybmVsVHlwZSB9IGZyb20gXCIuL2tlcm5lbFwiO1xyXG5pbXBvcnQgeyBLZXJuZWxIb3N0IH0gZnJvbSBcIi4va2VybmVsSG9zdFwiO1xyXG5pbXBvcnQgeyBLZXJuZWxJbnZvY2F0aW9uQ29udGV4dCB9IGZyb20gXCIuL2tlcm5lbEludm9jYXRpb25Db250ZXh0XCI7XHJcbmltcG9ydCB7IExvZ2dlciB9IGZyb20gXCIuL2xvZ2dlclwiO1xyXG5cclxuZXhwb3J0IGNsYXNzIENvbXBvc2l0ZUtlcm5lbCBleHRlbmRzIEtlcm5lbCB7XHJcbiAgICBwcml2YXRlIF9ob3N0OiBLZXJuZWxIb3N0IHwgbnVsbCA9IG51bGw7XHJcbiAgICBwcml2YXRlIHJlYWRvbmx5IF9kZWZhdWx0S2VybmVsTmFtZXNCeUNvbW1hbmRUeXBlOiBNYXA8Y29udHJhY3RzLktlcm5lbENvbW1hbmRUeXBlLCBzdHJpbmc+ID0gbmV3IE1hcCgpO1xyXG5cclxuICAgIGRlZmF1bHRLZXJuZWxOYW1lOiBzdHJpbmcgfCB1bmRlZmluZWQ7XHJcbiAgICBwcml2YXRlIF9jaGlsZEtlcm5lbHM6IEtlcm5lbENvbGxlY3Rpb247XHJcblxyXG4gICAgY29uc3RydWN0b3IobmFtZTogc3RyaW5nKSB7XHJcbiAgICAgICAgc3VwZXIobmFtZSk7XHJcbiAgICAgICAgdGhpcy5rZXJuZWxUeXBlID0gS2VybmVsVHlwZS5jb21wb3NpdGU7XHJcbiAgICAgICAgdGhpcy5fY2hpbGRLZXJuZWxzID0gbmV3IEtlcm5lbENvbGxlY3Rpb24odGhpcyk7XHJcbiAgICB9XHJcblxyXG4gICAgZ2V0IGNoaWxkS2VybmVscygpIHtcclxuICAgICAgICByZXR1cm4gQXJyYXkuZnJvbSh0aGlzLl9jaGlsZEtlcm5lbHMpO1xyXG4gICAgfVxyXG5cclxuICAgIGdldCBob3N0KCk6IEtlcm5lbEhvc3QgfCBudWxsIHtcclxuICAgICAgICByZXR1cm4gdGhpcy5faG9zdDtcclxuICAgIH1cclxuXHJcbiAgICBzZXQgaG9zdChob3N0OiBLZXJuZWxIb3N0IHwgbnVsbCkge1xyXG4gICAgICAgIHRoaXMuX2hvc3QgPSBob3N0O1xyXG4gICAgICAgIGlmICh0aGlzLl9ob3N0KSB7XHJcbiAgICAgICAgICAgIHRoaXMua2VybmVsSW5mby51cmkgPSB0aGlzLl9ob3N0LnVyaTtcclxuICAgICAgICAgICAgdGhpcy5fY2hpbGRLZXJuZWxzLm5vdGlmeVRoYXRIb3N0V2FzU2V0KCk7XHJcbiAgICAgICAgfVxyXG4gICAgfVxyXG5cclxuICAgIHByb3RlY3RlZCBvdmVycmlkZSBhc3luYyBoYW5kbGVSZXF1ZXN0S2VybmVsSW5mbyhpbnZvY2F0aW9uOiBJS2VybmVsQ29tbWFuZEludm9jYXRpb24pOiBQcm9taXNlPHZvaWQ+IHtcclxuXHJcbiAgICAgICAgY29uc3QgZXZlbnRFbnZlbG9wZTogY29udHJhY3RzLktlcm5lbEV2ZW50RW52ZWxvcGUgPSB7XHJcbiAgICAgICAgICAgIGV2ZW50VHlwZTogY29udHJhY3RzLktlcm5lbEluZm9Qcm9kdWNlZFR5cGUsXHJcbiAgICAgICAgICAgIGNvbW1hbmQ6IGludm9jYXRpb24uY29tbWFuZEVudmVsb3BlLFxyXG4gICAgICAgICAgICBldmVudDogPGNvbnRyYWN0cy5LZXJuZWxJbmZvUHJvZHVjZWQ+eyBrZXJuZWxJbmZvOiB0aGlzLmtlcm5lbEluZm8gfVxyXG4gICAgICAgIH07Ly8/XHJcblxyXG4gICAgICAgIGludm9jYXRpb24uY29udGV4dC5wdWJsaXNoKGV2ZW50RW52ZWxvcGUpO1xyXG5cclxuICAgICAgICBmb3IgKGxldCBrZXJuZWwgb2YgdGhpcy5fY2hpbGRLZXJuZWxzKSB7XHJcbiAgICAgICAgICAgIGlmIChrZXJuZWwuc3VwcG9ydHNDb21tYW5kKGludm9jYXRpb24uY29tbWFuZEVudmVsb3BlLmNvbW1hbmRUeXBlKSkge1xyXG4gICAgICAgICAgICAgICAgY29uc3QgY2hpbGRDb21tYW5kOiBjb250cmFjdHMuS2VybmVsQ29tbWFuZEVudmVsb3BlID0ge1xyXG4gICAgICAgICAgICAgICAgICAgIGNvbW1hbmRUeXBlOiBjb250cmFjdHMuUmVxdWVzdEtlcm5lbEluZm9UeXBlLFxyXG4gICAgICAgICAgICAgICAgICAgIGNvbW1hbmQ6IHtcclxuICAgICAgICAgICAgICAgICAgICAgICAgdGFyZ2V0S2VybmVsTmFtZToga2VybmVsLmtlcm5lbEluZm8ubG9jYWxOYW1lXHJcbiAgICAgICAgICAgICAgICAgICAgfSxcclxuICAgICAgICAgICAgICAgICAgICByb3V0aW5nU2xpcDogW11cclxuICAgICAgICAgICAgICAgIH07XHJcbiAgICAgICAgICAgICAgICByb3V0aW5nc2xpcC5jb250aW51ZUNvbW1hbmRSb3V0aW5nU2xpcChjaGlsZENvbW1hbmQsIGludm9jYXRpb24uY29tbWFuZEVudmVsb3BlLnJvdXRpbmdTbGlwIHx8IFtdKTtcclxuICAgICAgICAgICAgICAgIGF3YWl0IGtlcm5lbC5oYW5kbGVDb21tYW5kKGNoaWxkQ29tbWFuZCk7XHJcbiAgICAgICAgICAgIH1cclxuICAgICAgICB9XHJcbiAgICB9XHJcblxyXG4gICAgYWRkKGtlcm5lbDogS2VybmVsLCBhbGlhc2VzPzogc3RyaW5nW10pIHtcclxuICAgICAgICBpZiAoIWtlcm5lbCkge1xyXG4gICAgICAgICAgICB0aHJvdyBuZXcgRXJyb3IoXCJrZXJuZWwgY2Fubm90IGJlIG51bGwgb3IgdW5kZWZpbmVkXCIpO1xyXG4gICAgICAgIH1cclxuXHJcbiAgICAgICAgaWYgKCF0aGlzLmRlZmF1bHRLZXJuZWxOYW1lKSB7XHJcbiAgICAgICAgICAgIC8vIGRlZmF1bHQgdG8gZmlyc3Qga2VybmVsXHJcbiAgICAgICAgICAgIHRoaXMuZGVmYXVsdEtlcm5lbE5hbWUgPSBrZXJuZWwubmFtZTtcclxuICAgICAgICB9XHJcblxyXG4gICAgICAgIGtlcm5lbC5wYXJlbnRLZXJuZWwgPSB0aGlzO1xyXG4gICAgICAgIGtlcm5lbC5yb290S2VybmVsID0gdGhpcy5yb290S2VybmVsO1xyXG4gICAgICAgIGtlcm5lbC5rZXJuZWxFdmVudHMuc3Vic2NyaWJlKHtcclxuICAgICAgICAgICAgbmV4dDogKGV2ZW50KSA9PiB7XHJcbiAgICAgICAgICAgICAgICBldmVudDsvLz9cclxuICAgICAgICAgICAgICAgIGNvbnN0IGtlcm5lbFVyaSA9IGdldEtlcm5lbFVyaSh0aGlzKTtcclxuICAgICAgICAgICAgICAgIGlmICghcm91dGluZ3NsaXAuZXZlbnRSb3V0aW5nU2xpcENvbnRhaW5zKGV2ZW50LCBrZXJuZWxVcmkpKSB7XHJcbiAgICAgICAgICAgICAgICAgICAgcm91dGluZ3NsaXAuc3RhbXBFdmVudFJvdXRpbmdTbGlwKGV2ZW50LCBrZXJuZWxVcmkpO1xyXG4gICAgICAgICAgICAgICAgfVxyXG4gICAgICAgICAgICAgICAgZXZlbnQ7Ly8/XHJcbiAgICAgICAgICAgICAgICB0aGlzLnB1Ymxpc2hFdmVudChldmVudCk7XHJcbiAgICAgICAgICAgIH1cclxuICAgICAgICB9KTtcclxuXHJcbiAgICAgICAgaWYgKGFsaWFzZXMpIHtcclxuICAgICAgICAgICAgbGV0IHNldCA9IG5ldyBTZXQoYWxpYXNlcyk7XHJcblxyXG4gICAgICAgICAgICBpZiAoa2VybmVsLmtlcm5lbEluZm8uYWxpYXNlcykge1xyXG4gICAgICAgICAgICAgICAgZm9yIChsZXQgYWxpYXMgaW4ga2VybmVsLmtlcm5lbEluZm8uYWxpYXNlcykge1xyXG4gICAgICAgICAgICAgICAgICAgIHNldC5hZGQoYWxpYXMpO1xyXG4gICAgICAgICAgICAgICAgfVxyXG4gICAgICAgICAgICB9XHJcblxyXG4gICAgICAgICAgICBrZXJuZWwua2VybmVsSW5mby5hbGlhc2VzID0gQXJyYXkuZnJvbShzZXQpO1xyXG4gICAgICAgIH1cclxuXHJcbiAgICAgICAgdGhpcy5fY2hpbGRLZXJuZWxzLmFkZChrZXJuZWwsIGFsaWFzZXMpO1xyXG5cclxuICAgICAgICBjb25zdCBpbnZvY2F0aW9uQ29udGV4dCA9IEtlcm5lbEludm9jYXRpb25Db250ZXh0LmN1cnJlbnQ7XHJcblxyXG4gICAgICAgIGlmIChpbnZvY2F0aW9uQ29udGV4dCkge1xyXG4gICAgICAgICAgICBpbnZvY2F0aW9uQ29udGV4dC5jb21tYW5kRW52ZWxvcGU7Ly8/XHJcbiAgICAgICAgICAgIGludm9jYXRpb25Db250ZXh0LnB1Ymxpc2goe1xyXG4gICAgICAgICAgICAgICAgZXZlbnRUeXBlOiBjb250cmFjdHMuS2VybmVsSW5mb1Byb2R1Y2VkVHlwZSxcclxuICAgICAgICAgICAgICAgIGV2ZW50OiA8Y29udHJhY3RzLktlcm5lbEluZm9Qcm9kdWNlZD57XHJcbiAgICAgICAgICAgICAgICAgICAga2VybmVsSW5mbzoga2VybmVsLmtlcm5lbEluZm9cclxuICAgICAgICAgICAgICAgIH0sXHJcbiAgICAgICAgICAgICAgICBjb21tYW5kOiBpbnZvY2F0aW9uQ29udGV4dC5jb21tYW5kRW52ZWxvcGVcclxuICAgICAgICAgICAgfSk7XHJcbiAgICAgICAgfSBlbHNlIHtcclxuICAgICAgICAgICAgdGhpcy5wdWJsaXNoRXZlbnQoe1xyXG4gICAgICAgICAgICAgICAgZXZlbnRUeXBlOiBjb250cmFjdHMuS2VybmVsSW5mb1Byb2R1Y2VkVHlwZSxcclxuICAgICAgICAgICAgICAgIGV2ZW50OiA8Y29udHJhY3RzLktlcm5lbEluZm9Qcm9kdWNlZD57XHJcbiAgICAgICAgICAgICAgICAgICAga2VybmVsSW5mbzoga2VybmVsLmtlcm5lbEluZm9cclxuICAgICAgICAgICAgICAgIH1cclxuICAgICAgICAgICAgfSk7XHJcbiAgICAgICAgfVxyXG4gICAgfVxyXG5cclxuICAgIGZpbmRLZXJuZWxCeVVyaSh1cmk6IHN0cmluZyk6IEtlcm5lbCB8IHVuZGVmaW5lZCB7XHJcbiAgICAgICAgY29uc3Qgbm9ybWFsaXplZCA9IHJvdXRpbmdzbGlwLmNyZWF0ZUtlcm5lbFVyaSh1cmkpO1xyXG4gICAgICAgIGlmICh0aGlzLmtlcm5lbEluZm8udXJpID09PSBub3JtYWxpemVkKSB7XHJcbiAgICAgICAgICAgIHJldHVybiB0aGlzO1xyXG4gICAgICAgIH1cclxuICAgICAgICByZXR1cm4gdGhpcy5fY2hpbGRLZXJuZWxzLnRyeUdldEJ5VXJpKG5vcm1hbGl6ZWQpO1xyXG4gICAgfVxyXG5cclxuICAgIGZpbmRLZXJuZWxCeU5hbWUobmFtZTogc3RyaW5nKTogS2VybmVsIHwgdW5kZWZpbmVkIHtcclxuICAgICAgICBpZiAodGhpcy5rZXJuZWxJbmZvLmxvY2FsTmFtZSA9PT0gbmFtZSB8fCB0aGlzLmtlcm5lbEluZm8uYWxpYXNlcy5maW5kKGEgPT4gYSA9PT0gbmFtZSkpIHtcclxuICAgICAgICAgICAgcmV0dXJuIHRoaXM7XHJcbiAgICAgICAgfVxyXG4gICAgICAgIHJldHVybiB0aGlzLl9jaGlsZEtlcm5lbHMudHJ5R2V0QnlBbGlhcyhuYW1lKTtcclxuICAgIH1cclxuXHJcbiAgICBmaW5kS2VybmVscyhwcmVkaWNhdGU6IChrZXJuZWw6IEtlcm5lbCkgPT4gYm9vbGVhbik6IEtlcm5lbFtdIHtcclxuICAgICAgICB2YXIgcmVzdWx0czogS2VybmVsW10gPSBbXTtcclxuICAgICAgICBpZiAocHJlZGljYXRlKHRoaXMpKSB7XHJcbiAgICAgICAgICAgIHJlc3VsdHMucHVzaCh0aGlzKTtcclxuICAgICAgICB9XHJcbiAgICAgICAgZm9yIChsZXQga2VybmVsIG9mIHRoaXMuY2hpbGRLZXJuZWxzKSB7XHJcbiAgICAgICAgICAgIGlmIChwcmVkaWNhdGUoa2VybmVsKSkge1xyXG4gICAgICAgICAgICAgICAgcmVzdWx0cy5wdXNoKGtlcm5lbCk7XHJcbiAgICAgICAgICAgIH1cclxuICAgICAgICB9XHJcbiAgICAgICAgcmV0dXJuIHJlc3VsdHM7XHJcbiAgICB9XHJcblxyXG4gICAgZmluZEtlcm5lbChwcmVkaWNhdGU6IChrZXJuZWw6IEtlcm5lbCkgPT4gYm9vbGVhbik6IEtlcm5lbCB8IHVuZGVmaW5lZCB7XHJcbiAgICAgICAgaWYgKHByZWRpY2F0ZSh0aGlzKSkge1xyXG4gICAgICAgICAgICByZXR1cm4gdGhpcztcclxuICAgICAgICB9XHJcbiAgICAgICAgcmV0dXJuIHRoaXMuY2hpbGRLZXJuZWxzLmZpbmQocHJlZGljYXRlKTtcclxuICAgIH1cclxuXHJcbiAgICBzZXREZWZhdWx0VGFyZ2V0S2VybmVsTmFtZUZvckNvbW1hbmQoY29tbWFuZFR5cGU6IGNvbnRyYWN0cy5LZXJuZWxDb21tYW5kVHlwZSwga2VybmVsTmFtZTogc3RyaW5nKSB7XHJcbiAgICAgICAgdGhpcy5fZGVmYXVsdEtlcm5lbE5hbWVzQnlDb21tYW5kVHlwZS5zZXQoY29tbWFuZFR5cGUsIGtlcm5lbE5hbWUpO1xyXG4gICAgfVxyXG4gICAgb3ZlcnJpZGUgaGFuZGxlQ29tbWFuZChjb21tYW5kRW52ZWxvcGU6IGNvbnRyYWN0cy5LZXJuZWxDb21tYW5kRW52ZWxvcGUpOiBQcm9taXNlPHZvaWQ+IHtcclxuICAgICAgICBjb25zdCBpbnZvY2F0aW9uQ29udGV4dCA9IEtlcm5lbEludm9jYXRpb25Db250ZXh0LmN1cnJlbnQ7XHJcblxyXG4gICAgICAgIGxldCBrZXJuZWwgPSBjb21tYW5kRW52ZWxvcGUuY29tbWFuZC50YXJnZXRLZXJuZWxOYW1lID09PSB0aGlzLm5hbWVcclxuICAgICAgICAgICAgPyB0aGlzXHJcbiAgICAgICAgICAgIDogdGhpcy5nZXRIYW5kbGluZ0tlcm5lbChjb21tYW5kRW52ZWxvcGUsIGludm9jYXRpb25Db250ZXh0KTtcclxuXHJcblxyXG4gICAgICAgIGNvbnN0IHByZXZpdXNvSGFuZGxpbmdLZXJuZWwgPSBpbnZvY2F0aW9uQ29udGV4dD8uaGFuZGxpbmdLZXJuZWwgPz8gbnVsbDtcclxuXHJcbiAgICAgICAgaWYgKGtlcm5lbCA9PT0gdGhpcykge1xyXG4gICAgICAgICAgICBpZiAoaW52b2NhdGlvbkNvbnRleHQgIT09IG51bGwpIHtcclxuICAgICAgICAgICAgICAgIGludm9jYXRpb25Db250ZXh0LmhhbmRsaW5nS2VybmVsID0ga2VybmVsO1xyXG4gICAgICAgICAgICB9XHJcbiAgICAgICAgICAgIHJldHVybiBzdXBlci5oYW5kbGVDb21tYW5kKGNvbW1hbmRFbnZlbG9wZSkuZmluYWxseSgoKSA9PiB7XHJcbiAgICAgICAgICAgICAgICBpZiAoaW52b2NhdGlvbkNvbnRleHQgIT09IG51bGwpIHtcclxuICAgICAgICAgICAgICAgICAgICBpbnZvY2F0aW9uQ29udGV4dC5oYW5kbGluZ0tlcm5lbCA9IHByZXZpdXNvSGFuZGxpbmdLZXJuZWw7XHJcbiAgICAgICAgICAgICAgICB9XHJcbiAgICAgICAgICAgIH0pO1xyXG4gICAgICAgIH0gZWxzZSBpZiAoa2VybmVsKSB7XHJcbiAgICAgICAgICAgIGlmIChpbnZvY2F0aW9uQ29udGV4dCAhPT0gbnVsbCkge1xyXG4gICAgICAgICAgICAgICAgaW52b2NhdGlvbkNvbnRleHQuaGFuZGxpbmdLZXJuZWwgPSBrZXJuZWw7XHJcbiAgICAgICAgICAgIH1cclxuICAgICAgICAgICAgY29uc3Qga2VybmVsVXJpID0gZ2V0S2VybmVsVXJpKGtlcm5lbCk7XHJcbiAgICAgICAgICAgIGlmICghcm91dGluZ3NsaXAuY29tbWFuZFJvdXRpbmdTbGlwQ29udGFpbnMoY29tbWFuZEVudmVsb3BlLCBrZXJuZWxVcmkpKSB7XHJcbiAgICAgICAgICAgICAgICByb3V0aW5nc2xpcC5zdGFtcENvbW1hbmRSb3V0aW5nU2xpcEFzQXJyaXZlZChjb21tYW5kRW52ZWxvcGUsIGtlcm5lbFVyaSk7XHJcbiAgICAgICAgICAgIH0gZWxzZSB7XHJcbiAgICAgICAgICAgICAgICBcIndlIHNob3VsZCBub3QgYmUgaGVyZVwiOy8vP1xyXG4gICAgICAgICAgICB9XHJcbiAgICAgICAgICAgIHJldHVybiBrZXJuZWwuaGFuZGxlQ29tbWFuZChjb21tYW5kRW52ZWxvcGUpLmZpbmFsbHkoKCkgPT4ge1xyXG4gICAgICAgICAgICAgICAgaWYgKGludm9jYXRpb25Db250ZXh0ICE9PSBudWxsKSB7XHJcbiAgICAgICAgICAgICAgICAgICAgaW52b2NhdGlvbkNvbnRleHQuaGFuZGxpbmdLZXJuZWwgPSBwcmV2aXVzb0hhbmRsaW5nS2VybmVsO1xyXG4gICAgICAgICAgICAgICAgfVxyXG4gICAgICAgICAgICAgICAgaWYgKCFyb3V0aW5nc2xpcC5jb21tYW5kUm91dGluZ1NsaXBDb250YWlucyhjb21tYW5kRW52ZWxvcGUsIGtlcm5lbFVyaSkpIHtcclxuICAgICAgICAgICAgICAgICAgICByb3V0aW5nc2xpcC5zdGFtcENvbW1hbmRSb3V0aW5nU2xpcChjb21tYW5kRW52ZWxvcGUsIGtlcm5lbFVyaSk7XHJcbiAgICAgICAgICAgICAgICB9IGVsc2Uge1xyXG4gICAgICAgICAgICAgICAgICAgIFwid2Ugc2hvdWxkIG5vdCBiZSBoZXJlXCI7Ly8/XHJcbiAgICAgICAgICAgICAgICB9XHJcbiAgICAgICAgICAgIH0pO1xyXG4gICAgICAgIH1cclxuXHJcbiAgICAgICAgaWYgKGludm9jYXRpb25Db250ZXh0ICE9PSBudWxsKSB7XHJcbiAgICAgICAgICAgIGludm9jYXRpb25Db250ZXh0LmhhbmRsaW5nS2VybmVsID0gcHJldml1c29IYW5kbGluZ0tlcm5lbDtcclxuICAgICAgICB9XHJcbiAgICAgICAgcmV0dXJuIFByb21pc2UucmVqZWN0KG5ldyBFcnJvcihcIktlcm5lbCBub3QgZm91bmQ6IFwiICsgY29tbWFuZEVudmVsb3BlLmNvbW1hbmQudGFyZ2V0S2VybmVsTmFtZSkpO1xyXG4gICAgfVxyXG5cclxuICAgIG92ZXJyaWRlIGdldEhhbmRsaW5nS2VybmVsKGNvbW1hbmRFbnZlbG9wZTogY29udHJhY3RzLktlcm5lbENvbW1hbmRFbnZlbG9wZSwgY29udGV4dD86IEtlcm5lbEludm9jYXRpb25Db250ZXh0IHwgbnVsbCk6IEtlcm5lbCB8IG51bGwge1xyXG5cclxuICAgICAgICBsZXQga2VybmVsOiBLZXJuZWwgfCBudWxsID0gbnVsbDtcclxuICAgICAgICBpZiAoY29tbWFuZEVudmVsb3BlLmNvbW1hbmQuZGVzdGluYXRpb25VcmkpIHtcclxuICAgICAgICAgICAgY29uc3Qgbm9ybWFsaXplZCA9IHJvdXRpbmdzbGlwLmNyZWF0ZUtlcm5lbFVyaShjb21tYW5kRW52ZWxvcGUuY29tbWFuZC5kZXN0aW5hdGlvblVyaSk7XHJcbiAgICAgICAgICAgIGtlcm5lbCA9IHRoaXMuX2NoaWxkS2VybmVscy50cnlHZXRCeVVyaShub3JtYWxpemVkKSA/PyBudWxsO1xyXG4gICAgICAgICAgICBpZiAoa2VybmVsKSB7XHJcbiAgICAgICAgICAgICAgICByZXR1cm4ga2VybmVsO1xyXG4gICAgICAgICAgICB9XHJcbiAgICAgICAgfVxyXG5cclxuICAgICAgICBsZXQgdGFyZ2V0S2VybmVsTmFtZSA9IGNvbW1hbmRFbnZlbG9wZS5jb21tYW5kLnRhcmdldEtlcm5lbE5hbWU7XHJcblxyXG4gICAgICAgIGlmICh0YXJnZXRLZXJuZWxOYW1lID09PSB1bmRlZmluZWQgfHwgdGFyZ2V0S2VybmVsTmFtZSA9PT0gbnVsbCkge1xyXG4gICAgICAgICAgICBpZiAodGhpcy5jYW5IYW5kbGUoY29tbWFuZEVudmVsb3BlKSkge1xyXG4gICAgICAgICAgICAgICAgcmV0dXJuIHRoaXM7XHJcbiAgICAgICAgICAgIH1cclxuXHJcbiAgICAgICAgICAgIHRhcmdldEtlcm5lbE5hbWUgPSB0aGlzLl9kZWZhdWx0S2VybmVsTmFtZXNCeUNvbW1hbmRUeXBlLmdldChjb21tYW5kRW52ZWxvcGUuY29tbWFuZFR5cGUpID8/IHRoaXMuZGVmYXVsdEtlcm5lbE5hbWU7XHJcbiAgICAgICAgfVxyXG5cclxuICAgICAgICBpZiAodGFyZ2V0S2VybmVsTmFtZSAhPT0gdW5kZWZpbmVkICYmIHRhcmdldEtlcm5lbE5hbWUgIT09IG51bGwpIHtcclxuICAgICAgICAgICAga2VybmVsID0gdGhpcy5fY2hpbGRLZXJuZWxzLnRyeUdldEJ5QWxpYXModGFyZ2V0S2VybmVsTmFtZSkgPz8gbnVsbDtcclxuICAgICAgICB9XHJcblxyXG4gICAgICAgIGlmICh0YXJnZXRLZXJuZWxOYW1lICYmICFrZXJuZWwpIHtcclxuICAgICAgICAgICAgY29uc3QgZXJyb3JNZXNzYWdlID0gYEtlcm5lbCBub3QgZm91bmQ6ICR7dGFyZ2V0S2VybmVsTmFtZX1gO1xyXG4gICAgICAgICAgICBMb2dnZXIuZGVmYXVsdC5lcnJvcihlcnJvck1lc3NhZ2UpO1xyXG4gICAgICAgICAgICB0aHJvdyBuZXcgRXJyb3IoZXJyb3JNZXNzYWdlKTtcclxuICAgICAgICB9XHJcblxyXG4gICAgICAgIGlmICgha2VybmVsKSB7XHJcblxyXG4gICAgICAgICAgICBpZiAodGhpcy5fY2hpbGRLZXJuZWxzLmNvdW50ID09PSAxKSB7XHJcbiAgICAgICAgICAgICAgICBrZXJuZWwgPSB0aGlzLl9jaGlsZEtlcm5lbHMuc2luZ2xlKCkgPz8gbnVsbDtcclxuICAgICAgICAgICAgfVxyXG4gICAgICAgIH1cclxuXHJcbiAgICAgICAgaWYgKCFrZXJuZWwpIHtcclxuICAgICAgICAgICAga2VybmVsID0gY29udGV4dD8uaGFuZGxpbmdLZXJuZWwgPz8gbnVsbDtcclxuICAgICAgICB9XHJcbiAgICAgICAgcmV0dXJuIGtlcm5lbCA/PyB0aGlzO1xyXG5cclxuICAgIH1cclxufVxyXG5cclxuY2xhc3MgS2VybmVsQ29sbGVjdGlvbiBpbXBsZW1lbnRzIEl0ZXJhYmxlPEtlcm5lbD4ge1xyXG5cclxuICAgIHByaXZhdGUgX2NvbXBvc2l0ZUtlcm5lbDogQ29tcG9zaXRlS2VybmVsO1xyXG4gICAgcHJpdmF0ZSBfa2VybmVsczogS2VybmVsW10gPSBbXTtcclxuICAgIHByaXZhdGUgX25hbWVBbmRBbGlhc2VzQnlLZXJuZWw6IE1hcDxLZXJuZWwsIFNldDxzdHJpbmc+PiA9IG5ldyBNYXA8S2VybmVsLCBTZXQ8c3RyaW5nPj4oKTtcclxuICAgIHByaXZhdGUgX2tlcm5lbHNCeU5hbWVPckFsaWFzOiBNYXA8c3RyaW5nLCBLZXJuZWw+ID0gbmV3IE1hcDxzdHJpbmcsIEtlcm5lbD4oKTtcclxuICAgIHByaXZhdGUgX2tlcm5lbHNCeUxvY2FsVXJpOiBNYXA8c3RyaW5nLCBLZXJuZWw+ID0gbmV3IE1hcDxzdHJpbmcsIEtlcm5lbD4oKTtcclxuICAgIHByaXZhdGUgX2tlcm5lbHNCeVJlbW90ZVVyaTogTWFwPHN0cmluZywgS2VybmVsPiA9IG5ldyBNYXA8c3RyaW5nLCBLZXJuZWw+KCk7XHJcblxyXG4gICAgY29uc3RydWN0b3IoY29tcG9zaXRlS2VybmVsOiBDb21wb3NpdGVLZXJuZWwpIHtcclxuICAgICAgICB0aGlzLl9jb21wb3NpdGVLZXJuZWwgPSBjb21wb3NpdGVLZXJuZWw7XHJcbiAgICB9XHJcblxyXG4gICAgW1N5bWJvbC5pdGVyYXRvcl0oKTogSXRlcmF0b3I8S2VybmVsPiB7XHJcbiAgICAgICAgbGV0IGNvdW50ZXIgPSAwO1xyXG4gICAgICAgIHJldHVybiB7XHJcbiAgICAgICAgICAgIG5leHQ6ICgpID0+IHtcclxuICAgICAgICAgICAgICAgIHJldHVybiB7XHJcbiAgICAgICAgICAgICAgICAgICAgdmFsdWU6IHRoaXMuX2tlcm5lbHNbY291bnRlcisrXSxcclxuICAgICAgICAgICAgICAgICAgICBkb25lOiBjb3VudGVyID4gdGhpcy5fa2VybmVscy5sZW5ndGggLy8/XHJcbiAgICAgICAgICAgICAgICB9O1xyXG4gICAgICAgICAgICB9XHJcbiAgICAgICAgfTtcclxuICAgIH1cclxuXHJcbiAgICBzaW5nbGUoKTogS2VybmVsIHwgdW5kZWZpbmVkIHtcclxuICAgICAgICByZXR1cm4gdGhpcy5fa2VybmVscy5sZW5ndGggPT09IDEgPyB0aGlzLl9rZXJuZWxzWzBdIDogdW5kZWZpbmVkO1xyXG4gICAgfVxyXG5cclxuXHJcbiAgICBwdWJsaWMgYWRkKGtlcm5lbDogS2VybmVsLCBhbGlhc2VzPzogc3RyaW5nW10pOiB2b2lkIHtcclxuICAgICAgICBpZiAodGhpcy5fa2VybmVsc0J5TmFtZU9yQWxpYXMuaGFzKGtlcm5lbC5uYW1lKSkge1xyXG4gICAgICAgICAgICB0aHJvdyBuZXcgRXJyb3IoYGtlcm5lbCB3aXRoIG5hbWUgJHtrZXJuZWwubmFtZX0gYWxyZWFkeSBleGlzdHNgKTtcclxuICAgICAgICB9XHJcbiAgICAgICAgdGhpcy51cGRhdGVLZXJuZWxJbmZvQW5kSW5kZXgoa2VybmVsLCBhbGlhc2VzKTtcclxuICAgICAgICB0aGlzLl9rZXJuZWxzLnB1c2goa2VybmVsKTtcclxuICAgIH1cclxuXHJcblxyXG4gICAgZ2V0IGNvdW50KCk6IG51bWJlciB7XHJcbiAgICAgICAgcmV0dXJuIHRoaXMuX2tlcm5lbHMubGVuZ3RoO1xyXG4gICAgfVxyXG5cclxuICAgIHVwZGF0ZUtlcm5lbEluZm9BbmRJbmRleChrZXJuZWw6IEtlcm5lbCwgYWxpYXNlcz86IHN0cmluZ1tdKTogdm9pZCB7XHJcblxyXG4gICAgICAgIGlmIChhbGlhc2VzKSB7XHJcbiAgICAgICAgICAgIGZvciAobGV0IGFsaWFzIG9mIGFsaWFzZXMpIHtcclxuICAgICAgICAgICAgICAgIGlmICh0aGlzLl9rZXJuZWxzQnlOYW1lT3JBbGlhcy5oYXMoYWxpYXMpKSB7XHJcbiAgICAgICAgICAgICAgICAgICAgdGhyb3cgbmV3IEVycm9yKGBrZXJuZWwgd2l0aCBhbGlhcyAke2FsaWFzfSBhbHJlYWR5IGV4aXN0c2ApO1xyXG4gICAgICAgICAgICAgICAgfVxyXG4gICAgICAgICAgICB9XHJcbiAgICAgICAgfVxyXG5cclxuICAgICAgICBpZiAoIXRoaXMuX25hbWVBbmRBbGlhc2VzQnlLZXJuZWwuaGFzKGtlcm5lbCkpIHtcclxuXHJcbiAgICAgICAgICAgIGxldCBzZXQgPSBuZXcgU2V0PHN0cmluZz4oKTtcclxuXHJcbiAgICAgICAgICAgIGZvciAobGV0IGFsaWFzIG9mIGtlcm5lbC5rZXJuZWxJbmZvLmFsaWFzZXMpIHtcclxuICAgICAgICAgICAgICAgIHNldC5hZGQoYWxpYXMpO1xyXG4gICAgICAgICAgICB9XHJcblxyXG4gICAgICAgICAgICBrZXJuZWwua2VybmVsSW5mby5hbGlhc2VzID0gQXJyYXkuZnJvbShzZXQpO1xyXG5cclxuICAgICAgICAgICAgc2V0LmFkZChrZXJuZWwua2VybmVsSW5mby5sb2NhbE5hbWUpO1xyXG5cclxuICAgICAgICAgICAgdGhpcy5fbmFtZUFuZEFsaWFzZXNCeUtlcm5lbC5zZXQoa2VybmVsLCBzZXQpO1xyXG4gICAgICAgIH1cclxuICAgICAgICBpZiAoYWxpYXNlcykge1xyXG4gICAgICAgICAgICBmb3IgKGxldCBhbGlhcyBvZiBhbGlhc2VzKSB7XHJcbiAgICAgICAgICAgICAgICB0aGlzLl9uYW1lQW5kQWxpYXNlc0J5S2VybmVsLmdldChrZXJuZWwpIS5hZGQoYWxpYXMpO1xyXG4gICAgICAgICAgICB9XHJcbiAgICAgICAgfVxyXG5cclxuICAgICAgICB0aGlzLl9uYW1lQW5kQWxpYXNlc0J5S2VybmVsLmdldChrZXJuZWwpPy5mb3JFYWNoKGFsaWFzID0+IHtcclxuICAgICAgICAgICAgdGhpcy5fa2VybmVsc0J5TmFtZU9yQWxpYXMuc2V0KGFsaWFzLCBrZXJuZWwpO1xyXG4gICAgICAgIH0pO1xyXG5cclxuICAgICAgICBsZXQgYmFzZVVyaSA9IHRoaXMuX2NvbXBvc2l0ZUtlcm5lbC5ob3N0Py51cmkgfHwgdGhpcy5fY29tcG9zaXRlS2VybmVsLmtlcm5lbEluZm8udXJpO1xyXG5cclxuICAgICAgICBpZiAoIWJhc2VVcmkhLmVuZHNXaXRoKFwiL1wiKSkge1xyXG4gICAgICAgICAgICBiYXNlVXJpICs9IFwiL1wiO1xyXG5cclxuICAgICAgICB9XHJcbiAgICAgICAga2VybmVsLmtlcm5lbEluZm8udXJpID0gcm91dGluZ3NsaXAuY3JlYXRlS2VybmVsVXJpKGAke2Jhc2VVcml9JHtrZXJuZWwua2VybmVsSW5mby5sb2NhbE5hbWV9YCk7Ly8/XHJcbiAgICAgICAgdGhpcy5fa2VybmVsc0J5TG9jYWxVcmkuc2V0KGtlcm5lbC5rZXJuZWxJbmZvLnVyaSwga2VybmVsKTtcclxuXHJcblxyXG4gICAgICAgIGlmIChrZXJuZWwua2VybmVsVHlwZSA9PT0gS2VybmVsVHlwZS5wcm94eSkge1xyXG4gICAgICAgICAgICB0aGlzLl9rZXJuZWxzQnlSZW1vdGVVcmkuc2V0KGtlcm5lbC5rZXJuZWxJbmZvLnJlbW90ZVVyaSEsIGtlcm5lbCk7XHJcbiAgICAgICAgfVxyXG4gICAgfVxyXG5cclxuICAgIHB1YmxpYyB0cnlHZXRCeUFsaWFzKGFsaWFzOiBzdHJpbmcpOiBLZXJuZWwgfCB1bmRlZmluZWQge1xyXG4gICAgICAgIHJldHVybiB0aGlzLl9rZXJuZWxzQnlOYW1lT3JBbGlhcy5nZXQoYWxpYXMpO1xyXG4gICAgfVxyXG5cclxuICAgIHB1YmxpYyB0cnlHZXRCeVVyaSh1cmk6IHN0cmluZyk6IEtlcm5lbCB8IHVuZGVmaW5lZCB7XHJcbiAgICAgICAgbGV0IGtlcm5lbCA9IHRoaXMuX2tlcm5lbHNCeUxvY2FsVXJpLmdldCh1cmkpIHx8IHRoaXMuX2tlcm5lbHNCeVJlbW90ZVVyaS5nZXQodXJpKTtcclxuICAgICAgICByZXR1cm4ga2VybmVsO1xyXG4gICAgfVxyXG5cclxuICAgIG5vdGlmeVRoYXRIb3N0V2FzU2V0KCkge1xyXG4gICAgICAgIGZvciAobGV0IGtlcm5lbCBvZiB0aGlzLl9rZXJuZWxzKSB7XHJcbiAgICAgICAgICAgIHRoaXMudXBkYXRlS2VybmVsSW5mb0FuZEluZGV4KGtlcm5lbCk7XHJcbiAgICAgICAgfVxyXG4gICAgfVxyXG59XHJcbiIsIi8vIENvcHlyaWdodCAoYykgLk5FVCBGb3VuZGF0aW9uIGFuZCBjb250cmlidXRvcnMuIEFsbCByaWdodHMgcmVzZXJ2ZWQuXHJcbi8vIExpY2Vuc2VkIHVuZGVyIHRoZSBNSVQgbGljZW5zZS4gU2VlIExJQ0VOU0UgZmlsZSBpbiB0aGUgcHJvamVjdCByb290IGZvciBmdWxsIGxpY2Vuc2UgaW5mb3JtYXRpb24uXHJcblxyXG5pbXBvcnQgKiBhcyB1dGlsIGZyb20gXCJ1dGlsXCI7XHJcbmltcG9ydCAqIGFzIGNvbnRyYWN0cyBmcm9tIFwiLi9jb250cmFjdHNcIjtcclxuaW1wb3J0IHsgS2VybmVsSW52b2NhdGlvbkNvbnRleHQgfSBmcm9tIFwiLi9rZXJuZWxJbnZvY2F0aW9uQ29udGV4dFwiO1xyXG5pbXBvcnQgKiBhcyBkaXNwb3NhYmxlcyBmcm9tIFwiLi9kaXNwb3NhYmxlc1wiO1xyXG5cclxuZXhwb3J0IGNsYXNzIENvbnNvbGVDYXB0dXJlIGltcGxlbWVudHMgZGlzcG9zYWJsZXMuRGlzcG9zYWJsZSB7XHJcbiAgICBwcml2YXRlIG9yaWdpbmFsQ29uc29sZTogQ29uc29sZTtcclxuICAgIHByaXZhdGUgX2tlcm5lbEludm9jYXRpb25Db250ZXh0OiBLZXJuZWxJbnZvY2F0aW9uQ29udGV4dCB8IHVuZGVmaW5lZDtcclxuXHJcbiAgICBjb25zdHJ1Y3RvcigpIHtcclxuICAgICAgICB0aGlzLm9yaWdpbmFsQ29uc29sZSA9IGNvbnNvbGU7XHJcbiAgICAgICAgY29uc29sZSA9IDxDb25zb2xlPjxhbnk+dGhpcztcclxuICAgIH1cclxuXHJcbiAgICBzZXQga2VybmVsSW52b2NhdGlvbkNvbnRleHQodmFsdWU6IEtlcm5lbEludm9jYXRpb25Db250ZXh0IHwgdW5kZWZpbmVkKSB7XHJcbiAgICAgICAgdGhpcy5fa2VybmVsSW52b2NhdGlvbkNvbnRleHQgPSB2YWx1ZTtcclxuICAgIH1cclxuXHJcbiAgICBhc3NlcnQodmFsdWU6IGFueSwgbWVzc2FnZT86IHN0cmluZywgLi4ub3B0aW9uYWxQYXJhbXM6IGFueVtdKTogdm9pZCB7XHJcbiAgICAgICAgdGhpcy5vcmlnaW5hbENvbnNvbGUuYXNzZXJ0KHZhbHVlLCBtZXNzYWdlLCBvcHRpb25hbFBhcmFtcyk7XHJcbiAgICB9XHJcbiAgICBjbGVhcigpOiB2b2lkIHtcclxuICAgICAgICB0aGlzLm9yaWdpbmFsQ29uc29sZS5jbGVhcigpO1xyXG4gICAgfVxyXG4gICAgY291bnQobGFiZWw/OiBhbnkpOiB2b2lkIHtcclxuICAgICAgICB0aGlzLm9yaWdpbmFsQ29uc29sZS5jb3VudChsYWJlbCk7XHJcbiAgICB9XHJcbiAgICBjb3VudFJlc2V0KGxhYmVsPzogc3RyaW5nKTogdm9pZCB7XHJcbiAgICAgICAgdGhpcy5vcmlnaW5hbENvbnNvbGUuY291bnRSZXNldChsYWJlbCk7XHJcbiAgICB9XHJcbiAgICBkZWJ1ZyhtZXNzYWdlPzogYW55LCAuLi5vcHRpb25hbFBhcmFtczogYW55W10pOiB2b2lkIHtcclxuICAgICAgICB0aGlzLm9yaWdpbmFsQ29uc29sZS5kZWJ1ZyhtZXNzYWdlLCBvcHRpb25hbFBhcmFtcyk7XHJcbiAgICB9XHJcbiAgICBkaXIob2JqOiBhbnksIG9wdGlvbnM/OiB1dGlsLkluc3BlY3RPcHRpb25zKTogdm9pZCB7XHJcbiAgICAgICAgdGhpcy5vcmlnaW5hbENvbnNvbGUuZGlyKG9iaiwgb3B0aW9ucyk7XHJcbiAgICB9XHJcbiAgICBkaXJ4bWwoLi4uZGF0YTogYW55W10pOiB2b2lkIHtcclxuICAgICAgICB0aGlzLm9yaWdpbmFsQ29uc29sZS5kaXJ4bWwoZGF0YSk7XHJcbiAgICB9XHJcbiAgICBlcnJvcihtZXNzYWdlPzogYW55LCAuLi5vcHRpb25hbFBhcmFtczogYW55W10pOiB2b2lkIHtcclxuICAgICAgICB0aGlzLnJlZGlyZWN0QW5kUHVibGlzaCh0aGlzLm9yaWdpbmFsQ29uc29sZS5lcnJvciwgLi4uW21lc3NhZ2UsIC4uLm9wdGlvbmFsUGFyYW1zXSk7XHJcbiAgICB9XHJcblxyXG4gICAgZ3JvdXAoLi4ubGFiZWw6IGFueVtdKTogdm9pZCB7XHJcbiAgICAgICAgdGhpcy5vcmlnaW5hbENvbnNvbGUuZ3JvdXAobGFiZWwpO1xyXG4gICAgfVxyXG4gICAgZ3JvdXBDb2xsYXBzZWQoLi4ubGFiZWw6IGFueVtdKTogdm9pZCB7XHJcbiAgICAgICAgdGhpcy5vcmlnaW5hbENvbnNvbGUuZ3JvdXBDb2xsYXBzZWQobGFiZWwpO1xyXG4gICAgfVxyXG4gICAgZ3JvdXBFbmQoKTogdm9pZCB7XHJcbiAgICAgICAgdGhpcy5vcmlnaW5hbENvbnNvbGUuZ3JvdXBFbmQoKTtcclxuICAgIH1cclxuICAgIGluZm8obWVzc2FnZT86IGFueSwgLi4ub3B0aW9uYWxQYXJhbXM6IGFueVtdKTogdm9pZCB7XHJcbiAgICAgICAgdGhpcy5yZWRpcmVjdEFuZFB1Ymxpc2godGhpcy5vcmlnaW5hbENvbnNvbGUuaW5mbywgLi4uW21lc3NhZ2UsIC4uLm9wdGlvbmFsUGFyYW1zXSk7XHJcbiAgICB9XHJcbiAgICBsb2cobWVzc2FnZT86IGFueSwgLi4ub3B0aW9uYWxQYXJhbXM6IGFueVtdKTogdm9pZCB7XHJcbiAgICAgICAgdGhpcy5yZWRpcmVjdEFuZFB1Ymxpc2godGhpcy5vcmlnaW5hbENvbnNvbGUubG9nLCAuLi5bbWVzc2FnZSwgLi4ub3B0aW9uYWxQYXJhbXNdKTtcclxuICAgIH1cclxuXHJcbiAgICB0YWJsZSh0YWJ1bGFyRGF0YTogYW55LCBwcm9wZXJ0aWVzPzogc3RyaW5nW10pOiB2b2lkIHtcclxuICAgICAgICB0aGlzLm9yaWdpbmFsQ29uc29sZS50YWJsZSh0YWJ1bGFyRGF0YSwgcHJvcGVydGllcyk7XHJcbiAgICB9XHJcbiAgICB0aW1lKGxhYmVsPzogc3RyaW5nKTogdm9pZCB7XHJcbiAgICAgICAgdGhpcy5vcmlnaW5hbENvbnNvbGUudGltZShsYWJlbCk7XHJcbiAgICB9XHJcbiAgICB0aW1lRW5kKGxhYmVsPzogc3RyaW5nKTogdm9pZCB7XHJcbiAgICAgICAgdGhpcy5vcmlnaW5hbENvbnNvbGUudGltZUVuZChsYWJlbCk7XHJcbiAgICB9XHJcbiAgICB0aW1lTG9nKGxhYmVsPzogc3RyaW5nLCAuLi5kYXRhOiBhbnlbXSk6IHZvaWQge1xyXG4gICAgICAgIHRoaXMub3JpZ2luYWxDb25zb2xlLnRpbWVMb2cobGFiZWwsIGRhdGEpO1xyXG4gICAgfVxyXG4gICAgdGltZVN0YW1wKGxhYmVsPzogc3RyaW5nKTogdm9pZCB7XHJcbiAgICAgICAgdGhpcy5vcmlnaW5hbENvbnNvbGUudGltZVN0YW1wKGxhYmVsKTtcclxuICAgIH1cclxuICAgIHRyYWNlKG1lc3NhZ2U/OiBhbnksIC4uLm9wdGlvbmFsUGFyYW1zOiBhbnlbXSk6IHZvaWQge1xyXG4gICAgICAgIHRoaXMucmVkaXJlY3RBbmRQdWJsaXNoKHRoaXMub3JpZ2luYWxDb25zb2xlLnRyYWNlLCAuLi5bbWVzc2FnZSwgLi4ub3B0aW9uYWxQYXJhbXNdKTtcclxuICAgIH1cclxuICAgIHdhcm4obWVzc2FnZT86IGFueSwgLi4ub3B0aW9uYWxQYXJhbXM6IGFueVtdKTogdm9pZCB7XHJcbiAgICAgICAgdGhpcy5vcmlnaW5hbENvbnNvbGUud2FybihtZXNzYWdlLCBvcHRpb25hbFBhcmFtcyk7XHJcbiAgICB9XHJcblxyXG4gICAgcHJvZmlsZShsYWJlbD86IHN0cmluZyk6IHZvaWQge1xyXG4gICAgICAgIHRoaXMub3JpZ2luYWxDb25zb2xlLnByb2ZpbGUobGFiZWwpO1xyXG4gICAgfVxyXG4gICAgcHJvZmlsZUVuZChsYWJlbD86IHN0cmluZyk6IHZvaWQge1xyXG4gICAgICAgIHRoaXMub3JpZ2luYWxDb25zb2xlLnByb2ZpbGVFbmQobGFiZWwpO1xyXG4gICAgfVxyXG5cclxuICAgIGRpc3Bvc2UoKTogdm9pZCB7XHJcbiAgICAgICAgY29uc29sZSA9IHRoaXMub3JpZ2luYWxDb25zb2xlO1xyXG4gICAgfVxyXG5cclxuICAgIHByaXZhdGUgcmVkaXJlY3RBbmRQdWJsaXNoKHRhcmdldDogKC4uLmFyZ3M6IGFueVtdKSA9PiB2b2lkLCAuLi5hcmdzOiBhbnlbXSkge1xyXG4gICAgICAgIGlmICh0aGlzLl9rZXJuZWxJbnZvY2F0aW9uQ29udGV4dCkge1xyXG4gICAgICAgICAgICBmb3IgKGNvbnN0IGFyZyBvZiBhcmdzKSB7XHJcbiAgICAgICAgICAgICAgICBsZXQgbWltZVR5cGU6IHN0cmluZztcclxuICAgICAgICAgICAgICAgIGxldCB2YWx1ZTogc3RyaW5nO1xyXG4gICAgICAgICAgICAgICAgaWYgKHR5cGVvZiBhcmcgIT09ICdvYmplY3QnICYmICFBcnJheS5pc0FycmF5KGFyZykpIHtcclxuICAgICAgICAgICAgICAgICAgICBtaW1lVHlwZSA9ICd0ZXh0L3BsYWluJztcclxuICAgICAgICAgICAgICAgICAgICB2YWx1ZSA9IGFyZz8udG9TdHJpbmcoKTtcclxuICAgICAgICAgICAgICAgIH0gZWxzZSB7XHJcbiAgICAgICAgICAgICAgICAgICAgbWltZVR5cGUgPSAnYXBwbGljYXRpb24vanNvbic7XHJcbiAgICAgICAgICAgICAgICAgICAgdmFsdWUgPSBKU09OLnN0cmluZ2lmeShhcmcpO1xyXG4gICAgICAgICAgICAgICAgfVxyXG5cclxuICAgICAgICAgICAgICAgIGNvbnN0IGRpc3BsYXllZFZhbHVlOiBjb250cmFjdHMuRGlzcGxheWVkVmFsdWVQcm9kdWNlZCA9IHtcclxuICAgICAgICAgICAgICAgICAgICBmb3JtYXR0ZWRWYWx1ZXM6IFtcclxuICAgICAgICAgICAgICAgICAgICAgICAge1xyXG4gICAgICAgICAgICAgICAgICAgICAgICAgICAgbWltZVR5cGUsXHJcbiAgICAgICAgICAgICAgICAgICAgICAgICAgICB2YWx1ZSxcclxuICAgICAgICAgICAgICAgICAgICAgICAgfVxyXG4gICAgICAgICAgICAgICAgICAgIF1cclxuICAgICAgICAgICAgICAgIH07XHJcbiAgICAgICAgICAgICAgICBjb25zdCBldmVudEVudmVsb3BlOiBjb250cmFjdHMuS2VybmVsRXZlbnRFbnZlbG9wZSA9IHtcclxuICAgICAgICAgICAgICAgICAgICBldmVudFR5cGU6IGNvbnRyYWN0cy5EaXNwbGF5ZWRWYWx1ZVByb2R1Y2VkVHlwZSxcclxuICAgICAgICAgICAgICAgICAgICBldmVudDogZGlzcGxheWVkVmFsdWUsXHJcbiAgICAgICAgICAgICAgICAgICAgY29tbWFuZDogdGhpcy5fa2VybmVsSW52b2NhdGlvbkNvbnRleHQuY29tbWFuZEVudmVsb3BlXHJcbiAgICAgICAgICAgICAgICB9O1xyXG5cclxuICAgICAgICAgICAgICAgIHRoaXMuX2tlcm5lbEludm9jYXRpb25Db250ZXh0LnB1Ymxpc2goZXZlbnRFbnZlbG9wZSk7XHJcbiAgICAgICAgICAgIH1cclxuICAgICAgICB9XHJcbiAgICAgICAgaWYgKHRhcmdldCkge1xyXG4gICAgICAgICAgICB0YXJnZXQoLi4uYXJncyk7XHJcbiAgICAgICAgfVxyXG4gICAgfVxyXG59IiwiLy8gQ29weXJpZ2h0IChjKSAuTkVUIEZvdW5kYXRpb24gYW5kIGNvbnRyaWJ1dG9ycy4gQWxsIHJpZ2h0cyByZXNlcnZlZC5cclxuLy8gTGljZW5zZWQgdW5kZXIgdGhlIE1JVCBsaWNlbnNlLiBTZWUgTElDRU5TRSBmaWxlIGluIHRoZSBwcm9qZWN0IHJvb3QgZm9yIGZ1bGwgbGljZW5zZSBpbmZvcm1hdGlvbi5cclxuXHJcbmltcG9ydCAqIGFzIGNvbnRyYWN0cyBmcm9tIFwiLi9jb250cmFjdHNcIjtcclxuaW1wb3J0IHsgQ29uc29sZUNhcHR1cmUgfSBmcm9tIFwiLi9jb25zb2xlQ2FwdHVyZVwiO1xyXG5pbXBvcnQgeyBLZXJuZWwsIElLZXJuZWxDb21tYW5kSW52b2NhdGlvbiB9IGZyb20gXCIuL2tlcm5lbFwiO1xyXG5pbXBvcnQgeyBMb2dnZXIgfSBmcm9tIFwiLi9sb2dnZXJcIjtcclxuXHJcbmV4cG9ydCBjbGFzcyBKYXZhc2NyaXB0S2VybmVsIGV4dGVuZHMgS2VybmVsIHtcclxuICAgIHByaXZhdGUgc3VwcHJlc3NlZExvY2FsczogU2V0PHN0cmluZz47XHJcbiAgICBwcml2YXRlIGNhcHR1cmU6IENvbnNvbGVDYXB0dXJlO1xyXG5cclxuICAgIGNvbnN0cnVjdG9yKG5hbWU/OiBzdHJpbmcpIHtcclxuICAgICAgICBzdXBlcihuYW1lID8/IFwiamF2YXNjcmlwdFwiLCBcIkphdmFTY3JpcHRcIik7XHJcbiAgICAgICAgdGhpcy5zdXBwcmVzc2VkTG9jYWxzID0gbmV3IFNldDxzdHJpbmc+KHRoaXMuYWxsTG9jYWxWYXJpYWJsZU5hbWVzKCkpO1xyXG4gICAgICAgIHRoaXMucmVnaXN0ZXJDb21tYW5kSGFuZGxlcih7IGNvbW1hbmRUeXBlOiBjb250cmFjdHMuU3VibWl0Q29kZVR5cGUsIGhhbmRsZTogaW52b2NhdGlvbiA9PiB0aGlzLmhhbmRsZVN1Ym1pdENvZGUoaW52b2NhdGlvbikgfSk7XHJcbiAgICAgICAgdGhpcy5yZWdpc3RlckNvbW1hbmRIYW5kbGVyKHsgY29tbWFuZFR5cGU6IGNvbnRyYWN0cy5SZXF1ZXN0VmFsdWVJbmZvc1R5cGUsIGhhbmRsZTogaW52b2NhdGlvbiA9PiB0aGlzLmhhbmRsZVJlcXVlc3RWYWx1ZUluZm9zKGludm9jYXRpb24pIH0pO1xyXG4gICAgICAgIHRoaXMucmVnaXN0ZXJDb21tYW5kSGFuZGxlcih7IGNvbW1hbmRUeXBlOiBjb250cmFjdHMuUmVxdWVzdFZhbHVlVHlwZSwgaGFuZGxlOiBpbnZvY2F0aW9uID0+IHRoaXMuaGFuZGxlUmVxdWVzdFZhbHVlKGludm9jYXRpb24pIH0pO1xyXG4gICAgICAgIHRoaXMucmVnaXN0ZXJDb21tYW5kSGFuZGxlcih7IGNvbW1hbmRUeXBlOiBjb250cmFjdHMuU2VuZFZhbHVlVHlwZSwgaGFuZGxlOiBpbnZvY2F0aW9uID0+IHRoaXMuaGFuZGxlU2VuZFZhbHVlKGludm9jYXRpb24pIH0pO1xyXG5cclxuICAgICAgICB0aGlzLmNhcHR1cmUgPSBuZXcgQ29uc29sZUNhcHR1cmUoKTtcclxuICAgIH1cclxuXHJcbiAgICBwcml2YXRlIGhhbmRsZVNlbmRWYWx1ZShpbnZvY2F0aW9uOiBJS2VybmVsQ29tbWFuZEludm9jYXRpb24pOiBQcm9taXNlPHZvaWQ+IHtcclxuICAgICAgICBjb25zdCBzZW5kVmFsdWUgPSA8Y29udHJhY3RzLlNlbmRWYWx1ZT5pbnZvY2F0aW9uLmNvbW1hbmRFbnZlbG9wZS5jb21tYW5kO1xyXG4gICAgICAgIGlmIChzZW5kVmFsdWUuZm9ybWF0dGVkVmFsdWUpIHtcclxuICAgICAgICAgICAgc3dpdGNoIChzZW5kVmFsdWUuZm9ybWF0dGVkVmFsdWUubWltZVR5cGUpIHtcclxuICAgICAgICAgICAgICAgIGNhc2UgJ2FwcGxpY2F0aW9uL2pzb24nOlxyXG4gICAgICAgICAgICAgICAgICAgICg8YW55Pmdsb2JhbFRoaXMpW3NlbmRWYWx1ZS5uYW1lXSA9IEpTT04ucGFyc2Uoc2VuZFZhbHVlLmZvcm1hdHRlZFZhbHVlLnZhbHVlKTtcclxuICAgICAgICAgICAgICAgICAgICBicmVhaztcclxuICAgICAgICAgICAgICAgIGRlZmF1bHQ6XHJcbiAgICAgICAgICAgICAgICAgICAgdGhyb3cgbmV3IEVycm9yKGBtaW1ldHlwZSAke3NlbmRWYWx1ZS5mb3JtYXR0ZWRWYWx1ZS5taW1lVHlwZX0gbm90IHN1cHBvcnRlZGApO1xyXG4gICAgICAgICAgICB9XHJcbiAgICAgICAgICAgIHJldHVybiBQcm9taXNlLnJlc29sdmUoKTtcclxuICAgICAgICB9XHJcbiAgICAgICAgdGhyb3cgbmV3IEVycm9yKFwiZm9ybWF0dGVkVmFsdWUgaXMgcmVxdWlyZWRcIik7XHJcbiAgICB9XHJcblxyXG4gICAgcHJpdmF0ZSBhc3luYyBoYW5kbGVTdWJtaXRDb2RlKGludm9jYXRpb246IElLZXJuZWxDb21tYW5kSW52b2NhdGlvbik6IFByb21pc2U8dm9pZD4ge1xyXG4gICAgICAgIGNvbnN0IHN1Ym1pdENvZGUgPSA8Y29udHJhY3RzLlN1Ym1pdENvZGU+aW52b2NhdGlvbi5jb21tYW5kRW52ZWxvcGUuY29tbWFuZDtcclxuICAgICAgICBjb25zdCBjb2RlID0gc3VibWl0Q29kZS5jb2RlO1xyXG5cclxuICAgICAgICBzdXBlci5rZXJuZWxJbmZvLmxvY2FsTmFtZTsvLz9cclxuICAgICAgICBzdXBlci5rZXJuZWxJbmZvLnVyaTsvLz9cclxuICAgICAgICBzdXBlci5rZXJuZWxJbmZvLnJlbW90ZVVyaTsvLz9cclxuICAgICAgICBpbnZvY2F0aW9uLmNvbnRleHQucHVibGlzaCh7IGV2ZW50VHlwZTogY29udHJhY3RzLkNvZGVTdWJtaXNzaW9uUmVjZWl2ZWRUeXBlLCBldmVudDogeyBjb2RlIH0sIGNvbW1hbmQ6IGludm9jYXRpb24uY29tbWFuZEVudmVsb3BlIH0pO1xyXG4gICAgICAgIGludm9jYXRpb24uY29udGV4dC5jb21tYW5kRW52ZWxvcGUucm91dGluZ1NsaXA7Ly8/XHJcbiAgICAgICAgdGhpcy5jYXB0dXJlLmtlcm5lbEludm9jYXRpb25Db250ZXh0ID0gaW52b2NhdGlvbi5jb250ZXh0O1xyXG4gICAgICAgIGxldCByZXN1bHQ6IGFueSA9IHVuZGVmaW5lZDtcclxuXHJcbiAgICAgICAgdHJ5IHtcclxuICAgICAgICAgICAgY29uc3QgQXN5bmNGdW5jdGlvbiA9IGV2YWwoYE9iamVjdC5nZXRQcm90b3R5cGVPZihhc3luYyBmdW5jdGlvbigpe30pLmNvbnN0cnVjdG9yYCk7XHJcbiAgICAgICAgICAgIGNvbnN0IGV2YWx1YXRvciA9IEFzeW5jRnVuY3Rpb24oXCJjb25zb2xlXCIsIGNvZGUpO1xyXG4gICAgICAgICAgICByZXN1bHQgPSBhd2FpdCBldmFsdWF0b3IodGhpcy5jYXB0dXJlKTtcclxuICAgICAgICAgICAgaWYgKHJlc3VsdCAhPT0gdW5kZWZpbmVkKSB7XHJcbiAgICAgICAgICAgICAgICBjb25zdCBmb3JtYXR0ZWRWYWx1ZSA9IGZvcm1hdFZhbHVlKHJlc3VsdCwgJ2FwcGxpY2F0aW9uL2pzb24nKTtcclxuICAgICAgICAgICAgICAgIGNvbnN0IGV2ZW50OiBjb250cmFjdHMuUmV0dXJuVmFsdWVQcm9kdWNlZCA9IHtcclxuICAgICAgICAgICAgICAgICAgICBmb3JtYXR0ZWRWYWx1ZXM6IFtmb3JtYXR0ZWRWYWx1ZV1cclxuICAgICAgICAgICAgICAgIH07XHJcbiAgICAgICAgICAgICAgICBpbnZvY2F0aW9uLmNvbnRleHQucHVibGlzaCh7IGV2ZW50VHlwZTogY29udHJhY3RzLlJldHVyblZhbHVlUHJvZHVjZWRUeXBlLCBldmVudCwgY29tbWFuZDogaW52b2NhdGlvbi5jb21tYW5kRW52ZWxvcGUgfSk7XHJcbiAgICAgICAgICAgIH1cclxuICAgICAgICB9IGNhdGNoIChlKSB7XHJcbiAgICAgICAgICAgIHRocm93IGU7Ly8/XHJcbiAgICAgICAgfVxyXG4gICAgICAgIGZpbmFsbHkge1xyXG4gICAgICAgICAgICB0aGlzLmNhcHR1cmUua2VybmVsSW52b2NhdGlvbkNvbnRleHQgPSB1bmRlZmluZWQ7XHJcbiAgICAgICAgfVxyXG4gICAgfVxyXG5cclxuICAgIHByaXZhdGUgaGFuZGxlUmVxdWVzdFZhbHVlSW5mb3MoaW52b2NhdGlvbjogSUtlcm5lbENvbW1hbmRJbnZvY2F0aW9uKTogUHJvbWlzZTx2b2lkPiB7XHJcbiAgICAgICAgY29uc3QgdmFsdWVJbmZvczogY29udHJhY3RzLktlcm5lbFZhbHVlSW5mb1tdID0gdGhpcy5hbGxMb2NhbFZhcmlhYmxlTmFtZXMoKS5maWx0ZXIodiA9PiAhdGhpcy5zdXBwcmVzc2VkTG9jYWxzLmhhcyh2KSkubWFwKHYgPT4gKHsgbmFtZTogdiwgcHJlZmVycmVkTWltZVR5cGVzOiBbXSB9KSk7XHJcbiAgICAgICAgY29uc3QgZXZlbnQ6IGNvbnRyYWN0cy5WYWx1ZUluZm9zUHJvZHVjZWQgPSB7XHJcbiAgICAgICAgICAgIHZhbHVlSW5mb3NcclxuICAgICAgICB9O1xyXG4gICAgICAgIGludm9jYXRpb24uY29udGV4dC5wdWJsaXNoKHsgZXZlbnRUeXBlOiBjb250cmFjdHMuVmFsdWVJbmZvc1Byb2R1Y2VkVHlwZSwgZXZlbnQsIGNvbW1hbmQ6IGludm9jYXRpb24uY29tbWFuZEVudmVsb3BlIH0pO1xyXG4gICAgICAgIHJldHVybiBQcm9taXNlLnJlc29sdmUoKTtcclxuICAgIH1cclxuXHJcbiAgICBwcml2YXRlIGhhbmRsZVJlcXVlc3RWYWx1ZShpbnZvY2F0aW9uOiBJS2VybmVsQ29tbWFuZEludm9jYXRpb24pOiBQcm9taXNlPHZvaWQ+IHtcclxuICAgICAgICBjb25zdCByZXF1ZXN0VmFsdWUgPSA8Y29udHJhY3RzLlJlcXVlc3RWYWx1ZT5pbnZvY2F0aW9uLmNvbW1hbmRFbnZlbG9wZS5jb21tYW5kO1xyXG4gICAgICAgIGNvbnN0IHJhd1ZhbHVlID0gdGhpcy5nZXRMb2NhbFZhcmlhYmxlKHJlcXVlc3RWYWx1ZS5uYW1lKTtcclxuICAgICAgICBjb25zdCBmb3JtYXR0ZWRWYWx1ZSA9IGZvcm1hdFZhbHVlKHJhd1ZhbHVlLCByZXF1ZXN0VmFsdWUubWltZVR5cGUgfHwgJ2FwcGxpY2F0aW9uL2pzb24nKTtcclxuICAgICAgICBMb2dnZXIuZGVmYXVsdC5pbmZvKGByZXR1cm5pbmcgJHtKU09OLnN0cmluZ2lmeShmb3JtYXR0ZWRWYWx1ZSl9IGZvciAke3JlcXVlc3RWYWx1ZS5uYW1lfWApO1xyXG4gICAgICAgIGNvbnN0IGV2ZW50OiBjb250cmFjdHMuVmFsdWVQcm9kdWNlZCA9IHtcclxuICAgICAgICAgICAgbmFtZTogcmVxdWVzdFZhbHVlLm5hbWUsXHJcbiAgICAgICAgICAgIGZvcm1hdHRlZFZhbHVlXHJcbiAgICAgICAgfTtcclxuICAgICAgICBpbnZvY2F0aW9uLmNvbnRleHQucHVibGlzaCh7IGV2ZW50VHlwZTogY29udHJhY3RzLlZhbHVlUHJvZHVjZWRUeXBlLCBldmVudCwgY29tbWFuZDogaW52b2NhdGlvbi5jb21tYW5kRW52ZWxvcGUgfSk7XHJcbiAgICAgICAgcmV0dXJuIFByb21pc2UucmVzb2x2ZSgpO1xyXG4gICAgfVxyXG5cclxuICAgIHB1YmxpYyBhbGxMb2NhbFZhcmlhYmxlTmFtZXMoKTogc3RyaW5nW10ge1xyXG4gICAgICAgIGNvbnN0IHJlc3VsdDogc3RyaW5nW10gPSBbXTtcclxuICAgICAgICB0cnkge1xyXG4gICAgICAgICAgICBmb3IgKGNvbnN0IGtleSBpbiBnbG9iYWxUaGlzKSB7XHJcbiAgICAgICAgICAgICAgICB0cnkge1xyXG4gICAgICAgICAgICAgICAgICAgIGlmICh0eXBlb2YgKDxhbnk+Z2xvYmFsVGhpcylba2V5XSAhPT0gJ2Z1bmN0aW9uJykge1xyXG4gICAgICAgICAgICAgICAgICAgICAgICByZXN1bHQucHVzaChrZXkpO1xyXG4gICAgICAgICAgICAgICAgICAgIH1cclxuICAgICAgICAgICAgICAgIH0gY2F0Y2ggKGUpIHtcclxuICAgICAgICAgICAgICAgICAgICBMb2dnZXIuZGVmYXVsdC5lcnJvcihgZXJyb3IgZ2V0dGluZyB2YWx1ZSBmb3IgJHtrZXl9IDogJHtlfWApO1xyXG4gICAgICAgICAgICAgICAgfVxyXG4gICAgICAgICAgICB9XHJcbiAgICAgICAgfSBjYXRjaCAoZSkge1xyXG4gICAgICAgICAgICBMb2dnZXIuZGVmYXVsdC5lcnJvcihgZXJyb3Igc2Nhbm5pbmcgZ2xvYmxhIHZhcmlhYmxlcyA6ICR7ZX1gKTtcclxuICAgICAgICB9XHJcblxyXG4gICAgICAgIHJldHVybiByZXN1bHQ7XHJcbiAgICB9XHJcblxyXG4gICAgcHVibGljIGdldExvY2FsVmFyaWFibGUobmFtZTogc3RyaW5nKTogYW55IHtcclxuICAgICAgICByZXR1cm4gKDxhbnk+Z2xvYmFsVGhpcylbbmFtZV07XHJcbiAgICB9XHJcbn1cclxuXHJcbmV4cG9ydCBmdW5jdGlvbiBmb3JtYXRWYWx1ZShhcmc6IGFueSwgbWltZVR5cGU6IHN0cmluZyk6IGNvbnRyYWN0cy5Gb3JtYXR0ZWRWYWx1ZSB7XHJcbiAgICBsZXQgdmFsdWU6IHN0cmluZztcclxuXHJcbiAgICBzd2l0Y2ggKG1pbWVUeXBlKSB7XHJcbiAgICAgICAgY2FzZSAndGV4dC9wbGFpbic6XHJcbiAgICAgICAgICAgIHZhbHVlID0gYXJnPy50b1N0cmluZygpIHx8ICd1bmRlZmluZWQnO1xyXG4gICAgICAgICAgICBicmVhaztcclxuICAgICAgICBjYXNlICdhcHBsaWNhdGlvbi9qc29uJzpcclxuICAgICAgICAgICAgdmFsdWUgPSBKU09OLnN0cmluZ2lmeShhcmcpO1xyXG4gICAgICAgICAgICBicmVhaztcclxuICAgICAgICBkZWZhdWx0OlxyXG4gICAgICAgICAgICB0aHJvdyBuZXcgRXJyb3IoYHVuc3VwcG9ydGVkIG1pbWUgdHlwZTogJHttaW1lVHlwZX1gKTtcclxuICAgIH1cclxuXHJcbiAgICByZXR1cm4ge1xyXG4gICAgICAgIG1pbWVUeXBlLFxyXG4gICAgICAgIHZhbHVlLFxyXG4gICAgfTtcclxufVxyXG4iLCIvLyBDb3B5cmlnaHQgKGMpIC5ORVQgRm91bmRhdGlvbiBhbmQgY29udHJpYnV0b3JzLiBBbGwgcmlnaHRzIHJlc2VydmVkLlxyXG4vLyBMaWNlbnNlZCB1bmRlciB0aGUgTUlUIGxpY2Vuc2UuIFNlZSBMSUNFTlNFIGZpbGUgaW4gdGhlIHByb2plY3Qgcm9vdCBmb3IgZnVsbCBsaWNlbnNlIGluZm9ybWF0aW9uLlxyXG5cclxuaW1wb3J0ICogYXMgcnhqcyBmcm9tICdyeGpzJztcclxuaW1wb3J0IHsgQ29tcG9zaXRlS2VybmVsIH0gZnJvbSAnLi9jb21wb3NpdGVLZXJuZWwnO1xyXG5pbXBvcnQgKiBhcyBjb250cmFjdHMgZnJvbSAnLi9jb250cmFjdHMnO1xyXG5pbXBvcnQgKiBhcyBkaXNwb3NhYmxlcyBmcm9tICcuL2Rpc3Bvc2FibGVzJztcclxuaW1wb3J0IHsgRGlzcG9zYWJsZSB9IGZyb20gJy4vZGlzcG9zYWJsZXMnO1xyXG5pbXBvcnQgeyBLZXJuZWxUeXBlIH0gZnJvbSAnLi9rZXJuZWwnO1xyXG5pbXBvcnQgeyBMb2dnZXIgfSBmcm9tICcuL2xvZ2dlcic7XHJcblxyXG5leHBvcnQgdHlwZSBLZXJuZWxDb21tYW5kT3JFdmVudEVudmVsb3BlID0gY29udHJhY3RzLktlcm5lbENvbW1hbmRFbnZlbG9wZSB8IGNvbnRyYWN0cy5LZXJuZWxFdmVudEVudmVsb3BlO1xyXG5cclxuZXhwb3J0IGZ1bmN0aW9uIGlzS2VybmVsQ29tbWFuZEVudmVsb3BlKGNvbW1hbmRPckV2ZW50OiBLZXJuZWxDb21tYW5kT3JFdmVudEVudmVsb3BlKTogY29tbWFuZE9yRXZlbnQgaXMgY29udHJhY3RzLktlcm5lbENvbW1hbmRFbnZlbG9wZSB7XHJcbiAgICByZXR1cm4gKDxhbnk+Y29tbWFuZE9yRXZlbnQpLmNvbW1hbmRUeXBlICE9PSB1bmRlZmluZWQ7XHJcbn1cclxuXHJcbmV4cG9ydCBmdW5jdGlvbiBpc0tlcm5lbEV2ZW50RW52ZWxvcGUoY29tbWFuZE9yRXZlbnQ6IEtlcm5lbENvbW1hbmRPckV2ZW50RW52ZWxvcGUpOiBjb21tYW5kT3JFdmVudCBpcyBjb250cmFjdHMuS2VybmVsRXZlbnRFbnZlbG9wZSB7XHJcbiAgICByZXR1cm4gKDxhbnk+Y29tbWFuZE9yRXZlbnQpLmV2ZW50VHlwZSAhPT0gdW5kZWZpbmVkO1xyXG59XHJcblxyXG5leHBvcnQgaW50ZXJmYWNlIElLZXJuZWxDb21tYW5kQW5kRXZlbnRSZWNlaXZlciBleHRlbmRzIHJ4anMuU3Vic2NyaWJhYmxlPEtlcm5lbENvbW1hbmRPckV2ZW50RW52ZWxvcGU+IHtcclxufVxyXG5cclxuZXhwb3J0IGludGVyZmFjZSBJS2VybmVsQ29tbWFuZEFuZEV2ZW50U2VuZGVyIHtcclxuICAgIHNlbmQoa2VybmVsQ29tbWFuZE9yRXZlbnRFbnZlbG9wZTogS2VybmVsQ29tbWFuZE9yRXZlbnRFbnZlbG9wZSk6IFByb21pc2U8dm9pZD47XHJcbn1cclxuXHJcbmV4cG9ydCBjbGFzcyBLZXJuZWxDb21tYW5kQW5kRXZlbnRSZWNlaXZlciBpbXBsZW1lbnRzIElLZXJuZWxDb21tYW5kQW5kRXZlbnRSZWNlaXZlciB7XHJcbiAgICBwcml2YXRlIF9vYnNlcnZhYmxlOiByeGpzLlN1YnNjcmliYWJsZTxLZXJuZWxDb21tYW5kT3JFdmVudEVudmVsb3BlPjtcclxuICAgIHByaXZhdGUgX2Rpc3Bvc2FibGVzOiBkaXNwb3NhYmxlcy5EaXNwb3NhYmxlW10gPSBbXTtcclxuXHJcbiAgICBwcml2YXRlIGNvbnN0cnVjdG9yKG9ic2VydmVyOiByeGpzLk9ic2VydmFibGU8S2VybmVsQ29tbWFuZE9yRXZlbnRFbnZlbG9wZT4pIHtcclxuICAgICAgICB0aGlzLl9vYnNlcnZhYmxlID0gb2JzZXJ2ZXI7XHJcbiAgICB9XHJcblxyXG4gICAgc3Vic2NyaWJlKG9ic2VydmVyOiBQYXJ0aWFsPHJ4anMuT2JzZXJ2ZXI8S2VybmVsQ29tbWFuZE9yRXZlbnRFbnZlbG9wZT4+KTogcnhqcy5VbnN1YnNjcmliYWJsZSB7XHJcbiAgICAgICAgcmV0dXJuIHRoaXMuX29ic2VydmFibGUuc3Vic2NyaWJlKG9ic2VydmVyKTtcclxuICAgIH1cclxuXHJcbiAgICBwdWJsaWMgZGlzcG9zZSgpOiB2b2lkIHtcclxuICAgICAgICBmb3IgKGxldCBkaXNwb3NhYmxlIG9mIHRoaXMuX2Rpc3Bvc2FibGVzKSB7XHJcbiAgICAgICAgICAgIGRpc3Bvc2FibGUuZGlzcG9zZSgpO1xyXG4gICAgICAgIH1cclxuICAgIH1cclxuXHJcbiAgICBwdWJsaWMgc3RhdGljIEZyb21PYnNlcnZhYmxlKG9ic2VydmFibGU6IHJ4anMuT2JzZXJ2YWJsZTxLZXJuZWxDb21tYW5kT3JFdmVudEVudmVsb3BlPik6IElLZXJuZWxDb21tYW5kQW5kRXZlbnRSZWNlaXZlciB7XHJcbiAgICAgICAgcmV0dXJuIG5ldyBLZXJuZWxDb21tYW5kQW5kRXZlbnRSZWNlaXZlcihvYnNlcnZhYmxlKTtcclxuICAgIH1cclxuXHJcbiAgICBwdWJsaWMgc3RhdGljIEZyb21FdmVudExpc3RlbmVyKGFyZ3M6IHsgbWFwOiAoZGF0YTogRXZlbnQpID0+IEtlcm5lbENvbW1hbmRPckV2ZW50RW52ZWxvcGUsIGV2ZW50VGFyZ2V0OiBFdmVudFRhcmdldCwgZXZlbnQ6IHN0cmluZyB9KTogSUtlcm5lbENvbW1hbmRBbmRFdmVudFJlY2VpdmVyIHtcclxuICAgICAgICBsZXQgc3ViamVjdCA9IG5ldyByeGpzLlN1YmplY3Q8S2VybmVsQ29tbWFuZE9yRXZlbnRFbnZlbG9wZT4oKTtcclxuICAgICAgICBjb25zdCBsaXN0ZW5lciA9IChlOiBFdmVudCkgPT4ge1xyXG4gICAgICAgICAgICBsZXQgbWFwcGVkID0gYXJncy5tYXAoZSk7XHJcbiAgICAgICAgICAgIHN1YmplY3QubmV4dChtYXBwZWQpO1xyXG4gICAgICAgIH07XHJcbiAgICAgICAgYXJncy5ldmVudFRhcmdldC5hZGRFdmVudExpc3RlbmVyKGFyZ3MuZXZlbnQsIGxpc3RlbmVyKTtcclxuICAgICAgICBjb25zdCByZXQgPSBuZXcgS2VybmVsQ29tbWFuZEFuZEV2ZW50UmVjZWl2ZXIoc3ViamVjdCk7XHJcbiAgICAgICAgcmV0Ll9kaXNwb3NhYmxlcy5wdXNoKHtcclxuICAgICAgICAgICAgZGlzcG9zZTogKCkgPT4ge1xyXG4gICAgICAgICAgICAgICAgYXJncy5ldmVudFRhcmdldC5yZW1vdmVFdmVudExpc3RlbmVyKGFyZ3MuZXZlbnQsIGxpc3RlbmVyKTtcclxuICAgICAgICAgICAgfVxyXG4gICAgICAgIH0pO1xyXG4gICAgICAgIGFyZ3MuZXZlbnRUYXJnZXQucmVtb3ZlRXZlbnRMaXN0ZW5lcihhcmdzLmV2ZW50LCBsaXN0ZW5lcik7XHJcbiAgICAgICAgcmV0dXJuIHJldDtcclxuICAgIH1cclxufVxyXG5cclxuZnVuY3Rpb24gaXNPYnNlcnZhYmxlKHNvdXJjZTogYW55KTogc291cmNlIGlzIHJ4anMuT2JzZXJ2ZXI8S2VybmVsQ29tbWFuZE9yRXZlbnRFbnZlbG9wZT4ge1xyXG4gICAgcmV0dXJuICg8YW55PnNvdXJjZSkubmV4dCAhPT0gdW5kZWZpbmVkO1xyXG59XHJcblxyXG5leHBvcnQgY2xhc3MgS2VybmVsQ29tbWFuZEFuZEV2ZW50U2VuZGVyIGltcGxlbWVudHMgSUtlcm5lbENvbW1hbmRBbmRFdmVudFNlbmRlciB7XHJcbiAgICBwcml2YXRlIF9zZW5kZXI/OiByeGpzLk9ic2VydmVyPEtlcm5lbENvbW1hbmRPckV2ZW50RW52ZWxvcGU+IHwgKChrZXJuZWxFdmVudEVudmVsb3BlOiBLZXJuZWxDb21tYW5kT3JFdmVudEVudmVsb3BlKSA9PiB2b2lkKTtcclxuICAgIHByaXZhdGUgY29uc3RydWN0b3IoKSB7XHJcbiAgICB9XHJcbiAgICBzZW5kKGtlcm5lbENvbW1hbmRPckV2ZW50RW52ZWxvcGU6IEtlcm5lbENvbW1hbmRPckV2ZW50RW52ZWxvcGUpOiBQcm9taXNlPHZvaWQ+IHtcclxuICAgICAgICBpZiAodGhpcy5fc2VuZGVyKSB7XHJcbiAgICAgICAgICAgIHRyeSB7XHJcbiAgICAgICAgICAgICAgICBjb25zdCBzZXJpc2xpemVkID0gSlNPTi5wYXJzZShKU09OLnN0cmluZ2lmeShrZXJuZWxDb21tYW5kT3JFdmVudEVudmVsb3BlKSk7XHJcbiAgICAgICAgICAgICAgICBpZiAodHlwZW9mIHRoaXMuX3NlbmRlciA9PT0gXCJmdW5jdGlvblwiKSB7XHJcbiAgICAgICAgICAgICAgICAgICAgdGhpcy5fc2VuZGVyKHNlcmlzbGl6ZWQpO1xyXG4gICAgICAgICAgICAgICAgfSBlbHNlIGlmIChpc09ic2VydmFibGUodGhpcy5fc2VuZGVyKSkge1xyXG4gICAgICAgICAgICAgICAgICAgIHRoaXMuX3NlbmRlci5uZXh0KHNlcmlzbGl6ZWQpO1xyXG4gICAgICAgICAgICAgICAgfSBlbHNlIHtcclxuICAgICAgICAgICAgICAgICAgICByZXR1cm4gUHJvbWlzZS5yZWplY3QobmV3IEVycm9yKFwiU2VuZGVyIGlzIG5vdCBzZXRcIikpO1xyXG4gICAgICAgICAgICAgICAgfVxyXG4gICAgICAgICAgICB9XHJcbiAgICAgICAgICAgIGNhdGNoIChlcnJvcikge1xyXG4gICAgICAgICAgICAgICAgcmV0dXJuIFByb21pc2UucmVqZWN0KGVycm9yKTtcclxuICAgICAgICAgICAgfVxyXG4gICAgICAgICAgICByZXR1cm4gUHJvbWlzZS5yZXNvbHZlKCk7XHJcbiAgICAgICAgfVxyXG4gICAgICAgIHJldHVybiBQcm9taXNlLnJlamVjdChuZXcgRXJyb3IoXCJTZW5kZXIgaXMgbm90IHNldFwiKSk7XHJcbiAgICB9XHJcblxyXG4gICAgcHVibGljIHN0YXRpYyBGcm9tT2JzZXJ2ZXIob2JzZXJ2ZXI6IHJ4anMuT2JzZXJ2ZXI8S2VybmVsQ29tbWFuZE9yRXZlbnRFbnZlbG9wZT4pOiBJS2VybmVsQ29tbWFuZEFuZEV2ZW50U2VuZGVyIHtcclxuICAgICAgICBjb25zdCBzZW5kZXIgPSBuZXcgS2VybmVsQ29tbWFuZEFuZEV2ZW50U2VuZGVyKCk7XHJcbiAgICAgICAgc2VuZGVyLl9zZW5kZXIgPSBvYnNlcnZlcjtcclxuICAgICAgICByZXR1cm4gc2VuZGVyO1xyXG4gICAgfVxyXG5cclxuICAgIHB1YmxpYyBzdGF0aWMgRnJvbUZ1bmN0aW9uKHNlbmQ6IChrZXJuZWxFdmVudEVudmVsb3BlOiBLZXJuZWxDb21tYW5kT3JFdmVudEVudmVsb3BlKSA9PiB2b2lkKTogSUtlcm5lbENvbW1hbmRBbmRFdmVudFNlbmRlciB7XHJcbiAgICAgICAgY29uc3Qgc2VuZGVyID0gbmV3IEtlcm5lbENvbW1hbmRBbmRFdmVudFNlbmRlcigpO1xyXG4gICAgICAgIHNlbmRlci5fc2VuZGVyID0gc2VuZDtcclxuICAgICAgICByZXR1cm4gc2VuZGVyO1xyXG4gICAgfVxyXG59XHJcblxyXG5leHBvcnQgZnVuY3Rpb24gaXNTZXRPZlN0cmluZyhjb2xsZWN0aW9uOiBhbnkpOiBjb2xsZWN0aW9uIGlzIFNldDxzdHJpbmc+IHtcclxuICAgIHJldHVybiB0eXBlb2YgKGNvbGxlY3Rpb24pICE9PSB0eXBlb2YgKG5ldyBTZXQ8c3RyaW5nPigpKTtcclxufVxyXG5cclxuZXhwb3J0IGZ1bmN0aW9uIGlzQXJyYXlPZlN0cmluZyhjb2xsZWN0aW9uOiBhbnkpOiBjb2xsZWN0aW9uIGlzIHN0cmluZ1tdIHtcclxuICAgIHJldHVybiBBcnJheS5pc0FycmF5KGNvbGxlY3Rpb24pICYmIGNvbGxlY3Rpb24ubGVuZ3RoID4gMCAmJiB0eXBlb2YgKGNvbGxlY3Rpb25bMF0pID09PSB0eXBlb2YgKFwiXCIpO1xyXG59XHJcblxyXG5leHBvcnQgY29uc3Qgb25LZXJuZWxJbmZvVXBkYXRlczogKChjb21wb3NpdGVLZXJuZWw6IENvbXBvc2l0ZUtlcm5lbCkgPT4gdm9pZClbXSA9IFtdO1xyXG5cclxuZXhwb3J0IGZ1bmN0aW9uIGVuc3VyZU9yVXBkYXRlUHJveHlGb3JLZXJuZWxJbmZvKGtlcm5lbEluZm9Qcm9kdWNlZDogY29udHJhY3RzLktlcm5lbEluZm9Qcm9kdWNlZCwgY29tcG9zaXRlS2VybmVsOiBDb21wb3NpdGVLZXJuZWwpIHtcclxuICAgIGNvbnN0IHVyaVRvTG9va3VwID0ga2VybmVsSW5mb1Byb2R1Y2VkLmtlcm5lbEluZm8udXJpID8/IGtlcm5lbEluZm9Qcm9kdWNlZC5rZXJuZWxJbmZvLnJlbW90ZVVyaTtcclxuICAgIGlmICh1cmlUb0xvb2t1cCkge1xyXG4gICAgICAgIGxldCBrZXJuZWwgPSBjb21wb3NpdGVLZXJuZWwuZmluZEtlcm5lbEJ5VXJpKHVyaVRvTG9va3VwKTtcclxuICAgICAgICBpZiAoIWtlcm5lbCkge1xyXG4gICAgICAgICAgICAvLyBhZGRcclxuICAgICAgICAgICAgaWYgKGNvbXBvc2l0ZUtlcm5lbC5ob3N0KSB7XHJcbiAgICAgICAgICAgICAgICBMb2dnZXIuZGVmYXVsdC5pbmZvKGBjcmVhdGluZyBwcm94eSBmb3IgdXJpWyR7dXJpVG9Mb29rdXB9XXdpdGggaW5mbyAke0pTT04uc3RyaW5naWZ5KGtlcm5lbEluZm9Qcm9kdWNlZCl9YCk7XHJcbiAgICAgICAgICAgICAgICAvLyBjaGVjayBmb3IgY2xhc2ggd2l0aCBga2VybmVsSW5mby5sb2NhbE5hbWVgXHJcbiAgICAgICAgICAgICAgICBrZXJuZWwgPSBjb21wb3NpdGVLZXJuZWwuaG9zdC5jb25uZWN0UHJveHlLZXJuZWwoa2VybmVsSW5mb1Byb2R1Y2VkLmtlcm5lbEluZm8ubG9jYWxOYW1lLCB1cmlUb0xvb2t1cCwga2VybmVsSW5mb1Byb2R1Y2VkLmtlcm5lbEluZm8uYWxpYXNlcyk7XHJcbiAgICAgICAgICAgIH0gZWxzZSB7XHJcbiAgICAgICAgICAgICAgICB0aHJvdyBuZXcgRXJyb3IoJ25vIGtlcm5lbCBob3N0IGZvdW5kJyk7XHJcbiAgICAgICAgICAgIH1cclxuICAgICAgICB9IGVsc2Uge1xyXG4gICAgICAgICAgICBMb2dnZXIuZGVmYXVsdC5pbmZvKGBwYXRjaGluZyBwcm94eSBmb3IgdXJpWyR7dXJpVG9Mb29rdXB9XXdpdGggaW5mbyAke0pTT04uc3RyaW5naWZ5KGtlcm5lbEluZm9Qcm9kdWNlZCl9IGApO1xyXG4gICAgICAgIH1cclxuXHJcbiAgICAgICAgaWYgKGtlcm5lbC5rZXJuZWxUeXBlID09PSBLZXJuZWxUeXBlLnByb3h5KSB7XHJcbiAgICAgICAgICAgIC8vIHBhdGNoXHJcbiAgICAgICAgICAgIHVwZGF0ZUtlcm5lbEluZm8oa2VybmVsLmtlcm5lbEluZm8sIGtlcm5lbEluZm9Qcm9kdWNlZC5rZXJuZWxJbmZvKTtcclxuICAgICAgICB9XHJcblxyXG4gICAgICAgIGZvciAoY29uc3QgdXBkYXRlciBvZiBvbktlcm5lbEluZm9VcGRhdGVzKSB7XHJcbiAgICAgICAgICAgIHVwZGF0ZXIoY29tcG9zaXRlS2VybmVsKTtcclxuICAgICAgICB9XHJcbiAgICB9XHJcbn1cclxuXHJcbmV4cG9ydCBmdW5jdGlvbiBpc0tlcm5lbEluZm9Gb3JQcm94eShrZXJuZWxJbmZvOiBjb250cmFjdHMuS2VybmVsSW5mbyk6IGJvb2xlYW4ge1xyXG4gICAgY29uc3QgaGFzVXJpID0gISFrZXJuZWxJbmZvLnVyaTtcclxuICAgIGNvbnN0IGhhc1JlbW90ZVVyaSA9ICEha2VybmVsSW5mby5yZW1vdGVVcmk7XHJcbiAgICByZXR1cm4gaGFzVXJpICYmIGhhc1JlbW90ZVVyaTtcclxufVxyXG5cclxuZXhwb3J0IGZ1bmN0aW9uIHVwZGF0ZUtlcm5lbEluZm8oZGVzdGluYXRpb246IGNvbnRyYWN0cy5LZXJuZWxJbmZvLCBpbmNvbWluZzogY29udHJhY3RzLktlcm5lbEluZm8pIHtcclxuICAgIGRlc3RpbmF0aW9uLmxhbmd1YWdlTmFtZSA9IGluY29taW5nLmxhbmd1YWdlTmFtZSA/PyBkZXN0aW5hdGlvbi5sYW5ndWFnZU5hbWU7XHJcbiAgICBkZXN0aW5hdGlvbi5sYW5ndWFnZVZlcnNpb24gPSBpbmNvbWluZy5sYW5ndWFnZVZlcnNpb24gPz8gZGVzdGluYXRpb24ubGFuZ3VhZ2VWZXJzaW9uO1xyXG4gICAgZGVzdGluYXRpb24uZGlzcGxheU5hbWUgPSBpbmNvbWluZy5kaXNwbGF5TmFtZTtcclxuXHJcbiAgICBjb25zdCBzdXBwb3J0ZWREaXJlY3RpdmVzID0gbmV3IFNldDxzdHJpbmc+KCk7XHJcbiAgICBjb25zdCBzdXBwb3J0ZWRDb21tYW5kcyA9IG5ldyBTZXQ8c3RyaW5nPigpO1xyXG5cclxuICAgIGlmICghZGVzdGluYXRpb24uc3VwcG9ydGVkRGlyZWN0aXZlcykge1xyXG4gICAgICAgIGRlc3RpbmF0aW9uLnN1cHBvcnRlZERpcmVjdGl2ZXMgPSBbXTtcclxuICAgIH1cclxuXHJcbiAgICBpZiAoIWRlc3RpbmF0aW9uLnN1cHBvcnRlZEtlcm5lbENvbW1hbmRzKSB7XHJcbiAgICAgICAgZGVzdGluYXRpb24uc3VwcG9ydGVkS2VybmVsQ29tbWFuZHMgPSBbXTtcclxuICAgIH1cclxuXHJcbiAgICBmb3IgKGNvbnN0IHN1cHBvcnRlZERpcmVjdGl2ZSBvZiBkZXN0aW5hdGlvbi5zdXBwb3J0ZWREaXJlY3RpdmVzKSB7XHJcbiAgICAgICAgc3VwcG9ydGVkRGlyZWN0aXZlcy5hZGQoc3VwcG9ydGVkRGlyZWN0aXZlLm5hbWUpO1xyXG4gICAgfVxyXG5cclxuICAgIGZvciAoY29uc3Qgc3VwcG9ydGVkQ29tbWFuZCBvZiBkZXN0aW5hdGlvbi5zdXBwb3J0ZWRLZXJuZWxDb21tYW5kcykge1xyXG4gICAgICAgIHN1cHBvcnRlZENvbW1hbmRzLmFkZChzdXBwb3J0ZWRDb21tYW5kLm5hbWUpO1xyXG4gICAgfVxyXG5cclxuICAgIGZvciAoY29uc3Qgc3VwcG9ydGVkRGlyZWN0aXZlIG9mIGluY29taW5nLnN1cHBvcnRlZERpcmVjdGl2ZXMpIHtcclxuICAgICAgICBpZiAoIXN1cHBvcnRlZERpcmVjdGl2ZXMuaGFzKHN1cHBvcnRlZERpcmVjdGl2ZS5uYW1lKSkge1xyXG4gICAgICAgICAgICBzdXBwb3J0ZWREaXJlY3RpdmVzLmFkZChzdXBwb3J0ZWREaXJlY3RpdmUubmFtZSk7XHJcbiAgICAgICAgICAgIGRlc3RpbmF0aW9uLnN1cHBvcnRlZERpcmVjdGl2ZXMucHVzaChzdXBwb3J0ZWREaXJlY3RpdmUpO1xyXG4gICAgICAgIH1cclxuICAgIH1cclxuXHJcbiAgICBmb3IgKGNvbnN0IHN1cHBvcnRlZENvbW1hbmQgb2YgaW5jb21pbmcuc3VwcG9ydGVkS2VybmVsQ29tbWFuZHMpIHtcclxuICAgICAgICBpZiAoIXN1cHBvcnRlZENvbW1hbmRzLmhhcyhzdXBwb3J0ZWRDb21tYW5kLm5hbWUpKSB7XHJcbiAgICAgICAgICAgIHN1cHBvcnRlZENvbW1hbmRzLmFkZChzdXBwb3J0ZWRDb21tYW5kLm5hbWUpO1xyXG4gICAgICAgICAgICBkZXN0aW5hdGlvbi5zdXBwb3J0ZWRLZXJuZWxDb21tYW5kcy5wdXNoKHN1cHBvcnRlZENvbW1hbmQpO1xyXG4gICAgICAgIH1cclxuICAgIH1cclxufVxyXG5cclxuZXhwb3J0IGNsYXNzIENvbm5lY3RvciBpbXBsZW1lbnRzIERpc3Bvc2FibGUge1xyXG4gICAgcHJpdmF0ZSByZWFkb25seSBfbGlzdGVuZXI6IHJ4anMuVW5zdWJzY3JpYmFibGU7XHJcbiAgICBwcml2YXRlIHJlYWRvbmx5IF9yZWNlaXZlcjogSUtlcm5lbENvbW1hbmRBbmRFdmVudFJlY2VpdmVyO1xyXG4gICAgcHJpdmF0ZSByZWFkb25seSBfc2VuZGVyOiBJS2VybmVsQ29tbWFuZEFuZEV2ZW50U2VuZGVyO1xyXG4gICAgcHJpdmF0ZSByZWFkb25seSBfcmVtb3RlVXJpczogU2V0PHN0cmluZz4gPSBuZXcgU2V0PHN0cmluZz4oKTtcclxuXHJcbiAgICBwdWJsaWMgZ2V0IHJlbW90ZUhvc3RVcmlzKCk6IHN0cmluZ1tdIHtcclxuICAgICAgICByZXR1cm4gQXJyYXkuZnJvbSh0aGlzLl9yZW1vdGVVcmlzLnZhbHVlcygpKTtcclxuICAgIH1cclxuXHJcbiAgICBwdWJsaWMgZ2V0IHNlbmRlcigpOiBJS2VybmVsQ29tbWFuZEFuZEV2ZW50U2VuZGVyIHtcclxuICAgICAgICByZXR1cm4gdGhpcy5fc2VuZGVyO1xyXG4gICAgfVxyXG5cclxuICAgIHB1YmxpYyBnZXQgcmVjZWl2ZXIoKTogSUtlcm5lbENvbW1hbmRBbmRFdmVudFJlY2VpdmVyIHtcclxuICAgICAgICByZXR1cm4gdGhpcy5fcmVjZWl2ZXI7XHJcbiAgICB9XHJcblxyXG4gICAgY29uc3RydWN0b3IoY29uZmlndXJhdGlvbjogeyByZWNlaXZlcjogSUtlcm5lbENvbW1hbmRBbmRFdmVudFJlY2VpdmVyLCBzZW5kZXI6IElLZXJuZWxDb21tYW5kQW5kRXZlbnRTZW5kZXIsIHJlbW90ZVVyaXM/OiBzdHJpbmdbXSB9KSB7XHJcbiAgICAgICAgdGhpcy5fcmVjZWl2ZXIgPSBjb25maWd1cmF0aW9uLnJlY2VpdmVyO1xyXG4gICAgICAgIHRoaXMuX3NlbmRlciA9IGNvbmZpZ3VyYXRpb24uc2VuZGVyO1xyXG4gICAgICAgIGlmIChjb25maWd1cmF0aW9uLnJlbW90ZVVyaXMpIHtcclxuICAgICAgICAgICAgZm9yIChjb25zdCByZW1vdGVVcmkgb2YgY29uZmlndXJhdGlvbi5yZW1vdGVVcmlzKSB7XHJcbiAgICAgICAgICAgICAgICBjb25zdCB1cmkgPSBleHRyYWN0SG9zdEFuZE5vbWFsaXplKHJlbW90ZVVyaSk7XHJcbiAgICAgICAgICAgICAgICBpZiAodXJpKSB7XHJcbiAgICAgICAgICAgICAgICAgICAgdGhpcy5fcmVtb3RlVXJpcy5hZGQodXJpKTtcclxuICAgICAgICAgICAgICAgIH1cclxuICAgICAgICAgICAgfVxyXG4gICAgICAgIH1cclxuXHJcbiAgICAgICAgdGhpcy5fbGlzdGVuZXIgPSB0aGlzLl9yZWNlaXZlci5zdWJzY3JpYmUoe1xyXG4gICAgICAgICAgICBuZXh0OiAoa2VybmVsQ29tbWFuZE9yRXZlbnRFbnZlbG9wZTogS2VybmVsQ29tbWFuZE9yRXZlbnRFbnZlbG9wZSkgPT4ge1xyXG4gICAgICAgICAgICAgICAgaWYgKGlzS2VybmVsRXZlbnRFbnZlbG9wZShrZXJuZWxDb21tYW5kT3JFdmVudEVudmVsb3BlKSkge1xyXG4gICAgICAgICAgICAgICAgICAgIGlmIChrZXJuZWxDb21tYW5kT3JFdmVudEVudmVsb3BlLmV2ZW50VHlwZSA9PT0gY29udHJhY3RzLktlcm5lbEluZm9Qcm9kdWNlZFR5cGUpIHtcclxuICAgICAgICAgICAgICAgICAgICAgICAgY29uc3QgZXZlbnQgPSA8Y29udHJhY3RzLktlcm5lbEluZm9Qcm9kdWNlZD5rZXJuZWxDb21tYW5kT3JFdmVudEVudmVsb3BlLmV2ZW50O1xyXG4gICAgICAgICAgICAgICAgICAgICAgICBpZiAoIWV2ZW50Lmtlcm5lbEluZm8ucmVtb3RlVXJpKSB7XHJcbiAgICAgICAgICAgICAgICAgICAgICAgICAgICBjb25zdCB1cmkgPSBleHRyYWN0SG9zdEFuZE5vbWFsaXplKGV2ZW50Lmtlcm5lbEluZm8udXJpISk7XHJcbiAgICAgICAgICAgICAgICAgICAgICAgICAgICBpZiAodXJpKSB7XHJcbiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgdGhpcy5fcmVtb3RlVXJpcy5hZGQodXJpKTtcclxuICAgICAgICAgICAgICAgICAgICAgICAgICAgIH1cclxuICAgICAgICAgICAgICAgICAgICAgICAgfVxyXG4gICAgICAgICAgICAgICAgICAgIH1cclxuICAgICAgICAgICAgICAgICAgICBpZiAoKGtlcm5lbENvbW1hbmRPckV2ZW50RW52ZWxvcGUucm91dGluZ1NsaXA/Lmxlbmd0aCA/PyAwKSA+IDApIHtcclxuICAgICAgICAgICAgICAgICAgICAgICAgY29uc3QgZXZlbnRPcmlnaW4gPSBrZXJuZWxDb21tYW5kT3JFdmVudEVudmVsb3BlLnJvdXRpbmdTbGlwIVswXTtcclxuICAgICAgICAgICAgICAgICAgICAgICAgY29uc3QgdXJpID0gZXh0cmFjdEhvc3RBbmROb21hbGl6ZShldmVudE9yaWdpbik7XHJcbiAgICAgICAgICAgICAgICAgICAgICAgIGlmICh1cmkpIHtcclxuICAgICAgICAgICAgICAgICAgICAgICAgICAgIHRoaXMuX3JlbW90ZVVyaXMuYWRkKHVyaSk7XHJcbiAgICAgICAgICAgICAgICAgICAgICAgIH1cclxuICAgICAgICAgICAgICAgICAgICB9XHJcbiAgICAgICAgICAgICAgICB9XHJcbiAgICAgICAgICAgIH1cclxuICAgICAgICB9KTtcclxuICAgIH1cclxuXHJcbiAgICBwdWJsaWMgY2FuUmVhY2gocmVtb3RlVXJpOiBzdHJpbmcpOiBib29sZWFuIHtcclxuICAgICAgICBjb25zdCBob3N0ID0gZXh0cmFjdEhvc3RBbmROb21hbGl6ZShyZW1vdGVVcmkpOy8vP1xyXG4gICAgICAgIGlmIChob3N0KSB7XHJcbiAgICAgICAgICAgIHJldHVybiB0aGlzLl9yZW1vdGVVcmlzLmhhcyhob3N0KTtcclxuICAgICAgICB9XHJcbiAgICAgICAgcmV0dXJuIGZhbHNlO1xyXG4gICAgfVxyXG4gICAgZGlzcG9zZSgpOiB2b2lkIHtcclxuICAgICAgICB0aGlzLl9saXN0ZW5lci51bnN1YnNjcmliZSgpO1xyXG4gICAgfVxyXG59XHJcblxyXG5leHBvcnQgZnVuY3Rpb24gZXh0cmFjdEhvc3RBbmROb21hbGl6ZShrZXJuZWxVcmk6IHN0cmluZyk6IHN0cmluZyB8IHVuZGVmaW5lZCB7XHJcbiAgICBjb25zdCBmaWx0ZXI6IFJlZ0V4cCA9IC8oPzxob3N0Pi4rOlxcL1xcL1teXFwvXSspKFxcL1teXFwvXSkqL2dpO1xyXG4gICAgY29uc3QgbWF0Y2ggPSBmaWx0ZXIuZXhlYyhrZXJuZWxVcmkpOyAvLz9cclxuICAgIGlmIChtYXRjaD8uZ3JvdXBzPy5ob3N0KSB7XHJcbiAgICAgICAgY29uc3QgaG9zdCA9IG1hdGNoLmdyb3Vwcy5ob3N0O1xyXG4gICAgICAgIHJldHVybiBob3N0Oy8vP1xyXG4gICAgfVxyXG4gICAgcmV0dXJuIFwiXCI7XHJcbn1cclxuIiwiLy8gQ29weXJpZ2h0IChjKSAuTkVUIEZvdW5kYXRpb24gYW5kIGNvbnRyaWJ1dG9ycy4gQWxsIHJpZ2h0cyByZXNlcnZlZC5cclxuLy8gTGljZW5zZWQgdW5kZXIgdGhlIE1JVCBsaWNlbnNlLiBTZWUgTElDRU5TRSBmaWxlIGluIHRoZSBwcm9qZWN0IHJvb3QgZm9yIGZ1bGwgbGljZW5zZSBpbmZvcm1hdGlvbi5cclxuXHJcbmltcG9ydCAqIGFzIGNvbnRyYWN0cyBmcm9tIFwiLi9jb250cmFjdHNcIjtcclxuaW1wb3J0IHsgTG9nZ2VyIH0gZnJvbSBcIi4vbG9nZ2VyXCI7XHJcbmltcG9ydCB7IEtlcm5lbCwgSUtlcm5lbENvbW1hbmRIYW5kbGVyLCBJS2VybmVsQ29tbWFuZEludm9jYXRpb24sIGdldEtlcm5lbFVyaSwgS2VybmVsVHlwZSB9IGZyb20gXCIuL2tlcm5lbFwiO1xyXG5pbXBvcnQgKiBhcyBjb25uZWN0aW9uIGZyb20gXCIuL2Nvbm5lY3Rpb25cIjtcclxuaW1wb3J0ICogYXMgcm91dGluZ1NsaXAgZnJvbSBcIi4vcm91dGluZ3NsaXBcIjtcclxuaW1wb3J0IHsgUHJvbWlzZUNvbXBsZXRpb25Tb3VyY2UgfSBmcm9tIFwiLi9wcm9taXNlQ29tcGxldGlvblNvdXJjZVwiO1xyXG5pbXBvcnQgeyBLZXJuZWxJbnZvY2F0aW9uQ29udGV4dCB9IGZyb20gXCIuL2tlcm5lbEludm9jYXRpb25Db250ZXh0XCI7XHJcblxyXG5leHBvcnQgY2xhc3MgUHJveHlLZXJuZWwgZXh0ZW5kcyBLZXJuZWwge1xyXG5cclxuICAgIGNvbnN0cnVjdG9yKG92ZXJyaWRlIHJlYWRvbmx5IG5hbWU6IHN0cmluZywgcHJpdmF0ZSByZWFkb25seSBfc2VuZGVyOiBjb25uZWN0aW9uLklLZXJuZWxDb21tYW5kQW5kRXZlbnRTZW5kZXIsIHByaXZhdGUgcmVhZG9ubHkgX3JlY2VpdmVyOiBjb25uZWN0aW9uLklLZXJuZWxDb21tYW5kQW5kRXZlbnRSZWNlaXZlciwgbGFuZ3VhZ2VOYW1lPzogc3RyaW5nLCBsYW5ndWFnZVZlcnNpb24/OiBzdHJpbmcpIHtcclxuICAgICAgICBzdXBlcihuYW1lLCBsYW5ndWFnZU5hbWUsIGxhbmd1YWdlVmVyc2lvbik7XHJcbiAgICAgICAgdGhpcy5rZXJuZWxUeXBlID0gS2VybmVsVHlwZS5wcm94eTtcclxuICAgIH1cclxuXHJcbiAgICBvdmVycmlkZSBnZXRDb21tYW5kSGFuZGxlcihjb21tYW5kVHlwZTogY29udHJhY3RzLktlcm5lbENvbW1hbmRUeXBlKTogSUtlcm5lbENvbW1hbmRIYW5kbGVyIHwgdW5kZWZpbmVkIHtcclxuICAgICAgICByZXR1cm4ge1xyXG4gICAgICAgICAgICBjb21tYW5kVHlwZSxcclxuICAgICAgICAgICAgaGFuZGxlOiAoaW52b2NhdGlvbikgPT4ge1xyXG4gICAgICAgICAgICAgICAgcmV0dXJuIHRoaXMuX2NvbW1hbmRIYW5kbGVyKGludm9jYXRpb24pO1xyXG4gICAgICAgICAgICB9XHJcbiAgICAgICAgfTtcclxuICAgIH1cclxuXHJcbiAgICBwcml2YXRlIGRlbGVnYXRlUHVibGljYXRpb24oZW52ZWxvcGU6IGNvbnRyYWN0cy5LZXJuZWxFdmVudEVudmVsb3BlLCBpbnZvY2F0aW9uQ29udGV4dDogS2VybmVsSW52b2NhdGlvbkNvbnRleHQpOiB2b2lkIHtcclxuICAgICAgICBsZXQgYWxyZWFkeUJlZW5TZWVuID0gZmFsc2U7XHJcbiAgICAgICAgY29uc3Qga2VybmVsVXJpID0gZ2V0S2VybmVsVXJpKHRoaXMpO1xyXG4gICAgICAgIGlmIChrZXJuZWxVcmkgJiYgIXJvdXRpbmdTbGlwLmV2ZW50Um91dGluZ1NsaXBDb250YWlucyhlbnZlbG9wZSwga2VybmVsVXJpKSkge1xyXG4gICAgICAgICAgICByb3V0aW5nU2xpcC5zdGFtcEV2ZW50Um91dGluZ1NsaXAoZW52ZWxvcGUsIGtlcm5lbFVyaSk7XHJcbiAgICAgICAgfSBlbHNlIHtcclxuICAgICAgICAgICAgYWxyZWFkeUJlZW5TZWVuID0gdHJ1ZTtcclxuICAgICAgICB9XHJcblxyXG4gICAgICAgIGlmICh0aGlzLmhhc1NhbWVPcmlnaW4oZW52ZWxvcGUpKSB7XHJcbiAgICAgICAgICAgIGlmICghYWxyZWFkeUJlZW5TZWVuKSB7XHJcbiAgICAgICAgICAgICAgICBpbnZvY2F0aW9uQ29udGV4dC5wdWJsaXNoKGVudmVsb3BlKTtcclxuICAgICAgICAgICAgfVxyXG4gICAgICAgIH1cclxuICAgIH1cclxuXHJcbiAgICBwcml2YXRlIGhhc1NhbWVPcmlnaW4oZW52ZWxvcGU6IGNvbnRyYWN0cy5LZXJuZWxFdmVudEVudmVsb3BlKTogYm9vbGVhbiB7XHJcbiAgICAgICAgbGV0IGNvbW1hbmRPcmlnaW5VcmkgPSBlbnZlbG9wZS5jb21tYW5kPy5jb21tYW5kPy5vcmlnaW5VcmkgPz8gdGhpcy5rZXJuZWxJbmZvLnVyaTtcclxuICAgICAgICBpZiAoY29tbWFuZE9yaWdpblVyaSA9PT0gdGhpcy5rZXJuZWxJbmZvLnVyaSkge1xyXG4gICAgICAgICAgICByZXR1cm4gdHJ1ZTtcclxuICAgICAgICB9XHJcblxyXG4gICAgICAgIHJldHVybiBjb21tYW5kT3JpZ2luVXJpID09PSBudWxsO1xyXG4gICAgfVxyXG5cclxuICAgIHByaXZhdGUgdXBkYXRlS2VybmVsSW5mb0Zyb21FdmVudChrZXJuZWxJbmZvUHJvZHVjZWQ6IGNvbnRyYWN0cy5LZXJuZWxJbmZvUHJvZHVjZWQpIHtcclxuICAgICAgICBjb25uZWN0aW9uLnVwZGF0ZUtlcm5lbEluZm8odGhpcy5rZXJuZWxJbmZvLCBrZXJuZWxJbmZvUHJvZHVjZWQua2VybmVsSW5mbyk7XHJcbiAgICB9XHJcblxyXG4gICAgcHJpdmF0ZSBhc3luYyBfY29tbWFuZEhhbmRsZXIoY29tbWFuZEludm9jYXRpb246IElLZXJuZWxDb21tYW5kSW52b2NhdGlvbik6IFByb21pc2U8dm9pZD4ge1xyXG4gICAgICAgIHRoaXMuZW5zdXJlQ29tbWFuZFRva2VuQW5kSWQoY29tbWFuZEludm9jYXRpb24uY29tbWFuZEVudmVsb3BlKTtcclxuICAgICAgICBjb25zdCBjb21tYW5kVG9rZW4gPSBjb21tYW5kSW52b2NhdGlvbi5jb21tYW5kRW52ZWxvcGUudG9rZW47XHJcbiAgICAgICAgY29uc3QgY29tbWFuZElkID0gY29tbWFuZEludm9jYXRpb24uY29tbWFuZEVudmVsb3BlLmlkO1xyXG4gICAgICAgIGNvbnN0IGNvbXBsZXRpb25Tb3VyY2UgPSBuZXcgUHJvbWlzZUNvbXBsZXRpb25Tb3VyY2U8Y29udHJhY3RzLktlcm5lbEV2ZW50RW52ZWxvcGU+KCk7XHJcbiAgICAgICAgLy8gZml4IDogaXMgdGhpcyB0aGUgcmlnaHQgd2F5PyBXZSBhcmUgdHJ5aW5nIHRvIGF2b2lkIGZvcndhcmRpbmcgZXZlbnRzIHdlIGp1c3QgZGlkIGZvcndhcmRcclxuICAgICAgICBsZXQgZXZlbnRTdWJzY3JpcHRpb24gPSB0aGlzLl9yZWNlaXZlci5zdWJzY3JpYmUoe1xyXG4gICAgICAgICAgICBuZXh0OiAoZW52ZWxvcGUpID0+IHtcclxuICAgICAgICAgICAgICAgIGlmIChjb25uZWN0aW9uLmlzS2VybmVsRXZlbnRFbnZlbG9wZShlbnZlbG9wZSkpIHtcclxuICAgICAgICAgICAgICAgICAgICBpZiAoZW52ZWxvcGUuZXZlbnRUeXBlID09PSBjb250cmFjdHMuS2VybmVsSW5mb1Byb2R1Y2VkVHlwZSAmJlxyXG4gICAgICAgICAgICAgICAgICAgICAgICAoZW52ZWxvcGUuY29tbWFuZCA9PT0gbnVsbCB8fCBlbnZlbG9wZS5jb21tYW5kID09PSB1bmRlZmluZWQpKSB7XHJcblxyXG4gICAgICAgICAgICAgICAgICAgICAgICBjb25zdCBrZXJuZWxJbmZvUHJvZHVjZWQgPSA8Y29udHJhY3RzLktlcm5lbEluZm9Qcm9kdWNlZD5lbnZlbG9wZS5ldmVudDtcclxuICAgICAgICAgICAgICAgICAgICAgICAga2VybmVsSW5mb1Byb2R1Y2VkLmtlcm5lbEluZm87Ly8/XHJcbiAgICAgICAgICAgICAgICAgICAgICAgIHRoaXMua2VybmVsSW5mbzsvLz9cclxuICAgICAgICAgICAgICAgICAgICAgICAgaWYgKGtlcm5lbEluZm9Qcm9kdWNlZC5rZXJuZWxJbmZvLnVyaSA9PT0gdGhpcy5rZXJuZWxJbmZvLnJlbW90ZVVyaSkge1xyXG5cclxuICAgICAgICAgICAgICAgICAgICAgICAgICAgIHRoaXMudXBkYXRlS2VybmVsSW5mb0Zyb21FdmVudChrZXJuZWxJbmZvUHJvZHVjZWQpO1xyXG4gICAgICAgICAgICAgICAgICAgICAgICAgICAgdGhpcy5wdWJsaXNoRXZlbnQoXHJcbiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAge1xyXG4gICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICBldmVudFR5cGU6IGNvbnRyYWN0cy5LZXJuZWxJbmZvUHJvZHVjZWRUeXBlLFxyXG4gICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICBldmVudDogeyBrZXJuZWxJbmZvOiB0aGlzLmtlcm5lbEluZm8gfVxyXG4gICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIH0pO1xyXG4gICAgICAgICAgICAgICAgICAgICAgICB9XHJcbiAgICAgICAgICAgICAgICAgICAgfVxyXG4gICAgICAgICAgICAgICAgICAgIGVsc2UgaWYgKGVudmVsb3BlLmNvbW1hbmQhLnRva2VuID09PSBjb21tYW5kVG9rZW4pIHtcclxuXHJcbiAgICAgICAgICAgICAgICAgICAgICAgIExvZ2dlci5kZWZhdWx0LmluZm8oYHByb3h5IG5hbWU9JHt0aGlzLm5hbWV9W2xvY2FsIHVyaToke3RoaXMua2VybmVsSW5mby51cml9LCByZW1vdGUgdXJpOiR7dGhpcy5rZXJuZWxJbmZvLnJlbW90ZVVyaX1dIHByb2Nlc3NpbmcgZXZlbnQsIGVudmVsb3BlaWQ9JHtlbnZlbG9wZS5jb21tYW5kIS5pZH0sIGNvbW1hbmRpZD0ke2NvbW1hbmRJZH1gKTtcclxuICAgICAgICAgICAgICAgICAgICAgICAgTG9nZ2VyLmRlZmF1bHQuaW5mbyhgcHJveHkgbmFtZT0ke3RoaXMubmFtZX1bbG9jYWwgdXJpOiR7dGhpcy5rZXJuZWxJbmZvLnVyaX0sIHJlbW90ZSB1cmk6JHt0aGlzLmtlcm5lbEluZm8ucmVtb3RlVXJpfV0gcHJvY2Vzc2luZyBldmVudCwgJHtKU09OLnN0cmluZ2lmeShlbnZlbG9wZSl9YCk7XHJcblxyXG4gICAgICAgICAgICAgICAgICAgICAgICB0cnkge1xyXG4gICAgICAgICAgICAgICAgICAgICAgICAgICAgY29uc3Qgb3JpZ2luYWwgPSBbLi4uY29tbWFuZEludm9jYXRpb24uY29tbWFuZEVudmVsb3BlPy5yb3V0aW5nU2xpcCA/PyBbXV07XHJcbiAgICAgICAgICAgICAgICAgICAgICAgICAgICByb3V0aW5nU2xpcC5jb250aW51ZUNvbW1hbmRSb3V0aW5nU2xpcChjb21tYW5kSW52b2NhdGlvbi5jb21tYW5kRW52ZWxvcGUsIGVudmVsb3BlLmNvbW1hbmQhLnJvdXRpbmdTbGlwISk7XHJcbiAgICAgICAgICAgICAgICAgICAgICAgICAgICBlbnZlbG9wZS5jb21tYW5kIS5yb3V0aW5nU2xpcCA9IFsuLi5jb21tYW5kSW52b2NhdGlvbi5jb21tYW5kRW52ZWxvcGUucm91dGluZ1NsaXAgPz8gW11dOy8vP1xyXG4gICAgICAgICAgICAgICAgICAgICAgICAgICAgTG9nZ2VyLmRlZmF1bHQud2FybihgcHJveHkgbmFtZT0ke3RoaXMubmFtZX1bbG9jYWwgdXJpOiR7dGhpcy5rZXJuZWxJbmZvLnVyaX0sIGNvbW1hbmQgcm91dGluZ1NsaXAgOiR7b3JpZ2luYWx9XSBoYXMgY2hhbmdlZCB0bzogJHtKU09OLnN0cmluZ2lmeShjb21tYW5kSW52b2NhdGlvbi5jb21tYW5kRW52ZWxvcGUucm91dGluZ1NsaXAgPz8gW10pfWApO1xyXG4gICAgICAgICAgICAgICAgICAgICAgICB9IGNhdGNoIChlOiBhbnkpIHtcclxuICAgICAgICAgICAgICAgICAgICAgICAgICAgIExvZ2dlci5kZWZhdWx0LmVycm9yKGBwcm94eSBuYW1lPSR7dGhpcy5uYW1lfVtsb2NhbCB1cmk6JHt0aGlzLmtlcm5lbEluZm8udXJpfSwgZXJyb3IgJHtlPy5tZXNzYWdlfWApO1xyXG4gICAgICAgICAgICAgICAgICAgICAgICB9XHJcblxyXG4gICAgICAgICAgICAgICAgICAgICAgICBzd2l0Y2ggKGVudmVsb3BlLmV2ZW50VHlwZSkge1xyXG4gICAgICAgICAgICAgICAgICAgICAgICAgICAgY2FzZSBjb250cmFjdHMuS2VybmVsSW5mb1Byb2R1Y2VkVHlwZTpcclxuICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICB7XHJcbiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIGNvbnN0IGtlcm5lbEluZm9Qcm9kdWNlZCA9IDxjb250cmFjdHMuS2VybmVsSW5mb1Byb2R1Y2VkPmVudmVsb3BlLmV2ZW50O1xyXG4gICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICBpZiAoa2VybmVsSW5mb1Byb2R1Y2VkLmtlcm5lbEluZm8udXJpID09PSB0aGlzLmtlcm5lbEluZm8ucmVtb3RlVXJpKSB7XHJcbiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICB0aGlzLnVwZGF0ZUtlcm5lbEluZm9Gcm9tRXZlbnQoa2VybmVsSW5mb1Byb2R1Y2VkKTtcclxuICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIHRoaXMuZGVsZWdhdGVQdWJsaWNhdGlvbihcclxuICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICB7XHJcbiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIGV2ZW50VHlwZTogY29udHJhY3RzLktlcm5lbEluZm9Qcm9kdWNlZFR5cGUsXHJcbiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIGV2ZW50OiB7IGtlcm5lbEluZm86IHRoaXMua2VybmVsSW5mbyB9LFxyXG4gICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICByb3V0aW5nU2xpcDogZW52ZWxvcGUucm91dGluZ1NsaXAsXHJcbiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIGNvbW1hbmQ6IGNvbW1hbmRJbnZvY2F0aW9uLmNvbW1hbmRFbnZlbG9wZVxyXG4gICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIH0sIGNvbW1hbmRJbnZvY2F0aW9uLmNvbnRleHQpO1xyXG4gICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgdGhpcy5kZWxlZ2F0ZVB1YmxpY2F0aW9uKGVudmVsb3BlLCBjb21tYW5kSW52b2NhdGlvbi5jb250ZXh0KTtcclxuICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgfSBlbHNlIHtcclxuICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIHRoaXMuZGVsZWdhdGVQdWJsaWNhdGlvbihlbnZlbG9wZSwgY29tbWFuZEludm9jYXRpb24uY29udGV4dCk7XHJcbiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIH1cclxuICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICB9XHJcbiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgYnJlYWs7XHJcbiAgICAgICAgICAgICAgICAgICAgICAgICAgICBjYXNlIGNvbnRyYWN0cy5Db21tYW5kQ2FuY2VsbGVkVHlwZTpcclxuICAgICAgICAgICAgICAgICAgICAgICAgICAgIGNhc2UgY29udHJhY3RzLkNvbW1hbmRGYWlsZWRUeXBlOlxyXG4gICAgICAgICAgICAgICAgICAgICAgICAgICAgY2FzZSBjb250cmFjdHMuQ29tbWFuZFN1Y2NlZWRlZFR5cGU6XHJcbiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgTG9nZ2VyLmRlZmF1bHQuaW5mbyhgcHJveHkgbmFtZT0ke3RoaXMubmFtZX1bbG9jYWwgdXJpOiR7dGhpcy5rZXJuZWxJbmZvLnVyaX0sIHJlbW90ZSB1cmk6JHt0aGlzLmtlcm5lbEluZm8ucmVtb3RlVXJpfV0gZmluaXNoZWQsIGVudmVsb3BlaWQ9JHtlbnZlbG9wZS5jb21tYW5kIS5pZH0sIGNvbW1hbmRpZD0ke2NvbW1hbmRJZH1gKTtcclxuICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICBpZiAoZW52ZWxvcGUuY29tbWFuZCEuaWQgPT09IGNvbW1hbmRJZCkge1xyXG4gICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICBMb2dnZXIuZGVmYXVsdC5pbmZvKGBwcm94eSBuYW1lPSR7dGhpcy5uYW1lfVtsb2NhbCB1cmk6JHt0aGlzLmtlcm5lbEluZm8udXJpfSwgcmVtb3RlIHVyaToke3RoaXMua2VybmVsSW5mby5yZW1vdGVVcml9XSByZXNvbHZpbmcgcHJvbWlzZSwgZW52ZWxvcGVpZD0ke2VudmVsb3BlLmNvbW1hbmQhLmlkfSwgY29tbWFuZGlkPSR7Y29tbWFuZElkfWApO1xyXG4gICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICBjb21wbGV0aW9uU291cmNlLnJlc29sdmUoZW52ZWxvcGUpO1xyXG4gICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIH0gZWxzZSB7XHJcbiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIExvZ2dlci5kZWZhdWx0LmluZm8oYHByb3h5IG5hbWU9JHt0aGlzLm5hbWV9W2xvY2FsIHVyaToke3RoaXMua2VybmVsSW5mby51cml9LCByZW1vdGUgdXJpOiR7dGhpcy5rZXJuZWxJbmZvLnJlbW90ZVVyaX1dIG5vdCByZXNvbHZpbmcgcHJvbWlzZSwgZW52ZWxvcGVpZD0ke2VudmVsb3BlLmNvbW1hbmQhLmlkfSwgY29tbWFuZGlkPSR7Y29tbWFuZElkfWApO1xyXG4gICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICB0aGlzLmRlbGVnYXRlUHVibGljYXRpb24oZW52ZWxvcGUsIGNvbW1hbmRJbnZvY2F0aW9uLmNvbnRleHQpO1xyXG4gICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIH1cclxuICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICBicmVhaztcclxuICAgICAgICAgICAgICAgICAgICAgICAgICAgIGRlZmF1bHQ6XHJcbiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgdGhpcy5kZWxlZ2F0ZVB1YmxpY2F0aW9uKGVudmVsb3BlLCBjb21tYW5kSW52b2NhdGlvbi5jb250ZXh0KTtcclxuICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICBicmVhaztcclxuICAgICAgICAgICAgICAgICAgICAgICAgfVxyXG4gICAgICAgICAgICAgICAgICAgIH1cclxuICAgICAgICAgICAgICAgIH1cclxuICAgICAgICAgICAgfVxyXG4gICAgICAgIH0pO1xyXG5cclxuICAgICAgICB0cnkge1xyXG4gICAgICAgICAgICBpZiAoIWNvbW1hbmRJbnZvY2F0aW9uLmNvbW1hbmRFbnZlbG9wZS5jb21tYW5kLmRlc3RpbmF0aW9uVXJpIHx8ICFjb21tYW5kSW52b2NhdGlvbi5jb21tYW5kRW52ZWxvcGUuY29tbWFuZC5vcmlnaW5VcmkpIHtcclxuICAgICAgICAgICAgICAgIGNvbW1hbmRJbnZvY2F0aW9uLmNvbW1hbmRFbnZlbG9wZS5jb21tYW5kLm9yaWdpblVyaSA/Pz0gdGhpcy5rZXJuZWxJbmZvLnVyaTtcclxuICAgICAgICAgICAgICAgIGNvbW1hbmRJbnZvY2F0aW9uLmNvbW1hbmRFbnZlbG9wZS5jb21tYW5kLmRlc3RpbmF0aW9uVXJpID8/PSB0aGlzLmtlcm5lbEluZm8ucmVtb3RlVXJpO1xyXG4gICAgICAgICAgICB9XHJcblxyXG4gICAgICAgICAgICBjb21tYW5kSW52b2NhdGlvbi5jb21tYW5kRW52ZWxvcGUucm91dGluZ1NsaXA7Ly8/XHJcblxyXG4gICAgICAgICAgICBpZiAoY29tbWFuZEludm9jYXRpb24uY29tbWFuZEVudmVsb3BlLmNvbW1hbmRUeXBlID09PSBjb250cmFjdHMuUmVxdWVzdEtlcm5lbEluZm9UeXBlKSB7XHJcbiAgICAgICAgICAgICAgICBjb25zdCBkZXN0aW5hdGlvblVyaSA9IHRoaXMua2VybmVsSW5mby5yZW1vdGVVcmkhO1xyXG4gICAgICAgICAgICAgICAgaWYgKHJvdXRpbmdTbGlwLmNvbW1hbmRSb3V0aW5nU2xpcENvbnRhaW5zKGNvbW1hbmRJbnZvY2F0aW9uLmNvbW1hbmRFbnZlbG9wZSwgZGVzdGluYXRpb25VcmksIHRydWUpKSB7XHJcbiAgICAgICAgICAgICAgICAgICAgcmV0dXJuIFByb21pc2UucmVzb2x2ZSgpO1xyXG4gICAgICAgICAgICAgICAgfVxyXG4gICAgICAgICAgICB9XHJcbiAgICAgICAgICAgIExvZ2dlci5kZWZhdWx0LmluZm8oYHByb3h5ICR7dGhpcy5uYW1lfVtsb2NhbCB1cmk6JHt0aGlzLmtlcm5lbEluZm8udXJpfSwgcmVtb3RlIHVyaToke3RoaXMua2VybmVsSW5mby5yZW1vdGVVcml9XSBmb3J3YXJkaW5nIGNvbW1hbmQgJHtjb21tYW5kSW52b2NhdGlvbi5jb21tYW5kRW52ZWxvcGUuY29tbWFuZFR5cGV9IHRvICR7Y29tbWFuZEludm9jYXRpb24uY29tbWFuZEVudmVsb3BlLmNvbW1hbmQuZGVzdGluYXRpb25Vcml9YCk7XHJcbiAgICAgICAgICAgIHRoaXMuX3NlbmRlci5zZW5kKGNvbW1hbmRJbnZvY2F0aW9uLmNvbW1hbmRFbnZlbG9wZSk7XHJcbiAgICAgICAgICAgIExvZ2dlci5kZWZhdWx0LmluZm8oYHByb3h5ICR7dGhpcy5uYW1lfVtsb2NhbCB1cmk6JHt0aGlzLmtlcm5lbEluZm8udXJpfSwgcmVtb3RlIHVyaToke3RoaXMua2VybmVsSW5mby5yZW1vdGVVcml9XSBhYm91dCB0byBhd2FpdCB3aXRoIHRva2VuICR7Y29tbWFuZFRva2VufSBhbmQgIGNvbW1hbmRpZCAke2NvbW1hbmRJZH1gKTtcclxuICAgICAgICAgICAgY29uc3QgZW52ZW50RW52ZWxvcGUgPSBhd2FpdCBjb21wbGV0aW9uU291cmNlLnByb21pc2U7XHJcbiAgICAgICAgICAgIGlmIChlbnZlbnRFbnZlbG9wZS5ldmVudFR5cGUgPT09IGNvbnRyYWN0cy5Db21tYW5kRmFpbGVkVHlwZSkge1xyXG4gICAgICAgICAgICAgICAgY29tbWFuZEludm9jYXRpb24uY29udGV4dC5mYWlsKCg8Y29udHJhY3RzLkNvbW1hbmRGYWlsZWQ+ZW52ZW50RW52ZWxvcGUuZXZlbnQpLm1lc3NhZ2UpO1xyXG4gICAgICAgICAgICB9XHJcbiAgICAgICAgICAgIExvZ2dlci5kZWZhdWx0LmluZm8oYHByb3h5ICR7dGhpcy5uYW1lfVtsb2NhbCB1cmk6JHt0aGlzLmtlcm5lbEluZm8udXJpfSwgcmVtb3RlIHVyaToke3RoaXMua2VybmVsSW5mby5yZW1vdGVVcml9XSBkb25lIGF3YWl0aW5nIHdpdGggdG9rZW4gJHtjb21tYW5kVG9rZW59fSBhbmQgIGNvbW1hbmRpZCAke2NvbW1hbmRJZH1gKTtcclxuICAgICAgICB9XHJcbiAgICAgICAgY2F0Y2ggKGUpIHtcclxuICAgICAgICAgICAgY29tbWFuZEludm9jYXRpb24uY29udGV4dC5mYWlsKCg8YW55PmUpLm1lc3NhZ2UpO1xyXG4gICAgICAgIH1cclxuICAgICAgICBmaW5hbGx5IHtcclxuICAgICAgICAgICAgZXZlbnRTdWJzY3JpcHRpb24udW5zdWJzY3JpYmUoKTtcclxuICAgICAgICB9XHJcbiAgICB9XHJcbn1cclxuIiwiLy8gQ29weXJpZ2h0IChjKSAuTkVUIEZvdW5kYXRpb24gYW5kIGNvbnRyaWJ1dG9ycy4gQWxsIHJpZ2h0cyByZXNlcnZlZC5cclxuLy8gTGljZW5zZWQgdW5kZXIgdGhlIE1JVCBsaWNlbnNlLiBTZWUgTElDRU5TRSBmaWxlIGluIHRoZSBwcm9qZWN0IHJvb3QgZm9yIGZ1bGwgbGljZW5zZSBpbmZvcm1hdGlvbi5cclxuXHJcbmltcG9ydCB7IENvbXBvc2l0ZUtlcm5lbCB9IGZyb20gJy4vY29tcG9zaXRlS2VybmVsJztcclxuaW1wb3J0ICogYXMgY29udHJhY3RzIGZyb20gJy4vY29udHJhY3RzJztcclxuaW1wb3J0ICogYXMgY29ubmVjdGlvbiBmcm9tICcuL2Nvbm5lY3Rpb24nO1xyXG5pbXBvcnQgKiBhcyByb3V0aW5nU2xpcCBmcm9tICcuL3JvdXRpbmdzbGlwJztcclxuaW1wb3J0IHsgS2VybmVsIH0gZnJvbSAnLi9rZXJuZWwnO1xyXG5pbXBvcnQgeyBQcm94eUtlcm5lbCB9IGZyb20gJy4vcHJveHlLZXJuZWwnO1xyXG5pbXBvcnQgeyBMb2dnZXIgfSBmcm9tICcuL2xvZ2dlcic7XHJcbmltcG9ydCB7IEtlcm5lbFNjaGVkdWxlciB9IGZyb20gJy4va2VybmVsU2NoZWR1bGVyJztcclxuXHJcbmV4cG9ydCBjbGFzcyBLZXJuZWxIb3N0IHtcclxuICAgIHByaXZhdGUgcmVhZG9ubHkgX3JlbW90ZVVyaVRvS2VybmVsID0gbmV3IE1hcDxzdHJpbmcsIEtlcm5lbD4oKTtcclxuICAgIHByaXZhdGUgcmVhZG9ubHkgX3VyaVRvS2VybmVsID0gbmV3IE1hcDxzdHJpbmcsIEtlcm5lbD4oKTtcclxuICAgIHByaXZhdGUgcmVhZG9ubHkgX2tlcm5lbFRvS2VybmVsSW5mbyA9IG5ldyBNYXA8S2VybmVsLCBjb250cmFjdHMuS2VybmVsSW5mbz4oKTtcclxuICAgIHByaXZhdGUgcmVhZG9ubHkgX3VyaTogc3RyaW5nO1xyXG4gICAgcHJpdmF0ZSByZWFkb25seSBfc2NoZWR1bGVyOiBLZXJuZWxTY2hlZHVsZXI8Y29udHJhY3RzLktlcm5lbENvbW1hbmRFbnZlbG9wZT47XHJcbiAgICBwcml2YXRlIF9rZXJuZWw6IENvbXBvc2l0ZUtlcm5lbDtcclxuICAgIHByaXZhdGUgX2RlZmF1bHRDb25uZWN0b3I6IGNvbm5lY3Rpb24uQ29ubmVjdG9yO1xyXG4gICAgcHJpdmF0ZSByZWFkb25seSBfY29ubmVjdG9yczogY29ubmVjdGlvbi5Db25uZWN0b3JbXSA9IFtdO1xyXG5cclxuICAgIGNvbnN0cnVjdG9yKGtlcm5lbDogQ29tcG9zaXRlS2VybmVsLCBzZW5kZXI6IGNvbm5lY3Rpb24uSUtlcm5lbENvbW1hbmRBbmRFdmVudFNlbmRlciwgcmVjZWl2ZXI6IGNvbm5lY3Rpb24uSUtlcm5lbENvbW1hbmRBbmRFdmVudFJlY2VpdmVyLCBob3N0VXJpOiBzdHJpbmcpIHtcclxuICAgICAgICB0aGlzLl9rZXJuZWwgPSBrZXJuZWw7XHJcbiAgICAgICAgdGhpcy5fdXJpID0gcm91dGluZ1NsaXAuY3JlYXRlS2VybmVsVXJpKGhvc3RVcmkgfHwgXCJrZXJuZWw6Ly92c2NvZGVcIik7XHJcblxyXG4gICAgICAgIHRoaXMuX2tlcm5lbC5ob3N0ID0gdGhpcztcclxuICAgICAgICB0aGlzLl9zY2hlZHVsZXIgPSBuZXcgS2VybmVsU2NoZWR1bGVyPGNvbnRyYWN0cy5LZXJuZWxDb21tYW5kRW52ZWxvcGU+KCk7XHJcblxyXG4gICAgICAgIHRoaXMuX2RlZmF1bHRDb25uZWN0b3IgPSBuZXcgY29ubmVjdGlvbi5Db25uZWN0b3IoeyBzZW5kZXIsIHJlY2VpdmVyIH0pO1xyXG4gICAgICAgIHRoaXMuX2Nvbm5lY3RvcnMucHVzaCh0aGlzLl9kZWZhdWx0Q29ubmVjdG9yKTtcclxuICAgIH1cclxuXHJcbiAgICBwdWJsaWMgZ2V0IGRlZmF1bHRDb25uZWN0b3IoKTogY29ubmVjdGlvbi5Db25uZWN0b3Ige1xyXG4gICAgICAgIHJldHVybiB0aGlzLl9kZWZhdWx0Q29ubmVjdG9yO1xyXG4gICAgfVxyXG5cclxuICAgIHB1YmxpYyBnZXQgdXJpKCk6IHN0cmluZyB7XHJcbiAgICAgICAgcmV0dXJuIHRoaXMuX3VyaTtcclxuICAgIH1cclxuXHJcbiAgICBwdWJsaWMgdHJ5R2V0S2VybmVsQnlSZW1vdGVVcmkocmVtb3RlVXJpOiBzdHJpbmcpOiBLZXJuZWwgfCB1bmRlZmluZWQge1xyXG4gICAgICAgIHJldHVybiB0aGlzLl9yZW1vdGVVcmlUb0tlcm5lbC5nZXQocmVtb3RlVXJpKTtcclxuICAgIH1cclxuXHJcbiAgICBwdWJsaWMgdHJ5Z2V0S2VybmVsQnlPcmlnaW5Vcmkob3JpZ2luVXJpOiBzdHJpbmcpOiBLZXJuZWwgfCB1bmRlZmluZWQge1xyXG4gICAgICAgIHJldHVybiB0aGlzLl91cmlUb0tlcm5lbC5nZXQob3JpZ2luVXJpKTtcclxuICAgIH1cclxuXHJcbiAgICBwdWJsaWMgdHJ5R2V0S2VybmVsSW5mbyhrZXJuZWw6IEtlcm5lbCk6IGNvbnRyYWN0cy5LZXJuZWxJbmZvIHwgdW5kZWZpbmVkIHtcclxuICAgICAgICByZXR1cm4gdGhpcy5fa2VybmVsVG9LZXJuZWxJbmZvLmdldChrZXJuZWwpO1xyXG4gICAgfVxyXG5cclxuICAgIHB1YmxpYyBhZGRLZXJuZWxJbmZvKGtlcm5lbDogS2VybmVsLCBrZXJuZWxJbmZvOiBjb250cmFjdHMuS2VybmVsSW5mbykge1xyXG4gICAgICAgIGtlcm5lbEluZm8udXJpID0gcm91dGluZ1NsaXAuY3JlYXRlS2VybmVsVXJpKGAke3RoaXMuX3VyaX0ke2tlcm5lbC5uYW1lfWApO1xyXG4gICAgICAgIHRoaXMuX2tlcm5lbFRvS2VybmVsSW5mby5zZXQoa2VybmVsLCBrZXJuZWxJbmZvKTtcclxuICAgICAgICB0aGlzLl91cmlUb0tlcm5lbC5zZXQoa2VybmVsSW5mby51cmksIGtlcm5lbCk7XHJcbiAgICB9XHJcblxyXG4gICAgcHVibGljIGdldEtlcm5lbChrZXJuZWxDb21tYW5kRW52ZWxvcGU6IGNvbnRyYWN0cy5LZXJuZWxDb21tYW5kRW52ZWxvcGUpOiBLZXJuZWwge1xyXG5cclxuICAgICAgICBjb25zdCB1cmlUb0xvb2t1cCA9IGtlcm5lbENvbW1hbmRFbnZlbG9wZS5jb21tYW5kLmRlc3RpbmF0aW9uVXJpID8/IGtlcm5lbENvbW1hbmRFbnZlbG9wZS5jb21tYW5kLm9yaWdpblVyaTtcclxuICAgICAgICBsZXQga2VybmVsOiBLZXJuZWwgfCB1bmRlZmluZWQgPSB1bmRlZmluZWQ7XHJcbiAgICAgICAgaWYgKHVyaVRvTG9va3VwKSB7XHJcbiAgICAgICAgICAgIGtlcm5lbCA9IHRoaXMuX2tlcm5lbC5maW5kS2VybmVsQnlVcmkodXJpVG9Mb29rdXApO1xyXG4gICAgICAgIH1cclxuXHJcbiAgICAgICAgaWYgKCFrZXJuZWwpIHtcclxuICAgICAgICAgICAgaWYgKGtlcm5lbENvbW1hbmRFbnZlbG9wZS5jb21tYW5kLnRhcmdldEtlcm5lbE5hbWUpIHtcclxuICAgICAgICAgICAgICAgIGtlcm5lbCA9IHRoaXMuX2tlcm5lbC5maW5kS2VybmVsQnlOYW1lKGtlcm5lbENvbW1hbmRFbnZlbG9wZS5jb21tYW5kLnRhcmdldEtlcm5lbE5hbWUpO1xyXG4gICAgICAgICAgICB9XHJcbiAgICAgICAgfVxyXG5cclxuICAgICAgICBrZXJuZWwgPz89IHRoaXMuX2tlcm5lbDtcclxuICAgICAgICBMb2dnZXIuZGVmYXVsdC5pbmZvKGBVc2luZyBLZXJuZWwgJHtrZXJuZWwubmFtZX1gKTtcclxuICAgICAgICByZXR1cm4ga2VybmVsO1xyXG4gICAgfVxyXG5cclxuICAgIHB1YmxpYyBjb25uZWN0UHJveHlLZXJuZWxPbkRlZmF1bHRDb25uZWN0b3IobG9jYWxOYW1lOiBzdHJpbmcsIHJlbW90ZUtlcm5lbFVyaT86IHN0cmluZywgYWxpYXNlcz86IHN0cmluZ1tdKTogUHJveHlLZXJuZWwge1xyXG4gICAgICAgIHJldHVybiB0aGlzLmNvbm5lY3RQcm94eUtlcm5lbE9uQ29ubmVjdG9yKGxvY2FsTmFtZSwgdGhpcy5fZGVmYXVsdENvbm5lY3Rvci5zZW5kZXIsIHRoaXMuX2RlZmF1bHRDb25uZWN0b3IucmVjZWl2ZXIsIHJlbW90ZUtlcm5lbFVyaSwgYWxpYXNlcyk7XHJcbiAgICB9XHJcblxyXG4gICAgcHVibGljIHRyeUFkZENvbm5lY3Rvcihjb25uZWN0b3I6IHsgc2VuZGVyOiBjb25uZWN0aW9uLklLZXJuZWxDb21tYW5kQW5kRXZlbnRTZW5kZXIsIHJlY2VpdmVyOiBjb25uZWN0aW9uLklLZXJuZWxDb21tYW5kQW5kRXZlbnRSZWNlaXZlciwgcmVtb3RlVXJpcz86IHN0cmluZ1tdIH0pIHtcclxuICAgICAgICBpZiAoIWNvbm5lY3Rvci5yZW1vdGVVcmlzKSB7XHJcbiAgICAgICAgICAgIHRoaXMuX2Nvbm5lY3RvcnMucHVzaChuZXcgY29ubmVjdGlvbi5Db25uZWN0b3IoY29ubmVjdG9yKSk7XHJcbiAgICAgICAgICAgIHJldHVybiB0cnVlO1xyXG4gICAgICAgIH0gZWxzZSB7XHJcbiAgICAgICAgICAgIGNvbnN0IGZvdW5kID0gY29ubmVjdG9yLnJlbW90ZVVyaXMhLmZpbmQodXJpID0+IHRoaXMuX2Nvbm5lY3RvcnMuZmluZChjID0+IGMuY2FuUmVhY2godXJpKSkpO1xyXG4gICAgICAgICAgICBpZiAoIWZvdW5kKSB7XHJcbiAgICAgICAgICAgICAgICB0aGlzLl9jb25uZWN0b3JzLnB1c2gobmV3IGNvbm5lY3Rpb24uQ29ubmVjdG9yKGNvbm5lY3RvcikpO1xyXG4gICAgICAgICAgICAgICAgcmV0dXJuIHRydWU7XHJcbiAgICAgICAgICAgIH1cclxuICAgICAgICAgICAgcmV0dXJuIGZhbHNlO1xyXG4gICAgICAgIH1cclxuICAgIH1cclxuXHJcbiAgICBwdWJsaWMgdHJ5UmVtb3ZlQ29ubmVjdG9yKGNvbm5lY3RvcjogeyByZW1vdGVVcmlzPzogc3RyaW5nW10gfSkge1xyXG4gICAgICAgIGlmICghY29ubmVjdG9yLnJlbW90ZVVyaXMpIHtcclxuICAgICAgICAgICAgZm9yIChsZXQgdXJpIG9mIGNvbm5lY3Rvci5yZW1vdGVVcmlzISkge1xyXG4gICAgICAgICAgICAgICAgY29uc3QgaW5kZXggPSB0aGlzLl9jb25uZWN0b3JzLmZpbmRJbmRleChjID0+IGMuY2FuUmVhY2godXJpKSk7XHJcbiAgICAgICAgICAgICAgICBpZiAoaW5kZXggPj0gMCkge1xyXG4gICAgICAgICAgICAgICAgICAgIHRoaXMuX2Nvbm5lY3RvcnMuc3BsaWNlKGluZGV4LCAxKTtcclxuICAgICAgICAgICAgICAgIH1cclxuICAgICAgICAgICAgfVxyXG4gICAgICAgICAgICByZXR1cm4gdHJ1ZTtcclxuICAgICAgICB9IGVsc2Uge1xyXG5cclxuICAgICAgICAgICAgcmV0dXJuIGZhbHNlO1xyXG4gICAgICAgIH1cclxuICAgIH1cclxuXHJcbiAgICBwdWJsaWMgY29ubmVjdFByb3h5S2VybmVsKGxvY2FsTmFtZTogc3RyaW5nLCByZW1vdGVLZXJuZWxVcmk6IHN0cmluZywgYWxpYXNlcz86IHN0cmluZ1tdKTogUHJveHlLZXJuZWwge1xyXG4gICAgICAgIHRoaXMuX2Nvbm5lY3RvcnM7Ly8/XHJcbiAgICAgICAgY29uc3QgY29ubmVjdG9yID0gdGhpcy5fY29ubmVjdG9ycy5maW5kKGMgPT4gYy5jYW5SZWFjaChyZW1vdGVLZXJuZWxVcmkpKTtcclxuICAgICAgICBpZiAoIWNvbm5lY3Rvcikge1xyXG4gICAgICAgICAgICB0aHJvdyBuZXcgRXJyb3IoYENhbm5vdCBmaW5kIGNvbm5lY3RvciB0byByZWFjaCAke3JlbW90ZUtlcm5lbFVyaX1gKTtcclxuICAgICAgICB9XHJcbiAgICAgICAgbGV0IGtlcm5lbCA9IG5ldyBQcm94eUtlcm5lbChsb2NhbE5hbWUsIGNvbm5lY3Rvci5zZW5kZXIsIGNvbm5lY3Rvci5yZWNlaXZlcik7XHJcbiAgICAgICAga2VybmVsLmtlcm5lbEluZm8ucmVtb3RlVXJpID0gcmVtb3RlS2VybmVsVXJpO1xyXG4gICAgICAgIHRoaXMuX2tlcm5lbC5hZGQoa2VybmVsLCBhbGlhc2VzKTtcclxuICAgICAgICByZXR1cm4ga2VybmVsO1xyXG4gICAgfVxyXG5cclxuICAgIHByaXZhdGUgY29ubmVjdFByb3h5S2VybmVsT25Db25uZWN0b3IobG9jYWxOYW1lOiBzdHJpbmcsIHNlbmRlcjogY29ubmVjdGlvbi5JS2VybmVsQ29tbWFuZEFuZEV2ZW50U2VuZGVyLCByZWNlaXZlcjogY29ubmVjdGlvbi5JS2VybmVsQ29tbWFuZEFuZEV2ZW50UmVjZWl2ZXIsIHJlbW90ZUtlcm5lbFVyaT86IHN0cmluZywgYWxpYXNlcz86IHN0cmluZ1tdKTogUHJveHlLZXJuZWwge1xyXG4gICAgICAgIGxldCBrZXJuZWwgPSBuZXcgUHJveHlLZXJuZWwobG9jYWxOYW1lLCBzZW5kZXIsIHJlY2VpdmVyKTtcclxuICAgICAgICBrZXJuZWwua2VybmVsSW5mby5yZW1vdGVVcmkgPSByZW1vdGVLZXJuZWxVcmk7XHJcbiAgICAgICAgdGhpcy5fa2VybmVsLmFkZChrZXJuZWwsIGFsaWFzZXMpO1xyXG4gICAgICAgIHJldHVybiBrZXJuZWw7XHJcbiAgICB9XHJcblxyXG4gICAgcHVibGljIHRyeUdldENvbm5lY3RvcihyZW1vdGVVcmk6IHN0cmluZykge1xyXG4gICAgICAgIHJldHVybiB0aGlzLl9jb25uZWN0b3JzLmZpbmQoYyA9PiBjLmNhblJlYWNoKHJlbW90ZVVyaSkpO1xyXG4gICAgfVxyXG5cclxuICAgIHB1YmxpYyBjb25uZWN0KCkge1xyXG4gICAgICAgIHRoaXMuX2tlcm5lbC5zdWJzY3JpYmVUb0tlcm5lbEV2ZW50cyhlID0+IHtcclxuICAgICAgICAgICAgTG9nZ2VyLmRlZmF1bHQuaW5mbyhgS2VybmVsSG9zdCBmb3J3YXJkaW5nIGV2ZW50OiAke0pTT04uc3RyaW5naWZ5KGUpfWApO1xyXG4gICAgICAgICAgICB0aGlzLl9kZWZhdWx0Q29ubmVjdG9yLnNlbmRlci5zZW5kKGUpO1xyXG4gICAgICAgIH0pO1xyXG5cclxuICAgICAgICB0aGlzLl9kZWZhdWx0Q29ubmVjdG9yLnJlY2VpdmVyLnN1YnNjcmliZSh7XHJcbiAgICAgICAgICAgIG5leHQ6IChrZXJuZWxDb21tYW5kT3JFdmVudEVudmVsb3BlOiBjb25uZWN0aW9uLktlcm5lbENvbW1hbmRPckV2ZW50RW52ZWxvcGUpID0+IHtcclxuICAgICAgICAgICAgICAgIGlmIChjb25uZWN0aW9uLmlzS2VybmVsQ29tbWFuZEVudmVsb3BlKGtlcm5lbENvbW1hbmRPckV2ZW50RW52ZWxvcGUpKSB7XHJcbiAgICAgICAgICAgICAgICAgICAgTG9nZ2VyLmRlZmF1bHQuaW5mbyhgS2VybmVsSG9zdCBkaXNwYWN0aGluZyBjb21tYW5kOiAke0pTT04uc3RyaW5naWZ5KGtlcm5lbENvbW1hbmRPckV2ZW50RW52ZWxvcGUpfWApO1xyXG4gICAgICAgICAgICAgICAgICAgIHRoaXMuX3NjaGVkdWxlci5ydW5Bc3luYyhrZXJuZWxDb21tYW5kT3JFdmVudEVudmVsb3BlLCBjb21tYW5kRW52ZWxvcGUgPT4ge1xyXG4gICAgICAgICAgICAgICAgICAgICAgICBjb25zdCBrZXJuZWwgPSB0aGlzLl9rZXJuZWw7XHJcbiAgICAgICAgICAgICAgICAgICAgICAgIHJldHVybiBrZXJuZWwuc2VuZChjb21tYW5kRW52ZWxvcGUpO1xyXG4gICAgICAgICAgICAgICAgICAgIH0pO1xyXG4gICAgICAgICAgICAgICAgfVxyXG4gICAgICAgICAgICB9XHJcbiAgICAgICAgfSk7XHJcblxyXG4gICAgICAgIHRoaXMuX2RlZmF1bHRDb25uZWN0b3Iuc2VuZGVyLnNlbmQoeyBldmVudFR5cGU6IGNvbnRyYWN0cy5LZXJuZWxSZWFkeVR5cGUsIGV2ZW50OiB7fSwgcm91dGluZ1NsaXA6IFt0aGlzLl9rZXJuZWwua2VybmVsSW5mby51cmkhXSB9KTtcclxuXHJcbiAgICAgICAgdGhpcy5wdWJsaXNoS2VybmVJbmZvKCk7XHJcbiAgICB9XHJcblxyXG4gICAgcHVibGljIHB1Ymxpc2hLZXJuZUluZm8oKSB7XHJcblxyXG4gICAgICAgIGNvbnN0IGV2ZW50cyA9IHRoaXMuZ2V0S2VybmVsSW5mb1Byb2R1Y2VkKCk7XHJcblxyXG4gICAgICAgIGZvciAoY29uc3QgZXZlbnQgb2YgZXZlbnRzKSB7XHJcbiAgICAgICAgICAgIHRoaXMuX2RlZmF1bHRDb25uZWN0b3Iuc2VuZGVyLnNlbmQoZXZlbnQpO1xyXG4gICAgICAgIH1cclxuICAgIH1cclxuXHJcbiAgICBwdWJsaWMgZ2V0S2VybmVsSW5mb1Byb2R1Y2VkKCk6IGNvbnRyYWN0cy5LZXJuZWxFdmVudEVudmVsb3BlW10ge1xyXG4gICAgICAgIGxldCBldmVudHM6IGNvbnRyYWN0cy5LZXJuZWxFdmVudEVudmVsb3BlW10gPSBbXTtcclxuICAgICAgICBldmVudHMucHVzaCh7IGV2ZW50VHlwZTogY29udHJhY3RzLktlcm5lbEluZm9Qcm9kdWNlZFR5cGUsIGV2ZW50OiA8Y29udHJhY3RzLktlcm5lbEluZm9Qcm9kdWNlZD57IGtlcm5lbEluZm86IHRoaXMuX2tlcm5lbC5rZXJuZWxJbmZvIH0sIHJvdXRpbmdTbGlwOiBbdGhpcy5fa2VybmVsLmtlcm5lbEluZm8udXJpIV0gfSk7XHJcblxyXG4gICAgICAgIGZvciAobGV0IGtlcm5lbCBvZiB0aGlzLl9rZXJuZWwuY2hpbGRLZXJuZWxzKSB7XHJcbiAgICAgICAgICAgIGV2ZW50cy5wdXNoKHsgZXZlbnRUeXBlOiBjb250cmFjdHMuS2VybmVsSW5mb1Byb2R1Y2VkVHlwZSwgZXZlbnQ6IDxjb250cmFjdHMuS2VybmVsSW5mb1Byb2R1Y2VkPnsga2VybmVsSW5mbzoga2VybmVsLmtlcm5lbEluZm8gfSwgcm91dGluZ1NsaXA6IFtrZXJuZWwua2VybmVsSW5mby51cmkhXSB9KTtcclxuICAgICAgICB9XHJcblxyXG4gICAgICAgIHJldHVybiBldmVudHM7XHJcbiAgICB9XHJcbn1cclxuIiwiLy8gQ29weXJpZ2h0IChjKSAuTkVUIEZvdW5kYXRpb24gYW5kIGNvbnRyaWJ1dG9ycy4gQWxsIHJpZ2h0cyByZXNlcnZlZC5cclxuLy8gTGljZW5zZWQgdW5kZXIgdGhlIE1JVCBsaWNlbnNlLiBTZWUgTElDRU5TRSBmaWxlIGluIHRoZSBwcm9qZWN0IHJvb3QgZm9yIGZ1bGwgbGljZW5zZSBpbmZvcm1hdGlvbi5cclxuXHJcbmltcG9ydCB7IENvbXBvc2l0ZUtlcm5lbCB9IGZyb20gXCIuLi9jb21wb3NpdGVLZXJuZWxcIjtcclxuaW1wb3J0IHsgSmF2YXNjcmlwdEtlcm5lbCB9IGZyb20gXCIuLi9qYXZhc2NyaXB0S2VybmVsXCI7XHJcbmltcG9ydCB7IExvZ0VudHJ5LCBMb2dnZXIgfSBmcm9tIFwiLi4vbG9nZ2VyXCI7XHJcbmltcG9ydCB7IEtlcm5lbEhvc3QgfSBmcm9tIFwiLi4va2VybmVsSG9zdFwiO1xyXG5pbXBvcnQgKiBhcyByeGpzIGZyb20gXCJyeGpzXCI7XHJcbmltcG9ydCAqIGFzIGNvbm5lY3Rpb24gZnJvbSBcIi4uL2Nvbm5lY3Rpb25cIjtcclxuaW1wb3J0ICogYXMgY29udHJhY3RzIGZyb20gXCIuLi9jb250cmFjdHNcIjtcclxuXHJcbmV4cG9ydCBmdW5jdGlvbiBjcmVhdGVIb3N0KFxyXG4gICAgZ2xvYmFsOiBhbnksXHJcbiAgICBjb21wb3NpdGVLZXJuZWxOYW1lOiBzdHJpbmcsXHJcbiAgICBjb25maWd1cmVSZXF1aXJlOiAoaW50ZXJhY3RpdmU6IGFueSkgPT4gdm9pZCxcclxuICAgIGxvZ01lc3NhZ2U6IChlbnRyeTogTG9nRW50cnkpID0+IHZvaWQsXHJcbiAgICBsb2NhbFRvUmVtb3RlOiByeGpzLk9ic2VydmVyPGNvbm5lY3Rpb24uS2VybmVsQ29tbWFuZE9yRXZlbnRFbnZlbG9wZT4sXHJcbiAgICByZW1vdGVUb0xvY2FsOiByeGpzLk9ic2VydmFibGU8Y29ubmVjdGlvbi5LZXJuZWxDb21tYW5kT3JFdmVudEVudmVsb3BlPixcclxuICAgIG9uUmVhZHk6ICgpID0+IHZvaWQpIHtcclxuICAgIExvZ2dlci5jb25maWd1cmUoY29tcG9zaXRlS2VybmVsTmFtZSwgbG9nTWVzc2FnZSk7XHJcblxyXG4gICAgZ2xvYmFsLmludGVyYWN0aXZlID0ge307XHJcbiAgICBjb25maWd1cmVSZXF1aXJlKGdsb2JhbC5pbnRlcmFjdGl2ZSk7XHJcblxyXG4gICAgY29uc3QgY29tcG9zaXRlS2VybmVsID0gbmV3IENvbXBvc2l0ZUtlcm5lbChjb21wb3NpdGVLZXJuZWxOYW1lKTtcclxuICAgIGNvbnN0IGtlcm5lbEhvc3QgPSBuZXcgS2VybmVsSG9zdChjb21wb3NpdGVLZXJuZWwsIGNvbm5lY3Rpb24uS2VybmVsQ29tbWFuZEFuZEV2ZW50U2VuZGVyLkZyb21PYnNlcnZlcihsb2NhbFRvUmVtb3RlKSwgY29ubmVjdGlvbi5LZXJuZWxDb21tYW5kQW5kRXZlbnRSZWNlaXZlci5Gcm9tT2JzZXJ2YWJsZShyZW1vdGVUb0xvY2FsKSwgYGtlcm5lbDovLyR7Y29tcG9zaXRlS2VybmVsTmFtZX1gKTtcclxuXHJcbiAgICBrZXJuZWxIb3N0LmRlZmF1bHRDb25uZWN0b3IucmVjZWl2ZXIuc3Vic2NyaWJlKHtcclxuICAgICAgICBuZXh0OiAoZW52ZWxvcGUpID0+IHtcclxuICAgICAgICAgICAgaWYgKGNvbm5lY3Rpb24uaXNLZXJuZWxFdmVudEVudmVsb3BlKGVudmVsb3BlKSAmJiBlbnZlbG9wZS5ldmVudFR5cGUgPT09IGNvbnRyYWN0cy5LZXJuZWxJbmZvUHJvZHVjZWRUeXBlKSB7XHJcbiAgICAgICAgICAgICAgICBjb25zdCBrZXJuZWxJbmZvUHJvZHVjZWQgPSA8Y29udHJhY3RzLktlcm5lbEluZm9Qcm9kdWNlZD5lbnZlbG9wZS5ldmVudDtcclxuICAgICAgICAgICAgICAgIGNvbm5lY3Rpb24uZW5zdXJlT3JVcGRhdGVQcm94eUZvcktlcm5lbEluZm8oa2VybmVsSW5mb1Byb2R1Y2VkLCBjb21wb3NpdGVLZXJuZWwpO1xyXG4gICAgICAgICAgICB9XHJcbiAgICAgICAgfVxyXG4gICAgfSk7XHJcblxyXG4gICAgLy8gdXNlIGNvbXBvc2l0ZSBrZXJuZWwgYXMgcm9vdFxyXG5cclxuICAgIGdsb2JhbC5rZXJuZWwgPSB7XHJcbiAgICAgICAgZ2V0IHJvb3QoKSB7XHJcbiAgICAgICAgICAgIHJldHVybiBjb21wb3NpdGVLZXJuZWw7XHJcbiAgICAgICAgfVxyXG4gICAgfTtcclxuXHJcbiAgICBnbG9iYWxbY29tcG9zaXRlS2VybmVsTmFtZV0gPSB7XHJcbiAgICAgICAgY29tcG9zaXRlS2VybmVsLFxyXG4gICAgICAgIGtlcm5lbEhvc3QsXHJcbiAgICB9O1xyXG5cclxuICAgIGNvbnN0IGpzS2VybmVsID0gbmV3IEphdmFzY3JpcHRLZXJuZWwoKTtcclxuICAgIGNvbXBvc2l0ZUtlcm5lbC5hZGQoanNLZXJuZWwsIFtcImpzXCJdKTtcclxuXHJcbiAgICBrZXJuZWxIb3N0LmNvbm5lY3QoKTtcclxuXHJcbiAgICBvblJlYWR5KCk7XHJcbn1cclxuIiwiLy8gQ29weXJpZ2h0IChjKSAuTkVUIEZvdW5kYXRpb24gYW5kIGNvbnRyaWJ1dG9ycy4gQWxsIHJpZ2h0cyByZXNlcnZlZC5cclxuLy8gTGljZW5zZWQgdW5kZXIgdGhlIE1JVCBsaWNlbnNlLiBTZWUgTElDRU5TRSBmaWxlIGluIHRoZSBwcm9qZWN0IHJvb3QgZm9yIGZ1bGwgbGljZW5zZSBpbmZvcm1hdGlvbi5cclxuXHJcbmltcG9ydCAqIGFzIGZyb250RW5kSG9zdCBmcm9tICcuL2Zyb250RW5kSG9zdCc7XHJcbmltcG9ydCAqIGFzIHJ4anMgZnJvbSBcInJ4anNcIjtcclxuaW1wb3J0ICogYXMgY29ubmVjdGlvbiBmcm9tIFwiLi4vY29ubmVjdGlvblwiO1xyXG5pbXBvcnQgeyBMb2dnZXIgfSBmcm9tIFwiLi4vbG9nZ2VyXCI7XHJcbmltcG9ydCB7IEtlcm5lbEhvc3QgfSBmcm9tICcuLi9rZXJuZWxIb3N0JztcclxuXHJcbmV4cG9ydCBmdW5jdGlvbiBjb25maWd1cmUoZ2xvYmFsPzogYW55KSB7XHJcbiAgICBpZiAoIWdsb2JhbCkge1xyXG4gICAgICAgIGdsb2JhbCA9IHdpbmRvdztcclxuICAgIH1cclxuXHJcbiAgICBjb25zdCByZW1vdGVUb0xvY2FsID0gbmV3IHJ4anMuU3ViamVjdDxjb25uZWN0aW9uLktlcm5lbENvbW1hbmRPckV2ZW50RW52ZWxvcGU+KCk7XHJcbiAgICBjb25zdCBsb2NhbFRvUmVtb3RlID0gbmV3IHJ4anMuU3ViamVjdDxjb25uZWN0aW9uLktlcm5lbENvbW1hbmRPckV2ZW50RW52ZWxvcGU+KCk7XHJcblxyXG4gICAgbG9jYWxUb1JlbW90ZS5zdWJzY3JpYmUoe1xyXG4gICAgICAgIG5leHQ6IGVudmVsb3BlID0+IHtcclxuICAgICAgICAgICAgLy8gQHRzLWlnbm9yZVxyXG4gICAgICAgICAgICBwb3N0S2VybmVsTWVzc2FnZSh7IGVudmVsb3BlIH0pO1xyXG4gICAgICAgIH1cclxuICAgIH0pO1xyXG5cclxuICAgIC8vIEB0cy1pZ25vcmVcclxuICAgIG9uRGlkUmVjZWl2ZUtlcm5lbE1lc3NhZ2UoKGFyZzogYW55KSA9PiB7XHJcbiAgICAgICAgaWYgKGFyZy5lbnZlbG9wZSkge1xyXG4gICAgICAgICAgICBjb25zdCBlbnZlbG9wZSA9IDxjb25uZWN0aW9uLktlcm5lbENvbW1hbmRPckV2ZW50RW52ZWxvcGU+PGFueT4oYXJnLmVudmVsb3BlKTtcclxuICAgICAgICAgICAgaWYgKGNvbm5lY3Rpb24uaXNLZXJuZWxFdmVudEVudmVsb3BlKGVudmVsb3BlKSkge1xyXG4gICAgICAgICAgICAgICAgTG9nZ2VyLmRlZmF1bHQuaW5mbyhgY2hhbm5lbCBnb3QgJHtlbnZlbG9wZS5ldmVudFR5cGV9IHdpdGggdG9rZW4gJHtlbnZlbG9wZS5jb21tYW5kPy50b2tlbn0gYW5kIGlkICR7ZW52ZWxvcGUuY29tbWFuZD8uaWR9YCk7XHJcbiAgICAgICAgICAgIH1cclxuXHJcbiAgICAgICAgICAgIHJlbW90ZVRvTG9jYWwubmV4dChlbnZlbG9wZSk7XHJcbiAgICAgICAgfVxyXG4gICAgfSk7XHJcblxyXG4gICAgZnJvbnRFbmRIb3N0LmNyZWF0ZUhvc3QoXHJcbiAgICAgICAgZ2xvYmFsLFxyXG4gICAgICAgICd3ZWJ2aWV3JyxcclxuICAgICAgICBjb25maWd1cmVSZXF1aXJlLFxyXG4gICAgICAgIGVudHJ5ID0+IHtcclxuICAgICAgICAgICAgLy8gQHRzLWlnbm9yZVxyXG4gICAgICAgICAgICBwb3N0S2VybmVsTWVzc2FnZSh7IGxvZ0VudHJ5OiBlbnRyeSB9KTtcclxuICAgICAgICB9LFxyXG4gICAgICAgIGxvY2FsVG9SZW1vdGUsXHJcbiAgICAgICAgcmVtb3RlVG9Mb2NhbCxcclxuICAgICAgICAoKSA9PiB7XHJcbiAgICAgICAgICAgIGNvbnN0IGtlcm5lbEluZm9Qcm9kdWNlZCA9ICg8S2VybmVsSG9zdD4oZ2xvYmFsWyd3ZWJ2aWV3J10ua2VybmVsSG9zdCkpLmdldEtlcm5lbEluZm9Qcm9kdWNlZCgpO1xyXG4gICAgICAgICAgICBjb25zdCBob3N0VXJpID0gKDxLZXJuZWxIb3N0PihnbG9iYWxbJ3dlYnZpZXcnXS5rZXJuZWxIb3N0KSkudXJpO1xyXG4gICAgICAgICAgICAvLyBAdHMtaWdub3JlXHJcbiAgICAgICAgICAgIHBvc3RLZXJuZWxNZXNzYWdlKHsgcHJlbG9hZENvbW1hbmQ6ICcjIWNvbm5lY3QnLCBrZXJuZWxJbmZvUHJvZHVjZWQsIGhvc3RVcmkgfSk7XHJcblxyXG4gICAgICAgIH1cclxuICAgICk7XHJcbn1cclxuXHJcbmZ1bmN0aW9uIGNvbmZpZ3VyZVJlcXVpcmUoaW50ZXJhY3RpdmU6IGFueSkge1xyXG4gICAgaWYgKCh0eXBlb2YgKHJlcXVpcmUpICE9PSB0eXBlb2YgKEZ1bmN0aW9uKSkgfHwgKHR5cGVvZiAoKDxhbnk+cmVxdWlyZSkuY29uZmlnKSAhPT0gdHlwZW9mIChGdW5jdGlvbikpKSB7XHJcbiAgICAgICAgbGV0IHJlcXVpcmVfc2NyaXB0ID0gZG9jdW1lbnQuY3JlYXRlRWxlbWVudCgnc2NyaXB0Jyk7XHJcbiAgICAgICAgcmVxdWlyZV9zY3JpcHQuc2V0QXR0cmlidXRlKCdzcmMnLCAnaHR0cHM6Ly9jZG5qcy5jbG91ZGZsYXJlLmNvbS9hamF4L2xpYnMvcmVxdWlyZS5qcy8yLjMuNi9yZXF1aXJlLm1pbi5qcycpO1xyXG4gICAgICAgIHJlcXVpcmVfc2NyaXB0LnNldEF0dHJpYnV0ZSgndHlwZScsICd0ZXh0L2phdmFzY3JpcHQnKTtcclxuICAgICAgICByZXF1aXJlX3NjcmlwdC5vbmxvYWQgPSBmdW5jdGlvbiAoKSB7XHJcbiAgICAgICAgICAgIGludGVyYWN0aXZlLmNvbmZpZ3VyZVJlcXVpcmUgPSAoY29uZmluZzogYW55KSA9PiB7XHJcbiAgICAgICAgICAgICAgICByZXR1cm4gKDxhbnk+cmVxdWlyZSkuY29uZmlnKGNvbmZpbmcpIHx8IHJlcXVpcmU7XHJcbiAgICAgICAgICAgIH07XHJcblxyXG4gICAgICAgIH07XHJcbiAgICAgICAgZG9jdW1lbnQuZ2V0RWxlbWVudHNCeVRhZ05hbWUoJ2hlYWQnKVswXS5hcHBlbmRDaGlsZChyZXF1aXJlX3NjcmlwdCk7XHJcblxyXG4gICAgfSBlbHNlIHtcclxuICAgICAgICBpbnRlcmFjdGl2ZS5jb25maWd1cmVSZXF1aXJlID0gKGNvbmZpbmc6IGFueSkgPT4ge1xyXG4gICAgICAgICAgICByZXR1cm4gKDxhbnk+cmVxdWlyZSkuY29uZmlnKGNvbmZpbmcpIHx8IHJlcXVpcmU7XHJcbiAgICAgICAgfTtcclxuICAgIH1cclxufVxyXG5cclxuTG9nZ2VyLmRlZmF1bHQuaW5mbyhgc2V0dGluZyB1cCAnd2VidmlldycgaG9zdGApO1xyXG5jb25maWd1cmUod2luZG93KTtcclxuTG9nZ2VyLmRlZmF1bHQuaW5mbyhgc2V0IHVwICd3ZWJ2aWV3JyBob3N0IGNvbXBsZXRlYCk7XHJcbiJdLCJuYW1lcyI6WyJTeW1ib2xfb2JzZXJ2YWJsZSIsInJ4anMuU3ViamVjdCIsImNvbnRyYWN0cy5Db21tYW5kU3VjY2VlZGVkVHlwZSIsImNvbnRyYWN0cy5Db21tYW5kRmFpbGVkVHlwZSIsInJvdXRpbmdzbGlwLmV2ZW50Um91dGluZ1NsaXBDb250YWlucyIsInJvdXRpbmdzbGlwLnN0YW1wRXZlbnRSb3V0aW5nU2xpcCIsInJvdXRpbmdzbGlwLmNyZWF0ZUtlcm5lbFVyaSIsImNvbnRyYWN0cy5SZXF1ZXN0S2VybmVsSW5mb1R5cGUiLCJjb250cmFjdHMuS2VybmVsSW5mb1Byb2R1Y2VkVHlwZSIsInJvdXRpbmdzbGlwLmNvbW1hbmRSb3V0aW5nU2xpcENvbnRhaW5zIiwicm91dGluZ3NsaXAuc3RhbXBDb21tYW5kUm91dGluZ1NsaXBBc0Fycml2ZWQiLCJyb3V0aW5nc2xpcC5zdGFtcENvbW1hbmRSb3V0aW5nU2xpcCIsInJ4anMubWFwIiwicm91dGluZ3NsaXAuY29udGludWVDb21tYW5kUm91dGluZ1NsaXAiLCJjb250cmFjdHMuRGlzcGxheWVkVmFsdWVQcm9kdWNlZFR5cGUiLCJjb250cmFjdHMuU3VibWl0Q29kZVR5cGUiLCJjb250cmFjdHMuUmVxdWVzdFZhbHVlSW5mb3NUeXBlIiwiY29udHJhY3RzLlJlcXVlc3RWYWx1ZVR5cGUiLCJjb250cmFjdHMuU2VuZFZhbHVlVHlwZSIsImNvbnRyYWN0cy5Db2RlU3VibWlzc2lvblJlY2VpdmVkVHlwZSIsImNvbnRyYWN0cy5SZXR1cm5WYWx1ZVByb2R1Y2VkVHlwZSIsImNvbnRyYWN0cy5WYWx1ZUluZm9zUHJvZHVjZWRUeXBlIiwiY29udHJhY3RzLlZhbHVlUHJvZHVjZWRUeXBlIiwicm91dGluZ1NsaXAuZXZlbnRSb3V0aW5nU2xpcENvbnRhaW5zIiwicm91dGluZ1NsaXAuc3RhbXBFdmVudFJvdXRpbmdTbGlwIiwiY29ubmVjdGlvbi51cGRhdGVLZXJuZWxJbmZvIiwiY29ubmVjdGlvbi5pc0tlcm5lbEV2ZW50RW52ZWxvcGUiLCJyb3V0aW5nU2xpcC5jb250aW51ZUNvbW1hbmRSb3V0aW5nU2xpcCIsImNvbnRyYWN0cy5Db21tYW5kQ2FuY2VsbGVkVHlwZSIsInJvdXRpbmdTbGlwLmNvbW1hbmRSb3V0aW5nU2xpcENvbnRhaW5zIiwicm91dGluZ1NsaXAuY3JlYXRlS2VybmVsVXJpIiwiY29ubmVjdGlvbi5Db25uZWN0b3IiLCJjb25uZWN0aW9uLmlzS2VybmVsQ29tbWFuZEVudmVsb3BlIiwiY29udHJhY3RzLktlcm5lbFJlYWR5VHlwZSIsImNvbm5lY3Rpb24uS2VybmVsQ29tbWFuZEFuZEV2ZW50U2VuZGVyIiwiY29ubmVjdGlvbi5LZXJuZWxDb21tYW5kQW5kRXZlbnRSZWNlaXZlciIsImNvbm5lY3Rpb24uZW5zdXJlT3JVcGRhdGVQcm94eUZvcktlcm5lbEluZm8iLCJmcm9udEVuZEhvc3QuY3JlYXRlSG9zdCJdLCJtYXBwaW5ncyI6Ijs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7SUFBQSxJQUFJLEdBQUcsQ0FBQyxDQUFDLElBQUksQ0FBYyxJQUFJLENBQUMsQ0FBQyxDQUFDLEdBQUcsQ0FBQyxDQUFDLEVBQUUsQ0FBQyxTQUFTLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxHQUFHLFFBQVEsRUFBRSxPQUFPLENBQUMsQ0FBQyxNQUFNLElBQUksU0FBUyxDQUFDLGtDQUFrQyxDQUFDLElBQUksQ0FBQyxTQUFTLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxTQUFTLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsSUFBSSxJQUFJLENBQUMsQ0FBQyxDQUFDLENBQUMsRUFBRSxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLEVBQUUsQ0FBQyxDQUFDLE1BQU0sQ0FBQyxFQUFFLENBQUMsQ0FBQyxDQUFDLEdBQUcsQ0FBQyxDQUFDLENBQUMsQ0FBQyxNQUFNLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxVQUFVLENBQUMsQ0FBQyxDQUFDLENBQUMsS0FBSSxDQUFDLEdBQUcsRUFBRSxHQUFHLENBQUMsQ0FBQyxNQUFNLENBQUMsQ0FBQyxHQUFFLENBQUMsR0FBRyxFQUFFLEdBQUcsQ0FBQyxDQUFDLENBQUMsR0FBRyxDQUFDLEdBQUcsQ0FBQyxDQUFDLENBQUMsRUFBRSxDQUFDLEdBQUcsQ0FBQyxDQUFDLENBQUMsS0FBSyxHQUFHLENBQUMsR0FBRyxDQUFDLENBQUMsQ0FBQyxFQUFFLENBQUMsR0FBRyxDQUFDLENBQUMsQ0FBQyxHQUFHLENBQUMsQ0FBQyxNQUFNLENBQUMsQ0FBQyxFQUFFLENBQUMsR0FBRyxDQUFDLEVBQUUsRUFBRSxHQUFHLENBQUMsQ0FBQyxVQUFVLENBQUMsQ0FBQyxDQUFDLE1BQU0sQ0FBQyxDQUFDLENBQUMsRUFBRSxFQUFFLEdBQUcsQ0FBQyxDQUFDLFVBQVUsQ0FBQyxDQUFDLENBQUMsTUFBTSxDQUFDLENBQUMsQ0FBQyxDQUFDLEdBQUcsQ0FBQyxDQUFDLE1BQU0sQ0FBQyxDQUFDLENBQUMsQ0FBQyxJQUFJLENBQUMsQ0FBQyxDQUFDLENBQUMsV0FBVyxDQUFDLEdBQUcsQ0FBQyxDQUFDLEdBQUcsQ0FBQyxHQUFHLENBQUMsQ0FBQyxNQUFNLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLEdBQUcsQ0FBQyxFQUFFLENBQUMsQ0FBQyxFQUFFLENBQUMsQ0FBQyxDQUFDLENBQUMsRUFBRSxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLEtBQUssQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLEVBQUUsTUFBTSxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsV0FBVyxDQUFDLEdBQUcsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxRQUFRLENBQUMsQ0FBQyxLQUFLLEdBQUcsQ0FBQyxHQUFHLENBQUMsQ0FBQyxNQUFNLEVBQUUsQ0FBQyxHQUFHLENBQUMsQ0FBQyxNQUFNLENBQUMsQ0FBQyxDQUFDLENBQUMsRUFBRSxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLFFBQVEsQ0FBQyxDQUFDLEdBQUcsQ0FBQyxDQUFDLE1BQU0sQ0FBQyxDQUFDLENBQUMsQ0FBQyxFQUFFLEtBQUssQ0FBQyxDQUFDLENBQUMsSUFBSSxDQUFDLENBQUMsQ0FBQyxDQUFDLEVBQUMsQ0FBQyxLQUFLLENBQUMsQ0FBQyxNQUFNLENBQUMsQ0FBQyxDQUFDLENBQUMsRUFBRSxHQUFHLENBQUMsQ0FBQyxDQUFDLEtBQUssQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsS0FBSyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLEVBQUMsQ0FBQyxLQUFLLEVBQUUsR0FBRyxDQUFDLEVBQUUsQ0FBQyxDQUFDLEdBQUcsQ0FBQyxDQUFDLEVBQUUsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLEVBQUMsQ0FBQyxPQUFPLENBQUMsQ0FBQyxJQUFJLENBQUMsQ0FBQyxDQUFDLE9BQU8sQ0FBQyxVQUFVLENBQUMsSUFBSSxJQUFJLENBQUMsQ0FBQyxDQUFDLENBQUMsRUFBRSxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsU0FBUyxDQUFDLE1BQU0sQ0FBQyxDQUFDLENBQUMsQ0FBQyxFQUFFLENBQUMsQ0FBQyxFQUFFLENBQUMsQ0FBQyxDQUFDLENBQUMsRUFBRSxDQUFDLENBQUMsSUFBSSxDQUFDLENBQUMsQ0FBQyxFQUFFLENBQUMsQ0FBQyxDQUFDLENBQUMsU0FBUyxDQUFDLENBQUMsQ0FBQyxFQUFFLEtBQUssQ0FBQyxHQUFHLENBQUMsR0FBRyxDQUFDLENBQUMsT0FBTyxDQUFDLEdBQUcsRUFBRSxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLEdBQUcsQ0FBQyxDQUFDLE1BQU0sR0FBRyxDQUFDLENBQUMsQ0FBQyxDQUFDLEdBQUcsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLEVBQUUsR0FBRyxDQUFDLENBQUMsVUFBVSxDQUFDLENBQUMsQ0FBQyxFQUFDLENBQUMsT0FBTyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsTUFBTSxDQUFDLENBQUMsQ0FBQyxHQUFHLENBQUMsQ0FBQyxDQUFDLEdBQUcsQ0FBQyxDQUFDLENBQUMsTUFBTSxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsR0FBRyxDQUFDLENBQUMsU0FBUyxDQUFDLFNBQVMsQ0FBQyxDQUFDLENBQUMsR0FBRyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxHQUFHLENBQUMsQ0FBQyxNQUFNLENBQUMsT0FBTSxHQUFHLENBQUMsSUFBSSxDQUFDLENBQUMsRUFBRSxHQUFHLENBQUMsQ0FBQyxVQUFVLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLEVBQUUsR0FBRyxDQUFDLENBQUMsVUFBVSxDQUFDLENBQUMsQ0FBQyxNQUFNLENBQUMsQ0FBQyxDQUFDLENBQUMsT0FBTyxDQUFDLEdBQUcsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxFQUFFLE1BQU0sRUFBRSxDQUFDLEdBQUcsQ0FBQyxDQUFDLEdBQUcsQ0FBQyxDQUFDLENBQUMsQ0FBQyxNQUFNLENBQUMsQ0FBQyxFQUFFLENBQUMsR0FBRyxDQUFDLEVBQUUsR0FBRyxDQUFDLENBQUMsQ0FBQyxDQUFDLEdBQUcsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsVUFBVSxDQUFDLFNBQVMsQ0FBQyxDQUFDLENBQUMsT0FBTyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLE1BQU0sQ0FBQyxDQUFDLEVBQUUsRUFBRSxHQUFHLENBQUMsQ0FBQyxVQUFVLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxJQUFJLENBQUMsVUFBVSxDQUFDLEdBQUcsQ0FBQyxHQUFHLFNBQVMsQ0FBQyxNQUFNLENBQUMsT0FBTSxHQUFHLENBQUMsSUFBSSxJQUFJLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxTQUFTLENBQUMsTUFBTSxDQUFDLEVBQUUsQ0FBQyxDQUFDLENBQUMsSUFBSSxDQUFDLENBQUMsU0FBUyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsTUFBTSxDQUFDLENBQUMsR0FBRyxLQUFLLENBQUMsR0FBRyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLEVBQUUsR0FBRyxDQUFDLENBQUMsRUFBQyxDQUFDLE9BQU8sS0FBSyxDQUFDLEdBQUcsQ0FBQyxDQUFDLEdBQUcsQ0FBQyxDQUFDLENBQUMsU0FBUyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsUUFBUSxDQUFDLFNBQVMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLEdBQUcsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLEdBQUcsQ0FBQyxDQUFDLE9BQU0sRUFBRSxDQUFDLEdBQUcsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLE9BQU8sQ0FBQyxDQUFDLENBQUMsS0FBSyxDQUFDLENBQUMsQ0FBQyxDQUFDLE9BQU8sQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLE9BQU0sRUFBRSxDQUFDLElBQUksSUFBSSxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsTUFBTSxFQUFFLEVBQUUsR0FBRyxDQUFDLENBQUMsVUFBVSxDQUFDLENBQUMsQ0FBQyxDQUFDLEVBQUUsQ0FBQyxDQUFDLENBQUMsSUFBSSxJQUFJLENBQUMsQ0FBQyxDQUFDLENBQUMsTUFBTSxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxNQUFNLEVBQUUsRUFBRSxHQUFHLENBQUMsQ0FBQyxVQUFVLENBQUMsQ0FBQyxDQUFDLENBQUMsRUFBRSxDQUFDLENBQUMsQ0FBQyxJQUFJLElBQUksQ0FBQyxDQUFDLENBQUMsQ0FBQyxNQUFNLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxFQUFFLENBQUMsQ0FBQyxFQUFFLENBQUMsQ0FBQyxDQUFDLEdBQUcsQ0FBQyxHQUFHLENBQUMsQ0FBQyxDQUFDLEdBQUcsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLEdBQUcsRUFBRSxHQUFHLENBQUMsQ0FBQyxVQUFVLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLE9BQU8sQ0FBQyxDQUFDLEtBQUssQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLEdBQUcsQ0FBQyxHQUFHLENBQUMsQ0FBQyxPQUFPLENBQUMsQ0FBQyxLQUFLLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLEtBQUssQ0FBQyxDQUFDLENBQUMsR0FBRyxFQUFFLEdBQUcsQ0FBQyxDQUFDLFVBQVUsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLEdBQUcsQ0FBQyxHQUFHLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLEtBQUssQ0FBQyxJQUFJLENBQUMsQ0FBQyxDQUFDLENBQUMsVUFBVSxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxHQUFHLENBQUMsR0FBRyxDQUFDLENBQUMsVUFBVSxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxNQUFNLEVBQUUsR0FBRyxDQUFDLEdBQUcsQ0FBQyxDQUFDLENBQUMsRUFBQyxDQUFDLElBQUksQ0FBQyxDQUFDLEVBQUUsQ0FBQyxJQUFJLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLEVBQUUsQ0FBQyxDQUFDLEVBQUUsQ0FBQyxDQUFDLENBQUMsR0FBRyxDQUFDLEVBQUUsRUFBRSxHQUFHLENBQUMsQ0FBQyxVQUFVLENBQUMsQ0FBQyxDQUFDLEdBQUcsQ0FBQyxHQUFHLENBQUMsQ0FBQyxNQUFNLENBQUMsQ0FBQyxFQUFFLElBQUksQ0FBQyxDQUFDLEVBQUUsS0FBSyxDQUFDLENBQUMsT0FBTyxDQUFDLENBQUMsTUFBTSxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLEtBQUssQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLEVBQUUsQ0FBQyxFQUFFLENBQUMsQ0FBQyxFQUFFLEdBQUcsQ0FBQyxDQUFDLFVBQVUsQ0FBQyxDQUFDLENBQUMsRUFBRSxFQUFFLENBQUMsQ0FBQyxDQUFDLENBQUMsS0FBSyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxTQUFTLENBQUMsU0FBUyxDQUFDLENBQUMsQ0FBQyxPQUFPLENBQUMsQ0FBQyxDQUFDLE9BQU8sQ0FBQyxTQUFTLENBQUMsQ0FBQyxDQUFDLEdBQUcsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsR0FBRyxDQUFDLENBQUMsTUFBTSxDQUFDLE9BQU0sR0FBRyxDQUFDLElBQUksSUFBSSxDQUFDLENBQUMsQ0FBQyxDQUFDLFVBQVUsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsRUFBRSxHQUFHLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsTUFBTSxDQUFDLENBQUMsQ0FBQyxDQUFDLEVBQUUsQ0FBQyxDQUFDLEVBQUUsQ0FBQyxDQUFDLEdBQUcsRUFBRSxJQUFJLENBQUMsQ0FBQyxDQUFDLENBQUMsVUFBVSxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxHQUFHLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxLQUFLLENBQUMsQ0FBQyxLQUFLLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxPQUFNLENBQUMsQ0FBQyxHQUFHLENBQUMsQ0FBQyxDQUFDLENBQUMsR0FBRyxDQUFDLEdBQUcsQ0FBQyxDQUFDLEVBQUUsQ0FBQyxHQUFHLENBQUMsQ0FBQyxJQUFJLENBQUMsQ0FBQyxDQUFDLEtBQUssQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxRQUFRLENBQUMsU0FBUyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsR0FBRyxLQUFLLENBQUMsR0FBRyxDQUFDLEVBQUUsUUFBUSxFQUFFLE9BQU8sQ0FBQyxDQUFDLE1BQU0sSUFBSSxTQUFTLENBQUMsaUNBQWlDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsSUFBSSxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLEdBQUcsS0FBSyxDQUFDLEdBQUcsQ0FBQyxFQUFFLENBQUMsQ0FBQyxNQUFNLENBQUMsQ0FBQyxFQUFFLENBQUMsQ0FBQyxNQUFNLEVBQUUsQ0FBQyxDQUFDLE1BQU0sQ0FBQyxDQUFDLEdBQUcsQ0FBQyxDQUFDLE1BQU0sR0FBRyxDQUFDLENBQUMsTUFBTSxFQUFFLENBQUMsR0FBRyxDQUFDLENBQUMsT0FBTSxFQUFFLENBQUMsSUFBSSxDQUFDLENBQUMsQ0FBQyxDQUFDLE1BQU0sQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLElBQUksQ0FBQyxDQUFDLENBQUMsQ0FBQyxNQUFNLENBQUMsQ0FBQyxDQUFDLENBQUMsRUFBRSxDQUFDLENBQUMsRUFBRSxDQUFDLENBQUMsQ0FBQyxJQUFJLENBQUMsQ0FBQyxDQUFDLENBQUMsVUFBVSxDQUFDLENBQUMsQ0FBQyxDQUFDLEdBQUcsRUFBRSxHQUFHLENBQUMsQ0FBQyxDQUFDLEdBQUcsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxLQUFLLENBQUMsQ0FBQyxLQUFJLENBQUMsQ0FBQyxHQUFHLENBQUMsR0FBRyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLEVBQUUsQ0FBQyxHQUFHLENBQUMsR0FBRyxDQUFDLENBQUMsVUFBVSxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxFQUFFLEVBQUUsQ0FBQyxHQUFHLENBQUMsQ0FBQyxDQUFDLENBQUMsRUFBRSxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxFQUFDLENBQUMsT0FBTyxDQUFDLEdBQUcsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLEdBQUcsQ0FBQyxHQUFHLENBQUMsQ0FBQyxDQUFDLENBQUMsTUFBTSxDQUFDLENBQUMsQ0FBQyxDQUFDLEtBQUssQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsSUFBSSxDQUFDLENBQUMsQ0FBQyxDQUFDLE1BQU0sQ0FBQyxDQUFDLENBQUMsQ0FBQyxFQUFFLENBQUMsQ0FBQyxFQUFFLENBQUMsQ0FBQyxHQUFHLEVBQUUsR0FBRyxDQUFDLENBQUMsVUFBVSxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsR0FBRyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLEtBQUssQ0FBQyxDQUFDLEtBQUksQ0FBQyxDQUFDLEdBQUcsQ0FBQyxHQUFHLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLE9BQU0sQ0FBQyxDQUFDLEdBQUcsQ0FBQyxDQUFDLEVBQUUsQ0FBQyxDQUFDLENBQUMsS0FBSyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLE9BQU8sQ0FBQyxTQUFTLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxJQUFJLElBQUksQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsTUFBTSxDQUFDLENBQUMsQ0FBQyxDQUFDLEVBQUUsQ0FBQyxDQUFDLEVBQUUsQ0FBQyxDQUFDLENBQUMsSUFBSSxDQUFDLENBQUMsQ0FBQyxDQUFDLFVBQVUsQ0FBQyxDQUFDLENBQUMsQ0FBQyxHQUFHLEVBQUUsR0FBRyxDQUFDLENBQUMsQ0FBQyxDQUFDLEdBQUcsQ0FBQyxHQUFHLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLEVBQUUsR0FBRyxDQUFDLENBQUMsQ0FBQyxDQUFDLEdBQUcsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxHQUFHLENBQUMsR0FBRyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLEdBQUcsQ0FBQyxHQUFHLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLEtBQUssR0FBRyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLEtBQUssQ0FBQyxDQUFDLE9BQU0sQ0FBQyxDQUFDLEdBQUcsQ0FBQyxFQUFFLENBQUMsQ0FBQyxHQUFHLENBQUMsRUFBRSxDQUFDLEdBQUcsQ0FBQyxFQUFFLENBQUMsR0FBRyxDQUFDLEVBQUUsQ0FBQyxHQUFHLENBQUMsQ0FBQyxDQUFDLEVBQUUsQ0FBQyxHQUFHLENBQUMsQ0FBQyxDQUFDLENBQUMsRUFBRSxDQUFDLENBQUMsQ0FBQyxLQUFLLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsTUFBTSxDQUFDLFNBQVMsQ0FBQyxDQUFDLENBQUMsR0FBRyxJQUFJLEdBQUcsQ0FBQyxFQUFFLFFBQVEsRUFBRSxPQUFPLENBQUMsQ0FBQyxNQUFNLElBQUksU0FBUyxDQUFDLGtFQUFrRSxDQUFDLE9BQU8sQ0FBQyxDQUFDLENBQUMsT0FBTyxTQUFTLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxJQUFJLENBQUMsQ0FBQyxDQUFDLENBQUMsR0FBRyxFQUFFLENBQUMsQ0FBQyxJQUFJLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxJQUFJLEVBQUUsQ0FBQyxDQUFDLENBQUMsSUFBSSxFQUFFLEVBQUUsR0FBRyxDQUFDLENBQUMsR0FBRyxFQUFFLEVBQUUsQ0FBQyxDQUFDLE9BQU8sQ0FBQyxDQUFDLENBQUMsR0FBRyxDQUFDLENBQUMsSUFBSSxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLEdBQUcsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsS0FBSyxDQUFDLFNBQVMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLElBQUksQ0FBQyxDQUFDLENBQUMsSUFBSSxDQUFDLEVBQUUsQ0FBQyxHQUFHLENBQUMsRUFBRSxDQUFDLElBQUksQ0FBQyxFQUFFLENBQUMsR0FBRyxDQUFDLEVBQUUsQ0FBQyxJQUFJLENBQUMsRUFBRSxDQUFDLENBQUMsR0FBRyxDQUFDLEdBQUcsQ0FBQyxDQUFDLE1BQU0sQ0FBQyxPQUFPLENBQUMsQ0FBQyxJQUFJLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLFVBQVUsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsRUFBRSxHQUFHLENBQUMsQ0FBQyxDQUFDLEVBQUUsQ0FBQyxDQUFDLElBQUksQ0FBQyxHQUFHLENBQUMsQ0FBQyxDQUFDLENBQUMsRUFBRSxDQUFDLENBQUMsQ0FBQyxDQUFDLElBQUksSUFBSSxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsTUFBTSxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsRUFBRSxDQUFDLENBQUMsRUFBRSxDQUFDLENBQUMsR0FBRyxFQUFFLElBQUksQ0FBQyxDQUFDLENBQUMsQ0FBQyxVQUFVLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsR0FBRyxDQUFDLEdBQUcsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsRUFBRSxHQUFHLENBQUMsQ0FBQyxDQUFDLENBQUMsR0FBRyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLEdBQUcsQ0FBQyxHQUFHLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsR0FBRyxDQUFDLEdBQUcsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsS0FBSyxHQUFHLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsS0FBSyxDQUFDLE9BQU0sQ0FBQyxDQUFDLEdBQUcsQ0FBQyxFQUFFLENBQUMsQ0FBQyxHQUFHLENBQUMsRUFBRSxDQUFDLEdBQUcsQ0FBQyxFQUFFLENBQUMsR0FBRyxDQUFDLEVBQUUsQ0FBQyxHQUFHLENBQUMsQ0FBQyxDQUFDLEVBQUUsQ0FBQyxHQUFHLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLEdBQUcsQ0FBQyxHQUFHLENBQUMsQ0FBQyxJQUFJLENBQUMsQ0FBQyxDQUFDLElBQUksQ0FBQyxDQUFDLEdBQUcsQ0FBQyxFQUFFLENBQUMsQ0FBQyxDQUFDLENBQUMsS0FBSyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsS0FBSyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxFQUFFLENBQUMsR0FBRyxDQUFDLEVBQUUsQ0FBQyxFQUFFLENBQUMsQ0FBQyxJQUFJLENBQUMsQ0FBQyxDQUFDLEtBQUssQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLElBQUksQ0FBQyxDQUFDLENBQUMsS0FBSyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsR0FBRyxDQUFDLENBQUMsSUFBSSxDQUFDLENBQUMsQ0FBQyxLQUFLLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxJQUFJLENBQUMsQ0FBQyxDQUFDLEtBQUssQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsR0FBRyxDQUFDLENBQUMsQ0FBQyxLQUFLLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsR0FBRyxDQUFDLENBQUMsQ0FBQyxLQUFLLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLEdBQUcsQ0FBQyxDQUFDLEdBQUcsQ0FBQyxHQUFHLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxHQUFHLENBQUMsR0FBRyxDQUFDLFNBQVMsQ0FBQyxHQUFHLENBQUMsS0FBSyxDQUFDLElBQUksQ0FBQyxLQUFLLENBQUMsSUFBSSxDQUFDLENBQUMsQ0FBQyxDQUFDLEtBQUssQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLE9BQU8sQ0FBQyxFQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxFQUFFLENBQUMsU0FBUyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsSUFBSSxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLEdBQUcsS0FBSyxDQUFDLEdBQUcsQ0FBQyxDQUFDLE9BQU8sQ0FBQyxDQUFDLE9BQU8sQ0FBQyxJQUFJLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxPQUFPLENBQUMsRUFBRSxDQUFDLENBQUMsT0FBTyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxPQUFPLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLE9BQU8sQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsR0FBRyxDQUFDLElBQUksSUFBSSxDQUFDLElBQUksQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxFQUFFLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLEVBQUUsTUFBTSxDQUFDLGNBQWMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsVUFBVSxDQUFDLENBQUMsQ0FBQyxDQUFDLEdBQUcsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsRUFBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLEdBQUcsTUFBTSxDQUFDLFNBQVMsQ0FBQyxjQUFjLENBQUMsSUFBSSxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsRUFBRSxDQUFDLFdBQVcsRUFBRSxPQUFPLE1BQU0sRUFBRSxNQUFNLENBQUMsV0FBVyxFQUFFLE1BQU0sQ0FBQyxjQUFjLENBQUMsQ0FBQyxDQUFDLE1BQU0sQ0FBQyxXQUFXLENBQUMsQ0FBQyxLQUFLLENBQUMsUUFBUSxDQUFDLENBQUMsQ0FBQyxNQUFNLENBQUMsY0FBYyxDQUFDLENBQUMsQ0FBQyxZQUFZLENBQUMsQ0FBQyxLQUFLLENBQUMsQ0FBQyxDQUFDLENBQUMsRUFBQyxDQUFDLENBQUMsSUFBSSxDQUFDLENBQUMsRUFBRSxDQUFDLENBQUMsSUFBSSxDQUFDLElBQUksQ0FBQyxDQUFDLEdBQUcsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLEdBQUcsQ0FBQyxJQUFJLENBQUMsQ0FBQyxLQUFLLENBQUMsSUFBSSxDQUFDLENBQUMsQ0FBQyxDQUFDLFFBQVEsRUFBRSxPQUFPLE9BQU8sQ0FBQyxDQUFDLENBQUMsT0FBTyxHQUFHLE9BQU8sQ0FBQyxRQUFRLENBQUMsS0FBSyxHQUFHLFFBQVEsRUFBRSxPQUFPLFNBQVMsQ0FBQyxDQUFDLElBQUksQ0FBQyxDQUFDLFNBQVMsQ0FBQyxTQUFTLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxPQUFPLENBQUMsU0FBUyxDQUFDLEVBQUUsRUFBQyxDQUFDLElBQUksQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLEVBQUUsQ0FBQyxDQUFDLFNBQVMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLE9BQU8sQ0FBQyxDQUFDLE1BQU0sQ0FBQyxjQUFjLEVBQUUsQ0FBQyxTQUFTLENBQUMsRUFBRSxDQUFDLFdBQVcsS0FBSyxFQUFFLFNBQVMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxTQUFTLENBQUMsRUFBQyxDQUFDLEVBQUUsU0FBUyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsSUFBSSxJQUFJLENBQUMsSUFBSSxDQUFDLENBQUMsTUFBTSxDQUFDLFNBQVMsQ0FBQyxjQUFjLENBQUMsSUFBSSxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsR0FBRyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxFQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsU0FBUyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsR0FBRyxVQUFVLEVBQUUsT0FBTyxDQUFDLEVBQUUsSUFBSSxHQUFHLENBQUMsQ0FBQyxNQUFNLElBQUksU0FBUyxDQUFDLHNCQUFzQixDQUFDLE1BQU0sQ0FBQyxDQUFDLENBQUMsQ0FBQywrQkFBK0IsQ0FBQyxDQUFDLFNBQVMsQ0FBQyxFQUFFLENBQUMsSUFBSSxDQUFDLFdBQVcsQ0FBQyxFQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsU0FBUyxDQUFDLElBQUksR0FBRyxDQUFDLENBQUMsTUFBTSxDQUFDLE1BQU0sQ0FBQyxDQUFDLENBQUMsRUFBRSxDQUFDLENBQUMsU0FBUyxDQUFDLENBQUMsQ0FBQyxTQUFTLENBQUMsSUFBSSxDQUFDLEVBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLGdCQUFnQixDQUFDLENBQUMsQ0FBQyxLQUFLLENBQUMsQ0FBQyxDQUFDLE9BQU8sQ0FBQyxDQUFDLENBQUMsRUFBRSxDQUFDLENBQUMsQ0FBQyxHQUFHLENBQUMsQ0FBQyxDQUFDLDhEQUE4RCxDQUFDLENBQUMsQ0FBQyxVQUFVLENBQUMsU0FBUyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxLQUFLLENBQUMsR0FBRyxDQUFDLEdBQUcsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsUUFBUSxFQUFFLE9BQU8sQ0FBQyxFQUFFLElBQUksQ0FBQyxNQUFNLENBQUMsQ0FBQyxDQUFDLE1BQU0sRUFBRSxDQUFDLENBQUMsSUFBSSxDQUFDLFNBQVMsQ0FBQyxDQUFDLENBQUMsU0FBUyxFQUFFLENBQUMsQ0FBQyxJQUFJLENBQUMsSUFBSSxDQUFDLENBQUMsQ0FBQyxJQUFJLEVBQUUsQ0FBQyxDQUFDLElBQUksQ0FBQyxLQUFLLENBQUMsQ0FBQyxDQUFDLEtBQUssRUFBRSxDQUFDLENBQUMsSUFBSSxDQUFDLFFBQVEsQ0FBQyxDQUFDLENBQUMsUUFBUSxFQUFFLENBQUMsR0FBRyxJQUFJLENBQUMsTUFBTSxDQUFDLFNBQVMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLE9BQU8sQ0FBQyxFQUFFLENBQUMsQ0FBQyxDQUFDLENBQUMsTUFBTSxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLElBQUksQ0FBQyxTQUFTLENBQUMsQ0FBQyxFQUFFLENBQUMsQ0FBQyxJQUFJLENBQUMsSUFBSSxDQUFDLFNBQVMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLE9BQU8sQ0FBQyxFQUFFLElBQUksT0FBTyxDQUFDLElBQUksTUFBTSxDQUFDLElBQUksTUFBTSxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLEdBQUcsQ0FBQyxHQUFHLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLEVBQUMsQ0FBQyxPQUFPLENBQUMsQ0FBQyxDQUFDLElBQUksQ0FBQyxNQUFNLENBQUMsQ0FBQyxFQUFFLENBQUMsQ0FBQyxDQUFDLElBQUksQ0FBQyxLQUFLLENBQUMsQ0FBQyxFQUFFLENBQUMsQ0FBQyxJQUFJLENBQUMsUUFBUSxDQUFDLENBQUMsRUFBRSxDQUFDLENBQUMsU0FBUyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsR0FBRyxDQUFDLENBQUMsQ0FBQyxNQUFNLEVBQUUsQ0FBQyxDQUFDLE1BQU0sSUFBSSxLQUFLLENBQUMsMERBQTBELENBQUMsTUFBTSxDQUFDLENBQUMsQ0FBQyxTQUFTLENBQUMsWUFBWSxDQUFDLENBQUMsTUFBTSxDQUFDLENBQUMsQ0FBQyxJQUFJLENBQUMsYUFBYSxDQUFDLENBQUMsTUFBTSxDQUFDLENBQUMsQ0FBQyxLQUFLLENBQUMsZ0JBQWdCLENBQUMsQ0FBQyxNQUFNLENBQUMsQ0FBQyxDQUFDLFFBQVEsQ0FBQyxJQUFJLENBQUMsQ0FBQyxDQUFDLEdBQUcsQ0FBQyxDQUFDLE1BQU0sRUFBRSxDQUFDLENBQUMsQ0FBQyxJQUFJLENBQUMsQ0FBQyxDQUFDLE1BQU0sQ0FBQyxDQUFDLE1BQU0sSUFBSSxLQUFLLENBQUMsaURBQWlELENBQUMsQ0FBQyxHQUFHLENBQUMsQ0FBQyxJQUFJLENBQUMsR0FBRyxDQUFDLENBQUMsU0FBUyxDQUFDLENBQUMsR0FBRyxDQUFDLENBQUMsQ0FBQyxJQUFJLENBQUMsQ0FBQyxDQUFDLElBQUksQ0FBQyxDQUFDLE1BQU0sSUFBSSxLQUFLLENBQUMsMElBQTBJLENBQUMsQ0FBQyxLQUFLLEdBQUcsQ0FBQyxDQUFDLElBQUksQ0FBQyxDQUFDLENBQUMsSUFBSSxDQUFDLENBQUMsTUFBTSxJQUFJLEtBQUssQ0FBQywySEFBMkgsQ0FBQyxDQUFDLENBQUMsSUFBSSxDQUFDLENBQUMsQ0FBQyxFQUFDLENBQUMsT0FBTyxDQUFDLENBQUMsS0FBSyxDQUFDLFNBQVMsQ0FBQyxDQUFDLENBQUMsT0FBTyxDQUFDLFlBQVksQ0FBQyxFQUFFLENBQUMsQ0FBQyxDQUFDLEVBQUUsUUFBUSxFQUFFLE9BQU8sQ0FBQyxDQUFDLFNBQVMsRUFBRSxRQUFRLEVBQUUsT0FBTyxDQUFDLENBQUMsUUFBUSxFQUFFLFFBQVEsRUFBRSxPQUFPLENBQUMsQ0FBQyxJQUFJLEVBQUUsUUFBUSxFQUFFLE9BQU8sQ0FBQyxDQUFDLEtBQUssRUFBRSxRQUFRLEVBQUUsT0FBTyxDQUFDLENBQUMsTUFBTSxFQUFFLFFBQVEsRUFBRSxPQUFPLENBQUMsQ0FBQyxNQUFNLEVBQUUsVUFBVSxFQUFFLE9BQU8sQ0FBQyxDQUFDLElBQUksRUFBRSxVQUFVLEVBQUUsT0FBTyxDQUFDLENBQUMsUUFBUSxDQUFDLENBQUMsTUFBTSxDQUFDLGNBQWMsQ0FBQyxDQUFDLENBQUMsU0FBUyxDQUFDLFFBQVEsQ0FBQyxDQUFDLEdBQUcsQ0FBQyxVQUFVLENBQUMsT0FBTyxDQUFDLENBQUMsSUFBSSxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxVQUFVLENBQUMsQ0FBQyxDQUFDLENBQUMsWUFBWSxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsU0FBUyxDQUFDLElBQUksQ0FBQyxTQUFTLENBQUMsQ0FBQyxDQUFDLEdBQUcsQ0FBQyxDQUFDLENBQUMsT0FBTyxJQUFJLENBQUMsSUFBSSxDQUFDLENBQUMsQ0FBQyxDQUFDLE1BQU0sQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLFNBQVMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLElBQUksQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLEtBQUssQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLFFBQVEsQ0FBQyxPQUFPLEtBQUssQ0FBQyxHQUFHLENBQUMsQ0FBQyxDQUFDLENBQUMsSUFBSSxDQUFDLE1BQU0sQ0FBQyxJQUFJLEdBQUcsQ0FBQyxHQUFHLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxLQUFLLENBQUMsR0FBRyxDQUFDLENBQUMsQ0FBQyxDQUFDLElBQUksQ0FBQyxTQUFTLENBQUMsSUFBSSxHQUFHLENBQUMsR0FBRyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsS0FBSyxDQUFDLEdBQUcsQ0FBQyxDQUFDLENBQUMsQ0FBQyxJQUFJLENBQUMsSUFBSSxDQUFDLElBQUksR0FBRyxDQUFDLEdBQUcsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLEtBQUssQ0FBQyxHQUFHLENBQUMsQ0FBQyxDQUFDLENBQUMsSUFBSSxDQUFDLEtBQUssQ0FBQyxJQUFJLEdBQUcsQ0FBQyxHQUFHLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxLQUFLLENBQUMsR0FBRyxDQUFDLENBQUMsQ0FBQyxDQUFDLElBQUksQ0FBQyxRQUFRLENBQUMsSUFBSSxHQUFHLENBQUMsR0FBRyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxHQUFHLElBQUksQ0FBQyxNQUFNLEVBQUUsQ0FBQyxHQUFHLElBQUksQ0FBQyxTQUFTLEVBQUUsQ0FBQyxHQUFHLElBQUksQ0FBQyxJQUFJLEVBQUUsQ0FBQyxHQUFHLElBQUksQ0FBQyxLQUFLLEVBQUUsQ0FBQyxHQUFHLElBQUksQ0FBQyxRQUFRLENBQUMsSUFBSSxDQUFDLElBQUksQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxLQUFLLENBQUMsU0FBUyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsS0FBSyxDQUFDLEdBQUcsQ0FBQyxHQUFHLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLElBQUksQ0FBQyxDQUFDLENBQUMsQ0FBQyxJQUFJLENBQUMsQ0FBQyxDQUFDLENBQUMsT0FBTyxDQUFDLENBQUMsSUFBSSxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxFQUFFLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxFQUFFLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLEVBQUUsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsRUFBRSxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxFQUFFLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLElBQUksQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxJQUFJLENBQUMsU0FBUyxDQUFDLENBQUMsQ0FBQyxJQUFJLENBQUMsQ0FBQyxDQUFDLENBQUMsR0FBRyxDQUFDLEdBQUcsQ0FBQyxDQUFDLENBQUMsQ0FBQyxPQUFPLENBQUMsS0FBSyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxHQUFHLENBQUMsRUFBRSxDQUFDLENBQUMsQ0FBQyxDQUFDLEdBQUcsQ0FBQyxDQUFDLENBQUMsSUFBSSxDQUFDLENBQUMsQ0FBQyxDQUFDLE9BQU8sQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLEdBQUcsQ0FBQyxFQUFFLENBQUMsQ0FBQyxDQUFDLENBQUMsU0FBUyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLEdBQUcsQ0FBQyxDQUFDLENBQUMsQ0FBQyxTQUFTLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsU0FBUyxDQUFDLENBQUMsQ0FBQyxFQUFFLENBQUMsRUFBQyxDQUFDLE9BQU8sSUFBSSxDQUFDLENBQUMsTUFBTSxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLElBQUksQ0FBQyxTQUFTLENBQUMsQ0FBQyxDQUFDLE9BQU8sSUFBSSxDQUFDLENBQUMsQ0FBQyxDQUFDLE1BQU0sQ0FBQyxDQUFDLENBQUMsU0FBUyxDQUFDLENBQUMsQ0FBQyxJQUFJLENBQUMsQ0FBQyxDQUFDLEtBQUssQ0FBQyxDQUFDLENBQUMsUUFBUSxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsU0FBUyxDQUFDLFFBQVEsQ0FBQyxTQUFTLENBQUMsQ0FBQyxDQUFDLE9BQU8sS0FBSyxDQUFDLEdBQUcsQ0FBQyxHQUFHLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxJQUFJLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsU0FBUyxDQUFDLE1BQU0sQ0FBQyxVQUFVLENBQUMsT0FBTyxJQUFJLENBQUMsQ0FBQyxDQUFDLENBQUMsTUFBTSxDQUFDLFNBQVMsQ0FBQyxDQUFDLENBQUMsR0FBRyxDQUFDLENBQUMsQ0FBQyxHQUFHLENBQUMsWUFBWSxDQUFDLENBQUMsT0FBTyxDQUFDLENBQUMsSUFBSSxDQUFDLENBQUMsSUFBSSxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsT0FBTyxDQUFDLENBQUMsVUFBVSxDQUFDLENBQUMsQ0FBQyxRQUFRLENBQUMsQ0FBQyxDQUFDLE9BQU8sQ0FBQyxDQUFDLENBQUMsSUFBSSxHQUFHLENBQUMsQ0FBQyxDQUFDLENBQUMsTUFBTSxDQUFDLElBQUksQ0FBQyxDQUFDLENBQUMsT0FBTyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsRUFBRSxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLEtBQUssQ0FBQyxDQUFDLENBQUMsQ0FBQyxTQUFTLENBQUMsQ0FBQyxDQUFDLFNBQVMsQ0FBQyxFQUFFLENBQUMsSUFBSSxDQUFDLENBQUMsSUFBSSxHQUFHLENBQUMsRUFBRSxDQUFDLENBQUMsS0FBSyxDQUFDLElBQUksQ0FBQyxTQUFTLENBQUMsRUFBRSxJQUFJLENBQUMsT0FBTyxDQUFDLENBQUMsVUFBVSxDQUFDLElBQUksQ0FBQyxDQUFDLENBQUMsT0FBTyxDQUFDLElBQUksQ0FBQyxDQUFDLENBQUMsT0FBTyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLE1BQU0sQ0FBQyxjQUFjLENBQUMsQ0FBQyxDQUFDLFNBQVMsQ0FBQyxRQUFRLENBQUMsQ0FBQyxHQUFHLENBQUMsVUFBVSxDQUFDLE9BQU8sSUFBSSxDQUFDLE9BQU8sR0FBRyxJQUFJLENBQUMsT0FBTyxDQUFDLENBQUMsQ0FBQyxJQUFJLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLElBQUksQ0FBQyxPQUFPLENBQUMsQ0FBQyxVQUFVLENBQUMsQ0FBQyxDQUFDLENBQUMsWUFBWSxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsU0FBUyxDQUFDLFFBQVEsQ0FBQyxTQUFTLENBQUMsQ0FBQyxDQUFDLE9BQU8sS0FBSyxDQUFDLEdBQUcsQ0FBQyxHQUFHLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsSUFBSSxDQUFDLENBQUMsQ0FBQyxDQUFDLEVBQUUsSUFBSSxDQUFDLFVBQVUsR0FBRyxJQUFJLENBQUMsVUFBVSxDQUFDLENBQUMsQ0FBQyxJQUFJLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLElBQUksQ0FBQyxVQUFVLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxTQUFTLENBQUMsTUFBTSxDQUFDLFVBQVUsQ0FBQyxJQUFJLENBQUMsQ0FBQyxDQUFDLElBQUksQ0FBQyxDQUFDLENBQUMsQ0FBQyxPQUFPLElBQUksQ0FBQyxPQUFPLEdBQUcsQ0FBQyxDQUFDLE1BQU0sQ0FBQyxJQUFJLENBQUMsT0FBTyxDQUFDLENBQUMsQ0FBQyxJQUFJLENBQUMsQ0FBQyxDQUFDLENBQUMsSUFBSSxDQUFDLFVBQVUsR0FBRyxDQUFDLENBQUMsUUFBUSxDQUFDLElBQUksQ0FBQyxVQUFVLENBQUMsQ0FBQyxJQUFJLENBQUMsSUFBSSxHQUFHLENBQUMsQ0FBQyxJQUFJLENBQUMsSUFBSSxDQUFDLElBQUksQ0FBQyxDQUFDLElBQUksQ0FBQyxNQUFNLEdBQUcsQ0FBQyxDQUFDLE1BQU0sQ0FBQyxJQUFJLENBQUMsTUFBTSxDQUFDLENBQUMsSUFBSSxDQUFDLFNBQVMsR0FBRyxDQUFDLENBQUMsU0FBUyxDQUFDLElBQUksQ0FBQyxTQUFTLENBQUMsQ0FBQyxJQUFJLENBQUMsS0FBSyxHQUFHLENBQUMsQ0FBQyxLQUFLLENBQUMsSUFBSSxDQUFDLEtBQUssQ0FBQyxDQUFDLElBQUksQ0FBQyxRQUFRLEdBQUcsQ0FBQyxDQUFDLFFBQVEsQ0FBQyxJQUFJLENBQUMsUUFBUSxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxFQUFFLENBQUMsQ0FBQyxDQUFDLEVBQUUsRUFBRSxFQUFFLENBQUMsQ0FBQyxLQUFLLENBQUMsQ0FBQyxDQUFDLEVBQUUsQ0FBQyxDQUFDLEtBQUssQ0FBQyxDQUFDLENBQUMsRUFBRSxDQUFDLENBQUMsS0FBSyxDQUFDLENBQUMsQ0FBQyxFQUFFLENBQUMsQ0FBQyxLQUFLLENBQUMsQ0FBQyxDQUFDLEVBQUUsQ0FBQyxDQUFDLEtBQUssQ0FBQyxDQUFDLENBQUMsRUFBRSxDQUFDLENBQUMsS0FBSyxDQUFDLENBQUMsQ0FBQyxFQUFFLENBQUMsQ0FBQyxLQUFLLENBQUMsQ0FBQyxDQUFDLEVBQUUsQ0FBQyxDQUFDLEtBQUssQ0FBQyxDQUFDLENBQUMsRUFBRSxDQUFDLENBQUMsS0FBSyxDQUFDLENBQUMsQ0FBQyxFQUFFLENBQUMsQ0FBQyxLQUFLLENBQUMsQ0FBQyxDQUFDLEVBQUUsQ0FBQyxDQUFDLEtBQUssQ0FBQyxDQUFDLENBQUMsRUFBRSxDQUFDLENBQUMsS0FBSyxDQUFDLENBQUMsQ0FBQyxFQUFFLENBQUMsQ0FBQyxLQUFLLENBQUMsQ0FBQyxDQUFDLEVBQUUsQ0FBQyxDQUFDLEtBQUssQ0FBQyxDQUFDLENBQUMsRUFBRSxDQUFDLENBQUMsS0FBSyxDQUFDLENBQUMsQ0FBQyxFQUFFLENBQUMsQ0FBQyxLQUFLLENBQUMsQ0FBQyxDQUFDLEVBQUUsQ0FBQyxDQUFDLEtBQUssQ0FBQyxDQUFDLENBQUMsRUFBRSxDQUFDLENBQUMsS0FBSyxDQUFDLENBQUMsQ0FBQyxFQUFFLENBQUMsQ0FBQyxLQUFLLENBQUMsQ0FBQyxDQUFDLENBQUMsU0FBUyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLElBQUksSUFBSSxDQUFDLENBQUMsS0FBSyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxNQUFNLENBQUMsQ0FBQyxFQUFFLENBQUMsQ0FBQyxJQUFJLENBQUMsQ0FBQyxDQUFDLENBQUMsVUFBVSxDQUFDLENBQUMsQ0FBQyxDQUFDLEdBQUcsQ0FBQyxFQUFFLEVBQUUsRUFBRSxDQUFDLEVBQUUsR0FBRyxFQUFFLENBQUMsRUFBRSxFQUFFLEVBQUUsQ0FBQyxFQUFFLEVBQUUsRUFBRSxDQUFDLEVBQUUsRUFBRSxFQUFFLENBQUMsRUFBRSxFQUFFLEVBQUUsRUFBRSxHQUFHLENBQUMsRUFBRSxFQUFFLEdBQUcsQ0FBQyxFQUFFLEVBQUUsR0FBRyxDQUFDLEVBQUUsR0FBRyxHQUFHLENBQUMsRUFBRSxDQUFDLEVBQUUsRUFBRSxHQUFHLENBQUMsQ0FBQyxDQUFDLENBQUMsR0FBRyxDQUFDLEdBQUcsQ0FBQyxFQUFFLGtCQUFrQixDQUFDLENBQUMsQ0FBQyxTQUFTLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsS0FBSyxDQUFDLEdBQUcsQ0FBQyxHQUFHLENBQUMsRUFBRSxDQUFDLENBQUMsTUFBTSxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsS0FBSSxDQUFDLEtBQUssQ0FBQyxHQUFHLENBQUMsR0FBRyxDQUFDLENBQUMsQ0FBQyxDQUFDLE1BQU0sQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxJQUFJLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsS0FBSyxDQUFDLEdBQUcsQ0FBQyxFQUFFLENBQUMsQ0FBQyxHQUFHLENBQUMsR0FBRyxDQUFDLEVBQUUsa0JBQWtCLENBQUMsQ0FBQyxDQUFDLFNBQVMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLEVBQUUsQ0FBQyxFQUFFLENBQUMsQ0FBQyxHQUFHLENBQUMsR0FBRyxDQUFDLENBQUMsQ0FBQyxFQUFDLENBQUMsQ0FBQyxPQUFNLENBQUMsQ0FBQyxHQUFHLENBQUMsR0FBRyxDQUFDLEVBQUUsa0JBQWtCLENBQUMsQ0FBQyxDQUFDLFNBQVMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsS0FBSyxDQUFDLEdBQUcsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsU0FBUyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsSUFBSSxJQUFJLENBQUMsQ0FBQyxLQUFLLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsTUFBTSxDQUFDLENBQUMsRUFBRSxDQUFDLENBQUMsSUFBSSxDQUFDLENBQUMsQ0FBQyxDQUFDLFVBQVUsQ0FBQyxDQUFDLENBQUMsQ0FBQyxFQUFFLEdBQUcsQ0FBQyxFQUFFLEVBQUUsR0FBRyxDQUFDLEVBQUUsS0FBSyxDQUFDLEdBQUcsQ0FBQyxHQUFHLENBQUMsQ0FBQyxDQUFDLENBQUMsTUFBTSxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsRUFBRSxDQUFDLENBQUMsQ0FBQyxDQUFDLEVBQUUsS0FBSyxDQUFDLEdBQUcsQ0FBQyxHQUFHLENBQUMsRUFBRSxDQUFDLENBQUMsQ0FBQyxDQUFDLEVBQUMsQ0FBQyxPQUFPLEtBQUssQ0FBQyxHQUFHLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLFNBQVMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxJQUFJLENBQUMsQ0FBQyxPQUFPLENBQUMsQ0FBQyxDQUFDLENBQUMsU0FBUyxFQUFFLENBQUMsQ0FBQyxJQUFJLENBQUMsTUFBTSxDQUFDLENBQUMsRUFBRSxNQUFNLEdBQUcsQ0FBQyxDQUFDLE1BQU0sQ0FBQyxJQUFJLENBQUMsTUFBTSxDQUFDLENBQUMsQ0FBQyxTQUFTLENBQUMsQ0FBQyxNQUFNLENBQUMsQ0FBQyxDQUFDLElBQUksQ0FBQyxDQUFDLEVBQUUsR0FBRyxDQUFDLENBQUMsSUFBSSxDQUFDLFVBQVUsQ0FBQyxDQUFDLENBQUMsR0FBRyxDQUFDLENBQUMsSUFBSSxDQUFDLFVBQVUsQ0FBQyxDQUFDLENBQUMsRUFBRSxFQUFFLEVBQUUsQ0FBQyxDQUFDLElBQUksQ0FBQyxVQUFVLENBQUMsQ0FBQyxDQUFDLEVBQUUsRUFBRSxFQUFFLENBQUMsQ0FBQyxJQUFJLENBQUMsVUFBVSxDQUFDLENBQUMsQ0FBQyxFQUFFLEVBQUUsRUFBRSxDQUFDLENBQUMsSUFBSSxDQUFDLFVBQVUsQ0FBQyxDQUFDLENBQUMsRUFBRSxHQUFHLENBQUMsRUFBRSxFQUFFLEdBQUcsQ0FBQyxDQUFDLElBQUksQ0FBQyxVQUFVLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxJQUFJLENBQUMsTUFBTSxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxJQUFJLENBQUMsQ0FBQyxDQUFDLENBQUMsV0FBVyxFQUFFLENBQUMsQ0FBQyxDQUFDLElBQUksQ0FBQyxNQUFNLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLElBQUksQ0FBQyxDQUFDLEdBQUcsQ0FBQyxDQUFDLENBQUMsQ0FBQyxPQUFPLENBQUMsS0FBSyxDQUFDLElBQUksQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLFNBQVMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxJQUFJLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsRUFBRSxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsTUFBTSxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsU0FBUyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsSUFBSSxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsS0FBSyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsUUFBUSxDQUFDLEdBQUcsQ0FBQyxHQUFHLENBQUMsRUFBRSxDQUFDLENBQUMsQ0FBQyxFQUFFLEdBQUcsQ0FBQyxDQUFDLENBQUMsQ0FBQyxFQUFFLE1BQU0sR0FBRyxDQUFDLElBQUksQ0FBQyxFQUFFLENBQUMsQ0FBQyxDQUFDLEVBQUUsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsSUFBSSxDQUFDLENBQUMsQ0FBQyxDQUFDLE9BQU8sQ0FBQyxHQUFHLENBQUMsQ0FBQyxHQUFHLENBQUMsQ0FBQyxHQUFHLENBQUMsQ0FBQyxDQUFDLElBQUksQ0FBQyxDQUFDLENBQUMsQ0FBQyxNQUFNLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsTUFBTSxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsSUFBSSxDQUFDLENBQUMsQ0FBQyxDQUFDLE9BQU8sQ0FBQyxHQUFHLENBQUMsQ0FBQyxDQUFDLENBQUMsRUFBRSxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLEVBQUUsQ0FBQyxFQUFFLENBQUMsQ0FBQyxDQUFDLENBQUMsTUFBTSxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsRUFBRSxHQUFHLENBQUMsQ0FBQyxFQUFFLENBQUMsQ0FBQyxDQUFDLENBQUMsTUFBTSxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxFQUFFLElBQUcsQ0FBQyxDQUFDLENBQUMsSUFBSSxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLFdBQVcsRUFBRSxFQUFFLE9BQU8sQ0FBQyxHQUFHLENBQUMsQ0FBQyxDQUFDLENBQUMsRUFBRSxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLEVBQUUsQ0FBQyxFQUFFLENBQUMsQ0FBQyxDQUFDLENBQUMsTUFBTSxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsRUFBRSxDQUFDLENBQUMsTUFBTSxDQUFDLENBQUMsQ0FBQyxFQUFDLENBQUMsR0FBRyxDQUFDLENBQUMsQ0FBQyxHQUFHLENBQUMsQ0FBQyxNQUFNLEVBQUUsQ0FBQyxFQUFFLEVBQUUsR0FBRyxDQUFDLENBQUMsVUFBVSxDQUFDLENBQUMsQ0FBQyxFQUFFLEVBQUUsR0FBRyxDQUFDLENBQUMsVUFBVSxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxVQUFVLENBQUMsQ0FBQyxDQUFDLEdBQUcsRUFBRSxFQUFFLENBQUMsRUFBRSxFQUFFLEdBQUcsQ0FBQyxDQUFDLEdBQUcsQ0FBQyxNQUFNLENBQUMsTUFBTSxDQUFDLFlBQVksQ0FBQyxDQUFDLENBQUMsRUFBRSxDQUFDLENBQUMsR0FBRyxDQUFDLENBQUMsTUFBTSxDQUFDLENBQUMsQ0FBQyxNQUFNLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLEtBQUssR0FBRyxDQUFDLENBQUMsTUFBTSxFQUFFLENBQUMsRUFBRSxFQUFFLEdBQUcsQ0FBQyxDQUFDLFVBQVUsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLElBQUksQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxVQUFVLENBQUMsQ0FBQyxDQUFDLEdBQUcsRUFBRSxFQUFFLENBQUMsRUFBRSxFQUFFLEdBQUcsQ0FBQyxDQUFDLEVBQUUsQ0FBQyxNQUFNLENBQUMsTUFBTSxDQUFDLFlBQVksQ0FBQyxDQUFDLENBQUMsRUFBRSxDQUFDLENBQUMsR0FBRyxDQUFDLENBQUMsTUFBTSxDQUFDLENBQUMsQ0FBQyxNQUFNLENBQUMsQ0FBQyxDQUFDLENBQUMsRUFBQyxDQUFDLENBQUMsRUFBRSxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxFQUFDLENBQUMsT0FBTyxDQUFDLEdBQUcsQ0FBQyxFQUFFLEdBQUcsQ0FBQyxDQUFDLEVBQUUsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxHQUFHLENBQUMsRUFBRSxHQUFHLENBQUMsQ0FBQyxFQUFFLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLFNBQVMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLEdBQUcsQ0FBQyxPQUFPLGtCQUFrQixDQUFDLENBQUMsQ0FBQyxDQUFDLE1BQU0sQ0FBQyxDQUFDLENBQUMsT0FBTyxDQUFDLENBQUMsTUFBTSxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsTUFBTSxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLE1BQU0sQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLElBQUksQ0FBQyxDQUFDLDZCQUE2QixDQUFDLFNBQVMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLE9BQU8sQ0FBQyxDQUFDLEtBQUssQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsT0FBTyxDQUFDLENBQUMsRUFBRSxTQUFTLENBQUMsQ0FBQyxDQUFDLE9BQU8sQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLEVBQUUsQ0FBQyxDQUFDLENBQUMsSUFBSSxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxHQUFHLENBQUMsQ0FBQyxDQUFDLENBQUMsU0FBUyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLEdBQUcsQ0FBQyxFQUFFLENBQUMsR0FBRyxTQUFTLENBQUMsTUFBTSxDQUFDLElBQUksSUFBSSxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLE1BQU0sQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsRUFBRSxDQUFDLENBQUMsQ0FBQyxFQUFFLENBQUMsSUFBSSxDQUFDLEdBQUcsQ0FBQyxHQUFHLENBQUMsQ0FBQyxLQUFLLENBQUMsU0FBUyxDQUFDLEtBQUssQ0FBQyxJQUFJLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxPQUFPLENBQUMsQ0FBQyxNQUFNLENBQUMsQ0FBQyxFQUFFLEtBQUssQ0FBQyxTQUFTLENBQUMsS0FBSyxDQUFDLElBQUksQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxLQUFLLEVBQUUsQ0FBQyxDQUFDLENBQUMsQ0FBQyxHQUFHLENBQUMsQ0FBQyxTQUFTLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxRQUFRLENBQUMsU0FBUyxDQUFDLENBQUMsQ0FBQyxJQUFJLElBQUksQ0FBQyxDQUFDLEVBQUUsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxTQUFTLENBQUMsTUFBTSxDQUFDLENBQUMsRUFBRSxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsU0FBUyxDQUFDLENBQUMsQ0FBQyxDQUFDLE9BQU8sQ0FBQyxDQUFDLElBQUksQ0FBQyxDQUFDLElBQUksQ0FBQyxDQUFDLENBQUMsSUFBSSxDQUFDLEtBQUssQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLElBQUksQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLFdBQVcsQ0FBQyxTQUFTLENBQUMsQ0FBQyxDQUFDLElBQUksSUFBSSxDQUFDLENBQUMsRUFBRSxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLFNBQVMsQ0FBQyxNQUFNLENBQUMsQ0FBQyxFQUFFLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxTQUFTLENBQUMsQ0FBQyxDQUFDLENBQUMsSUFBSSxDQUFDLENBQUMsQ0FBQyxDQUFDLElBQUksQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxHQUFHLENBQUMsR0FBRyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxJQUFJLENBQUMsQ0FBQyxDQUFDLENBQUMsT0FBTyxDQUFDLEtBQUssQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxPQUFPLENBQUMsRUFBRSxDQUFDLENBQUMsQ0FBQyxDQUFDLEdBQUcsQ0FBQyxFQUFFLENBQUMsQ0FBQyxDQUFDLFNBQVMsR0FBRyxDQUFDLENBQUMsQ0FBQyxDQUFDLFNBQVMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxJQUFJLENBQUMsQ0FBQyxJQUFJLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxPQUFPLENBQUMsU0FBUyxDQUFDLENBQUMsQ0FBQyxHQUFHLENBQUMsR0FBRyxDQUFDLENBQUMsSUFBSSxDQUFDLE1BQU0sRUFBRSxDQUFDLENBQUMsSUFBSSxHQUFHLENBQUMsQ0FBQyxPQUFPLENBQUMsQ0FBQyxJQUFJLENBQUMsQ0FBQyxDQUFDLENBQUMsT0FBTyxDQUFDLENBQUMsQ0FBQyxJQUFJLENBQUMsQ0FBQyxPQUFPLENBQUMsR0FBRyxDQUFDLENBQUMsTUFBTSxFQUFFLEVBQUUsR0FBRyxDQUFDLENBQUMsVUFBVSxDQUFDLENBQUMsQ0FBQyxHQUFHLENBQUMsQ0FBQyxFQUFFLENBQUMsQ0FBQyxDQUFDLENBQUMsSUFBSSxDQUFDLENBQUMsSUFBSSxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsUUFBUSxDQUFDLFNBQVMsQ0FBQyxDQUFDLENBQUMsT0FBTyxDQUFDLENBQUMsUUFBUSxDQUFDLENBQUMsQ0FBQyxJQUFJLENBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxPQUFPLENBQUMsU0FBUyxDQUFDLENBQUMsQ0FBQyxPQUFPLENBQUMsQ0FBQyxPQUFPLENBQUMsQ0FBQyxDQUFDLElBQUksQ0FBQyxFQUFDLENBQUMsQ0FBQyxDQUFDLEdBQUcsQ0FBQyxDQUFDLEVBQUUsQ0FBQyxFQUFDLENBQUMsR0FBRyxDQUFDLEdBQUcsQ0FBQyxFQUFDLENBQUMsR0FBRyxDQUFRLEtBQUssQ0FBQyxHQUFHLENBQUMsS0FBSyxDQUFDLENBQUMsR0FBRzs7SUNBdnFYO0lBU00sU0FBVSxlQUFlLENBQUMsU0FBaUIsRUFBQTtRQUU3QyxNQUFNLEdBQUcsR0FBRyxHQUFHLENBQUMsS0FBSyxDQUFDLFNBQVMsQ0FBQyxDQUFDO0lBQ2pDLElBQUEsR0FBRyxDQUFDLFNBQVMsQ0FBQztJQUNkLElBQUEsR0FBRyxDQUFDLElBQUksQ0FBQztJQUNULElBQUEsSUFBSSxXQUFXLEdBQUcsQ0FBQSxFQUFHLEdBQUcsQ0FBQyxNQUFNLENBQU0sR0FBQSxFQUFBLEdBQUcsQ0FBQyxTQUFTLEdBQUcsR0FBRyxDQUFDLElBQUksSUFBSSxHQUFHLEVBQUUsQ0FBQztRQUN2RSxPQUFPLFdBQVcsQ0FBQztJQUN2QixDQUFDO0lBRUssU0FBVSx3QkFBd0IsQ0FBQyxTQUFpQixFQUFBO1FBRXRELE1BQU0sR0FBRyxHQUFHLEdBQUcsQ0FBQyxLQUFLLENBQUMsU0FBUyxDQUFDLENBQUM7SUFDakMsSUFBQSxHQUFHLENBQUMsU0FBUyxDQUFDO0lBQ2QsSUFBQSxHQUFHLENBQUMsSUFBSSxDQUFDO0lBQ1QsSUFBQSxJQUFJLFdBQVcsR0FBRyxDQUFBLEVBQUcsR0FBRyxDQUFDLE1BQU0sQ0FBTSxHQUFBLEVBQUEsR0FBRyxDQUFDLFNBQVMsR0FBRyxHQUFHLENBQUMsSUFBSSxJQUFJLEdBQUcsRUFBRSxDQUFDO1FBQ3ZFLElBQUksR0FBRyxDQUFDLEtBQUssRUFBRTtJQUNYLFFBQUEsV0FBVyxJQUFJLENBQUksQ0FBQSxFQUFBLEdBQUcsQ0FBQyxLQUFLLEVBQUUsQ0FB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SyxFQUFFO0lBQ3ZELFFBQVEsSUFBSSxFQUFFLEVBQUUsRUFBRSxDQUFDO0lBQ25CLFFBQVEsQ0FBQyxFQUFFLEdBQUcsQ0FBQyxFQUFFLEdBQUcsSUFBSSxDQUFDLFdBQVcsTUFBTSxJQUFJLElBQUksRUFBRSxLQUFLLEtBQUssQ0FBQyxHQUFHLEtBQUssQ0FBQyxHQUFHLEVBQUUsQ0FBQyxJQUFJLE1BQU0sSUFBSSxJQUFJLEVBQUUsS0FBSyxLQUFLLENBQUMsR0FBRyxLQUFLLENBQUMsR0FBRyxFQUFFLENBQUMsSUFBSSxDQUFDLEVBQUUsRUFBRSxLQUFLLENBQUMsQ0FBQztJQUM1SSxLQUFLLENBQUM7SUFDTixJQUFJLGdCQUFnQixDQUFDLFNBQVMsQ0FBQyxLQUFLLEdBQUcsVUFBVSxHQUFHLEVBQUU7SUFDdEQsUUFBUSxJQUFJLEVBQUUsRUFBRSxFQUFFLENBQUM7SUFDbkIsUUFBUSxDQUFDLEVBQUUsR0FBRyxDQUFDLEVBQUUsR0FBRyxJQUFJLENBQUMsV0FBVyxNQUFNLElBQUksSUFBSSxFQUFFLEtBQUssS0FBSyxDQUFDLEdBQUcsS0FBSyxDQUFDLEdBQUcsRUFBRSxDQUFDLEtBQUssTUFBTSxJQUFJLElBQUksRUFBRSxLQUFLLEtBQUssQ0FBQyxHQUFHLEtBQUssQ0FBQyxHQUFHLEVBQUUsQ0FBQyxJQUFJLENBQUMsRUFBRSxFQUFFLEdBQUcsQ0FBQyxDQUFDO0lBQzNJLEtBQUssQ0FBQztJQUNOLElBQUksZ0JBQWdCLENBQUMsU0FBUyxDQUFDLFFBQVEsR0FBRyxZQUFZO0lBQ3RELFFBQVEsSUFBSSxFQUFFLEVBQUUsRUFBRSxDQUFDO0lBQ25CLFFBQVEsQ0FBQyxFQUFFLEdBQUcsQ0FBQyxFQUFFLEdBQUcsSUFBSSxDQUFDLFdBQVcsTUFBTSxJQUFJLElBQUksRUFBRSxLQUFLLEtBQUssQ0FBQyxHQUFHLEtBQUssQ0FBQyxHQUFHLEVBQUUsQ0FBQyxRQUFRLE1BQU0sSUFBSSxJQUFJLEVBQUUsS0FBSyxLQUFLLENBQUMsR0FBRyxLQUFLLENBQUMsR0FBRyxFQUFFLENBQUMsSUFBSSxD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
