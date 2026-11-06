// <copyright file="BusinessRuleException.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Exceptions;

using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public sealed class BusinessRuleException : Exception
{
    public BusinessRuleException(string message)
        : base(message)
    {
    }
}
