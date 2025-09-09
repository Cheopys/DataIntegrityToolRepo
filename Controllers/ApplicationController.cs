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

		// <snippet_WebLogin>
		/// <summary>
		/// Login for web site
		/// </summary>
		[HttpPost, Route("WebLoginRSA")]
		[Produces("application/json")]
		public LoginResponse WebLoginRSA(string requestB64)
		{
			LoginResponse   response = new();

			WebLoginRequest request = ServerCryptographyService.DecryptRSA<WebLoginRequest>(requestB64);

			response = ApplicationService.WebLogin(request.Email, ServerCryptographyService.SHA256(request.Password), request.LoginType);

			if (response.errorcode == ErrorCodes.errorNone)
			{
				response.errorcode = ServerCryptographyService.SetAesKey(request.LoginType, response.PrimaryKey, Convert.FromHexString(request.AesKeyHex));
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
		[Produces("application/json")]
		public async Task<string> ChangePasswordAsk([FromBody] string requestRSA)
		{
			ChangePasswordAskRequest request = ServerCryptographyService.DecryptRSA<ChangePasswordAskRequest>(requestRSA);
			EncryptionWrapperDITString wrapperString = new()
			{
				aesIVHex = request.AesIVHex,
				type	 = request.LoginType
			};

			byte[] aeskey = Convert.FromHexString(request.AesKeyHex);

			ErrorCodes error = ErrorCodes.errorNone;

			using (DataContext context = new())
			{
				switch (request.LoginType)
				{
					case LoginType.typeUser:
						Users? user = context.Users.Where(us => us.Email.Equals(request.Email)).FirstOrDefault();
						if (user != null)
						{
							user.AesKey = aeskey;
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
							customer.AesKey = aeskey;
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
							administrator.AesKey	 = aeskey;
							wrapperString.primaryKey = administrator.Id;
						}
						else
						{
							error = ErrorCodes.errorInvalidUserId;
						}
						break;

					default:
						error = ErrorCodes.errorInvalidLoginType;
						break;
				}

				context.SaveChanges();
				context.Dispose();		
			}

			ChangePasswordAskResponse response;

			EncryptionWrapperDIT wrapper = new EncryptionWrapperDIT()
			{
				primaryKey	= wrapperString.primaryKey,
				type		= wrapperString.type,
				aesIV		= Convert.FromHexString(wrapperString.aesIVHex)
			};

			if (error == ErrorCodes.errorNone)
			{
				response = UsersService.ChangePasswordAsk(wrapperString);

				response.ErrorCode = error;
			}
			else
			{
				response = new ChangePasswordAskResponse()
				{
					PrimaryKey			= 0,
					LoginType			= request.LoginType,
					Email				= request.Email,
					NameFirst			= String.Empty,
					Namelast			= String.Empty,
					ChangePasswordToken = 0,
					ErrorCode			= error
				};
			}

			return await ServerCryptographyService.EncryptAndEncodeResponse(wrapper, response);
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


