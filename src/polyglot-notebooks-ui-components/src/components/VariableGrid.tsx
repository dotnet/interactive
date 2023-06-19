// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from "react";
import { GridLocalization, VariableGridRow } from "../contracts/types";


export type VariableGridProps = {
    rows: VariableGridRow[]
};



export type VariableGridState = {
    rows: VariableGridRow[]
    shareValue: (data: VariableGridRow) => void,
    filter: string,
    drag?: {
        iniMouse: number,
        sizes: {
            targetColumn: {
                iniSize: number,
                id: string
            },
            affectedColumn?: {
                iniSize: number,
                id: string
            }
        }
    },
    gridLocalization?: GridLocalization
};

function ensureId(rows: VariableGridRow[]) {

    for (let row of rows) {
        row.id = row.id || `${row.kernelName}-${row.name}`;
    }
}

function processNumerics(rows: VariableGridRow[]) {
    for (let row of rows) {
        if (couldBeNumeric(row.typeName)) {

        }
    }
}

function couldBeNumeric(typeName: string): boolean {
    const lower = typeName.toLowerCase();
    if (lower.match(/(int|float|double|decimal|short|byte|long)/)) {
        return true;
    }
    return false;
}

const defaultGridLocalization: GridLocalization = {
    actionsColumnHeader: 'Actions',
    nameColumnHeader: 'Name',
    valueColumnHeader: 'Value',
    typeColumnHeader: 'Type',
    kernelNameColumnHeader: 'Kernel',
    shareTemplate: 'Share value {value-name} from {kernel-name} kernel'
}

export class VariableGrid extends React.Component<VariableGridProps, VariableGridState> {

    state: VariableGridState = {
        rows: [],
        filter: "",
        shareValue: (data: VariableGridRow) => {
            console.log(data);
        },
        gridLocalization: { ...defaultGridLocalization }
    };

    idToClass: { [key: string]: string } = {
        "0-0": "name",
        "0-1": "value",
        "0-2": "type",
        "0-3": "kernel",
        "0-4": "actions"
    };

    idlayout: {
        [key: string]: {
            left?: string,
            right?: string
        }
    } = {
            "0-0": {
                right: "0-1"
            },
            "0-1": {
                left: "0-0",
                right: "0-2"
            },
            "0-2": {
                left: "0-1",
                right: "0-3"
            },
            "0-3": {
                left: "0-2",
                right: "0-4"
            },
            "0-4": {
                left: "0-3",
            },
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

        this.setColumnsWidth();

        window.addEventListener('message', event => {
            switch (event.data.command) {
                case 'set-rows':
                    {
                        if (event.data.rows) {
                            const rows = [...event.data.rows];
                            ensureId(rows);
                            processNumerics(rows);
                            const localization = event.data.localizationStrings ?? defaultGridLocalization;
                            this.setState({
                                ...this.state,
                                rows: rows,
                                gridLocalization: { ...localization }
                            });
                        } else {
                            const localization = event.data.localizationStrings ?? defaultGridLocalization;
                            this.setState({
                                ...this.state,
                                gridLocalization: { ...localization }
                            });
                        }
                    }
                    break;

                case 'configure-grid':
                    {
                        const localisazion = event.data.localizationStrings ?? defaultGridLocalization;
                        this.setState({
                            ...this.state,
                            gridLocalization: { ...localisazion }
                        });
                    }
                    break;
            }
        });
    }

    setColumnsWidth() {
        const width = this.getTableSize();
        this.setColumnWidth("name-column", width);
        this.setColumnWidth("value-column", width);
        this.setColumnWidth("type-column", width);
        this.setColumnWidth("kernel-column", width);
        this.setColumnWidth("actions-column", width);
    }

    setColumnWidth(columnId: string, tableWidth: number) {
        const tableElement = document.getElementById(columnId)!;
        const columnWidth = this.getWidth(tableElement);
        const columnWidthPercentage = (columnWidth / tableWidth) * 100.0;
        tableElement.style.width = `${columnWidthPercentage}%`;
    }

    getTableSize(): number {
        const tableElement = document.getElementById("table-root")!;
        return this.getWidth(tableElement);
    }

    getWidth(element: Element) {
        const computedStyle = window.getComputedStyle(element);
        const width = parseInt(computedStyle.width);
        return width;
    }

    handleStart(e: React.DragEvent<HTMLDivElement>, id: string) {
        const element = document.getElementById(id);
        if (element) {
            let iniMouse = e.clientX;
            let iniSize = this.getWidth(element);
            let sizes: {
                targetColumn: {
                    iniSize: number,
                    id: string
                },
                affectedColumn?: {
                    iniSize: number,
                    id: string
                }
            } = {
                targetColumn: {
                    iniSize,
                    id
                }
            };
            const affectedcolumnId = this.idlayout[id].right;
            if (affectedcolumnId) {
                sizes.affectedColumn = {
                    id: affectedcolumnId,
                    iniSize: this.getWidth(document.getElementById(affectedcolumnId)!)
                }
            }
            this.setState({
                ...this.state,
                drag: {
                    iniMouse: iniMouse,
                    sizes: sizes
                }
            });
        }
    }

    handleMove(e: React.DragEvent<HTMLDivElement>, id: string) {
        if (this.state.drag && e.clientX) {

            const element = document.getElementById(id)!;
            const tableWidth = this.getTableSize();

            if (element) {
                const startDragPosition = this.state.drag.iniMouse!;
                const sizes = this.state.drag.sizes;
                const targetColumnSize = sizes.targetColumn.iniSize!;
                const endDragPosition = e.clientX;
                let delta = (endDragPosition - startDragPosition);

                const targetClass = `${this.idToClass[id]}-column`;
                const affectedcolumnId = sizes.affectedColumn?.id;

                if (targetClass && affectedcolumnId) {

                    const sizeLimit = 40;
                    const affectedColumnSize = sizes.affectedColumn?.iniSize!;
                    if ((targetColumnSize + delta) < sizeLimit) {
                        delta = sizeLimit - targetColumnSize;
                    }
                    else if ((affectedColumnSize - delta) < sizeLimit) {
                        delta = affectedColumnSize - sizeLimit;
                    }

                    const iniSizePercentage = (targetColumnSize / tableWidth) * 100.0;
                    const deltaPercentage = (delta / tableWidth) * 100.0;
                    const endSizePercentage = iniSizePercentage + deltaPercentage;

                    const affectedColumnClass = `${this.idToClass[affectedcolumnId]}-column`;
                    const w = ((affectedColumnSize / tableWidth) * 100.0) - deltaPercentage;

                    const targetColumn: any = document.querySelector(`col.${targetClass}`)!;
                    const affectedColumn: any = document.querySelector(`col.${affectedColumnClass}`)!;

                    targetColumn.style["width"] = `${endSizePercentage}%`;
                    targetColumn.style["max-width"] = `${endSizePercentage}%`;
                    affectedColumn.style["width"] = `${w}%`;
                    affectedColumn.style["max-width"] = `${w}%`;
                }
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

    handleInput(e: React.FormEvent<HTMLInputElement>): void {
        const inputField = e.target as HTMLInputElement;
        this.updateFilter(inputField.value);
    }

    updateFilter(filter: string) {
        this.setState({
            ...this.state,
            filter: filter
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
        if (e.code === "Escape") {
            this.clearFilter();
        }
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
                        onInput={(e) => this.handleInput(e)}
                    />
                </div>
                <div className="table-container" >
                    <table id="table-root">
                        <colgroup>
                            <col id="name-column" className="name-column"></col>
                            <col id="value-column" className="value-column"></col>
                            <col id="type-column" className="type-column"></col>
                            <col id="kernel-column" className="kernel-column"></col>
                            <col id="actions-column" className="actions-column"></col>
                        </colgroup>
                        <tbody>
                            <tr>
                                <th
                                    key={0}
                                    id={`0-0`}
                                    className="header name-column"
                                >
                                    {this.state.gridLocalization!.nameColumnHeader}
                                    <div
                                        className='grip'
                                        draggable={true}
                                        onDragStart={(e) => this.handleStart(e, `0-0`)}
                                        onDrag={(e) => this.handleMove(e, `0-0`)}
                                    />
                                </th>
                                <th
                                    key={1}
                                    id={`0-1`}
                                    className="header value-column"
                                >
                                    {this.state.gridLocalization!.valueColumnHeader}
                                    <div
                                        className='grip'
                                        draggable={true}
                                        onDragStart={(e) => this.handleStart(e, `0-1`)}
                                        onDrag={(e) => this.handleMove(e, `0-1`)}
                                    />
                                </th>
                                <th
                                    key={2}
                                    id={`0-2`}
                                    className="header type-column"
                                >
                                    {this.state.gridLocalization!.typeColumnHeader}
                                    <div
                                        className='grip'
                                        draggable={true}
                                        onDragStart={(e) => this.handleStart(e, `0-2`)}
                                        onDrag={(e) => this.handleMove(e, `0-2`)}
                                    />
                                </th>
                                <th
                                    key={3}
                                    id={`0-3`}
                                    className="header kernel-column"
                                >
                                    {this.state.gridLocalization!.kernelNameColumnHeader}
                                    <div
                                        className='grip'
                                        draggable={true}
                                        onDragStart={(e) => this.handleStart(e, `0-3`)}
                                        onDrag={(e) => this.handleMove(e, `0-3`)}
                                    />
                                </th>
                                <th
                                    key={4}
                                    id={`0-4`}
                                    className="header actions-column"
                                >
                                    {this.state.gridLocalization!.actionsColumnHeader}
                                </th>
                            </tr>
                            {rows.map((row: VariableGridRow, i) =>
                                <tr key={i + 1}>
                                    <td key={0} id={`${row.id}-${0}`} className="name-column">
                                        <div
                                            title={row.name}
                                            className="data-cell long-text name-column-content">
                                            {row.name}
                                        </div>
                                        <div
                                            className='grip'
                                            draggable={true}
                                            onDragStart={(e) => this.handleStart(e, `0-0`)}
                                            onDrag={(e) => this.handleMove(e, `0-0`)}
                                        />
                                    </td>
                                    <td key={1} id={`${row.id}-${1}`} className="value-column">
                                        <div
                                            title={row.value}
                                            className="data-cell long-text value-column-content">
                                            {row.value}
                                        </div>
                                        <div
                                            className='grip'
                                            draggable={true}
                                            onDragStart={(e) => this.handleStart(e, `0-1`)}
                                            onDrag={(e) => this.handleMove(e, `0-1`)}
                                        />
                                    </td>
                                    <td key={2} id={`${row.id}-${2}`} className="type-column">
                                        <div
                                            title={row.typeName}
                                            className="data-cell long-text type-column-content">
                                            {row.typeName}
                                        </div>
                                        <div
                                            className='grip'
                                            draggable={true}
                                            onDragStart={(e) => this.handleStart(e, `0-2`)}
                                            onDrag={(e) => this.handleMove(e, `0-2`)}
                                        />
                                    </td>
                                    <td key={3} id={`${row.id}-${3}`} className="kernel-column">
                                        <div
                                            title={row.kernelDisplayName}
                                            className="data-cell long-text kernel-column-content">
                                            {row.kernelDisplayName}
                                        </div>
                                        <div
                                            className='grip'
                                            draggable={true}
                                            onDragStart={(e) => this.handleStart(e, `0-3`)}
                                            onDrag={(e) => this.handleMove(e, `0-3`)}
                                        />
                                    </td>
                                    <td key={4} id={`${row.id}-${4}`} className="actions-column">
                                        <div className="actions">
                                            <button
                                                title={this.state.gridLocalization!.shareTemplate.replace("{value-name}", row.name).replace("{kernel-name}", row.kernelDisplayName)}
                                                className="share"
                                                aria-label={this.state.gridLocalization!.shareTemplate.replace("{value-name}", row.name).replace("{kernel-name}", row.kernelDisplayName)}
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
            </div>
        );
    }
}

