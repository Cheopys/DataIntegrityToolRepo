using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using DataIntegrityTool.Shared;

// TO BE RUN ON THE TOOL; MUST HAVE NO DEPENDENCIES OUTSIDE NUGET

namespace DataIntegrityTool.Services
{
	public class ToolCryptographyService
	{
        public string GetServerPublicKey()
        {
            return ServerCryptographyService.GetServerRSAPublicKey();
        }
        public static Aes CreateAesKey()
        {
            Aes aes		= Aes.Create();
            aes.KeySize = 256;
			aes.Mode	= CipherMode.CBC;
			aes.Padding = PaddingMode.PKCS7;

            return aes;
        }

        public async Task<string> HashPassword(string passwordClear)
		{
			string passwordHex = null;

			using (MemoryStream memstream = new())
			{
				byte[] passwordAsBytes = Encoding.UTF8.GetBytes(passwordClear);
				memstream.Write(passwordAsBytes, 0, passwordAsBytes.Length);
				memstream.Position = 0;
				byte[] hashValue = await SHA256.Create().ComputeHashAsync(memstream);

				passwordHex = Convert.ToHexString(hashValue);
			}

			return passwordHex;
		}

		public static async Task<EncryptionWrapperDIT?> EncodeAndEncryptRequest<T>(EncryptionWrapperDIT wrapperIn, 
																				   T					request)
		{
			string json = JsonSerializer.Serialize(request);
			Aes aes		= ServerCryptographyService.GetAesKey(wrapperIn);

			EncryptionWrapperDIT wrapper = new()
			{
				primaryKey  = wrapperIn.primaryKey,
				type		= wrapperIn.type,	
				aesIV		= wrapperIn.aesIV
			};

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

				byte[] encrypted = memorystream.ToArray();

				wrapper.encryptedData = Convert.ToBase64String(encrypted); //JsonSerializer.Serialize(encrypted); //Convert.ToHexString(encrypted);

				await memorystream.DisposeAsync();
			}

			return wrapper;
		}

		public static string DecodeAndDecryptResponse<T>(EncryptionWrapperDIT wrapper,
														 byte[]               aeskey)
		{
			string json = String.Empty;

			byte[]? encrypted = Convert.FromBase64String(wrapper.encryptedData);

			ICryptoTransform decryptor = Aes.Create().CreateDecryptor(aeskey, wrapper.aesIV);

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
