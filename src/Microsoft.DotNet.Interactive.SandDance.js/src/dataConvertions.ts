// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Data } from "./dataTypes";

export function convertListOfRows(data: Data) : any{
    return JSON.parse(JSON.stringify(data.data));
}