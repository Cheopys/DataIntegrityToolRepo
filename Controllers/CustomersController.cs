using System.Text.Json;
using System.Net;
using DataIntegrityTool.Schema;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DataIntegrityTool.Services;
using DataIntegrityTool.Shared;
using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using Amazon.Runtime.Internal;
using NLog;
using System.Runtime.Intrinsics.Arm;
using DataIntegrityTool.Db;

/*
	This controller is for use of DIT 
 */

namespace DataIntegrityTool.Controllers
{
	[ApiController]
	[Route("[controller]")]
	public class CustomersController : ControllerBase
	{
		static Logger logger;
		public CustomersController()
		{
			var config = new NLog.Config.LoggingConfiguration();

			// Targets where to log to: File and Console
			var logconsole = new NLog.Targets.ConsoleTarget("logconsole");

			// Rules for mapping loggers to targets            
			config.AddRule(NLog.LogLevel.Info, NLog.LogLevel.Fatal, logconsole);

            // Apply config           
            LogManager.Configuration = config;
			logger = LogManager.GetCurrentClassLogger();
		}

		[HttpPut, Route("RegisterCustomer")]
		public async Task<RegisterCustomerResponse> RegisterCustomer(string registerUserB64 )
		{
			byte[] decrypted =ServerCryptographyService.DecryptRSA(registerUserB64);

            RegisterCustomerRequest? request = JsonSerializer.Deserialize<RegisterCustomerRequest>(decrypted);
            
			RegisterCustomerResponse response = CustomersService.RegisterCustomer(request);

			return response;
		}

		[HttpGet, Route("GetCustomer")]
		public async Task<string> GetCustomer(Int32  CustomerId, Int32 UserId)
		{
			Customers? customer = CustomersService.GetCustomer(CustomerId);

			string customerJSON = JsonSerializer.Serialize(customer);

			System.Security.Cryptography.Aes aesDIT = ServerCryptographyService.CreateAes();

			EncryptionWrapperDIT wrapper = new()
			{
				type			= CustomerOrUser.typeUser, // query comes from the tools, therefore user
				primaryKey		= UserId,
				aesIV			= aesDIT.IV,
				encryptedData	= customerJSON
			};

			return await ServerCryptographyService.EncryptAndEncodeResponse(wrapper, customer);
		}

		[HttpDelete, Route("DeleteCustomer")]
		public void DeleteCustomer(Int32 customerId)
		{
			CustomersService.DeleteCustomer(customerId);
        }

		[HttpGet, Route("GetCustomers")]
		[Produces("application/json")]
		public async Task<string> GetCustomers()
		{
			List<Customers> customers = await CustomersService.GetCustomers();

			System.Security.Cryptography.Aes aesDIT = ServerCryptographyService.CreateAes();

			EncryptionWrapperDIT wrapper = new()
			{
				aesIV		= aesDIT.IV,
				primaryKey	= 0,
				type		= CustomerOrUser.typeDIT,
			};
				
			return await ServerCryptographyService.EncryptAndEncodeResponse(wrapper, customers);
		}

		[HttpGet, Route("GetCustomerUsage")]
		[Produces("application/json")]
		public List<CustomerUsage> GetCustomerUsages (Int32? customerId) 
		{
			return CustomersService.GetCustomerUsages(customerId);
		}

		[HttpPut, Route("AddNewUserTokens")]
		public ErrorCodes AddNewUserTokens(List<UserRegistration> registrations)
		{
			return CustomersService.AddNewUserTokens(registrations);
		}

		[HttpPut, Route("AllocateLicenses")]
        public AllocateLicensesResponse AllocateLicenses(AllocateLicensesRequest request)
		{
			return CustomersService.AllocateLicenses(request);
		}

		[HttpGet, Route("CheckEmail")]
		public CustomerOrUser CheckEmail(string Email)
		{
			return CustomersService.CheckEmail(Email);
		}
    }
}
