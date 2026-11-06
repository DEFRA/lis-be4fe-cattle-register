client.test("Request executed successfully", function () {
    client.assert(response.status === 404, "Response status is 404");
});
