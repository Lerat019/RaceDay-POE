using RaceDayMVC.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddSingleton<System.Net.CookieContainer>();

builder.Services.AddHttpClient<IApiService, RaceDayMVC.Services.ApiService>()
    .ConfigurePrimaryHttpMessageHandler(sp =>
    {
        var cookieContainer = sp.GetRequiredService<System.Net.CookieContainer>();
        return new System.Net.Http.HttpClientHandler
        {
            CookieContainer = cookieContainer,
            UseCookies = true,
     
            ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
        };
    });

builder.Services.AddHttpClient<IApiService, RaceDayMVC.Services.ApiService>()
    .ConfigurePrimaryHttpMessageHandler(sp =>
    {
        var cookieContainer = sp.GetRequiredService<System.Net.CookieContainer>();
        return new System.Net.Http.HttpClientHandler
        {
            CookieContainer = cookieContainer,
            UseCookies = true
        };
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseSession();
app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
