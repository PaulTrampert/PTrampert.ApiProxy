var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApiProxy(builder.Configuration.GetSection("ApiProxy"));

var app = builder.Build();


app.UseRouting();
app.UseStaticFiles();

app.UseApiProxy();

app.MapFallbackToFile("index.html");

app.Run();
