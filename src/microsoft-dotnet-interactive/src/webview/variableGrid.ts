import { VariableGridRow } from './variableGridInterfaces';

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
                contains(row.row.kernel, filterElement.value)) {
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
    document.getElementById('clear')!.addEventListener('click', clearFilter);
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

    const valueHeader = document.createElement('th');
    valueHeader.classList.add('value-column');
    valueHeader.innerText = 'Value';
    header.appendChild(valueHeader);

    const kernelHeader = document.createElement('th');
    kernelHeader.classList.add('kernel-column');
    kernelHeader.innerText = 'Kernel';
    header.appendChild(kernelHeader);

    const shareHeader = document.createElement('th');
    shareHeader.classList.add('share-column');
    shareHeader.innerText = 'Share';
    header.appendChild(shareHeader);

    for (const row of rows) {
        const dataRow = document.createElement('tr');
        table.appendChild(dataRow);

        const dataName = document.createElement('td');
        dataName.innerText = truncateValue(row.name);
        dataRow.appendChild(dataName);

        const dataValue = document.createElement('td');
        dataValue.innerText = truncateValue(row.value);
        dataRow.appendChild(dataValue);

        const dataKernel = document.createElement('td');
        dataKernel.innerText = truncateValue(row.kernel);
        dataRow.appendChild(dataKernel);

        const dataShare = document.createElement('td');
        dataShare.innerHTML = `<a href="${row.link}">Share to...</a>`;
        dataRow.appendChild(dataShare);

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

function doTheThing(kernelName: string, valueName: string) {
    vscode.postMessage({ kernelName, valueName });
}
