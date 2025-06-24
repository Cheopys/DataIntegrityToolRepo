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
        
        /*
         * Example
         * 
         * wrapper.primaryKey = Cust/User
         * wrapper.type       = CustomerOrUser.User
         * wrapper.aesIV      = aesIV
         * wrapper,encryo     = null
         */

        [HttpGet, Route("GetAesKey")]
        public Aes GetAesKey(EncryptionWrapperDIT wrapper)
        {
            return ServerCryptographyService.GetAesKey(wrapper);
        }
    }
}
