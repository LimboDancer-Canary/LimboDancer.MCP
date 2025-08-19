using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public sealed class DomainException : Exception
{
    public string Code { get; }

    public DomainException(string code, string message) : base(message) => Code = code;

    public static DomainException NotFound(string what) => new("not_found", $"{what} not found.");
    public static DomainException Invalid(string what) => new("invalid", $"{what} is invalid.");
}