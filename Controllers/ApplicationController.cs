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
using Microsoft.Extensions.Primitives;
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

		[HttpPut, Route("RegisterAdministratorRSA")]
		[Produces("application/json")]
		public async Task<string> RegisterAdministratorRSA([FromBody] string registerAdminisrtatorB64)
		{
			RegisterAdministratorRequest request = ServerCryptographyService.DecryptRSA<RegisterAdministratorRequest>(registerAdminisrtatorB64);

			RegisterAdministratorResponse response = new()
			{
				errorCode = ErrorCodes.errorNone
			};

			using (DataContext context = new())
			{
				Administrators administrator = new Administrators()
				{
					NameFirst	 = request.NameFirst,
					NameLast	 = request.NameLast,
					Email        = request.Email,
					PasswordHash = ServerCryptographyService.SHA256(request.Password),
					AesKey		 = Convert.FromHexString(request.AesKey),
					DateAdded	 = DateTime.UtcNow
				};

				context.Administrators.Add(administrator);

				context.SaveChanges();

				response.AdministratorId = administrator.Id;

				context.Dispose();
			}

			return JsonSerializer.Serialize(response);
		}

		[HttpDelete, Route("DeleteAdministrator")]
		[Produces("application/json")]
		public void DeleteAdministrator(Int32 AdministratorId)
		{
			using (DataContext context = new())
			{
				Administrators? administrator = context.Administrators.FirstOrDefault(x => x.Id == AdministratorId);

				if (administrator != null)
				{
					context.Administrators.Remove(administrator);

					context.SaveChanges();
				}

				context.Dispose();
			}
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
				type	= LoginType.typeAdministrator,
			};

			return await ServerCryptographyService.EncryptAndEncodeResponse(wrapper, customers);
		}

		[HttpPost, Route("ChangePasswordAsk")]
		public async Task<string> ChangePasswordAsk(string requestRSA)
		{
			string retval = string.Empty;
			ChangePasswordAskRequest request = ServerCryptographyService.DecryptRSA<ChangePasswordAskRequest>(requestRSA);
			EncryptionWrapperDITString wrapperString = new()
			{
				aesIVHex = request.AesIVHex,
				type	 = request.LoginType
			};

			ErrorCodes error = ErrorCodes.errorNone;

			using (DataContext context = new())
			{
				switch (request.LoginType)
				{
					case LoginType.typeUser:
						Users? user = context.Users.Where(us => us.Email.Equals(request.Email)).FirstOrDefault();
						if (user != null)
						{
							wrapperString.primaryKey = user.Id;
						}
						else
						{
							error = ErrorCodes.errorInvalidUserId;
						}
						break;

					case LoginType.typeCustomer:
						Customers? customer = context.Customers.Where(cus => cus.Email.Equals(request.Email)).FirstOrDefault();
						if (customer != null)
						{
							wrapperString.primaryKey = customer.Id;
						}
						else
						{
							error = ErrorCodes.errorInvalidCustomerId;
						}
						break;

					case LoginType.typeAdministrator:
						Administrators? administrator = context.Administrators.Where(ad => ad.Email.Equals(request.Email)).FirstOrDefault();
						if (administrator != null)
						{
							wrapperString.primaryKey = administrator.Id;
						}
						else
						{
							error = ErrorCodes.errorInvalidUserId;
						}
						break;
				}
			}

			ChangePasswordAskResponse response;

			if (error == ErrorCodes.errorNone)
			{
				response = UsersService.ChangePasswordAsk(wrapperString);

				EncryptionWrapperDIT wrapper = new EncryptionWrapperDIT()
				{
					primaryKey	= wrapperString.primaryKey,
					type		= wrapperString.type,
					aesIV		= Convert.FromHexString(wrapperString.aesIVHex)
				};

				retval = await ServerCryptographyService.EncryptAndEncodeResponse(wrapper, response);
			}
			else
			{
				retval = $"error {error} finding Email {request.Email} of type {request.LoginType}";
			}

			return retval;
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


