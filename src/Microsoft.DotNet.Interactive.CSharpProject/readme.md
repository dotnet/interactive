The `CSharpProjectKernel` is the .NET Interactive backend for Try .NET.

## Try .NET / Microsoft Learn flow

The following diagram shows the interaction between the Try .NET service (trydotnet.microsoft.com) and a host page (learn.microsoft.com).

```mermaid
sequenceDiagram
    box Browser
    participant U as learn.microsoft.com
    participant I as trydotnet.microsoft.com (IFRAME)
    participant W as trydotnet.microsoft.com (WASM runner IFRAME)
    end
    box Server
    participant A as learn.microsoft.com
    participant B as trydotnet.microsoft.com
    end
    U->>A: request host page
    A->>U: page containing IFRAME, trydotnet.js links
    U->>A: request trydotnet.js
    A-->>U: trydotnet.js
    I->>B: request trydotnet.microsoft.com/editor
    B-->>I: Monaco editor
    I->>B: request trydotnet.microsoft.com/wasmrunner
    B-->>W: WASM runner
    alt Run code
    rect rgba(200, 150, 255,0.4)
    U->>I: clicking Run button calls trydotnet.js which calls postMessage API
    I->>B: send kernel commands to trydotnet.microsoft.com/commands
    B->>B: compile user code
    B-->>I: .NET assembly
    I->>W:  send .NET assembly via postMessage API
    W->>W: execute .NET assembly, capture Console.Out and Console.Error
    W-->>I: outputs via postMessage API
    I-->>U: output events via postMessage API to trydotnet.js
    U->>U: display outputs
    end
    else Diagnostics, completions, signature help
    rect rgba(128, 128, 255,0.4)
    I->>I: typing code in Monaco editor triggers language services
    I->>B: send kernel commands to trydotnet.microsoft.com/commands
    B->>B: compile user code
    B-->>I: Kernel events
    I->>I: Monace editor displays diagnostics, completions, signature help, etc.
    end
    end
```
