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

        public static RegisterUserResponse RegisterUser(RegisterUserRequest request)
        {
            RegisterUserResponse response = new()
            {
                errorCode = ErrorCodes.errorNone
            };

            if (request.AesKey.Length != 32)
            { 
                using (DataContext context = new())
                {
                    Users user = new Users()
                    {
                        CustomerId               = request.CustomerId,
                        Name                     = request.Name,
                        Email                    = request.Email,
                        PasswordHash             = request.PasswordHash,
//                      LicensingIntervalSeconds = request.LicensingIntervalSeconds,
                        LicensingMeteredCount    = request.LicensingMeteredCount,
                        AesKey                   = Convert.FromHexString(request.AesKey),
                        Tools                    = request.Tools,
                        DateAdded                = DateTime.UtcNow
                    };

                    context.Users.Add(user);

                    context.SaveChanges();

                    response.UserId = user.Id;

                    context.Dispose();
                }
			}
            else
			{
                response.errorCode = ErrorCodes.errorBadKeySize;
                    ;
			}

			return response;
        }


        public static void DeleteUser(Int32 UserId)
        {
            using (DataContext context = new())
            {
                Users?        user     = context.Users.Where(us => us.Id.Equals(UserId)).FirstOrDefault();
                List<Session> sessions = context.Session.Where(s => s.UserId.Equals(UserId)).ToList();
                List<LicenseInterval> licensesInterval = context.LicenseInterval.Where(li => li.UserId.Equals(UserId)).ToList();
                List<LicenseMetered>  licensesetered   = context.LicenseMetered .Where(lm => lm.CustomerId.Equals(UserId)).ToList();

                context.Remove      (user);
                context.RemoveRange(sessions);
                context.RemoveRange(licensesetered);
                context.RemoveRange(licensesInterval);

                context.SaveChanges();
                context.Dispose();
            }
        }

		public static Users GetUser(Int32 UserId)
		{
			Users? user = null;

			using (DataContext context = new())
			{
				user = context.Users.Where(cu => cu.Id.Equals(UserId)).FirstOrDefault();

				context.Dispose();
			}

			return user;
		}

		public static async Task<List<Users>> GetUsers(Int32 CustomerId)
		{
			List<Users> Users;

			using (DataContext context = new())
			{
				Users = context.Users.Where(us => us.CustomerId.Equals(CustomerId))
                                     .OrderBy(c => c.Name)
                                     .ToList();

				await context.DisposeAsync();
			}

			return Users;
		}
	}
}
