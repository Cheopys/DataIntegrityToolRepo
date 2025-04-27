using DataIntegrityTool.Db;
using DataIntegrityTool.Schema;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.EntityFrameworkCore.Internal;
//using NLog;

namespace DataIntegrityTool.Services
{
	public static class ContentService
	{
		public static NLog.Logger logger;
		static ContentService()
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
		public static async Task<BeginSessionResponse> BeginSession(BeginSessionRequest request)
		{
			bool OK = false;

			BeginSessionResponse response = new();

			using (DataContext context = new())
			{
				Int32 customerId = context.Users.Where(us => us.Id.Equals(request.UserId)).Select(us => us.CustomerId).FirstOrDefault();

				AuthorizedToolsUser? authorizedtoolsuser = context.AuthorizedToolsUser.Where(atu => atu.UserId.Equals(request.UserId)
																								 && atu.tooltype.Equals(request.tooltype))
																						 .FirstOrDefault();

				if (authorizedtoolsuser != null)
				{
					if (request.Licensetype == LicenseTypes.licenseTypeMetered)
					{
						LicenseMetered? metered = context.LicenseMetered.Where(lm => lm.CustomerId.Equals(request.UserId)).FirstOrDefault();

						if (metered != null
						&& metered.Count > 0)
						{
							metered.Count--;

							OK = true;
						}

						await context.SaveChangesAsync();
					} // end metered
					else
					{
						Int32 seconds = ContentService.IntervalRemaining(request.UserId);
						Int32 remainingSeconds = context.Customers.Where(cu => cu.Id.Equals(customerId)).Select(cu => cu.LicensingIntervalSeconds).FirstOrDefault();
						Int32 minimumInterval = context.ToolParameters.Select(tp => tp.MinimumInterval).FirstOrDefault();

						if (seconds > minimumInterval
						&& seconds < remainingSeconds)
						{
							response.ReaminingSeconds = remainingSeconds;
							OK = true;
						}
					} // end interval

					if (OK)
					{
						Session session = new()
						{
							UserId = request.UserId,
							Licensetype = request.Licensetype,
							ToolType = request.tooltype,
							TimeBegin = DateTime.UtcNow
						};

						context.Session.Add(session);

						await context.SaveChangesAsync();

						response.SessionId = session.Id;
					}

					await context.DisposeAsync();
				}
			}

			return response;
		}

		public static async Task<bool> EndSession(Int32 sessionId)
		{
			bool Ok = false;

			using (DataContext context = new())
			{
				Session? session = context.Session.Where(se => se.Id.Equals(sessionId)).FirstOrDefault();
				Customers customer = context.Customers.Where(cu => cu.Id.Equals(session.CustomerId)).FirstOrDefault();
				Users user = context.Users.Where(us => us.Id.Equals(session.UserId)).FirstOrDefault();

				session.TimeEnd = DateTime.UtcNow;

				if (session?.Licensetype == LicenseTypes.licenseTypeInterval)
				{
					TimeSpan duration = session.TimeEnd.Subtract(session.TimeBegin);

					customer.LicensingIntervalSeconds -= (Int32)duration.TotalSeconds;

					if (customer.UserLicensingPool)
					{
						user.LicensingIntervalSeconds -= (Int32)duration.TotalSeconds;
					}
				}
				else
				{
				}

				context.SaveChanges();
				context.Dispose();
			}

			return Ok;
		}

		public static void SessionPing(Int32 sessionId)
		{
			using (DataContext context = new())
			{
				context.SessionPing.Add(new SessionPing()
				{
					SessionId = sessionId,
					DateTime  = DateTime.UtcNow
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
				Users?     user		= context.Users    .Where(us => us.Id.Equals(userId))		  .FirstOrDefault();
				Customers? customer = context.Customers.Where(cu => cu.Id.Equals(user.CustomerId)).FirstOrDefault();

				if (customer.UserLicensingPool)
				{
					any = user.LicensingMeteredCount > 0;
				}
				else
				{
					any = customer.LicensingMeteredCount > 0;
				}
			}

				return any;
		}

        public static Int32 IntervalRemaining(Int32 userId)
        {
			Int32 seconds = 0;

            using (DataContext context = new())
            {
                Users?     user     = context.Users    .Where(us => us.Id.Equals(userId))         .FirstOrDefault();
                Customers? customer = context.Customers.Where(cu => cu.Id.Equals(user.CustomerId)).FirstOrDefault();

                if (customer.UserLicensingPool)
                {
					seconds = user.LicensingIntervalSeconds;
                }
                else
                {
                    seconds = customer.LicensingIntervalSeconds;
                }

				context.Dispose();
            }

            return seconds;
        }
    } // end class
} // end namespace
