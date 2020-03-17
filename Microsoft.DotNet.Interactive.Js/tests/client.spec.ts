//import  * as interactive from "../src/dotnet-interactive";
import { expect } from "chai";
import * as interactive from "../src/dotnet-interactive"

describe("client", () => {
    describe("not sure", () => {
        it("runs", () => {
            let client = interactive.createClient();
            let value = client.GetVariable("code");
            expect(value).to.eq(1);
        });
    });
});