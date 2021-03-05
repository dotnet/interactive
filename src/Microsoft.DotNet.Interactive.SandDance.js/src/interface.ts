// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { convertListOfRows } from "./dataConvertions";
import { Data } from "./dataTypes";
import * as deck from '@deck.gl/core';
import * as layers from '@deck.gl/layers';
import * as luma from '@luma.gl/core';
import * as fluentui from '@fluentui/react';
import * as vega from 'vega';
import * as React from 'react';
import * as ReactDOM from 'react-dom';
import { Explorer, use, Props, Explorer_Class } from '@msrvida/sanddance-explorer';

import "@msrvida/sanddance-explorer/dist/css/sanddance-explorer.css";
import "./app.css";

fluentui.initializeIcons();

use(fluentui, React, ReactDOM, vega, deck, layers, luma);

export interface DataExplorerSettings {
    container: HTMLDivElement,
    data: Data
}

export function createSandDanceExplorer(settings: DataExplorerSettings) {
    let converted = convertListOfRows(settings.data);
    let currentExplorer:Explorer_Class = null; 
    const explorerProps:Props = {
        logoClickUrl: 'https://microsoft.github.io/SandDance/',
        mounted: explorer => {
            currentExplorer = explorer;
            currentExplorer.load(converted);
        }
    };
    
    ReactDOM.render(React.createElement(Explorer, explorerProps), settings.container);
}