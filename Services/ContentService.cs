using DataIntegrityTool.Db;
using DataIntegrityTool.Schema;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.CodeAnalysis.Operations;
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

                AuthorizedToolsUser? authorizedtoolsuser = context.AuthorizedToolsUser.Where(atu => atu.UserId  .Equals(request.UserId)
				                                                                                 && atu.tooltype.Equals(request.tooltype))
							  														   .FirstOrDefault();

				if (authorizedtoolsuser != null)
				{
					if (request.Licensetype == LicenseTypes.licenseTypeMetered)
					{
						LicenseMetered? metered = context.LicenseMetered.Where(lm => lm.CustomerId.Equals(request.UserId)).FirstOrDefault();

						if (metered != null
						&&  metered.count > 0)
						{
							metered.count--;

							OK = true;
						}

                        await context.SaveChangesAsync();
                    } // end metered
                    else
					{
						LicenseInterval? interval	= context.LicenseInterval.Where (li => li.UserId.Equals(request.UserId)).FirstOrDefault();
						Int32 remainingSeconds		= context.Customers      .Where(cu => cu.Id.Equals(customerId)).Select(cu => cu.LicensingIntervalSeconds).FirstOrDefault();
						Int32 minimumInterval		= context.ToolParameters .Select(tp => tp.minimumInterval).FirstOrDefault();

						if (interval != null
						&&  interval.Seconds > minimumInterval
						&&  interval.Seconds < remainingSeconds)
						{
							response.ReaminingSeconds = remainingSeconds;
							OK = true;
						}
					} // end interval

					if (OK)
					{
                        Session session = new()
                        {
                            UserId		= request.UserId,
                            Licensetype = request.Licensetype,
							ToolType    = request.tooltype,
                            timeBegin	= DateTime.UtcNow
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
                Session?  session  = context.Session  .Where(se => se.Id.Equals(sessionId))			.FirstOrDefault();
				Customers customer = context.Customers.Where(cu => cu.Id.Equals(session.CustomerId)).FirstOrDefault();
				Users     user     = context.Users    .Where(us => us.Id.Equals(session.UserId))    .FirstOrDefault();

                session.timeEnd = DateTime.UtcNow;

                if (session?.Licensetype == LicenseTypes.licenseTypeInterval)
				{
                    TimeSpan duration = session.timeEnd.Subtract(session.timeBegin);

					customer.LicensingIntervalSeconds -= (Int32) duration.TotalSeconds;

					if (customer.UserLicensingPool)
					{
                        user.LicensingIntervalSeconds -= (Int32) duration.TotalSeconds;
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
	} // end class
} // end namespace
