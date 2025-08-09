namespace TiDB.Vector.Options
{
    public sealed record UpsertOptions
    {
        public bool UseChunking { get; init; } = false;
        public bool Overwrite { get; init; } = true;
    }
}


