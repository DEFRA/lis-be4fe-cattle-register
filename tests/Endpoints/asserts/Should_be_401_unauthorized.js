client.test("Should be 401 Unauthorized", () => {
    client.assert(response.status === 401, "HTTP Response status code is not 401");
});
