//using Microsoft.Extensions.Logging;
//using OpenAI.RealtimeConversation;
//using System;
//using System.Buffers;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Channels;
//using System.Threading.Tasks;

//namespace Showcase.AudioOrchestration;


//#pragma warning disable OPENAI002

//public class ConversationAgent : IConversationAgent, IDisposable
//{
//    private RealtimeConversationSession _session;
//    private readonly Channel<AiAgentCommand> _commandChannel;
//    private readonly ILogger<ConversationAgent> _logger;
//    private readonly ArrayPool<byte> _bufferPool = ArrayPool<byte>.Shared;

//    public ConversationAgent(RealtimeConversationSession session, ILogger<ConversationAgent> logger)
//    {
//        _session = session;
//        _logger = logger;
//        _commandChannel = Channel.CreateUnbounded<AiAgentCommand>();

//        // Start background command processing.
//        _ = ProcessInboundFeedbackAsync(CancellationToken.None);
//    }

//    public async Task StartConversation()
//    {
//        // Start the conversation session.
//        await _session.StartAsync();
//    }

//    /// <summary>
//    /// For inbound audio from the user, forward the PCM data to the OpenAI session.
//    /// </summary>
//    public async Task ProcessInboundAudioAsync(AudioFrame frame, CancellationToken cancellationToken)
//    {
//        try
//        {
//            using var audioStream = new MemoryStream([.. frame.Buffer]);
//            await _session.SendInputAudioAsync(audioStream, cancellationToken);
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "Error sending audio to OpenAI session.");
//        }
//    }

//    /// <summary>
//    /// Outbound audio processing (if any additional processing is needed).
//    /// </summary>
//    public async Task ProcessConversationUpdateAsync(ConversationUpdate update, CancellationToken cancellationToken)
//    {
//        // For this example, we assume the outbound audio is produced by _session.
//        await Task.CompletedTask;
//    }

//    /// <summary>
//    /// Enqueue a command for processing.
//    /// </summary>
//    public async Task ProcessCommandAsync(AiAgentCommand command, CancellationToken cancellationToken)
//    {
//        await _commandChannel.Writer.WriteAsync(command, cancellationToken);
//    }

//    /// <summary>
//    /// Background loop to process incoming commands.
//    /// </summary>
//    private async Task ProcessInboundFeedbackAsync(CancellationToken cancellationToken)
//    {
//        await foreach (var command in _commandChannel.Reader.ReadAllAsync(cancellationToken))
//        {
//            switch (command)
//            {
//                case OverrideInstructionCommand oic:
//                    _logger.LogInformation("Processing override instruction: {Instruction}", oic.Instruction);
//                    await _session.AddItemAsync(ConversationItem.CreateAssistantMessage([ConversationContentPart.CreateInputTextPart(oic.Instruction)]), cancellationToken);
//                    break;
//                default:
//                    _logger.LogWarning("Unknown command received.");
//                    break;
//            }
//        }
//    }

//    public void Dispose()
//    {
//        _commandChannel.Writer.Complete();
//        _session.Dispose();
//    }
//}
