namespace TiDB.Vector.OpenAI;

public class OpenAIConfig
{
    public required string ApiKey { get; set; }
    public required string Model { get; set; }
    public int? Dimension { get; set; }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            throw new Exception($"OpenAI: {nameof(ApiKey)} is empty");
        }

        if (string.IsNullOrWhiteSpace(Model))
        {
            throw new Exception($"OpenAI: {nameof(Model)} is empty");
        }

        if (Dimension.HasValue && Dimension.Value < 1)
        {
            throw new Exception($"OpenAI: {nameof(Dimension)} must be at least 1");
        }
    }
}