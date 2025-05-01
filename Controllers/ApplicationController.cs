using Amazon.Runtime.Internal;
using DataIntegrityTool.Db;
using DataIntegrityTool.Schema;
using DataIntegrityTool.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DataIntegrityTool.Services;
using System.Collections.Generic;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using System.Xml.Schema;

public class UploadToolRequest
{
	public string fileB64;
	public string tooltype;
}


namespace DataIntegrityTool.Controllers
{
	[ApiController]
	[Route("[controller]")]

	public class ApplicationController : Controller
	{
		[HttpPut, Route("UploadTool")]
		[Consumes("application/json")]
		public async Task<bool> UploadTool([FromBody] UploadToolRequest request)
		{
			byte[] buffer = Convert.FromBase64String(request.fileB64);

			await S3Service.StoreTool(buffer, request.tooltype);

			return true;
		}


		[HttpGet, Route("DownloadTool")]
		public async Task<byte[]> DownloadTool()
		{
			return await  S3Service.GetTool();
		}
	}
}


