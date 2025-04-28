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

	public class SessionController : ControllerBase
	{
		[HttpGet, Route("Login")]
		public Int32 Login(string Email,
						   string PasswordHash)
		{
			return SessionService.Login(Email, PasswordHash);
		}

		[HttpPut, Route("BeginSession")]
		public async Task<BeginSessionResponse> BeginSession(Int32		  UserId, 
															 LicenseTypes LicenseType, 
															 ToolTypes    ToolType)
		{
			BeginSessionRequest request = new()
			{
				UserId      = UserId,
				Licensetype = LicenseType,
				Tooltype    = ToolType
			};

            return await SessionService.BeginSession(request);
		}

		[HttpPut, Route("EndSession")]
		public async Task<bool> EndSession(Int32 sessionId)
		{
			return await SessionService.EndSession(sessionId);
		}
	}
}
