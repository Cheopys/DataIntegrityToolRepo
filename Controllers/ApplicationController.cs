using Amazon.Runtime.Internal;
using DataIntegrityTool.Db;
using DataIntegrityTool.Schema;
using DataIntegrityTool.Services;
using DataIntegrityTool.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DataIntegrityTool.Services;
using System.Collections.Generic;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using System.Xml.Schema;
using Microsoft.AspNetCore.Mvc;


namespace DataIntegrityTool.Controllers
{
	[ApiController]
	[Route("[controller]")]

	public class ApplicationController : Controller
	{

		[HttpGet, Route("DownloadTool")]
		public async Task<byte[]> DownloadTool()
		{
			return await  S3Service.GetTool();
		}

		[HttpPost, Route("WebLogin")]
		[Produces("application/json")]
		public async Task<LoginResponse> WebLogin([FromBody] LoginRequest request)
		{
			return ApplicationService.WebLogin(request.Email, 
											   request.PasswordHash, 
											   request.loginType);
		}
	}
}


