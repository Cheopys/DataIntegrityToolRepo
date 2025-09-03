using System;
using System.Security.Cryptography;
using DataIntegrityTool.Db;
using DataIntegrityTool.Schema;
using DataIntegrityTool.Services;
using DataIntegrityTool.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NuGet.Packaging.Core;

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
