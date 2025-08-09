using System;
using System.Collections.Generic;

namespace TiDB.Vector.Models
{
    public sealed record Answer
    {
        public string Text { get; init; } = string.Empty;
        public IReadOnlyList<Citation> Sources { get; init; } = Array.Empty<Citation>();
    }
}


