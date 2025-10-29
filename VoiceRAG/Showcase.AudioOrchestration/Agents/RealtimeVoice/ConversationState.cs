//#pragma warning disable OPENAI002

//using Microsoft.Extensions.Logging;
//using OpenAI.RealtimeConversation;
//using Showcase.AudioOrchestration;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace Showcase.AI.Voice.Agents.RealtimeVoice;


//public class ConversationState
//{
//    // Index of the current step in the process.
//    public int CurrentStepIndex { get; set; } = 0;
//    // Arbitrary data gathered during the conversation.
//    public Dictionary<string, object> Data { get; } = [];
//}

//public abstract class ProcessStep<TState>
//{
//    public string StepName { get; }
//    protected ProcessStep(string stepName) => StepName = stepName;

//    public string SystemMessage { get; set; } = string.Empty;

//    // Execute the step. Implementations should send a prompt,
//    // wait for user input, update state, etc.
//    public abstract Task ExecuteAsync(TState state, ConversationParticipant participant, CancellationToken cancellationToken);
//}

//public class GreetingStep : ProcessStep<ConversationState>
//{
//    public GreetingStep() : base("Greeting") { }

//    public override async Task ExecuteAsync(ConversationState state, ConversationParticipant participant, CancellationToken cancellationToken)
//    {
//        // Send a greeting message.
//        var msg = new RealtimeMessageEvent(["# "], ConversationMessageRole.System.ToString());
//        await participant.SendAsync(msg, cancellationToken);

//        // Wait for the user’s transcript input.
//        var userResponse = await WaitForTranscriptAsync(participant, cancellationToken);
//        // Save the user’s intent into state.
//        state.Data["UserIntent"] = userResponse;
//    }

//    // Waits for the next transcript from the user.
//    private Task<string> WaitForTranscriptAsync(ConversationParticipant participant, CancellationToken cancellationToken)
//    {
//        if (participant is IInboundEventProvider provider)
//        {
//            return provider.WaitForInboundTranscriptAsync(cancellationToken);
//        }
//        throw new NotSupportedException("Participant does not support inbound event waiting.");
//    }
//}
//public class AskAccountNumberStep : ProcessStep<ConversationState>
//{
//    public AskAccountNumberStep() : base("AskAccountNumber") { }

//    //public override async Task ExecuteAsync(ConversationState state, ConversationParticipant participant, CancellationToken cancellationToken)
//    //{
//    //    var prompt = new RealtimeMessageEvent("Please provide your account number.");
//    //    await participant.SendAsync(prompt, cancellationToken);

//    //    var accountNumber = await WaitForTranscriptAsync(participant, cancellationToken);
//    //    state.Data["AccountNumber"] = accountNumber;
//    //}

//    private Task<string> WaitForTranscriptAsync(ConversationParticipant participant, CancellationToken cancellationToken)
//    {
//        if (participant is IInboundEventProvider provider)
//        {
//            return provider.WaitForInboundTranscriptAsync(cancellationToken);
//        }
//        throw new NotSupportedException("Participant does not support inbound event waiting.");
//    }
//}
//// Orchestrates the multi-step process.
//// (This is our “actor” for the conversation process; its internal state is persistent.)
////public class ConversationProcess: ProcessStep<ConversationState>
////{
////    private readonly List<ProcessStep<ConversationState>> _steps;
////    public ConversationState State { get; } = new ConversationState();

////    public ConversationProcess(List<ProcessStep<ConversationState>> steps) => _steps = steps;

////    // Execute each step sequentially until the process is complete.
////    public async Task RunAsync(ConversationParticipant participant, CancellationToken cancellationToken)
////    {
////        while (State.CurrentStepIndex < _steps.Count && !cancellationToken.IsCancellationRequested)
////        {
////            var currentStep = _steps[State.CurrentStepIndex];
////            await currentStep.ExecuteAsync(State, participant, cancellationToken);
////            State.CurrentStepIndex++;
////        }
////        // Optionally, send a final message upon process completion.
////        var goodbye = new RealtimeMessageEvent("Thank you for using our service. Goodbye!");
////        await participant.SendAsync(goodbye, cancellationToken);
////    }
////}

//// Defines a contract for ConversationParticipants that can wait for inbound transcript events.
//public interface IInboundEventProvider
//{
//    // Blocks until a transcript event is available and returns its text.
//    Task<string> WaitForInboundTranscriptAsync(CancellationToken cancellationToken);
//}