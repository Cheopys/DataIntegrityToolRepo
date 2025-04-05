using Microsoft.EntityFrameworkCore;
using DataIntegrityTool.Db;
using DataIntegrityTool.Schema;
using DataIntegrityTool.Services;
using NuGet.Versioning;
using Amazon.Runtime.Internal;
using Npgsql.EntityFrameworkCore.PostgreSQL.Query.Expressions.Internal;
using Humanizer;
using System.Net;
using NLog;
using NLog.LayoutRenderers;
using System.Text.Json;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Drawing;
using NuGet.Packaging;
using System.Globalization;

namespace DataIntegrityTool.Services
{
	public static class CustomersService
	{
		static Logger logger;
		static CustomersService()
		{
			var config = new NLog.Config.LoggingConfiguration();

			// Targets where to log to: File and Console
			var logconsole = new NLog.Targets.ConsoleTarget("logconsole");

			// Rules for mapping loggers to targets            
			config.AddRule(NLog.LogLevel.Info, NLog.LogLevel.Fatal, logconsole);

			// Apply config           
			NLog.LogManager.Configuration = config;
			logger = LogManager.GetCurrentClassLogger();
		}

		public async static Task<string> AddCustomer(Customers customer)
		{
			using (DataContext context = new())
			{
				customer.Unique		= CustomersService.UniqueId();
				customer.DateAdded	= DateTime.UtcNow;

				await context.AddAsync(customer);
				await context.SaveChangesAsync();
				await context.DisposeAsync();

				return customer.Unique;
			}
		}

		public static async Task<List<Customers>> GetCustomers()
		{
			List<Customers> customers = null;

			using (DataContext context = new())
			{
				customers = context.Customers.OrderByDescending(c => c.DateAdded).ToList();

				await context.DisposeAsync();
			}
			return customers;
		}


		private static string UniqueId()
		{
			byte[] unique = new byte[8]; 
			
			new Random().NextBytes(unique);

			return Convert.ToBase64String(unique);
		}
	}
}
