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

			if (CustomersService.IsValidEmail(request.Email))
			{ 
				using (DataContext context = new())
				{
					if (context.Users.Where(cu => cu.Email.ToLower().Equals(request.Email.ToLower())).FirstOrDefault() == null)
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
								PhoneNumber				 = request.PhoneNumber == null ? "0" : request.PhoneNumber,
								PasswordHash             = ServerCryptographyService.SHA256(request.Password),
								AesKey                   = Convert.FromHexString(request.AesKey),
								//Tools                    = request.Tools,
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
					}
					else
					{
						response.errorCode = ErrorCodes.errorEmailAlreadyExists;
					}

					context.Dispose();
				}
			}
			else
			{
				response.errorCode = ErrorCodes.errorInvalidEmailFormat;
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
						if (CustomersService.IsValidEmail(request.Email))
						{
							user.Email = request.Email;
						}
						else
						{
							ret = $"Invalid email format {request.Email}";
						}
					}

					if (request.Password != null)
					{
						user.PasswordHash = ServerCryptographyService.SHA256(request.Password);
					}

					if (request.PhoneNumber != null)
					{
						user.PhoneNumber = request.PhoneNumber;
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

        public static ErrorCodes ChangePasswordAnswer(LoginType loginType,
													  Int32     primaryKey,
													  Int32     token,
													  string    passwordNewHash)
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
							user.PasswordHash = passwordNewHash; //'ServerCryptographyService.SHA256(passwordNew);
						}
						else
						{
							errorCode = ErrorCodes.errorWrongToken;
						}

						user.ChangePasswordToken = 0;
					}
					else
					{
						errorCode = ErrorCodes.errorInvalidUserId;
					}
				}
				else if (loginType == LoginType.typeCustomer)
				{
					Customers? customer = context.Customers.Where(us => us.Id.Equals(primaryKey)).FirstOrDefault();

					if (customer != null)
					{
						if (customer.ChangePasswordToken.Equals(token))
						{
							customer.PasswordHash        = passwordNewHash; //'ServerCryptographyService.SHA256(passwordNew);
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
				else if (loginType == LoginType.typeAdministrator)
				{
					Administrators? administrator = context.Administrators.Where(us => us.Id.Equals(primaryKey)).FirstOrDefault();

					if (administrator != null)
					{
						if (administrator.ChangePasswordToken.Equals(token))
						{
							administrator.PasswordHash = passwordNewHash; //'ServerCryptographyService.SHA256(passwordNew);
						}
						else
						{
							errorCode = ErrorCodes.errorWrongToken;
						}

						administrator.ChangePasswordToken = 0;
					}
					else
					{
						errorCode = ErrorCodes.errorInvalidAdministratorId;
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

