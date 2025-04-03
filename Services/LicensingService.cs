﻿using System;
using DataIntegrityTool.Schema;
using DataIntegrityTool.Db;
using Microsoft.EntityFrameworkCore;
using NLog;
using NLog.LayoutRenderers.Wrappers;

namespace DataIntegrityTool.Services
{
	public static class FriendsService
	{
		static Logger logger;
		static FriendsService()
		{
			var config = new NLog.Config.LoggingConfiguration();

			// Targets where to log to: File and Console
			var logconsole = new NLog.Targets.ConsoleTarget("logconsole");

			// Rules for mapping loggers to targets            
			config.AddRule(NLog.LogLevel.Info, NLog.LogLevel.Fatal, logconsole);

			// Apply config           
			NLog.LogManager.Configuration = config;
			logger = LogManager.GetCurrentClassLogger();
		}
	}
}
