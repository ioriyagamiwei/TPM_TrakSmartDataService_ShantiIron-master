using Modbus.Device;
using Modbus.Utility;
using S7.Net;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using TPM_TrakReportsEngine;

namespace TPM_TrakSmartDataService_Phantom
{
    class CreateClient
    {
        private string _ipAddress;
        private string _machineId;
        private int _portNo;
        private int _opnID;
        private string _interfaceId;
        private string _reqFolderPath;
        private string _resFolderPath;
        private string _ackFolderPath;
        private string _spcFolderPath;
        private string _protocol;
        private string MName;
        MachineInfoDTO _machineDTO = null;


        private static string appPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        private static DateTime _serviceStartedTimeStamp = DateTime.Now;
        private static DateTime _nextLicCheckedTimeStamp = _serviceStartedTimeStamp;
        private static string _password = string.Empty;
        private static string _userName = string.Empty;

        Plc PhantomClient = null;
        Plc LeakTestClient = null;
        Plc WashingMachineClient = null;
        Plc sPhantomClient = null;
        Plc RoboClient = null;
        private Socket socket = default(Socket);
        bool PhantomStatus = false;
        //Modbus
        string cur_id;
        ushort commNo = 100;
        private readonly ushort holdingRegisterStartAddress = 100;
        private readonly ushort numberOfBytesToRead = 70;
        private readonly ushort AckHoldingRegisterAddress = 200;
        private readonly ushort DateAndStatusAckAddress = 700;
        private readonly ushort HoldingRegisterDateAndStatus = 400;
        private readonly ushort HoldingRegister28TypeString = 300;
        private readonly ushort BytesToRead28Type = 70;
        private readonly ushort AckAddressFor28TypeString = 500;
        private readonly ushort HoldingRegisterForCommunictaion = 600;

        //DailyCheckList for Shanthi Iron
        private readonly ushort HoldingRegisterDailyCheckList = 450;//Date and time :452 and 454 ......
        private readonly ushort HoldingFlagsRegisterDailyCheckList = 540;
        List<dailyCheckList> dailyChkLsts = new List<dailyCheckList>();

        //PM CheckList for Shanthi Iron
        #region PM CheckList Holding Regs Address
        private readonly ushort HoldingRegisterPMCheckList70Perc = 532;
        private readonly ushort HoldingRegisterPMCheckListOKNOTOK = 533;
        private readonly ushort HoldingRegisterPMCheckListAllDone = 534;

        private readonly ushort HoldingRegisterStr56PMMachineID = 501;
        private readonly ushort HoldingRegisterStr56PMSelectionCode = 502;
        private readonly ushort HoldingRegisterStr56PMTarget = 504;
        private readonly ushort HoldingRegisterStr56PMActual = 506;
        private readonly ushort HoldingRegisterStr56PMPercentage = 503;
        private readonly ushort HoldingRegisterStr56PMCurDate = 508;
        private readonly ushort HoldingRegisterStr56PMCurTime = 510;

        private readonly ushort HoldingRegisterStr57PMMachineID = 512;
        private readonly ushort HoldingRegisterStr57PMOprID = 513;
        private readonly ushort HoldingRegisterStr57PMCatID = 514;
        private readonly ushort HoldingRegisterStr57PMSubCatID = 515;
        private readonly ushort HoldingRegisterStr57PMSelectionCode = 516;
        private readonly ushort HoldingRegisterStr57PMNOKreason = 517;
        private readonly ushort HoldingRegisterStr57PMTarget = 518;
        private readonly ushort HoldingRegisterStr57PMActual = 520;
        private readonly ushort HoldingRegisterStr57PMCurDate = 522;
        private readonly ushort HoldingRegisterStr57PMCurTime = 524;

        private readonly ushort HoldingRegisterStr59PMMachineID = 526;
        private readonly ushort HoldingRegisterStr59PMOprID = 527;
        private readonly ushort HoldingRegisterStr59PMCurDate = 528;
        private readonly ushort HoldingRegisterStr59PMCurTime = 530;

        #endregion

        #region Process Patameter Operation For Shanti Iron
        ushort HoldingRegisterReadParameter = (ushort)1325;
        ushort HoldingRegisterReadParamValue = (ushort)1550;
        ushort[] processStrToWrite;
        ushort HoldingRegisterWriteParameter = (ushort)1100;
        List<string> processParameter_Strings = new List<string>();
        #endregion

        private readonly ushort HoldingRegister6_7TypeString;
        private readonly ushort BytesToRead6_7Type;
        private readonly ushort AckAddressFor6_7TypeString;

        string UnprocessedFilePath = Path.Combine(appPath,"SPC", "Unprocessed");
        string ProcessedFilePath = Path.Combine(appPath, "SPC", "Processed");
        CultureInfo enUS = new CultureInfo("en-US");
      
        public CreateClient(MachineInfoDTO machine)
        {
            this._machineDTO = machine;
            this._ipAddress = machine.IpAddress;
            this._portNo = machine.PortNo;
            this._opnID = machine.OpnID;
            this._machineId = machine.MachineId;
            this._interfaceId = machine.InterfaceId;
            this._protocol = string.IsNullOrEmpty(machine.DataCollectionProtocol) ? "RAW" : machine.DataCollectionProtocol;

            //if (!machine.Process.Equals("Equator(Phantom)", StringComparison.OrdinalIgnoreCase))
            //{
            //    if (!string.IsNullOrEmpty(machine.REQFolderPath))
            //        this._reqFolderPath = Path.Combine(machine.REQFolderPath, "UnProcessed");
            //    if (!string.IsNullOrEmpty(machine.RESFolderPath))
            //        this._resFolderPath = Path.Combine(machine.RESFolderPath, "UnProcessed");
            //    if (!string.IsNullOrEmpty(machine.ACKFolderPath))
            //        this._ackFolderPath = Path.Combine(machine.ACKFolderPath, "UnProcessed");
            //}
            //else
            //{
            //    if (!string.IsNullOrEmpty(machine.REQFolderPath))
            //        this._reqFolderPath = machine.REQFolderPath;
            //    if (!string.IsNullOrEmpty(machine.RESFolderPath))
            //        this._resFolderPath = machine.RESFolderPath;
            //    if (!string.IsNullOrEmpty(machine.ACKFolderPath))
            //        this._ackFolderPath = machine.ACKFolderPath;
               
            //}

            if (!string.IsNullOrEmpty(machine.REQFolderPath))
                this._reqFolderPath = Path.Combine(machine.REQFolderPath, "UnProcessed");
            if (!string.IsNullOrEmpty(machine.RESFolderPath))
                this._resFolderPath = Path.Combine(machine.RESFolderPath, "UnProcessed");
            if (!string.IsNullOrEmpty(machine.ACKFolderPath))
                this._ackFolderPath = Path.Combine(machine.ACKFolderPath, "UnProcessed");
            this._spcFolderPath = machine.SPCFolderPath;
            this.MName = machine.MachineId;

            //MODBUS
            holdingRegisterStartAddress = machine.HoldingRegisterStartAddress_M1;
            numberOfBytesToRead = machine.BytesToRead_M1;
            AckHoldingRegisterAddress = machine.AckAddress_M1;

            HoldingRegister28TypeString = machine.HoldingRegisterStartAddress_M2;
            BytesToRead28Type = machine.BytesToRead_M2;
            AckAddressFor28TypeString = machine.AckAddress_M2;

            HoldingRegister6_7TypeString = machine.HoldingRegisterStartAddress_M3;
            BytesToRead6_7Type = machine.BytesToRead_M3;
            AckAddressFor6_7TypeString = machine.AckAddress_M3;

            HoldingRegisterForCommunictaion = machine.HoldingRegisterForCommunication;
            HoldingRegisterDateAndStatus = machine.HoldingRegisterDateAndStatus;
            DateAndStatusAckAddress = machine.HoldingRegisterDateAndStatusAckAddress;

            cur_id = "0";
            dailyChkLsts = DatabaseAccess.getCheckListMaster();

            try
            {
                //if (!Directory.Exists(this.UnprocessedFilePath))
                //{
                //    CreateDir(UnprocessedFilePath);
                //}
                //if (!Directory.Exists(this.ProcessedFilePath))
                //{
                //    CreateDir(ProcessedFilePath);
                //}

                //if (!Directory.Exists(this._reqFolderPath))
                //{
                //    CreateDir(this._reqFolderPath);
                //}
                //if (!Directory.Exists(this._ackFolderPath))
                //{
                //    CreateDir(_ackFolderPath);
                //}
                //if (!Directory.Exists(this._resFolderPath))
                //{
                //    CreateDir(_resFolderPath);
                //}
                //if (!Directory.Exists(this._spcFolderPath))
                //{
                //    CreateDir(_spcFolderPath);
                //}

               
            }
            catch(Exception exx)
            {
                Logger.WriteErrorLog(exx.Message);
            }

        }

        internal void GetClient()
        {
            while (true)
            {
                try
                {
                    #region stop_service                   
                    if (ServiceStop.stop_service == 1)
                    {
                        try
                        {
                            Logger.WriteDebugLog("stopping the service. coming out of main while loop.");
                            break;
                        }
                        catch (Exception ex)
                        {
                            Logger.WriteErrorLog(ex.ToString());
                            break;
                        }
                    }
                    #endregion

                    #region "CSV"
                    if (_protocol.Equals("csv", StringComparison.OrdinalIgnoreCase))
                    {
                        Logger.WriteDebugLog("Entered into CSV.");
                        #region "Laser Marking Machine"
                        if (_machineDTO.Process.Equals("LaserMarkingMachine", StringComparison.OrdinalIgnoreCase))
                        {
                            string REQsourceFile = string.Empty;
                            string ACKFilePath = string.Empty;
                            string RESsourceFile = string.Empty;
                            string REQDestFile = string.Empty;
                            string RESDestFile = string.Empty;
                            string REQData = string.Empty;
                            string RESData = string.Empty;
                            string PartNumber = string.Empty;
                            string PartSerialNumber = string.Empty;
                            string SupplierCode = string.Empty;
                            string HeatCode = string.Empty;
                            string RevNumber = string.Empty;
                            string DataString = string.Empty;
                            string Comp = string.Empty;
                            int OperatorID;
                            int SupervisorID;
                            string STDATE = DateTime.Now.ToString("yyyyMMdd");
                            string STTIME = DateTime.Now.ToString("HHmmss");
                            int Output = -1;
                            int prevStatus = -1, curStatus = 1;
                            DateTime lastSentLogs = DateTime.Now.AddMinutes(-10);

                            NetworkConnection nc = null;
                            _password = ConfigurationManager.AppSettings["LMM_Password_Fileshare"].ToString();
                            _userName = ConfigurationManager.AppSettings["LMM_UserID_Fileshare"].ToString();
                            bool networkConnection = false;
                            Logger.WriteDebugLog("Reading LASER marking machine data.....");
                            var REQdirectory = new DirectoryInfo(this._reqFolderPath);
                            var RESdirectory = new DirectoryInfo(this._resFolderPath);
                            while (true)
                            {
                                if (ServiceStop.stop_service == 1)
                                    break;
                                if (!networkConnection)
                                {
                                    if (!string.IsNullOrEmpty(_userName) && !string.IsNullOrEmpty(_password))
                                    {
                                        try
                                        {
                                            nc = new NetworkConnection(this._reqFolderPath, new NetworkCredential(_userName, _password));
                                            networkConnection = true;
                                            curStatus = 1;
                                            if ((curStatus != prevStatus) || DateTime.Now >= lastSentLogs)
                                            {
                                                Logger.WriteDebugLog("Connection has been established to the Network Path Folder.");
                                                prevStatus = curStatus;
                                                lastSentLogs = DateTime.Now.AddMinutes(10);
                                            }
                                        }
                                        catch (Exception exx)
                                        {
                                            curStatus = 2;
                                            networkConnection = false;
                                            if (nc != null)
                                                nc.Dispose();

                                            if ((curStatus != prevStatus) || DateTime.Now >= lastSentLogs)
                                            {
                                                Logger.WriteErrorLog(exx.ToString());
                                                prevStatus = curStatus;
                                                lastSentLogs = DateTime.Now.AddMinutes(10);
                                            }
                                            
                                        }
                                    }
                                }
                                
                                try
                                {
                                    var AllREQFiles = REQdirectory.GetFiles("REQ*.txt", SearchOption.TopDirectoryOnly);
                                    foreach (FileInfo f in AllREQFiles)
                                    {
                                        try
                                        {
                                            //process file and move file to unprocessed folder
                                            //REQData : partno-revno-partname-suppliercode-partslNo-heatcode
                                            REQData = File.ReadAllText(f.FullName).Trim();
                                            if (!string.IsNullOrEmpty(REQData))
                                            {
                                                Logger.WriteDebugLog(string.Format("Data from REQ file : {0}", REQData));
                                                string[] spl = REQData.Split(new string[] { "-" }, StringSplitOptions.None);

                                                PartNumber = spl[0].Trim();
                                                RevNumber = spl[1].Trim();
                                                PartSerialNumber = spl[4].Trim();
                                                STDATE = f.LastWriteTime.ToString("yyyyMMdd");
                                                STTIME = f.LastWriteTime.ToString("HHmmss");

                                                //START-28-21-[3539598-DX00]-20-N112F-20200606-103850-END
                                                DataString = string.Format("START-28-{0}-[{1}-{2}]-{3}-{4}-{5}-{6}-END", this._interfaceId, PartNumber, RevNumber, this._opnID.ToString(), PartSerialNumber, STDATE, STTIME);
                                                SaveStringToTPMFile(DataString);
                                                Logger.WriteDebugLog(string.Format("Req Validation string : {0}", DataString));
                                                string tempString = DatabaseAccess.GetStatus28Type(DataString.Trim());
                                                Logger.WriteDebugLog("Req Validation Result" + tempString);
                                                if (!string.IsNullOrEmpty(tempString))
                                                {
                                                    //string DataToWrite = PartNumber + ", " + PartSerialNumber + ", " + getValidationResult(tempString.ToString());
                                                    string DataToWrite = PartNumber + "-" + RevNumber + ", " + PartSerialNumber + ", " + getValidationResult(tempString.ToString());
                                                    ACKFilePath = Path.Combine(this._machineDTO.ACKFolderPath, "UnProcessed", string.Format("ACK_{0}_{1}_{2}.csv", this._machineId, PartNumber, DateTime.Now.ToString("yyyyMMdd_HHmmss")));

                                                    WriteToACKFile(ACKFilePath, DataToWrite);
                                                    Logger.WriteDebugLog("ACK Data send = " + DataToWrite);
                                                }
                                            }
                                            else
                                            {
                                                Logger.WriteDebugLog(string.Format("REQ File is empty : {0}", this._reqFolderPath));
                                            }

                                            REQsourceFile = Path.Combine(this._machineDTO.REQFolderPath, "Processed", f.Name);
                                            MoveWithReplace(f.FullName, REQsourceFile);
                                            Logger.WriteDebugLog("Files are moved to Processed folder successfully.");
                                        }
                                        catch (Exception ex)
                                        {
                                            curStatus = 2;
                                            networkConnection = false;
                                            if (nc != null)
                                                nc.Dispose();

                                            if ((curStatus != prevStatus) || DateTime.Now >= lastSentLogs)
                                            {
                                                Logger.WriteErrorLog(ex.Message);
                                                prevStatus = curStatus;
                                                lastSentLogs = DateTime.Now.AddMinutes(10);
                                            }
                                            
                                        }
                                    }

                                    //process Result file from machine                             
                                    var AllRESFiles = RESdirectory.GetFiles("RES*.txt", SearchOption.TopDirectoryOnly);
                                    foreach (FileInfo f in AllRESFiles)
                                    {
                                        try
                                        {
                                            RESData = File.ReadAllText(f.FullName).Trim();
                                            //RES data: partno-revno-partname-suppliercode-partslNo-heatcode,Sttime,Endtime,Result
                                            if (!string.IsNullOrEmpty(RESData))
                                            {
                                                DateTime Sttime = DateTime.MinValue;
                                                DateTime Endtime = DateTime.MinValue;
                                                Logger.WriteDebugLog("Result data found : " + RESData);

                                                string[] spl1 = RESData.Split(new string[] { "," }, StringSplitOptions.None);
                                                string[] BARCODEStringArr = spl1[0].Split(new string[] { "-" }, StringSplitOptions.None);
                                                Comp = BARCODEStringArr[0].Trim() + "-" + BARCODEStringArr[1].Trim();
                                                SupplierCode = BARCODEStringArr[3].Trim();
                                                PartSerialNumber = BARCODEStringArr[4].Trim();
                                                HeatCode = BARCODEStringArr[5].Trim();

                                                //DateTime.TryParse(spl1[1].Trim(), out Sttime);
                                                //DateTime.TryParse(spl1[2].Trim(), out Endtime);
                                                DateTime.TryParseExact(spl1[1].Trim(), "dd-MM-yyyy hh:mm:ss tt", CultureInfo.InvariantCulture, DateTimeStyles.None, out Sttime);
                                                DateTime.TryParseExact(spl1[2].Trim(), "dd-MM-yyyy hh:mm:ss tt", CultureInfo.InvariantCulture, DateTimeStyles.None, out Endtime);
                                                string Result = spl1[3].Trim();

                                                //Logger.WriteDebugLog(string.Format("Inserting Data into Raw Data.....MachineID : {0}, Comp : {1}, OPN : {2},Sttime: {3}, Endtime : {4}, HeatCode : {5}, Result : {6} ", this._interfaceId, Comp, this._opnID, Sttime.ToString(), Endtime.ToString(), HeatCode, Result));

                                                //TODO - Update the Result to AutoData
                                                //'START-1-21-[5646751-DX00]-20-2-1-WorkOrder1-PartSL01-Supplier01-Supervisor01-20200527-111800-20200527-123000-END

                                                DatabaseAccess.GetOperatorAndSupervisorForShift(this._machineId, DateTime.Now, out OperatorID, out SupervisorID);

                                                var type1Str = string.Format("START-1-{0}-[{1}]-{2}-{3}-{4}-{5}-{6}-{7}-{8}-{9}-{10}-{11}-{12}-END", _machineDTO.InterfaceId, Comp, this._opnID,OperatorID, "1", HeatCode, PartSerialNumber, SupplierCode, SupervisorID, Sttime.ToString("yyyyMMdd"), Sttime.ToString("HHmmss"), Endtime.ToString("yyyyMMdd"), Endtime.ToString("HHmmss"));
                                                SaveStringToTPMFile(type1Str);
                                                DatabaseAccess.ProcessDataString(type1Str, out Output);
                                                //TODO - insert rejection record if result = 2
                                                //START-20-mc-[comp]-opn-opr-rejcode-rejqty-rejdate-rejshift-workoder-slno-suppliercode-supervisorcode-date-time-end
                                                //add supervisorcode and opr to strings
                                                if (Result.Trim() == "2")
                                                {
                                                    var rejStr = string.Format("START-20-{0}-[{1}]-{2}-{3}-{4}-{5}-{6}-{7}-{8}-{9}-{10}-{11}-{12}-{13}-END", _machineDTO.InterfaceId, Comp, this._opnID, OperatorID, "1", "1", "0", "0", HeatCode, PartSerialNumber, SupplierCode, SupervisorID, Sttime.ToString("yyyyMMdd"), Sttime.ToString("HHmmss"));
                                                    SaveStringToTPMFile(rejStr);
                                                    DatabaseAccess.ProcessDataString(rejStr, out Output);
                                                }

                                                RESsourceFile = Path.Combine(this._machineDTO.RESFolderPath, "Processed", f.Name);
                                                MoveWithReplace(f.FullName, RESsourceFile);
                                                Logger.WriteDebugLog("File moved to Processed folder successfully.");
                                            }
                                            else
                                            {
                                                Logger.WriteDebugLog(string.Format("RES File is not available in this path : {0}", this._resFolderPath));
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            curStatus = 2;
                                            networkConnection = false;
                                            if (nc != null)
                                                nc.Dispose();

                                            if ((curStatus != prevStatus) || DateTime.Now >= lastSentLogs)
                                            {
                                                Logger.WriteErrorLog(ex.ToString());
                                                prevStatus = curStatus;
                                                lastSentLogs = DateTime.Now.AddMinutes(10);
                                            }
                                            
                                        }

                                    }

                                }
                                catch (IOException dirEx)
                                {
                                    curStatus = 2;
                                    networkConnection = false;
                                    if (nc != null)
                                        nc.Dispose();

                                    if ((curStatus != prevStatus) || DateTime.Now >= lastSentLogs)
                                    {
                                        Logger.WriteErrorLog("IOException: " + dirEx.Message);
                                        prevStatus = curStatus;
                                        lastSentLogs = DateTime.Now.AddMinutes(10);
                                    }
                                    
                                }
                                catch (Exception ex)
                                {
                                    curStatus = 2;
                                    networkConnection = false;
                                    if (nc != null)
                                        nc.Dispose();

                                    if ((curStatus != prevStatus) || DateTime.Now >= lastSentLogs)
                                    {
                                        Logger.WriteErrorLog(ex.ToString());
                                        prevStatus = curStatus;
                                        lastSentLogs = DateTime.Now.AddMinutes(10);
                                    }
                                    
                                }
                                Thread.Sleep(600);
                            }
                        }
                        #endregion

                        #region "Equator(CompactX)"
                        else if (_machineDTO.Process.Equals("EquatorCompactX", StringComparison.OrdinalIgnoreCase))
                        {
                            string REQsourceFile = string.Empty;
                            string ACKFilePath = string.Empty;
                            string RESsourceFile = string.Empty;
                            string SPCsourceFile = string.Empty;
                            string REQDestFile = string.Empty;
                            string RESDestFile = string.Empty;
                            string SPCDestFile = string.Empty;
                            string REQData = string.Empty;
                            string RESData = string.Empty;
                            string SupplierCode = string.Empty;
                            string PartNumber = string.Empty;
                            string PartSerialNumber = string.Empty;
                            string HeatCode = string.Empty;
                            string RevNumber = string.Empty;
                            string DataString = string.Empty;
                            string Comp = string.Empty;
                            int OperatorID;
                            int SupervisorID;
                            string STDATE = DateTime.Now.ToString("yyyyMMdd");
                            string STTIME = DateTime.Now.ToString("HHmmss");
                            int Output = -1;
                            int prevStatus = -1, curStatus = 1;
                            DateTime lastSentLogs = DateTime.Now.AddMinutes(-10);
                            NetworkConnection nc = null;
                            _password = ConfigurationManager.AppSettings["EqCompX_Password_Fileshare"].ToString();
                            _userName = ConfigurationManager.AppSettings["EqCompX_UserID_Fileshare"].ToString();
                            bool networkConnection = false;
                            Logger.WriteDebugLog("Reading CompactX Equator data.....");
                            var REQdirectory = new DirectoryInfo(this._reqFolderPath);
                            var RESdirectory = new DirectoryInfo(this._resFolderPath);
                            var SPCdirectory = new DirectoryInfo(this._spcFolderPath);

                            while (true)
                            {
                                if (ServiceStop.stop_service == 1)
                                    break;
                                if (!networkConnection)
                                {
                                    if (!string.IsNullOrEmpty(_userName) && !string.IsNullOrEmpty(_password))
                                    {
                                        try
                                        {
                                            nc = new NetworkConnection(this._reqFolderPath, new NetworkCredential(_userName, _password));
                                            networkConnection = true;
                                            curStatus = 1;
                                            if ((curStatus != prevStatus) || DateTime.Now >= lastSentLogs)
                                            {
                                                Logger.WriteDebugLog("Connection has been established to the Network Path Folder.");
                                                prevStatus = curStatus;
                                                lastSentLogs = DateTime.Now.AddMinutes(10);
                                            }
                                        }
                                        catch (Exception exx)
                                        {
                                            networkConnection = false;
                                            if (nc != null)
                                                nc.Dispose();
                                            curStatus = 2;
                                            if((curStatus!=prevStatus)|| DateTime.Now >= lastSentLogs)
                                            {
                                                Logger.WriteErrorLog(exx.ToString());
                                                prevStatus = curStatus;
                                                lastSentLogs = DateTime.Now.AddMinutes(10);
                                            }
                                            

                                        }
                                    }
                                }

                                try
                                {
                                    var AllREQFiles = REQdirectory.GetFiles("REQ*.txt", SearchOption.TopDirectoryOnly);                                  
                                    foreach (FileInfo f in AllREQFiles)
                                    {
                                        try
                                        {
                                            REQData = File.ReadAllText(f.FullName).Trim();//REQData : partno-revno-partname-suppliercode-partslNo-heatcode
                                            if (!string.IsNullOrEmpty(REQData))
                                            {
                                                Logger.WriteDebugLog(string.Format("Data from REQ file : {0}", REQData));
                                                string[] spl = REQData.Split(new string[] { "-" }, StringSplitOptions.None);

                                                PartNumber = spl[0];
                                                RevNumber = spl[1];
                                                PartSerialNumber = spl[4];
                                                STDATE = DateTime.Now.ToString("yyyyMMdd");
                                                STTIME = DateTime.Now.ToString("HHmmss");
                                                DataString = string.Format("START-28-{0}-[{1}-{2}]-{3}-{4}-{5}-{6}-END", this._interfaceId, PartNumber, RevNumber, this._opnID.ToString(), PartSerialNumber, STDATE, STTIME);
                                                SaveStringToTPMFile(DataString);

                                                Logger.WriteDebugLog(string.Format("Req Validation string : {0}", DataString));
                                                string tempString = DatabaseAccess.GetStatus28Type(DataString.Trim());
                                                Logger.WriteDebugLog("Req Validation Result" + tempString);
                                                if (!string.IsNullOrEmpty(tempString))
                                                {
                                                    string DataToWrite = PartNumber + "-" + RevNumber + "_" + PartSerialNumber + "," + getValidationResult(tempString.ToString()) + "," + DateTime.Now.ToString("yyyyMMddHHmmss");


                                                    ACKFilePath = Path.Combine(this._machineDTO.ACKFolderPath, string.Format("SERIAL_NO.txt", this._machineId, PartNumber, DateTime.Now.ToString("yyyyMMdd_HHmmss")));

                                                    WriteToACKFile(ACKFilePath, DataToWrite);
                                                    Logger.WriteDebugLog("ACK Data send = " + DataToWrite);
                                                }
                                            }
                                            else
                                            {
                                                Logger.WriteDebugLog(string.Format("REQ file is not available in this path : {0}", this._reqFolderPath));
                                            }

                                            REQsourceFile = Path.Combine(this._machineDTO.REQFolderPath, "Processed", f.Name);
                                            MoveWithReplace(f.FullName, REQsourceFile);
                                            Logger.WriteDebugLog("Files are moved to Processed folder successfully.");
                                        }                                        
                                        catch (Exception exx)
                                        {
                                            networkConnection = false;
                                            if (nc != null)
                                                nc.Dispose();
                                            curStatus = 2;
                                            if ((curStatus != prevStatus) || DateTime.Now >= lastSentLogs)
                                            {
                                                Logger.WriteErrorLog(exx.ToString());
                                                prevStatus = curStatus;
                                                lastSentLogs = DateTime.Now.AddMinutes(10);
                                            }
                                        }
                                    }


                                    var AllRESFiles = RESdirectory.GetFiles("RES*.txt", SearchOption.TopDirectoryOnly);
                                    foreach (FileInfo f in AllRESFiles)
                                    {
                                        try
                                        {

                                            RESData = File.ReadAllText(f.FullName).Trim(); //RES data: partno-revno-partname-suppliercode-partslNoheatcode,Sttime,Endtime,Result
                                            if (!string.IsNullOrEmpty(RESData))
                                            {
                                                Logger.WriteDebugLog("Result data found : " + RESData);
                                                DateTime Sttime = DateTime.MinValue;
                                                DateTime Endtime = DateTime.MinValue;

                                                string[] spl1 = RESData.Split(new string[] { "," }, StringSplitOptions.None);
                                                string[] BARCODEStringArr = spl1[0].Split(new string[] { "-" }, StringSplitOptions.None);
                                                Comp = BARCODEStringArr[0] + "-" + BARCODEStringArr[1];
                                                SupplierCode = BARCODEStringArr[3];
                                                PartSerialNumber = BARCODEStringArr[4];
                                                HeatCode = BARCODEStringArr[5];
                                                //todo-prince
                                                //DateTime.TryParse(spl1[1].Trim(), out Sttime);
                                                //DateTime.TryParse(spl1[2].Trim(), out Endtime);
                                                DateTime.TryParseExact(spl1[1], "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out Sttime);
                                                DateTime.TryParseExact(spl1[2], "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out Endtime);
                                                string Result = spl1[3];

                                                DatabaseAccess.GetOperatorAndSupervisorForShift(this._machineId, DateTime.Now, out OperatorID, out SupervisorID);

                                                //  var type1Str = string.Format("START-1-{0}-[{1}]-{2}-{3}-{4}-{5}-{6}-{7}-{8}-{9}-{10}-{11}-{12}-END", _machineDTO.InterfaceId, Comp, this._opnID, "0", "1", HeatCode, PartSerialNumber, SupplierCode, "0", Sttime.ToString("yyyyMMdd"), Sttime.ToString("HHmmss"), Endtime.ToString("yyyyMMdd"), Endtime.ToString("HHmmss"));

                                                string type1Str = $"START-1-{_machineDTO.InterfaceId}-[{Comp}]-{this._opnID}-{OperatorID}-1-{HeatCode}-{PartSerialNumber}-{SupplierCode}-{SupervisorID}-{Sttime:yyyyMMdd}-{Sttime:HHmmss}-{Endtime:yyyyMMdd}-{Endtime:HHmmss}-END";
                                                SaveStringToTPMFile(type1Str);
                                                DatabaseAccess.ProcessDataString(type1Str, out Output);

                                                if (Result.Trim() == "2")
                                                {
                                                    var rejStr = string.Format("START-20-{0}-[{1}]-{2}-{3}-{4}-{5}-{6}-{7}-{8}-{9}-{10}-{11}-{12}-{13}-END", _machineDTO.InterfaceId, Comp, this._opnID, OperatorID, "1", "1", "0", "0", HeatCode, PartSerialNumber, SupplierCode, SupervisorID, Sttime.ToString("yyyyMMdd"), Sttime.ToString("HHmmss"));
                                                    SaveStringToTPMFile(rejStr);
                                                    DatabaseAccess.ProcessDataString(rejStr, out Output);
                                                }
                                            }
                                            else
                                            {
                                                Logger.WriteDebugLog(string.Format("RES file is not available in this path : {0}", this._resFolderPath));
                                            }

                                            RESsourceFile = Path.Combine(this._machineDTO.RESFolderPath, "Processed", f.Name);
                                            MoveWithReplace(f.FullName, RESsourceFile);
                                            Logger.WriteDebugLog("File moved to Processed folder successfully.");                                          
                                        }
                                        catch (Exception e)
                                        {
                                            networkConnection = false;
                                            if (nc != null)
                                                nc.Dispose();
                                            curStatus = 2;
                                            if ((curStatus != prevStatus) || DateTime.Now >= lastSentLogs)
                                            {
                                                Logger.WriteErrorLog(e.Message);
                                                prevStatus = curStatus;
                                                lastSentLogs = DateTime.Now.AddMinutes(10);
                                            }
                                        }
                                    }

                                    if (SPCdirectory.Exists)
                                    {
                                        var SPCfiles = SPCdirectory.GetFiles("*.csv", SearchOption.TopDirectoryOnly).OrderBy(file => file.LastWriteTime).ToList();
                                        foreach (FileInfo SPCfile in SPCfiles)
                                        {
                                            try
                                            {
                                                var sourceFile = SPCfile.FullName;
                                                var UnprocessedFile_SPC = Path.Combine(UnprocessedFilePath, Path.GetFileName(SPCfile.Name));
                                                var ProcessedFile_SPC = Path.Combine(ProcessedFilePath, Path.GetFileName(SPCfile.Name));

                                                MoveWithReplace(sourceFile, UnprocessedFile_SPC);

                                                if (!string.IsNullOrEmpty(UnprocessedFile_SPC))
                                                {
                                                    UpdateSPCData(UnprocessedFile_SPC, ProcessedFile_SPC, this._machineId, this._opnID);
                                                }
                                            }
                                            catch (Exception exxx)
                                            {
                                                networkConnection = false;
                                                if (nc != null)
                                                    nc.Dispose();
                                                curStatus = 2;
                                                if ((curStatus != prevStatus) || DateTime.Now >= lastSentLogs)
                                                {
                                                    Logger.WriteErrorLog(exxx.ToString());
                                                    prevStatus = curStatus;
                                                    lastSentLogs = DateTime.Now.AddMinutes(10);
                                                }
                                            }
                                        }
                                    }
                                }
                                catch (IOException dirEx)
                                {
                                    networkConnection = false;
                                    if (nc != null)
                                        nc.Dispose();
                                    
                                    curStatus = 2;
                                    if ((curStatus != prevStatus) || DateTime.Now >= lastSentLogs)
                                    {
                                        Logger.WriteErrorLog("IOException: " + dirEx.Message);
                                        prevStatus = curStatus;
                                        lastSentLogs = DateTime.Now.AddMinutes(10);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    networkConnection = false;
                                    if (nc != null)
                                        nc.Dispose();
                                    curStatus = 2;
                                    if ((curStatus != prevStatus) || DateTime.Now >= lastSentLogs)
                                    {
                                        Logger.WriteErrorLog(ex.ToString());
                                        prevStatus = curStatus;
                                        lastSentLogs = DateTime.Now.AddMinutes(10);
                                    }
                                    
                                }
                                finally
                                {

                                }
                            }
                        }
                        #endregion

                        #region "MPI"
                        else if (_machineDTO.Process.Equals("MPI", StringComparison.OrdinalIgnoreCase))
                        {
                            string REQsourceFile = string.Empty;
                            string ACKFilePath = string.Empty;
                            string RESsourceFile = string.Empty;
                            string REQDestFile = string.Empty;
                            string RESDestFile = string.Empty;
                            string REQData = string.Empty;
                            string RESData = string.Empty;
                            string PartNumber = string.Empty;
                            string PartSerialNumber = string.Empty;
                            string SupplierCode = string.Empty;
                            string HeatCode = string.Empty;
                            string RevNumber = string.Empty;
                            string DataString = string.Empty;
                            string Comp = string.Empty;
                            //int OperatorID;
                            //int SupervisorID;
                            string STDATE = DateTime.Now.ToString("yyyyMMdd");
                            string STTIME = DateTime.Now.ToString("HHmmss");
                            int Output = -1;
                            int prevStatus = -1, curStatus = 1;
                            DateTime lastSentLogs = DateTime.Now.AddMinutes(-10);
                            NetworkConnection nc = null;
                            _password = ConfigurationManager.AppSettings["MPI_Password_Fileshare"].ToString();
                            _userName = ConfigurationManager.AppSettings["MPI_UserID_Fileshare"].ToString();
                            bool networkConnection = false;
                            Logger.WriteDebugLog("Reading MPI data.....");
                            Logger.WriteDebugLog("Reading LASER marking machine data.....");
                            var REQdirectory = new DirectoryInfo(this._reqFolderPath);
                            var RESdirectory = new DirectoryInfo(this._resFolderPath);

                            while (true)
                            {
                                if (ServiceStop.stop_service == 1)
                                    break;
                                if (!networkConnection)
                                {
                                    if (!string.IsNullOrEmpty(_userName) && !string.IsNullOrEmpty(_password))
                                    {
                                        try
                                        {
                                            nc = new NetworkConnection(this._reqFolderPath, new NetworkCredential(_userName, _password));
                                            networkConnection = true;
                                            curStatus = 1;
                                            if((curStatus!=prevStatus) || DateTime.Now >= lastSentLogs)
                                            {
                                                Logger.WriteDebugLog("Connection has been established to the Network Path Folder.");
                                                prevStatus = curStatus;
                                                lastSentLogs = DateTime.Now.AddMinutes(10);
                                            }
                                        }
                                        catch (Exception exx)
                                        {
                                            networkConnection = false;
                                            if (nc != null)
                                                nc.Dispose();
                                            curStatus = 2;
                                            if ((curStatus != prevStatus) || DateTime.Now >= lastSentLogs)
                                            {
                                                Logger.WriteErrorLog(exx.ToString());
                                                prevStatus = curStatus;
                                                lastSentLogs = DateTime.Now.AddMinutes(10);
                                            }
                                                
                                        }
                                    }
                                }
                                try
                                {
                                    var AllREQFiles = REQdirectory.GetFiles("REQ*.txt", SearchOption.TopDirectoryOnly);
                                    foreach (FileInfo f in AllREQFiles)
                                    {
                                        try
                                        {
                                            //process file and move file to unprocessed folder
                                            //REQData : partno-revno-partname-suppliercode-partslNo-heatcode
                                            REQData = File.ReadAllText(f.FullName).Trim();
                                            if (!string.IsNullOrEmpty(REQData))
                                            {
                                                Logger.WriteDebugLog(string.Format("Data from REQ file : {0}", REQData));
                                                string[] spl = REQData.Split(new string[] { "-" }, StringSplitOptions.None);

                                                PartNumber = spl[0].Trim();
                                                RevNumber = spl[1].Trim();
                                                PartSerialNumber = spl[4].Trim();
                                                STDATE = f.LastWriteTime.ToString("yyyyMMdd");
                                                STTIME = f.LastWriteTime.ToString("HHmmss");

                                                //START-28-21-[3539598-DX00]-20-N112F-20200606-103850-END
                                                DataString = string.Format("START-28-{0}-[{1}-{2}]-{3}-{4}-{5}-{6}-END", this._interfaceId, PartNumber, RevNumber, this._opnID.ToString(), PartSerialNumber, STDATE, STTIME);
                                                SaveStringToTPMFile(DataString);
                                                Logger.WriteDebugLog(string.Format("Req Validation string : {0}", DataString));
                                                string tempString = DatabaseAccess.GetStatus28Type(DataString.Trim());
                                                Logger.WriteDebugLog("Req Validation Result" + tempString);
                                                if (!string.IsNullOrEmpty(tempString))
                                                {
                                                    string DataToWrite = PartNumber + ", " + PartSerialNumber + ", " + getValidationResult(tempString.ToString());

                                                    ACKFilePath = Path.Combine(this._machineDTO.ACKFolderPath, "UnProcessed", string.Format("ACK_{0}_{1}_{2}.csv", this._machineId, PartNumber, DateTime.Now.ToString("yyyyMMdd_HHmmss")));

                                                    WriteToACKFile(ACKFilePath, DataToWrite);
                                                    Logger.WriteDebugLog("ACK Data send = " + DataToWrite);
                                                }
                                            }
                                            else
                                            {
                                                Logger.WriteDebugLog(string.Format("REQ File is empty : {0}", this._reqFolderPath));
                                            }

                                            REQsourceFile = Path.Combine(this._machineDTO.REQFolderPath, "Processed", f.Name);
                                            MoveWithReplace(f.FullName, REQsourceFile);
                                            Logger.WriteDebugLog("Files are moved to Processed folder successfully.");
                                        }
                                        catch (Exception ex)
                                        {
                                            networkConnection = false;
                                            if (nc != null)
                                                nc.Dispose();
                                            curStatus = 2;
                                            if ((curStatus != prevStatus) || DateTime.Now >= lastSentLogs)
                                            {
                                                Logger.WriteErrorLog(ex.Message);
                                                prevStatus = curStatus;
                                                lastSentLogs = DateTime.Now.AddMinutes(10);
                                            }
                                        }
                                    }

                                    //TODO - process response file as per file "MPI Integration.pdf" or "Shanti-Iron-Pending-Items-29-may-20202.docx"

                                    var AllRESFiles = RESdirectory.GetFiles("RES*.*", SearchOption.TopDirectoryOnly);
                                    foreach (FileInfo f in AllRESFiles)
                                    {
                                        RESData = File.ReadAllText(f.FullName).Trim();
                                        //RES data: InspectorID,PartName,PartNo, Part Serial No, Heat Code Number, Inspection Start DateTime, Inspection End DateTime, CameraInsp Result, Camera Pic Link, Manual Insp Result, De-Mag Leval, Remarks
                                        //inspector name, barcode,start time, end time, camera result, camera pic link,manual ins resuult,De-MagLevel,Remarks
                                        //XYZ123,3713A081D-DX-FLYWHEEL_HOUSING-B478F0-ZE-1232C-UNUNUK,16-MAR-2020 19:18:00,16-MAR-2020 19:19:00,1,D:\FileshareIOT\MPI\Camera\ZE-1232C,1,1.2G,Visual Inspection OK
                                        if (!string.IsNullOrEmpty(RESData))
                                        {
                                            DateTime Sttime = DateTime.MinValue;
                                            DateTime Endtime = DateTime.MinValue;

                                            string[] spl1 = RESData.Split(new string[] { "," }, StringSplitOptions.None);
                                            //string[] BARCODEStringArr = spl1[1].Split(new string[] { "-" }, StringSplitOptions.None);
                                            string Opr = spl1[0].Replace(char.MinValue,' ').Trim();
                                            Comp = (spl1[1] + "-" + spl1[2]).Replace(char.MinValue, ' ').Trim();
                                            SupplierCode = spl1[4].Replace(char.MinValue, ' ').Trim(); ;
                                            PartSerialNumber = spl1[5].Replace(char.MinValue, ' ').Trim(); ;
                                            HeatCode = spl1[6].Replace(char.MinValue, ' ').Trim(); ;

                                            //DateTime.TryParse(spl1[7].Trim(), out Sttime);
                                            //DateTime.TryParse(spl1[8].Trim(), out Endtime);
                                            DateTime.TryParseExact(spl1[7], "yyyyMMdd HHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out Sttime);
                                            DateTime.TryParseExact(spl1[8], "yyyyMMdd HHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out Endtime);
                                            string CameraInspResult = spl1[9].Replace(char.MinValue, ' ').Trim(); ;
                                            string CameraPicLink = spl1[10].Replace(char.MinValue, ' ').Trim(); ;
                                            string ManualInspResult = spl1[11].Replace(char.MinValue, ' ').Trim(); ;
                                            string DeMagLeval = spl1[12].Replace(char.MinValue, ' ').Trim(); ;
                                            string VisualInspec = spl1[13].Replace(char.MinValue, ' ').Trim(); ;
                                            string Remarks = spl1[14].Replace(char.MinValue, ' ').Trim(); ;
                                            string Supvisor = spl1[15].Trim(char.MinValue).Trim();

                                            //If CameraInspResult is 1 OR CameraInspResult is 2 and ManualInspResult is 1 then Result is 2 then Result is 1 AND If CameraInspResult and ManualInspResult both are 2 then Result is 2
                                            string Result = "1";
                                            if (CameraInspResult == "1")
                                                Result = "1";
                                            else if (ManualInspResult == "1")
                                                Result = "1";
                                            else /*if (CameraInspResult == "2" && (ManualInspResult == "2" || ManualInspResult == "0"))*/
                                                Result = "2";

                                                Logger.WriteDebugLog(string.Format("Inserting Data into Raw Data.....MachineID : {0}, Comp : {1}, OPN : {2},Sttime: {3}, Endtime : {4}, HeatCode : {5}, Result : {6} ", this._interfaceId, Comp, this._opnID, Sttime.ToString(), Endtime.ToString(), HeatCode, Result));
                                            //DatabaseAccess.GetOperatorAndSupervisorForShift(this._machineId, DateTime.Now, out OperatorID, out SupervisorID);
                                            string type1Str = $"START-1-{_machineDTO.InterfaceId}-[{Comp}]-{this._opnID}-{Opr}-1-{HeatCode}-{PartSerialNumber}-{SupplierCode}-{Supvisor}-{Sttime:yyyyMMdd}-{Sttime:HHmmss}-{Endtime:yyyyMMdd}-{Endtime:HHmmss}-END";
                                            SaveStringToTPMFile(type1Str);
                                            DatabaseAccess.ProcessDataString(type1Str, out Output);
                                            //TODO - insert to some other table also??
                                            Logger.WriteDebugLog("");
                                            DatabaseAccess.InsertToRawdata_MPI(Comp, PartSerialNumber, HeatCode, SupplierCode, ManualInspResult, CameraInspResult, CameraPicLink, DeMagLeval, Sttime, Endtime, "MPI", Remarks, VisualInspec);

                                            //insert rejection record if result = 2
                                            //START-20-mc-[comp]-opn-opr-rejcode-rejqty-rejdate-rejshift-workoder-slno-suppliercode-supervisorcode-date-time-end
                                            if (Result == "2")
                                            {
                                                var rejStr = string.Format("START-20-{0}-[{1}]-{2}-{3}-{4}-{5}-{6}-{7}-{8}-{9}-{10}-{11}-{12}-{13}-END", _machineDTO.InterfaceId, Comp, this._opnID, Opr, "5", "1", "0", "0", HeatCode, PartSerialNumber, SupplierCode, Supvisor, Sttime.ToString("yyyyMMdd"), Sttime.ToString("HHmmss"));
                                                SaveStringToTPMFile(rejStr);
                                                DatabaseAccess.ProcessDataString(rejStr, out Output);
                                            }
                                        }
                                        else
                                        {
                                            Logger.WriteDebugLog(string.Format("RES File is not available in this path : {0}", this._resFolderPath));
                                        }

                                        RESsourceFile = Path.Combine(this._machineDTO.RESFolderPath, "Processed", f.Name);
                                        MoveWithReplace(f.FullName, RESsourceFile);
                                        Logger.WriteDebugLog("File moved to Processed folder successfully." + RESsourceFile);

                                    }
                                }
                                catch (IOException dirEx)
                                {
                                    networkConnection = false;
                                    if (nc != null)
                                        nc.Dispose();

                                    curStatus = 2;
                                    if ((curStatus != prevStatus) || DateTime.Now >= lastSentLogs)
                                    {
                                        Logger.WriteErrorLog("IOException: " + dirEx.Message);
                                        prevStatus = curStatus;
                                        lastSentLogs = DateTime.Now.AddMinutes(10);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    networkConnection = false;
                                    if (nc != null)
                                        nc.Dispose();

                                    curStatus = 2;
                                    if ((curStatus != prevStatus) || DateTime.Now >= lastSentLogs)
                                    {
                                        Logger.WriteErrorLog(ex.ToString());
                                        prevStatus = curStatus;
                                        lastSentLogs = DateTime.Now.AddMinutes(10);
                                    }
                                    
                                }
                            }                            
                        }
                        #endregion
                    }
                    #endregion

                    #region "Profinet"
                    else if (_protocol.Equals("profinet", StringComparison.OrdinalIgnoreCase))
                    {
                        Logger.WriteDebugLog("Entered into PROFINET Protocol");
                        int DBNumber = this._portNo;
                        #region "Leak test machine"
                        if (_machineDTO.Process.Equals("LeakTestMachine", StringComparison.OrdinalIgnoreCase))
                        {
                            Logger.WriteDebugLog("Reading Leak Test machine data.....");

                            if (CheckPingStatus(this._ipAddress))
                            {
                                LeakTestClient = new Plc(CpuType.S71200, this._ipAddress, 0, 1);
                                int PlantID = 0;
                                int MachineID = 0;
                                int OPN = 0;
                                int OPR = 0;
                                int SUP = 0;
                                int ReadBARCODE = 0;
                                string BARCODEData = string.Empty;
                                string BARCODEReadDate = string.Empty;
                                string BARCODEReadTime = string.Empty;
                                string PartNumber = string.Empty;
                                string SupplierCode = string.Empty;
                                string PartSerialNumber = string.Empty;
                                string HeatCode = string.Empty;
                                string RevNumber = string.Empty;
                                string Comp = string.Empty;
                                int Output = -1;
                                int ReadCycleStart = 0;
                                int ReadCycleEnd = 0;
                                string CycleStartDate = string.Empty;
                                string CycleStartTime = string.Empty;
                                string CycleEndDate = string.Empty;
                                string CycleEndTime = string.Empty;
                                DateTime CycleStartDatetime = DateTime.MinValue;
                                DateTime CycleEndDatetime = DateTime.MinValue;
                                int Result = -1;
                                int dataType = 0;
                                string LT_Remarks = string.Empty;
                                string LT_Result = string.Empty;
                                double Parameter1 = 0;
                                double Parameter2 = 0;
                                int Comm = 100;
                                while (true)
                                {
                                    if (ServiceStop.stop_service == 1)
                                        break;

                                    try
                                    {
                                        if (CheckPingStatus(this._ipAddress))
                                        {
                                            if (LeakTestClient.IsConnected == false)
                                            {
                                                ErrorCode erc = LeakTestClient.Open();
                                                if (erc != ErrorCode.NoError)
                                                {
                                                    Logger.WriteErrorLog(string.Format("Not able to connect to PLC. Error = {0}", LeakTestClient.LastErrorString));
                                                    LeakTestClient.ClearLastError();
                                                    Thread.Sleep(1000);
                                                    continue;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            Logger.WriteDebugLog(string.Format("No ping to the device. Please check the PLC IP address {0} and Ping status.", this._ipAddress));
                                            Thread.Sleep(1000);
                                            continue;
                                        }

                                        ReadBARCODE = Convert.ToInt32(LeakTestClient.Read(string.Format("DB{0}.DBB5", DBNumber)));//ok
                                        if (ReadBARCODE == 1)
                                        {
                                            PlantID = ((ushort)LeakTestClient.Read(string.Format("DB{0}.DBW0", DBNumber))).ConvertToShort();
                                            OPR = ((ushort)LeakTestClient.Read(string.Format("DB{0}.DBW134", DBNumber))).ConvertToShort();
                                            SUP = ((ushort)LeakTestClient.Read(string.Format("DB{0}.DBW136", DBNumber))).ConvertToShort();
                                            MachineID = ((ushort)LeakTestClient.Read(string.Format("DB{0}.DBW2", DBNumber))).ConvertToShort();
                                            OPN = Convert.ToInt32(LeakTestClient.Read(string.Format("DB{0}.DBB4", DBNumber)));//ok
                                            BARCODEData = LeakTestClient.Read(DataType.DataBlock, DBNumber, 6 + 2, VarType.String, 60).ToString().
                                            Trim(new char[] { char.MinValue, ' ' });//ok

                                            if (!string.IsNullOrEmpty(BARCODEData))
                                            {
                                                Logger.WriteDebugLog(string.Format("BARCODE string : {0}", BARCODEData));
                                                BARCODEReadDate = ((uint)LeakTestClient.Read(string.Format("DB{0}.DBD68", DBNumber))).ConvertToInt().ToString("00000000");
                                                BARCODEReadTime = ((uint)LeakTestClient.Read(string.Format("DB{0}.DBD72", DBNumber))).ConvertToInt().ToString("000000");
                                                LeakTestClient.Write(string.Format("DB{0}.DBB5", DBNumber), (byte)2);//ok

                                                string[] spl = BARCODEData.Split(new string[] { "-" }, StringSplitOptions.None);
                                                PartNumber = spl[0];
                                                PartSerialNumber = spl[4];
                                                HeatCode = spl[5];
                                                RevNumber = spl[1];
                                                Comp = PartNumber + "-" + RevNumber;
                                                SupplierCode = spl[3];

                                                string DataString = string.Format("START-28-{0}-[{1}]-{2}-{3}-{4}-{5}-END", MachineID, Comp, OPN.ToString(), PartSerialNumber, BARCODEReadDate, BARCODEReadTime);

                                                Logger.WriteDebugLog(string.Format("Processing Data string : {0}", DataString));

                                                //TODO - check te db method
                                                var result = DatabaseAccess.GetStatus28Type(DataString);

                                                if (!string.IsNullOrEmpty(result))
                                                {
                                                    LeakTestClient.Write(string.Format("DB{0}.DBB76", DBNumber), getValidationResult(result.ToString()));

                                                }
                                            }
                                           
                                        }

                                        ReadCycleStart = Convert.ToInt32(LeakTestClient.Read(string.Format("DB{0}.DBB77", DBNumber)));//ok
                                        if (ReadCycleStart == 1)
                                        {
                                            OPR = ((ushort)LeakTestClient.Read(string.Format("DB{0}.DBW134", DBNumber))).ConvertToShort();
                                            PlantID = ((ushort)LeakTestClient.Read(string.Format("DB{0}.DBW0", DBNumber))).ConvertToShort();
                                            SUP = ((ushort)LeakTestClient.Read(string.Format("DB{0}.DBW136", DBNumber))).ConvertToShort();
                                            MachineID = ((ushort)LeakTestClient.Read(string.Format("DB{0}.DBW2", DBNumber))).ConvertToShort();
                                            OPN = Convert.ToInt32(LeakTestClient.Read(string.Format("DB{0}.DBB4", DBNumber)));//ok
                                            CycleStartDate = ((uint)LeakTestClient.Read(string.Format("DB{0}.DBD80", DBNumber))).ConvertToInt().ToString("00000000");
                                            CycleStartTime = ((uint)LeakTestClient.Read(string.Format("DB{0}.DBD84", DBNumber))).ConvertToInt().ToString("000000");

                                            Logger.WriteDebugLog("Cycle Start Date : " + CycleStartDate + " Time : " + CycleStartTime);
                                            DateTime.TryParseExact(CycleStartDate + " " + CycleStartTime, "yyyyMMdd HHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out CycleStartDatetime);
                                            BARCODEData = LeakTestClient.Read(DataType.DataBlock, DBNumber, 6 + 2, VarType.String, 60).ToString().
                                           Trim(new char[] { char.MinValue, ' ' });//ok

                                            if (!string.IsNullOrEmpty(BARCODEData))
                                            {
                                                string[] spl = BARCODEData.Split(new string[] { "-" }, StringSplitOptions.None);
                                                PartNumber = spl[0];
                                                PartSerialNumber = spl[4];
                                                HeatCode = spl[5];
                                                RevNumber = spl[1];
                                                Comp = PartNumber + "-" + RevNumber;
                                                SupplierCode = spl[3];
                                            }

                                            //DatabaseAccess.InsertToRawdata(11, _machineDTO.InterfaceId, this._ipAddress, Comp, this._opnID, OPR, SUP, CycleStartDatetime, CycleEndDatetime, SupplierCode, PartSerialNumber, HeatCode, string.Empty, string.Empty);

                                            var type11Str = string.Format("START-11-{0}-[{1}]-{2}-{3}-{4}-{5}-{6}-{7}-{8}-{9}-END", _machineDTO.InterfaceId, Comp, this._opnID, OPR, "1", PartSerialNumber, SupplierCode, SUP, CycleStartDatetime.ToString("yyyyMMdd"), CycleStartDatetime.ToString("HHmmss"));
                                            SaveStringToTPMFile(type11Str);
                                            DatabaseAccess.ProcessDataString(type11Str, out Output);

                                            LeakTestClient.Write(string.Format("DB{0}.DBB77", DBNumber), (byte)2);
                                        }

                                        ReadCycleEnd = Convert.ToInt32(LeakTestClient.Read(string.Format("DB{0}.DBB78", DBNumber)));//ok
                                        if (ReadCycleEnd == 1)
                                        {
                                            PlantID = ((ushort)LeakTestClient.Read(string.Format("DB{0}.DBW0", DBNumber))).ConvertToShort();
                                            OPR = ((ushort)LeakTestClient.Read(string.Format("DB{0}.DBW134", DBNumber))).ConvertToShort();
                                            SUP = ((ushort)LeakTestClient.Read(string.Format("DB{0}.DBW136", DBNumber))).ConvertToShort();
                                            MachineID = ((ushort)LeakTestClient.Read(string.Format("DB{0}.DBW2", DBNumber))).ConvertToShort();
                                            OPN = Convert.ToInt32(LeakTestClient.Read(string.Format("DB{0}.DBB4", DBNumber)));//ok
                                            CycleEndDate = ((uint)LeakTestClient.Read(string.Format("DB{0}.DBD88", DBNumber))).ConvertToInt().ToString("00000000");
                                            CycleEndTime = ((uint)LeakTestClient.Read(string.Format("DB{0}.DBD92", DBNumber))).ConvertToInt().ToString("000000");

                                            Logger.WriteDebugLog("Cycle End Date : " + CycleEndDate + " Time : " + CycleEndTime);
                                            DateTime.TryParseExact(CycleEndDate + " " + CycleEndTime, "yyyyMMdd HHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out CycleEndDatetime);
                                            BARCODEData = LeakTestClient.Read(DataType.DataBlock, DBNumber, 6 + 2, VarType.String, 60).ToString().
                                           Trim(new char[] { char.MinValue, ' ' });//ok

                                            if (!string.IsNullOrEmpty(BARCODEData))
                                            {
                                                string[] spl = BARCODEData.Split(new string[] { "-" }, StringSplitOptions.None);
                                                PartNumber = spl[0];
                                                PartSerialNumber = spl[4];
                                                HeatCode = spl[5];
                                                RevNumber = spl[1];
                                                Comp = PartNumber + "-" + RevNumber;
                                                SupplierCode = spl[3];
                                            }
                                            Result = Convert.ToInt32(LeakTestClient.Read(string.Format("DB{0}.DBB79", DBNumber)));
                                            LT_Result = LeakTestClient.Read(DataType.DataBlock, DBNumber, 96 + 2, VarType.String, 16).ToString().
                                           Trim(new char[] { char.MinValue, ' ' });
                                            LT_Remarks = LeakTestClient.Read(DataType.DataBlock, DBNumber, 114 + 2, VarType.String, 16).ToString().
                                           Trim(new char[] { char.MinValue, ' ' });
                                            //Parameter1 = ((uint)LeakTestClient.Read(string.Format("DB{0}.DBD96", DBNumber))).ConvertToDouble();//ok
                                            //Parameter2 = ((uint)LeakTestClient.Read(string.Format("DB{0}.DBD100", DBNumber))).ConvertToDouble();//ok
                                            
                                            //double[] Parameters = new double[] { Parameter1, Parameter2 };
                                            LeakTestClient.Write(string.Format("DB{0}.DBB78", DBNumber), (byte)2);
                                            
                                            DatabaseAccess.InsertToRawdata(1, _machineDTO.InterfaceId, this._ipAddress, Comp, this._opnID, OPR, SUP, CycleStartDatetime, CycleEndDatetime, SupplierCode, PartSerialNumber, HeatCode,  LT_Result, LT_Remarks);
                                            //TODO - build string and call proc - Prince
                                            var type1Str = string.Format("START-1-{0}-[{1}]-{2}-{3}-{4}-{5}-{6}-{7}-{8}-{9}-{10}-{11}-{12}-END", _machineDTO.InterfaceId, Comp, this._opnID, OPR, "1", HeatCode, PartSerialNumber, SupplierCode, SUP, CycleStartDatetime.ToString("yyyyMMdd"), CycleStartDatetime.ToString("HHmmss"), CycleEndDatetime.ToString("yyyyMMdd"), CycleEndDatetime.ToString("HHmmss"));
                                            SaveStringToTPMFile(type1Str);
                                            //DatabaseAccess.ProcessDataString(type1Str, out Output);
                                            //START-25-21-[5646751-DX00]-20-2-1-1-20200527-2-WorkOrder1-PartSL01-Supplier01-Supervisor01-20200527-150000-END -Rework
                                            //TODO - If the Result is NotOK then create entry in Autodatarejections table - Prince
                                            if (Result == 2)
                                            {
                                                if ((bool)LeakTestClient.Read(string.Format("DB{0}.DBX138.0", DBNumber))) // Rejection
                                                {
                                                    var rejStr = string.Format("START-20-{0}-[{1}]-{2}-{3}-{4}-{5}-{6}-{7}-{8}-{9}-{10}-{11}-{12}-{13}-END", _machineDTO.InterfaceId, Comp, this._opnID, OPR, "4", "1", "0", "0", HeatCode, PartSerialNumber, SupplierCode, SUP, CycleStartDatetime.ToString("yyyyMMdd"), CycleStartDatetime.ToString("HHmmss"));
                                                    SaveStringToTPMFile(rejStr);
                                                    DatabaseAccess.ProcessDataString(rejStr, out Output);
                                                    LeakTestClient.Write(string.Format("DB{0}.DBX138.0", DBNumber), false);
                                                }
                                                else if ((bool)LeakTestClient.Read(string.Format("DB{0}.DBX138.1", DBNumber))) // Rework
                                                {
                                                    DateTime shiftStart = DateTime.MinValue;
                                                    string shiftID;
                                                    DatabaseAccess.GetCurrentShiftDetails(out shiftStart,out shiftID);
                                                    
                                                    var reworkStr = string.Format("START-25-{0}-[{1}]-{2}-{3}-{4}-{5}-{6}-{7}-{8}-{9}-{10}-{11}-{12}-{13}-END", _machineDTO.InterfaceId, Comp, this._opnID, OPR, "1", "1", shiftStart.ToString("yyyyMMdd"), shiftID, HeatCode, PartSerialNumber, SupplierCode, SUP, CycleStartDatetime.ToString("yyyyMMdd"), CycleStartDatetime.ToString("HHmmss"));
                                                    SaveStringToTPMFile(reworkStr);
                                                    DatabaseAccess.ProcessDataString(reworkStr, out Output);
                                                    LeakTestClient.Write(string.Format("DB{0}.DBX138.1", DBNumber), false);
                                                }
                                                //DatabaseAccess.ProcessDataString(notOKStr, out Output);
                                            }

                                            //DatabaseAccess.UpdateRawData(_machineDTO.InterfaceId.ToString(), Comp, OPN,Result, Remarks);
                                            //Logger.WriteDebugLog(string.Format("Inserting Parameter value into SPCAutoData.....P1 : {0}, P2 : {1}", Parameter1.ToString("##.####"), Parameter2.ToString("##.####")));

                                            //int i = 1;
                                            //foreach (double P in Parameters)
                                            //{
                                            //    DatabaseAccess.InsertToSPCAutodata(_machineDTO.InterfaceId.ToString(), Comp, OPN, ("P" + i.ToString()), P, CycleStartDatetime);
                                            //    i++;
                                            //}
                                        }

                                        #region Communication
                                        try
                                        {
                                            LeakTestClient.Write(string.Format("DB{0}.DBB132", DBNumber), (byte)Comm);
                                        }
                                        catch (Exception ex)
                                        {
                                            Logger.WriteErrorLog(ex.ToString());
                                        }
                                        if (Comm == 100)
                                            Comm = 200;
                                        else
                                            Comm = 100;
                                        #endregion
                                    }
                                    catch (Exception ex)
                                    {
                                        Logger.WriteErrorLog(ex.ToString());
                                        if (LeakTestClient != null && LeakTestClient.IsConnected)
                                        {
                                            LeakTestClient.Close();
                                        }
                                    }
                                    finally
                                    {
                                        if (LeakTestClient != null && LeakTestClient.IsConnected)
                                        {
                                            //LeakTestClient.Close();
                                        }
                                    }
                                }
                                if (LeakTestClient != null && LeakTestClient.IsConnected)
                                {
                                    LeakTestClient.Close();
                                }

                            }
                        }
                        #endregion

                        #region "Washing Machine"
                        else if (_machineDTO.Process.Equals("WashingMachine", StringComparison.OrdinalIgnoreCase))
                        {
                            Logger.WriteDebugLog("Reading Washing machine data.....");

                            if (CheckPingStatus(this._ipAddress))
                            {
                                WashingMachineClient = new Plc(CpuType.S71200, this._ipAddress, 0, 1);

                                string process_String = string.Empty;
                                string barcode_Data = string.Empty;
                                string Part_No = string.Empty;
                                string Rev_No = string.Empty;
                                string Part_Name = string.Empty;
                                string Supplier_Code = string.Empty;
                                string Part_Sl_No = string.Empty;
                                string Heat_Code_No = string.Empty;
                                string Component = string.Empty;
                                int Output = -1;
                                int Comm = 100;
                                DateTime CycleStartDatetime = DateTime.MinValue;
                                DateTime CycleEndDatetime = DateTime.MinValue;

                                while (true)
                                {
                                    if (ServiceStop.stop_service == 1)
                                        break;

                                    try
                                    {
                                        if (CheckPingStatus(this._ipAddress))
                                        {
                                            if (WashingMachineClient.IsConnected == false)
                                            {
                                                ErrorCode erc = WashingMachineClient.Open();
                                                if (erc != ErrorCode.NoError)
                                                {
                                                    Logger.WriteErrorLog(string.Format("Not able to connect to PLC. Error = {0}", WashingMachineClient.LastErrorString));
                                                    WashingMachineClient.ClearLastError();
                                                    Thread.Sleep(1000);
                                                    continue;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            Logger.WriteDebugLog(string.Format("Not able to ping to the device. Please check the PLC IP address {0} and Ping status.", this._ipAddress));
                                            Thread.Sleep(1000);
                                            continue;
                                        }


                                        try
                                        {
                                            byte Read_barcode = Convert.ToByte(WashingMachineClient.Read(string.Format("DB{0}.DBB{1}", DBNumber, "5")));//ok
                                            if (Read_barcode == 1)
                                            {
                                                int PlantID = ((ushort)WashingMachineClient.Read(string.Format("DB{0}.DBW0", DBNumber))).ConvertToShort();
                                                int MachineID = ((ushort)WashingMachineClient.Read(string.Format("DB{0}.DBW{1}", DBNumber, "2"))).ConvertToShort();
                                                int OperationID = Convert.ToInt32(WashingMachineClient.Read(string.Format("DB{0}.DBB{1}", DBNumber, "4")));
                                                int OPR = ((ushort)WashingMachineClient.Read(string.Format("DB{0}.DBW98", DBNumber))).ConvertToShort();
                                                int SUP = ((ushort)WashingMachineClient.Read(string.Format("DB{0}.DBW100", DBNumber))).ConvertToShort();
                                                barcode_Data = WashingMachineClient.Read(DataType.DataBlock, DBNumber, 6 + 2, VarType.String, 60).ToString().Trim(char.MinValue).Trim();//ok
                                                if (barcode_Data.Length > 0)
                                                {
                                                    Logger.WriteDebugLog("Barcode_Data From PLC :" + barcode_Data);
                                                    var data = barcode_Data.Split('-');
                                                    Part_No = data[0];
                                                    Rev_No = data[1];
                                                    Part_Sl_No = data[4];
                                                    Heat_Code_No = data[5];
                                                    Component = Part_No;
                                                    Supplier_Code = data[3];
                                                    var startDate = ((uint)WashingMachineClient.Read(string.Format("DB{0}.DBD{1}", DBNumber, "68"))).ConvertToInt().ToString("00000000");
                                                    var startTime = ((uint)WashingMachineClient.Read(string.Format("DB{0}.DBD{1}", DBNumber, "72"))).ConvertToInt().ToString("000000");//ok
                                                    WashingMachineClient.Write(string.Format("DB{0}.DBB{1}", DBNumber, "5"), (byte)2);//ok

                                                    process_String = string.Format("START-28-{0}-[{1}-{2}]-{3}-{4}-{5}-{6}-END", MachineID, Part_No, Rev_No, OperationID.ToString(), Part_Sl_No, startDate, startTime);
                                                    Logger.WriteDebugLog(string.Format("Processing Data string : {0}", process_String));

                                                    var result = DatabaseAccess.GetStatus28Type(process_String);
                                                    if (!string.IsNullOrEmpty(result))
                                                    {
                                                        //TODO - write result to PLC
                                                        WashingMachineClient.Write(string.Format("DB{0}.DBB{1}", DBNumber, "76"), getValidationResult(result.ToString()));//ok
                                                    }
                                                }                                               
                                            }

                                            byte Read_CycleStartData = Convert.ToByte(WashingMachineClient.Read(string.Format("DB{0}.DBB{1}", DBNumber, "77")));
                                            if (Read_CycleStartData == 1)
                                            {
                                                int PlantID = ((ushort)WashingMachineClient.Read(string.Format("DB{0}.DBW0", DBNumber))).ConvertToShort();
                                                int MachineID = ((ushort)WashingMachineClient.Read(string.Format("DB{0}.DBW{1}", DBNumber, "2"))).ConvertToShort();
                                                int OperationID = Convert.ToInt32(WashingMachineClient.Read(string.Format("DB{0}.DBB{1}", DBNumber, "4")));
                                                int OPR = ((ushort)WashingMachineClient.Read(string.Format("DB{0}.DBW98", DBNumber))).ConvertToShort();
                                                int SUP = ((ushort)WashingMachineClient.Read(string.Format("DB{0}.DBW100", DBNumber))).ConvertToShort();
                                                barcode_Data = WashingMachineClient.Read(DataType.DataBlock, DBNumber, 6 + 2, VarType.String, 60).ToString().Trim(char.MinValue).Trim();//ok
                                                if (barcode_Data.Length > 0)
                                                {
                                                    Logger.WriteDebugLog("Barcode_Data From PLC :" + barcode_Data);
                                                    var data = barcode_Data.Split('-');
                                                    Part_No = data[0];
                                                    Rev_No = data[1];
                                                    Part_Sl_No = data[4];
                                                    Heat_Code_No = data[5];
                                                    Component = Part_No+"-"+Rev_No;
                                                    Supplier_Code = data[3];
                                                }
                                                string CycleStartDate = ((uint)WashingMachineClient.Read(string.Format("DB{0}.DBD80", DBNumber))).ConvertToInt().ToString("00000000");
                                                string CycleStartTime = ((uint)WashingMachineClient.Read(string.Format("DB{0}.DBD84", DBNumber))).ConvertToInt().ToString("000000");

                                                Logger.WriteDebugLog("Cycle Start Date : " + CycleStartDate + " Time : " + CycleStartTime);
                                                DateTime.TryParseExact(CycleStartDate + " " + CycleStartTime, "yyyyMMdd HHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out CycleStartDatetime);
                                                WashingMachineClient.Write(string.Format("DB{0}.DBB77", DBNumber), (byte)2);

                                                var type11Str = string.Format("START-11-{0}-[{1}]-{2}-{3}-{4}-{5}-{6}-{7}-{8}-{9}-END", _machineDTO.InterfaceId, Component, this._opnID, OPR, "1", Part_Sl_No, Supplier_Code, SUP, CycleStartDatetime.ToString("yyyyMMdd"), CycleStartDatetime.ToString("HHmmss"));
                                                SaveStringToTPMFile(type11Str);
                                                DatabaseAccess.ProcessDataString(type11Str, out Output);
                                            }

                                            byte Read_CycleEndData = Convert.ToByte(WashingMachineClient.Read(string.Format("DB{0}.DBB{1}", DBNumber, "78")));
                                            if (Read_CycleEndData == 1)
                                            {
                                                int PlantID = ((ushort)WashingMachineClient.Read(string.Format("DB{0}.DBW0", DBNumber))).ConvertToShort();
                                                int MachineID = ((ushort)WashingMachineClient.Read(string.Format("DB{0}.DBW{1}", DBNumber, "2"))).ConvertToShort();
                                                int OperationID = Convert.ToInt32(WashingMachineClient.Read(string.Format("DB{0}.DBB{1}", DBNumber, "4")));
                                                int OPR = ((ushort)WashingMachineClient.Read(string.Format("DB{0}.DBW98", DBNumber))).ConvertToShort();
                                                int SUP = ((ushort)WashingMachineClient.Read(string.Format("DB{0}.DBW100", DBNumber))).ConvertToShort();
                                                barcode_Data = WashingMachineClient.Read(DataType.DataBlock, DBNumber, 6 + 2, VarType.String, 60).ToString().Trim(char.MinValue).Trim();//ok
                                                if (barcode_Data.Length > 0)
                                                {
                                                    Logger.WriteDebugLog("Barcode_Data From PLC :" + barcode_Data);
                                                    var data = barcode_Data.Split('-');
                                                    Part_No = data[0];
                                                    Rev_No = data[1];
                                                    Part_Sl_No = data[4];
                                                    Heat_Code_No = data[5];
                                                    Component = Part_No + "-" + Rev_No;
                                                    Supplier_Code = data[3];
                                                }
                                                int Read_result = ((ushort)WashingMachineClient.Read(string.Format("DB{0}.DBW{1}", DBNumber, "79"))).ConvertToShort();
                                                string CycleEndDate = ((uint)WashingMachineClient.Read(string.Format("DB{0}.DBD88", DBNumber))).ConvertToInt().ToString("00000000");
                                                string CycleEndTime = ((uint)WashingMachineClient.Read(string.Format("DB{0}.DBD92", DBNumber))).ConvertToInt().ToString("000000");

                                                Logger.WriteDebugLog("Cycle Start Date : " + CycleEndDate + " Time : " + CycleEndTime);
                                                DateTime.TryParseExact(CycleEndDate + " " + CycleEndTime, "yyyyMMdd HHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out CycleEndDatetime);
                                                WashingMachineClient.Write(string.Format("DB{0}.DBB{1}", DBNumber, "78"), (byte)2);

                                                Logger.WriteDebugLog(string.Format("Inserting Data into Raw Data.....MachineID : {0}, Comp : {1}, OPN : {2}, Sttime: {3}, Endtime : {4}, HeatCode : {5}, Result : {6}, IP : {7}", MachineID, Component, OperationID.ToString(), CycleStartDatetime.ToString(), CycleEndDatetime.ToString(), Heat_Code_No, Read_result.ToString(), this._ipAddress));

                                                //TODO - build string for type 1 string and insert to AutoData - Prince
                                                //If the Result is NotOK then create entry in Autodatarejections table - Prince
                                                var type1Str = string.Format("START-1-{0}-[{1}]-{2}-{3}-{4}-{5}-{6}-{7}-{8}-{9}-{10}-{11}-{12}-END", _machineDTO.InterfaceId, Component, this._opnID, OPR, "1", Heat_Code_No, Part_Sl_No, Supplier_Code, SUP, CycleStartDatetime.ToString("yyyyMMdd"), CycleStartDatetime.ToString("HHmmss"), CycleEndDatetime.ToString("yyyyMMdd"), CycleEndDatetime.ToString("HHmmss"));
                                                SaveStringToTPMFile(type1Str);
                                                DatabaseAccess.ProcessDataString(type1Str, out Output);

                                                if (Read_result == 2)
                                                {
                                                    var rejStr = string.Format("START-20-{0}-[{1}]-{2}-{3}-{4}-{5}-{6}-{7}-{8}-{9}-{10}-{11}-{12}-{13}-END", _machineDTO.InterfaceId, Component, this._opnID, OPR, "1", "1", "0", "0", Heat_Code_No, Part_Sl_No, Supplier_Code, SUP, CycleStartDatetime.ToString("yyyyMMdd"), CycleStartDatetime.ToString("HHmmss"));
                                                    SaveStringToTPMFile(rejStr);
                                                    DatabaseAccess.ProcessDataString(rejStr, out Output);
                                                }
                                                //DatabaseAccess.InsertToRawdata(MachineID, this._ipAddress, Component, (int)OperationID,CycleStartDatetime,CycleEndDatetime, Heat_Code_No, Read_result.ToString());		 							
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            Logger.WriteErrorLog(ex.Message);
                                        }

                                        #region Communication
                                        try
                                        {
                                            WashingMachineClient.Write(string.Format("DB{0}.DBB96", DBNumber), (byte)Comm);
                                        }
                                        catch (Exception ex)
                                        {
                                            Logger.WriteErrorLog(ex.ToString());
                                        }
                                        if (Comm == 100)
                                            Comm = 200;
                                        else
                                            Comm = 100;
                                        #endregion

                                    }
                                    catch (Exception ex)
                                    {
                                        Logger.WriteErrorLog(ex.ToString());
                                        if (WashingMachineClient != null && WashingMachineClient.IsConnected)
                                        {
                                            WashingMachineClient.Close();
                                        }
                                    }
                                    finally
                                    {
                                        if (WashingMachineClient != null && WashingMachineClient.IsConnected)
                                        {
                                            //WashingMachineClient.Close();
                                        }
                                    }
                                }
                                if (WashingMachineClient != null && WashingMachineClient.IsConnected)
                                {
                                    WashingMachineClient.Close();
                                }
                            }
                        }
                        #endregion

                        #region "Equator(Phantom)"
                        else if (_machineDTO.Process.Equals("Equator(Phantom)", StringComparison.OrdinalIgnoreCase))
                        {
                            Logger.WriteDebugLog("Reading Equator(Phantom) machine data.....");
                            if (PhantomClient == null)
                                PhantomClient = new Plc(CpuType.S71200, this._ipAddress, 0, 1);
                            string BarcodeString = string.Empty;
                            //PhantomStatus = false;
                            string sourceFile = string.Empty;
                            string RESData = string.Empty;
                            string UnprocessedFile_SPC = string.Empty;
                            string ProcessedFile_SPC = string.Empty;
                            DateTime LastSentTS = DateTime.Now.AddHours(-1);
                            StringBuilder strTxtData = new StringBuilder();
                            int OperatorID=0;
                            int SupervisorID=0;
                            int Output = -1;

                            NetworkConnection nc = null;
                            DirectoryInfo directoryRes = new DirectoryInfo(this._resFolderPath);
                            DirectoryInfo directorySPC = null;
                            _password = ConfigurationManager.AppSettings["EqPhm_Password_Fileshare"].ToString();
                            _userName = ConfigurationManager.AppSettings["EqPhm_UserID_Fileshare"].ToString();
                            bool networkConnection = false;
                            while (true)
                            {
                                if (ServiceStop.stop_service == 1)
                                    break;
                                if (!networkConnection)
                                {
                                    if (!string.IsNullOrEmpty(_userName) && !string.IsNullOrEmpty(_password))
                                    {
                                        try
                                        {
                                            nc = new NetworkConnection(this._spcFolderPath, new NetworkCredential(_userName, _password));
                                            networkConnection = true;
                                        }
                                        catch (Exception exx)
                                        {
                                            networkConnection = false;
                                            if (nc != null)
                                                nc.Dispose();
                                            Logger.WriteErrorLog(exx.ToString());
                                        }
                                    }
                                }
                                if(directorySPC == null)
                                {
                                    directorySPC = new DirectoryInfo(this._spcFolderPath);
                                }

                                try
                                {
                                    if (CheckPingStatus(this._ipAddress))
                                    {
                                        if (PhantomClient.IsConnected == false)
                                        {
                                            ErrorCode erc = PhantomClient.Open();
                                            if (erc != ErrorCode.NoError)
                                            {
                                                Logger.WriteErrorLog(string.Format("Not able to connect to PLC. Error = {0}", PhantomClient.LastErrorString));
                                                PhantomClient.ClearLastError();
                                                Thread.Sleep(1000);
                                                continue;
                                            }
                                            else
                                            {
                                                Logger.WriteDebugLog(string.Format("Connected successfully To PLC."));
                                            }
                                        }                                        
                                    }
                                    else
                                    {
                                        Logger.WriteDebugLog(string.Format("Not able to connect to the device. Please check the PLC IP address {0} and Ping status.", this._ipAddress));
                                        Thread.Sleep(1000);
                                        continue;
                                    }

                                    var PartLoaded = (bool)PhantomClient.Read(string.Format("DB{0}.DBX62.0", DBNumber));
                                    if (PartLoaded)
                                    {
                                        Logger.WriteDebugLog("Reading Data from PLC.");
                                        Logger.WriteDebugLog("Data Read flag (DBX62.0) is high");
                                        BarcodeString = PhantomClient.Read(DataType.DataBlock, DBNumber, 0 + 2, VarType.String, 60).ToString().Trim(new char[] { char.MinValue, ' ' });
                                        PhantomClient.Write(string.Format("DB{0}.DBX62.0", DBNumber), false);

                                        var flag = (bool)PhantomClient.Read(string.Format("DB{0}.DBX62.0", DBNumber));
                                        Logger.WriteDebugLog("Flag DBX62.0 = : " + flag.ToString());

                                        if (!string.IsNullOrEmpty(BarcodeString))
                                        {
                                            Logger.WriteDebugLog(string.Format("Barcode String : {0}", BarcodeString));
                                            string[] spl = BarcodeString.Split(new char[] { '-' });

                                            string PartNumber = spl[0];
                                            string Rev_No = spl[1];
                                            string PartSerialNumber = spl[4];
                                            string STDATE = DateTime.Now.ToString("yyyyMMdd");
                                            string STTIME = DateTime.Now.ToString("HHmmss");

                                            //string DataString = string.Format("START-28-{0}-[{1}-{2}]-{3}-{4}-{5}-{6}-END", this._interfaceId, PartNumber, Rev_No, this._opnID.ToString(), PartSerialNumber, STDATE, STTIME);
                                            //SaveStringToTPMFile(DataString);

                                            //Logger.WriteDebugLog(string.Format("Validation string : {0}", DataString));
                                            //string tempString = DatabaseAccess.GetStatus28Type(DataString.Trim());
                                            //Logger.WriteDebugLog("Validation Result" + tempString);

                                            int count = PartNumber.Length;
                                            if (count < 8)
                                            {
                                                for (int i = 0; i < (8 - count); i++)
                                                {
                                                    PartNumber = PartNumber + " ";
                                                }
                                            }

                                            //string _Values = (PartNumber + "-" + Rev_No + "_" + PartSerialNumber) + Environment.NewLine;
                                            
                                            //if (_Values.Length > 0)
                                            //{
                                            //    Logger.WriteDebugLog(string.Format("Writting Serial no Validation data = {0} , to file = {1} ", _Values, this._machineDTO.ACKFolderPath));

                                            //    byte countTry = 0;
                                            //    while (!PhantomStatus && countTry <= 10)
                                            //    {
                                            //        SaveStringToTxtFile(_Values.ToString().Trim());
                                            //        countTry++;
                                            //    }
                                            //}

                                            //if (tempString.Length > 0)
                                            //{
                                                //string DataToWrite = PartNumber + "-" + Rev_No + "_" + PartSerialNumber + "," + getValidationResult(tempString.ToString()) + "," + DateTime.Now.ToString("yyyyMMddHHmmss") + Environment.NewLine;

                                                string DataToWrite = BarcodeString + Environment.NewLine;
                                                Logger.WriteDebugLog(string.Format("Writting Barcode data = {0} , to file = {1} ", DataToWrite, this._machineDTO.ACKFolderPath));
                                            //Logger.WriteDebugLog(string.Format("Writting Serial no Validation data = {0} , to file = {1} ", DataToWrite, this._machineDTO.ACKFolderPath));
                                            PhantomStatus = false;
                                            byte countTry = 0;
                                                while (!PhantomStatus && countTry <= 10)
                                                {
                                                    SaveStringToTxtFile(DataToWrite.ToString().Trim());
                                                    countTry++;
                                                }
                                            //}
                                        }
                                        else
                                        {
                                            Logger.WriteDebugLog("Barcode Data Is NULL/Empty : "+BarcodeString);
                                        }
                                    }
                                    if (directoryRes.Exists)
                                    {
                                        var RESfiles=directoryRes.GetFiles("RES*.txt", SearchOption.TopDirectoryOnly).OrderBy(file => file.LastWriteTime).ToList();

                                        foreach(FileInfo resFile in RESfiles)
                                        {
                                            try
                                            {
                                                RESData = File.ReadAllText(resFile.FullName).Trim(); //RES data: partno-revno-partname-suppliercode-partslNo-heatcode_stDate_Sttime_EndDate_Endtime_Result
                                                if (!string.IsNullOrEmpty(RESData))
                                                {
                                                    Logger.WriteDebugLog("Result data found : " + RESData);
                                                    DateTime Sttime = DateTime.MinValue;
                                                    DateTime Endtime = DateTime.MinValue;

                                                    string[] BARCODEStringArr = RESData.Split(new string[] { "-" }, StringSplitOptions.None);
                                                    string[] spl1 = BARCODEStringArr[5].Split(new string[] { "_" }, StringSplitOptions.None);
                                                    
                                                    string Comp = BARCODEStringArr[0] + "-" + BARCODEStringArr[1];
                                                    string SupplierCode = BARCODEStringArr[3];
                                                    string PartSerialNumber = BARCODEStringArr[4];
                                                    string HeatCode = spl1[0];
                                                    string Result = spl1[5];

                                                    DateTime.TryParseExact(spl1[1]+" "+spl1[2], "yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out Sttime);
                                                    DateTime.TryParseExact(spl1[3]+" "+spl1[4], "yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out Endtime);                                                   

                                                    DatabaseAccess.GetOperatorAndSupervisorForShift(this._machineId, DateTime.Now, out OperatorID, out SupervisorID);

                                                    //  var type1Str = string.Format("START-1-{0}-[{1}]-{2}-{3}-{4}-{5}-{6}-{7}-{8}-{9}-{10}-{11}-{12}-END", _machineDTO.InterfaceId, Comp, this._opnID, "0", "1", HeatCode, PartSerialNumber, SupplierCode, "0", Sttime.ToString("yyyyMMdd"), Sttime.ToString("HHmmss"), Endtime.ToString("yyyyMMdd"), Endtime.ToString("HHmmss"));

                                                    string type1Str = $"START-1-{_machineDTO.InterfaceId}-[{Comp}]-{this._opnID}-{OperatorID}-1-{HeatCode}-{PartSerialNumber}-{SupplierCode}-{SupervisorID}-{Sttime:yyyyMMdd}-{Sttime:HHmmss}-{Endtime:yyyyMMdd}-{Endtime:HHmmss}-END";
                                                    SaveStringToTPMFile(type1Str);
                                                    DatabaseAccess.ProcessDataString(type1Str, out Output);

                                                    if (Result.Trim() == "0")
                                                    {
                                                        var rejStr = string.Format("START-20-{0}-[{1}]-{2}-{3}-{4}-{5}-{6}-{7}-{8}-{9}-{10}-{11}-{12}-{13}-END", _machineDTO.InterfaceId, Comp, this._opnID, OperatorID, "1", "1", "0", "0", HeatCode, PartSerialNumber, SupplierCode, SupervisorID, Sttime.ToString("yyyyMMdd"), Sttime.ToString("HHmmss"));
                                                        SaveStringToTPMFile(rejStr);
                                                        DatabaseAccess.ProcessDataString(rejStr, out Output);
                                                    }
                                                }
                                                else
                                                {
                                                    Logger.WriteDebugLog(string.Format("RES file is not available in this path : {0}", this._resFolderPath));
                                                }

                                                var RESsourceFile = Path.Combine(this._machineDTO.RESFolderPath, "Processed", resFile.Name);
                                                MoveWithReplace(resFile.FullName, RESsourceFile);
                                                Logger.WriteDebugLog("File moved to Processed folder successfully.");
                                            }
                                            catch(Exception ex)
                                            {
                                                Logger.WriteErrorLog(ex.Message);
                                            }
                                        }
                                    }
                                    if (directorySPC.Exists)
                                    {                                      
                                        var SPCfiles = directorySPC.GetFiles("*.csv", SearchOption.TopDirectoryOnly).OrderBy(file => file.LastWriteTime).ToList();                                      
                                        foreach (FileInfo SPCfile in SPCfiles)
                                        {
                                            try
                                            {
                                                sourceFile = SPCfile.FullName;
                                                UnprocessedFile_SPC = Path.Combine(UnprocessedFilePath, Path.GetFileName(SPCfile.Name));
                                                ProcessedFile_SPC = Path.Combine(ProcessedFilePath, Path.GetFileName(SPCfile.Name));

                                                MoveWithReplace(sourceFile, UnprocessedFile_SPC);

                                                if (!string.IsNullOrEmpty(UnprocessedFile_SPC))
                                                {
                                                    UpdateSPCData(UnprocessedFile_SPC, ProcessedFile_SPC, this._machineId, this._opnID);
                                                }
                                            }
                                            catch (Exception exxx)
                                            {
                                                Logger.WriteErrorLog(exxx.ToString());
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (DateTime.Now > LastSentTS)
                                        {
                                            Logger.WriteDebugLog("did not found the directory = " + directorySPC.FullName);
                                            LastSentTS = DateTime.Now.AddMinutes(30);
                                        }

                                    }
                                }
                                catch (IOException dirEx)
                                {
                                    networkConnection = false;
                                    Logger.WriteErrorLog("IOException: " + dirEx.Message);
                                }
                                catch (Exception ex)
                                {
                                    Logger.WriteErrorLog(ex.ToString());
                                    if (PhantomClient != null && PhantomClient.IsConnected)
                                    {
                                        PhantomClient.Close();
                                    }
                                }
                                finally
                                {
                                    if (PhantomClient != null && PhantomClient.IsConnected)
                                    {
                                        //PhantomClient.Close();
                                    }
                                }
                            }
                            if (nc != null)
                                nc.Dispose();
                            if (PhantomClient != null && PhantomClient.IsConnected)
                            {
                                PhantomClient.Close();
                            }
                        }
                        #endregion

                        #region "Phantom"
                        else if (_machineDTO.Process.Equals("Phantom", StringComparison.OrdinalIgnoreCase))
                        {
                            Logger.WriteDebugLog("Reading Phantom machine data.....");
                            if (sPhantomClient == null)
                                sPhantomClient = new Plc(CpuType.S71200, this._ipAddress, 0, 1);

                            string BarcodeString = string.Empty;
                            string process_String = string.Empty;
                            string barcode_Data = string.Empty;
                            string Part_No = string.Empty;
                            string Rev_No = string.Empty;
                            string Part_Name = string.Empty;
                            string Supplier_Code = string.Empty;
                            string Part_Sl_No = string.Empty;
                            string Heat_Code_No = string.Empty;
                            string Component = string.Empty;
                            DateTime LoadDateTime;
                            int machineId = 0;
                            int operationID = 0;
                            int supervisorID = 0;

                            int PrevStatus = -1, currentStatus = 1;
                            while (true)
                            {
                                if (ServiceStop.stop_service == 1)
                                    break;
                                try
                                {
                                    if (CheckPingStatus(this._ipAddress))
                                    {
                                        if (sPhantomClient.IsConnected == false)
                                        {
                                            sPhantomClient.Close();
                                            Thread.Sleep(1000);
                                            ErrorCode erc = sPhantomClient.Open();
                                            if (erc != ErrorCode.NoError)
                                            {
                                                Logger.WriteErrorLog(string.Format("Not able to connect to PLC. Error = {0}", sPhantomClient.LastErrorString));
                                                Thread.Sleep(1000);
                                                sPhantomClient.ClearLastError();
                                                currentStatus = 2;
                                                if(PrevStatus != currentStatus)
                                                {
                                                    DatabaseAccess.UpdatePhantomMachineStatus(this._interfaceId, 2, DateTime.Now);
                                                    PrevStatus = currentStatus;
                                                }
                                                    
                                                Thread.Sleep(1000);
                                                continue;
                                            }
                                            else
                                            {                                                
                                                currentStatus = 1;
                                                //if (PrevStatus != currentStatus)
                                                {
                                                    DatabaseAccess.UpdatePhantomMachineStatus(this._interfaceId, 1, DateTime.Now);
                                                    PrevStatus = currentStatus;
                                                }
                                                Logger.WriteDebugLog(string.Format("Connected Successfully To PLC"));

                                            }
                                        }
                                        else
                                        {
                                            currentStatus = 1;
                                            if (PrevStatus != currentStatus)
                                            {
                                                PrevStatus = currentStatus;
                                                DatabaseAccess.UpdatePhantomMachineStatus(this._interfaceId, 1, DateTime.Now);                                               
                                            }
                                        }
                                    }
                                    else
                                    {
                                       // Logger.WriteDebugLog(string.Format("Not able to connect to the device. Please check the PLC IP address {0} and Ping status.", this._ipAddress));

                                        currentStatus = 2;
                                        if (PrevStatus != currentStatus)
                                        {
                                            PrevStatus = currentStatus;
                                            DatabaseAccess.UpdatePhantomMachineStatus(this._interfaceId, 2, DateTime.Now);
                                            Logger.WriteDebugLog(string.Format("Not able to connect to the device. Please check the PLC IP address {0} and Ping status.", this._ipAddress));
                                        }
                                        continue;
                                    }

                                    machineId = (ushort)sPhantomClient.Read(string.Format("DB{0}.DBW0", DBNumber));
                                    operationID = Convert.ToByte(sPhantomClient.Read(string.Format("DB{0}.DBB2", DBNumber)));
                                    supervisorID = (ushort)sPhantomClient.Read("DB109.DBW0");

                                    if (operationID == 40)
                                    {
                                        bool PALET_A = (bool)sPhantomClient.Read(string.Format("DB{0}.DBX148.0", DBNumber));
                                        bool PALET_B = (bool)sPhantomClient.Read(string.Format("DB{0}.DBX148.1", DBNumber));

                                        if (PALET_A)
                                        {
                                            byte readDataB = Convert.ToByte(sPhantomClient.Read(string.Format("DB{0}.DBB138", DBNumber)));
                                            if (readDataB == 1)
                                            {
                                                BarcodeString = sPhantomClient.Read(DataType.DataBlock, DBNumber, 76 + 2, VarType.String, 60).ToString().Trim(char.MinValue).Trim();

                                                if (BarcodeString.Length > 0)
                                                {
                                                    Logger.WriteDebugLog(string.Format("Barcode_Data From PLC : {0} for Machine - {1}, Operation - {2}. Supervisor - {3} ", BarcodeString, machineId, operationID, supervisorID));

                                                    var data = BarcodeString.Split('-');
                                                    Part_No = data[0];
                                                    Rev_No = data[1];
                                                    Part_Sl_No = data[4];
                                                    Heat_Code_No = data[5];
                                                    Component = Part_No + "-" + Rev_No;
                                                    Supplier_Code = data[3];
                                                    var LoadDate = ((uint)sPhantomClient.Read(string.Format("DB{0}.DBD{1}", DBNumber, "140"))).ConvertToInt().ToString("00000000");
                                                    var LoadTime = ((uint)sPhantomClient.Read(string.Format("DB{0}.DBD{1}", DBNumber, "144"))).ConvertToInt().ToString("000000");

                                                    Logger.WriteDebugLog("Load DateTime : " + LoadDate + " " + LoadTime);
                                                    DateTime.TryParseExact(LoadDate + " " + LoadTime, "yyyyMMdd HHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out LoadDateTime);
                                                    DatabaseAccess.InsertToPhantomData(machineId, operationID, Component, Part_Sl_No, Heat_Code_No, Supplier_Code, supervisorID, LoadDateTime);
                                                    sPhantomClient.Write(string.Format("DB{0}.DBB138", DBNumber), 2);
                                                    DatabaseAccess.UpdatePhantomMachineStatus(this._interfaceId, 1, DateTime.Now);
                                                }
                                            }
                                        }
                                        else if (PALET_B)
                                        {
                                            byte readDataA = Convert.ToByte(sPhantomClient.Read(string.Format("DB{0}.DBB66", DBNumber)));

                                            if (readDataA == 1)
                                            {
                                                BarcodeString = sPhantomClient.Read(DataType.DataBlock, DBNumber, 4 + 2, VarType.String, 60).ToString().Trim(char.MinValue).Trim();

                                                if (BarcodeString.Length > 0)
                                                {
                                                    Logger.WriteDebugLog(string.Format("Barcode_Data From PLC : {0} for Machine - {1}, Operation - {2}. Supervisor - {3} ", BarcodeString, machineId, operationID, supervisorID));

                                                    var data = BarcodeString.Split('-');
                                                    Part_No = data[0];
                                                    Rev_No = data[1];
                                                    Part_Sl_No = data[4];
                                                    Heat_Code_No = data[5];
                                                    Component = Part_No + "-" + Rev_No;
                                                    Supplier_Code = data[3];
                                                    var LoadDate = ((uint)sPhantomClient.Read(string.Format("DB{0}.DBD{1}", DBNumber, "68"))).ConvertToInt().ToString("00000000");
                                                    var LoadTime = ((uint)sPhantomClient.Read(string.Format("DB{0}.DBD{1}", DBNumber, "72"))).ConvertToInt().ToString("000000");

                                                    Logger.WriteDebugLog("Load DateTime : " + LoadDate + " " + LoadTime);
                                                    DateTime.TryParseExact(LoadDate + " " + LoadTime, "yyyyMMdd HHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out LoadDateTime);
                                                    DatabaseAccess.InsertToPhantomData(machineId, operationID, Component, Part_Sl_No, Heat_Code_No, Supplier_Code, supervisorID, LoadDateTime);
                                                    sPhantomClient.Write(string.Format("DB{0}.DBB66", DBNumber), 2);
                                                    DatabaseAccess.UpdatePhantomMachineStatus(this._interfaceId, 1, DateTime.Now);
                                                }
                                            }

                                        }
                                    }
                                    else
                                    {
                                        byte readData = Convert.ToByte(sPhantomClient.Read(string.Format("DB{0}.DBB66", DBNumber)));
                                        if (readData == 1)
                                        {
                                            BarcodeString = sPhantomClient.Read(DataType.DataBlock, DBNumber, 4 + 2, VarType.String, 60).ToString().Trim(char.MinValue).Trim();

                                            if (BarcodeString.Length > 0)
                                            {
                                                Logger.WriteDebugLog(string.Format("Barcode_Data From PLC : {0} for Machine - {1}, Operation - {2}. Supervisor - {3} ", BarcodeString, machineId, operationID, supervisorID));

                                                var data = BarcodeString.Split('-');
                                                Part_No = data[0];
                                                Rev_No = data[1];
                                                Part_Sl_No = data[4];
                                                Heat_Code_No = data[5];
                                                Component = Part_No + "-" + Rev_No;
                                                Supplier_Code = data[3];
                                                var LoadDate = ((uint)sPhantomClient.Read(string.Format("DB{0}.DBD{1}", DBNumber, "68"))).ConvertToInt().ToString("00000000");
                                                var LoadTime = ((uint)sPhantomClient.Read(string.Format("DB{0}.DBD{1}", DBNumber, "72"))).ConvertToInt().ToString("000000");

                                                Logger.WriteDebugLog("Load DateTime : " + LoadDate + " " + LoadTime);
                                                DateTime.TryParseExact(LoadDate + " " + LoadTime, "yyyyMMdd HHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out LoadDateTime);
                                                DatabaseAccess.InsertToPhantomData(machineId, operationID, Component, Part_Sl_No, Heat_Code_No, Supplier_Code, supervisorID, LoadDateTime);
                                                sPhantomClient.Write(string.Format("DB{0}.DBB66", DBNumber), 2);
                                                DatabaseAccess.UpdatePhantomMachineStatus(this._interfaceId, 1, DateTime.Now);
                                            }
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    PrevStatus = 0;
                                    Logger.WriteErrorLog(ex.Message);
                                    if (sPhantomClient != null)
                                    {
                                        sPhantomClient.Close();
                                    }
                                }
                                finally
                                {
                                    if (sPhantomClient != null)
                                    {
                                        //sPhantomClient.Close();
                                    }
                                }
                            }
                            if (sPhantomClient != null)
                            {
                                sPhantomClient.Close();
                            }
                        }
                        #endregion

                        #region "ROBO"
                        else if (_machineDTO.Process.Equals("ROBO", StringComparison.OrdinalIgnoreCase))
                        {
                            Logger.WriteDebugLog("Reading ROBO Machines data.....");

                            if (CheckPingStatus(this._ipAddress))
                            {
                                if(RoboClient == null)
                                    RoboClient = new Plc(CpuType.S71200, this._ipAddress, 0, 1);

                                int OPN = 10;
                                int OPR;
                                int SUP;
                                bool ReadFlag = false;

                                BitArray _alarmsBits_Previous = null;
                                //Dictionary<int, BitArray> _previous_AlarmBits = new Dictionary<int, BitArray>();
                                //string PartNumber = "5646751";
                                string SupplierCode = "S9999";
                                string PartSerialNumber = "H10F";
                                string HeatCode = "H9999";
                                //string RevNumber = "DX00";
                                string Comp = "9999";
                                int PartCount = 0;
                                int Output = -1;
                                DateTime UpStartDatetime = DateTime.MinValue;
                                DateTime UpEndDatetime = DateTime.MinValue;
                                DataTable dtAlarmMasterInfo = DatabaseAccess.GetAlarmMasterInfo();
                                //int Comm = 100;
                                while (true)
                                {
                                    if (ServiceStop.stop_service == 1)
                                        break;

                                    try
                                    {
                                        if (CheckPingStatus(this._ipAddress))
                                        {
                                            if (RoboClient.IsConnected == false)
                                            {
                                                ErrorCode erc = RoboClient.Open();
                                                if (erc != ErrorCode.NoError)
                                                {
                                                    Logger.WriteErrorLog(string.Format("Not able to connect to PLC. Error = {0}", RoboClient.LastErrorString));
                                                    RoboClient.ClearLastError();
                                                    Thread.Sleep(1000);
                                                    continue;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            Logger.WriteDebugLog(string.Format("No ping to the device. Please check the PLC IP address {0} and Ping status.", this._ipAddress));
                                            Thread.Sleep(1000);
                                            continue;
                                        }

                                        ReadFlag = (bool)RoboClient.Read(string.Format("DB{0}.DBX28.0", DBNumber));
                                        if (ReadFlag)
                                        {
                                            Logger.WriteDebugLog("Data Read flag (DBX28.0) is high, Reading Data From PLC.");

                                            var ReadUpStartDate = ((uint)RoboClient.Read(string.Format("DB{0}.DBD{1}", DBNumber, "10"))).ConvertToInt().ToString("00000000");
                                            var ReadUpStartTime = ((uint)RoboClient.Read(string.Format("DB{0}.DBD{1}", DBNumber, "14"))).ConvertToInt().ToString("000000");

                                            var ReadUpEndDate = ((uint)RoboClient.Read(string.Format("DB{0}.DBD{1}", DBNumber, "18"))).ConvertToInt().ToString("00000000");
                                            var ReadUpEndTime = ((uint)RoboClient.Read(string.Format("DB{0}.DBD{1}", DBNumber, "22"))).ConvertToInt().ToString("000000");

                                            Logger.WriteDebugLog("Start DateTime : " + ReadUpStartDate + " " + ReadUpStartTime+ " Start DateTime : " + ReadUpEndDate + " " + ReadUpEndTime);
                                            DateTime.TryParseExact(ReadUpStartDate + " " + ReadUpStartTime, "yyyyMMdd HHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out UpStartDatetime);
                                            DateTime.TryParseExact(ReadUpEndDate + " " + ReadUpEndTime, "yyyyMMdd HHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out UpEndDatetime);

                                            DatabaseAccess.GetDefaultValues(out OPR, out SUP);

                                            var type1Str = string.Format("START-1-{0}-[{1}]-{2}-{3}-{4}-{5}-{6}-{7}-{8}-{9}-{10}-{11}-{12}-END", _machineDTO.InterfaceId, Comp, OPN, OPR, PartCount, HeatCode, PartSerialNumber, SupplierCode, SUP, UpStartDatetime.ToString("yyyyMMdd"), UpStartDatetime.ToString("HHmmss"), UpEndDatetime.ToString("yyyyMMdd"), UpEndDatetime.ToString("HHmmss"));
                                            SaveStringToTPMFile(type1Str);
                                            DatabaseAccess.ProcessDataString(type1Str, out Output);

                                            #region Handle Alarm
                                            if (dtAlarmMasterInfo != null && dtAlarmMasterInfo.Rows.Count > 0)
                                            {
                                                int startAddress = dtAlarmMasterInfo.AsEnumerable().Max(X => X.Field<int>("AlarmNo"));
                                                int endAddress = dtAlarmMasterInfo.AsEnumerable().Min(X => X.Field<int>("AlarmNo"));
                                                int addr = startAddress;
                                                var datas = RoboClient.ReadBytes(DataType.Memory, DBNumber, startAddress, (endAddress - startAddress) + 1);
                                                BitArray alarmsBits_current = new BitArray(datas.ToArray());

                                                if (_alarmsBits_Previous == null)
                                                {
                                                    _alarmsBits_Previous = new BitArray(alarmsBits_current.Length);
                                                }
                                                for (int pos = 0; pos < alarmsBits_current.Length; pos++)
                                                {
                                                    if (!_alarmsBits_Previous[pos] && alarmsBits_current[pos])
                                                    {
                                                        string _alarm_Address = string.Format("{0}.{1}", (addr + pos / 8), (pos % 8));
                                                        var Raised_Alarm = dtAlarmMasterInfo.AsEnumerable().Where(x => x.Field<string>("AlarmAddress").Equals(_alarm_Address)).FirstOrDefault();
                                                        if (Raised_Alarm != null)
                                                        {
                                                            DatabaseAccess.UpdateAlarmRaisedToDB(this._interfaceId, Raised_Alarm);
                                                        }
                                                    }
                                                }
                                                _alarmsBits_Previous = alarmsBits_current;
                                            } 
                                            #endregion
                                        }

                                        RoboClient.Write(string.Format("DB{0}.DBX28.0", DBNumber), false);
                                        Logger.WriteDebugLog("Data Read flag (DBX28.0) is set to Low. Stopped Reading Data From PLC.");


                                        //#region Communication
                                        //try
                                        //{
                                        //    RoboClient.Write(string.Format("DB{0}.DBB104", DBNumber), (byte)Comm);
                                        //}
                                        //catch (Exception ex)
                                        //{
                                        //    Logger.WriteErrorLog(ex.ToString());
                                        //}
                                        //if (Comm == 100)
                                        //    Comm = 200;
                                        //else
                                        //    Comm = 100;
                                        //#endregion
                                    }
                                    catch (Exception ex)
                                    {
                                        Logger.WriteDebugLog("Exception In ROBO Process While Loop : " + ex.Message);
                                        if (RoboClient != null)
                                        {
                                            RoboClient.Close();
                                        }
                                    }
                                    finally
                                    {
                                        //Thread.Sleep(1000);
                                    }
                                }
                                if (RoboClient != null)
                                {
                                    RoboClient.Close();
                                }
                            }
                        }
                        #endregion
                    }
                    #endregion

                    #region "Modbus"
                    else if (_protocol.Equals("modbus", StringComparison.OrdinalIgnoreCase))
                    {
                        Logger.WriteDebugLog("Entered into MODBUS Protocol");
                        ushort prevAckNumber = ushort.MinValue;
                        ushort prevAckNumber28Type = ushort.MinValue;
                        //ModbusIpMaster master = default(ModbusIpMaster);
                        commNo = 100;
                        DateTime timeToUpdateDate = DateTime.MinValue;
                        while (true)
                        {
                            #region StopService
                            if (ServiceStop.stop_service == 1)
                            {
                                try
                                {
                                    if (master != null)
                                    {
                                        master.Dispose();
                                        master = null;
                                    }
                                    Logger.WriteDebugLog("stopping the service. coming out of main while loop.");
                                    break;
                                }
                                catch (Exception ex)
                                {
                                    Logger.WriteErrorLog(ex.Message);
                                    break;
                                }
                            }
                            #endregion

                            //Why below condition?
                            try
                            {
                                master = ConnectModBus();
                                if (master != null)
                                {
                                    Handling28TypeString(ref master, HoldingRegister28TypeString, BytesToRead28Type, ref prevAckNumber28Type);
                                    WriteParametersToPLC(ref master);
                                    HandlingType7String(ref master, ref prevAckNumber);
                                    HandlingType_6_7String(ref master, ref prevAckNumber);
                                    InvokeDailyCheckListActivity(ref master);
                                    InvokePreventiveMaintenanceActivity(ref master);
                                    HandingProcessParameter_String(ref master);
                                }
                                else
                                {
                                    Logger.WriteDebugLog("Disconnected from network (No ping).");
                                    Thread.Sleep(1000);
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.WriteErrorLog(ex.ToString());
                            }
                            finally
                            {
                                //if (master != null)
                                //{
                                //    master.Dispose();
                                //}
                                //master = null;
                            }
                            Thread.Sleep(1500);
                        }
                    }
                    #endregion
                }
                catch (Exception ex)
                {
                    Logger.WriteErrorLog("Exception from main while loop : " + ex.ToString());
                    Thread.Sleep(2000);
                }
            }
            Logger.WriteDebugLog("End of while loop." + Environment.NewLine + "------------------------------------------");
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
                catch(Exception exx)
                {
                    Logger.WriteErrorLog(exx.Message);
                }
            }
            try
            {
                File.Move(sourceFileName, destFileName);
            }
            catch(Exception exx)
            {
                Logger.WriteDebugLog(exx.Message);
            }
        }
        private string getValidationResult(string validationResult)
        {
            string result = "0";
            // '<' + @SlNo + '-' + @component + '@' + @PrevOpnFromAutodata + '#' + '4>' --(Rejection)
            //split the string and get the result
            if (validationResult.Contains("#"))
            {
                Logger.WriteDebugLog("Validation Result from database : " + validationResult);
                var strs = validationResult.Split(new char[] { '<', '>', '@', '#' }, StringSplitOptions.RemoveEmptyEntries);
                result = strs[strs.Length - 1].Trim();
            }
            return result == "1" ? "1" : "2";
        }

        //private void UpdateSPCAutoData(string SPCsourceFile, string SPCDestFile, string _machineId, int _opnID)
        //{
        //    try
        //    {
        //        string[] spcData = File.ReadAllLines(SPCsourceFile);
        //        DateTime updatedts = DateTime.MinValue;
        //        foreach (string data in spcData)
        //        {
        //            string[] spl = data.Split(',');

        //            DateTime.TryParseExact(spl[2] + " " + spl[3], @"dd/MM//yyyy HH:mm:ss", enUS, DateTimeStyles.None, out updatedts);
        //            string partno = spl[4];
        //            string partSlno = spl[5];
        //            string Characteristics = spl[6];
        //            double actualValue = Convert.ToDouble(spl[7]);
        //            double nominalValue = Convert.ToDouble(spl[8]);
        //            double USL = Convert.ToDouble(spl[9]);
        //            double LSL = Convert.ToDouble(spl[10]);

        //            DatabaseAccess.InsertToSPCAutodata(this._machineId, partno, this._opnID, Characteristics, actualValue);
        //        }

        //        MoveWithReplace(SPCsourceFile, SPCDestFile);
        //        Logger.WriteDebugLog("SPC file is moved to Processed folder successfully.");
        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.WriteErrorLog(ex.ToString());
        //    }
        //}
        private void UpdateSPCData(string SPCsourceFile, string SPCDestFile, string _machineId, int _opnID)
        {
            try
            {
                if(!File.Exists(SPCsourceFile))
                {
                    Logger.WriteDebugLog("Did not found SPC file at " + SPCsourceFile);
                    return;
                }

                string[] spcData = File.ReadAllLines(SPCsourceFile);
                DateTime updatedts = DateTime.MinValue;
                string fileName = Path.GetFileNameWithoutExtension(SPCsourceFile);

                var spilitValues = fileName.Split('-');
                string componentID = spilitValues.Length > 0 ? spilitValues[0]+"-"+spilitValues[1] : "Comp1"; //spilitValues[0].Trim().Replace(" ", "") : "Comp1";
                string slNo = spilitValues.Length >= 1 ? spilitValues[4].Trim() : "999999";

                foreach (string data in spcData.Skip(1))
                {
                    try
                    {
                        string[] spl = data.Split(',');

                        if (!DateTime.TryParseExact(spl[2].Trim() + " " + spl[3].Trim(), @"dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out updatedts))
                        {
                            Logger.WriteDebugLog(string.Format("Wrong date {0} format, expected date format : dd/MM/yyyy HH:mm:ss ", spl[2].Trim() + " " + spl[3].Trim()));
                            continue;
                        }
                        string partno = spilitValues[0];// componentID;
                        string partSlno = slNo;
                        string Characteristics = spl[5].Trim();
                        double actualValue = Convert.ToDouble(spl[6].Trim());
                        double nominalValue = Convert.ToDouble(spl[7].Trim());
                        double USL = Convert.ToDouble(spl[8].Trim());
                        double LSL = Convert.ToDouble(spl[9].Trim());

                        DatabaseAccess.InsertToSPCAutodata(this._machineDTO.InterfaceId, componentID, this._opnID, Characteristics, actualValue, partSlno);
                        DatabaseAccess.InsertToSPCCharacteristic(this._machineId, componentID, this._opnID, partSlno, Characteristics, nominalValue, LSL, USL);
                    }
                    catch(Exception ww)
                    {
                        Logger.WriteErrorLog(ww.ToString());
                    }
                }

                MoveWithReplace(SPCsourceFile, SPCDestFile);
                Logger.WriteDebugLog("SPC file is moved to Local app Processed folder successfully.");
            }
            catch (Exception ex)
            {
                Logger.WriteErrorLog(ex.ToString());
            }
        }

        
       

        private void SaveStringToTxtFile(string strTxtData)
        {
            StreamWriter writer = default(StreamWriter);
            string sourceFile = string.Empty;
            NetworkConnection nc = null;
            try
            {               
                var EquatorPhantomFilePathWithName = this._machineDTO.ACKFolderPath;

                Logger.WriteDebugLog("Folder = " + Path.GetDirectoryName(EquatorPhantomFilePathWithName));

                if (!string.IsNullOrEmpty(_userName) && !string.IsNullOrEmpty(_password))
                {
                    try
                    {
                        nc = new NetworkConnection(Path.GetDirectoryName(EquatorPhantomFilePathWithName), new NetworkCredential(_userName, _password));
                    }
                    catch (Exception exx)
                    {
                        Logger.WriteErrorLog(exx.ToString());
                    }
                }


                if (!string.IsNullOrEmpty( EquatorPhantomFilePathWithName))
                {
                    writer = new StreamWriter(EquatorPhantomFilePathWithName, false);
                    writer.Write(strTxtData);
                    writer.Flush();
                    PhantomStatus = true;
                    Logger.WriteDebugLog("Data written to file successfully.");
                }
            }
            catch (Exception ex)
            {
                Logger.WriteErrorLog(ex.ToString());
                Thread.Sleep(1000);
            }
            finally
            {
                if (nc != null) nc.Dispose();
                if (writer != null)
                {
                    writer.Close();
                    writer.Dispose();
                }
            }
        }
        private void CreateDir(string path)
        {
            if (string.IsNullOrEmpty(path))
                return;
            try
            {
                Directory.CreateDirectory(path);
            }
            catch (Exception ex)
            {

                Logger.WriteDebugLog("Not able to access\\create directory " + path);
            }
        }
        private void Handling28TypeString(ref ModbusIpMaster master, ushort HoldingRegister28TypeString, ushort BytesToRead28Type, ref ushort prevAckNumber28Type)
        {
            if (master == null)
            {
                Logger.WriteDebugLog("Getting Null for ModbusIpMaster object master. Exiting From Handling28TypeString()");
                return;
            }
            ushort[] output = null;
            ushort ackNumber = ushort.MinValue;
            string outputString = string.Empty;
            string messageID = string.Empty;
            string CompID = string.Empty;
            bool isException = false;
            int count = 0;
            while (isException || count == 0)
            {

                #region StopService
                if (ServiceStop.stop_service == 1)
                {
                    try
                    {
                        if (master != null)
                        {
                            master.Dispose();
                            master = null;
                        }

                        Logger.WriteDebugLog("stopping the service. coming out of main while loop.");
                        break;
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteErrorLog(ex.Message);
                        break;
                    }
                }
                #endregion

                count++;
                if (count > 4)
                {
                    break;
                }
                if (isException)
                {
                    master = ConnectModBus();
                }
                if (master == null)
                {
                    isException = true;
                    continue;
                }
                else
                {
                    isException = false;
                }
                output = null;

                try
                {
                    output = master.ReadHoldingRegisters(HoldingRegister28TypeString, BytesToRead28Type);
                    // Logger.WriteDebugLog("Type 28 string reading is successfull.");
                }
                catch (Exception ex)
                {
                    isException = true;
                    if (master != null) master.Dispose();
                    Logger.WriteErrorLog(ex.ToString());
                    continue;
                }
                outputString = string.Empty;
                outputString = GetString(output).Trim(char.MinValue);//.TrimEnd(new char[] { '0' })
                if (outputString != null && outputString.Length > 0)
                {
                    SaveStringToTPMFile(outputString);
                    Logger.WriteDebugLog(string.Format("String recieved : {0}", outputString));

                    //Process Parameter - Done by Prince

                }
                if (outputString != null && outputString.Length >= 4)
                {
                    messageID = string.Empty;
                    messageID = outputString.Substring(0, 4);
                    //send ack back
                    ackNumber = ushort.MinValue;
                    ushort.TryParse(messageID, out ackNumber);

                    //Do we need to pass prevAckNumber28Type as ref??
                    //if (prevAckNumber28Type != ackNumber)
                    //{
                    //Why to insert 2 times same string???
                    //Can we write below line after sending ack or insert to database??
                    try
                    {
                        master.WriteSingleRegister(AckAddressFor28TypeString, ackNumber);
                        Logger.WriteDebugLog(string.Format("Ack {0} send for string {1}", messageID, outputString));
                    }
                    catch (Exception ex)
                    {
                        isException = true;
                        if (master != null) master.Dispose();
                        Logger.WriteErrorLog(ex.ToString());
                        continue;
                    }
                    //}
                    //------------------------------------------Sending Response--------------------------------------------------------//    
                    //Why to insert 2 times same string???
                    string tempString = DatabaseAccess.GetStatus28Type(outputString.Trim());
                    prevAckNumber28Type = ackNumber;
                    Logger.WriteDebugLog("Status for 28 type string updation started. " + tempString);
                    string tempStatus = "0005" + tempString;      //GetStatusOF28Type(outputString);
                    ushort[] tempStatusUshort = GetuShort("0004CMPSTSBEGIN");
                    int retry = 0;
                    do
                    {
                        #region StopService
                        if (ServiceStop.stop_service == 1)
                        {
                            try
                            {
                                if (master != null)
                                {
                                    master.Dispose();
                                    master = null;
                                }

                                Logger.WriteDebugLog("stopping the service. coming out of main while loop.");
                                break;
                            }
                            catch (Exception ex)
                            {

                                Logger.WriteErrorLog(ex.Message);
                                break;
                            }
                        }
                        #endregion
                        if (retry > 0)
                        {
                            Thread.Sleep(4000);
                        }
                        retry++;
                        if (retry == 4)
                        {
                            Logger.WriteDebugLog("software retried thrice to write 0004CMPSTSBEGIN");
                            break;
                        }

                        try
                        {
                            master.WriteMultipleRegisters(0, HoldingRegisterDateAndStatus, tempStatusUshort);
                            Logger.WriteDebugLog(string.Format("{0} sent from computer.", "0004CMPSTSBEGIN"));
                            Thread.Sleep(1000);
                        }
                        catch (Exception ex)
                        {
                            isException = true;
                            if (master != null) master.Dispose();
                            Logger.WriteErrorLog(ex.ToString());
                            master = ConnectModBus();
                            continue;
                        }
                    } while (isException);

                    retry = 0;
                    //read Ack number 0004                    
                    do
                    {
                        #region StopService
                        if (ServiceStop.stop_service == 1)
                        {
                            try
                            {
                                if (master != null)
                                {
                                    master.Dispose();
                                    master = null;
                                }

                                Logger.WriteDebugLog("stopping the service. coming out of main while loop.");
                                break;
                            }
                            catch (Exception ex)
                            {

                                Logger.WriteErrorLog(ex.Message);
                                break;
                            }
                        }
                        #endregion
                        if (retry > 0)
                        {
                            Thread.Sleep(4000);
                        }
                        retry++;
                        if (retry == 4)
                        {
                            Logger.WriteDebugLog("software retried thrice to get ack 0004");
                            break;
                        }
                        try
                        {
                            output = master.ReadHoldingRegisters(DateAndStatusAckAddress, numberOfBytesToRead);
                            Thread.Sleep(1000);
                        }
                        catch (Exception ex)
                        {
                            isException = true;
                            if (master != null) master.Dispose();
                            Logger.WriteErrorLog(ex.ToString());
                            master = ConnectModBus();
                            continue;
                        }
                        outputString = GetString(output).Trim(char.MinValue);//.TrimEnd(new char[] { '0' })
                        Logger.WriteDebugLog(string.Format("Ack {0} recieved from machine.", outputString));

                    } while (outputString != "0004");
                    if (outputString == "0004")
                    {
                        //sending data
                        tempStatusUshort = null;
                        tempStatusUshort = GetuShort(tempStatus);

                        retry = 0;

                        do
                        {
                            #region StopService
                            if (ServiceStop.stop_service == 1)
                            {
                                try
                                {
                                    if (master != null)
                                    {
                                        master.Dispose();
                                        master = null;
                                    }

                                    Logger.WriteDebugLog("stopping the service. coming out of main while loop.");
                                    break;
                                }
                                catch (Exception ex)
                                {

                                    Logger.WriteErrorLog(ex.Message);
                                    break;
                                }
                            }
                            #endregion
                            if (retry > 0)
                            {
                                Thread.Sleep(4000);
                            }
                            retry++;
                            if (retry == 4)
                            {
                                Logger.WriteDebugLog("software retried thrice to write " + tempStatus);
                                break;
                            }

                            try
                            {
                                master.WriteMultipleRegisters(0, HoldingRegisterDateAndStatus, tempStatusUshort);
                                Logger.WriteDebugLog(string.Format("{0} sent from computer.", tempStatus));
                                isException = false;
                                Thread.Sleep(1000);
                            }
                            catch (Exception ex)
                            {
                                isException = true;
                                if (master != null) master.Dispose();
                                Logger.WriteErrorLog(ex.ToString());
                                master = ConnectModBus();
                                continue;
                            }
                        } while (isException);

                        //read Ack number 0005
                        retry = 0;
                        do
                        {
                            #region StopService
                            if (ServiceStop.stop_service == 1)
                            {
                                try
                                {
                                    if (master != null)
                                    {
                                        master.Dispose();
                                        master = null;
                                    }

                                    Logger.WriteDebugLog("stopping the service. coming out of main while loop.");
                                    break;
                                }
                                catch (Exception ex)
                                {
                                    Logger.WriteErrorLog(ex.Message);
                                    break;
                                }
                            }
                            #endregion
                            if (retry > 0)
                            {
                                Thread.Sleep(4000);
                            }
                            retry++;
                            if (retry == 4)
                            {
                                Logger.WriteDebugLog("software tried thrice to get ack 0005");
                                break;
                            }
                            try
                            {
                                output = master.ReadHoldingRegisters(DateAndStatusAckAddress, numberOfBytesToRead);
                                Thread.Sleep(1000);
                            }
                            catch (Exception ex)
                            {
                                isException = true;
                                if (master != null) master.Dispose();
                                Logger.WriteErrorLog(ex.ToString());
                                master = ConnectModBus();
                                continue;
                            }
                            outputString = GetString(output).Trim(char.MinValue);//.TrimEnd(new char[] { '0' })
                            Logger.WriteDebugLog(string.Format("Ack {0} recieved from machine.", outputString));
                        } while (outputString != "0005");
                        if (outputString == "0005")
                        {
                            Thread.Sleep(1000);
                            tempStatusUshort = null;
                            //sending END header
                            tempStatusUshort = GetuShort("0006CMPSTSEND");
                            retry = 0;
                            do
                            {
                                #region StopService
                                if (ServiceStop.stop_service == 1)
                                {
                                    try
                                    {
                                        if (master != null)
                                        {
                                            master.Dispose();
                                            master = null;
                                        }

                                        Logger.WriteDebugLog("stopping the service. coming out of main while loop.");
                                        break;
                                    }
                                    catch (Exception ex)
                                    {
                                        Logger.WriteErrorLog(ex.Message);
                                        break;
                                    }
                                }
                                #endregion
                                if (retry > 0)
                                {
                                    Thread.Sleep(4000);
                                }
                                retry++;
                                if (retry == 4)
                                {
                                    Logger.WriteDebugLog("software retried thrice to write 0006CMPSTSEND");
                                    break;
                                }

                                try
                                {
                                    master.WriteMultipleRegisters(0, HoldingRegisterDateAndStatus, tempStatusUshort);
                                    Logger.WriteDebugLog(string.Format("{0} sent from computer.", "0006CMPSTSEND"));
                                    isException = false;
                                    Thread.Sleep(1000);
                                }
                                catch (Exception ex)
                                {
                                    isException = true;
                                    if (master != null) master.Dispose();
                                    Logger.WriteErrorLog(ex.ToString());
                                    master = ConnectModBus();
                                    continue;
                                }
                            } while (isException);
                            //read Ack number 0006
                            retry = 0;
                            do
                            {
                                #region StopService
                                if (ServiceStop.stop_service == 1)
                                {
                                    try
                                    {
                                        if (master != null)
                                        {
                                            master.Dispose();
                                            master = null;
                                        }

                                        Logger.WriteDebugLog("stopping the service. coming out of main while loop.");
                                        break;
                                    }
                                    catch (Exception ex)
                                    {
                                        Logger.WriteErrorLog(ex.Message);
                                        break;
                                    }
                                }
                                #endregion
                                if (retry > 0)
                                {
                                    Thread.Sleep(4000);
                                }
                                retry++;
                                if (retry == 4)
                                {
                                    Logger.WriteDebugLog("software retried thrice to get ack 0006");
                                    break;
                                }
                                try
                                {
                                    output = master.ReadHoldingRegisters(DateAndStatusAckAddress, numberOfBytesToRead);
                                    Thread.Sleep(1000);
                                }
                                catch (Exception ex)
                                {
                                    isException = true;
                                    if (master != null) master.Dispose();
                                    Logger.WriteErrorLog(ex.ToString());
                                    master = ConnectModBus();
                                    continue;
                                }
                                outputString = GetString(output).Trim(char.MinValue);//.TrimEnd(new char[] { '0' })
                                Logger.WriteDebugLog(string.Format("Ack {0} recieved from machine.", outputString));

                            } while (outputString != "0006");
                            if (outputString == "0006")
                            {
                                Logger.WriteDebugLog(tempStatus + " . Status sent successfull to PLC.");
                            }
                            else
                            {
                                Logger.WriteDebugLog("Not able to get ack for 0006CMPSTSEND");
                            }
                        }
                        else
                        {
                            Logger.WriteDebugLog("Not able to get ack for " + tempStatus);
                        }
                    }
                    else
                    {
                        Logger.WriteDebugLog("Not able to get ack for 0004CMPSTSBEGIN");
                    }
                    // code to update status based upon procedure output.
                }

                #region Communication
                //send communicationNo
                try
                {
                    master.WriteSingleRegister(HoldingRegisterForCommunictaion, commNo);
                }
                catch (Exception ex)
                {
                    isException = true;
                    if (master != null) master.Dispose();
                    Logger.WriteErrorLog(ex.ToString());
                    continue;
                }
                if (commNo == 100)
                {
                    commNo = 200;
                }
                else commNo = 100;

                #endregion
            }
        }

        private void HandlingType7String(ref ModbusIpMaster master, ref ushort prevAckNumber)
        {
            if (master == null)
            {
                Logger.WriteDebugLog("Getting Null for ModbusIpMaster object master. Exiting From HandlingType7String()");
                return;
            }
            ushort[] output = null;
            string outputString = string.Empty;
            ushort ackNumber = 0;
            ushort[] currentDate = null;
            string tempDatenStatus = string.Empty;
            string messageID = string.Empty;
            #region Datacollection
            bool isException = false;
            int count = 0;
            while (isException || count == 0)
            {
                #region StopService
                if (ServiceStop.stop_service == 1)
                {
                    try
                    {
                        if (master != null)
                        {
                            master.Dispose();
                            master = null;
                        }

                        Logger.WriteDebugLog("stopping the service. coming out of main while loop.");
                        break;
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteErrorLog(ex.Message);
                        break;
                    }
                }
                #endregion
                count++;
                if (count > 4)
                {
                    break;
                }
                if (isException)
                {
                    master = ConnectModBus();
                }
                if (master == null)
                {
                    isException = true;
                    continue;
                }
                else
                {
                    isException = false;
                }
                try
                {
                    output = master.ReadHoldingRegisters(holdingRegisterStartAddress, numberOfBytesToRead);
                    //Logger.WriteDebugLog("Reading tpm trak strings successfully.");
                }
                catch (Exception ex)
                {
                    isException = true;
                    if (master != null) master.Dispose();
                    Logger.WriteErrorLog(ex.ToString());
                    continue;
                }
                outputString = GetString(output).Trim(char.MinValue);//.TrimEnd(new char[] { '0' });

                if (outputString != null && outputString.Length > 0)
                {
                    SaveStringToTPMFile(outputString);
                    Logger.WriteDebugLog(string.Format("String recieved : {0}", outputString));
                }
                if (outputString != null && outputString.Length >= 4)
                {
                    ackNumber = ushort.MinValue;
                    messageID = outputString.Substring(0, 4);
                    //send ack back
                    ushort.TryParse(messageID, out ackNumber);
                    //if (prevAckNumber != ackNumber)
                    // {
                    try
                    {
                        master.WriteSingleRegister(AckHoldingRegisterAddress, ackNumber);
                        prevAckNumber = ackNumber;
                        Logger.WriteDebugLog(string.Format("Ack {0} for tpm trak string type sent. - {1}", messageID, outputString));
                        //Is delay required here ??
                    }
                    catch (Exception ex)
                    {
                        isException = true;
                        if (master != null) master.Dispose();
                        Logger.WriteErrorLog(ex.ToString());
                        continue;
                    }
                    #region Type6 7 commented code
                    ////What will happen if ack not received by device (prevAckNumber != ackNumber)??   
                    //if (!outputString.Contains("START-6-"))
                    if (!outputString.Contains("START-6-") || (outputString.Contains("START-6-") && outputString.Split('-')[3] == "P600"))
                    {
                        ProcessFile(outputString, this._ipAddress, this._portNo.ToString(), MName);
                    }
                    //#region  DateTime updation
                    //if (outputString.Contains("START-7-") && outputString.Split('-')[3] == "2")
                    //{
                    //    Logger.WriteDebugLog("DateTime Updation started.");
                    //    currentDate = null;
                    //    tempDatenStatus = "0001TPMDTUBEGIN";
                    //    currentDate = GetuShort(tempDatenStatus);
                    //    int retry = 0;

                    //    do
                    //    {
                    //        #region StopService
                    //        if (ServiceStop.stop_service == 1)
                    //        {
                    //            try
                    //            {
                    //                if (master != null)
                    //                {
                    //                    master.Dispose();
                    //                    master = null;
                    //                }

                    //                Logger.WriteDebugLog("stopping the service. coming out of main while loop.");
                    //                break;
                    //            }
                    //            catch (Exception ex)
                    //            {
                    //                Logger.WriteErrorLog(ex.Message);
                    //                break;
                    //            }
                    //        }
                    //        #endregion

                    //        if (retry > 0)
                    //        {
                    //            Thread.Sleep(4000);
                    //        }
                    //        retry++;
                    //        if (retry == 4)
                    //        {
                    //            Logger.WriteDebugLog("software retried thrice to write ack 0001TPMDTUBEGIN");
                    //            break;
                    //        }
                    //        try
                    //        {
                    //            master.WriteMultipleRegisters(0, HoldingRegisterDateAndStatus, currentDate);
                    //            Logger.WriteDebugLog(string.Format("{0} sent from computer.", tempDatenStatus));
                    //            isException = false;
                    //            Thread.Sleep(1000);
                    //        }
                    //        catch (Exception ex)
                    //        {
                    //            isException = true;
                    //            if (master != null) master.Dispose();
                    //            Logger.WriteErrorLog(ex.ToString());
                    //            master = ConnectModBus();
                    //            continue;
                    //        }
                    //    } while (isException);


                    //    retry = 0;
                    //    do
                    //    {
                    //        #region StopService
                    //        if (ServiceStop.stop_service == 1)
                    //        {
                    //            try
                    //            {
                    //                if (master != null)
                    //                {
                    //                    master.Dispose();
                    //                    master = null;
                    //                }

                    //                Logger.WriteDebugLog("stopping the service. coming out of main while loop.");
                    //                break;
                    //            }
                    //            catch (Exception ex)
                    //            {
                    //                Logger.WriteErrorLog(ex.Message);
                    //                break;
                    //            }
                    //        }
                    //        #endregion

                    //        if (retry > 0)
                    //        {
                    //            Thread.Sleep(4000);
                    //        }
                    //        retry++;
                    //        if (retry == 4)
                    //        {
                    //            Logger.WriteDebugLog("software retried thrice to get ack 0001");
                    //            break;
                    //        }
                    //        try
                    //        {
                    //            output = master.ReadHoldingRegisters(DateAndStatusAckAddress, numberOfBytesToRead);
                    //            Thread.Sleep(1000);

                    //        }
                    //        catch (Exception ex)
                    //        {
                    //            isException = true;
                    //            if (master != null) master.Dispose();
                    //            Logger.WriteErrorLog(ex.ToString());
                    //            master = ConnectModBus();
                    //            continue;
                    //        }
                    //        outputString = GetString(output).Trim(char.MinValue);//.TrimEnd(new char[] { '0' });
                    //        Logger.WriteDebugLog(string.Format("Ack {0} recieved from machine.", outputString));

                    //    } while (outputString != "0001");

                    //    if (outputString == "0001")
                    //    {
                    //        tempDatenStatus = DateTime.Now.ToString("yyyyMMddHHmmss");
                    //        tempDatenStatus = "0002" + tempDatenStatus;
                    //        currentDate = GetuShort(tempDatenStatus);
                    //        retry = 0;

                    //        do
                    //        {
                    //            #region StopService
                    //            if (ServiceStop.stop_service == 1)
                    //            {
                    //                try
                    //                {
                    //                    if (master != null)
                    //                    {
                    //                        master.Dispose();
                    //                        master = null;
                    //                    }

                    //                    Logger.WriteDebugLog("stopping the service. coming out of main while loop.");
                    //                    break;
                    //                }
                    //                catch (Exception ex)
                    //                {
                    //                    Logger.WriteErrorLog(ex.Message);
                    //                    break;
                    //                }
                    //            }
                    //            #endregion
                    //            if (retry > 0)
                    //            {
                    //                Thread.Sleep(4000);
                    //            }
                    //            retry++;
                    //            if (retry == 4)
                    //            {
                    //                Logger.WriteDebugLog("software retried thrice to write " + tempDatenStatus);
                    //                break;
                    //            }
                    //            try
                    //            {
                    //                master.WriteMultipleRegisters(0, HoldingRegisterDateAndStatus, currentDate);
                    //                Logger.WriteDebugLog(string.Format("{0} sent from computer.", tempDatenStatus));
                    //                isException = false;
                    //                Thread.Sleep(1000);
                    //            }
                    //            catch (Exception ex)
                    //            {

                    //                isException = true;
                    //                if (master != null) master.Dispose();
                    //                Logger.WriteErrorLog(ex.ToString());
                    //                master = ConnectModBus();
                    //                continue;
                    //            }
                    //        } while (isException);

                    //        retry = 0;
                    //        do
                    //        {
                    //            #region StopService
                    //            if (ServiceStop.stop_service == 1)
                    //            {
                    //                try
                    //                {
                    //                    if (master != null)
                    //                    {
                    //                        master.Dispose();
                    //                        master = null;
                    //                    }

                    //                    Logger.WriteDebugLog("stopping the service. coming out of main while loop.");
                    //                    break;
                    //                }
                    //                catch (Exception ex)
                    //                {
                    //                    Logger.WriteErrorLog(ex.Message);
                    //                    break;
                    //                }
                    //            }
                    //            #endregion
                    //            if (retry > 0)
                    //            {
                    //                Thread.Sleep(4000);
                    //            }
                    //            retry++;
                    //            if (retry == 4)
                    //            {
                    //                Logger.WriteDebugLog("software retried thrice to get ack 0002");
                    //                break;
                    //            }
                    //            try
                    //            {
                    //                output = master.ReadHoldingRegisters(DateAndStatusAckAddress, numberOfBytesToRead);
                    //                Thread.Sleep(1000);
                    //            }
                    //            catch (Exception ex)
                    //            {
                    //                isException = true;
                    //                if (master != null) master.Dispose();
                    //                Logger.WriteErrorLog(ex.ToString());
                    //                master = ConnectModBus();
                    //                continue;
                    //            }
                    //            outputString = GetString(output).Trim(char.MinValue);//.TrimEnd(new char[] { '0' })
                    //            Logger.WriteDebugLog(string.Format("Ack {0} recieved from machine.", outputString));

                    //        } while (outputString != "0002");
                    //        if (outputString == "0002")
                    //        {
                    //            Thread.Sleep(1000);
                    //            tempDatenStatus = "0003TPMDTUEND";
                    //            currentDate = GetuShort(tempDatenStatus);
                    //            retry = 0;

                    //            do
                    //            {
                    //                #region StopService
                    //                if (ServiceStop.stop_service == 1)
                    //                {
                    //                    try
                    //                    {
                    //                        if (master != null)
                    //                        {
                    //                            master.Dispose();
                    //                            master = null;
                    //                        }

                    //                        Logger.WriteDebugLog("stopping the service. coming out of main while loop.");
                    //                        break;
                    //                    }
                    //                    catch (Exception ex)
                    //                    {
                    //                        Logger.WriteErrorLog(ex.Message);
                    //                        break;
                    //                    }
                    //                }
                    //                #endregion
                    //                if (retry > 0)
                    //                {
                    //                    Thread.Sleep(4000);
                    //                }
                    //                retry++;
                    //                if (retry == 4)
                    //                {
                    //                    Logger.WriteDebugLog("software retried thrice to write 0003TPMDTUEND");
                    //                    break;
                    //                }
                    //                try
                    //                {
                    //                    master.WriteMultipleRegisters(0, HoldingRegisterDateAndStatus, currentDate);
                    //                    Logger.WriteDebugLog(string.Format("{0} sent from computer.", tempDatenStatus));
                    //                    isException = false;
                    //                    Thread.Sleep(1000);
                    //                }
                    //                catch (Exception ex)
                    //                {
                    //                    isException = true;
                    //                    if (master != null) master.Dispose();
                    //                    Logger.WriteErrorLog(ex.ToString());
                    //                    master = ConnectModBus();
                    //                    continue;
                    //                }
                    //            } while (isException);
                    //            retry = 0;
                    //            do
                    //            {
                    //                #region StopService
                    //                if (ServiceStop.stop_service == 1)
                    //                {
                    //                    try
                    //                    {
                    //                        if (master != null)
                    //                        {
                    //                            master.Dispose();
                    //                            master = null;
                    //                        }

                    //                        Logger.WriteDebugLog("stopping the service. coming out of main while loop.");
                    //                        break;
                    //                    }
                    //                    catch (Exception ex)
                    //                    {
                    //                        Logger.WriteErrorLog(ex.Message);
                    //                        break;
                    //                    }
                    //                }
                    //                #endregion
                    //                if (retry > 0)
                    //                {
                    //                    Thread.Sleep(4000);
                    //                }
                    //                retry++;
                    //                if (retry == 4)
                    //                {
                    //                    Logger.WriteDebugLog("software retried thrice to get ack 0003");
                    //                    break;
                    //                }
                    //                try
                    //                {
                    //                    output = master.ReadHoldingRegisters(DateAndStatusAckAddress, numberOfBytesToRead);
                    //                    Thread.Sleep(1000);
                    //                }
                    //                catch (Exception ex)
                    //                {
                    //                    isException = true;
                    //                    if (master != null) master.Dispose();
                    //                    Logger.WriteErrorLog(ex.ToString());
                    //                    master = ConnectModBus();
                    //                    continue;
                    //                }
                    //                outputString = GetString(output).Trim(char.MinValue);//.TrimEnd(new char[] { '0' })
                    //                Logger.WriteDebugLog(string.Format("Ack {0} recieved from machine.", outputString));

                    //            } while (outputString != "0003");
                    //            if (outputString == "0003")
                    //            {
                    //                Logger.WriteDebugLog("Date Time Updation Successfull");
                    //            }
                    //            else
                    //            {
                    //                Logger.WriteDebugLog("Not able to get ack for TPMDTUEND");
                    //            }
                    //        }
                    //        else
                    //        {
                    //            Logger.WriteDebugLog("Not able to get ack for CurrentDate");
                    //        }
                    //    }
                    //    else
                    //    {
                    //        Logger.WriteDebugLog("Not able to get ack for TPMDTUBEGIN");
                    //    }
                    //}

                    //#endregion 
                    #endregion

                    // }
                }
            }
            #endregion
        }

        private void HandlingType_6_7String(ref ModbusIpMaster master, ref ushort prevAckNumber)
        {
            if (master == null)
            {
                Logger.WriteDebugLog("Getting Null for ModbusIpMaster object master. Exiting From HandlingType_6_7String()");
                return;
            }
            ushort[] output = null;
            string outputString = string.Empty;
            ushort ackNumber = 0;
            ushort[] currentDate = null;
            string tempDatenStatus = string.Empty;
            string messageID = string.Empty;
            #region Datacollection
            bool isException = false;
            int count = 0;
            while (isException || count == 0)
            {
                #region StopService
                if (ServiceStop.stop_service == 1)
                {
                    try
                    {
                        if (master != null)
                        {
                            master.Dispose();
                            master = null;
                        }

                        Logger.WriteDebugLog("stopping the service. coming out of main while loop.");
                        break;
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteErrorLog(ex.Message);
                        break;
                    }
                }
                #endregion
                count++;
                if (count > 4)
                {
                    break;
                }
                if (isException)
                {
                    master = ConnectModBus();
                }
                if (master == null)
                {
                    isException = true;
                    continue;
                }
                else
                {
                    isException = false;
                }
                try
                {
                    //Change to M3 buffer
                    output = master.ReadHoldingRegisters(HoldingRegister6_7TypeString, BytesToRead6_7Type);
                    //Logger.WriteDebugLog("Reading tpm trak strings successfully.");
                }
                catch (Exception ex)
                {
                    isException = true;
                    if (master != null) master.Dispose();
                    Logger.WriteErrorLog(ex.ToString());
                    continue;
                }
                outputString = GetString(output).Trim(char.MinValue);//.TrimEnd(new char[] { '0' });

                if (outputString != null && outputString.Length > 0)
                {
                    SaveStringToTPMFile(outputString);
                    Logger.WriteDebugLog(string.Format("String recieved : {0}", outputString));
                }
                if (outputString != null && outputString.Length >= 4)
                {
                    ackNumber = ushort.MinValue;
                    messageID = outputString.Substring(0, 4);
                    //send ack back
                    ushort.TryParse(messageID, out ackNumber);
                    //if (prevAckNumber != ackNumber)
                    // {
                    try
                    {
                        master.WriteSingleRegister(AckAddressFor6_7TypeString, ackNumber);
                        prevAckNumber = ackNumber;
                        Logger.WriteDebugLog(string.Format("Ack {0} for tpm trak string type sent. - {1}", messageID, outputString));
                        //Is delay required here ??
                    }
                    catch (Exception ex)
                    {
                        isException = true;
                        if (master != null) master.Dispose();
                        Logger.WriteErrorLog(ex.ToString());
                        continue;
                    }
                    //What will happen if ack not received by device (prevAckNumber != ackNumber)??   
                    //if (!outputString.Contains("START-6-"))
                    if (!outputString.Contains("START-6-") || (outputString.Contains("START-6-") && outputString.Split('-')[3] == "P600"))
                    {
                        ProcessFile(outputString, this._ipAddress, this._portNo.ToString(), MName);
                    }
                    #region  DateTime updation
                    if (outputString.Contains("START-7-") && outputString.Split('-')[3] == "2")
                    {
                        Logger.WriteDebugLog("DateTime Updation started.");
                        currentDate = null;
                        tempDatenStatus = "0001TPMDTUBEGIN";
                        currentDate = GetuShort(tempDatenStatus);
                        int retry = 0;

                        do
                        {
                            #region StopService
                            if (ServiceStop.stop_service == 1)
                            {
                                try
                                {
                                    if (master != null)
                                    {
                                        master.Dispose();
                                        master = null;
                                    }

                                    Logger.WriteDebugLog("stopping the service. coming out of main while loop.");
                                    break;
                                }
                                catch (Exception ex)
                                {
                                    Logger.WriteErrorLog(ex.Message);
                                    break;
                                }
                            }
                            #endregion

                            if (retry > 0)
                            {
                                Thread.Sleep(4000);
                            }
                            retry++;
                            if (retry == 4)
                            {
                                Logger.WriteDebugLog("software retried thrice to write ack 0001TPMDTUBEGIN");
                                break;
                            }
                            try
                            {
                                master.WriteMultipleRegisters(0, HoldingRegisterDateAndStatus, currentDate);
                                Logger.WriteDebugLog(string.Format("{0} sent from computer.", tempDatenStatus));
                                isException = false;
                                Thread.Sleep(1000);
                            }
                            catch (Exception ex)
                            {
                                isException = true;
                                if (master != null) master.Dispose();
                                Logger.WriteErrorLog(ex.ToString());
                                master = ConnectModBus();
                                continue;
                            }
                        } while (isException);


                        retry = 0;
                        do
                        {
                            #region StopService
                            if (ServiceStop.stop_service == 1)
                            {
                                try
                                {
                                    if (master != null)
                                    {
                                        master.Dispose();
                                        master = null;
                                    }

                                    Logger.WriteDebugLog("stopping the service. coming out of main while loop.");
                                    break;
                                }
                                catch (Exception ex)
                                {
                                    Logger.WriteErrorLog(ex.Message);
                                    break;
                                }
                            }
                            #endregion

                            if (retry > 0)
                            {
                                Thread.Sleep(4000);
                            }
                            retry++;
                            if (retry == 4)
                            {
                                Logger.WriteDebugLog("software retried thrice to get ack 0001");
                                break;
                            }
                            try
                            {
                                output = master.ReadHoldingRegisters(DateAndStatusAckAddress, numberOfBytesToRead);
                                Thread.Sleep(1000);

                            }
                            catch (Exception ex)
                            {
                                isException = true;
                                if (master != null) master.Dispose();
                                Logger.WriteErrorLog(ex.ToString());
                                master = ConnectModBus();
                                continue;
                            }
                            outputString = GetString(output).Trim(char.MinValue);//.TrimEnd(new char[] { '0' });
                            Logger.WriteDebugLog(string.Format("Ack {0} recieved from machine.", outputString));

                        } while (outputString != "0001");

                        if (outputString == "0001")
                        {
                            tempDatenStatus = DateTime.Now.ToString("yyyyMMddHHmmss");
                            tempDatenStatus = "0002" + tempDatenStatus;
                            currentDate = GetuShort(tempDatenStatus);
                            retry = 0;

                            do
                            {
                                #region StopService
                                if (ServiceStop.stop_service == 1)
                                {
                                    try
                                    {
                                        if (master != null)
                                        {
                                            master.Dispose();
                                            master = null;
                                        }

                                        Logger.WriteDebugLog("stopping the service. coming out of main while loop.");
                                        break;
                                    }
                                    catch (Exception ex)
                                    {
                                        Logger.WriteErrorLog(ex.Message);
                                        break;
                                    }
                                }
                                #endregion
                                if (retry > 0)
                                {
                                    Thread.Sleep(4000);
                                }
                                retry++;
                                if (retry == 4)
                                {
                                    Logger.WriteDebugLog("software retried thrice to write " + tempDatenStatus);
                                    break;
                                }
                                try
                                {
                                    master.WriteMultipleRegisters(0, HoldingRegisterDateAndStatus, currentDate);
                                    Logger.WriteDebugLog(string.Format("{0} sent from computer.", tempDatenStatus));
                                    isException = false;
                                    Thread.Sleep(1000);
                                }
                                catch (Exception ex)
                                {

                                    isException = true;
                                    if (master != null) master.Dispose();
                                    Logger.WriteErrorLog(ex.ToString());
                                    master = ConnectModBus();
                                    continue;
                                }
                            } while (isException);

                            retry = 0;
                            do
                            {
                                #region StopService
                                if (ServiceStop.stop_service == 1)
                                {
                                    try
                                    {
                                        if (master != null)
                                        {
                                            master.Dispose();
                                            master = null;
                                        }

                                        Logger.WriteDebugLog("stopping the service. coming out of main while loop.");
                                        break;
                                    }
                                    catch (Exception ex)
                                    {
                                        Logger.WriteErrorLog(ex.Message);
                                        break;
                                    }
                                }
                                #endregion
                                if (retry > 0)
                                {
                                    Thread.Sleep(4000);
                                }
                                retry++;
                                if (retry == 4)
                                {
                                    Logger.WriteDebugLog("software retried thrice to get ack 0002");
                                    break;
                                }
                                try
                                {
                                    output = master.ReadHoldingRegisters(DateAndStatusAckAddress, numberOfBytesToRead);
                                    Thread.Sleep(1000);
                                }
                                catch (Exception ex)
                                {
                                    isException = true;
                                    if (master != null) master.Dispose();
                                    Logger.WriteErrorLog(ex.ToString());
                                    master = ConnectModBus();
                                    continue;
                                }
                                outputString = GetString(output).Trim(char.MinValue);//.TrimEnd(new char[] { '0' })
                                Logger.WriteDebugLog(string.Format("Ack {0} recieved from machine.", outputString));

                            } while (outputString != "0002");
                            if (outputString == "0002")
                            {
                                Thread.Sleep(1000);
                                tempDatenStatus = "0003TPMDTUEND";
                                currentDate = GetuShort(tempDatenStatus);
                                retry = 0;

                                do
                                {
                                    #region StopService
                                    if (ServiceStop.stop_service == 1)
                                    {
                                        try
                                        {
                                            if (master != null)
                                            {
                                                master.Dispose();
                                                master = null;
                                            }

                                            Logger.WriteDebugLog("stopping the service. coming out of main while loop.");
                                            break;
                                        }
                                        catch (Exception ex)
                                        {
                                            Logger.WriteErrorLog(ex.Message);
                                            break;
                                        }
                                    }
                                    #endregion
                                    if (retry > 0)
                                    {
                                        Thread.Sleep(4000);
                                    }
                                    retry++;
                                    if (retry == 4)
                                    {
                                        Logger.WriteDebugLog("software retried thrice to write 0003TPMDTUEND");
                                        break;
                                    }
                                    try
                                    {
                                        master.WriteMultipleRegisters(0, HoldingRegisterDateAndStatus, currentDate);
                                        Logger.WriteDebugLog(string.Format("{0} sent from computer.", tempDatenStatus));
                                        isException = false;
                                        Thread.Sleep(1000);
                                    }
                                    catch (Exception ex)
                                    {
                                        isException = true;
                                        if (master != null) master.Dispose();
                                        Logger.WriteErrorLog(ex.ToString());
                                        master = ConnectModBus();
                                        continue;
                                    }
                                } while (isException);
                                retry = 0;
                                do
                                {
                                    #region StopService
                                    if (ServiceStop.stop_service == 1)
                                    {
                                        try
                                        {
                                            if (master != null)
                                            {
                                                master.Dispose();
                                                master = null;
                                            }

                                            Logger.WriteDebugLog("stopping the service. coming out of main while loop.");
                                            break;
                                        }
                                        catch (Exception ex)
                                        {
                                            Logger.WriteErrorLog(ex.Message);
                                            break;
                                        }
                                    }
                                    #endregion
                                    if (retry > 0)
                                    {
                                        Thread.Sleep(4000);
                                    }
                                    retry++;
                                    if (retry == 4)
                                    {
                                        Logger.WriteDebugLog("software retried thrice to get ack 0003");
                                        break;
                                    }
                                    try
                                    {
                                        output = master.ReadHoldingRegisters(DateAndStatusAckAddress, numberOfBytesToRead);
                                        Thread.Sleep(1000);
                                    }
                                    catch (Exception ex)
                                    {
                                        isException = true;
                                        if (master != null) master.Dispose();
                                        Logger.WriteErrorLog(ex.ToString());
                                        master = ConnectModBus();
                                        continue;
                                    }
                                    outputString = GetString(output).Trim(char.MinValue);//.TrimEnd(new char[] { '0' })
                                    Logger.WriteDebugLog(string.Format("Ack {0} recieved from machine.", outputString));

                                } while (outputString != "0003");
                                if (outputString == "0003")
                                {
                                    Logger.WriteDebugLog("Date Time Updation Successfull");
                                }
                                else
                                {
                                    Logger.WriteDebugLog("Not able to get ack for TPMDTUEND");
                                }
                            }
                            else
                            {
                                Logger.WriteDebugLog("Not able to get ack for CurrentDate");
                            }
                        }
                        else
                        {
                            Logger.WriteDebugLog("Not able to get ack for TPMDTUBEGIN");
                        }
                    }

                    #endregion

                    // }
                }
            }
            #endregion
        }

        private void InvokeDailyCheckListActivity(ref ModbusIpMaster master)
        {
            if (master == null)
            {
                Logger.WriteDebugLog("Getting Null for ModbusIpMaster object master. Exiting From InvokeDailyCheckListActivity()");
                return;
            }
            ushort[] output = null;

            #region StopService
            if (ServiceStop.stop_service == 1)
            {
                try
                {
                    if (master != null)
                    {
                        master.Dispose();
                        master = null;
                    }

                    Logger.WriteDebugLog("stopping the service. coming out of main while loop.");
                    return;
                }
                catch (Exception ex)
                {
                    Logger.WriteErrorLog(ex.Message);
                    return;
                }
            }
            #endregion

            try
            {
                #region Daily CheckLists

                ushort dateregCnt = 2, timeregCnt = 4;
                ushort dailyChkListflg = HoldingFlagsRegisterDailyCheckList;
                for (int i = 1; i <= dailyChkLsts.Count; i++)
                {
                    output = master.ReadHoldingRegisters(dailyChkListflg, 1);
                    if (output[0] == 1)
                    {
                        string date = string.Empty, time = string.Empty;
                        output = master.ReadHoldingRegisters((ushort)(HoldingRegisterDailyCheckList + dateregCnt), 2);
                        date = ModbusUtility.GetUInt32(output[1], output[0]).ToString();
                        output = master.ReadHoldingRegisters((ushort)(HoldingRegisterDailyCheckList + timeregCnt), 2);
                        time = ModbusUtility.GetUInt32(output[1], output[0]).ToString("000000");
                        Logger.WriteDebugLog(string.Format("Daily Maintenance CheckLists => Reading Date : {0} (In Register address : {1}) | Time : {2} (In Register address : {3}) | for Activity : {4} ", date, (HoldingRegisterDailyCheckList + dateregCnt), time, (HoldingRegisterDailyCheckList + timeregCnt), dailyChkLsts[i - 1].activity));
                        DatabaseAccess.insertDailyCheckList(this._machineId, DateTime.ParseExact(string.Format("{0}{1}", date, time), "yyyyMMddHHmmss", null), dailyChkLsts[i - 1].SlNo);

                        master.WriteSingleRegister(dailyChkListflg, 2);
                    }
                    dailyChkListflg++;
                    dateregCnt += 4;
                    timeregCnt += 4;
                }

                #endregion
            }
            catch (Exception ex)
            {
                Logger.WriteErrorLog(ex.ToString());
            }
        }

        private void InvokePreventiveMaintenanceActivity(ref ModbusIpMaster master)
        {
            if (master == null)
            {
                Logger.WriteDebugLog("Getting Null for ModbusIpMaster object master. Exiting From InvokePreventiveMaintenanceActivity()");
                return;
            }
            ushort[] output = null;

            #region StopService
            if (ServiceStop.stop_service == 1)
            {
                try
                {
                    if (master != null)
                    {
                        master.Dispose();
                        master = null;
                    }

                    Logger.WriteDebugLog("stopping the service. coming out of main while loop.");
                    return;
                }
                catch (Exception ex)
                {
                    Logger.WriteErrorLog(ex.Message);
                    return;
                }
            }
            #endregion

            try
            {
                #region 70% or 100%flag
                output = master.ReadHoldingRegisters(HoldingRegisterPMCheckList70Perc, 1);
                if (output[0] == 1)
                {
                    //START - 56 - MACHINE ID - MAIN CATEGORY CODE - SUB CATEGORY CODE - SELECTION CODE - TARGET - ACTUAL -PERCENT COMPLETED -DATE - TIME - END                                 
                    var machineID = (int)(master.ReadHoldingRegisters(HoldingRegisterStr56PMMachineID, 1)[0]);
                    var selectionCode = (int)(master.ReadHoldingRegisters(HoldingRegisterStr56PMSelectionCode, 1)[0]);
                    output = master.ReadHoldingRegisters(HoldingRegisterStr56PMTarget, 2);
                    var target = ModbusUtility.GetUInt32(output[1], output[0]).ToString();
                    output = master.ReadHoldingRegisters(HoldingRegisterStr56PMActual, 2);
                    var actual = ModbusUtility.GetUInt32(output[1], output[0]).ToString();
                    output = master.ReadHoldingRegisters(HoldingRegisterStr56PMPercentage, 1);
                    var persentCompleted = Convert.ToInt16(output[0]);
                    output = master.ReadHoldingRegisters(HoldingRegisterStr56PMCurDate, 2);
                    var date = ModbusUtility.GetUInt32(output[1], output[0]).ToString();
                    output = master.ReadHoldingRegisters(HoldingRegisterStr56PMCurTime, 2);
                    var time = ModbusUtility.GetUInt32(output[1], output[0]).ToString("000000");
                    string str56 = string.Format("START-56-{0}-0-0-{1}-{2}-{3}-{4}-{5}-{6}-END", machineID, selectionCode, target, actual, persentCompleted, date, time);
                    Logger.WriteDebugLog(string.Format("Start-56 string recieved at {0} | Actual string : {1}", DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss tt"), str56));
                    if (!string.IsNullOrEmpty(str56))
                    {
                        SaveStringToTPMFile(str56);
                        ProcessFile(str56, this._ipAddress, this._portNo.ToString(), this._machineId);
                    }
                    master.WriteSingleRegister(HoldingRegisterPMCheckList70Perc, 2);
                }
                #endregion

                #region OK and NOT OK | Str57 and Str58
                output = master.ReadHoldingRegisters(HoldingRegisterPMCheckListOKNOTOK, 1);
                while (output[0] == 1)
                {
                    var machineID = (int)(master.ReadHoldingRegisters(HoldingRegisterStr57PMMachineID, 1)[0]);
                    var operatorID = (int)(master.ReadHoldingRegisters(HoldingRegisterStr57PMOprID, 1)[0]);
                    var categoryId = (int)(master.ReadHoldingRegisters(HoldingRegisterStr57PMCatID, 1)[0]);
                    var subCategoryID = (int)(master.ReadHoldingRegisters(HoldingRegisterStr57PMSubCatID, 1)[0]);
                    var selectionCode = (int)(master.ReadHoldingRegisters(HoldingRegisterStr57PMSelectionCode, 1)[0]);
                    var notOkReason = (int)(master.ReadHoldingRegisters(HoldingRegisterStr57PMNOKreason, 1)[0]);
                    int dataType = notOkReason > 0 ? 58 : 57;
                    output = master.ReadHoldingRegisters(HoldingRegisterStr57PMTarget, 2);
                    var target = ModbusUtility.GetUInt32(output[1], output[0]).ToString();
                    output = master.ReadHoldingRegisters(HoldingRegisterStr57PMActual, 2);
                    var actual = ModbusUtility.GetUInt32(output[1], output[0]).ToString();
                    output = master.ReadHoldingRegisters(HoldingRegisterStr57PMCurDate, 2);
                    var date = ModbusUtility.GetUInt32(output[1], output[0]).ToString();
                    output = master.ReadHoldingRegisters(HoldingRegisterStr57PMCurTime, 2);
                    var time = ModbusUtility.GetUInt32(output[1], output[0]).ToString("000000");
                    #region string formats
                    /*/*  PM OK STRING FORMAT ;
                       START - 57 - MACHINE ID 0- OPERATORID -1 MAIN CATEGORY CODE 2- SUB CATEGORY CODE 3- SELECTION CODE4 - TARGET 5- ACTUAL 6- DATE7 - TIME8 - END
                       PM NOT OK STRING FORMAT ;
                       START - 58 - MACHINE ID - OPERATORID - MAIN CATEGORY CODE - SUB CATEGORY CODE - SELECTION CODE - TARGET - ACTUAL - REASON - DATE - TIME - END */
                    #endregion
                    string strData57_58 = string.Empty;
                    if (dataType == 57)
                    {
                        strData57_58 = string.Format("START-57-{0}-{1}-{2}-{3}-{4}-{5}-{6}-{7}-{8}-END", machineID, operatorID, categoryId, subCategoryID, selectionCode, target, actual, date, time);
                        Logger.WriteDebugLog(string.Format("Start-57 string recieved at {0} | Actual string : {1}", DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss tt"), strData57_58));
                    }
                    else if (dataType == 58)
                    {
                        strData57_58 = string.Format("START-58-{0}-{1}-{2}-{3}-{4}-{5}-{6}-{7}-{8}-{9}-END", machineID, operatorID, categoryId, subCategoryID, selectionCode, target, actual, notOkReason, date, time);
                        Logger.WriteDebugLog(string.Format("Start-58 string recieved at {0} | Actual string : {1}", DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss tt"), strData57_58));
                    }



                    if (!string.IsNullOrEmpty(strData57_58))
                    {
                        SaveStringToTPMFile(strData57_58);
                        ProcessFile(strData57_58, this._ipAddress, this._portNo.ToString(), this._machineId);
                    }
                    master.WriteSingleRegister(HoldingRegisterPMCheckListOKNOTOK, 2);
                    Thread.Sleep(1000);
                    output = master.ReadHoldingRegisters(HoldingRegisterPMCheckListOKNOTOK, 1);
                }
                #endregion

                #region ALL PM Done
                //START-59-MC-DATE-TIME-END
                output = master.ReadHoldingRegisters(HoldingRegisterPMCheckListAllDone, 1);
                if (output[0] == 1)
                {
                    var machineID = (int)(master.ReadHoldingRegisters(HoldingRegisterStr59PMMachineID, 1)[0]);
                    var operatorID = (int)(master.ReadHoldingRegisters(HoldingRegisterStr59PMOprID, 1)[0]);
                    output = master.ReadHoldingRegisters(HoldingRegisterStr59PMCurDate, 2);
                    var date = ModbusUtility.GetUInt32(output[1], output[0]).ToString();
                    output = master.ReadHoldingRegisters(HoldingRegisterStr59PMCurTime, 2);
                    var time = ModbusUtility.GetUInt32(output[1], output[0]).ToString("000000");
                    string str59 = string.Format("START-59-{0}-{1}-{2}-{3}-END", machineID, operatorID, date, time);
                    Logger.WriteDebugLog(string.Format("Start-59 string recieved at {0} | string : {1}", DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss tt"), str59));
                    if (!string.IsNullOrEmpty(str59))
                    {
                        SaveStringToTPMFile(str59);
                        ProcessFile(str59, this._ipAddress, this._portNo.ToString(), this._machineId);
                    }
                    master.WriteSingleRegister(HoldingRegisterPMCheckListAllDone, 2);
                }
                #endregion
            }
            catch (Exception ex)
            {
                Logger.WriteErrorLog(ex.ToString());
            }
        }

        // Done by Prince
        #region Shanti Iron - Process Parameter
        private void WriteParametersToPLC(ref ModbusIpMaster master)
        {
            if (master == null)
            {
                Logger.WriteDebugLog("Getting Null for ModbusIpMaster object master. Exiting From WriteParametersToPLC()");
                return;
            }
            try
            {
                ushort[] FlagToWritePLC = master.ReadHoldingRegisters((ushort)1606, (ushort)1);
                if (FlagToWritePLC[0] == 1)
                {
                    Logger.WriteDebugLog("Started writing Process Parameters to PLC");
                    ushort[] output = master.ReadHoldingRegisters((ushort)1607, (ushort)10);
                    string CompID = GetStringParameter(output).Trim(char.MinValue);
                    output = master.ReadHoldingRegisters((ushort)1617, (ushort)2);
                    string OpnID = GetStringParameter(output).Trim(char.MinValue);

                    processParameter_Strings = DatabaseAccess.GetProcessParameterForPLC(this._machineId, CompID, OpnID);
                    master.WriteSingleRegister((ushort)1581, (ushort)processParameter_Strings.Count);
                    if (processParameter_Strings != null && processParameter_Strings.Count > 0)
                    {
                        foreach (string stringFromDB in processParameter_Strings)
                        {
                            processStrToWrite = GetuShortProcess(stringFromDB);
                            master.WriteMultipleRegisters(HoldingRegisterWriteParameter, processStrToWrite);
                            HoldingRegisterWriteParameter = (ushort)(HoldingRegisterWriteParameter + 15);
                            Logger.WriteDebugLog("Writing " + stringFromDB + " to PLC");
                        }

                        if (processParameter_Strings.Count != 15)
                        {
                            int i = processParameter_Strings.Count;
                            while (i < 15)
                            {
                                processStrToWrite = GetuShort(" ");
                                master.WriteMultipleRegisters(HoldingRegisterWriteParameter, processStrToWrite);
                                HoldingRegisterWriteParameter = (ushort)(HoldingRegisterWriteParameter + 15);
                                i++;
                            }
                        }
                        HoldingRegisterWriteParameter = (ushort)1100;
                        Logger.WriteDebugLog("Process Parameters Has been sent to PLC");
                    }
                    master.WriteSingleRegister((ushort)1606, (ushort)0);
                }
            }
            catch (Exception ex)
            {
                Logger.WriteErrorLog("Exception In Writing Process: " + ex.Message);
            }

        }
        private void HandingProcessParameter_String(ref ModbusIpMaster master)
        {
            if (master == null)
            {
                Logger.WriteDebugLog("Getting Null for ModbusIpMaster object master. Exiting From HandingProcessParameter_String()");
                return;
            }
            ushort[] output;
            ushort[] stringToInsertToDB;
            ushort[] RealToInsertToDB;
            string CompID = string.Empty;
            try
            {
                ushort[] NoOfParamterToRead = master.ReadHoldingRegisters((ushort)1582, (ushort)1);
                ushort[] readFlag = master.ReadHoldingRegisters((ushort)1580, (ushort)1);

                #region Read Flag-Trial
                //if(readFlag[0] ==1 || readFlag[0]==3 || readFlag[0] == 5)
                //{
                //    if (NoOfParamterToRead[0] - 5 > 0)
                //    {
                //        for (int i = 0; i < 5; i++)
                //        {
                //            stringToInsertToDB = master.ReadHoldingRegisters(HoldingRegisterReadParameter, (ushort)15);
                //            RealToInsertToDB = master.ReadHoldingRegisters(HoldingRegisterReadParamValue, (ushort)4);

                //            string Dimension = GetStringParameter(stringToInsertToDB).Trim(char.MinValue);
                //            float Value = GetFloat(RealToInsertToDB);

                //            output = master.ReadHoldingRegisters((ushort)1584, (ushort)2);
                //            string machineId = GetStringParameter(output).Trim(char.MinValue);
                //            output = master.ReadHoldingRegisters((ushort)1586, (ushort)10);
                //            string Comp = GetStringParameter(output).Trim(char.MinValue);
                //            output = master.ReadHoldingRegisters((ushort)1596, (ushort)2);
                //            string Opn = GetStringParameter(output).Trim(char.MinValue);
                //            output = master.ReadHoldingRegisters((ushort)1598, (ushort)2);
                //            string Opr = GetStringParameter(output).Trim(char.MinValue);
                //            DateTime TimeStamp = DateTime.Now;
                //            DateTime BatchTS = DateTime.Now;

                //            DatabaseAccess.InsertStringRealToSPCAutoData(machineId, Comp, Opn, Opr, Dimension, Value, TimeStamp, BatchTS);
                //            HoldingRegisterReadParamValue = (ushort)(HoldingRegisterReadParamValue + 2);
                //            HoldingRegisterReadParameter = (ushort)(HoldingRegisterReadParameter + 15);
                //        }
                //        master.WriteSingleRegister((ushort)1580, readFlag[0] += (ushort)1);
                //        NoOfParamterToRead[0] -= (ushort)5;
                //    }
                //    else
                //    {
                //        for (int i = 0; i < NoOfParamterToRead[0]; i++)
                //        {
                //            stringToInsertToDB = master.ReadHoldingRegisters(HoldingRegisterReadParameter, (ushort)15);
                //            RealToInsertToDB = master.ReadHoldingRegisters(HoldingRegisterReadParamValue, (ushort)4);

                //            string Dimension = GetStringParameter(stringToInsertToDB).Trim(char.MinValue);
                //            float Value = GetFloat(RealToInsertToDB);

                //            output = master.ReadHoldingRegisters((ushort)1584, (ushort)2);
                //            string machineId = GetStringParameter(output).Trim(char.MinValue);
                //            output = master.ReadHoldingRegisters((ushort)1586, (ushort)10);
                //            string Comp = GetStringParameter(output).Trim(char.MinValue);
                //            output = master.ReadHoldingRegisters((ushort)1596, (ushort)2);
                //            string Opn = GetStringParameter(output).Trim(char.MinValue);
                //            output = master.ReadHoldingRegisters((ushort)1598, (ushort)2);
                //            string Opr = GetStringParameter(output).Trim(char.MinValue);
                //            DateTime TimeStamp = DateTime.Now;
                //            DateTime BatchTS = DateTime.Now;

                //            DatabaseAccess.InsertStringRealToSPCAutoData(machineId, Comp, Opn, Opr, Dimension, Value, TimeStamp, BatchTS);
                //            HoldingRegisterReadParamValue = (ushort)(HoldingRegisterReadParamValue + 2);
                //            HoldingRegisterReadParameter = (ushort)(HoldingRegisterReadParameter + 15);
                //        }
                //        NoOfParamterToRead[0] = (ushort)0;
                //        //master.WriteSingleRegister((ushort)1580, readFlag[0] += (ushort)1);
                //    }
                //}
                #endregion
                if (readFlag[0] == 1 || readFlag[0] == 3 || readFlag[0] == 5)
                {
                    if (readFlag[0] == 1)
                    {
                        HoldingRegisterReadParameter = (ushort)1325;
                        HoldingRegisterReadParamValue = (ushort)1550;
                    }

                    for (int i = 0; i < 5; i++)
                    {
                        stringToInsertToDB = master.ReadHoldingRegisters(HoldingRegisterReadParameter, (ushort)15);
                        RealToInsertToDB = master.ReadHoldingRegisters(HoldingRegisterReadParamValue, (ushort)2);

                        string Dimension = GetStringParameter(stringToInsertToDB).Trim(char.MinValue);
                        if (string.IsNullOrEmpty(Dimension))
                            continue;
                        float Value = GetFloat(RealToInsertToDB);

                        output = master.ReadHoldingRegisters((ushort)1584, (ushort)2);
                        string machineId = GetStringParameter(output).Trim(char.MinValue);
                        output = master.ReadHoldingRegisters((ushort)1586, (ushort)10);
                        string Comp = GetStringParameter(output).Trim(char.MinValue);
                        output = master.ReadHoldingRegisters((ushort)1596, (ushort)2);
                        string Opn = GetStringParameter(output).Trim(char.MinValue);
                        output = master.ReadHoldingRegisters((ushort)1598, (ushort)2);
                        string Opr = GetStringParameter(output).Trim(char.MinValue);
                        output = master.ReadHoldingRegisters((ushort)1600, (ushort)4);
                        string slNo = GetStringParameter(output).Trim(char.MinValue);
                        DateTime TimeStamp = DateTime.Now;
                        output = master.ReadHoldingRegisters((ushort)1620, (ushort)2);
                        string date = ModbusUtility.GetUInt32(output[1], output[0]).ToString("00000000");//cycle start date
                        output = master.ReadHoldingRegisters((ushort)1622, (ushort)2);
                        string time = ModbusUtility.GetUInt32(output[1], output[0]).ToString("000000");//cycle start time
                        DateTime BatchTS = DateTime.ParseExact(string.Format("{0}{1}", date, time), "yyyyMMddHHmmss", null);

                        Logger.WriteDebugLog(string.Format("Data Received : SerialNo-{0} Component-{1} Operation-{2} Operator-{3} Dimension-{4} Value-{5}",slNo,Comp,Opn,Opr,Dimension,Value));
                        DatabaseAccess.InsertParameterDataToSPCAutoData(slNo, machineId, Comp, Opn, Opr, Dimension, Value, TimeStamp, BatchTS);
                        HoldingRegisterReadParamValue = (ushort)(HoldingRegisterReadParamValue + 2);
                        HoldingRegisterReadParameter = (ushort)(HoldingRegisterReadParameter + 15);
                    }
                    master.WriteSingleRegister((ushort)1580, readFlag[0] += (ushort)1);

                }
            }
            catch(Exception ex)
            {
                Logger.WriteErrorLog(ex.Message);
            }
            

        }
        #endregion


        private static string GetString(ushort[] data)
        {
            StringBuilder str = new StringBuilder();
            foreach (ushort i in data)
            {
                byte[] byteArray = BitConverter.GetBytes(i);
                byte temp = byteArray[0];
                byteArray[0] = byteArray[1];
                byteArray[1] = temp;
                str.Append(Encoding.UTF8.GetString(byteArray));
            }
            return str.ToString().Trim();
        }

        private static string GetStringParameter(ushort[] data)
        {
            StringBuilder str = new StringBuilder();
            foreach (ushort i in data)
            {
                byte[] byteArray = BitConverter.GetBytes(i);
                str.Append(Encoding.UTF8.GetString(byteArray));
            }
            return str.ToString();
        }
        //private float GetFloat(ushort[] input)
        //{
        //    byte[] b = { (byte)(input[0] & 0xff), (byte)(input[0] >> 8), (byte)(input[1] & 0xff), (byte)(input[1] >> 8) };
        //    return System.BitConverter.ToSingle(b, 0);
        //}

        private float GetFloat(ushort[] input)
        {
            byte[] b = { (byte)(input[0] & 0xff), (byte)(input[0] >> 8), (byte)(input[1] & 0xff), (byte)(input[1] >> 8) };
            return System.BitConverter.ToSingle(b, 0);
        }

        private static ushort[] GetuShort(string str)
        {
            ushort[] uTempShort = null;
            try
            {
                char[] tempChararray = str.ToCharArray();
                if (tempChararray.Length % 2 == 0) uTempShort = new ushort[tempChararray.Length / 2];
                else uTempShort = new ushort[tempChararray.Length / 2 + 1];
                int j = 0;
                for (int i = 0; i < tempChararray.Length; i = i + 2)
                {
                    byte[] byteArray1 = new byte[2];
                    byte[] byteArray2 = new byte[2];
                    byteArray1 = BitConverter.GetBytes(tempChararray[i]);
                    if (i + 1 < tempChararray.Length) byteArray2 = BitConverter.GetBytes(tempChararray[i + 1]);
                    ushort us = (ushort)(byteArray2[0] + byteArray1[0] * 256);
                    uTempShort[j] = us;
                    j++;
                    // ADD THIS USHORT TO ARRAY
                }
            }
            catch (Exception ex)
            {
                Logger.WriteErrorLog(ex.ToString());
            }
            return uTempShort;

        }

        private static ushort[] GetuShortProcess(string str)
        {
            ushort[] uTempShort = null;
            try
            {
                char[] tempChararray = str.ToCharArray();
                if (tempChararray.Length % 2 == 0) uTempShort = new ushort[tempChararray.Length / 2];
                else uTempShort = new ushort[tempChararray.Length / 2 + 1];
                int j = 0;
                for (int i = 0; i < tempChararray.Length; i = i + 2)
                {
                    byte[] byteArray1 = new byte[2];
                    byte[] byteArray2 = new byte[2];
                    byteArray1 = BitConverter.GetBytes(tempChararray[i]);
                    if (i + 1 < tempChararray.Length) byteArray2 = BitConverter.GetBytes(tempChararray[i + 1]);
                    ushort us = (ushort)(byteArray1[0] + byteArray2[0] * 256);
                    uTempShort[j] = us;
                    j++;
                    // ADD THIS USHORT TO ARRAY
                }
            }
            catch (Exception ex)
            {
                Logger.WriteErrorLog(ex.ToString());
            }
            return uTempShort;

        }

        private int CheckMessageType(byte[] smallbuff)
        {
            int messageType = 0;
            messageType = smallbuff[0];
            return messageType;
        }

        private int Get_header_length(byte[] smallbuff)
        {
            byte[] len = new byte[2];
            len[0] = smallbuff[2];
            len[1] = smallbuff[3];
            int msg_len_specified = 0;
            msg_len_specified = get_decimal(len[0], 0);
            msg_len_specified += get_decimal(len[1], 1);
            return msg_len_specified;
        }

        private void MakeFileId()
        {
            try
            {
                Logger.WriteDebugLog("comes to make id file function");
                string apath = "";
                string max_id_value = "65535";
                appPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                apath = appPath;
                if (!Directory.Exists(apath + "\\LAST_ID\\"))
                {
                    Console.WriteLine("last id dir is created");
                    Logger.WriteDebugLog("last id dir is created");
                    Directory.CreateDirectory(apath + "\\LAST_ID\\");
                }

                StreamWriter writer1 = new StreamWriter(apath + "\\LAST_ID\\" + this._ipAddress.ToString() + ".txt", true);
                writer1.Close();
                writer1.Dispose();

                //Thread.Sleep(1000);
                // Thread.Sleep(500); //sande

                Thread.Sleep(50);

                StreamReader reader = new StreamReader(apath + "\\LAST_ID\\" + this._ipAddress.ToString() + ".txt");
                string res;
                res = reader.ReadLine();

                Logger.WriteDebugLog("res value is" + res);
                reader.Close();
                reader.Dispose();


                if (res == null)
                {
                    StreamWriter writer2 = new StreamWriter(apath + "\\LAST_ID\\" + this._ipAddress.ToString() + ".txt", false);
                    Console.WriteLine("id file is null");
                    Logger.WriteDebugLog("id ffile is null");
                    //   Console.ReadLine();
                    writer2.WriteLine("0");
                    writer2.Flush();
                    writer2.Close();
                    writer2.Dispose();

                }
                else if (res == max_id_value)
                {
                    StreamWriter writer2 = new StreamWriter(apath + "\\LAST_ID\\" + this._ipAddress.ToString() + ".txt", false);
                    Console.WriteLine("max id");
                    Logger.WriteDebugLog("max id");
                    writer2.WriteLine("0");
                    writer2.Flush();
                    writer2.Close();
                    writer2.Dispose();

                }
                else
                {
                    StreamWriter writer3 = new StreamWriter(apath + "\\LAST_ID\\" + this._ipAddress.ToString() + ".txt", true);
                    writer3.Close();
                    writer3.Dispose();
                }
            }
            catch (Exception ex)
            {
                Logger.WriteErrorLog(ex.Message);
            }
        }

        private void SetMsgId(byte[] smallbuff)//gets the current id from buffer rxd//
        {
            try
            {
                byte[] id = new byte[2];

                id[0] = smallbuff[6];
                id[1] = smallbuff[7];

                int res = get_decimal(id[0], 0);
                res += get_decimal(id[1], 1);

                cur_id = res.ToString();
                Logger.WriteDebugLog("currnt Message ID : " + cur_id);
            }
            catch (Exception ex)
            {
                Logger.WriteErrorLog(ex.Message);
            }
        }

        private static int get_decimal(byte byt, int type)
        {
            int b = (int)byt;
            int p = 0;
            if (type == 0)
                p = 8;
            if (type == 1)
                p = 0;

            int sum = 0;
            int r = 0;
            do
            {
                r = b % 2;
                sum += r * (int)Math.Pow((double)2.0, (double)p);
                p++;
                b /= 2;

            } while (b > 0);
            return sum;

        }

        private string SaveDataToFile(string data, string ip, string port, string MName)
        {
            string pendingDataToProcess = string.Empty;
            try
            {
                pendingDataToProcess = ProcessFile(data, ip, port, MName);
                Logger.WriteDebugLog("Unprocessed string(pendingDataToProcess) is: " + pendingDataToProcess);
            }
            catch (Exception ex)
            {
                Logger.WriteErrorLog("Error in SaveDataToFile() - " + ex.Message);
            }
            return pendingDataToProcess;
        }

        private void SaveStringToTPMFile(string str)
        {
            string progTime = String.Format("_{0:yyyyMMdd}", DateTime.Now);

            StreamWriter writer = default(StreamWriter);
            try
            {
                writer = new StreamWriter(appPath + "\\TPMFiles\\F-" + Thread.CurrentThread.Name + progTime + ".txt", true);
                writer.WriteLine(str);
                writer.Flush();
            }
            catch (Exception ex)
            {
                Logger.WriteErrorLog(ex.Message);
            }
            finally
            {
                if (writer != null)
                {
                    writer.Close();
                    writer.Dispose();
                }
            }
        }

        private void SaveLastStringID()
        {
            string apath = appPath;
            string max_id_value = "65535";
            if (!Directory.Exists(apath + "\\LAST_ID\\"))
            {
                Directory.CreateDirectory(apath + "\\LAST_ID\\");
            }
            StreamWriter writer = new StreamWriter(apath + "\\LAST_ID\\" + this._ipAddress.ToString() + ".txt", false);

            if (cur_id == max_id_value)
            {
                writer.WriteLine("0");
                writer.Flush();
            }
            else
            {
                if (cur_id == null)
                    Logger.WriteDebugLog("current id is made as null now");
                writer.WriteLine(cur_id.ToString());
                writer.Flush();
            }
            //writer.Flush();
            writer.Close();
            writer.Dispose();
            Logger.WriteDebugLog("current id is savd to the file");
            // Thread.Sleep(500); commented sande

            ////string Apath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            ////string ProgTime1 = String.Format("{0:ddMMMyy}", DateTime.Now);
            ////if(!Directory.Exists(Apath+"\\last_ID_files\\"))
            ////{
            ////    Directory.CreateDirectory(Apath+"\\last_ID_files\\");//with the execution module//
            ////}

            ////StreamWriter writer = new StreamWriter(Apath+"\\last_ID_files\\" + MName.Replace("\\", "") + ProgTime1 + "ID.txt",false);

            ////writer.Write(cur_id);
            ////writer.Flush();
            ////writer.Close();
            ////writer.Dispose();



        }

        private void SendAck(byte byte6, byte byte7)
        {
            Logger.WriteDebugLog("Sending ACK for message ID : " + cur_id);

            byte[] ack = new byte[8];
            Array.Clear(ack, 0, 8);
            ack[6] = byte6;
            ack[7] = byte7;
            ack[0] = byte.Parse("19");
            ack[1] = byte.Parse("1");

            try
            {
                int n = socket.Send(ack);
                if (n <= 0)
                {
                    Logger.WriteErrorLog("From SendAck() returns : " + n.ToString());
                }
            }
            catch (ObjectDisposedException ex)
            {
                Logger.WriteErrorLog("From SendAck() Object disposed exception" + ex.Message);
                //throw;
            }
            catch (Exception ex)
            {
                Logger.WriteErrorLog("From SendAck() " + ex.Message);
                //throw;
            }

            Thread.Sleep(100);
            Logger.WriteDebugLog("ACK sent for message ID : " + cur_id);
        }

        private string GetLastSavedId()
        {
            //////string Apath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            ////string ProgTime1 = String.Format("{0:ddMMMyy}", DateTime.Now);
            ////StreamReader reader = new StreamReader(Apath + "\\last_ID_files\\" + MName.Replace("\\", "") + ProgTime1 + "ID.txt", false);
            ////string saved_id = reader.ReadLine();
            ////reader.Close();
            ////reader.Dispose();
            //WriteInToFile("get savd id starts");
            ////return saved_id;
            string apath = "";
            apath = appPath;

            StreamReader reader = new StreamReader(apath + "\\LAST_ID\\" + this._ipAddress.ToString() + ".txt");
            //reader.re
            string id = reader.ReadLine();
            reader.Close();
            reader.Dispose();
            Logger.WriteDebugLog(" saved id: returnd is :" + id + " " + DateTime.Now.ToString());
            Console.WriteLine("savd id is :" + id);
            // WriteInToFile("end of getting savd id");
            // Thread.Sleep(500); commented by sande
            return id;
        }

        public void WriteInToFileDataArr(string str)
        {
            string ProgTime = String.Format("_{0:yyyyMMdd}", DateTime.Now);
            string Location = appPath + "\\Logs\\DataArrival-" + ProgTime + ".txt";

            StreamWriter writer = default(StreamWriter);
            try
            {
                writer = new StreamWriter(Location, true, Encoding.Default, 8195);
                writer.WriteLine(str);
                writer.Flush();
            }
            catch (Exception ex)
            {
                Logger.WriteErrorLog(ex.Message);
            }
            finally
            {
                writer.Close();
                writer.Dispose();
            }
        }

        public void WriteInToFileDBInsert(string str)
        {
            string progTime = String.Format("_{0:yyyyMMdd}", DateTime.Now);
            string location = appPath + "\\Logs\\DBInsert-" + MName.Replace("\\", "") + progTime + ".txt";

            StreamWriter writer = default(StreamWriter);
            try
            {
                writer = new StreamWriter(location, true, Encoding.Default, 8195);
                writer.WriteLine(str);
                writer.Flush();
            }
            catch (Exception ex)
            {
                Logger.WriteErrorLog(ex.Message);
            }
            finally
            {
                if (writer != null)
                {
                    writer.Close();
                    writer.Dispose();
                }
            }
        }

        public string ProcessFile(string InputStr, string IP, string PortNo, string MName)
        {
            //string IStr = "STR-1-0111-000000001-1-0002-1-20110713-175258914-20110713-175847898-ENDSTR-2-0111-000000001-1-0002-004-20110713-175847898-20110713-180414434-ENDSTR-2-0111-000000001-1-0002-004-20110713-180414434-20110713-180426533-ENDSTR-11-0111-000000001-1-0002-20110713-180428371-ENDSTR-1-0111-000000001-1-0002-1-20110713-180428371-20110713-180518682-ENDSTR-2-0111-000000001-1-0002-003-20110713-180518682-20110713-181211112-ENDSTR-11-0111-000000001-1-0002-20110713-181212663-ENDSTR-1-0111-000000001-1-0002-1-20110713-181212663-20110713-184411916-ENDSTR-2-0111-000000001-1-0002-004-20110713-184411916-20110713-184518368-ENDSTR-11-0111-000000001-1-0002-20110713-184524040-ENDSTR-1-0111-000000001-1-0002-1-20110713-184524040-20110713-184525875-ENDSTR-11-0111-000000001-1-0002-20110713-184528122-ENDSTR-1-0111-000000001-1-0002-1-20110713-184528122-20110713-185403214-ENDSTR-2-0111-000000001-1-0002-003-20110713-185403214-20110713-185603297-ENDSTR-11-0111-000000001-1-0002-20110713-185605522-ENDSTR-1-0111-000000001-1-0002-1-20110713-185605522-20110713-185606226-END";
            //string IStr = "STR-1-0111-000000001-1-0002-1-20110713-175258914-20110713-175847898-ENDSTR-2-0111-000000001-1-0002-004-20110713-175847898-20110713-180414434-ENDSTR-2-0111-000000001-1-0002-004-20110713-180414434-20110713-180426533-ENDSTR-11-0111-000000001-1-0002-20110713-180428371-ENDSTR-1-0111-000000001-1-0002-1-20110713-180428371-20110713-180518682-ENDSTR-2-0111-000000001-1-0002-003-20110713-180518682-20110713-181211112-ENDSTR-11-0111-000000001-1-0002-20110713-181212663-ENDSTR-1-0111-000000001-1-0002-1-20110713-181212663-20110713-184411916-ENDSTR-2-0111-000000001-1-0002-004-20110713-184411916-20110713-184518368-ENDSTR-11-0111-000000001-1-0002-20110713-184524040-ENDSTR-1-0111-000000001-1-0002-1-20110713-184524040-20110713-184525875-ENDSTR-11-0111-000000001-1-0002-20110713-184528122-ENDSTR-1-0111-000000001-1-0002-1-20110713-184528122-20110713-185403214-ENDSTR-2-0111-000000001-1-0002-003-20110713-185403214-20110713-185603297-ENDSTR-11-0111-000000001-1-0002-20110713-185605522-ENDSTR-1-0111-000000001-1-0002-1-20110713";
            //string IStr = "STR-1-0111-000000001-1-0002-1-20110713-175258914-20110713-175847898-ENDSTR-2-0111-000000001-1-0002-004-20110713-175847898-20110713-180414434-ENDSTR-2-0111-000000001-1-0002-004-20110713-180414434-20110713-180426533-ENDSTR-11-0111-000000001-1-0002-20110713-180428371-ENDSTR-1-0111-000000001-1-0002-1-20110713-180428371-20110713-180518682-ENDSTR-2-0111-000000001-1-0002-003-20110713-180518682-20110713-181211112-ENDSTR-11-0111-000000001-1-0002-20110713-181212663-ENDSTR-1-0111-000000001-1-0002-1-20110713-181212663-20110713-184411916-ENDSTR-2-0111-000000001-1-0002-004-20110713-184411916-20110713-184518368-ENDSTR-11-0111-000000001-1-0002-20110713-184524040-ENDSTR-1-0111-000000001-1-0002-1-20110713-184524040-20110713-184525875-ENDSTR-11-0111-000000001-1-0002-20110713-184528122-ENDSTR-1-0111-000000001-1-0002-1-20110713-184528122-20110713-185403214-ENDSTR-2-0111-000000001-1-0002-003-20110713-185403214-20110713-185603297-ENDSTR-11-0111-000000001-1-0002-20110713-185605522-E";
            string IStr = InputStr.ToUpper().Trim();
            RequestData r1 = new RequestData();
            r1.LOF = IStr.Length;

            while (r1.LOF > 0 && r1.Status != 2)
            {
                try
                {
                    r1 = FindStartEndAlt(IStr.Substring(IStr.Length - r1.LOF), IP);
                    if (r1.SEStatus == "N")
                    {
                        WriteInToFileDBInsert(string.Format("{0} : String return from ProcessFile() : {1} ; for machine IP : {2}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss:FFFF"), r1.Program, IP));
                        return r1.Program;
                    }
                    if (r1.Program.IndexOf("START", "START".Length) > 0)
                    {
                        r1.Program = r1.Program.Substring(r1.Program.IndexOf("START", "START".Length));
                    }
                    string ValidString = FilterInvalids(r1.Program);
                    // string vs = ValidString;

                    WriteInToFileDBInsert(string.Format("{0} : Start Insert Record - {1} ; IP = {2}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss:FFFF"), ValidString, IP));
                    InsertDataUsingSP(ValidString, IP, PortNo);
                    WriteInToFileDBInsert(string.Format("{0} : Stop Insert - {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss:FFFF"), IP));

                    /*  Review required
                     bool type_3 = false;
                     type_3 = check_if_type3(vs);
                     if (type_3 == true)
                     {
                         PullProgramTransfer ppt = new PullProgramTransfer(interfaceId);
                         ppt.pull_function(vs);//to check and register program to send// 
                     }
                     else
                     {
                         WriteInToFileDBInsert(string.Format("{0} : Start Insert Record - {1} ; IP = {2}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss FFFFF"), ValidString, IP));
                         InsertDataUsingSP(ValidString, IP, PortNo);
                         WriteInToFileDBInsert(string.Format("{0} : Stop Insert - {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss FFFFF"), IP));
                     }
                     type_3 = false;
                     */
                }
                catch (Exception ex)
                {
                    Logger.WriteErrorLog("ProcessFile() :" + ex.Message);
                }
            }
            return string.Empty;
        }

        private bool check_if_type3(string temp)
        {
            if (temp.IndexOf("START-3-") >= 0 && temp.IndexOf("END") > 0 && temp.IndexOf("END") > temp.IndexOf("START-3-"))
            {
                return true;
            }
            else
                return false;
        }

        public static string FilterInvalids(string DataString)
        {
            string FilterString = string.Empty;
            try
            {
                for (int i = 0; i < DataString.Length; i++)
                {
                    byte[] asciiBytes = Encoding.ASCII.GetBytes(DataString.Substring(i, 1));

                    if (asciiBytes[0] >= Encoding.ASCII.GetBytes("#")[0] && asciiBytes[0] <= Encoding.ASCII.GetBytes("}")[0])  //to handle STR   -1-0111-000000001-1-0002-1-20110713-175258914-20110713-175847898-END more than 2 spaces in string
                    {
                        FilterString = FilterString + DataString.Substring(i, 1);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteErrorLog(ex.Message);
            }
            return FilterString;
        }

        public static int InsertDataUsingSP(string DataString, string IP, string PortNo)
        {
            int OutPut = 0;
            bool succeeded = false;
            int tries = 4;
            do
            {
                SqlConnection Con = ConnectionManager.GetConnection();
                SqlCommand cmd = new SqlCommand("s_GetProcessDataString", Con);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.Add("@datastring", SqlDbType.NVarChar).Value = DataString;
                cmd.Parameters.Add("@IpAddress", SqlDbType.NVarChar).Value = IP;
                cmd.Parameters.Add("@OutputPara", SqlDbType.Int).Value = 0;
                cmd.Parameters.Add("@LogicalPortNo", SqlDbType.SmallInt).Value = PortNo;

                try
                {
                    OutPut = cmd.ExecuteNonQuery();
                    succeeded = true;
                    if (OutPut < 0)
                    {
                        Logger.WriteErrorLog(string.Format("InsertDataUsingSP() - ExecuteNonQuery returns < 0 value : {0} :- {1}", IP, DataString));
                    }
                }
                catch (Exception ex)
                {
                    tries--;
                    Logger.WriteErrorLog("InsertDataUsingSP():" + ex.ToString());
                    Thread.Sleep(1000);
                }
                finally
                {
                    if (Con != null) Con.Close();
                }
            } while (!succeeded && tries > 0);

            return OutPut;
        }

        public static RequestData FindStartEnd(string Infiledata, string ip)
        {
            RequestData Req = new RequestData();
            Req.LOF = 0;
            Req.Status = 0;
            try
            {
                Regex StartmyRegEx = new Regex(@"S\s*T\s*A\s*R\s*T\s*");//\s* means any no of
                                                                        //white space
                Regex EndmyRegEx = new Regex(@"E\s*N\s*D\s*");

                bool ResultS = StartmyRegEx.IsMatch(Infiledata);
                bool ResultE = EndmyRegEx.IsMatch(Infiledata);

                if (ResultS == true && ResultE == true)
                {
                    //sande
                    int Sstr = 0, Estr = 0;
                    string prg = string.Empty;
                    int LOF = 0;
                    //sande
                    if (Infiledata.IndexOf("END") > Infiledata.IndexOf("START"))///if START comes before END
					{
                        Sstr = Infiledata.IndexOf("START");
                        Estr = Infiledata.IndexOf("END", Sstr);
                        prg = GetString(Infiledata.Substring(Sstr), "START", "END", out LOF);

                        Req.LOF = LOF;
                        Req.Program = prg;
                        if (prg == "" || prg.Length < 3)
                        {
                            Req.Status = 2;
                        }
                        else
                        {
                            Req.Status = 0;
                        }
                        Req.Process = 1;
                        Req.SEStatus = "Y";
                        return Req;
                    }

                    else
                    {
                        //"ST-76-05-03-01-20121003-085839-ENDSTART-76-05-03-01-02-20121003-085839-20121003-085840-END";

                        Sstr = Infiledata.IndexOf("START");
                        Estr = Infiledata.IndexOf("END", Sstr);

                        if (Estr == -1)//if end not comes//
                        {
                            Req.Program = "";
                            Req.Status = 2;
                        }

                        else//if end comes before start//
                        {
                            int IndexOfFirstEnd = Infiledata.IndexOf("END");
                            string TargetString = Infiledata.Substring(IndexOfFirstEnd + 3, Infiledata.Length - IndexOfFirstEnd - 3);
                            prg = GetString(TargetString, "START", "END", out LOF);
                            Req.LOF = LOF;
                            Req.Program = prg;
                            Req.SEStatus = "Y";
                            Req.Process = 1;
                        }
                    }
                }

                if (ResultS == true && ResultE == false)//if there is no END in the string//
                {
                    if (Infiledata.IndexOf("START") > 0)//if there is START//
                    {
                        int Sstr = 0;
                        string prg = string.Empty;
                        Sstr = Infiledata.IndexOf("START");
                        int LOF = 0;
                        prg = GetString(Infiledata.Substring(Sstr), "START", "END", out LOF);

                        Req.LOF = LOF;
                        Req.Program = prg;
                        if (prg == "" || (prg.IndexOf("END") != 0))
                        {
                            Req.Status = 2;
                            //Req.Status = 3;
                        }
                        else
                        {
                            Req.Status = 0;
                        }
                        Req.Process = 1;
                        return Req;
                    }
                }

                if (ResultS == false && ResultE == true)//if there is only END and not START//
                {
                    if (Infiledata.IndexOf("END") > 0)
                    {
                        int Sstr = 0;
                        string prg = string.Empty;
                        Sstr = Infiledata.IndexOf("START");

                        Req.LOF = 0;
                        Req.Program = "";
                        Req.Status = 2;
                        Req.Process = 1;

                        return Req;
                    }
                }

                if (Req.Process == 0)
                {
                    Req.Program = Infiledata;
                    Req.LOF = -2;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteErrorLog("FindStartEnd() : " + ex.Message);

            }
            return Req;
        }

        public static RequestData FindStartEndAlt(string Infiledata, string ip)
        {
            RequestData Req = new RequestData();
            Req.LOF = 0;
            Req.Status = 0;
            try
            {
                Regex StartmyRegEx = new Regex(@"S\s*T\s*A\s*R\s*T\s*-\s*");//\s* means any no of
                                                                            //white space
                Regex EndmyRegEx = new Regex(@"-\s*E\s*N\s*D\s*");

                bool ResultS = StartmyRegEx.IsMatch(Infiledata);
                bool ResultE = EndmyRegEx.IsMatch(Infiledata);

                if (ResultS == true && ResultE == true)
                {
                    //sande
                    int Sstr = 0, Estr = 0;
                    string prg = string.Empty;
                    int LOF = 0;
                    //sande
                    if (Infiledata.IndexOf("-END") > Infiledata.IndexOf("START-"))///if START comes before END
					{
                        Sstr = Infiledata.IndexOf("START-");
                        Estr = Infiledata.IndexOf("-END", Sstr);
                        prg = GetString(Infiledata.Substring(Sstr), "START-", "-END", out LOF);

                        Req.LOF = LOF;
                        Req.Program = prg;
                        if (prg == "" || prg.Length < 3)
                        {
                            Req.Status = 2;
                        }
                        else
                        {
                            Req.Status = 0;
                        }
                        Req.Process = 1;
                        Req.SEStatus = "Y";
                        return Req;
                    }

                    else
                    {
                        //"ST-76-05-03-01-20121003-085839-ENDSTART-76-05-03-01-02-20121003-085839-20121003-085840-END";

                        Sstr = Infiledata.IndexOf("START-");
                        Estr = Infiledata.IndexOf("-END", Sstr);

                        if (Estr == -1)//if end not comes//
                        {
                            Req.Program = "";
                            Req.Status = 2;
                        }

                        else//if end comes before start//
                        {
                            int IndexOfFirstEnd = Infiledata.IndexOf("-END");
                            string TargetString = Infiledata.Substring(IndexOfFirstEnd + 3, Infiledata.Length - IndexOfFirstEnd - 3);
                            prg = GetString(TargetString, "START-", "-END", out LOF);
                            Req.LOF = LOF;
                            Req.Program = prg;
                            Req.SEStatus = "Y";
                            Req.Process = 1;
                        }
                    }
                }

                if (ResultS == true && ResultE == false)//if there is no END in the string//
                {
                    if (Infiledata.IndexOf("START-") > 0)//if there is START//
                    {
                        int Sstr = 0;
                        string prg = string.Empty;
                        Sstr = Infiledata.IndexOf("START-");
                        int LOF = 0;
                        prg = GetString(Infiledata.Substring(Sstr), "START-", "-END", out LOF);

                        Req.LOF = LOF;
                        Req.Program = prg;
                        if (prg == "" || (prg.IndexOf("-END") != 0))
                        {
                            Req.Status = 2;
                            //Req.Status = 3;
                        }
                        else
                        {
                            Req.Status = 0;
                        }
                        Req.Process = 1;
                        return Req;
                    }
                }

                if (ResultS == false && ResultE == true)//if there is only END and not START//
                {
                    if (Infiledata.IndexOf("-END") > 0)
                    {
                        int Sstr = 0;
                        string prg = string.Empty;
                        Sstr = Infiledata.IndexOf("START-");

                        Req.LOF = 0;
                        Req.Program = "";
                        Req.Status = 2;
                        Req.Process = 1;

                        return Req;
                    }
                }

                if (Req.Process == 0)
                {
                    Req.Program = Infiledata;
                    Req.LOF = -2;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteErrorLog("FindStartEnd() : " + ex.Message);

            }
            return Req;
        }

        public static string GetString(string prg, string SDelimit, string EDelimit, out int LOF)
        {
            int PrgIs = 0, PrgIe = 0;
            string Program = string.Empty;
            if (SDelimit == EDelimit)
            {
                PrgIs = prg.IndexOf(SDelimit);
                PrgIs = prg.IndexOf(SDelimit, PrgIs + 1);
                PrgIe = prg.IndexOf(EDelimit, PrgIs + 1);
                //PrgIe = prg.IndexOf(EDelimit, PrgIs + 1);
            }
            else
            {
                PrgIs = prg.IndexOf(SDelimit);
                //PrgIe = prg.IndexOf(EDelimit, PrgIs + 1);
                //PrgIe = prg.IndexOf(EDelimit, PrgIs + EDelimit.Length);
                PrgIe = prg.IndexOf(EDelimit, PrgIs);
            }

            if (PrgIs != -1 && PrgIe != -1)
            {
                Program = prg.Substring(PrgIs, PrgIe - PrgIs + EDelimit.Length);
                //LOF = prg.Length - PrgIe - 1;
                LOF = prg.Length - PrgIe - EDelimit.Length;
            }
            else
            {
                if (PrgIs != -1)
                {
                    Program = prg.Substring(PrgIs, prg.Length - PrgIs);
                    //LOF = prg.Length - PrgIs;
                    LOF = prg.Length - PrgIs - EDelimit.Length;
                }
                else
                {
                    Program = prg;
                    LOF = prg.Length;
                }
            }
            return Program;
        }

        public static string ReadFullFileData(string location)
        {
            FileStream fileStream = null;
            string str = "";
            byte[] buffer = null;
            try
            {
                fileStream = File.Open(location, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                int length = (int)fileStream.Length;  // get file length
                buffer = new byte[length];            // create buffer
                int count;                            // actual number of bytes read
                int sum = 0;                          // total number of bytes read

                // read until Read method returns 0 (end of the stream has been reached)
                while ((count = fileStream.Read(buffer, sum, length - sum)) > 0)
                    sum += count;  // sum is a buffer offset for next reading
            }
            catch (Exception ex)
            {
                str = @"\n" + ex.Message.ToString() + @"\n";
            }
            finally
            {
                fileStream.Close();
            }
            str = System.Text.Encoding.ASCII.GetString(buffer);
            return str;
        }

        TcpClient tcpClient = null;
        ModbusIpMaster master = null;       
        private ModbusIpMaster ConnectModBus()
        {          
           
            int count = 0;
            Ping netMon = default(Ping);
            netMon = new Ping();
            PingReply reply = null;
            do
            {
                #region StopService
                if (ServiceStop.stop_service == 1)
                {
                    try
                    {
                        if (tcpClient != null && tcpClient.Connected)
                        {
                            tcpClient.Close();
                        }

                        if (master != null)
                        {
                            master.Dispose();
                        }
                       
                        Logger.WriteDebugLog("stopping the service. coming out of main while loop.");
                        break;
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteErrorLog(ex.Message);
                        break;
                    }
                }
                #endregion
               
                try
                {
                    reply = netMon.Send(this._ipAddress, 4000);
                    if (reply.Status == IPStatus.Success)
                    {
                        if (tcpClient == null || master == null || (tcpClient != null && tcpClient.Client.Connected == false))
                        {
                            if (tcpClient  != null) { tcpClient.Close(); tcpClient = null; }
                            if (master != null) { master.Dispose(); master = null; }
                            tcpClient = new TcpClient(this._ipAddress, this._portNo);  //Port no is always 502
                            tcpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                            tcpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                            tcpClient.Client.NoDelay = true;
                            Thread.Sleep(300);
                            master = ModbusIpMaster.CreateIp(tcpClient);
                            master.Transport.Retries = 10;
                            master.Transport.ReadTimeout = 4000;
                            master.Transport.WriteTimeout = 4000;
                            master.Transport.WaitToRetryMilliseconds = 2000;
                        }
                        return master;
                    }
                    else
                    {
                        Logger.WriteDebugLog("Disconnected from network (No ping). Ping Status = " + reply.Status.ToString());
                        Thread.Sleep(1000 * 4);
                    }
                    count++;
                }
                catch (Exception ex)
                {
                    Logger.WriteErrorLog(ex.ToString());
                }
                finally
                {

                }
            } while (reply.Status != IPStatus.Success && count < 3);
            if (netMon != null)
            {
                netMon.Dispose();
            }
            return master;
        }

        private void WriteToACKFile(string ACKFilePath, string dataToWrite)
        {
            StreamWriter writer = null;
            try
            {
                writer = new StreamWriter(ACKFilePath, false);
                writer.Write(dataToWrite);
                writer.Flush();
                Logger.WriteDebugLog("ACK file generated successfully at PATH." + ACKFilePath);
            }
            catch (Exception ex)
            {
                Logger.WriteErrorLog(ex.ToString());
            }
            finally
            {
                if (writer != null)
                {
                    writer.Close();
                    writer.Dispose();
                }
            }
        }

        private bool CheckPingStatus(string ipAddress)
        {
            bool pingStatus = false;
            IPStatus status = IPStatus.Unknown;
            Ping ping = null;
            try
            {
                ping = new Ping();
                PingReply pingReply = ping.Send(ipAddress, 10000);
                status = pingReply.Status;
                if (pingReply.Status == IPStatus.Success)
                {
                    pingStatus = true;
                }
                else
                {
                    Logger.WriteErrorLog(string.Format("Not able to ping IP Address {0} . Ping status {1} ", ipAddress, status.ToString()));
                }
            }
            catch (Exception)
            {
                Logger.WriteErrorLog(string.Format("Not able to ping IP Address {0} . Ping status {1} ", ipAddress, status.ToString()));
            }
            finally
            {
                if (ping != null)
                {
                    ping.Dispose();
                }
            }
            return pingStatus;
        }
    }
}
