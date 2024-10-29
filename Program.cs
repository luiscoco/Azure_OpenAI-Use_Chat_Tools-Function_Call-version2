using Azure;
using Azure.AI.OpenAI;
using OpenAI.Chat;
using System.Text.Json;

var endpoint = new Uri("https://luiscocoaiservice.openai.azure.com/");
var credentials = new AzureKeyCredential("");
var deploymentName = "gpt-4o";

var openAIClient = new AzureOpenAIClient(endpoint, credentials);
var chatClient = openAIClient.GetChatClient(deploymentName);

static string GetAge()
{
    // Call the location API here.
    return "Luis Coco is 50 years old";
}

static string GetLocation()
{
    // Call the location API here.
    return "Luis Coco is in Madrid, Spain";
}

ChatTool getAgeTool = ChatTool.CreateFunctionTool(
    functionName: nameof(GetAge),
    functionDescription: "Provides Luis Coco age"
);

ChatTool getLocationTool = ChatTool.CreateFunctionTool(
    functionName: nameof(GetLocation),
    functionDescription: "Provides Luis Coco location"
);

ChatCompletionOptions options = new()
{
    Tools = { getAgeTool, getLocationTool },
};

List<ChatMessage> conversationMessages =
[
    new UserChatMessage("Hello"),
    new UserChatMessage("I am Luis Coco, What information do you know about me?"),
];

ChatCompletion completion = chatClient.CompleteChat(conversationMessages,options);

if (completion.FinishReason == ChatFinishReason.ToolCalls)
{
    // Add a new assistant message to the conversation history that includes the tool calls
    conversationMessages.Add(new AssistantChatMessage(completion));

    foreach (ChatToolCall toolCall in completion.ToolCalls)
    {
        conversationMessages.Add(new ToolChatMessage(toolCall.Id, GetToolCallContent(toolCall)));
    }

    // Now make a new request with all the messages thus far, including the original
    ChatCompletion updatedCompletion = chatClient.CompleteChat(conversationMessages);

    Console.WriteLine($"{updatedCompletion.Role}: {updatedCompletion.Content[0].Text}");
}

// Purely for convenience and clarity, this standalone local method handles tool call responses.
string GetToolCallContent(ChatToolCall toolCall)
{
    if (toolCall.FunctionName == getAgeTool.FunctionName)
    {
        // Validate arguments before using them; it's not always guaranteed to be valid JSON!
        try
        {
            return GetAge();
        }
        catch (JsonException)
        {
            // Handle the JsonException (bad arguments) here
        }
    }
    else if (toolCall.FunctionName == getLocationTool.FunctionName)
    {
        try
        {
            return GetLocation();
        }
        catch (JsonException)
        {
            // Handle the JsonException (bad arguments) here
        }
    }

    // Handle unexpected tool calls
    throw new NotImplementedException();
}
