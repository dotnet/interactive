Components for LSP api
    * code-mirror
    * monaco editor
    * js lsp client
    * js dotnet-interactive client  
    * c# lsp api over http

Why test
    * design feedback
    * each component works correctly
    * componente A and component B work correctly together
      * lsp-client to dotnet-interactive client
      * code-mirror to lsp-client
      * monaco to lsp-client
    * external contract is not broken

code-mirror wiring for lsp tests
    test editor discovery
    test editor gestures and expected api call on lsp js client
