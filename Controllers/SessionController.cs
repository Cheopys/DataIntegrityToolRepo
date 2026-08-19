using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using DataIntegrityTool.Schema;
using DataIntegrityTool.Services;

namespace DataIntegrityTool.Controllers
{
	[ApiController]
	[Route("[controller]")]

	public class SessionController : ControllerBase
	{
		[HttpGet, Route("Login")]
		public LoginResponse Login(string Email,
								   string PasswordHash)		
		{
			return SessionService.Login(Email, PasswordHash);
		}

		[HttpPut, Route("BeginSession")]
		public async Task<BeginSessionResponse> BeginSession(Int32		  UserId, 
															 ToolTypes    ToolType)
		{
			BeginSessionRequest request = new()
			{
				UserId      = UserId,
				Tooltype    = 0
			};

            return await SessionService.BeginSession(request);
		}

		[HttpPut, Route("EndSession")]
		[Produces("application/json")]
		public async Task<string> EndSession(Int32 sessionId)
		{
			List<EndSessionResponse> transitions = await SessionService.EndSession(sessionId);

			return JsonSerializer.Serialize(transitions);
		}
    }
}
