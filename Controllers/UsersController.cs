using Amazon.Runtime.Internal;
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
using System.Text.Json;

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

		[HttpPut, Route("RegisterUserRSA")]
		[Produces("application/json")]
		public async Task<string> RegisterUserRSA([FromBody] string registerUserB64)
		{
			RegisterUserRequest request = ServerCryptographyService.DecryptRSA<RegisterUserRequest>(registerUserB64);

			RegisterUserResponse response = UsersService.RegisterUser(request);

			return JsonSerializer.Serialize(response);
		}

		[HttpGet, Route("GetUser")]
		public async Task<string> GetUser(Int32		UserId, 
										  string	AesIVHex, 
										  LoginType LoginType,
										  Int32		PrimaryKey)

		{
			Users? user = UsersService.GetUser(UserId);

			string userJSON = JsonSerializer.Serialize(user);

			EncryptionWrapperDIT wrapperCaller = new()
			{
				aesIV           = Convert.FromHexString(AesIVHex),
				type			= LoginType,
				primaryKey		= PrimaryKey,
			};

			System.Security.Cryptography.Aes AesKey = ServerCryptographyService.GetAesKey(wrapperCaller);

			EncryptionWrapperDIT wrapper = new()
			{
				type		  = LoginType,
				primaryKey	  = PrimaryKey,
				aesIV		  = AesKey.IV,
				encryptedData = userJSON
			};

			return await ServerCryptographyService.EncryptAndEncodeResponse(wrapper, user);
		}

		[HttpPost, Route("UpdateUser")]
		public void UpdateUser([FromBody] EncryptionWrapperDIT wrapper)
		{
			UpdateUserRequest request;

			ServerCryptographyService.DecodeAndDecryptRequest<UpdateUserRequest>(wrapper, out request);

			UsersService.UpdateUser(request);
		 }

		[HttpGet, Route("GetUsersForCustomer")]
		public async Task<string> GetUsersForCustomer(Int32  CustomerId, 
													  string AesIVHex)
		{
			List<Users> users = await UsersService.GetUsersForCustomer(CustomerId);

			EncryptionWrapperDIT wrapper = new()
			{
				aesIV		= Convert.FromHexString(AesIVHex),
				primaryKey	= CustomerId,
				type		= LoginType.typeCustomer,
			};
				
			return await ServerCryptographyService.EncryptAndEncodeResponse(wrapper, users);
		}

		[HttpGet, Route("GetCustomerUsage")]
		[Produces("application/json")]
		public List<CustomerUsage> GetCustomerUsages (Int32? customerId) 
		{
			return CustomersService.GetCustomerUsages(customerId);
		}

		[HttpPost, Route("ChangePasswordAsk")]
		public Int32 ChangePasswordAsk(Int32 UserId)
		{
			return (Int32) UsersService.ChangePasswordAsk(UserId);
		}

		[HttpPost, Route("ChangePasswordAnswer")]
		public ErrorCodes ChangePasswordAnswer([FromBody] EncryptionWrapperDIT wrapper)
		{
			ChangePasswordRequest? request;
			ServerCryptographyService.DecodeAndDecryptRequest<ChangePasswordRequest>(wrapper, out request);

			return UsersService.ChangePasswordAnswer(request);
		}
	}
}
