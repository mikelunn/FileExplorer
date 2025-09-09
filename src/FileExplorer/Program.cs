using FileExplorer.Files;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddTransient<IFileService, FileService>();
var app = builder.Build();
app.UseHttpsRedirection();
app.MapFileEndpoints();
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapFallbackToFile("index.html");
app.Run();