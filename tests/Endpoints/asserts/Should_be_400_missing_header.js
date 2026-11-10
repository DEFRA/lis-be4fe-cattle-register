client.test("Should be 400 Bad Request", () => {
    client.assert(response.status === 400, "HTTP Response status code is not 400");
});

client.test("Error code is missing_header", () => {
    var code = response.body.error && response.body.error.code;
    client.assert(code === "missing_header", "Expected error.code 'missing_header' but received '" + code + "'");
});
