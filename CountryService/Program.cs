#nullable disable

using System.Reflection;
using System.Text;
using AwsS3.Services;
using CountryService.Constants;
using CountryService.Data;
using CountryService.Extensions;
using CountryService.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Prometheus;
using Serilog;
using Serilog.Exceptions;
using Serilog.Sinks.Elasticsearch;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<AppDbContext>(opt => opt.UseSqlServer(builder.Configuration.GetConnectionString("CountryConn")));
builder.Services.AddScoped<ICountryRepo, CountryRepo>();
builder.Services.AddScoped<ICityRepo, CityRepo>();
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
builder.Services.AddScoped<ICacheService, CacheService>();
builder.Services.AddScoped<IStorageService, StorageService>();


// Authentication
builder.Services.AddAuthentication(options => 
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(jwt => 
{
    var key = Encoding.ASCII.GetBytes(builder.Configuration.GetSection(ConnStringKeys.Const.CONFIG_JWT_SECRET).Value);
    jwt.SaveToken = true;
    jwt.TokenValidationParameters = new TokenValidationParameters()
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false,
        RequireExpirationTime = false,
        ValidateLifetime = true
    };
});
builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddEntityFrameworkStores<AppDbContext>();

ConfigureLogs();
builder.Host.UseSerilog();

builder.Services.AddElasticSearch(builder.Configuration);

builder.Services.AddControllersWithViews();

// CSS isolation for MVC would not work if RazorRuntimeCompilation is enabled
// builder.Services.AddRazorPages().AddRazorRuntimeCompilation();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseHttpMetrics();
app.UseAuthentication();
app.UseAuthorization();

app.MapMetrics();
app.MapControllers();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

PrepDb.PrepPopulation(app);

app.Run();


# region helper
void ConfigureLogs()
{
    // Get the environment which the application is running on
    var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

    // Get the configuration
    var configuration = new ConfigurationBuilder().AddJsonFile(
        "appsettings.Development.json", 
        optional: false, 
        reloadOnChange: true
    ).Build();

    Log.Logger = new LoggerConfiguration()
        .Enrich.FromLogContext()
        .Enrich.WithExceptionDetails() // add details exception
        .WriteTo.Debug()
        .WriteTo.Console()
        .WriteTo.Elasticsearch(ConfigureELS(configuration, env))
        .CreateLogger();
}

ElasticsearchSinkOptions ConfigureELS(IConfigurationRoot configuration, string env)
{
    return new ElasticsearchSinkOptions(new Uri(configuration["ELKConfiguration:Uri"]))
    {
        AutoRegisterTemplate = true,
        IndexFormat = $"{Assembly.GetExecutingAssembly().GetName().Name.ToLower()}-{env.ToLower().Replace(".","-")}-{DateTime.UtcNow:yyyy-MM}"
    };
}
# endregion
