using dotenv.net;

namespace TiDB.Vector.Samples
{
    internal static class AppConfig
    {
        private static bool _loaded;
        private static string? _openAiApiKey;
        private static string? _tidbConnString;
        private static string? _azureOpenAIApiKey;
        private static string? _azureOpenAIEndpoint;

        public static string EmbeddingModel = "text-embedding-3-small";
        public static string CompletionModel = "gpt-4.1";

        public static void Load()
        {
            if (_loaded)
                return;
            DotEnv.Load();
            _openAiApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            _tidbConnString = Environment.GetEnvironmentVariable("TIDB_CONN_STRING");
            _azureOpenAIApiKey = Environment.GetEnvironmentVariable("AZURE_AI_APIKEY");
            _azureOpenAIEndpoint = Environment.GetEnvironmentVariable("AZURE_AI_ENDPOINT");
            _loaded = true;
        }

        public static string OpenAIApiKey
        {
            get
            {
                Load();
                return _openAiApiKey
                    ?? throw new InvalidOperationException(
                        "OPENAI_API_KEY not found in environment/.env"
                    );
            }
        }

        public static string TiDBConnectionString
        {
            get
            {
                Load();
                return _tidbConnString
                    ?? throw new InvalidOperationException(
                        "TIDB_CONN_STRING not found in environment/.env"
                    );
            }
        }

        public static string AzureOpenAIApiKey
        {
            get
            {
                Load();
                return _azureOpenAIApiKey
                    ?? throw new InvalidOperationException(
                        "AZURE_AI_APIKEY not found in environment/.env"
                    );
            }
        }

        public static string AzureOpenAIEndpoint
        {
            get
            {
                Load();
                return _azureOpenAIEndpoint
                    ?? throw new InvalidOperationException(
                        "AZURE_AI_ENDPOINT not found in environment/.env"
                    );
            }
        }
    }
}
