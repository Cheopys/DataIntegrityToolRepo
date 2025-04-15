using Microsoft.EntityFrameworkCore;
using DataIntegrityTool.Db;
using DataIntegrityTool.Services;
using System.Configuration;
using Microsoft.AspNetCore.HttpOverrides;
using System.Net;
using Microsoft.Extensions.Options;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
/*
public static IHostBuilder CreateHostBuilder(string[] args) =>
	Host.CreateDefaultBuilder(args)
		.ConfigureWebHostDefaults(webBuilder =>
		{
			webBuilder.UseUrls("http://0.0.0.0:5001", "https://0.0.0.0:5001");
			webBuilder.UseStartup<Startup>();
		});
*/
// Add services to the container.

builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


if (builder.Environment.IsDevelopment())
{
	builder.Services.AddHttpsRedirection(options =>
	{
		options.RedirectStatusCode = (int)HttpStatusCode.TemporaryRedirect;
		options.HttpsPort          = 5001;
	});
}
else
{
	builder.Services.AddHttpsRedirection(options =>
	{
		options.RedirectStatusCode = (int)HttpStatusCode.PermanentRedirect;
		options.HttpsPort          = 443;
	});
}

builder.WebHost.ConfigureKestrel(serverOptions =>
{
	int port = 5001;
	string pfxFilePath = "/etc/pki/tls/certs/kestrelCert.pfx";
	// The password you specified when exporting the PFX file using OpenSSL.
	// This would normally be stored in configuration or an environment variable;
	// I've hard-coded it here just to make it easier to see what's going on.
	string pfxPassword = "foxcj431";

	serverOptions.Listen(IPAddress.Any, port, listenOptions =>
	{
		// Configure Kestrel to use a certificate from a local .PFX file for hosting HTTPS
		listenOptions.UseHttps(pfxFilePath, pfxPassword);
	});

	serverOptions.Limits.KeepAliveTimeout = TimeSpan.FromDays(2);
	serverOptions.Limits.MaxRequestBodySize = 100000000;
	serverOptions.AllowSynchronousIO = true;
});

builder.Services.AddDbContext<DataContext>(options => options.UseNpgsql("Host=dataintegritytool.ct6cykcgeval.ca-central-1.rds.amazonaws.com; Port=5432; Database=dataintegritytool; Username=postgres; Password=YD4NKpMxscgQcFsSN8NA6y5"));

WebApplication app = builder.Build();

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}
else
{
	app.UseSwagger();
	app.UseSwaggerUI();
	//app.UseHttpsRedirection();
}
/*
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
	ForwardedHeaders = ForwardedHeaders.XForwardedFor 
					 | ForwardedHeaders.XForwardedProto
});
*/
app.UseAuthorization();

app.MapControllers();

app.Run();
