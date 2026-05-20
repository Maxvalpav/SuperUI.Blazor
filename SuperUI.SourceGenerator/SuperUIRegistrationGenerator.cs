using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Text;

namespace SuperUI.SourceGenerator;

[Generator]
public class SuperUIRegistrationGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context) { }

    public void Execute(GeneratorExecutionContext context)
    {
        // Simple generator to create a registration helper
        var sourceBuilder = new StringBuilder(@"
using Microsoft.Extensions.DependencyInjection;
using SuperUI.Services;
using SuperUI.Components;

namespace SuperUI;

public static class SuperUIServiceExtensions
{
    public static IServiceCollection AddSuperUIAuto(this IServiceCollection services)
    {
        services.AddScoped<SgThemeService>();
        services.AddScoped<SgSmartFormProvider>();
        // Automatically discovered services could be added here
        return services;
    }
}");
        context.AddSource("SuperUIServiceExtensions.g.cs", SourceText.From(sourceBuilder.ToString(), Encoding.UTF8));
    }
}
