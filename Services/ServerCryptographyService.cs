using Amazon.Runtime.Internal;
using DataIntegrityTool.Db;
using DataIntegrityTool.Schema;
using DataIntegrityTool.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using NLog;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

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


                if (toolparameters			 == null
				||  toolparameters.publicKey == null)
                {
                    RSA rsa = RSA.Create(4096);
                    context.ToolParameters.Add(new ToolParameters
                    {
                        publicKey		= rsa.ExportRSAPublicKey(),
                        privateKey		= rsa.ExportRSAPrivateKey(),
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

		public static Aes CreateAes()
		{
            Aes aes		= Aes.Create();
            aes.KeySize = 256;
            aes.Mode	= CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

			return aes;
        }

        public static Aes GetAesKey(EncryptionWrapperDIT wrapper)  
        {
			Aes     aes = CreateAes();
			byte[]? key = null;

			using (DataContext context = new())
            {
				if (wrapper.type == LoginType.typeAdministrator)
				{
					key = context.Administrators.Where(cu => cu.Id == wrapper.primaryKey)
											    .Select(tp => tp.AesKey)
												.FirstOrDefault();
				}
				else if (wrapper.type == LoginType.typeCustomer)
				{
					key = context.Customers.Where (cu => cu.Id == wrapper.primaryKey)
										   .Select(cu => cu.AesKey)
										   .FirstOrDefault();
				}
				else 
				{
                    key = context.Users.Where(cu => cu.Id == wrapper.primaryKey)
                                       .Select(cu => cu.AesKey)
                                       .FirstOrDefault();
                }

                if (key != null)
				{
					aes.KeySize = 256;
					aes.Key		= key;
					aes.IV		= wrapper.aesIV;
                    aes.Mode	= CipherMode.CBC; 
			        aes.Padding = PaddingMode.PKCS7;
                }

                context.Dispose();
            }

            return aes;
        }

		public static string EncryptRSA(byte[] cleartext)
		{
			byte[] publicKey = GetServerRSAPublicKey();

			RSACryptoServiceProvider csp = new RSACryptoServiceProvider(4096);

			int cbRead;
			csp.ImportRSAPublicKey(publicKey, out cbRead);

			byte[] textEncrypted = csp.Encrypt(cleartext, false); //PKCS7 padding

			return Convert.ToBase64String(textEncrypted);

		}

		public static T DecryptRSA<T>(string requestEncryptedB64)
		{
            byte[] privateKey = GetServerRSAPrivateKey();

            RSACryptoServiceProvider csp = new RSACryptoServiceProvider(4096);

            int cbRead;
            csp.ImportRSAPrivateKey(privateKey, out cbRead);

            byte[] requestEncrypted = Convert.FromBase64String(requestEncryptedB64);

            byte[] textEncoded = csp.Decrypt(requestEncrypted, false); //PKCS7 padding

			return JsonSerializer.Deserialize<T>(textEncoded);
        }

		public static void DecodeAndDecryptLoginRequest(Aes				aes,
			                                            string          requestB64,
													out WebLoginRequest request)
		{
			byte[]? encrypted = Convert.FromBase64String(requestB64);

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

						request = JsonSerializer.Deserialize<WebLoginRequest>(json);

						streamreader.Dispose();
					}

					cryptostream.Dispose();
				}

				memorystream.Dispose();
			}
		}


		public static void	DecodeAndDecryptRequest<T>(EncryptionWrapperDIT wrapper, 
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

		public static async Task<string> EncrypytAES(System.Security.Cryptography.Aes aes, string cleartext)
		{
			string responseB64 = null;
			byte[] encrypted = null;

			ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

			// Create the streams used for encryption.

			using (MemoryStream memorystream = new MemoryStream())
			{
				using (CryptoStream cryptostream = new CryptoStream(memorystream, encryptor, CryptoStreamMode.Write))
				{
					using (StreamWriter streamwriter = new StreamWriter(cryptostream))
					{
						// Write data to the stream.
						streamwriter.Write(cleartext);

						await streamwriter.DisposeAsync();
					}

					await cryptostream.DisposeAsync();
				}

				encrypted = memorystream.ToArray();

				await memorystream.DisposeAsync();
			}

			responseB64 = Convert.ToBase64String(encrypted); //JsonSerializer.Serialize(encrypted);

			return responseB64;
		}

		public static string SHA256(string input)
		{
			string output;
			using (SHA256Managed sha256 = new SHA256Managed())
			{
				byte[] data = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
				output = Convert.ToHexString(data).ToLower();
			}

			return output;
		}

		public static ErrorCodes SetAesKey(LoginType loginType, Int32 id, Byte[] key)
		{
			Users? user;
			Customers? customer;
			Administrators? administrator;
			ErrorCodes errorCodes = ErrorCodes.errorNone;

			using (DataContext context = new())
			{
				switch (loginType)
				{
					case LoginType.typeUser:
						user = context.Users.Where(u => u.Id.Equals(id)).FirstOrDefault();
						user.AesKey = key;
						break;

					case LoginType.typeCustomer:
						customer = context.Customers.Where(u => u.Id.Equals(id)).FirstOrDefault();
						customer.AesKey = key;
						break;

					case LoginType.typeAdministrator:
						administrator = context.Administrators.Where(u => u.Id.Equals(id)).FirstOrDefault();
						administrator.AesKey = key;
						break;

					default:
						errorCodes = ErrorCodes.errorInvalidCustomerId;
						break;
				}
				context.SaveChanges();
				context.Dispose();

				return errorCodes;
			}



		}
	}
}