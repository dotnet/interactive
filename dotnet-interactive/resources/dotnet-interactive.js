function createDotnetInteractiveClient(address) {
    let rootUrl = address;
    if (!address.endsWith("/")) {
        rootUrl = `${rootUrl}/`;
    }
    let clientFetch = async (url, init) => {
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

    client.getReource = (resource) => {
        let response = await clientFetch(`resources/${resource}`);
        return response;
    };

    return client;
}