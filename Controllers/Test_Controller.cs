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
using System.Runtime.Intrinsics.Arm;
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
		[HttpPut, Route("RegisterCustomer_Example")]
		public async Task<RegisterCustomerResponse> RegisterCustomer_Example()
		{
			RegisterCustomerResponse response;

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
				}
			};

			response = CustomersService.RegisterCustomer(request);

			return response;
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
			List<Users> users = await UsersService.GetUsers(CustomerId);

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

		[HttpPut, Route("CustomersToUsers")]
		public void CustomersToUsers()
		{
			using (DataContext context = new())
			{
				List<Customers> customers = context.Customers.ToList();

				customers.ForEach(cu =>
				{
					context.Add(new Users()
					{
						CustomerId   = (cu.Id > 4) ? cu.Id : 4,
						AesKey		 = cu.AesKey,
						DateAdded	 = cu.DateAdded,
						Email		 = cu.Email,
						NameFirst	 = cu.NameFirst,
						NameLast     = cu.NameLast,
						PasswordHash = cu.PasswordHash,
						Tools		 = cu.Tools
					});
				});

				context.SaveChanges();
				context.Dispose();
			}
		}

		[HttpPut, Route("CustomersToAdministrators")]
		public void CustomersToAdministrators()
		{
			using (DataContext context = new())
			{
				List<Customers> customers = context.Customers.Where(cu => cu.Id < 4).ToList();

				customers.ForEach(cu =>
				{
					Administrators administrator = new Administrators()
					{
						AesKey = cu.AesKey,
						DateAdded = cu.DateAdded,
						Email = cu.Email,
						NameFirst = cu.NameFirst,
						NameLast = cu.NameLast,
						PasswordHash = cu.PasswordHash,
					};

					context.Add(administrator);
				});

				context.SaveChanges();
				context.Dispose();
			}
		}


		[HttpPut, Route("UsersToCustomers")]
		public void UsersToCustomers()
		{
			using (DataContext context = new())
			{
				List<Users> users = context.Users.Where(cu => cu.Id > 4).ToList();

				users.ForEach(us =>
				{
					Customers customer = new Customers()
					{
						AesKey		 = us.AesKey,
						DateAdded	 = us.DateAdded,
						Email		 = us.Email,
						NameFirst    = us.NameFirst,
						NameLast     = us.NameLast,
						PasswordHash = us.PasswordHash,
						Company      = "Beta Testers",
						Tools		 = us.Tools,
						MeteringCount= 1000,
						Notes		 = "beta",
						SubscriptionTime = TimeSpan.FromDays(365),
					};

					context.Add(customer);
				});

				context.SaveChanges();
				context.Dispose();
			}
		}

		[HttpPut, Route("RegisterUser_Raw")]
		public RegisterUserResponse RegisterUser_Raw(RegisterUserRequest request)
		{
			System.Security.Cryptography.Aes aes = ServerCryptographyService.CreateAes();

			request.AesKey = Convert.ToHexString(aes.Key);
			request.CustomerId = 89;

			return UsersService.RegisterUser(request);
		}

		[HttpGet, Route("UsersForCustomer")]
		[Produces("application/json")]
		public string UsersForCustomer(Int32 CustomerId)
		{
			List<Users> users = Usera
		}
	}
}