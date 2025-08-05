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
using DataIntegrityTool.Shared;

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

                Int32 SeatsMax  = context.Customers.Where(cu => cu.Id.Equals(request.CustomerId)).FirstOrDefault().SeatsMax;
				Int32 seatsUsed = context.Users    .Where(us => us.CustomerId.Equals(request.CustomerId)).Count();

                if (seatsUsed < SeatsMax)
				{
                    Users user = new Users()
                    {
                        CustomerId               = request.CustomerId,
                        NameFirst                = request.NameFirst,
						NameLast				 = request.NameLast,
						Email					 = request.Email,
                        PasswordHash             = ServerCryptographyService.SHA256(request.Password),
                        AesKey                   = Convert.FromHexString(request.AesKey),
                        Tools                    = request.Tools,
                        DateAdded                = DateTime.UtcNow
                    };

                    context.Users.Add(user);

                    context.SaveChanges();

                    response.UserId = user.Id;
				}
				else
				{
					response.errorCode = ErrorCodes.errorNoSeats;
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

		public static string UpdateUser(UpdateUserRequest request)
		{
			string ret = $"changes applied to user {request.UserId}";

			using (DataContext context = new())
			{
				Users? user = context.Users.Where(cu => cu.Id.Equals(request.UserId)).FirstOrDefault();

				if (user != null)
				{
					if (request.Tools != null)
					{
						user.Tools = request.Tools;
					}

					if (request.NameFirst != null)
					{
						user.NameFirst = request.NameFirst;
					}

					if (request.NameLast != null)
					{
						user.NameLast = request.NameLast;
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
				}
				else
				{
					ret = $"User {request.UserId} not found";
				}
					
				context.Dispose();
			}

			return ret;
		}

        // D

		public static string  DeleteUser(Int32 CustomerId, 
									     Int32 UserId)
		{
			string ret = $"User {UserId} deleted";
			using (DataContext context = new())
			{
				Users?        user     = context.Users  .Where(us => us.Id.Equals(UserId)).FirstOrDefault();
				List<Session> sessions = context.Session.Where(s => s.UserId.Equals(UserId)).ToList();
				List<LicenseMetered> licensesetered = context.LicenseMetered.Where(lm => lm.CustomerId.Equals(UserId)).ToList();

				if (user.CustomerId == CustomerId)
				{
					context.Remove(user);
					context.RemoveRange(sessions);
					context.RemoveRange(licensesetered);

					context.SaveChanges();
				}
				else
				{
					ret = $"User {UserId} does not belong to customer {CustomerId}";
				}
					
				context.Dispose();
			}

			return ret;
		}

		public static async Task<List<Users>> GetUsersForCustomer(Int32 CustomerId)
		{
			List<Users> Users;

			using (DataContext context = new())
			{
				Users = await context.Users.Where(us => us.CustomerId.Equals(CustomerId))
										   .OrderBy(c => c.NameLast)
                                           .ToListAsync();

				await context.DisposeAsync();
			}

			return Users;
		}

        // change password

        public static ChangePasswordAskResponse ChangePasswordAsk(EncryptionWrapperDITString wrapperString)
        {
			ChangePasswordAskResponse response = new ChangePasswordAskResponse()
			{
				ErrorCode = ErrorCodes.errorNone
			};
            using (DataContext context = new())
            {
				if (wrapperString.type == LoginType.typeUser)
				{
					Users? user = context.Users.Where(us => us.Id.Equals(wrapperString.primaryKey)).FirstOrDefault();

					if (user != null)
					{
						response.Namelast			 = user.NameLast;
						response.NameFirst			 = user.NameFirst;
						response.Email				 = user.Email;
						response.ChangePasswordToken = (new Random()).Next() % 1000000;
						response.PrimaryKey			 = wrapperString.primaryKey;
						response.LoginType = wrapperString.type;

						user.ChangePasswordToken = response.ChangePasswordToken;
						context.SaveChanges();
					}
					else
					{
						response.ErrorCode = ErrorCodes.errorInvalidUser;
					}
				}
				else if (wrapperString.type == LoginType.typeCustomer)
				{
					Customers? customer = context.Customers.Where(us => us.Id.Equals(wrapperString.primaryKey)).FirstOrDefault();

					if (customer != null)
					{
						response.Namelast			 = customer.NameLast;
						response.NameFirst			 = customer.NameFirst;
						response.Email				 = customer.Email;
						response.ChangePasswordToken = (new Random()).Next() % 1000000;
						response.PrimaryKey			 = wrapperString.primaryKey;
						response.LoginType			 = wrapperString.type;

						customer.ChangePasswordToken = response.ChangePasswordToken;
						context.SaveChanges();
					}
					else
					{
						response.ErrorCode = ErrorCodes.errorInvalidCustomerId;
					}
				}

				if (response.ErrorCode == ErrorCodes.errorNone)
				{
					context.SaveChanges();
				}

				context.Dispose();
            }

            return response;
		}

        public static ErrorCodes ChangePasswordAnswer(LoginType loginType,
													  Int32     primaryKey,
													  Int32     token,
													  string    passwordNew)
        {
			ErrorCodes errorCode = ErrorCodes.errorNone;

			using (DataContext context = new())
			{
				if (loginType == LoginType.typeUser)
				{
					Users? user = context.Users.Where(us => us.Id.Equals(primaryKey)).FirstOrDefault();

					if (user != null)
					{
						if (user.ChangePasswordToken.Equals(token))
						{
							user.PasswordHash        = ServerCryptographyService.SHA256(passwordNew);
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
				}
				else if (loginType == LoginType.typeCustomer)
				{
					Customers? customer = context.Customers.Where(us => us.Id.Equals(primaryKey)).FirstOrDefault();

					if (customer != null)
					{
						if (customer.ChangePasswordToken.Equals(token))
						{
							customer.PasswordHash = ServerCryptographyService.SHA256(passwordNew);
						}
						else
						{
							errorCode = ErrorCodes.errorWrongToken;
						}

						customer.ChangePasswordToken = 0;
					}
					else
					{
						errorCode = ErrorCodes.errorInvalidCustomerId;
					}
				}

				if (errorCode == ErrorCodes.errorNone)
				{
					context.SaveChanges();
				}
				context.Dispose();
			}

			return errorCode;
		}
	}
}

