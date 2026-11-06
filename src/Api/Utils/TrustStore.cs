// <copyright file="TrustStore.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Utils;

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography.X509Certificates;

[ExcludeFromCodeCoverage]
public static class TrustStore
{
    public static void LoadCustomTrustStoreFromEnvironment(this IServiceCollection _)
    {
        var certificates = GetCertificates();
        AddCertificates(certificates);
    }

    private static List<byte[]> GetCertificates()
    {
        return Environment.GetEnvironmentVariables().Cast<DictionaryEntry>()
            .Where(entry =>
                entry.Key.ToString()!.StartsWith("TRUSTSTORE_", StringComparison.Ordinal) &&
                IsBase64String(entry.Value?.ToString() ?? string.Empty))
            .Select(entry => Convert.FromBase64String(entry.Value?.ToString() ?? string.Empty)).ToList();
    }

    private static void AddCertificates(List<byte[]> certificates)
    {
        if (certificates.Count == 0)
        {
            return; // to stop trust store access denied issues on Macs
        }

        var x509Certificate2S = certificates.Select(X509CertificateLoader.LoadCertificate);
        var certificateCollection = new X509Certificate2Collection();

        foreach (var certificate2 in x509Certificate2S)
        {
            certificateCollection.Add(certificate2);
        }

        var store = new X509Store(StoreName.Root, StoreLocation.CurrentUser);
        try
        {
            store.Open(OpenFlags.ReadWrite);
            store.AddRange(certificateCollection);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Root certificate import failed: " + ex.Message, ex);
        }
        finally
        {
            store.Close();
        }
    }

    private static bool IsBase64String(string str)
    {
        var buffer = new Span<byte>(new byte[str.Length]);
        return Convert.TryFromBase64String(str, buffer, out _);
    }
}
