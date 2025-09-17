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

		// retrieve a lost AES key; this is only for emergencies where fetched data must be decrypted, otherwise log in again and create a new AES

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
		public async Task<ChangePasswordAskResponse> ChangePasswordAsk(ChangePasswordAskRequest request)
		{
			ChangePasswordAskResponse response = new()
			{
				LoginType = request.LoginType,
				ErrorCode = ErrorCodes.errorNone,
				Email	  = request.Email,
			};

			using (DataContext context = new())
			{
				switch (request.LoginType)
				{
					case LoginType.typeUser:
						Users? user = context.Users.Where(us => us.Email.Equals(request.Email)).FirstOrDefault();
						if (user != null)
						{
							response.PrimaryKey = user.Id;
						}
						else
						{
							response.ErrorCode = ErrorCodes.errorInvalidUserId;
						}
						break;

					case LoginType.typeCustomer:
						Customers? customer = context.Customers.Where(cus => cus.Email.Equals(request.Email)).FirstOrDefault();
						if (customer != null)
						{
							response.PrimaryKey = customer.Id;
						}
						else
						{
							response.ErrorCode = ErrorCodes.errorInvalidCustomerId;
						}
						break;

					case LoginType.typeAdministrator:
						Administrators? administrator = context.Administrators.Where(ad => ad.Email.Equals(request.Email)).FirstOrDefault();
						if (administrator != null)
						{
							response.PrimaryKey = administrator.Id;
						}
						else
						{
							response.ErrorCode = ErrorCodes.errorInvalidAdministratorId;
						}
						break;

					default:
						response.ErrorCode = ErrorCodes.errorInvalidLoginType;
						break;
				}

				context.SaveChanges();
				context.Dispose();		
			}

			if (response.ErrorCode == ErrorCodes.errorNone)
			{
				do
				{
					response.ChangePasswordToken = (new Random()).Next() % 1000000;
				}
				while (response.ChangePasswordToken.ToString().Length < 6);
			}

			return response;
		}

		[HttpPost, Route("ChangePasswordAnswer")]
		/*
		public ErrorCodes ChangePasswordAnswer([FromBody] EncryptionWrapperDITString wrapperString)
		{
			ChangePasswordRequest? request;
			ServerCryptographyService.DecodeAndDecryptRequest<ChangePasswordRequest>(wrapperString.ToBinaryVersion(), out request);
*/
		public ErrorCodes ChangePasswordAnswer(LoginType logintype, //2
											   Int32	 PrimaryKey,    // 135
											   Int32	 Token,         // 797478
											   string    PasswordNewHASHED)
		{ 
			return UsersService.ChangePasswordAnswer(logintype,
													 PrimaryKey,
													 Token,
													 PasswordNewHASHED);
		}
	}
}


