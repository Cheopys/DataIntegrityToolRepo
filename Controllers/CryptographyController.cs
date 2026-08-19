using DataIntegrityTool.Services;
using Microsoft.AspNetCore.Mvc;

namespace DataIntegrityTool.Controllers
{
    [Route("[controller]")]
    [ApiController]

    public class CryptographyController : ControllerBase
    {
        
        [HttpGet, Route("GetServerRSAPublicKey")]
        public string GetServerPublicKey()
        {
            return Convert.ToBase64String(ServerCryptographyService.GetServerRSAPublicKey());
        }
        
		[HttpGet, Route("CreateAesIV")]
		public string CreateAesIV()
		{
			return Convert.ToHexString(ServerCryptographyService.CreateAes().IV);
		}
	}
}
