// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { createRoot } from 'react-dom/client';
import { VariableGrid } from "./components/VariableGrid";
import './style.css';

const container = document.getElementById("VariableGridContainer");
const root = createRoot(container!);

root.render(<VariableGrid rows={[]} />);