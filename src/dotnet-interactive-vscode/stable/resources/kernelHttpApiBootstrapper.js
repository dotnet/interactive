(function (global) {
    console.log("Initializaing .NET Interactive http api");
    if (!global) {
        global = window;
    }

    const timerId = global.setInterval(() => {
        console.log('Sending getHttpApiEndpoint Command');
        postKernelMessage({
            command: 'getHttpApiEndpoint'
        });
    }, 500);

    function clearApiRequest() {
        global.clearInterval(timerId);
    }

    // Handle the message inside the webview
    onDidReceiveKernelMessage(event => {

        const message = event;

        switch (message.command) {
            case 'resetFactories':
                clearApiRequest();
                postKernelMessage({
                    command: 'getHttpApiEndpoint'
                });
                break;
            case 'configureFactories':
                clearApiRequest();
                let uri = message.endpointUri + "";
                if (!uri.endsWith("/")) {
                    uri += "/";
                }
                console.log(`setting up factories using ${uri}`);
                let hash = Date.now().toString();

                let loadDotnetInteractiveApi = function () {
                    // use probing to find host url and api resources
                    // load interactive helpers and language services
                    let dotnetInteractiveRequire = require.config({
                        context: hash,
                        paths: {
                            'dotnet-interactive': `${uri}resources`
                        },
                        urlArgs: `cacheBuster=${hash}`
                    }) || require;


                    global.dotnetInteractiveRequire = dotnetInteractiveRequire;
                    global.configureRequireFromExtension = function (extensionName, extensionCacheBuster) {
                        let paths = {};
                        paths[extensionName] = `${uri}extensions/${extensionName}/resources/`;

                        let internalRequire = require.config({
                            context: extensionCacheBuster,
                            paths: paths,
                            urlArgs: `cacheBuster=${extensionCacheBuster}`
                        }) || require;

                        return internalRequire;
                    };

                    dotnetInteractiveRequire([
                        'dotnet-interactive/dotnet-interactive'
                    ],
                        function (dotnet) {
                            dotnet.init(global);
                            dotnet.createDotnetInteractiveClient(uri)
                                .then(client => {
                                    global.createDotnetInteractiveClient = function () { return Promise.resolve(client); }
                                    console.log('dotnet-interactive client connection established');
                                })
                                .catch(error => {
                                    console.log('dotnet-interactive client connection cannot be established');
                                    console.log(error);
                                });
                            console.log(`dotnet-interactive js api initialised for ${uri}`);
                        },
                        function (error) {
                            console.log(`dotnet-interactive js api initialisation failed for ${uri}`);
                            console.log(error);
                        }
                    );

                };

                if (typeof require !== typeof Function || typeof require.config !== typeof Function) {
                    let require_script = document.createElement('script');
                    require_script.setAttribute('src', 'https://cdnjs.cloudflare.com/ajax/libs/require.js/2.3.6/require.min.js');
                    require_script.setAttribute('type', 'text/javascript');
                    require_script.onload = function () {
                        loadDotnetInteractiveApi();
                    };

                    document.getElementsByTagName('head')[0].appendChild(require_script);
                }
                else {
                    loadDotnetInteractiveApi();
                }
                break;
        }
    });
})(window);
