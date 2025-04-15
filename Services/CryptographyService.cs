using System;
using System.Security.Cryptography;
using System.Text.Json;
using DataIntegrityService.Services;
using DataIntegrityTool.Shared;
using DataIntegrityTool.Db;
using DataIntegrityTool.Schema;
using NLog;
using Amazon.Runtime.Internal;
using Amazon.Runtime.Internal.Util;
using Microsoft.EntityFrameworkCore;
using Amazon.Runtime.Internal.Auth;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DataIntegrityService.Services
{
    public class CryptographyService
    {
		static NLog.Logger logger;
		public CryptographyService()
		{
			var config = new NLog.Config.LoggingConfiguration();

			// Targets where to log to: File and Console
			var logconsole = new NLog.Targets.ConsoleTarget("logconsole");

			// Rules for mapping loggers to targets            
			config.AddRule(NLog.LogLevel.Info, NLog.LogLevel.Fatal, logconsole);

			// Apply config           
			NLog.LogManager.Configuration = config;
			logger = LogManager.GetCurrentClassLogger();
		}

		public static Aes GetAesKey(Int64 userId)  
        {
			Aes    aes  = Aes.Create();

			using (DataContext dbcontext = new())
            {
				Customers? customer = dbcontext.Customers.Where(i => i.Id == userId).FirstOrDefault();

				if (customer != null)
				{
					aes = customer.aeskey;
				}
				dbcontext.Dispose();
            }

            return aes;
        }
		/*
		public static async Task<Int64> RegisterClient(string requestB64)
		{
			Random random = new();
			RegisterClientRequest? request = JsonSerializer.Deserialize<RegisterClientRequest>(requestB64);

			UserRegistering user = new()
			{
				Id     = random.NextInt64(),
				aesKey = request.aeskey,
				aesIV  = request.aesiv
			}; 

			using (DataContext dbcontext = new())
			{
				await dbcontext.UserRegistering.AddAsync(user);
				await dbcontext.SaveChangesAsync();
				await dbcontext.DisposeAsync();
			}

			return user.Id;
		}

		public static Aes CreateAesKey()
		{
			Aes aes = Aes.Create();
			aes.KeySize = 256;
			aes.GenerateKey();
			byte[] iv = new byte[16];
			new Random().NextBytes(iv);
			aes.IV = iv;

			return aes;
		}
		*/
		public static void DecodeAndDecryptRequest<T>(EncryptionWrapperDIT wrapper, 
												  out T?				   request, 
													  bool				   bypass 		= false)
		{
			if (bypass)
			{
				request = JsonSerializer.Deserialize<T>(wrapper.encryptedRequest);
			}
			else
			{
				request = default(T);

				Aes aes = CryptographyService.GetAesKey(wrapper.customerId);

				byte[]? encrypted = Convert.FromBase64String(wrapper.encryptedRequest);//JsonSerializer.Deserialize<byte[]>(wrapper.encryptedRequest);

				ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

				// Create the streams used for encryption.

				using (MemoryStream memorystream = new MemoryStream(encrypted))
				{
					using (CryptoStream cryptostream = new CryptoStream(memorystream, decryptor, CryptoStreamMode.Read))
					{
						using (StreamReader streamreader = new StreamReader(cryptostream))
						{
							// read data from the stream.
							string json = streamreader.ReadToEnd();

							request = JsonSerializer.Deserialize<T>(json);

							streamreader.Dispose();
						}

						cryptostream.Dispose();
					}

					memorystream.Dispose();
				}
			}
		}

		public static async Task<string> EncryptAndEncodeResponse<T>(Int64	userId, 
																	 T		response, 
																	 bool   bypass      = false)
		{
			logger?.Info("registering = {registering)");

			if (bypass)
			{
				return JsonSerializer.Serialize(response);
			}
			else
			{
				byte[] encrypted = null;

				Aes aes = CryptographyService.GetAesKey(userId);

				aes.Mode    = CipherMode.CBC;
				aes.Padding = PaddingMode.PKCS7;

				if (aes != null)
				{
					string json = JsonSerializer.Serialize(response);

					ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

					// Create the streams used for encryption.

					using (MemoryStream memorystream = new MemoryStream())
					{
						using (CryptoStream cryptostream = new CryptoStream(memorystream, encryptor, CryptoStreamMode.Write))
						{
							using (StreamWriter streamwriter = new StreamWriter(cryptostream))
							{
								// Write data to the stream.
								streamwriter.Write(json);

								await streamwriter.DisposeAsync();
							}

							await cryptostream.DisposeAsync();
						}

						encrypted = memorystream.ToArray();

						await memorystream.DisposeAsync();
					}
				}

				return Convert.ToBase64String(encrypted); //JsonSerializer.Serialize(encrypted);
			}
		}
	}
}