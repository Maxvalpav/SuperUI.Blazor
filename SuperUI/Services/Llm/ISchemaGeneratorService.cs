using SuperUI.Components.Llm.Models;

namespace SuperUI.Services.Llm;

public interface ISchemaGeneratorService
{
    DocumentSchema ParseOpenAiResponse(string jsonResponse);
}
