define({
    init: function (global, document, host, sentinelElement) {
        // detect the editor type
        let editorType = null;

        // from the given sentinel element, try to navigate up to the notebook root and determine its type
        // ... jupyter lab
        let notebookRoot = sentinelElement.closest('.jp-Notebook');
        if (notebookRoot) {
            editorType = 'jupyter';
        }

        // ... jupyter notebook (classic)
        if (!notebookRoot) {
            notebookRoot = document.getElementById('notebook-container');
            if (notebookRoot) {
                editorType = 'jupyter';
            }
        }

        // decorate notebook root so it can easily be found and attach the appropriate language service
        if (notebookRoot) {
            notebookRoot.classList.add('dotnet-interactive-notebook-root');
            notebookRoot.data = notebookRoot.data || {};
            notebookRoot.data['dotnet-interactive-host'] = host;

            // editor-specific lsp hookup
            switch (editorType) {
                case 'jupyter':
                    dotnet_require(['dotnet-interactive/code-mirror-lsp'], function (codeMirror) {
                        codeMirror.init(global, document, notebookRoot);
                    });
                    break;
                default:
                    break;
            }
        }
    }
});
