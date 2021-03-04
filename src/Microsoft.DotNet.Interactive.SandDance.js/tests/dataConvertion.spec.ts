// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as chai from "chai";
import { Data } from "../src/dataTypes";
import { convertListOfRows } from "../src/dataConvertions"
const expect = chai.expect;
describe("Data", ()=>{
    describe("Tabular JSON", ()=>{
        it("can be converted to list of rows", ()=>{
            let source: Data = {
                schema: {
                    fields: [
                        {
                            name: "valueA",
                            type: "integer"
                        },
                        {
                            name: "valueB",
                            type: "integer"
                        },
                        {
                            name: "valueC",
                            type: "integer"
                        },
                        {
                            name: "label",
                            type: "string"
                        },
                        {
                            name: "isGood",
                            type: "boolean"
                        }
                    ],
                    primaryKey: []
                },
                data: [
                    { valueA: 3, valueB: 5, valueC: 1, label: "first", isGood: false },
                    { valueA: 10, valueB: 1, valueC: 12, label: "second", isGood: true },
                    { valueA: 10, valueB: 1, valueC: 12, label: "fifth", isGood: true },
                    { valueA: 10, valueB: 1, valueC: 12, label: "sixth", isGood: true },
                    { valueA: 10, valueB: 1, valueC: 12, label: "seventh", isGood: true },
                    { valueA: -5, valueB: 8, valueC: 6, label: "third", isGood: true },
                    { valueA: -50, valueB: 8, valueC: 6, label: "third", isGood: true }
                ]
            };

            let expected =  [
                { valueA: 3, valueB: 5, valueC: 1, label: "first", isGood: false },
                { valueA: 10, valueB: 1, valueC: 12, label: "second", isGood: true },
                { valueA: 10, valueB: 1, valueC: 12, label: "fifth", isGood: true },
                { valueA: 10, valueB: 1, valueC: 12, label: "sixth", isGood: true },
                { valueA: 10, valueB: 1, valueC: 12, label: "seventh", isGood: true },
                { valueA: -5, valueB: 8, valueC: 6, label: "third", isGood: true },
                { valueA: -50, valueB: 8, valueC: 6, label: "third", isGood: true }
            ];

            let converted = convertListOfRows(source);

            expect(converted).to.deep.equal(expected);
        })
    })
});