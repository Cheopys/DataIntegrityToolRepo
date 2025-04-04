using System;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using DataIntegrityTool.Schema;
using DataIntegrityTool.Services;
using DataIntegrityTool.Db;

namespace DataIntegrityTool.Controllers
{
	[ApiController]
	[Route("[controller]")]
		
	// for populating the choices on the radius slider

	public class ContentController : ControllerBase
    {
		[HttpPut, Route("BeginSession")]
		public async Task<bool> BeginSession(Session session)
		{
			bool OK = false;

			session.timeBegin = DateTime.UtcNow;

			using (DataContext context = new())
			{
				context.Session.Add(session);

				await context.SaveChangesAsync();
				await context.DisposeAsync();

				OK = true;
			}

			return OK;
		}
	}
}
