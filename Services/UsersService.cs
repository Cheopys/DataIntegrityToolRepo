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

        // C

        public static RegisterUserResponse RegisterUser(RegisterUserRequest request)
        {
            RegisterUserResponse response = new()
            {
                errorCode = ErrorCodes.errorNone
            };

            using (DataContext context = new())
            {
                // check if any available seats

                Customers customer = context.Customers.Where(cu => cu.Id.Equals(request.CustomerId)).FirstOrDefault();
                bool      CanAdd   = true;

                if (customer.LicenseType.Equals(LicenseTypes.licenseTypeSubscription))
                {
                    Int32 seatsMax  = context.Subscriptions.Where(cu => cu.Equals(customer.Id)).Select(cu => cu.SeatCount).FirstOrDefault();
                    Int32 seatsUsed = context.Users        .Where(us => us.Equals(customer.Id)).Count();

                    CanAdd = (seatsUsed < seatsMax);
                }

                if (CanAdd)
                {
                    Users user = new Users()
                    {
                        CustomerId               = request.CustomerId,
                        Name                     = request.Name,
                        Email                    = request.Email,
                        PasswordHash             = ServerCryptographyService.SHA256(request.Password),
                        AesKey                   = Convert.FromHexString(request.AesKey),
                        Tools                    = request.Tools,
                        DateAdded                = DateTime.UtcNow
                    };

                    context.Users.Add(user);

                    context.SaveChanges();

                    response.UserId = user.Id;
				}

				context.Dispose();
            }

			return response;
        }

        // R

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

		// U

		public static void UpdateUser(UpdateUserRequest request)
		{
			using (DataContext context = new())
			{
				Users? user = context.Users.Where(cu => cu.Id.Equals(request.UserId)).FirstOrDefault();

                if (request.Tools != null)
                {
                    user.Tools = request.Tools;
                }

                if (request.Name != null)
                {
                    user.Name = request.Name;
                }

                if (request.Email != null)
                {
                    user.Email = request.Email;
                }

                if (request.Password != null)
                {
                    user.PasswordHash = ServerCryptographyService.SHA256(request.Password);
                }

				context.SaveChanges();

				context.Dispose();
			}
		}

        // D

		public static void  DeleteUser(Int32 UserId)
		{
			using (DataContext context = new())
			{
				Users?        user     = context.Users  .Where(us => us.Id.Equals(UserId)).FirstOrDefault();
				List<Session> sessions = context.Session.Where(s => s.UserId.Equals(UserId)).ToList();
				List<LicenseInterval> licensesInterval = context.LicenseInterval.Where(li => li.UserId.Equals(UserId)).ToList();
				List<LicenseMetered> licensesetered = context.LicenseMetered.Where(lm => lm.CustomerId.Equals(UserId)).ToList();

				context.Remove(user);
				context.RemoveRange(sessions);
				context.RemoveRange(licensesetered);
				context.RemoveRange(licensesInterval);

				context.SaveChanges();
				context.Dispose();
			}
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

        // change password

        public static ErrorCodes ChangePasswordAsk(Int32 UserId)
        {
            ErrorCodes errorCode = ErrorCodes.errorNone;

            using (DataContext context = new())
            {
                Users? user = context.Users.Where(us => us.Id.Equals(UserId)).FirstOrDefault();

                if (user != null)
                {
                    user.ChangePasswordToken = (new Random()).Next() % 1000000;

                    context.SaveChanges();  
                }
                else
                {
                    errorCode = ErrorCodes.errorInvalidUser;
                }

                context.Dispose();
            }

            return errorCode;
		}

        public static ErrorCodes ChangePasswordAnswer(ChangePasswordRequest request)
        {
			ErrorCodes errorCode = ErrorCodes.errorNone;

			using (DataContext context = new())
			{
				Users? user = context.Users.Where(us => us.Id.Equals(request.UserId)).FirstOrDefault();

				if (user != null)
				{
                    if (user.ChangePasswordToken.Equals(request.Token))
                    {
                        user.PasswordHash        = ServerCryptographyService.SHA256(request.PasswordNew);
                        user.ChangePasswordToken = 0;
                    }
                    else
                    {
                        errorCode = ErrorCodes.errorWrongToken;
                    }
				}
				else
				{
					errorCode = ErrorCodes.errorInvalidUser;
				}

				context.Dispose();
			}

			return errorCode;
		}
	}
}

