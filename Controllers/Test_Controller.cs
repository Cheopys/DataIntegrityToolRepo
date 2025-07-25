using DataIntegrityTool.Db;
using DataIntegrityTool.Schema;
using DataIntegrityTool.Services;
using DataIntegrityTool.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NLog;
using System.Collections.Generic;
using System.Net;
using System.Reflection.Metadata;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using static System.Net.Mime.MediaTypeNames;

namespace DataIntegrityTool.Controllers
{
	[ApiController]
	[Route("[controller]")]

	public class Test_Controller : ControllerBase
	{
		private static string EncryptRSA(byte[] cleartext)
		{
			byte[] publicKey = ServerCryptographyService.GetServerRSAPublicKey();

			RSACryptoServiceProvider csp = new RSACryptoServiceProvider(4096);

			int cbRead;
			csp.ImportRSAPublicKey(publicKey, out cbRead);

			byte[] textEncrypted = csp.Encrypt(cleartext, false); //PKCS7 padding

			return Convert.ToBase64String(textEncrypted);
		}

		[HttpPut, Route("RegisterCustomer_Client")]
		public string RegisterCustomer_Client()
		{
			System.Security.Cryptography.Aes aeskey = ServerCryptographyService.CreateAes();

			RegisterCustomerRequest request = new()
			{
				AesKey		 = Convert.ToHexString(aeskey.Key),
				Email		 = "testcust@example.com",
				Company      = "testcompany@example.com",
				NameFirst	 = "Test",
				NameLast     = "Customer",
				Notes	     = "each time this is run it will increment the primary key",
				Password	 = "DIT",
				Tools		 = new List<ToolTypes>
				{
					ToolTypes.tooltypeVFX,
					ToolTypes.tooltypeDI,
					ToolTypes.tooltypeArchive,
					ToolTypes.tooltypeProduction
				},
				InitialUser = false,
				SubscriptionId = 20
			};

			string requestSerialized = JsonSerializer.Serialize(request);

			byte[] requestEncoded = Encoding.UTF8.GetBytes(requestSerialized);
			return EncryptRSA(requestEncoded);
		}

		[HttpPut, Route("RegisterCustomer_Raw")]
		public async Task<RegisterCustomerResponse> RegisterCustomer_Raw(RegisterCustomerRequest request)
		{
			System.Security.Cryptography.Aes aes = ServerCryptographyService.CreateAes();

			request.AesKey = Convert.ToHexString(aes.Key);

			return CustomersService.RegisterCustomer(request);
		}

		[HttpPut, Route("RegisterCustomer")]
		[Produces("application/json")]
		public RegisterCustomerResponse RegisterCustomer(RegisterCustomerRequest request)
		{
			System.Security.Cryptography.Aes aes = ServerCryptographyService.CreateAes();

			request.AesKey = Convert.ToHexString(aes.Key);

			string requestSerialized = JsonSerializer.Serialize(request);

			byte[] requestEncoded   = Encoding.UTF8.GetBytes(requestSerialized);
			string requestEncryptedB64 = ServerCryptographyService.EncryptRSA(requestEncoded);

			// begin API logic

			RegisterCustomerRequest requestOut = ServerCryptographyService.DecryptRSA<RegisterCustomerRequest>(requestEncryptedB64);

			return CustomersService.RegisterCustomer(requestOut);
		}

		[HttpGet, Route("GetCustomers_Raw")]
		[Produces("application/json")]
		public async Task<string> GetCustomers_Raw()
		{
			List<Customers> customers = await CustomersService.GetCustomers();

			return JsonSerializer.Serialize(customers);
		}

		[HttpGet, Route("GetUsers_Raw")]
		[Produces("application/json")]
		public async Task<string> GetUsers_Raw(Int32 CustomerId)
		{
			List<Users> users = await UsersService.GetUsersForCustomer(CustomerId);

			return JsonSerializer.Serialize(users);
		}

		[HttpPut, Route("Login_Raw")]
		public LoginResponse Login_Raw(string Email,
									   string Password)
		{
			LoginResponse response = SessionService.Login(Email, 
														  ServerCryptographyService.SHA256(Password)); 

			return response;
		}

		[HttpGet, Route("LoginRolesForEmail")]
		public string LoginRolesForEmail(string Email)
		{
			List<LoginType> types = ApplicationService.LoginRolesForEmail(Email);
			string Types = String.Empty;

			if (types.Contains(LoginType.typeDIT))
			{
				Types = "Administrator ";
			}

			if (types.Contains(LoginType.typeCustomer))
			{
				Types += "Customer ";
			}

			if (types.Contains(LoginType.typeUser))
			{
				Types += "User";
			}

			return Types; ;
		}

		[HttpPost, Route("WebLogin_Raw")]
		public static LoginResponse WebLogin_Raw(string		Email,
												 string		Password,
												 LoginType	loginType)
		{
			return ApplicationService.WebLogin(Email, 
											   ServerCryptographyService.SHA256(Password), 
											   loginType);
		}


		[HttpPost, Route("ChangePasswordAnswer")]
		public ErrorCodes ChangePasswordAnswer(ChangePasswordRequest request)
		{
			return UsersService.ChangePasswordAnswer(request);
		}

		[HttpPut, Route("RegisterUser_Raw")]
		public RegisterUserResponse RegisterUser_Raw(RegisterUserRequest request)
		{
			System.Security.Cryptography.Aes aes = ServerCryptographyService.CreateAes();

			request.AesKey = Convert.ToHexString(aes.Key);

			return UsersService.RegisterUser(request);
		}

		[HttpGet, Route("UsersForCustomer")]
		[Produces("application/json")]
		public async Task<string> UsersForCustomer(Int32 CustomerId)
		{
			List<Users> users = await UsersService.GetUsersForCustomer(CustomerId);

			return JsonSerializer.Serialize(users);
		}

		[HttpGet, Route("GetUsersForCustomer")]
		public async Task<string> GetUsers(Int32 CustomerId)
		{
			List<Users> users = await UsersService.GetUsersForCustomer(CustomerId);

			Aes aes = ServerCryptographyService.CreateAes();

			EncryptionWrapperDIT wrapper = new()
			{
				aesIV		= aes.IV,
				primaryKey	= CustomerId,
				type		= LoginType.typeCustomer,
			};

			return await ServerCryptographyService.EncryptAndEncodeResponse(wrapper, users);
		}

	}
}