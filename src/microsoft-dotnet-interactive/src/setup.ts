import { CompositeKernel } from "./compositeKernel";
import { JavascriptKernel } from "./javascriptKernel";
import { Kernel } from "./kernel";

export function setup() {
    let compositeKernel = new CompositeKernel("browser");

    const jsKernel = new JavascriptKernel();

    compositeKernel.add(jsKernel, [ "js" ]);

    // @ts-ignore
    if (publishCommandOrEvent) {
        compositeKernel.subscribeToKernelEvents(envelope => {
            // @ts-ignore
            publishCommandOrEvent(envelope);
        });
    }
}