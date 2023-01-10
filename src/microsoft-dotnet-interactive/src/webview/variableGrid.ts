// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { VariableGridRow, VariableInfo } from './variableGridInterfaces';

interface DisplayedVariableGridRow {
    row: VariableGridRow;
    element: HTMLElement;
}

window.addEventListener('DOMContentLoaded', () => {
    const filterElement: HTMLInputElement = document.getElementById('filter') as HTMLInputElement;
    const contentElement: HTMLDivElement = document.getElementById('content') as HTMLDivElement;
    let tableRows: DisplayedVariableGridRow[] = [];
    window.addEventListener('message', event => {
        switch (event.data.command) {
            case 'set-rows':
                tableRows = setDataRows(contentElement, event.data.rows);
                doFilter();
                break;
        }
    });

    function doFilter() {
        for (const row of tableRows) {
            row.element.style.display = 'none';
            if (contains(row.row.name, filterElement.value) ||
                contains(row.row.value, filterElement.value) ||
                contains(row.row.typeName, filterElement.value) ||
                contains(row.row.kernelDisplayName, filterElement.value)) {
                row.element.style.display = '';
            }
        }
    }

    function clearFilter() {
        filterElement.value = '';
        doFilter();
    }

    filterElement.addEventListener('keydown', (e) => {
        if (e.key === 'Escape') {
            clearFilter();
        }
    });
    filterElement.addEventListener('input', doFilter);
});

function contains(text: string, search: string): boolean {
    return text.toLowerCase().indexOf(search.toLocaleLowerCase()) > -1;
}

function setDataRows(container: HTMLElement, rows: VariableGridRow[]): DisplayedVariableGridRow[] {
    const displayedRows: DisplayedVariableGridRow[] = [];

    const table = document.createElement('table');
    const header = document.createElement('tr');
    table.appendChild(header);

    // create headers
    const nameHeader = document.createElement('th');
    nameHeader.classList.add('name-column');
    nameHeader.innerText = 'Name';
    header.appendChild(nameHeader);

    const shareHeader = document.createElement('th');
    shareHeader.classList.add('share-column');
    shareHeader.innerText = 'Share';
    header.appendChild(shareHeader);

    const valueHeader = document.createElement('th');
    valueHeader.classList.add('value-column');
    valueHeader.innerText = 'Value';
    header.appendChild(valueHeader);

    const typeHeader = document.createElement('th');
    typeHeader.classList.add('type-column');
    typeHeader.innerText = 'Type';
    header.appendChild(typeHeader);

    const kernelHeader = document.createElement('th');
    kernelHeader.classList.add('kernel-column');
    kernelHeader.innerText = 'Kernel';
    header.appendChild(kernelHeader);

    for (const row of rows) {
        const dataRow = document.createElement('tr');
        table.appendChild(dataRow);

        const dataName = document.createElement('td');
        dataName.innerText = truncateValue(row.name);
        dataRow.appendChild(dataName);

        const dataShare = document.createElement('td');
        dataShare.classList.add('share-data');
        const button = document.createElement('button');
        button.type = 'button';
        button.classList.add('share');
        button.setAttribute('aria-label', `Share ${row.name} from ${row.kernelDisplayName} kernel to`);
        button.addEventListener('click', () => shareValueWith({ sourceKernelName: row.kernelName, valueName: row.name }));
        button.innerHTML = '<svg class="share-symbol"><use xlink:href="#share-icon" aria-hidden="true"></use></svg>';
        dataShare.appendChild(button);
        dataRow.appendChild(dataShare);

        const dataValue = document.createElement('td');
        dataValue.innerText = truncateValue(row.value);
        dataRow.appendChild(dataValue);

        const dataType = document.createElement('td');
        dataType.innerText = truncateValue(row.typeName);
        dataRow.appendChild(dataType);

        const dataKernel = document.createElement('td');
        dataKernel.innerText = truncateValue(row.kernelDisplayName);
        dataRow.appendChild(dataKernel);

        displayedRows.push({
            row,
            element: dataRow,
        });
    }

    container.innerHTML = '';
    container.appendChild(table);

    return displayedRows;
}

const maxDisplayLength = 100;

function truncateValue(value: string): string {
    if (value.length > maxDisplayLength) {
        return value.substring(0, maxDisplayLength - 3) + '...';
    }

    return value;
}

// @ts-ignore
const vscode = acquireVsCodeApi();

function shareValueWith(variableInfo: VariableInfo) {
    vscode.postMessage({
        command: 'shareValueWith',
        variableInfo: variableInfo
    });
}
