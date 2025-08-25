using Amazon.Runtime.Internal;
using DataIntegrityTool.Db;
using DataIntegrityTool.Schema;
using DataIntegrityTool.Services;
using DataIntegrityTool.Shared;
using Microsoft.AspNetCore.Identity;
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
using Swashbuckle.AspNetCore.Annotations;
using NSwag.Annotations;

namespace DataIntegrityTool.Controllers
{
	[ApiController]
	[Route("[controller]")]

	/// <summary>
	/// Application Controller
	/// </summary>
	public class ApplicationController : Controller
	{

		[HttpGet, Route("LoginRolesForEmail")]
		public List<LoginType> LoginRolesForEmail(string Email)
		{
			return ApplicationService.LoginRolesForEmail(Email);
		}

		private static byte[] ExtractInterleavedKey(string keyInterleaved)
		{
			string hexOriginal = String.Empty;

			for (int i = 2; i < keyInterleaved.Length; i += 4)
			{
				hexOriginal += keyInterleaved.Substring(i, 2);
			}

			return Convert.FromHexString(hexOriginal);
		}

		// <snippet_WebLogin>
		/// <summary>
		/// Login for web site
		/// </summary>
		[HttpPost, Route("WebLogin")]
		public LoginResponse WebLogin(string requestB64,
									  string keyInterleaved,
									  string hexIV)
		{
			LoginResponse response = new();

			byte[] key = ExtractInterleavedKey(keyInterleaved);
			Aes aes = ServerCryptographyService.CreateAes();
			aes.Key = key;
			aes.IV  = Convert.FromHexString(hexIV);

			if (aes.Key.Length == 32)
			{
				WebLoginRequest request;
				ServerCryptographyService.DecodeAndDecryptLoginRequest(aes, requestB64, out request);

				response = ApplicationService.WebLogin(request.Email, ServerCryptographyService.SHA256(request.Password), request.LoginType);

				response.errorcode = ServerCryptographyService.SetAesKey(request.LoginType, response.Identifier, key);
			}
			else
			{
				response.errorcode = ErrorCodes.errorBadKeySize;
			}
				
			return response;
		}
		// </snippet_WebLogin>

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

				context.Administrators.AddAsync(administrator);

				context.SaveChangesAsync();

				response.AdministratorId = administrator.Id;

				context.DisposeAsync();
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


