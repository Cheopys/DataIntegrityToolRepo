using System.Runtime.InteropServices;
using DataIntegrityTool.Db;
using DataIntegrityTool.Schema;
using DataIntegrityTool.Shared;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.EntityFrameworkCore.Internal;
//using NLog;

namespace DataIntegrityTool.Services
{
	public static class SessionService
	{
		public static NLog.Logger logger;
		static SessionService()
		{
			var config = new NLog.Config.LoggingConfiguration();

			// Targets where to log to: File and Console
			var logconsole = new NLog.Targets.ConsoleTarget("logconsole");

			// Rules for mapping loggers to targets            
			config.AddRule(NLog.LogLevel.Info, NLog.LogLevel.Fatal, logconsole);

			// Apply config           
			NLog.LogManager.Configuration = config;
			logger = NLog.LogManager.GetCurrentClassLogger();
		}

		public static LoginResponse Login(string Email,
										  string PasswordHash)
		{
			LoginResponse response = new()
			{
				loginType = LoginType.typeUser,
				errorcode = ErrorCodes.errorNone
			};

			using (DataContext context = new())
			{
				Users? user = context.Users.Where(us => us.Email.ToLower().Equals(Email.ToLower())).FirstOrDefault();

				if (user != null)
				{
					if (user.PasswordHash.Equals(PasswordHash))
					{
						response.PrimaryKey = user.Id;
					}
					else
					{
						response.errorcode = ErrorCodes.errorInvalidPassword;
					}
				}
				else
				{
					response.errorcode = ErrorCodes.errorInvalidUserId;
				}
			}
			
			return response;
		}

		public static async Task<BeginSessionResponse> BeginSession(BeginSessionRequest request)
		{
			bool OK = false;

			BeginSessionResponse response = new();

			using (DataContext context = new())
			{
				Users?	       user			= context.Users	       .Where(us => us.Id.Equals(request.UserId)).FirstOrDefault();
				Customers?     customer		= context.Customers	   .Where(cu => cu.Id.Equals(user.CustomerId)).FirstOrDefault();
				CustomerSubscriptions? subscription = context.CustomerSubscriptions.Where(su => su.CustomerId.Equals(user.CustomerId)).FirstOrDefault();

				// subscription begins with first use

				if (user != null)
				{
					logger.Info($"userId = {user.Id}");

					if (customer.Tools.Contains(request.Tooltype))
					{
						if (subscription.ExpirationDate > DateTime.UtcNow)
						{
							OK = true; 
						}
						else
						{
							response.Error = ErrorCodes.errorNoLicense;
						}
					}
					else
					{
						response.Error = ErrorCodes.errorToolNotAuthorized;
					}

					if (OK)
					{
						Session session = new()
						{
							UserId		= request.UserId,
							ToolType	= request.Tooltype,
							TimeBegin	= DateTime.UtcNow,
							TimeEnd		= DateTime.MaxValue,
							CustomerId  = user.CustomerId
						};

						context.Session.Add(session);

						await context.SaveChangesAsync();

						response.SessionId = session.Id;
					}
				}
				else
				{
					response.Error = ErrorCodes.errorInvalidUserId;
				}

				await context.DisposeAsync();
			}

			return response;
		}

		public static async Task<List<EndSessionResponse>> EndSession(Int32 sessionId)
		{
			using (DataContext context = new())
			{
				Session? session	= context.Session  .Where(se => se.Id.Equals(sessionId))         .FirstOrDefault();
				Customers? customer = context.Customers.Where(cu => cu.Id.Equals(session.CustomerId)).FirstOrDefault();
				Users? user		    = context.Users    .Where(us => us.Id.Equals(session.UserId))    .FirstOrDefault();

				session.TimeEnd = DateTime.UtcNow;

				context.SaveChanges();
				context.Dispose();
			}

			List<EndSessionResponse> response = new();

			return response;
		}
    } // end class
} // end namespace
