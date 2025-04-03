using DataIntegrityTool.Schema;
using DataIntegrityTool.Db;
using Microsoft.EntityFrameworkCore;
using NLog;
using System.Diagnostics.Metrics;
using System.Threading.Channels;
using Microsoft.CodeAnalysis;
using System;

namespace DataIntegrityTool.Services
{
	public static class ContentService
	{
		static Logger logger;
		static ContentService()
		{
			var config = new NLog.Config.LoggingConfiguration();

			// Targets where to log to: File and Console
			var logconsole = new NLog.Targets.ConsoleTarget("logconsole");

			// Rules for mapping loggers to targets            
			config.AddRule(NLog.LogLevel.Info, NLog.LogLevel.Fatal, logconsole);

			// Apply config           
			NLog.LogManager.Configuration = config;
			logger = NLog.LogManager.GetCurrentClassLogger();
		}

	} // end class
} // end namespace
