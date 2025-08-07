// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { expect } from "chai";
import { CompositeKernel } from '../src/compositeKernel';
import * as connection from "../src/connection";
import * as commandsAndEvents from "../src/commandsAndEvents";
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
                seenMessages.push(message);
            }
        });
        frontEndHost.createHost(testGlobal, 'testKernel', noop, noop, localToRemote, remoteToLocal, noop);
        expect(seenMessages.map(e => clearTokenAndId(e.toJson()))).to.deep.equal([{
            command: undefined,
            event:
            {
                kernelInfos:
                    [{
                        aliases: [],
                        displayName: 'testKernel',
                        isComposite: true,
                        isProxy: false,
                        languageName: undefined,
                        languageVersion: undefined,
                        localName: 'testKernel',
                        supportedKernelCommands: [{ name: 'RequestKernelInfo' }],
                        uri: 'kernel://testKernel/'
                    },
                    {
                        aliases: ['js'],
                        description: 'Run JavaScript code',
                        displayName: 'javascript - JavaScript',
                        isComposite: false,
                        isProxy: false,
                        languageName: 'JavaScript',
                        languageVersion: undefined,
                        localName: 'javascript',
                        supportedKernelCommands:
                            [{ name: 'RequestKernelInfo' },
                            { name: 'SubmitCode' },
                            { name: 'RequestValueInfos' },
                            { name: 'RequestValue' },
                            { name: 'SendValue' }],
                        uri: 'kernel://testKernel/javascript'
                    }]
            },
            eventType: 'KernelReady',
            routingSlip: ['kernel://testKernel/']
        }]);
    });

    it("front end kernel adds proxy when new kernel info is seen", () => {
        const remoteToLocal = new rxjs.Subject<connection.KernelCommandOrEventEnvelope>();
        const localToRemote = new rxjs.Subject<connection.KernelCommandOrEventEnvelope>();
        const testGlobal: any = {};
        frontEndHost.createHost(testGlobal, 'testKernel', noop, noop, localToRemote, remoteToLocal, noop);
        const compositeKernel = testGlobal['testKernel'].compositeKernel as CompositeKernel;
        remoteToLocal.next(new commandsAndEvents.KernelEventEnvelope(
            commandsAndEvents.KernelInfoProducedType,
            {
                kernelInfo: {
                    isComposite: false,
                    isProxy: false,
                    localName: 'sql',
                    uri: 'kernel://remote/sql',
                    description: 'This Kernel is for executing SQL code.',
                    aliases: [],
                    languageName: 'SQL',
                    languageVersion: '10',
                    displayName: 'SQL',
                    supportedKernelCommands: [
                        {
                            name: commandsAndEvents.RequestKernelInfoType
                        },
                        {
                            name: commandsAndEvents.SubmitCodeType
                        },
                        {
                            name: commandsAndEvents.RequestValueInfosType
                        },
                        {
                            name: commandsAndEvents.RequestValueType
                        }
                    ]
                }
            } as commandsAndEvents.KernelInfoProduced
        ));
        const kernel = compositeKernel.findKernelByName('sql');
        expect(kernel).to.not.be.undefined;
        expect(kernel!.kernelInfo).to.deep.equal({
            aliases: [],
            displayName: 'SQL',
            isComposite: false,
            isProxy: true,
            languageName: 'SQL',
            languageVersion: '10',
            localName: 'sql',
            remoteUri: 'kernel://remote/sql',
            description: 'This Kernel is for executing SQL code.',
            supportedKernelCommands:
                [{ name: 'RequestKernelInfo' },
                { name: 'SubmitCode' },
                { name: 'RequestValueInfos' },
                { name: 'RequestValue' }],
            uri: 'kernel://testKernel/sql'
        });
    });

    it("front end kernel updates proxy when new kernel info is seen", () => {
        const remoteToLocal = new rxjs.Subject<connection.KernelCommandOrEventEnvelope>();
        const localToRemote = new rxjs.Subject<connection.KernelCommandOrEventEnvelope>();
        const testGlobal: any = {};
        frontEndHost.createHost(testGlobal, 'testKernel', noop, noop, localToRemote, remoteToLocal, noop);
        const compositeKernel = testGlobal['testKernel'].compositeKernel as CompositeKernel;
        remoteToLocal.next(new commandsAndEvents.KernelEventEnvelope(
            commandsAndEvents.KernelInfoProducedType,
            {
                kernelInfo: {
                    isComposite: false,
                    isProxy: false,
                    localName: 'sql',
                    uri: 'kernel://remote/sql',
                    description: 'This Kernel is for executing SQL code.',
                    aliases: [],
                    displayName: 'SQL',
                    supportedKernelCommands: [
                        {
                            name: commandsAndEvents.RequestKernelInfoType
                        }
                    ]
                }
            } as commandsAndEvents.KernelInfoProduced
        ));
        remoteToLocal.next(new commandsAndEvents.KernelEventEnvelope(
            commandsAndEvents.KernelInfoProducedType,
            {
                kernelInfo: {
                    isComposite: false,
                    isProxy: false,
                    localName: 'sql',
                    uri: 'kernel://remote/sql',
                    aliases: [],
                    languageName: 'SQL',
                    languageVersion: '10',
                    displayName: 'SQL',
                    description: 'This Kernel is for executing SQL code.',
                    supportedKernelCommands: [
                        {
                            name: commandsAndEvents.RequestKernelInfoType
                        },
                        {
                            name: commandsAndEvents.SubmitCodeType
                        },
                        {
                            name: commandsAndEvents.RequestValueInfosType
                        },
                        {
                            name: commandsAndEvents.RequestValueType
                        }
                    ]
                }
            } as commandsAndEvents.KernelInfoProduced
        ));
        const kernel = compositeKernel.findKernelByName('sql');
        expect(kernel).to.not.be.undefined;
        expect(kernel!.kernelInfo).to.deep.equal({
            aliases: [],
            displayName: 'SQL',
            isComposite: false,
            isProxy: true,
            languageName: 'SQL',
            languageVersion: '10',
            localName: 'sql',
            remoteUri: 'kernel://remote/sql',
            description: 'This Kernel is for executing SQL code.',
            supportedKernelCommands:
                [{ name: 'RequestKernelInfo' },
                { name: 'SubmitCode' },
                { name: 'RequestValueInfos' },
                { name: 'RequestValue' }],
            uri: 'kernel://testKernel/sql'
        });
    });

});
