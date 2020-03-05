define({
    createDotnetInteractiveClient: function (address, global) {
        let rootUrl = address;
        if (!address.endsWith("/")) {
            rootUrl = `${rootUrl}/`;
        }

        async function clientFetch(url, init) {
            let address = url;
            if (!address.startsWith("http")) {
                address = `${rootUrl}${url}`;
            }
            let response = await fetch(address, init);
            return response;
        };

        let client = {};

        client.fetch = clientFetch;

        client.getVariable = async (kernel, variable) => {
            let response = await clientFetch(`variables/${kernel}/${variable}`);
            let variableValue = await response.json();
            return variableValue;
        };

        client.getResource = async (resource) => {
            let response = await clientFetch(`resources/${resource}`);
            return response;
        };

        client.getResourceUrl = (resource) => {
            let resourceUrl = `${rootUrl}resources/${resource}`;
            return resourceUrl;
        };

        client.loadKernels = () => {
            clientFetch("kernels")
                .then(r => {
                    return r.json();
                })
                .then(kernelNames => {
                    if (Array.isArray(kernelNames) && kernelNames.length > 0) {
                        for (let index = 0; index < kernelNames.length; index++) {
                            let kernelName = kernelNames[index];
                            client[kernelName] = {
                                getVariable: (variableName) => {
                                    return client.getVariable(kernelName, variableName);
                                }
                            }
                        }
                    }
                });
        }

        global.interactive = client;

        client.loadKernels();

    }

});