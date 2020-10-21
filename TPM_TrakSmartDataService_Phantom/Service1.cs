using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TPM_TrakReportsEngine;

namespace TPM_TrakSmartDataService_Phantom
{
    public partial class Service1 : ServiceBase
    {
        List<Thread> threads = new List<Thread>();
        List<CreateClient> clients = new List<CreateClient>();
        string appPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        private volatile bool stopping = false;
        private static string _password = string.Empty;
        private static string _userName = string.Empty;
        private string _phantomPDIFolderPath;
        private string _compactXPDIFolderPath;
        private string _phantomBatchFolderPath;
        private string _compactXBatchFolderPath;
        private string _destFolderPath;
        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
            Thread.CurrentThread.Name = "TPM_TrakSmartDataService_Phantom";

            if (!Directory.Exists(appPath + "\\Logs\\"))
            {
                Directory.CreateDirectory(appPath + "\\Logs\\");
            }
            if (!Directory.Exists(appPath + "\\TPMFiles\\"))
            {
                Directory.CreateDirectory(appPath + "\\TPMFiles\\");
            }
            List<MachineInfoDTO> machines = DatabaseAccess.GetTPMTrakMachine();
            if (machines.Count == 0)
            {
                Logger.WriteDebugLog("No machine is enabled for TPM-Trak. modify the machine setting and restart the service.");
                return;
            }

            try
            {
                foreach (MachineInfoDTO machine in machines)
                {
                    //MachineInfoDTO machine = machines[0]; //g: test
                    CreateClient client = new CreateClient(machine);
                    clients.Add(client);

                    ThreadStart job = new ThreadStart(client.GetClient);
                    Thread thread = new Thread(job);
                    thread.Name = SafeFileName(machine.MachineId);
                    thread.CurrentCulture = new System.Globalization.CultureInfo("en-US");
                    thread.Start();
                    threads.Add(thread);
                    Logger.WriteDebugLog(string.Format("Machine {0} started for DataCollection", machine.MachineId));

                }
            }
            catch (Exception e)
            {
                Logger.WriteErrorLog(e.ToString());
            }

            try
            {
                ThreadStart job = new ThreadStart(ProcessFinalInspection);
                Thread thread = new Thread(job);
                thread.Name = SafeFileName("FinalInspection");
                thread.CurrentCulture = new System.Globalization.CultureInfo("en-US");
                thread.Start();
                threads.Add(thread);
                Logger.WriteDebugLog(string.Format("Final Inspection started Successfully"));
            }
            catch (Exception e)
            {
                Logger.WriteErrorLog(e.ToString());
            }
        }

        internal void StartDebug()
        {
            OnStart(null);
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
            Exception e = args.ExceptionObject as Exception;
            if (e != null)
            {
                Logger.WriteErrorLog("Unhandled Exception caught : " + e.ToString());
                Logger.WriteErrorLog("Runtime terminating:" + args.IsTerminating);
                var threadName = Thread.CurrentThread.Name;
                Logger.WriteErrorLog("Exception from Thread = " + threadName);
                Process p = Process.GetCurrentProcess();
                StringBuilder str = new StringBuilder();
                if (p != null)
                {
                    str.AppendLine("Total Handle count = " + p.HandleCount);
                    str.AppendLine("Total Threads count = " + p.Threads.Count);
                    str.AppendLine("Total Physical memory usage: " + p.WorkingSet64);

                    str.AppendLine("Peak physical memory usage of the process: " + p.PeakWorkingSet64);
                    str.AppendLine("Peak paged memory usage of the process: " + p.PeakPagedMemorySize64);
                    str.AppendLine("Peak virtual memory usage of the process: " + p.PeakVirtualMemorySize64);
                    Logger.WriteErrorLog(str.ToString());
                }
                Thread.CurrentThread.Abort();
            }
        }

        protected override void OnStop()
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");
            if (string.IsNullOrEmpty(Thread.CurrentThread.Name))
            {
                Thread.CurrentThread.Name = "TPM_TrakSmartDataService_Phantom";
            }

            stopping = true;
            ServiceStop.stop_service = 1;

            Thread.SpinWait(60000 * 10);
            try
            {
                Logger.WriteDebugLog("Service Stop request has come!!! ");
                Logger.WriteDebugLog("Thread count is: " + threads.Count.ToString());
                foreach (Thread thread in threads)
                {
                    if (thread != null && thread.IsAlive)
                    {
                        // Try to stop by allowing the thread to stop on its own.
                        this.RequestAdditionalTime(6000);
                        if (!thread.Join(6000))
                        {
                            thread.Abort();
                            Logger.WriteDebugLog("Aborted.");
                        }
                    }
                }
                threads.Clear();
            }
            catch (Exception ex)
            {
                Logger.WriteErrorLog(ex.ToString());
            }
            finally
            {
                clients.Clear();
            }
            Logger.WriteDebugLog("Service has stopped.");
        }

        private void ProcessFinalInspection()
        {
            NetworkConnection nc_PhantomPDI = null;
            NetworkConnection nc_CompactXPDI = null;
            NetworkConnection nc_PhantomBatch = null;
            NetworkConnection nc_CompactXBatch = null;
            _phantomPDIFolderPath = ConfigurationManager.AppSettings["PhantomPDIFilePath"].ToString();
            _compactXPDIFolderPath = ConfigurationManager.AppSettings["CompactXPDIFilePath"].ToString();
            _phantomBatchFolderPath = ConfigurationManager.AppSettings["PhantomBatchFilePath"].ToString();
            _compactXBatchFolderPath = ConfigurationManager.AppSettings["CompactXBatchFilePath"].ToString();
            _destFolderPath = ConfigurationManager.AppSettings["DestinationFilePath"].ToString();

            var PhantomPDIDirectory = new DirectoryInfo(_phantomPDIFolderPath);
            var CompactXPDIDirectory = new DirectoryInfo(_compactXPDIFolderPath);
            var PhantomBatchDirectory = new DirectoryInfo(_phantomBatchFolderPath);
            var CompactXBatchDirectory = new DirectoryInfo(_compactXBatchFolderPath);
            var DestDirectory = new DirectoryInfo(_destFolderPath);
            int curStatus = -1, prevStatus = -1;
            DateTime lastSentLogs = DateTime.Now.AddMinutes(-10);
            bool networkConnection = false;
            while (true)
            {
                if (ServiceStop.stop_service == 1)
                    break;

                #region Network Connectivity
                if (!networkConnection)
                {
                    _userName = ConfigurationManager.AppSettings["PhantomPDI_UserID_Fileshare"].ToString();
                    _password = ConfigurationManager.AppSettings["PhantomPDI_Password_Fileshare"].ToString();
                    if (!string.IsNullOrEmpty(_userName) && !string.IsNullOrEmpty(_password))
                    {
                        try
                        {
                            nc_PhantomPDI = new NetworkConnection(this._phantomPDIFolderPath, new NetworkCredential(_userName, _password));

                            _userName = ConfigurationManager.AppSettings["CompactXPDI_UserID_Fileshare"].ToString();
                            _password = ConfigurationManager.AppSettings["CompactXPDI_Password_Fileshare"].ToString();
                            nc_CompactXPDI = new NetworkConnection(this._compactXPDIFolderPath, new NetworkCredential(_userName, _password));

                            _userName = ConfigurationManager.AppSettings["PhantomBatch_UserID_Fileshare"].ToString();
                            _password = ConfigurationManager.AppSettings["PhantomBatch_Password_Fileshare"].ToString();
                            nc_PhantomBatch = new NetworkConnection(this._phantomBatchFolderPath, new NetworkCredential(_userName, _password));

                            _userName = ConfigurationManager.AppSettings["CompactXBatch_UserID_Fileshare"].ToString();
                            _password = ConfigurationManager.AppSettings["CompactXBatch_Password_Fileshare"].ToString();
                            nc_CompactXBatch = new NetworkConnection(this._phantomBatchFolderPath, new NetworkCredential(_userName, _password));

                            curStatus = 1;
                            if ((curStatus != prevStatus) || DateTime.Now >= lastSentLogs)
                            {
                                Logger.WriteDebugLog("Connection has been established to the Network Path Folder.");
                                prevStatus = curStatus;
                                lastSentLogs = DateTime.Now.AddMinutes(10);
                            }
                            networkConnection = true;
                        }
                        catch (Exception exx)
                        {
                            curStatus = 2;
                            networkConnection = false;
                            if (nc_PhantomPDI != null)
                                nc_PhantomPDI.Dispose();

                            if (nc_PhantomBatch != null)
                                nc_PhantomBatch.Dispose();

                            if (nc_CompactXPDI != null)
                                nc_CompactXPDI.Dispose();

                            if (nc_CompactXBatch != null)
                                nc_CompactXBatch.Dispose();

                            if ((curStatus != prevStatus) || DateTime.Now >= lastSentLogs)
                            {
                                Logger.WriteErrorLog(exx.ToString());
                                prevStatus = curStatus;
                                lastSentLogs = DateTime.Now.AddMinutes(10);
                            }

                        }
                    }
                } 
                #endregion

                #region Phantom PDI File Processing
                try
                {
                    var AllPhantomPDIFiles = PhantomPDIDirectory.GetFiles("*.csv", SearchOption.TopDirectoryOnly);
                    foreach (FileInfo f in AllPhantomPDIFiles)
                    {
                        try
                        {
                            var fileName = f.Name;
                            var splitsValue = fileName.Split('_');
                            string Component = splitsValue[1];
                            string SerialNo = splitsValue[2];
                            var data = File.ReadAllText(f.FullName).Trim();
                            if (!string.IsNullOrEmpty(data))
                            {
                                Logger.WriteDebugLog(string.Format("Data from PDI file In Phantom Folder : {0}", data));
                                DatabaseAccess.UpdatePackingStation_Shanti("Phantom", Component, SerialNo, f.FullName, "PDI");
                            }
                            else
                            {
                                Logger.WriteDebugLog(string.Format("PDI File is empty : {0}", this._phantomPDIFolderPath));
                            }

                            var phantomProcessedPath = Path.Combine(_phantomPDIFolderPath, "Processed", f.Name);
                            MoveWithReplace(f.FullName, phantomProcessedPath);
                            Logger.WriteDebugLog("Files are moved to Processed folder successfully.");
                            File.Copy(f.FullName, Path.Combine(_destFolderPath, f.Name));
                            Logger.WriteDebugLog("Files are copied to Destination folder successfully.");
                        }
                        catch (Exception ex)
                        {
                            curStatus = 2;
                            networkConnection = false;
                            if (nc_PhantomPDI != null)
                                nc_PhantomPDI.Dispose();

                            if ((curStatus != prevStatus) || DateTime.Now >= lastSentLogs)
                            {
                                Logger.WriteErrorLog(ex.Message);
                                prevStatus = curStatus;
                                lastSentLogs = DateTime.Now.AddMinutes(10);
                            }

                        }
                    }
                }
                catch (Exception ex)
                {
                    curStatus = 2;
                    networkConnection = false;
                    if (nc_PhantomPDI != null)
                        nc_PhantomPDI.Dispose();

                    if ((curStatus != prevStatus) || DateTime.Now >= lastSentLogs)
                    {
                        Logger.WriteErrorLog(ex.Message);
                        prevStatus = curStatus;
                        lastSentLogs = DateTime.Now.AddMinutes(10);
                    }
                }
                #endregion

                #region CompactX PDI File Processing
                try
                {
                    var AllCompactXPDIFiles = CompactXPDIDirectory.GetFiles("*.csv", SearchOption.TopDirectoryOnly);
                    foreach (FileInfo f in AllCompactXPDIFiles)
                    {
                        try
                        {
                            var fileName = f.Name;
                            var splitsValue = fileName.Split('_');
                            string Component = splitsValue[1];
                            string SerialNo = splitsValue[2];
                            var data = File.ReadAllText(f.FullName).Trim();
                            if (!string.IsNullOrEmpty(data))
                            {
                                Logger.WriteDebugLog(string.Format("Data from PDI file In CompactX Folder : {0}", data));
                                DatabaseAccess.UpdatePackingStation_Shanti("CompactX", Component, SerialNo, f.FullName, "PDI");
                            }
                            else
                            {
                                Logger.WriteDebugLog(string.Format("PDI File is empty : {0}", this._compactXPDIFolderPath));
                            }

                            var CompactXProcessedPath = Path.Combine(_compactXPDIFolderPath, "Processed", f.Name);
                            MoveWithReplace(f.FullName, CompactXProcessedPath);
                            Logger.WriteDebugLog("Files are moved to Processed folder successfully.");
                            File.Copy(f.FullName, Path.Combine(_destFolderPath, f.Name));
                            Logger.WriteDebugLog("Files are copied to Destination folder successfully.");
                        }
                        catch (Exception ex)
                        {
                            curStatus = 2;
                            networkConnection = false;
                            if (nc_CompactXPDI != null)
                                nc_CompactXPDI.Dispose();

                            if ((curStatus != prevStatus) || DateTime.Now >= lastSentLogs)
                            {
                                Logger.WriteErrorLog(ex.Message);
                                prevStatus = curStatus;
                                lastSentLogs = DateTime.Now.AddMinutes(10);
                            }

                        }
                    }
                }
                catch (Exception ex)
                {
                    curStatus = 2;
                    networkConnection = false;
                    if (nc_CompactXPDI != null)
                        nc_CompactXPDI.Dispose();

                    if ((curStatus != prevStatus) || DateTime.Now >= lastSentLogs)
                    {
                        Logger.WriteErrorLog(ex.Message);
                        prevStatus = curStatus;
                        lastSentLogs = DateTime.Now.AddMinutes(10);
                    }
                }
                #endregion

                #region Phantom Batch File Processing
                try
                {
                    var AllPhantomBatchFiles = PhantomBatchDirectory.GetFiles("*.csv", SearchOption.TopDirectoryOnly);
                    foreach (FileInfo f in AllPhantomBatchFiles)
                    {
                        try
                        {
                            var fileName = f.Name;
                            var splitsValue = fileName.Split('_');
                            string Component = splitsValue[1];
                            string SerialNo = splitsValue[2];
                            var data = File.ReadAllText(f.FullName).Trim();
                            if (!string.IsNullOrEmpty(data))
                            {
                                var linedata = File.ReadAllLines(f.FullName);
                                foreach (var slNo in linedata)
                                {
                                    DatabaseAccess.UpdatePackingStation_Shanti("Phantom", Component, slNo, f.FullName, "BATCH");
                                }
                                //Logger.WriteDebugLog(string.Format("Data from Batch file In Phantom Folder : {0}", data));
                                //DatabaseAccess.UpdatePackingStation_Shanti("Phantom", Component, SerialNo, f.FullName, "BATCH");
                            }
                            else
                            {
                                Logger.WriteDebugLog(string.Format("Batch File is empty : {0}", this._phantomBatchFolderPath));
                            }

                            var phantomProcessedPath = Path.Combine(_phantomBatchFolderPath, "Processed", f.Name);
                            MoveWithReplace(f.FullName, phantomProcessedPath);
                            Logger.WriteDebugLog("Files are moved to Processed folder successfully.");
                            File.Copy(f.FullName, Path.Combine(_destFolderPath, f.Name));
                            Logger.WriteDebugLog("Files are copied to Destination folder successfully.");
                        }
                        catch (Exception ex)
                        {
                            curStatus = 2;
                            networkConnection = false;
                            if (nc_PhantomBatch != null)
                                nc_PhantomBatch.Dispose();

                            if ((curStatus != prevStatus) || DateTime.Now >= lastSentLogs)
                            {
                                Logger.WriteErrorLog(ex.Message);
                                prevStatus = curStatus;
                                lastSentLogs = DateTime.Now.AddMinutes(10);
                            }

                        }
                    }
                }
                catch (Exception ex)
                {
                    curStatus = 2;
                    networkConnection = false;
                    if (nc_PhantomBatch != null)
                        nc_PhantomBatch.Dispose();

                    if ((curStatus != prevStatus) || DateTime.Now >= lastSentLogs)
                    {
                        Logger.WriteErrorLog(ex.Message);
                        prevStatus = curStatus;
                        lastSentLogs = DateTime.Now.AddMinutes(10);
                    }
                }
                #endregion

                #region CompactX Batch File Processing
                try
                {
                    var AllCompactXBatchFiles = CompactXBatchDirectory.GetFiles("*.csv", SearchOption.TopDirectoryOnly);
                    foreach (FileInfo f in AllCompactXBatchFiles)
                    {
                        try
                        {
                            var fileName = f.Name;
                            var splitsValue = fileName.Split('_');
                            string Component = splitsValue[1];
                            string SerialNo = splitsValue[2];
                            var data = File.ReadAllText(f.FullName).Trim();
                            if (!string.IsNullOrEmpty(data))
                            {
                                var linedata = File.ReadAllLines(f.FullName);
                                foreach (var slNo in linedata)
                                {
                                    DatabaseAccess.UpdatePackingStation_Shanti("CompactX", Component, slNo, f.FullName, "BATCH");
                                }
                                //Logger.WriteDebugLog(string.Format("Data from Batch file In CompactX Folder : {0}", data));
                                //DatabaseAccess.UpdatePackingStation_Shanti("CompactX", Component, SerialNo, f.FullName, "BATCH");
                            }
                            else
                            {
                                Logger.WriteDebugLog(string.Format("Batch File is empty : {0}", this._compactXBatchFolderPath));
                            }

                            var CompactXProcessedPath = Path.Combine(_compactXBatchFolderPath, "Processed", f.Name);
                            MoveWithReplace(f.FullName, CompactXProcessedPath);
                            Logger.WriteDebugLog("Files are moved to Processed folder successfully.");
                            File.Copy(f.FullName, Path.Combine(_destFolderPath, f.Name));
                            Logger.WriteDebugLog("Files are copied to Destination folder successfully.");
                        }
                        catch (Exception ex)
                        {
                            curStatus = 2;
                            networkConnection = false;
                            if (nc_CompactXBatch != null)
                                nc_CompactXBatch.Dispose();

                            if ((curStatus != prevStatus) || DateTime.Now >= lastSentLogs)
                            {
                                Logger.WriteErrorLog(ex.Message);
                                prevStatus = curStatus;
                                lastSentLogs = DateTime.Now.AddMinutes(10);
                            }

                        }
                    }
                }
                catch (Exception ex)
                {
                    curStatus = 2;
                    networkConnection = false;
                    if (nc_CompactXBatch != null)
                        nc_CompactXBatch.Dispose();

                    if ((curStatus != prevStatus) || DateTime.Now >= lastSentLogs)
                    {
                        Logger.WriteErrorLog(ex.Message);
                        prevStatus = curStatus;
                        lastSentLogs = DateTime.Now.AddMinutes(10);
                    }
                }
                #endregion
            }

        }
        public static void MoveWithReplace(string sourceFileName, string destFileName)
        {

            //first, delete target file if exists, as File.Move() does not support overwrite
            if (File.Exists(destFileName))
            {
                try
                {
                    File.Delete(destFileName);

                }
                catch (Exception exx)
                {
                    Logger.WriteErrorLog(exx.Message);
                }
            }
            try
            {
                File.Move(sourceFileName, destFileName);
            }
            catch (Exception exx)
            {
                Logger.WriteDebugLog(exx.Message);
            }
        }
        public string SafeFileName(string name)
        {
            StringBuilder str = new StringBuilder(name);
            foreach (char c in System.IO.Path.GetInvalidFileNameChars())
            {
                str = str.Replace(c, '_');
            }
            return str.ToString();
        }
    }
}
