// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { createRoot } from 'react-dom/client';
import { VariableGrid } from "./components/VariableGrid";
import './style.css';

const container = document.getElementById("VariableGridContainer");
const root = createRoot(container!);
const data = [
    {
        kernelDisplayName: "csharp - display",
        link: "",
        name: "a",
        value: '"123"',
        kernelName: "csharp",
        typeName: "System.String"
    },
    {
        kernelDisplayName: "csharp - display",
        link: "",
        name: "b",
        value: '"123"',
        kernelName: "csharp",
        typeName: "System.String"
    },
    {
        kernelDisplayName: "csharp - display",
        link: "",
        name: "c",
        value: '"123"',
        kernelName: "csharp",
        typeName: "System.String"
    },
    {
        kernelDisplayName: "fsharp - display",
        link: "",
        name: "c",
        value: '92342',
        kernelName: "fsharp",
        typeName: "System.Int64"
    }
]
root.render(<VariableGrid rows={data} />);