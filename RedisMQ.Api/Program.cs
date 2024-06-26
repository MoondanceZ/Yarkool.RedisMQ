using FreeRedis;
using Yarkool.RedisMQ;

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

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();