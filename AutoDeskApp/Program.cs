using AutoDeskApp.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
if (string.IsNullOrEmpty(builder.Configuration["APS_CLIENT_ID"]) || string.IsNullOrEmpty(builder.Configuration["APS_CLIENT_SECRET"]) || string.IsNullOrEmpty(builder.Configuration["APS_CALLBACK_URL"]))
{
    throw new ApplicationException("Missing required environment variables APS_CLIENT_ID, APS_CLIENT_SECRET, or APS_CALLBACK_URL.");
}
builder.Services.AddSingleton<APS>(new APS(builder.Configuration["APS_CLIENT_ID"]!, builder.Configuration["APS_CLIENT_SECRET"]!, builder.Configuration["APS_CALLBACK_URL"]!));
var app = builder.Build();


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseRouting();
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
});

app.Run();
