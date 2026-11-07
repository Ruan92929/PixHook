using PixHook.Web.Components;
using PixHook.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpClient<PixHookApiClient>(client =>
{
    var baseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5180";
    client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
