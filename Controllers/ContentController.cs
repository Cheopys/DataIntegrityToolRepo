using System;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using DataIntegrityTool.Schema;
using DataIntegrityTool.Services;
using NuGet.Common;
using System.Threading.Tasks;

namespace DataIntegrityTool.Controllers
{
	[ApiController]
	[Route("[controller]")]

	public class ContentController : ControllerBase
	{
		[HttpPut, Route("BeginSession")]
		public async Task<bool> BeginSession([FromBody] Session session)
		{
			return await ContentService.BeginSession(session);
		}

		[HttpPut, Route("EndSession")]
		public async Task<bool> EndSession(Int32 sessionId0)
		{
			return await ContentService.EndSession();
		}
	}
}
