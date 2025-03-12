#pragma warning disable OPENAI002

using OpenAI.RealtimeConversation;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Showcase.AudioOrchestration;

public interface IConversationStore
{
    Task AppendConversationHistoryAsync(string callId, ConversationItem conversationItem);
    Task<IList<TranscriptItem>> GetConversationHistoryAsync(string callId);
}

public record TranscriptItem(string Role, string Content);

//public class RedisConversationStore : IConversationStore
//{
//    private readonly IDatabase _redis;
//    public RedisConversationStore(IConnectionMultiplexer redisConnection)
//    {
//        _redis = redisConnection.GetDatabase();
//    }
//    public async Task AppendConversationHistoryAsync(string callId, ConversationItem conversationItem)
//    {

//        await _redis.ListRightPushAsync($"transcripts:{callId}", $"user:{text}");
//    }

//    public async Task<IList<TranscriptItem>> GetConversationHistoryAsync(string callId)
//    {
//        var entries = await _redis.ListRangeAsync($"transcripts:{callId}", 0, -1);
//        var history = new List<TranscriptItem>();
//        foreach (var entry in entries)
//        {
//            var val = entry.ToString();
//            int sep = val.IndexOf(':');
//            if (sep > 0)
//            {
//                string role = val.Substring(0, sep);
//                string content = val.Substring(sep + 1);
//                history.Add(new TranscriptItem(role, content));
//            }
//        }
//        return history;
//    }
//}
