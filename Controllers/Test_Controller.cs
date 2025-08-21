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
using System.Security.Cryptography.Xml;
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
				AesKey = Convert.ToHexString(aeskey.Key),
				Email = "testcust@example.com",
				Company = "testcompany@example.com",
				NameFirst = "Test",
				NameLast = "Customer",
				Notes = "each time this is run it will increment the primary key",
				Password = "DIT",
				Tools = new List<ToolTypes>
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

			byte[] requestEncoded = Encoding.UTF8.GetBytes(requestSerialized);
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

			if (types.Contains(LoginType.typeAdministrator))
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
		public static LoginResponse WebLogin_Raw(string Email,
												 string Password,
												 LoginType loginType)
		{
			return ApplicationService.WebLogin(Email,
											   ServerCryptographyService.SHA256(Password),
											   loginType);
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
				aesIV = aes.IV,
				primaryKey = CustomerId,
				type = LoginType.typeCustomer,
			};

			return await ServerCryptographyService.EncryptAndEncodeResponse(wrapper, users);
		}

		[HttpPost, Route("UpdateUser")]
		public async Task<string> UpdateUser(UpdateUserRequest request)
		{
			EncryptionWrapperDIT wrapper = new()
			{
				type = LoginType.typeUser,
				primaryKey = 99,
				aesIV = ServerCryptographyService.CreateAes().IV,
			};

			wrapper.encryptedData = await ServerCryptographyService.EncryptAndEncodeResponse(wrapper, request);

			//			return wrapper.encryptedData;

			UpdateUserRequest requestInner;

			ServerCryptographyService.DecodeAndDecryptRequest(wrapper, out requestInner);

			return UsersService.UpdateUser(requestInner);
		}

		[HttpPost, Route("UpdateUser_Raw")]
		public async Task<string> UpdateUser_Raw(UpdateUserRequest request)
		{
			return UsersService.UpdateUser(request);
		}

		[HttpGet, Route("DecryptPasswordAskResponse")]
		[Produces("application/json")]
		public string DecryptPasswordAskResponse(EncryptionWrapperDITString wrapperString)
		{
			ChangePasswordAskResponse response;
			ServerCryptographyService.DecodeAndDecryptRequest<ChangePasswordAskResponse>(wrapperString.ToBinaryVersion(), out response);

			return JsonSerializer.Serialize(response);
		}

		[HttpPost, Route("EncryptChangePasswordRequest")]
		public async Task<EncryptionWrapperDITString> EncryptChangePasswordRequest(ChangePasswordRequest request)
		{
			EncryptionWrapperDITString wrapperString = new()
			{
				primaryKey = request.PrimaryKey,
				type = request.LoginType,
				aesIVHex = request.AesIVHex,
			};

			wrapperString.encryptedData = await ServerCryptographyService.EncryptAndEncodeResponse<ChangePasswordRequest>(wrapperString.ToBinaryVersion(), request);

			return wrapperString;
		}

		[HttpGet, Route("InterleavedKeyTest")]
		public async Task<string> InterleavedKeyTest(string input)
		{
			Aes aesInput = ServerCryptographyService.CreateAes();
			Aes aesInterleaved = ServerCryptographyService.CreateAes();

			string hexInput       = Convert.ToHexString(aesInput.Key);
			string hexInterleaved = Convert.ToHexString(aesInterleaved.Key);
			string stegnokey      = String.Empty;

//			string stringEncrypted = await ServerCryptographyService.EncrypytAES(aesInput, input);

			for (int i = 0; i < hexInput.Length; i += 2)
			{
				stegnokey += hexInterleaved.Substring(i, 2);
				stegnokey += hexInput.Substring(i, 2);
			}

			string hexOriginal = String.Empty;

			for (int i = 2; i < stegnokey.Length; i += 4)
			{
				hexOriginal += stegnokey.Substring(i, 2);
			}

			byte[] key = Convert.FromHexString(hexOriginal);

			return $"before interleave {hexInput} after restore {hexOriginal}";
		}

		[HttpGet, Route("InterleavedKeyLoginTest")]
		public async Task<string> InterleavedKeyLoginTest()
		{
			Aes aesInput = ServerCryptographyService.CreateAes();
			Aes aesInterleaved = ServerCryptographyService.CreateAes();

			string hexInput = Convert.ToHexString(aesInput.Key);
			string hexInterleaved = Convert.ToHexString(aesInterleaved.Key);
			string stegnokey = String.Empty;

			//			string stringEncrypted = await ServerCryptographyService.EncrypytAES(aesInput, input);

			for (int i = 0; i < hexInput.Length; i += 2)
			{
				stegnokey += hexInterleaved.Substring(i, 2);
				stegnokey += hexInput.Substring(i, 2);
			}

			WebLoginRequest loginRequest = new()
			{
				Email = "davidpcrawford@gmail.com",
				LoginType = LoginType.typeCustomer,
				Password = "DIT"
			};

			string req = JsonSerializer.Serialize(loginRequest);

			string reqb = await ServerCryptographyService.EncrypytAES(aesInput, req);

			return $"req = {reqb}, keyInt = {stegnokey}, IV = {Convert.ToHexString(aesInput.IV)}";
		}
	}
}