using Amazon.Runtime.Internal;
using DataIntegrityTool.Db;
using DataIntegrityTool.Schema;
using DataIntegrityTool.Services;
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
using System.Security.Cryptography;

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

		[HttpGet, Route("LoginRolesForEmail")]
		public List<LoginType> LoginRolesForEmail(string Email)
		{
			return ApplicationService.LoginRolesForEmail(Email);
		}

		[HttpPost, Route("WebLogin")]
		public static LoginResponse WebLogin(string		Email,
											 string		PasswordHash,
											 LoginType	loginType)
		{
			return ApplicationService.WebLogin(Email, PasswordHash, loginType);
		}

		[HttpGet, Route("RecoverAESKey")]
		[Produces("application/json")]
		public async Task<RecoverAESKeyResponse> RecoverAESKey(RecoverAESKeyRequest request)
		{
			RecoverAESKeyResponse response = new()
			{
				ErrorCode = ErrorCodes.errorNone
			};

			using (DataContext context = new())
			{
				Aes aesCaller   = ServerCryptographyService.GetAesKey(request.wrapperCaller);
				Aes aesRecovery = ServerCryptographyService.GetAesKey(request.wrapperRecovery);

				response.AesIVCaller   = aesCaller.IV;
				response.AesKeyRecover = await ServerCryptographyService.EncrypytAES(aesCaller, Convert.ToHexString(aesRecovery.Key));

				context.Dispose();
			}

			return response;
		}
	}
}


