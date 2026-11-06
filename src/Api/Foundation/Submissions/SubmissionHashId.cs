// <copyright file="SubmissionHashId.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Foundation.Submissions;

using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

/// <summary>
/// The id the API uses for a submission: nine Crockford base32 characters from a hash of its GUID, as <c>XXX-XXX-XXX</c>.
/// </summary>
public static class SubmissionHashId
{
    private const string Alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";
    private const int BitsPerCharacter = 5;
    private const int CharactersPerGroup = 3;
    private const int Length = 9;
    private const char Separator = '-';

    public static string Create(string submissionGuid)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(submissionGuid));
        var bits = BinaryPrimitives.ReadUInt64BigEndian(hash);

        var id = new StringBuilder(Length + 2);
        for (var index = 0; index < Length; index++)
        {
            if (index > 0 && index % CharactersPerGroup == 0)
            {
                id.Append(Separator);
            }

            // The top five bits first, then the next five, and so on.
            var shift = (sizeof(ulong) * 8) - (BitsPerCharacter * (index + 1));
            id.Append(Alphabet[(int)((bits >> shift) & 0b11111)]);
        }

        return id.ToString();
    }

    /// <summary>
    /// Returns the stored form of an id received from a caller, who may have typed it in lower case or mistaken a
    /// digit for a similar letter. As Crockford base32 sets out, <c>O</c> is read as <c>0</c>, and <c>I</c> and <c>L</c> as <c>1</c>.
    /// </summary>
    /// <param name="id">The id received.</param>
    /// <returns>The id in upper case, without surrounding white space or look-alike letters.</returns>
    public static string Normalise(string id) =>
        id.Trim()
            .ToUpperInvariant()
            .Replace('O', '0')
            .Replace('I', '1')
            .Replace('L', '1');
}
