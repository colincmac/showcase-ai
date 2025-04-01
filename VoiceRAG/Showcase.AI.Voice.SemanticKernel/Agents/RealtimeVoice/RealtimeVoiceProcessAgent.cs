#pragma warning disable OPENAI002
#pragma warning disable SKEXP0080 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.


using Azure.Communication.CallAutomation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using OpenAI.RealtimeConversation;
using Showcase.AudioOrchestration;
using Showcase.Shared.AIExtensions.Realtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Showcase.AI.Voice.SemanticKernel.Agents.RealtimeVoice;

public class RealtimeVoiceProcessAgent : OpenAIRealtimeAgent
{
    private readonly KernelProcess _process;
    public RealtimeVoiceProcessAgent(RealtimeConversationClient aiClient, RealtimeSessionOptions sessionOptions, ILoggerFactory loggerFactory, string id, string name) : base(aiClient, sessionOptions, loggerFactory, id, name)
    {
        var kernelBuilder = Kernel.CreateBuilder();

        kernelBuilder.Services.AddSingleton(this);

        ProcessBuilder _processBuilder = new(sessionOptions.AgentName ?? nameof(RealtimeVoiceProcessAgent));

        // Define Steps
        var realtimeAgentStep = _processBuilder
            .AddStepFromType<AuthenticationStep>();

        var kickoff = _processBuilder
            .AddStepFromType<KickoffStep>();

        var getName = _processBuilder
            .AddStepFromType<GetName>();

        var handoff = _processBuilder
            .AddStepFromType<HandOffStep, ConversationState>(initialState: new());

        // Setup Step Connections/Edges
        _processBuilder.OnInputEvent(nameof(RealtimeConversationStartedEvent))
            .SendEventTo(new ProcessFunctionTargetBuilder(kickoff));

        kickoff.OnEvent(KickoffStep.OutputEvents.WelcomedUser)
            .SendEventTo(new ProcessFunctionTargetBuilder(getName));

        _process = _processBuilder.Build();
    }

    private sealed class AuthenticationStep : KernelProcessStep
    {

        public static class OutputEvents
        {
            public const string AgentResponse = nameof(AgentResponse);
        }


        [KernelFunction]
        public async ValueTask UpdateDirectiveAsync(KernelProcessStepContext context)
        {
            Console.WriteLine("##### Kickoff ran.");
            await context.EmitEventAsync(new() { Id = OutputEvents.AgentResponse });
        }
    }

    private sealed class UserIntentStep : KernelProcessStep
    {
        public UserIntentStep()
        {

        }
        public static class OutputEvents
        {
            public const string AgentResponse = nameof(AgentResponse);
        }


        [KernelFunction]
        public async ValueTask UpdateDirectiveAsync(KernelProcessStepContext context)
        {
            Console.WriteLine("##### Kickoff ran.");
            await context.EmitEventAsync(new() { Id = OutputEvents.AgentResponse });
        }
    }

    private sealed class KickoffStep : KernelProcessStep
    {
        private readonly string _instructions = """

            """;
        //public KickoffStep(ILoggerFactory loggerFactory)
        //{
        //}
        //public override ValueTask ActivateAsync(KernelProcessStepState state)
        //{
        //    Console.WriteLine($"##### KickoffStep activated with instructions = '{_instructions}'.");
        //    return base.ActivateAsync(state);
        //}
        public static class OutputEvents
        {
            public const string WelcomedUser = nameof(WelcomedUser);
        }


        [KernelFunction]
        public async ValueTask PrintWelcomeMessageAsync(KernelProcessStepContext context)
        {
            Console.WriteLine("##### Kickoff ran.");
            await context.EmitEventAsync(new() { });
            await context.EmitEventAsync(new() { Id = OutputEvents.WelcomedUser, Data = "Get Going" });
        }
    }

    private sealed class GetName : KernelProcessStep
    {
        public static class OutputEvents
        {
            public const string CondimentsAdded = nameof(CondimentsAdded);
        }


        [KernelFunction]
        public async ValueTask GetNameAsync(KernelProcessStepContext context)
        {
            Console.WriteLine("##### Kickoff ran.");
            await context.EmitEventAsync(new() { });
        }
    }

    private sealed class HandOffStep : KernelProcessStep<ConversationState>
    {
        private ConversationState? _state;

        public override ValueTask ActivateAsync(KernelProcessStepState<ConversationState> state)
        {
            this._state = state.State;
            Console.WriteLine($"##### HandOffStep activated with info = '{_state}'.");
            return base.ActivateAsync(state);
        }

        [KernelFunction]
        public async ValueTask PrintWelcomeMessageAsync(KernelProcessStepContext context)
        {
            Console.WriteLine("##### Kickoff ran.");
            await context.EmitEventAsync(new() { });
        }
        public static class OutputEvents
        {
            public const string CondimentsAdded = nameof(CondimentsAdded);
        }
    }


    [JsonDerivedType(typeof(RealtimeEvent), nameof(UserNameUpdatedEvent))]
    private record UserNameUpdatedEvent(string FullName) : RealtimeEvent
    {
        public override string EventType => nameof(UserNameUpdatedEvent);
        public bool IsEmpty => false;
    }

    [JsonDerivedType(typeof(RealtimeEvent), nameof(PhoneNumberUpdatedEvent))]
    private record PhoneNumberUpdatedEvent(string PhoneNumber) : RealtimeEvent
    {
        public override string EventType => nameof(PhoneNumberUpdatedEvent);
        public bool IsEmpty => false;
    }

    [JsonDerivedType(typeof(RealtimeEvent), nameof(AccountNumberUpdatedEvent))]
    private record AccountNumberUpdatedEvent(string AccountNumber) : RealtimeEvent
    {
        public override string EventType => nameof(AccountNumberUpdatedEvent);
        public bool IsEmpty => false;
    }

    [JsonDerivedType(typeof(RealtimeEvent), nameof(DateOfBirthUpdatedEvent))]
    private record DateOfBirthUpdatedEvent(string DateOfBirth) : RealtimeEvent
    {
        public override string EventType => nameof(DateOfBirthUpdatedEvent);
        public bool IsEmpty => false;
    }
}

[JsonDerivedType(typeof(RealtimeEvent), nameof(RealtimeConversationStartedEvent))]
public record RealtimeConversationStartedEvent(): RealtimeEvent
{
    public override string EventType => nameof(RealtimeConversationStartedEvent);
    public bool IsEmpty => false;
}


[DataContract]
public sealed record ConversationState
{
    [DataMember]
    public string? LastMessage { get; set; }

    [DataMember]
    public string? UserName { get; set; }
    [DataMember]
    public DateTime? DateOfBirth { get; set; }

    [DataMember]
    public string? AccountNumber { get; set; }

    [DataMember]
    public string? Email { get; set; }

    [DataMember]
    public string? PhoneNumber { get; set; }
}