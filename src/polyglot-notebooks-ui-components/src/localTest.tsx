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
        name: "bergtaergradadfae asefasfasdf adfadsf asdf asdf asdf asdf asdf asdf a fadf ",
        value: '"123 lllllllllllllllllllllllllllllllllllllllooooooonnnnn  gg wer ewr wer wer wer wer we sdfgsdfgsfgsdfgsdfg sfg sgsdfg sdfg sdfg sdfg sdfg s fgsdfg g assdfa sdfa sdfadf ad fadf asdf adf"',
        kernelName: "csharp",
        typeName: "System.String"
    },
    {
        kernelDisplayName: "csharp - display  wer wer wer wer qwe354t6 wsert w54tsergsrgb zsxbtygh sbxzr",
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