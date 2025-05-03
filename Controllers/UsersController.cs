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

/*
	This controller is for use of DIT 
 */

namespace DataIntegrityTool.Controllers
{
	[ApiController]
	[Route("[controller]")]
	public class UsersController : ControllerBase
	{
		static Logger logger;
		public UsersController()
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

		[HttpPut, Route("RegisterUser")]
		public async Task<RegisterUserResponse> RegisterUser(EncryptionWrapperDIT wrapper)
		{
			RegisterUserRequest? request;

			ServerCryptographyService.DecodeAndDecryptRequest(wrapper, out request);
			return UsersService.RegisterUser(request);
		}

		[HttpGet, Route("GetUsers")]
		[Produces("application/json")]
		public async Task<string> GetUsers(Int32 CustomerId)
		{
			List<Users> users = await UsersService.GetUsers(CustomerId);

			System.Security.Cryptography.Aes aesDIT = ServerCryptographyService.CreateAes();

			EncryptionWrapperDIT wrapper = new()
			{
				aesIV		= aesDIT.IV,
				primaryKey	= 0,
				type		= CustomerOrUser.typeDIT,
			};
				
			return await ServerCryptographyService.EncryptAndEncodeResponse(wrapper, users);
		}

		[HttpGet, Route("GetCustomerUsage")]
		[Produces("application/json")]
		public List<CustomerUsage> GetCustomerUsages (Int32? customerId) 
		{
			return CustomersService.GetCustomerUsages(customerId);
		}
	}
}
