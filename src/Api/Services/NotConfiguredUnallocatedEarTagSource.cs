// <copyright file="NotConfiguredUnallocatedEarTagSource.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Services;

using System.Text.Json;
using Defra.Lis.Be4Fe.Api.Config;
using Defra.Lis.Be4Fe.Models.Data;
using Microsoft.Extensions.Options;

/// <summary>
/// Stands in until the cattle API endpoint for unallocated eartags is defined.
/// </summary>
public sealed class NotConfiguredUnallocatedEarTagSource(
    IOptions<UnallocatedEarTagOptions> earTagOptions)
    : IUnallocatedEarTagSource
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public Task<IReadOnlyCollection<UnallocatedEarTag>> GetUnallocatedEarTagAsync(
        string cph,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(earTagOptions);

        if (string.IsNullOrWhiteSpace(earTagOptions.Value?.Data))
        {
            return Task.FromResult<IReadOnlyCollection<UnallocatedEarTag>>([]);
        }

        var bytes = Convert.FromBase64String(earTagOptions.Value.Data);
        var tags = JsonSerializer.Deserialize<List<UnallocatedTagItem>>(bytes, SerializerOptions) ?? [];

        return Task.FromResult<IReadOnlyCollection<UnallocatedEarTag>>(
        [
            .. tags
                .Where(t => string.Equals(t.Cph, cph, StringComparison.OrdinalIgnoreCase))
                .Select(x => new UnallocatedEarTag { Eartag = x.Tag ?? string.Empty }),
        ]);
    }

    private sealed record UnallocatedTagItem(string? Cph, string? Tag);
}
