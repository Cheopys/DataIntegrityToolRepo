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
				Description	 = "Test Customer",
				Email		 = "testcust@example.com",
				Name		 = "Test Customer",
				Notes		 = "each time this is run it will increment the primary key",
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
									   string Password,
									   LoginType loginType = LoginType.typeCustomer)
		{
			LoginResponse response = SessionService.Login(Email, 
														  ServerCryptographyService.SHA256(Password), 
														  loginType); 

			return response;
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
					Users? user = context.Users.Where(user => user.Email.Equals(cu.Email)).FirstOrDefault();

					if (user == null)
					{
						user = new Users()
						{
							AesKey			 = cu.AesKey,
							DateAdded	 = cu.DateAdded,
							Email		 = cu.Email,
							Name		 = cu.Name,
							PasswordHash = cu.PasswordHash,
							Tools		 = cu.Tools
						};

						context.Add(user);
					}

					context.SaveChanges();
					context.Dispose();
				});
			}
		}
	}
}