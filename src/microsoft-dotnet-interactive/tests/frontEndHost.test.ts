// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { expect } from "chai";
import { CompositeKernel } from '../src/compositeKernel';
import * as connection from "../src/connection";
import * as contracts from "../src/contracts";
import { clearTokenAndId } from "./testSupport";
import * as frontEndHost from "../src/webview/frontEndHost";
import * as rxjs from "rxjs";

describe("frontEndHost", () => {

    function noop() {
    };

    it("createHost adds interactive object to global and configures require", () => {
        const testGlobal: any = {};
        const remoteToLocal = new rxjs.Subject<connection.KernelCommandOrEventEnvelope>();
        const localToRemote = new rxjs.Subject<connection.KernelCommandOrEventEnvelope>();
        frontEndHost.createHost(testGlobal, 'testKernel', interactive => {
            interactive.require = "noop";
        }, noop, localToRemote, remoteToLocal, noop);
        expect(testGlobal.interactive.require).to.equal("noop");
        expect(testGlobal.kernel).to.not.be.undefined;
    });

    it("createHost adds composite kernel and kernel host to global object", () => {
        const testGlobal: any = {};
        const remoteToLocal = new rxjs.Subject<connection.KernelCommandOrEventEnvelope>();
        const localToRemote = new rxjs.Subject<connection.KernelCommandOrEventEnvelope>();
        frontEndHost.createHost(testGlobal, 'testKernel', noop, noop, localToRemote, remoteToLocal, noop);
        expect(testGlobal['testKernel'].compositeKernel).to.not.be.undefined;
        expect(testGlobal['testKernel'].kernelHost).to.not.be.undefined;
    });

    it("createHost notifies when ready", (done) => {
        const remoteToLocal = new rxjs.Subject<connection.KernelCommandOrEventEnvelope>();
        const localToRemote = new rxjs.Subject<connection.KernelCommandOrEventEnvelope>();
        frontEndHost.createHost({}, 'testKernel', noop, noop, localToRemote, remoteToLocal, () => {
            done();
        });
    });

    it("createHost sends initialization events", () => {
        const remoteToLocal = new rxjs.Subject<connection.KernelCommandOrEventEnvelope>();
        const localToRemote = new rxjs.Subject<connection.KernelCommandOrEventEnvelope>();
        const testGlobal: any = {};
        const seenMessages: connection.KernelCommandOrEventEnvelope[] = [];
        localToRemote.subscribe({
            next: message => {
                seenMessages.push(clearTokenAndId(message));
            }
        });
        frontEndHost.createHost(testGlobal, 'testKernel', noop, noop, localToRemote, remoteToLocal, noop);
        expect(seenMessages).to.deep.equal([{
            event: {},
            eventType: 'KernelReady',
            routingSlip: ['kernel://testkernel/']
        },
        {
            event:
            {
                kernelInfo:
                {
                    aliases: [],
                    languageName: undefined,
                    languageVersion: undefined,
                    localName: 'testKernel',
                    supportedDirectives: [],
                    supportedKernelCommands: [{ name: 'RequestKernelInfo' }],
                    uri: 'kernel://testkernel/'
                }
            },
            eventType: 'KernelInfoProduced',
            routingSlip: ['kernel://testkernel/']
        },
        {
            event:
            {
                kernelInfo:
                {
                    aliases: ['js'],
                    languageName: 'JavaScript',
                    languageVersion: undefined,
                    localName: 'javascript',
                    supportedDirectives: [],
                    supportedKernelCommands:
                        [{ name: 'RequestKernelInfo' },
                        { name: 'SubmitCode' },
                        { name: 'RequestValueInfos' },
                        { name: 'RequestValue' },
                        { name: 'SendValue' }],
                    uri: 'kernel://testkernel/javascript'
                }
            },
            eventType: 'KernelInfoProduced',
            routingSlip: ['kernel://testkernel/javascript']
        }]);
    });

    it("front end kernel adds proxy when new kernel info is seen", () => {
        const remoteToLocal = new rxjs.Subject<connection.KernelCommandOrEventEnvelope>();
        const localToRemote = new rxjs.Subject<connection.KernelCommandOrEventEnvelope>();
        const testGlobal: any = {};
        frontEndHost.createHost(testGlobal, 'testKernel', noop, noop, localToRemote, remoteToLocal, noop);
        const compositeKernel = <CompositeKernel>testGlobal['testKernel'].compositeKernel;
        remoteToLocal.next({
            eventType: contracts.KernelInfoProducedType,
            event: <contracts.KernelInfoProduced>{
                kernelInfo: {
                    localName: 'sql',
                    uri: 'kernel://remote/sql',
                    aliases: [],
                    languageName: 'SQL',
                    languageVersion: '10',
                    supportedDirectives: [],
                    supportedKernelCommands: [
                        {
                            name: contracts.RequestKernelInfoType
                        },
                        {
                            name: contracts.SubmitCodeType
                        },
                        {
                            name: contracts.RequestValueInfosType
                        },
                        {
                            name: contracts.RequestValueType
                        }
                    ]
                }
            }
        });
        const kernel = compositeKernel.findKernelByName('sql');
        expect(kernel).to.not.be.undefined;
        expect(kernel!.kernelInfo).to.deep.equal({
            aliases: [],
            languageName: 'SQL',
            languageVersion: '10',
            localName: 'sql',
            remoteUri: 'kernel://remote/sql',
            supportedDirectives: [],
            supportedKernelCommands:
                [{ name: 'RequestKernelInfo' },
                { name: 'SubmitCode' },
                { name: 'RequestValueInfos' },
                { name: 'RequestValue' }],
            uri: 'kernel://testkernel/sql'
        });
    });

    it("front end kernel updates proxy when new kernel info is seen", () => {
        const remoteToLocal = new rxjs.Subject<connection.KernelCommandOrEventEnvelope>();
        const localToRemote = new rxjs.Subject<connection.KernelCommandOrEventEnvelope>();
        const testGlobal: any = {};
        frontEndHost.createHost(testGlobal, 'testKernel', noop, noop, localToRemote, remoteToLocal, noop);
        const compositeKernel = <CompositeKernel>testGlobal['testKernel'].compositeKernel;
        remoteToLocal.next({
            eventType: contracts.KernelInfoProducedType,
            event: <contracts.KernelInfoProduced>{
                kernelInfo: {
                    localName: 'sql',
                    uri: 'kernel://remote/sql',
                    aliases: [],
                    supportedDirectives: [],
                    supportedKernelCommands: [
                        {
                            name: contracts.RequestKernelInfoType
                        }
                    ]
                }
            }
        });
        remoteToLocal.next({
            eventType: contracts.KernelInfoProducedType,
            event: <contracts.KernelInfoProduced>{
                kernelInfo: {
                    localName: 'sql',
                    uri: 'kernel://remote/sql',
                    aliases: [],
                    languageName: 'SQL',
                    languageVersion: '10',
                    supportedDirectives: [],
                    supportedKernelCommands: [
                        {
                            name: contracts.RequestKernelInfoType
                        },
                        {
                            name: contracts.SubmitCodeType
                        },
                        {
                            name: contracts.RequestValueInfosType
                        },
                        {
                            name: contracts.RequestValueType
                        }
                    ]
                }
            }
        });
        const kernel = compositeKernel.findKernelByName('sql');
        expect(kernel).to.not.be.undefined;
        expect(kernel!.kernelInfo).to.deep.equal({
            aliases: [],
            languageName: 'SQL',
            languageVersion: '10',
            localName: 'sql',
            remoteUri: 'kernel://remote/sql',
            supportedDirectives: [],
            supportedKernelCommands:
                [{ name: 'RequestKernelInfo' },
                { name: 'SubmitCode' },
                { name: 'RequestValueInfos' },
                { name: 'RequestValue' }],
            uri: 'kernel://testkernel/sql'
        });
    });

});
