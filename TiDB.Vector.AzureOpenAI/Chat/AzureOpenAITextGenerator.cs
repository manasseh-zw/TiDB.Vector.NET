using System.ClientModel;
using Azure.AI.OpenAI;
using OpenAI.Chat;
using TiDB.Vector.Abstractions;

namespace TiDB.Vector.AzureOpenAI.Chat;

public class AzureOpenAITextGenerator : ITextGenerator
{
    private readonly AzureOpenAIClient _client;
    private readonly ChatClient _chatClient;

    public AzureOpenAITextGenerator(AzureOpenAIConfig config)
    {
        config.Validate();
        _client = config.Auth switch
        {
            AuthType.APIKey => new AzureOpenAIClient(
                new Uri(config.Endpoint),
                new ApiKeyCredential(config.ApiKey)
            ),
            _ => throw new NotSupportedException(
                $"Authentication type '{config.Auth}' is not supported."
            ),
        };

        _chatClient = _client.GetChatClient(config.DeploymentName);
    }

    public async Task<string> CompleteAsync(
        string system,
        IReadOnlyList<(string role, string content)> messages,
        CancellationToken cancellationToken = default
    )
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

        var completion = await _chatClient
            .CompleteChatAsync(chatMessages, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        return completion.Value.Content[0].Text;
    }
}
