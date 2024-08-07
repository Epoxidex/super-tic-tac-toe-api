using Serilog;
using Serilog.Events;
using super_tic_tac_toe_api.Services;
using super_tic_tac_toe_api.Services.Interfaces;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Information)
    .WriteTo.Console()
    .Filter.ByExcluding(logEvent =>
        logEvent.Properties.TryGetValue("SourceContext", out var sourceContext) &&
        (sourceContext.ToString().Contains("Microsoft.AspNetCore.Mvc.Infrastructure.ControllerActionInvoker") ||
         sourceContext.ToString().Contains("Microsoft.AspNetCore.Routing.EndpointMiddleware") ||
         sourceContext.ToString().Contains("Microsoft.AspNetCore.Mvc.Infrastructure.ObjectResultExecutor")))
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSingleton<ILobbyService, LobbyService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Host.UseSerilog();

var app = builder.Build();

var lobbyService = app.Services.GetRequiredService<ILobbyService>();
WebSocketHandler.Initialize(lobbyService);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();
app.UseHttpsRedirection();
app.UseAuthorization();

app.UseWebSockets();

app.MapControllers();
app.Map("/ws", async context =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        await WebSocketHandler.Handle(context, webSocket);
    }
    else
    {
        context.Response.StatusCode = 400;
    }
});

app.Run();
