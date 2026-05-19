using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SuperUI.Components.DocumentExtractor;

namespace SuperUI.Components.DocumentExtractor.Services;

public static class DocumentExtractorServiceCollectionExtensions
{
    /// <summary>
    /// Registers the document extractor pipeline: LLM client, default extractors, default savers,
    /// and the endpoint config store. Safe to call multiple times.
    /// </summary>
    public static IServiceCollection AddSgDocumentExtractor(this IServiceCollection services)
    {
        services.TryAddScoped<SgLlmEndpointConfigStore>();
        services.TryAddScoped<ILlmExtractionClient>(sp =>
        {
            var http = sp.GetService<HttpClient>() ?? new HttpClient();
            return new OpenAiCompatibleLlmExtractionClient(http);
        });

        // Extractors — enumerable so the component can pick by id.
        services.AddScoped<IDocumentExtractor, DocxTextDocumentExtractor>();
        services.AddScoped<IDocumentExtractor, PlainTextDocumentExtractor>();
        services.AddScoped<IDocumentExtractor>(sp => new LlmDocumentExtractor(
            sp.GetRequiredService<ILlmExtractionClient>(),
            () => sp.GetRequiredService<SgLlmEndpointConfigStore>().Current,
            textExtractor: new DocxTextDocumentExtractor()));

        // Savers
        services.AddScoped<IDocumentSaver, DocxDocumentSaver>();
        services.AddScoped<IDocumentSaver, PlainTextDocumentSaver>();
        services.AddScoped<IDocumentSaver, PassthroughDocumentSaver>();

        return services;
    }
}
