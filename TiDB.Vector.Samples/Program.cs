using TiDB.Vector.Samples;
using TiDB.Vector.Samples.Samples;
AppConfig.Load();
// await UpsertSample.RunAsync();
await SearchSample.RunAsync();
await AskSample.RunAsync();