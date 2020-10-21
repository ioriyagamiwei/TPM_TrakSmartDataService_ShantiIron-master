using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TPM_TrakSmartDataService_Phantom
{
	class CleanUpProcess
	{
		static string appPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
		public static void RenameLogFiles()
		{
			string progTime = String.Format("_{0:yyyyMMdd}", DateTime.Now);
			string location = appPath + "\\Logs\\F-" + Thread.CurrentThread.Name + progTime + "-Status.txt";
			FileInfo f = new FileInfo(location);
			if (f.Exists && f.Length > 2097152)
			{
				string newfile = appPath + "\\Logs\\" + (Path.GetFileNameWithoutExtension(f.Name)) + String.Format("_{0:HHmmss}", DateTime.Now) + ".txt";// + String.Format("{0:HHmmss}", DateTime.Now));
				try
				{
					f.MoveTo(newfile);
					Thread.Sleep(1000);
					//Logger.WriteDebugLog( string.Format("File {0} has been renamed to {1}.", location, newfile));
				}
				catch (Exception ex)
				{
					//Logger.WriteErrorLog(" RenameLogFiles(): " + ex.Message);
				}
			}
		}
	}
}
