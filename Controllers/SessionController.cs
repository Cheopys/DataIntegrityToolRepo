using System;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using DataIntegrityTool.Schema;
using DataIntegrityTool.Shared;
using DataIntegrityTool.Services;
using NuGet.Common;
using System.Threading.Tasks;
using Amazon.S3.Model.Internal.MarshallTransformations;

namespace DataIntegrityTool.Controllers
{
	[ApiController]
	[Route("[controller]")]

	public class SessionController : ControllerBase
	{
		[HttpGet, Route("Login")]
		public LoginResponse Login(string Email,
								   string PasswordHash,
								   LoginType loginType = LoginType.typeCustomer)		
		{
			Program.loginType = loginType;
			return SessionService.Login(Email, PasswordHash, loginType);
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
		[Produces("application/json")]
		public async Task<string> EndSession(Int32 sessionId)
		{
			List<EndSessionResponse> transitions = await SessionService.EndSession(sessionId);

			return JsonSerializer.Serialize(transitions);
		}

		[HttpPut, Route("SessionTransition")]
		[Produces("application/json")]
		public void SessionTransition(Int32			SessionId,
									  Int16			Frame,
									  Int16			Layer,
									  ErrorCodes Error = ErrorCodes.errorNone)
		{
			SessionService.SessionTransition(SessionId, Frame, Layer, Error);
		}
    }
}
