function createDotnetInteractiveClient(address) {
    let rootUrl = address;

    let clientFetch = (url, init) => {
        let address = url;
        if (!address.startsWith("http")) {
            address = `${rootUrl}${url}`
        }
        return fetch(address, init);
    };

    let client = {};

    client.fetch = clientFetch;

    client.getVariable = (kernel, variable) => {
        return clientFetch(`/${kernel}/variables/${variable}`);
    };

    client.getReource = (resource) => {
        return clientFetch(`/resources/${resource}`);
    };

    return client
}