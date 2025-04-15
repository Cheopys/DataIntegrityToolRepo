﻿using System.Security.Cryptography;
using System.Text.Json;
using DataIntegrityTool.Db;
using DataIntegrityTool.Schema;
using DataIntegrityTool.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using NLog;

namespace DataIntegrityTool.Services
{ 
	public static class ServerCryptographyService
	{
		static NLog.Logger logger;
		static ServerCryptographyService()
		{
			var config = new NLog.Config.LoggingConfiguration();

			// Targets where to log to: File and Console
			var logconsole = new NLog.Targets.ConsoleTarget("logconsole");

			// Rules for mapping loggers to targets            
			config.AddRule(NLog.LogLevel.Info, NLog.LogLevel.Fatal, logconsole);

			// Apply config           
			NLog.LogManager.Configuration = config;
			logger = LogManager.GetCurrentClassLogger();

            using (DataContext context = new())
            {
                ToolParameters? toolparameters = context.ToolParameters.FirstOrDefault();


                if (toolparameters == null)
                {
                    RSA rsa = RSA.Create(4096);
                    context.ToolParameters.Add(new ToolParameters
                    {
                        publicKey  = rsa.ExportRSAPublicKey(),
                        privateKey = rsa.ExportRSAPrivateKey(),
                    });

                    context.SaveChanges();
                }

                context.Dispose();
            }
        }

        public static byte[] GetServerRSAPublicKey()
		{
			byte[] key = null;
			using (DataContext context = new())
			{
                ToolParameters? toolparameters = context.ToolParameters.FirstOrDefault();

				if (toolparameters == null)
				{
					RSA rsa = RSA.Create(4096);
					context.ToolParameters.Add(new ToolParameters
					{
						publicKey  = rsa.ExportRSAPublicKey(),
						privateKey = rsa.ExportRSAPrivateKey(),
					});

					key = rsa.ExportRSAPublicKey();

					context.SaveChanges();
				}
				else
				{
					key = toolparameters.publicKey;
				}

					context.Dispose();
			}

			return key;
		}

		private static byte[] GetServerRSAPrivateKey()
		{
			byte[]? key = null;	

			using (DataContext context = new())
			{ 
				key = context.ToolParameters.Select(tp => tp.privateKey).FirstOrDefault();

				context.Dispose();
            }

			return key;
		}

		public static Aes GetAesKey(EncryptionWrapperDIT wrapper)  
        {
			Aes     aes = Aes.Create();
			byte[]? key = null;

			using (DataContext context = new())
            {
				if (wrapper.type == CustomerOrUser.typeCustomer)
				{
					key = context.Customers.Where (cu => cu.Id == wrapper.primaryKey)
										   .Select(cu => cu.aeskey)
										   .FirstOrDefault();
				}
				else
				{
                    key = context.Users.Where(cu => cu.Id == wrapper.primaryKey)
                                       .Select(cu => cu.aeskey)
                                       .FirstOrDefault();
                }

                if (key != null)
				{
					aes.Key		= key;
					aes.IV		= wrapper.aesIV;
                    aes.Mode	= CipherMode.CBC; 
			        aes.Padding = PaddingMode.PKCS7;
                }

                context.Dispose();
            }

            return aes;
        }

		public static byte[] DecryptRSA(string requestEncryptedB64)
		{
            byte[] privateKey = GetServerRSAPrivateKey();

            RSACryptoServiceProvider csp = new RSACryptoServiceProvider(4096);

            int cbRead;
            csp.ImportRSAPrivateKey(privateKey, out cbRead);

            byte[] requestEncrypted = Convert.FromBase64String(requestEncryptedB64);

            byte[] textEncoded = csp.Decrypt(requestEncrypted, false); //PKCS7 padding

            string requestDecryptedB64 = System.Text.Encoding.Unicode.GetString(textEncoded);

            return Convert.FromBase64String(requestDecryptedB64);
        }

		public static void DecodeAndDecryptRequest<T>(EncryptionWrapperDIT wrapper, 
													  out T?			   request)
		{
			request = default(T);

			Aes aes = ServerCryptographyService.GetAesKey(wrapper);

			byte[]? encrypted = Convert.FromBase64String(wrapper.encryptedData);//JsonSerializer.Deserialize<byte[]>(wrapper.encryptedRequest);

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

		public static async Task<string> EncryptAndEncodeResponse<T>(EncryptionWrapperDIT wrapper,
																	 T					  response)
		{
			string responseB64 = null;
			byte[] encrypted   = null;

			Aes aes = ServerCryptographyService.GetAesKey(wrapper);

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

				responseB64 = Convert.ToBase64String(encrypted); //JsonSerializer.Serialize(encrypted);
			} // aes != null

			return responseB64;
		}		
	}
}