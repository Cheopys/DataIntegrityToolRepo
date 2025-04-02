using System.Text.Json;
using System.Net;
using ProxChat.Db;
using ProxChat.Schema;
using ProxChat.SharedObjectTypes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProxChat.Services;
using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using Amazon.Runtime.Internal;
using System.Xml.Linq;

namespace ProxChat.Controllers
{
	[ApiController]
	[Route("[controller]")]
	public class ApplicationController : Controller
	{
		[HttpGet, Route("GetProvisioningData")]
		[Produces("application/json")] // ProvisioningData
		public async Task<string> GetProvisioningData(string localeProposed)
		{
			ProvisioningData data = new();

			string locale = await UsersService.GetMatchingLocale(localeProposed);

			using (DataContext context = new())
			{
				List<string> localesDB = await context.ApplicationText.Select(l => l.locale).Distinct().ToListAsync();
				List<Locales> locales  = await context.Locales.Where(l => localesDB.Contains(l.locale)).ToListAsync();

				data.localesSupported = locales.OrderBy(l => l.nameLocalized)
											   .Select(l => new Locale
												{
													locale        = l.locale,
													nameEnglish   = l.nameEnglish,
													nameLocalized = l.nameLocalized
												})
											   .ToList();
				data.localizedStrings = await context.ApplicationText.Where(at => at.locale == locale)
																	 .Select(at => new LocaleText
																	  {
																		 locale = at.locale,
																		 text   = at.textLocalized,
																		 token  = at.token
																	  })
																	 .OrderBy(at => at.token)
																	 .ToListAsync();
				data.radiiImperial    = await context.ChatRadii
													 .Where  (cr => cr.distanceUnit == DistanceUnits.British)
													 .OrderBy(cr => cr.value)
													 .Select (cr => new ChatRadius
													 {
														 Id			 = cr.Id,
														 description = cr.description,
														 value		 = cr.value
													 })
													 .ToListAsync();
				data.radiiMetric = await context.ChatRadii
											    .Where  (cr => cr.distanceUnit == DistanceUnits.Metric)
												.OrderBy(cr => cr.value)
												.Select (cr => new ChatRadius
												{
													Id			= cr.Id,
													description = cr.description,
													value		= cr.value
												})
												.ToListAsync();

				await context.DisposeAsync();
			}

			return JsonSerializer.Serialize(data);
		}

		[HttpGet, Route("GetImageRejectionData")]
		public async Task<List<ImageRejectionLogResponse>> GetImageRejectionData()
		{
			List<ImageRejectionLogResponse> response = new();

			using (DataContext context = new())
			{
				List<ImageRejectionLog> logs = context.ImageRejectionLog.ToList();

				logs.ForEach(async log =>
				{
					S3Attachment attachment = await S3Service.GetRejectedImage(log.name);

					response.Add(new ImageRejectionLogResponse()
					{
						name			= log.name,
						violationsJSON	= log.violationsJSON,
					});

				});
				await context.DisposeAsync();
			}

			return response;
		}

		[HttpDelete, Route("DeleteRectedImages")]
		public async Task DeleteRectedImages()
		{
			using (DataContext context = new())
			{
				context.RemoveRange(context.ImageRejectionLog.ToList());
				await context.DisposeAsync();
			}

			S3Service.DeleteRejectedImages();
		}
	}
}
