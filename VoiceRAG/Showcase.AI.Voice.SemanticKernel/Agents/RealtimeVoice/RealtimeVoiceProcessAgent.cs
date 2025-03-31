//#pragma warning disable OPENAI002
//#pragma warning disable SKEXP0080 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.


//using Microsoft.Extensions.Logging;
//using Microsoft.SemanticKernel;
//using OpenAI.RealtimeConversation;
//using Showcase.AudioOrchestration;
//using Showcase.Shared.AIExtensions.Realtime;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Runtime.Serialization;
//using System.Text;
//using System.Text.Json;
//using System.Threading.Tasks;

//namespace Showcase.AI.Voice.SemanticKernel.Agents.RealtimeVoice;

//public class RealtimeVoiceProcessAgent : OpenAIRealtimeAgent
//{
//    private readonly ProcessBuilder _processBuilder = new("RealtimeVoiceProcessAgent");

//    public RealtimeVoiceProcessAgent(RealtimeConversationClient aiClient, RealtimeSessionOptions sessionOptions, ILoggerFactory loggerFactory, string id, string name) : base(aiClient, sessionOptions, loggerFactory, id, name)
//    {
//        _processBuilder
//            .AddStepFromType<KickoffStep>();

//        _processBuilder.AddStepFromType<GetName>();

//        _processBuilder.AddStepFromType<HandOffStep, ConversationState>(initialState: new());

//    }

//    private sealed class KickoffStep : KernelProcessStep
//    {


//        [KernelFunction]
//        public async ValueTask PrintWelcomeMessageAsync(KernelProcessStepContext context)
//        {
//            Console.WriteLine("##### Kickoff ran.");
//            await context.EmitEventAsync(new() { });
//            await context.EmitEventAsync(new() { Id = CommonEvents.StartARequested, Data = "Get Going" });
//        }
//    }

//    private sealed class GetName : KernelProcessStep
//    {


//        [KernelFunction]
//        public async ValueTask GetNameAsync(KernelProcessStepContext context)
//        {
//            Console.WriteLine("##### Kickoff ran.");
//            await context.EmitEventAsync(new() { Id = CommonEvents.StartARequested, Data = "Get Going" });
//        }
//    }

//    private sealed class HandOffStep : KernelProcessStep<ConversationState>
//    {
//        private ConversationState? _state;

//        public override ValueTask ActivateAsync(KernelProcessStepState<ConversationState> state)
//        {
//            this._state = state.State;
//            Console.WriteLine($"##### HandOffStep activated with info = '{_state}'.");
//            return base.ActivateAsync(state);
//        }

//        [KernelFunction]
//        public async ValueTask PrintWelcomeMessageAsync(KernelProcessStepContext context)
//        {
//            Console.WriteLine("##### Kickoff ran.");
//            await context.EmitEventAsync(new() { Id = CommonEvents.StartARequested, Data = "Get Going" });
//        }
//    }
//}

//[DataContract]
//public sealed record ConversationState
//{
//    [DataMember]
//    public string? LastMessage { get; set; }

//    [DataMember]
//    public string? UserName { get; set; }
//    [DataMember]
//    public DateTime? DateOfBirth { get; set; }

//    [DataMember]
//    public string? AccountNumber { get; set; }

//    [DataMember]
//    public string? Email { get; set; }

//    [DataMember]
//    public string? PhoneNumber { get; set; }
//}