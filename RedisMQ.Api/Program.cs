using FreeRedis;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.OpenApi.Models;
using Yarkool.RedisMQ;
using Yarkool.SwaggerUI;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

var cli = new RedisClient("127.0.0.1:6379,password=,defaultDatabase=3");
builder.Services.AddRedisMQ(cli, config =>
{
    config.UseErrorQueue = true;
    config.RedisPrefix = "RedisMQ:";
    config.RegisterConsumerService = true;
    config.RepublishNonAckTimeOutMessage = true;
    // config.Serializer = new RedisMQ.Api.NewtonsoftJsonSerializer();
});

#region Swagger
builder.Services.AddSwaggerGen(options =>
{
    options.AddServer(new OpenApiServer());

    options.CustomOperationIds(apiDesc =>
    {
        var controllerAction = apiDesc.ActionDescriptor as ControllerActionDescriptor;
        return $"{controllerAction?.ControllerName}-{controllerAction?.ActionName}";
    });

    // Set the comments path for the Swagger JSON and UI.
    foreach (var file in Directory.GetFiles(AppContext.BaseDirectory, "*.xml"))
    {
        options.IncludeXmlComments(file);
    }
});
#endregion

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();
app.UseYarkoolSwaggerUI(c => { c.SwaggerEndpoint("/v1/swagger.json", "V1 Docs"); });

app.Run();