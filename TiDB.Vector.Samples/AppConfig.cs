using dotenv.net;

namespace TiDB.Vector.Samples
{
    internal static class AppConfig
    {
        private static bool _loaded;
        private static string? _openAiApiKey;
        private static string? _tidbConnString;

        public static void Load()
        {
            if (_loaded) return;
            DotEnv.Load();
            _openAiApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            _tidbConnString = Environment.GetEnvironmentVariable("TIDB_CONN_STRING");
            _loaded = true;
        }

        public static string OpenAIApiKey
        {
            get
            {
                Load();
                return _openAiApiKey ?? throw new InvalidOperationException("OPENAI_API_KEY not found in environment/.env");
            }
        }

        public static string TiDBConnectionString
        {
            get
            {
                Load();
                return _tidbConnString ?? throw new InvalidOperationException("TIDB_CONN_STRING not found in environment/.env");
            }
        }
    }
}


