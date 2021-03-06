﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TPM_TrakSmartDataService_Phantom
{
	class ConnectionManager
	{
		public static bool timeOut = false;
		static string conString = ConfigurationManager.ConnectionStrings["ConnectionString"].ToString();
		static string appPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

		public static SqlConnection GetConnection()
		{
			bool writeDown = false;
			DateTime dt = DateTime.Now;
			SqlConnection conn = new SqlConnection(conString);
			do
			{
				try
				{
					conn.Open();
				}
				catch (Exception ex)
				{
					if (writeDown == false)
					{
						dt = DateTime.Now.AddHours(2);

						Logger.WriteErrorLog(ex.Message);

						writeDown = true;
					}
					if (dt < DateTime.Now)
					{
						Logger.WriteErrorLog(ex.Message);
						writeDown = false;
						timeOut = true;
					}
					Thread.Sleep(1000);
				}
			} while (conn.State != ConnectionState.Open);
			return conn;
		}
	}
}
