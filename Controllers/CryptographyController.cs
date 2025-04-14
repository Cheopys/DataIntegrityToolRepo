using System;
using System.Security.Cryptography;
using DataIntegrityTool.Db;
using DataIntegrityTool.Schema;
using DataIntegrityTool.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NuGet.Packaging.Core;
using ServerCryptography.Service;

namespace DataIntegrityTool.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class CryptographyController : ControllerBase
    {
        static ServerCryptographyService service = new();

        [HttpGet, Route("GetServerPublicKey")]
        public byte[] GetServerPublicKey()
        {
            return ServerCryptographyService.GetServerRSAPublicKey();
        }
    }
}
