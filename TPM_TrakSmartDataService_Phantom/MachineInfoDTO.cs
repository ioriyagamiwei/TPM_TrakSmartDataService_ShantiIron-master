using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TPM_TrakSmartDataService_Phantom
{
	class MachineInfoDTO
	{
		#region private
		private string _ip;
		private int _portNo;
		private string _interfaceId;
		private string _machineId;
		private string _dataCollectionProtocol;
		private string _process;
		private string _reqfolderpath;
		private string _resfolderpath;
		private string _ackfolderpath;
		private string _spcfolderpath;
		private string _machinePath;
		private int _opnid;

		#endregion

		public string IpAddress
		{
			get { return _ip; }
			set { _ip = value; }
		}

		public string MachinePath
		{
			get { return _machinePath; }
			set { _machinePath = value; }
		}

		public int PortNo
		{
			get { return _portNo; }
			set { _portNo = value; }
		}
		public string MachineId
		{
			get { return _machineId; }
			set { _machineId = value; }
		}

		public string InterfaceId
		{
			get { return _interfaceId; }
			set { _interfaceId = value; }
		}

		public string DataCollectionProtocol
		{
			get { return _dataCollectionProtocol; }
			set { _dataCollectionProtocol = value; }
		}

		public string Process
		{
			get { return _process; }
			set { _process = value; }
		}

		public string REQFolderPath
		{
			get { return _reqfolderpath; }
			set { _reqfolderpath = value; }
		}
		public string RESFolderPath
		{
			get { return _resfolderpath; }
			set { _resfolderpath = value; }
		}
		public string ACKFolderPath
		{
			get { return _ackfolderpath; }
			set { _ackfolderpath = value; }
		}
		public string SPCFolderPath
		{
			get { return _spcfolderpath; }
			set { _spcfolderpath = value; }
		}
		public int OpnID
		{
			get { return _opnid; }
			set { _opnid = value; }
		}

		public ushort HoldingRegisterForCommunication { get; set; }
		public ushort HoldingRegisterDateAndStatus { get; set; }
		public ushort HoldingRegisterDateAndStatusAckAddress { get; set; }

		public ushort HoldingRegisterStartAddress_M1 { get; set; }
		public ushort HoldingRegisterStartAddress_M2 { get; set; }
		public ushort HoldingRegisterStartAddress_M3 { get; set; }

		public ushort BytesToRead_M1 { get; set; }
		public ushort BytesToRead_M2 { get; set; }
		public ushort BytesToRead_M3 { get; set; }

		public ushort AckAddress_M1 { get; set; }
		public ushort AckAddress_M2 { get; set; }
		public ushort AckAddress_M3 { get; set; }
	}

	public class RequestData
	{
		public string Program;
		public string SEStatus;

		public int Process;
		public int LOF;
		public int Status;

		public RequestData()
		{
			Program = string.Empty;
			SEStatus = "N";
		}
	}

	public class dailyCheckList
	{
		public int SlNo { get; set; }
		public string activity { get; set; }
	}
}
