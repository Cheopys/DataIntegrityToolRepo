using Amazon.Runtime.Internal;
using DataIntegrityTool.Db;
using DataIntegrityTool.Schema;
using DataIntegrityTool.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProxChat.Services;
using System.Collections.Generic;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using System.Xml.Schema;

namespace DataIntegrityTool.Controllers
{
	[ApiController]
	[Route("[controller]")]

	public class ApplicationController : Controller
	{
		[HttpPut, Route("UploadTool")]
		public async Task<bool> UploadTool(string filepath)
		{
			byte[] buffer = System.IO.File.ReadAllBytes(filepath);

			S3Service.StoreTool(buffer);

			return true;
		}


		[HttpGet, Route("DownloadTool")]
		public async Task<byte[]> DownloadTool()
		{
			return await  S3Service.GetTool();
			
		}
	}
}


