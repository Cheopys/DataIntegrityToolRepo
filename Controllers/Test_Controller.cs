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
		[HttpPut, Route("RegisterCustomer_Raw")]
		public async Task<RegisterCustomerResponse> RegisterCustomer_Raw()
		{
			RegisterCustomerResponse response;

			System.Security.Cryptography.Aes aeskey = ServerCryptographyService.CreateAes();

			RegisterCustomerRequest request = new()
			{
				AesKey = Convert.ToHexString(aeskey.Key),
				Description = "Test Customer",
				Email = "testcust@example.com",
				Name = "Test Customer",
				Notes = "each time this is run it will increment the primary key",
				PasswordHash = "5e884898da28047151d0e56f8dc6292773603d0d6aabbdd62a11ef721d1542d8",
				Tools = new List<ToolTypes>
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
										bool isAdministrator)
		{
			string PasswordHash;
			byte[] data;
			using (var sha256 = new SHA256Managed())
			{
				data  = sha256.ComputeHash(Encoding.UTF8.GetBytes(Password));
				PasswordHash = Convert.ToBase64String(data);
			}

			LoginResponse response = SessionService.Login(Email, PasswordHash, isAdministrator);

			response.PasswordHash = PasswordHash;
			response.data = data;

			return response;
		}
	}
}