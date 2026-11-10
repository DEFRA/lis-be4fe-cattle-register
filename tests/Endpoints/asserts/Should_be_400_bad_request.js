client.test("Should be 400 Bad Request", () => {
    client.assert(response.status === 400, "HTTP Response status code is not 400");
});

client.test("Response content-type is json", () => {
    var contentType = response.contentType.mimeType;
    client.assert(contentType === "application/problem+json" || contentType === "application/json", "Expected content-type 'application/problem+json' or 'application/json' but received '" + contentType + "'");
});
