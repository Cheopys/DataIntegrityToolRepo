using Amazon.Runtime.Internal;
using DataIntegrityTool.Db;
using DataIntegrityTool.Schema;
using DataIntegrityTool.Services;
using DataIntegrityTool.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
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

		[HttpGet, Route("CustomerGetUser")]
		public async Task<string> CustomerGetUser(Int32  UserIdSought,
												  Int32  CustomerIdSeeker,
												  string AesIVHex)
		{
			Users? user = UsersService.GetUser(UserIdSought);

			if (user.CustomerId.Equals(CustomerIdSeeker))
			{
				EncryptionWrapperDIT wrapper = new()
				{
					type		= LoginType.typeCustomer,
					primaryKey	= CustomerIdSeeker,
					aesIV		= Convert.FromHexString(AesIVHex),
				};

				return await ServerCryptographyService.EncryptAndEncodeResponse(wrapper, user);
			}
			else
			{
				return $"Error User {UserIdSought} does not belong to customer {CustomerIdSeeker}";
			}
		}

		[HttpGet, Route("AdminGetUser")]
		public async Task<string> AdminGetUser(Int32  UserIdSought,
											   Int32  AdminIdSeeker,
											   string AesIVHex)
		{
			Users? user = UsersService.GetUser(UserIdSought);

			EncryptionWrapperDIT wrapper = new()
			{
				type		= LoginType.typeAdministrator,
				primaryKey	= AdminIdSeeker,
				aesIV		= Convert.FromHexString(AesIVHex),
			};

			return await ServerCryptographyService.EncryptAndEncodeResponse(wrapper, user);
		}

		[HttpPost, Route("UpdateUser")]
		public string UpdateUser(EncryptionWrapperDITString wrapperString)
		{
			string ret = $"incoming primary key is {wrapperString.primaryKey}; ";

			EncryptionWrapperDIT wrapper = new EncryptionWrapperDIT()
			{
				primaryKey		= wrapperString.primaryKey,
				type			= LoginType.typeUser,
				encryptedData	= wrapperString.encryptedData,
				aesIV			= Convert.FromHexString(wrapperString.aesIVHex)
			};

			UpdateUserRequest request;

			ServerCryptographyService.DecodeAndDecryptRequest<UpdateUserRequest>(wrapper, out request);

			ret += $"decrypted request PK = {request.UserId}, first name = {request.NameFirst}; ";

			request.UserId = wrapper.primaryKey;

			return ret + UsersService.UpdateUser(request);
		}

		[HttpGet, Route("GetUsersForCustomer")]
		public async Task<string> GetUsers(Int32 CustomerId, string AesIVHex)
		{
			List<Users> users = await UsersService.GetUsersForCustomer(CustomerId);

			EncryptionWrapperDIT wrapper = new()
			{
				aesIV = Convert.FromHexString(AesIVHex),
				primaryKey = CustomerId,
				type = LoginType.typeCustomer
			};

			return await ServerCryptographyService.EncryptAndEncodeResponse(wrapper, users);
		}

		[HttpGet, Route("GetCustomerUsage")]
		[Produces("application/json")]
		public List<CustomerUsage> GetCustomerUsages (Int32? customerId) 
		{
			return CustomersService.GetCustomerUsages(customerId);
		}

		[HttpDelete, Route("DeleteUser")]
		public string DeleteUser(Int32 CustomerId, 
							     Int32 UserId)
		{
			return UsersService.DeleteUser(CustomerId, UserId);
		}
	}
}
