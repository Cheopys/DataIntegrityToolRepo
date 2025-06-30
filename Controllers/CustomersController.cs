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
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

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

		//  C

		private static Aes CreateAes()
		{
			Aes aes		= Aes.Create();
			aes.KeySize = 256;
			aes.Mode	= CipherMode.CBC;
			aes.Padding = PaddingMode.PKCS7;

			return aes;
		}

		private static string EncryptRSA(byte[] cleartext)
		{
			byte[] publicKey = ServerCryptographyService.GetServerRSAPublicKey();

			RSACryptoServiceProvider csp = new RSACryptoServiceProvider(4096);

			int cbRead;
			csp.ImportRSAPublicKey(publicKey, out cbRead);

			byte[] textEncrypted = csp.Encrypt(cleartext, false); //PKCS7 padding

			return Convert.ToBase64String(textEncrypted);
		}

		[HttpPut, Route("PrepareRegisterCustomerRequest")]
		public string PrepareRegisterCustomerRequest(RegisterCustomerRequest request)
		{
			Aes aes = CreateAes();

			request.AesKey = Convert.ToHexString(aes.Key);

			string requestSerialized = JsonSerializer.Serialize(request);

			byte[] requestEncoded = Encoding.UTF8.GetBytes(requestSerialized);
			Program.registerCustomerB64 = EncryptRSA(requestEncoded);

			return Convert.ToHexString(aes.Key)
		}

		[HttpPut, Route("RegisterCustomer")]
		public RegisterCustomerResponse RegisterCustomer()
		{
			RegisterCustomerRequest request = ServerCryptographyService.DecryptRSA<RegisterCustomerRequest>(Program.registerCustomerB64);

			Program.registerCustomerB64 = String.Empty;

			RegisterCustomerResponse response = CustomersService.RegisterCustomer(request);

			return response;
		}

		[HttpPut, Route("PrepareReprovisionCustomerRequest")]
		public void PrepareReprovisionCustomerRequest(ReprovisionCustomerRequest request)
		{
			string requestSerialized = JsonSerializer.Serialize(request);

			byte[] requestEncoded = Encoding.UTF8.GetBytes(requestSerialized);
			Program.reprovisionCustomerB64 = EncryptRSA(requestEncoded);
		}

		[HttpGet, Route("ReprovisionCustomer")]
		public ReprovisionCustomerResponse ReprovisionCustomer()
		{
			ReprovisionCustomerRequest request = ServerCryptographyService.DecryptRSA<ReprovisionCustomerRequest>(Program.registerCustomerB64);

			Program.reprovisionCustomerB64 = String.Empty;

			return CustomersService.ReprovisionCustomer(request);
		}

		//  R

		[HttpGet, Route("GetCustomer")]
		public async Task<string> GetCustomer(Int32  CustomerId, Int32 UserId)
		{
			Customers? customer = CustomersService.GetCustomer(CustomerId);

			string customerJSON = JsonSerializer.Serialize(customer);

			System.Security.Cryptography.Aes aesDIT = ServerCryptographyService.CreateAes();

			EncryptionWrapperDIT wrapper = new()
			{
				type			= LoginType.typeUser, // query comes from the tools, therefore user
				primaryKey		= UserId,
				aesIV			= aesDIT.IV,
				encryptedData	= customerJSON
			};

			return await ServerCryptographyService.EncryptAndEncodeResponse(wrapper, customer);
		}

		//  U

		[HttpPost, Route("UpdateCustomer")]
		public void UpdateCustomer(EncryptionWrapperDIT wrapper)
		{
			UpdateCustomerRequest request;

			ServerCryptographyService.DecodeAndDecryptRequest<UpdateCustomerRequest>(wrapper, out request);

			CustomersService.UpdateCustomer(request);
		}

		//  D

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
				type		= LoginType.typeDIT,
			};
				
			return await ServerCryptographyService.EncryptAndEncodeResponse(wrapper, customers);
		}

		[HttpGet, Route("GetCustomerUsage")]
		[Produces("application/json")]
		public List<CustomerUsage> GetCustomerUsages (Int32? customerId) 
		{
			return CustomersService.GetCustomerUsages(customerId);
		}

		[HttpPut, Route("AllocateLicenses")]
        public AllocateLicensesResponse AllocateLicenses(AllocateLicensesRequest request)
		{
			return CustomersService.AllocateLicenses(request);
		}

		[HttpGet, Route("CheckEmail")]
		public LoginType CheckEmail(string Email)
		{
			return CustomersService.CheckEmail(Email);
		}
    }
}
