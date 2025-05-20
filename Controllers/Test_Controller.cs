using System.Text.Json;
using System.Net;
using DataIntegrityTool.Schema;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DataIntegrityTool.Services;
using DataIntegrityTool.Shared;
using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using NLog;
using System.Runtime.Intrinsics.Arm;
using DataIntegrityTool.Db;

namespace DataIntegrityTool.Controllers
{
	[ApiController]
	[Route("[controller]")]

	public class Test_Controller : ControllerBase
	{
		static Logger logger;
		public Test_Controller()
		{
			var config = new NLog.Config.LoggingConfiguration();

			// Targets where to log to: File and Console
			var logconsole = new NLog.Targets.ConsoleTarget("logconsole");

			// Rules for mapping loggers to targets            
			config.AddRule(NLog.LogLevel.Info, NLog.LogLevel.Info, logconsole);

			// Apply config           
			LogManager.Configuration = config;
			logger = LogManager.GetCurrentClassLogger();
		}

		[HttpPut, Route("RegisterCustomer_Raw")]
		public async Task<RegisterCustomerResponse> RegisterCustomer_Raw()
		{
			RegisterCustomerResponse response;

			System.Security.Cryptography.Aes aeskey = ServerCryptographyService.CreateAes();

			logger.Info($"AES key size is {aeskey.Key.Length}");

			RegisterCustomerRequest request = new()
			{
				AesKey		 = Convert.ToHexString(aeskey.Key),
				Description  = "Test Customer",
				Email		 = "testcust@example.com",
				Name		 = "Test Customer",
				Notes		 = "each time this is run it will increment the primary key",
				PasswordHash = "5e884898da28047151d0e56f8dc6292773603d0d6aabbdd62a11ef721d1542d8",
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

		class TestReq
		{
			public Int32  CustomerId { get; set; }
			public byte[] Key		 { get; set; }
		};

		[HttpPut, Route("GetAESKeySize")]
		public string GetAESKeySize()
		{
			System.Security.Cryptography.Aes aeskey = ServerCryptographyService.CreateAes();

			TestReq tr = new()
			{
				CustomerId = 12,
				Key = aeskey.Key
			};

			return $"AES key size is {aeskey.Key.Length}; request key size is { tr.Key.Length }";
		}
	}
}