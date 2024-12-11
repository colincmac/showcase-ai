# Showcase of various AI integration patterns (WIP)

## [VoiceRAG](./VoiceRag.md) - Working Demo

**Features**
  - Able to call tools defined with the Microsoft.Extensions.AI AITool abstraction

**Todo**
  - Improve documentation
  - Add tools/plugins
  - Need to add RAG

## [GitHub Copilot Agent](./GitHubCopilotAgent.md) - Working Demo

[GitHub Copilot Extension](https://github.com/features/copilot/extensions) written in .NET, using the Microsoft.Extensions.AI.OpenAI library.

**Features**
  - Created a Factory class to create a Microsoft.Extensions.AI.OpenAI ChatClient, using the AccessToken passed in from GitHub
  - Authorization middleware validates the payload signature
  - Ability to parse "slash commands" (e.g. `@my-copilot /myTool execute specific tool`)

**Todo**
  - Add documentation
  - Add tools/plugins

## Aspire + .Net AI Tools + Python AI Tools - Partially Complete

**Features**
  - Aspire runs both the ASP.NET API and Python API, referencing eachother

**Todo**
  - Partially complete, see Aspire Host and Python Plugins project
  - Add an example AI Gateway that can orchestrate between these two projects

