// <copyright file="NotConfiguredUnallocatedEarTagSourceTest.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Tests.Services;

using System.Text;
using Defra.Lis.Be4Fe.Api.Config;
using Defra.Lis.Be4Fe.Api.Services;
using Microsoft.Extensions.Options;

public sealed class NotConfiguredUnallocatedEarTagSourceTest
{
    private const string Cph = "12/345/6789";

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public async Task ShouldReturnNoTagsWhenNoDataIsConfigured(string? data)
    {
        var source = CreateSource(data);

        var tags = await source.GetUnallocatedEarTagAsync(Cph, TestContext.Current.CancellationToken);

        tags.ShouldBeEmpty();
    }

    [Fact]
    public async Task ShouldReturnOnlyTheTagsForTheCph()
    {
        var source = CreateSource(Encode("""
            [
              {"cph":"12/345/6789","tag":"UK123456700001"},
              {"cph":"98/765/4321","tag":"UK987654300001"},
              {"cph":"12/345/6789","tag":"UK123456700002"}
            ]
            """));

        var tags = await source.GetUnallocatedEarTagAsync(Cph, TestContext.Current.CancellationToken);

        tags.Select(tag => tag.Eartag).ShouldBe(["UK123456700001", "UK123456700002"]);
    }

    [Fact]
    public async Task ShouldReadPropertyNamesInAnyCase()
    {
        var source = CreateSource(Encode("""[{"Cph":"12/345/6789","Tag":"UK123456700001"}]"""));

        var tags = await source.GetUnallocatedEarTagAsync(Cph, TestContext.Current.CancellationToken);

        tags.ShouldHaveSingleItem().Eartag.ShouldBe("UK123456700001");
    }

    [Fact]
    public async Task ShouldReturnAnEmptyEartagWhenATagHasNone()
    {
        var source = CreateSource(Encode("""[{"cph":"12/345/6789","tag":null}]"""));

        var tags = await source.GetUnallocatedEarTagAsync(Cph, TestContext.Current.CancellationToken);

        tags.ShouldHaveSingleItem().Eartag.ShouldBe(string.Empty);
    }

    [Fact]
    public async Task ShouldReturnNoTagsWhenTheDataIsJsonNull()
    {
        var source = CreateSource(Encode("null"));

        var tags = await source.GetUnallocatedEarTagAsync(Cph, TestContext.Current.CancellationToken);

        tags.ShouldBeEmpty();
    }

    [Fact]
    public async Task ShouldFailWhenTheDataIsNotBase64()
    {
        var source = CreateSource("not base64!");

        await Should.ThrowAsync<FormatException>(() => source.GetUnallocatedEarTagAsync(Cph, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ShouldRejectMissingOptions()
    {
        var source = new NotConfiguredUnallocatedEarTagSource(null!);

        await Should.ThrowAsync<ArgumentNullException>(() => source.GetUnallocatedEarTagAsync(Cph, TestContext.Current.CancellationToken));
    }

    private static NotConfiguredUnallocatedEarTagSource CreateSource(string? data) =>
        new(Options.Create(new UnallocatedEarTagOptions { Data = data }));

    private static string Encode(string json) => Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
}
