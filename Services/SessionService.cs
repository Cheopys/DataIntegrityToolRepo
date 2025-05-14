using System.Runtime.InteropServices;
using DataIntegrityTool.Db;
using DataIntegrityTool.Schema;
using DataIntegrityTool.Shared;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
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
								          string PasswordHash,
										  bool   IsAdministrator)
		{
			LoginResponse response = new()
			{
				errorcode = ErrorCodes.errorNone
			};

			using (DataContext context = new())
			{
				// From web site

				if (IsAdministrator)
				{
                    Customers? customer = context.Customers.Where(us => us.Email.ToLower().Equals(Email.ToLower())).FirstOrDefault();

                    if (customer != null)
                    {
                        if (customer.PasswordHash.Equals(PasswordHash))
						{
							response.Identifier = customer.Id;
						}
						else
						{
							response.errorcode = ErrorCodes.errorInvalidPassword;
						}
                    }
                    else
                    {
                        response.errorcode = ErrorCodes.errorInvalidUser;
                    }
                } // end is admin

                // from DIT Tool

                else
				{
					Users? user = context.Users.Where(us => us.Email.ToLower().Equals(Email.ToLower())).FirstOrDefault();

					if (user != null)
					{
						if (user.PasswordHash.Equals(PasswordHash))
						{
							response.Identifier = user.Id;
						}
						else
						{
							response.errorcode = ErrorCodes.errorInvalidPassword;
						}
					}
					else
					{
						response.errorcode = ErrorCodes.errorInvalidUser;
					}
				}
				context.Dispose();
			}

			return response;
		}

		public static async Task<BeginSessionResponse> BeginSession(BeginSessionRequest request)
		{
			bool OK = false;

			BeginSessionResponse response = new();

			using (DataContext context = new())
			{
				{
					Users? user = context.Users.Where(us => us.Id.Equals(request.UserId)).FirstOrDefault();
					
					if (user != null)
					{
						logger.Info($"userId = {user.Id}");

						if (user.Tools.Contains(request.Tooltype))
						{
							if (request.Licensetype.Equals(LicenseTypes.licenseTypeMetered))
							{
								logger.Info("LicenseType.Metered");

								response.RemainingSeconds = null;

								if (user.LicensingMeteredCount > 0)
								{
									logger.Info($"user has {user.LicensingMeteredCount} of license type 0");

									user.LicensingMeteredCount--;

									OK = true;
								}
								else
								{
									response.Error = ErrorCodes.errorNoLicense;
								}

								await context.SaveChangesAsync();
							} // end metered
							else
							{
								logger.Info("LicenseType.Interval");

								response.RemainingMetered = null;

								Int32 remainingSeconds	= context.Users.Where(cu => cu.Id.Equals(request.UserId)).Select(cu => cu.LicensingIntervalSeconds).FirstOrDefault();
								Int32 minimumInterval	= context.ToolParameters.Select(tp => tp.MinimumInterval).FirstOrDefault();

								logger.Info($"Remaining Seconds = {remainingSeconds}");

								if (minimumInterval < remainingSeconds)
								{
									response.RemainingSeconds = remainingSeconds;
									OK = true;
								}
								else
								{
									response.Error = ErrorCodes.errorNoLicense;
								}
							} // end interval
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
								Licensetype = request.Licensetype,
								ToolType	= request.Tooltype,
								TimeBegin	= DateTime.UtcNow
							};

							context.Session.Add(session);

							await context.SaveChangesAsync();

							response.SessionId = session.Id;

							SessionTransition(session.Id,
											  0, 
											  0);

						}
					}
					else
					{
						response.Error = ErrorCodes.errorInvalidUser;
					}

					await context.DisposeAsync();
				}
			}

			return response;
		}

		public static async Task<List<SessionTransition>> EndSession(Int32 sessionId)
		{
			List<SessionTransition> transitions = new();

			using (DataContext context = new())
			{
				Session?	session		= context.Session  .Where(se => se.Id.Equals(sessionId))		 .FirstOrDefault();
				Customers?  customer	= context.Customers.Where(cu => cu.Id.Equals(session.CustomerId)).FirstOrDefault();
				Users?		user		= context.Users    .Where(us => us.Id.Equals(session.UserId))	 .FirstOrDefault();

				session.TimeEnd = DateTime.UtcNow;

				if (session?.Licensetype == LicenseTypes.licenseTypeInterval)
				{
					TimeSpan duration = session.TimeEnd.Subtract(session.TimeBegin);

					user.LicensingIntervalSeconds -= (Int32)duration.TotalSeconds;
				}

				transitions = context.SessionTransition.Where(st => st.SessionId.Equals(sessionId)).OrderBy(st => st.DateTime).ToList();

				context.SaveChanges();
				context.Dispose();
			}

			return transitions;
		}
/*
		private static string CSVPath(Int32 sessionId)
		{
			string path = String.Empty;

            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				path = "%USERPROFILE%\\AppData\\Local";

				if (Directory.Exists($"path\\DataIntegrityTool") == false)
				{
					Directory.CreateDirectory($"path\\DataIntegrityTool");
				}

				path = $"path\\DataIntegrityTool\\DIT{sessionId}.csv";

            }
			else if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
				path = "~/.config";

                if (Directory.Exists($"path/DataIntegrityTool") == false)
                {
                    Directory.CreateDirectory($"path/DataIntegrityTool");
                }
            
				path = $"path/DataIntegrityTool/DIT{sessionId}.csv";
            }

            return path;
        }
*/
		public static void SessionTransition(Int32 sessionId,
											 Int16 Frame,
											 Int16 Layer,
											 ErrorCodes Error = ErrorCodes.errorNone)
		{	
			using (DataContext context = new())
			{
				context.SessionTransition.Add(new SessionTransition()
				{
					SessionId	 = sessionId,
					DateTime     = DateTime.UtcNow,
					FrameOrdinal = Frame,
					LayerOrdinal = Layer,
					ErrorCode	 = Error
				});

				context.SaveChanges();
				context.Dispose();
			}
		}

		public static bool MeteredRemaining(Int32 userId)
		{
			bool any = true;

			using (DataContext context = new())
			{
				any = context.Users.Where(us => us.Id.Equals(userId)).Select(us => us.LicensingMeteredCount).FirstOrDefault() > 0;

				context.Dispose();
			}

			return any;
		}

        public static Int32 IntervalRemaining(Int32 userId)
        {
			Int32 seconds = 0;

            using (DataContext context = new())
            {
                seconds = context.Users.Where(us => us.Id.Equals(userId)) .Select(us => us.LicensingIntervalSeconds).FirstOrDefault();

				context.Dispose();
            }

            return seconds;
        }
    } // end class
} // end namespace
