using System.Diagnostics.Metrics;
using BlazorComponent;
using Microsoft.AspNetCore.Builder;
using Yarkool.RedisMQ.Dashboard.Client.Pages;
using Yarkool.RedisMQ.Dashboard.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddMasaBlazor();

var app = builder.Build();

var pathPrefix = "/RedisMQ";


app.Map(pathPrefix, subApp =>
{
    //https://learn.microsoft.com/zh-cn/aspnet/core/blazor/host-and-deploy/?view=aspnetcore-8.0&tabs=visual-studio#app-base-path
    //https://learn.microsoft.com/zh-cn/aspnet/core/blazor/host-and-deploy/multiple-hosted-webassembly?view=aspnetcore-7.0&source=recommendations&pivots=port-domain
    //https://learn.microsoft.com/zh-cn/aspnet/core/fundamentals/routing?view=aspnetcore-8.0
    //均可以单独配置
    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        subApp.UseWebAssemblyDebugging();
    }
    else
    {
        subApp.UseExceptionHandler("/Error", createScopeForErrors: true);
    }

    subApp.UsePathBase(pathPrefix);
    subApp.UseRouting();
    subApp.UseStaticFiles();
    subApp.UseBlazorFrameworkFiles();
    subApp.UseAntiforgery();

    subApp.UseEndpoints(endpoints =>
    {
        endpoints.MapBlazorHub(pathPrefix);
        endpoints.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode()
            .AddInteractiveWebAssemblyRenderMode(options => options.PathPrefix = pathPrefix)
            .AddAdditionalAssemblies(typeof(Counter).Assembly);
    });
});

app.Run();
