using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OpenAI.Chat;
using TiDB.Vector.Abstractions;
using TiDB.Vector.OpenAI;

namespace TiDB.Vector.OpenAI.Chat
{
    public sealed class OpenAITextGenerator : ITextGenerator
    {
        private readonly ChatClient _client;

        public OpenAITextGenerator(OpenAIConfig config)
        {
            config.Validate();
            _client = new ChatClient(config.Model, config.ApiKey);
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
                if (role == "user") chatMessages.Add(new UserChatMessage(content));
                else if (role == "assistant") chatMessages.Add(new AssistantChatMessage(content));
                else chatMessages.Add(new UserChatMessage(content));
            }

            var completion = await _client.CompleteChatAsync(chatMessages, cancellationToken: cancellationToken).ConfigureAwait(false);
            return completion.Value.Content[0].Text;
        }
    }
}


