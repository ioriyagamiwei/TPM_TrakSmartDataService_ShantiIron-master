using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TPM_TrakSmartDataService_Phantom
{
	static class TPM_TrakSmartDataService_Phantom
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		static void Main()
		{
			//ServiceBase[] ServicesToRun;
			//ServicesToRun = new ServiceBase[]
			//{
			//	new Service1()
			//};
			//ServiceBase.Run(ServicesToRun);

#if (!DEBUG)
            CultureInfo culture = CultureInfo.CreateSpecificCulture("en");
            CultureInfo.DefaultThreadCurrentCulture = culture;
            Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");        
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[] { new Service1() };
            ServiceBase.Run(ServicesToRun);
#else

			Service1 service = new Service1();
			service.StartDebug();
			System.Threading.Thread.Sleep(System.Threading.Timeout.Infinite);
#endif
		}
	}
}
