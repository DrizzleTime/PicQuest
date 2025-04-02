using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using MudBlazor.Services;
using PicQuest;
using PicQuest.Components;
using PicQuest.Services;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMudServices();
var environment = builder.Environment;
Console.WriteLine($"当前环境: {environment.EnvironmentName}");

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
Console.WriteLine($"数据库连接: {connectionString}");

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddDbContextFactory<MyDbContext>(options =>
    options.UseNpgsql(connectionString, o => o.UseVector()));
builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<AiVisionService>();
builder.Services.AddSingleton<EmbeddingService>();
builder.Services.AddSingleton<IPictureService, PictureService>();
var app = builder.Build();
var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads"
});
app.MapControllers();
app.MapOpenApi();
app.MapScalarApiReference();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
app.Run();