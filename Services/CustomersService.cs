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

        public static Int32 RegisterCustomer(string requestEncryptedB64)
        {
            Int32 customerId = 0;

            byte[] requestDecrypted = ServerCryptographyService.DecryptRSA(requestEncryptedB64);

            RegisterCustomerRequest request = JsonSerializer.Deserialize<RegisterCustomerRequest>(requestDecrypted);

            using (DataContext context = new())
            {
				Customers customer = new Customers()
                {
                    Name		 = request.Name,
                    Description  = request.Description,
                    EmailContact = request.EmailContact,
                    Notes		 = request.Notes,
                    aeskey		 = request.aesKey,
                    DateAdded	 = DateTime.UtcNow,
                };

                context.Customers.Add(customer);

                context.SaveChanges();
                context.Dispose();

                customerId = customer.Id;
            }

            return customerId;
        }

        public static async Task<List<Customers>> GetCustomers()
		{
			List<Customers> customers = null;

			using (DataContext context = new())
			{
				customers = context.Customers.OrderBy(c => c.Name).ToList();

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
