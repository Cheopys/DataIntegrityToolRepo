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
using System.Security.Cryptography.Xml;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

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
			Aes aes = Aes.Create();
			aes.KeySize = 256;
			aes.Mode = CipherMode.CBC;
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

		[HttpPut, Route("RegisterCustomerRSA")]
		[Produces("application/json")]
		public async Task<string> RegisterCustomerRSA([FromBody] string registerCustomerB64)
		{
			RegisterCustomerRequest request = ServerCryptographyService.DecryptRSA<RegisterCustomerRequest>(registerCustomerB64);

			RegisterCustomerResponse response = CustomersService.RegisterCustomer(request);

			return JsonSerializer.Serialize(response);
		}

		[HttpPut, Route("PrepareReprovisionCustomerRequest")]
		public void PrepareReprovisionCustomerRequest(ReprovisionCustomerRequest request)
		{
			string requestSerialized = JsonSerializer.Serialize(request);

			byte[] requestEncoded = Encoding.UTF8.GetBytes(requestSerialized);
			Program.reprovisionCustomerB64 = EncryptRSA(requestEncoded);
		}

		[HttpGet, Route("ReprovisionCustomer")]
		public async Task<string> ReprovisionCustomer(System.Security.Cryptography.Aes AesKey)
		{
			ReprovisionCustomerRequest request = ServerCryptographyService.DecryptRSA<ReprovisionCustomerRequest>(Program.reprovisionCustomerB64);

			Program.reprovisionCustomerB64 = String.Empty;

			ReprovisionCustomerResponse response = CustomersService.ReprovisionCustomer(request);

			string responseSeriaized = JsonSerializer.Serialize(response);

			return await ServerCryptographyService.EncrypytAES(AesKey, responseSeriaized);
		}

		[HttpGet, Route("AdminGetCustomer")]
		public async Task<string> AdminGetCustomer(Int32 CustomerIdSought,
												   Int32 AdminIdSeeker,
												   string AesIVHex)
		{
			Customers? customer = CustomersService.GetCustomer(CustomerIdSought);

			EncryptionWrapperDIT wrapper = new()
			{
				type		= LoginType.typeAdministrator,
				primaryKey	= AdminIdSeeker,
				aesIV		= Convert.FromHexString(AesIVHex),
			};

			return await ServerCryptographyService.EncryptAndEncodeResponse(wrapper, customer);
		}

		//  R
		// N/A

		//  U

		[HttpPost, Route("UpdateCustomer")]
		public ErrorCodes UpdateCustomer(EncryptionWrapperDITString wrapperString)
		{
			EncryptionWrapperDIT wrapper = new EncryptionWrapperDIT()
			{
				primaryKey		= wrapperString.primaryKey,
				type			= wrapperString.type,
				encryptedData	= wrapperString.encryptedData,
				aesIV			= Convert.FromHexString(wrapperString.aesIVHex)
			};

			UpdateCustomerRequest request;

			ServerCryptographyService.DecodeAndDecryptRequest<UpdateCustomerRequest>(wrapper, out request);

			return CustomersService.UpdateCustomer(request);
		}

		//  D

		[HttpDelete, Route("DeleteCustomer")]
		public void DeleteCustomer(Int32 customerId)
		{
			CustomersService.DeleteCustomer(customerId);
		}

		[HttpGet, Route("GetCustomerUsage")]
		[Produces("application/json")]
		public List<CustomerUsage> GetCustomerUsages(Int32? customerId)
		{
			return CustomersService.GetCustomerUsages(customerId);
		}

		[HttpPost, Route("AddCustomerScans")]
		public Int32 AddCustomerScans(Int32 CustomerId,
									  Int32 newScans)
		{
			return CustomersService.AddCustomerScans(CustomerId, newScans);
		}

		[HttpGet, Route("CheckEmail")]
		public LoginType CheckEmail(string Email)
		{
			return CustomersService.CheckEmail(Email);
		}

		[HttpGet, Route("GetSubscriptionTypes")]
		[Produces("application/json")]
		public string GetSubscriptionTypes()
		{
			List<SubscriptionTypes> subscriptions = new();

			using (DataContext context = new())
			{
				subscriptions = context.SubscriptionTypes.OrderBy(st => st.Id).ToList();

				context.Dispose();
			}

			return JsonSerializer.Serialize(subscriptions);
		}

		[HttpPut, Route("AddCustomerPayment")]
		public AddSubscriptionResponse AddCustomerPayment(Int32  CustomerId,
														  Int32  Amount,
														  Int32  SubscriptionType,
														  Int16? Scans)
		{
			AddSubscriptionResponse response = new()
			{
				CustomerId = CustomerId,
				Error      = ErrorCodes.errorNone,
			};

			response = CustomersService.AddSubscription(CustomerId, SubscriptionType);

			if (Scans != null
			&&  Scans  > 0)
			{
				TopupScansResponse responseTU = CustomersService.TopUpScans(CustomerId, Scans.Value, Amount);

				response.ScansAfter = responseTU.ScansAfter;
			}

			return response;
		}

		[HttpGet, Route("GetCustomerPayments")]
		[Produces("application/json")]
		public List<CustomerPayments> GetCustomerPayments(Int32? CustomerId)
		{
			List<CustomerPayments> payments = null;

			using (DataContext context = new())
			{
				payments = context.CustomerPayments.ToList();

				if (CustomerId != null)
				{
					payments = payments.Where(p => p.CustomerId.Equals(CustomerId)).ToList();
				}

				context.Dispose();
			}

			return payments;
		}

		[HttpGet, Route("CustomerRemainingScans")]
		public Int32 CustomerRemainingScans(Int32 customerId)
		{
			return CustomersService.CustomerRemainingScans(customerId);
		}
	}
}
