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

        [HttpPut, Route("RegisterUser")]
        public RegisterUserResponse RegisterUser([FromBody] EncryptionWrapperDIT wrapper)
        {
            ErrorCodes errorcode = ErrorCodes.errorNone;
            RegisterUserRequest request;

            ServerCryptographyService.DecodeAndDecryptRequest(wrapper, out request);

            return UsersService.RegisterUser(request);
        }

		[HttpGet, Route("GetUser")]
		public async Task<string> GetUser(Int32 UserId)
		{
			Users? user = UsersService.GetUser(UserId);

			string userJSON = JsonSerializer.Serialize(user);

			System.Security.Cryptography.Aes aesDIT = ServerCryptographyService.CreateAes();

			EncryptionWrapperDIT wrapper = new()
			{
				type		  = LoginType.typeUser,
				primaryKey	  = UserId,
				aesIV		  = aesDIT.IV,
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

		[HttpGet, Route("GetUsers")]
		public async Task<string> GetUsers(Int32 CustomerId)
		{
			List<Users> users = await UsersService.GetUsers(CustomerId);

			System.Security.Cryptography.Aes aesCustomer = ServerCryptographyService.CreateAes();

			EncryptionWrapperDIT wrapper = new()
			{
				aesIV		= aesCustomer.IV,
				primaryKey	= 0,
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
