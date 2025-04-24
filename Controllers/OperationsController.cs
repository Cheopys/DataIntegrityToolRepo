using System;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using DataIntegrityTool.Schema;
using DataIntegrityTool.Shared;
using DataIntegrityTool.Services;
using NuGet.Common;
using System.Threading.Tasks;

namespace DataIntegrityTool.Controllers
{
	[ApiController]
	[Route("[controller]")]

	public class OperationsController : ControllerBase
	{
		[HttpPut, Route("BeginSession")]
		public async Task<BeginSessionResponse> BeginSession([FromBody] BeginSessionRequest request)
		{
			return await ContentService.BeginSession(request);
		}

		[HttpPut, Route("EndSession")]
		public async Task<bool> EndSession(Int32 sessionId)
		{
			return await ContentService.EndSession(sessionId);
		}
	}
}
