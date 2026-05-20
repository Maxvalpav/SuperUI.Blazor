using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using SuperUI;
using SuperUI.Components;
using SuperUI.Demo;
using SuperUI.Demo.Components;


var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Register Blazor components as Web Components (Custom Elements)
builder.RootComponents.RegisterCustomElement<SgMfeWidget>("sg-mfe-widget");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddSuperUI();

// Register mock permission service for demo

await builder.Build().RunAsync();
