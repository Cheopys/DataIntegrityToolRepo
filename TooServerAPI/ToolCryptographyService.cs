﻿using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ToolCryptographyService
{
	public class EncryptionWrapperDIT
	{
		public Int32 sessionId { get; set; }
		
		public string encryptedRequest { get; set; }
	}

    public class ToolCryptographyService
	{
		public static string CreateClientKeys()
		{
			// create the client AES key

			Aes aes = Aes.Create();
			aes.KeySize = 256;
			aes.GenerateKey();
			aes.GenerateIV();

			RegisterClientRequest request = new()
			{
				aeskey = aes.Key,
				aesiv  = aes.IV
			};

			return JsonSerializer.Serialize(request);
		}

		public async Task<string> HashPassword(string passwordClear)
		{
			string passwordB64 = null;

			using (MemoryStream memstream = new())
			{
				byte[] passwordAsBytes = Encoding.ASCII.GetBytes(passwordClear);
				memstream.Write(passwordAsBytes, 0, passwordAsBytes.Length);
				memstream.Position = 0;
				byte[] hashValue = await SHA256.Create().ComputeHashAsync(memstream);

				passwordB64 = Convert.ToBase64String(hashValue);
			}

			return passwordB64;
		}

		public static async Task<EncryptionWrapperDIT?> EncodeAndEncryptRequest<T>(Int32   session, 
																	 byte[]  aeskey, 
																	 byte[]  aesiv, 
																	 T		 request,
																	 bool    bypass = false)
		{
			string json = JsonSerializer.Serialize(request);
			EncryptionWrapperDIT wrapper = new()
			{
				sessionId = session
			};

			if (bypass)
			{
				wrapper.encryptedRequest = JsonSerializer.Serialize(request);
			}
			else
			{
				using (Aes aes = Aes.Create())
				{
					ICryptoTransform encryptor = aes.CreateEncryptor(aeskey, aesiv);

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

						byte[] encrypted = memorystream.ToArray();

						wrapper.encryptedRequest = Convert.ToBase64String(encrypted); //JsonSerializer.Serialize(encrypted); //Convert.ToHexString(encrypted);

						await memorystream.DisposeAsync();
					}

					aes.Dispose();
				}
			}
			return wrapper;
		}

		public static string DecodeAndDecryptResponse<T>(string encryptedB64,
													     byte[] aesKey,
													     byte[] aesIV,
													     bool bypass = false)
		{
			string json = String.Empty;

			if (bypass)
			{
				json = encryptedB64; ;/*response = JsonSerializer.Deserialize<T>(encryptedJSON);*/
			}
			else
			{
				byte[]? encrypted = Convert.FromBase64String(encryptedB64);// JsonSerializer.Deserialize<byte[]>(encryptedJSON);

				ICryptoTransform decryptor = Aes.Create().CreateDecryptor(aesKey, aesIV);

				// Create the streams used for encryption.

				using (MemoryStream memorystream = new MemoryStream(encrypted))
				{
					using (CryptoStream cryptostream = new CryptoStream(memorystream, decryptor, CryptoStreamMode.Read))
					{
						using (StreamReader streamreader = new StreamReader(cryptostream))
						{
							// Write data to the stream.
							json = streamreader.ReadToEnd();

							streamreader.Dispose();
						}

						cryptostream.Dispose();
					}

					memorystream.Dispose();
				} // end using memory stream
			}
			
			return json;
		} // end function

		// for later: create a distinct initialization vector for each encrypted call

		public byte[] CreateInitializationVector(string seed)
		{
			byte[] data = SHA1.Create().ComputeHash(UTF8Encoding.UTF8.GetBytes(seed));

			string hex = Convert.ToHexString(data);

			hex = hex.Substring(0, 32);

			return Convert.FromHexString(hex);
		}
	}
}
