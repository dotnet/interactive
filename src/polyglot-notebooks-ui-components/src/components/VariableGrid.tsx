// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from "react";
import { VariableGridRow } from "../contracts/types";


export type VariableGridProps = {
    rows: VariableGridRow[]
};

export type VariableGridState = {
    rows: VariableGridRow[]
    shareValue: (data: VariableGridRow) => void,
    filter: string,
    drag?: {
        iniMouse: number,
        iniSize: number
    }
};

function ensureId(rows: VariableGridRow[]) {

    for (let row of rows) {
        row.id = row.id || `${row.kernelName}-${row.name}`;
    }
}

export class VariableGrid extends React.Component<VariableGridProps, VariableGridState> {

    state: VariableGridState = {
        rows: [],
        filter: "",
        shareValue: (data: VariableGridRow) => {
            console.log(data);
        }
    };

    constructor(props: VariableGridProps) {
        super(props);

    }

    componentDidMount(): void {
        const rows = [...this.props.rows];
        ensureId(rows);
        this.setState({
            ...this.state,
            rows: rows
        });

        this.configureShare();

        window.addEventListener('message', event => {
            switch (event.data.command) {
                case 'set-rows':
                    const rows = [...event.data.rows];
                    ensureId(rows);
                    this.setState({
                        ...this.state,
                        rows: rows
                    });

                    break;
            }
        });
    }

    handleStart(e: React.DragEvent<HTMLDivElement>, id: string) {

        const element = document.getElementById(id);
        if (element) {
            let iniMouse = e.clientX;
            let iniSize = parseInt(window.getComputedStyle(element).width);
            this.setState({
                ...this.state,
                drag: {
                    iniMouse: iniMouse,
                    iniSize: iniSize
                }
            });
        }
    }

    handleMove(e: React.DragEvent<HTMLDivElement>, id: string) {

        if (this.state.drag && e.clientX) {
            const element = document.getElementById(id);
            if (element) {
                let iniMouse = this.state.drag.iniMouse!;
                let iniSize = this.state.drag.iniSize!;
                let endMouse = e.clientX;

                let endSize = iniSize + (endMouse - iniMouse);
                element.style.width = `${endSize}px`;
            }
        }
    }

    configureShare() {
        try {
            // @ts-ignore
            if (acquireVsCodeApi !== undefined) {
                // @ts-ignore
                if (typeof acquireVsCodeApi === 'function') {
                    // @ts-ignore
                    const vscode = acquireVsCodeApi();

                    if (vscode.postMessage) {
                        this.setState({
                            ...this.setState,
                            shareValue: (data) => {
                                vscode.postMessage({
                                    command: 'shareValueWith',
                                    variableInfo: { sourceKernelName: data.kernelName, valueName: data.name }
                                });
                            }
                        });
                    }
                }
            }
        }
        catch (error) {

        }
    }

    handleFilterChange(e: React.ChangeEvent<HTMLInputElement>): void {
        this.setState({
            ...this.state,
            filter: e.target.value
        });
    }


    clearFilter(): void {
        this.setState({
            ...this.state,
            filter: ""
        });

        const element: HTMLInputElement = document.getElementById("search-filter") as HTMLInputElement;
        if (element) {
            element.value = "";
        }
    }

    cancelOnEscKey(e: React.KeyboardEvent<HTMLInputElement>): void {
        this.clearFilter();
    }

    render(): React.ReactNode {
        let rows: VariableGridRow[] = this.state?.rows || [];

        if (this.state.filter && this.state.filter !== "") {
            const filter = this.state.filter.toLocaleLowerCase();
            rows = rows.filter((row) => {
                let matches = false;

                for (const [key, value] of Object.entries(row)) {
                    const valueText = JSON.stringify(value).toLocaleLowerCase();
                    matches = valueText.includes(this.state.filter);

                    if (matches) {
                        return true;
                    }
                }

                return false;
            });
        }

        return (
            <div className="container">
                <svg style={
                    {
                        display: "none"
                    }
                } >
                    <symbol id="share-icon" viewBox="0 0 16 16">
                        <g id="canvas">
                            <path d="M16,16H0V0H16Z" fill="none" opacity="0" />
                        </g>
                        <g id="level-1">
                            <path className="arrow" d="M10.5,9.5v-2a9.556,9.556,0,0,0-7,3c0-7,7-7,7-7v-2l4,4Z" opacity="0.1" />
                            <path className="arrow" d="M15.207,5.5,10,.293V3.032C8.322,3.2,3,4.223,3,10.5v1.371l.883-1.05A9.133,9.133,0,0,1,10,8.014v2.693ZM4.085,9.26C4.834,4.081,10.254,4,10.5,4L11,4V2.707L13.793,5.5,11,8.293V7h-.5A10.141,10.141,0,0,0,4.085,9.26Z" />
                            <path className="arrow-box" d="M12,10.121V15H0V4H1V14H11V11.121Z" />
                        </g>
                    </symbol>
                </svg>
                <div id="toolbar" className="toolbar">
                    <input
                        id="search-filter"
                        aria-label="filter the grid results"
                        placeholder="filter"
                        onKeyDown={(e) => this.cancelOnEscKey(e)}
                        onChange={(e) => this.handleFilterChange(e)}
                    />
                </div>
                <table>
                    <tbody>
                        <tr>
                            <th
                                key={0}
                                id={`0-0`}
                                className="header"
                            >
                                Name
                                <div
                                    className='dragger'
                                    draggable={true}
                                    onDragStart={(e) => this.handleStart(e, `0-0`)}
                                    onDrag={(e) => this.handleMove(e, `0-0`)}
                                />
                            </th>
                            <th
                                key={1}
                                id={`0-1`}
                                className="header"
                            >
                                Value
                                <div
                                    className='dragger'
                                    draggable={true}
                                    onDragStart={(e) => this.handleStart(e, `0-1`)}
                                    onDrag={(e) => this.handleMove(e, `0-1`)}
                                />
                            </th>
                            <th
                                key={2}
                                id={`0-2`}
                                className="header"
                            >
                                TypeName
                                <div
                                    className='dragger'
                                    draggable={true}
                                    onDragStart={(e) => this.handleStart(e, `0-2`)}
                                    onDrag={(e) => this.handleMove(e, `0-2`)}
                                />
                            </th>
                            <th
                                key={3}
                                id={`0-3`}
                                className="header"
                            >
                                Kernel
                                <div
                                    className='dragger'
                                    draggable={true}
                                    onDragStart={(e) => this.handleStart(e, `0-3`)}
                                    onDrag={(e) => this.handleMove(e, `0-3`)}
                                />
                            </th>
                            <th
                                key={4}
                                id={`0-4`}
                                className="header"
                            >
                                Actions
                            </th>
                        </tr>
                        {rows.map((row: VariableGridRow, i) =>
                            <tr key={i + 1}>
                                <td key={0} id={`${row.id}-${0}`}>
                                    <pre className="data-cell">
                                        {row.name}
                                    </pre>
                                    <div
                                        className='dragger'
                                        draggable={true}
                                        onDragStart={(e) => this.handleStart(e, `${0}-${0}`)}
                                        onDrag={(e) => this.handleMove(e, `${0}-${0}`)}
                                    />
                                </td>
                                <td key={1} id={`${row.id}-${1}`}>
                                    <pre className="data-cell">
                                        {row.value}
                                    </pre>
                                    <div
                                        className='dragger'
                                        draggable={true}
                                        onDragStart={(e) => this.handleStart(e, `${0}-${1}`)}
                                        onDrag={(e) => this.handleMove(e, `${0}-${1}`)}
                                    />
                                </td>
                                <td key={2} id={`${row.id}-${2}`}>
                                    <pre className="data-cell">
                                        {row.typeName}
                                    </pre>
                                    <div
                                        className='dragger'
                                        draggable={true}
                                        onDragStart={(e) => this.handleStart(e, `${0}-${2}`)}
                                        onDrag={(e) => this.handleMove(e, `${0}-${2}`)}
                                    />
                                </td>
                                <td key={3} id={`${row.id}-${3}`}>
                                    <pre className="data-cell">
                                        {row.kernelDisplayName}
                                    </pre>
                                    <div
                                        className='dragger'
                                        draggable={true}
                                        onDragStart={(e) => this.handleStart(e, `${0}-${3}`)}
                                        onDrag={(e) => this.handleMove(e, `${0}-${3}`)}
                                    />
                                </td>
                                <td key={4} id={`${row.id}-${4}`}>
                                    <div className="actions">
                                        <button
                                            title={`Share ${row.name} from ${row.kernelDisplayName}`}
                                            className="share"
                                            aria-label={`Share ${row.name} from ${row.kernelDisplayName} kernel to`}
                                            style={{ marginRight: 16 }}
                                            onClick={() => {
                                                this.state.shareValue(row);
                                            }}
                                        >
                                            <svg

                                                className="share-symbol"
                                                aria-hidden={true}>
                                                <use
                                                    xlinkHref="#share-icon"
                                                    aria-hidden="true">
                                                </use>
                                            </svg>

                                        </button>
                                    </div>
                                </td>
                            </tr>
                        )}
                    </tbody>
                </table>
            </div>
        );
    }
}