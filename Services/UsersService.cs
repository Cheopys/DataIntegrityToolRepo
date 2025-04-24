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
	public static class UsersService
	{
		static Logger logger;
		static UsersService()
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

        public static Int32 RegisterUser(string requestEncryptedB64)
        {
            Int32 mfa = new Random().Next();

            while (mfa < 999999)
            {
                mfa = new Random().Next();
            }

            byte[] requestDecrypted = ServerCryptographyService.DecryptRSA(requestEncryptedB64);

            RegisterUserRequest request = JsonSerializer.Deserialize<RegisterUserRequest>(requestDecrypted);

            using (DataContext context = new())
            {
                Users user = new Users()
                {
					CustomerId		= request.CompanyId,
					Email			= request.Email,
					PasswordHash	= request.PasswordHash,
                    Name			= request.Name,
                    aeskey			= request.aeskey,
					Tools			= request.Tools,
                    DateAdded		= DateTime.UtcNow,
                };

                context.Users.Add(user);

                context.SaveChanges();

                foreach (ToolTypes tooltype in request.Tools)
                {
                    context.AuthorizedToolsUser.Add(new AuthorizedToolsUser()
                    {
                        UserId   = user.Id,
                        tooltype = tooltype
                    });
                }

				context.SaveChanges();

                context.UsersAwaitingMFA.Add(new UsersAwaitingMFA()
				{
					Id  = user.Id,
					MFA = mfa
				});

                context.SaveChanges();
                context.Dispose();
          }

			// TBD: MFA
            return mfa;
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
