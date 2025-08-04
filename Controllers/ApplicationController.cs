using Amazon.Runtime.Internal;
using DataIntegrityTool.Db;
using DataIntegrityTool.Schema;
using DataIntegrityTool.Services;
using DataIntegrityTool.Services;
using DataIntegrityTool.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Net;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using System.Xml.Schema;

namespace DataIntegrityTool.Controllers
{
	[ApiController]
	[Route("[controller]")]

	public class ApplicationController : Controller
	{

		[HttpGet, Route("DownloadTool")]
		public async Task<byte[]> DownloadTool()
		{
			return await S3Service.GetTool();
		}

		[HttpGet, Route("LoginRolesForEmail")]
		public List<LoginType> LoginRolesForEmail(string Email)
		{
			return ApplicationService.LoginRolesForEmail(Email);
		}

		[HttpPost, Route("WebLogin")]
		public static LoginResponse WebLogin(string Email,
											 string PasswordHash,
											 LoginType loginType)
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
				Aes aesCaller = ServerCryptographyService.GetAesKey(request.wrapperCaller);
				Aes aesRecovery = ServerCryptographyService.GetAesKey(request.wrapperRecovery);

				response.AesIVCaller = aesCaller.IV;
				response.AesKeyRecover = await ServerCryptographyService.EncrypytAES(aesCaller, Convert.ToHexString(aesRecovery.Key));

				context.Dispose();
			}

			return response;
		}

		[HttpGet, Route("GetCustomers")]
		[Produces("application/json")]
		public async Task<string> GetCustomers(Int32 AdministratorID, string AesIVHex)
		{
			List<Customers> customers = await CustomersService.GetCustomers();

			EncryptionWrapperDIT wrapper = new()
			{
				aesIV		= Convert.FromHexString(AesIVHex),
				primaryKey	= AdministratorID,
				type		= LoginType.typeDIT,
			};

			return await ServerCryptographyService.EncryptAndEncodeResponse(wrapper, customers);
		}

		[HttpPost, Route("ChangePasswordAsk")]
		public async Task<string> ChangePasswordAsk(EncryptionWrapperDITString wrapperString)
		{
			ChangePasswordAskResponse response = UsersService.ChangePasswordAsk(wrapperString);

			return await ServerCryptographyService.EncryptAndEncodeResponse(wrapperString.ToBinaryVersion(), response);
		}

		[HttpPost, Route("ChangePasswordAnswer")]
		public ErrorCodes ChangePasswordAnswer([FromBody] EncryptionWrapperDITString wrapperString)
		{
			ChangePasswordRequest? request;
			ServerCryptographyService.DecodeAndDecryptRequest<ChangePasswordRequest>(wrapperString.ToBinaryVersion(), out request);

			return UsersService.ChangePasswordAnswer(request.LoginType,
													 request.PrimaryKey,
													 request.Token,
													 request.PasswordNew);
		}


	}
}


