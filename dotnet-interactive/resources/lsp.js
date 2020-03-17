define(function () {

    function getHostFromElement(element) {
        let notebookRoot = element.closest('.dotnet-interactive-notebook-root');
        return notebookRoot.data['dotnet-interactive-host'];
    }

    return {
        init: function (global) {
            let lsp = {};

            lsp.textDocumentHover = async function (element, textDocument, line, column) {
                let host = getHostFromElement(element);
                let url = `${host}lsp/textDocument/hover`;
                let request = {
                    textDocument: {
                        uri: textDocument
                    },
                    position: {
                        line: line,
                        character: column
                    }
                };
                const response = await fetch(url, {
                    method: 'POST',
                    mode: 'cors',
                    cache: 'no-cache',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify(request)
                });
                return await response.json();
            };

            global.Lsp = lsp;
        }
    };
});
