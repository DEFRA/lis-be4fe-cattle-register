// <copyright file="NotFoundException.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Be4Fe.Api.Exceptions;

public sealed class NotFoundException
    : Exception
{
    public NotFoundException(string message)
        : base(message)
    {
    }
}
