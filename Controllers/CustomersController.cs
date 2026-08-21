using Amazon;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using DataIntegrityTool.Db;
using DataIntegrityTool.Schema;
using DataIntegrityTool.Services;
using DataIntegrityTool.Shared;
using Microsoft.AspNetCore.Mvc;
using NLog;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace DataIntegrityTool.Controllers
{
	[ApiController]
	[Route("[controller]")]
	public class CustomersController : ControllerBase
	{
		static Logger				 logger;
		static IAmazonSecretsManager secretsClient;

		static CustomersController()
		{
			var config = new NLog.Config.LoggingConfiguration();

			// Targets where to log to: File and Console
			var logconsole = new NLog.Targets.ConsoleTarget("logconsole");

			// Rules for mapping loggers to targets            
			config.AddRule(NLog.LogLevel.Info, NLog.LogLevel.Fatal, logconsole);

			// Apply config           
			LogManager.Configuration = config;
			logger = LogManager.GetCurrentClassLogger();

			// singleton client

			secretsClient = new AmazonSecretsManagerClient(RegionEndpoint.GetBySystemName("ca-central-1"));
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

			// remove trial from list; the int cast is required because the enum is boxed and
			// without the cast the comparison will silently fail.

			SubscriptionTypes? trial = subscriptions.Where(s => s.Id.Equals((int) SubscriptionType.subscriptionTrial)).FirstOrDefault();

			subscriptions.Remove(trial);

			return JsonSerializer.Serialize(subscriptions);
		}

		public class Secret
		{
			public string DITAddSubscriptionKey { get; set; }
		};

		static async Task<string> GetAuthSecret()
		{
			string value = string.Empty;

			GetSecretValueRequest request = new()
			{
				SecretId     = "DITAuthorizationKey",
				VersionStage = "AWSCURRENT" 
			};

			GetSecretValueResponse response;

			try
			{
				response = await secretsClient.GetSecretValueAsync(request);

				Secret secret = JsonSerializer.Deserialize<Secret>(response.SecretString);

				value = secret.DITAddSubscriptionKey;
			}
			catch (Exception excxeption)
			{
				logger.ForExceptionEvent(excxeption).Log();
			}

			return value;
		}

		[HttpPut, Route("AddCustomerPayment")]
		public async Task<AddSubscriptionResponse> AddCustomerPayment([FromHeader(Name = "X-DIT-Internal-Key")] string? ApiKey,
																	  Int32  CustomerId, 
																	  Int32  Amount,
																	  Int32  SubscriptionType,
																	  Int64  ExpirationDateUNIX)
		{
			AddSubscriptionResponse response;

			// ApiKey comes with escaped quotes

			string apikey = ApiKey.Replace("\"", string.Empty);

			string expectedKey = await GetAuthSecret();
			bool keyMatches = string.IsNullOrEmpty(expectedKey) == false
							 && CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(apikey ?? String.Empty),
																	    Encoding.UTF8.GetBytes(expectedKey));

			if (keyMatches)
			{
				DateTime ExpirationDate = DateTimeOffset.FromUnixTimeSeconds(ExpirationDateUNIX).UtcDateTime;

				response = CustomersService.AddSubscription(CustomerId, SubscriptionType, Amount, ExpirationDate);
			}
			else
			{
				string logMessage;
				ErrorCodes error;

				if (string.IsNullOrEmpty(expectedKey))
				{
					logMessage = "AddCustomerPayment authentication key not found in Secrets Manager";
					error      = ErrorCodes.errorKeyNotInSecretsManager;
				}
				else
				{
					logMessage = "AddCustomerPayment authentication key invalid";
					error      = ErrorCodes.errorNotAuthenticated;
				}

				logger.Error(logMessage);

				response = new()
				{
					CustomerId = CustomerId,
					Error      = error,
					expected = expectedKey,
					apikey = ApiKey
				};
			}

			return response;
		}

		[HttpGet, Route("GetCustomerPayments")]
		[Produces("application/json")]
		public List<CustomerPayments> GetCustomerPayments(Int32 CustomerId)
		{
			List<CustomerPayments> payments;

			using (DataContext context = new())
			{
				payments = context.CustomerPayments.Where(p => p.CustomerId.Equals(CustomerId)).ToList();

				context.Dispose();
			}

			return payments;
		}

		[HttpGet, Route("GetAllCustomerPayments")]
		[Produces("application/json")]
		public List<CustomerPayments> GetAllCustomerPayments()
		{
			List<CustomerPayments> payments;

			using (DataContext context = new())
			{
				payments = context.CustomerPayments.ToList();

				context.Dispose();
			}

			return payments;
		}

		[HttpGet, Route("CustomerExpirationDate")]
		public DateTime? CustomerExpirationDate(Int32 customerId)
		{
			return CustomersService.CustomerExpirationDate(customerId);
		}
	}
}
