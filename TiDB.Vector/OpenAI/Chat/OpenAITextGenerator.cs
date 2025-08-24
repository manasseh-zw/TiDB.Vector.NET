using OpenAI.Chat;
using TiDB.Vector.Abstractions;
using OpenAI;
using System.ClientModel;

namespace TiDB.Vector.OpenAI.Chat;

public sealed class OpenAITextGenerator : ITextGenerator
{
    private readonly ChatClient _client;

    public OpenAITextGenerator(OpenAIConfig config)
    {
        config.ApiType = ApiType.Chat;
        config.Validate();
        
        var apiKeyCredential = new ApiKeyCredential(config.ApiKey);

        // Create client with custom endpoint if provided, otherwise use OpenAI's default
        if (!string.IsNullOrEmpty(config.Endpoint))
        {
            var options = new OpenAIClientOptions
            {
                Endpoint = new Uri(config.Endpoint)
            };
            var openAIClient = new OpenAIClient(apiKeyCredential, options);
            _client = openAIClient.GetChatClient(config.Model);
        }
        else
        {
            var openAIClient = new OpenAIClient(apiKeyCredential);
            _client = openAIClient.GetChatClient(config.Model);
        }
    }

    public async Task<string> CompleteAsync(
        string system,
        IReadOnlyList<(string role, string content)> messages,
        CancellationToken cancellationToken = default)
    {
        var chatMessages = new List<ChatMessage>();
        if (!string.IsNullOrWhiteSpace(system))
        {
            chatMessages.Add(new SystemChatMessage(system));
        }

        foreach (var (role, content) in messages)
        {
            if (role == "user") 
                chatMessages.Add(new UserChatMessage(content));
            else if (role == "assistant") 
                chatMessages.Add(new AssistantChatMessage(content));
            else 
                chatMessages.Add(new UserChatMessage(content));
        }

        var completion = await _client.CompleteChatAsync(chatMessages, cancellationToken: cancellationToken).ConfigureAwait(false);
        return completion.Value.Content[0].Text;
    }
}
