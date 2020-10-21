using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TPM_TrakSmartDataService_Phantom
{
	class DatabaseAccess
	{
		internal static List<MachineInfoDTO> GetTPMTrakMachine()
		{
			List<MachineInfoDTO> machines = new List<MachineInfoDTO>();
			string query = @"select T.machineid,T.IP,T.IPPortNO,T.InterfaceID,T.DAPEnabled,T.OpnID,T.REQFolderPath,T.RESFolderPath,T.ACKFolderPath,
                                           T.SPCFolderPath,T.Process,TDRI.AutoID,
                            TDRI.HoldingRegisterForCommunication,TDRI.HoldingRegisterDateAndStatus,TDRI.HoldingRegisterDateAndStatusAckAddress,
					        TDRI.HoldingRegisterStartAddress_M1,TDRI.BytesToRead_M1,TDRI.AckAddress_M1,
                            TDRI.HoldingRegisterStartAddress_M2,TDRI.BytesToRead_M2,TDRI.AckAddress_M2,
                            TDRI.HoldingRegisterStartAddress_M3,TDRI.BytesToRead_M3,TDRI.AckAddress_M3					
                             from  ( select machineid,IP,IPPortno,Interfaceid,DAPEnabled, iSNULL(OpnID,0) AS OpnID, REQFolderPath,RESFolderPath,ACKFolderPath,SPCFolderPath,Process 
                            from MachineInformation where TPMTrakEnabled = 1)T 
							left outer join [SmartDataModbusRegisterInfo] TDRI on T.machineid = TDRI.MachineID";

			SqlConnection conn = ConnectionManager.GetConnection();
			SqlCommand cmd = new SqlCommand(query, conn);
			SqlDataReader reader = default(SqlDataReader);
			try
			{
				reader = cmd.ExecuteReader(CommandBehavior.CloseConnection);
				while (reader.Read())
				{
					MachineInfoDTO machine = new MachineInfoDTO();
					machine.MachineId = reader["machineid"].ToString().Trim();
					machine.IpAddress = reader["IP"].ToString().Trim();
					machine.PortNo = Int32.Parse(reader["IPPortNo"].ToString().Trim());
					machine.InterfaceId = reader["Interfaceid"].ToString().Trim();
					machine.DataCollectionProtocol = GetProtocol(reader["DAPEnabled"].ToString());
					machine.REQFolderPath = reader["REQFolderPath"].ToString().Trim();
					machine.RESFolderPath = reader["RESFolderPath"].ToString().Trim();
					machine.ACKFolderPath = reader["ACKFolderPath"].ToString().Trim();
					machine.SPCFolderPath = reader["SPCFolderPath"].ToString().Trim();
					machine.Process = reader["Process"].ToString().Trim();
					machine.OpnID = Int32.Parse(reader["OpnID"].ToString().Trim());

					if (machine.DataCollectionProtocol.Equals("modbus", StringComparison.OrdinalIgnoreCase) || machine.DataCollectionProtocol.Equals("profinet", StringComparison.OrdinalIgnoreCase))
					{
						if (reader["HoldingRegisterForCommunication"] != DBNull.Value) machine.HoldingRegisterForCommunication = Convert.ToUInt16(reader["HoldingRegisterForCommunication"].ToString());
						if (reader["HoldingRegisterDateAndStatus"] != DBNull.Value) machine.HoldingRegisterDateAndStatus = Convert.ToUInt16(reader["HoldingRegisterDateAndStatus"].ToString());
						if (reader["HoldingRegisterDateAndStatusAckAddress"] != DBNull.Value) machine.HoldingRegisterDateAndStatusAckAddress = Convert.ToUInt16(reader["HoldingRegisterDateAndStatusAckAddress"].ToString());

						if (reader["AckAddress_M1"] != DBNull.Value) machine.AckAddress_M1 = Convert.ToUInt16(reader["AckAddress_M1"].ToString());
						if (reader["AckAddress_M2"] != DBNull.Value) machine.AckAddress_M2 = Convert.ToUInt16(reader["AckAddress_M2"].ToString());
						if (reader["AckAddress_M3"] != DBNull.Value) machine.AckAddress_M3 = Convert.ToUInt16(reader["AckAddress_M3"].ToString());

						if (reader["BytesToRead_M1"] != DBNull.Value) machine.BytesToRead_M1 = Convert.ToUInt16(reader["BytesToRead_M1"].ToString());
						if (reader["BytesToRead_M2"] != DBNull.Value) machine.BytesToRead_M2 = Convert.ToUInt16(reader["BytesToRead_M2"].ToString());
						if (reader["BytesToRead_M3"] != DBNull.Value) machine.BytesToRead_M3 = Convert.ToUInt16(reader["BytesToRead_M3"].ToString());

						if (reader["HoldingRegisterStartAddress_M1"] != DBNull.Value) machine.HoldingRegisterStartAddress_M1 = Convert.ToUInt16(reader["HoldingRegisterStartAddress_M1"].ToString());
						if (reader["HoldingRegisterStartAddress_M2"] != DBNull.Value) machine.HoldingRegisterStartAddress_M2 = Convert.ToUInt16(reader["HoldingRegisterStartAddress_M2"].ToString());
						if (reader["HoldingRegisterStartAddress_M3"] != DBNull.Value) machine.HoldingRegisterStartAddress_M3 = Convert.ToUInt16(reader["HoldingRegisterStartAddress_M3"].ToString());
					}

					machines.Add(machine);
				}
			}
			catch (Exception ex)
			{
				Logger.WriteErrorLog(ex.Message);
			}
			finally
			{
				if (reader != null) reader.Close();
				if (conn != null) conn.Close();
			}

			return machines;
		}

		private static string GetProtocol(string str)
		{
			string protocol = "profinet";
			switch (str)
			{
				case "0":
					protocol = "raw";
					break;
				case "1":
					protocol = "dap";
					break;
				case "2":
					protocol = "modbus";
					break;
				case "3":
					protocol = "profinet";
					break;
				case "4":
					protocol = "csv";
					break;

			}
			return protocol;
		}

		internal static void ProcessDataString(string dataString, out int Output)
		{
			SqlConnection conn = ConnectionManager.GetConnection();
			SqlCommand cmd = null;
			Output = -1;
			try
			{
				cmd = new SqlCommand("s_GetProcessDataString", conn);
				cmd.Parameters.AddWithValue("@datastring", dataString);
				cmd.Parameters.Add("@IpAddress", SqlDbType.NVarChar).Value = "127.0.0.1";
				cmd.Parameters.Add("@OutputPara", SqlDbType.Int).Value = 0;
				cmd.Parameters.Add("@LogicalPortNo", SqlDbType.SmallInt).Value = "33";
				cmd.CommandType = CommandType.StoredProcedure;
				Logger.WriteDebugLog("Inserting to database : " + dataString);
				Output = cmd.ExecuteNonQuery();
				Logger.WriteDebugLog("inserted to database. Outpot value = " + Output);
			}
			catch (Exception ex)
			{
				Logger.WriteErrorLog(ex.ToString());
			}
			finally
			{
				if (conn != null) conn.Close();
			}
		}

		internal static void InsertToRawdata(int dataType,string machine, string IP ,string comp, int OPN, int OPR, int sup, DateTime sttime, DateTime endtime,string supplierCode, string partSlNo, string heatCode,string result, string remarks)
		{
			SqlConnection conn = ConnectionManager.GetConnection();
			SqlCommand cmd = null;
			int RecordEffected = 0;
			string Query = string.Empty;
			try
			{
				//Query = @"insert into RawData(DataType,IPAddress,Mc,Comp,Opn,Opr,SPLSTRING1,Sttime,Ndtime,Status,WorkOrderNumber,SPLString3,SPLString4,SPLString5,SPLString6)
				//		Values(@DataType,@IPAddress,@Mc,@Comp,@Opn,@Opr,@SPLString1,@Sttime,@Ndtime,@Status,@WorkOrderNumber,@SPLString3,@SPLString4,@SPLString5,@SPLString6)";

				Query = @"Insert into Rawdata(datatype,IPAddress,Mc,Comp,Opn,Opr,SPLSTRING1,Sttime,Ndtime,Status,WorkOrderNumber,SPLString3,SPLString4,SPLString5,SPLString6,SPLString7)
						values
				   (@tp_int, @IpAddress, @McInterfaceID, @component, @operation, @operator, @PalletCount, @Sttime, @Ndtime, @Status, @WorkOrder, @PartSlNo, @SupplierCode, @SupervisorCode, @Result, @Remarks)";

				cmd = new SqlCommand(Query, conn);
				cmd.Parameters.AddWithValue("@tp_int", dataType);
				cmd.Parameters.AddWithValue("@IpAddress", IP);
				cmd.Parameters.AddWithValue("@McInterfaceID", machine);
				cmd.Parameters.AddWithValue("@component", comp);
				cmd.Parameters.AddWithValue("@operation", OPN);
				cmd.Parameters.AddWithValue("@operator", OPR);
				cmd.Parameters.AddWithValue("@Sttime", sttime.ToString("yyyy-MM-dd HH:mm:ss"));
				cmd.Parameters.AddWithValue("@Ndtime", endtime.ToString("yyyy-MM-dd HH:mm:ss"));
				cmd.Parameters.AddWithValue("@PalletCount", 1);
				cmd.Parameters.AddWithValue("@Status", 0);
				cmd.Parameters.AddWithValue("@WorkOrder", heatCode);
				cmd.Parameters.AddWithValue("@PartSlNo", partSlNo);
				cmd.Parameters.AddWithValue("@SupplierCode", supplierCode);
				cmd.Parameters.AddWithValue("@SupervisorCode", sup);
				cmd.Parameters.AddWithValue("@Result", result);
				cmd.Parameters.AddWithValue("@Remarks", remarks);

				cmd.CommandType = CommandType.Text;
				RecordEffected= cmd.ExecuteNonQuery();
				if (RecordEffected > 0)
					Logger.WriteDebugLog("Record inserted into Raw Data successfully.");
				else
					Logger.WriteDebugLog("Could not insert data into Raw Data.");
			}
			catch (Exception ex)
			{
				Logger.WriteErrorLog(ex.ToString());
			}
			finally
			{
				if (conn != null) conn.Close();
			}
		}

		internal static void InsertToSPCAutodata(string Machine, string Comp,int OPN,string ParameterID,double value)
		{
			SqlConnection conn = ConnectionManager.GetConnection();
			SqlCommand cmd = null;
			int RecordEffected = 0;
			string Query = string.Empty;
			try
			{
				Query = @"insert into SPCAutodata (Mc, Comp, Opn, Dimension, [Value], Opr,[Timestamp],BatchTS)
							values(@Machine,@Comp,@Opn,@Dimention,@value,@opr,@Timestamp,@Timestamp)";

				cmd = new SqlCommand(Query, conn);
				cmd.Parameters.AddWithValue("@Machine", Machine);
				cmd.Parameters.AddWithValue("@Comp", Comp);
				cmd.Parameters.AddWithValue("@Opn", OPN);
				cmd.Parameters.AddWithValue("@Dimention", ParameterID);
				cmd.Parameters.AddWithValue("@value", value);
				cmd.Parameters.AddWithValue("@Opr", "1");
				cmd.Parameters.AddWithValue("@Timestamp", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

				cmd.CommandType = CommandType.Text;
				RecordEffected = cmd.ExecuteNonQuery();
				
				if (RecordEffected > 0)
					Logger.WriteDebugLog("Record inserted into SPCAutoData successfully.");
				else
					Logger.WriteDebugLog("Could not insert data into SPCAutoData.");
			}
			catch (Exception ex)
			{
				Logger.WriteErrorLog(ex.ToString());
			}
			finally
			{
				if (conn != null) conn.Close();
			}
		}

        internal static void UpdatePackingStation_Shanti(string Machine, string component, string serialNo, string ReportfullName, string ReportType)
        {
			SqlConnection conn = ConnectionManager.GetConnection();
			SqlCommand cmd = null;
			string query = string.Empty;
            if (ReportType.Equals("pdi", StringComparison.OrdinalIgnoreCase))
            {
				query = @"IF NOT EXISTS(SELECT * from [PackingStation_Shanti] where ComponentId=@comp and CompSlNo=@slNo)
								BEGIN
									UPDATE [PackingStation_Shanti] set PDIReportName=@PDIreport where ComponentId=@comp and CompSlNo=@slNo
								END
							 ELSE
								BEGIN
									INSERT into PackingStation_Shanti (MachineId, ComponentId, CompSlNo, PDIReportName) values (@mc ,@comp, @slNo, @PDIreport)
								END";
			}
            else if (ReportType.Equals("batch", StringComparison.OrdinalIgnoreCase))
			{
				query = @"IF NOT EXISTS(SELECT * from [PackingStation_Shanti] where ComponentId=@comp and CompSlNo=@slNo)
								BEGIN
									UPDATE [PackingStation_Shanti] set BatchReportName=@Batchreport where ComponentId=@comp and CompSlNo=@slNo
								END
							 ELSE
								BEGIN
									INSERT into PackingStation_Shanti (MachineId, ComponentId, CompSlNo, BatchReportName) values (@mc ,@comp, @slNo, @Batchreport)
								END";
			}

            try
            {
				cmd = new SqlCommand(query, conn);
				cmd.Parameters.AddWithValue("@mc", Machine);
				cmd.Parameters.AddWithValue("@comp", component);
				cmd.Parameters.AddWithValue("@slNo", serialNo);
				cmd.Parameters.AddWithValue("@PDIreport", ReportfullName);
				cmd.Parameters.AddWithValue("@Batchreport", ReportfullName);

				int res = cmd.ExecuteNonQuery();
                if (res >= 0)
                {
					Logger.WriteDebugLog(ReportfullName+" Inserted/Updated To PackingStation_Shanti Table Successfully");
                }
                else
                {
					Logger.WriteDebugLog(ReportfullName+" Insertion/Updation To PackingStation_Shanti Table Failed");
				}

			}
			catch(Exception ex)
            {
				Logger.WriteErrorLog(ex.Message);
            }
            finally
            {
				if (conn != null) conn.Close();
            }

        }

        internal static void InsertToSPCAutodata(string Machine, string Comp, int OPN, string ParameterID, double value, string partSlNo)
		{
			SqlConnection conn = ConnectionManager.GetConnection();
			SqlCommand cmd = null;
			int RecordEffected = 0;
			string Query = string.Empty;
			try
			{
				Query = @"if not exists(select * from SPCAutodata where Mc = @Machine and Comp = @Comp and Opn = @Opn and Dimension = @Dimention and SerialNumber = @SerialNumber) 
begin 
insert into SPCAutodata (Mc, Comp, Opn, Dimension, [Value], Opr, [Timestamp], BatchTS, SerialNumber) values(@Machine, @Comp, @Opn, @Dimention, @value, @opr, @Timestamp, @Timestamp, @SerialNumber) 
end 
else begin 

update SPCAutodata set [Value] = @value, [Timestamp] = @Timestamp, BatchTS = @Timestamp, SerialNumber = @SerialNumber where Mc = @Machine and Comp = @Comp and Opn = @Opn and Dimension = @Dimention and SerialNumber = @SerialNumber
end";

				cmd = new SqlCommand(Query, conn);
				cmd.Parameters.AddWithValue("@Machine", Machine);
				cmd.Parameters.AddWithValue("@Comp", Comp);
				cmd.Parameters.AddWithValue("@Opn", OPN);
				cmd.Parameters.AddWithValue("@Dimention", ParameterID);
				cmd.Parameters.AddWithValue("@value", value.ToString("##.####"));
				cmd.Parameters.AddWithValue("@Opr", "1");
				cmd.Parameters.AddWithValue("@Timestamp", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
				cmd.Parameters.AddWithValue("@SerialNumber", partSlNo);

				cmd.CommandType = CommandType.Text;
				RecordEffected = cmd.ExecuteNonQuery();

				if (RecordEffected > 0)
					Logger.WriteDebugLog("Record inserted into SPCAutoData successfully.");
				else
					Logger.WriteDebugLog("Could not insert data into SPCAutoData.");
			}
			catch (Exception ex)
			{
				Logger.WriteErrorLog(ex.ToString());
			}
			finally
			{
				if (conn != null) conn.Close();
			}
		}

		internal static void InsertToSPCCharacteristic(string machineId, string componentID, int opnID, string partSlno, string characteristics, double nominalValue, double lSL, double uSL)
		{
			SqlConnection conn = ConnectionManager.GetConnection();
			try
			{
				string Query = @"if not exists(select * from SPC_Characteristic where MachineID = @MachineID and ComponentID = @ComponentID and OperationNo = @OperationNo and CharacteristicID = @CharacteristicID) 
					begin 
						insert into SPC_Characteristic (MachineID, ComponentID, OperationNo, CharacteristicCode, CharacteristicID, SpecificationMean, LSL, USL, Datatype, InspectedBy) values(@MachineID, @ComponentID, @OperationNo, @CharacteristicCode, @CharacteristicID, @SpecificationMean, @LSL, @USL, @dataType, @inspectedBy) 
					end 
					else 
						begin 
							update SPC_Characteristic set CharacteristicCode = @CharacteristicCode, SpecificationMean = @SpecificationMean, LSL = @LSL, USL = @USL, Datatype = @dataType, InspectedBy = @inspectedBy where MachineID = @MachineID and ComponentID = @ComponentID and OperationNo = @OperationNo and CharacteristicID = @CharacteristicID 
					   end";

				SqlCommand cmd = new SqlCommand(Query, conn);
				cmd.Parameters.AddWithValue("@MachineID", machineId);
				cmd.Parameters.AddWithValue("@ComponentID", componentID);
				cmd.Parameters.AddWithValue("@OperationNo", opnID);
				cmd.Parameters.AddWithValue("@CharacteristicCode", characteristics);
				cmd.Parameters.AddWithValue("@CharacteristicID", characteristics);
				cmd.Parameters.AddWithValue("@SpecificationMean", nominalValue.ToString("##.####"));
				cmd.Parameters.AddWithValue("@LSL", (nominalValue + lSL).ToString("##.####"));
				cmd.Parameters.AddWithValue("@USL", (nominalValue + uSL).ToString("##.####"));
				cmd.Parameters.AddWithValue("@dataType", "Numeric");
				cmd.Parameters.AddWithValue("@inspectedBy", "Operator");

				cmd.CommandType = CommandType.Text;
				int RecordEffected = cmd.ExecuteNonQuery();

				if (RecordEffected > 0)
					Logger.WriteDebugLog("Record inserted into SPC_Characteristic successfully.");
				else
					Logger.WriteDebugLog("Could not insert data into SPC_Characteristic.");
			}
			catch (Exception ex)
			{
				Logger.WriteErrorLog(ex.ToString());
			}
			finally
			{
				if (conn != null) conn.Close();
			}
		}


		// 4 -rej, 5=marked, 7=not allowerd on this mc, 9= next operation not exists for co
		public static string GetStatus28Type(string dataString)
		{
			SqlConnection conn = ConnectionManager.GetConnection();
			SqlCommand cmd = new SqlCommand("s_GetProcessDataString", conn);
			string strTemp = string.Empty;
			cmd.CommandType = CommandType.StoredProcedure;

			cmd.Parameters.Add("@datastring", SqlDbType.NVarChar).Value = dataString;
			cmd.Parameters.Add("@IpAddress", SqlDbType.NVarChar).Value = "127.0.0.1";
			cmd.Parameters.Add("@OutputPara", SqlDbType.Int).Value = 0;
			cmd.Parameters.Add("@LogicalPortNo", SqlDbType.SmallInt).Value = "33";
			object obj = null;
			try
			{
				obj = cmd.ExecuteScalar();
				if (obj != null)
				{
					strTemp = obj.ToString();
				}
			}
			catch (Exception ex)
			{
				Logger.WriteErrorLog(ex.Message);
			}
			finally
			{
				if (conn != null)
				{
					conn.Close();
					cmd = null;
					conn = null;
				}
			}

            //1=ok,  2=this not allowerd on this mc(mco valid),3=already exists,6=sl not exists, 8 = out of sequence, 4 -rej, 5=marked for rek, 
            //7 =this not allowerd on this mc, 9= next operation not exists for co
            return strTemp;
		}

		public static void insertDailyCheckList(string machineID, DateTime dt, int activityID)
		{
			SqlConnection conn = ConnectionManager.GetConnection();
			SqlCommand cmd = null;
			try
			{
				string sqlQry = @"Insert into dailyCheckListShanti_Transaction(MachineID,Activity,UpdatedTS)
                                  values(@MachineID,@Activity,@UpdatedTS)";
				cmd = new SqlCommand(sqlQry, conn);
				cmd.CommandType = CommandType.Text;
				cmd.Parameters.AddWithValue("@MachineID", machineID);
				cmd.Parameters.AddWithValue("@Activity", activityID);
				cmd.Parameters.AddWithValue("@UpdatedTS", dt.ToString("yyyy-MM-dd HH:mm:ss"));
				cmd.ExecuteNonQuery();
			}
			catch (Exception ex)
			{
				Logger.WriteErrorLog(ex.ToString());
			}
			finally
			{
				if (conn != null) conn.Close();
			}
		}

		public static List<dailyCheckList> getCheckListMaster()
		{
			List<dailyCheckList> checKLists = new List<dailyCheckList>();
			SqlConnection conn = ConnectionManager.GetConnection();
			SqlCommand cmd = null;
			SqlDataReader sdr = null;
			try
			{
				cmd = new SqlCommand("select * from DailyCheckListShanti_Master order by SlNO asc", conn);
				sdr = cmd.ExecuteReader();
				while (sdr.Read())
				{
					dailyCheckList cL = new dailyCheckList();
					cL.SlNo = Convert.ToInt16(sdr["SlNO"].ToString());
					cL.activity = sdr["Activity"].ToString();
					checKLists.Add(cL);
				}
			}
			catch (Exception ex)
			{
				Logger.WriteErrorLog(ex.ToString());
			}
			finally
			{
				if (sdr != null) sdr.Close();
				if (conn != null) conn.Close();
			}
			return checKLists;
		}

        internal static List<string> GetProcessParameterForPLC(string Machine, string Comp, string Opn)
        {
			List<string> dataStrings = new List<string>();
			SqlConnection conn = ConnectionManager.GetConnection();
			SqlCommand cmd = null;
			SqlDataReader rdr = null;
			string query = @"SELECT top 15 CharacteristicID FROM [dbo].[SPC_Characteristic] where MachineID = @machine and ComponentID = @comp and 
							OperationNo = @opn and IsEnabled = 1";
			try
			{
				cmd = new SqlCommand(query, conn);
				cmd.Parameters.AddWithValue("@comp", Comp);// comp = 56
				cmd.Parameters.AddWithValue("@machine", Machine);
				cmd.Parameters.AddWithValue("@opn", Opn);
				rdr = cmd.ExecuteReader();
				while (rdr.Read())
				{
					dataStrings.Add(rdr["CharacteristicID"].ToString());
				}
			}catch(Exception ex)
			{
				Logger.WriteErrorLog(ex.Message);
			}
			finally
			{
				if (rdr != null) rdr.Close();
				if (conn != null) conn.Close();
			}
			return dataStrings;
        }

		internal static void InsertParameterDataToSPCAutoData(string slNo,string machine, string comp, string opn, string opr, string dimension, float value, DateTime timeStamp, DateTime batchTS)
		{
			SqlConnection conn = ConnectionManager.GetConnection();
			SqlCommand cmd = null;
			string query = @"insert into SPCAutodata (Mc, Comp, Opn, Opr, Dimension, Value, TimeStamp, BatchTS, SerialNumber) values (@mc,@comp,@opn,@opr,@dimension,@value,@timestamp,@batchTS,@slNo)";

			try
			{
				cmd = new SqlCommand(query, conn);
				cmd.CommandType = CommandType.Text;
				cmd.Parameters.AddWithValue("@mc", machine);
				cmd.Parameters.AddWithValue("@comp", comp);
				cmd.Parameters.AddWithValue("@opn", opn);
				cmd.Parameters.AddWithValue("@opr", opr);
				cmd.Parameters.AddWithValue("@dimension", dimension);
				cmd.Parameters.AddWithValue("@value", value.ToString());
				cmd.Parameters.AddWithValue("@timestamp", timeStamp.ToString("yyyy-MM-dd HH:mm:ss"));
				cmd.Parameters.AddWithValue("@batchTS", batchTS.ToString("yyyy-MM-dd HH:mm:ss"));
				cmd.Parameters.AddWithValue("@slNo", slNo);
				cmd.ExecuteNonQuery();
				Logger.WriteDebugLog("DataInserted to SPC Auto Data successfully ");
			}catch(Exception ex)
			{
				Logger.WriteErrorLog(ex.Message);
			}
			finally
			{
				if (conn != null) conn.Close();
			}
		}

		internal static void GetOperatorAndSupervisorForShift(string MachineID, DateTime CurrentDateTime, out int operatorID, out int supervisorID)
		{
			operatorID = supervisorID = 1;
			int ID = 1;
			SqlConnection conn = ConnectionManager.GetConnection();
			SqlCommand cmd = null;
			SqlDataReader reader = null;
			string Proc = @"s_GetEmployeeAllocation_Shanti";
			try
			{
				cmd = new SqlCommand(Proc, conn);
				cmd.CommandType = CommandType.StoredProcedure;
				cmd.Parameters.AddWithValue("@StartDate", CurrentDateTime.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.Parameters.AddWithValue("@MachineId", MachineID);
                reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    int.TryParse(reader["InterfaceId"].ToString(), out ID);

                    if (reader["Type"].ToString().Equals("Operator", StringComparison.OrdinalIgnoreCase))
                        operatorID = ID;

                    else if (reader["Type"].ToString().Equals("Supervisor", StringComparison.OrdinalIgnoreCase))
						supervisorID = ID;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteErrorLog("Exception while executing [s_GetEmployeeAllocation_Shanti] proc : " + ex.Message);
			}
			finally
			{
				if (reader != null) reader.Close();
				if (conn != null) conn.Close();
				if (ID <= 0)
				{
					operatorID = supervisorID = 1;
				}
			}

		}

		internal static void InsertToSPCAutodata(string Machine, string Comp, int OPN, string ParameterID, double value, DateTime CycleStart)
		{
			SqlConnection conn = ConnectionManager.GetConnection();
			SqlCommand cmd = null;
			int RecordEffected = 0;
			string Query = string.Empty;
			try
			{
				Query = @"insert into SPCAutodata (Mc, Comp, Opn, Dimension, [Value], Opr,[Timestamp],BatchTS)
							values(@Machine,@Comp,@Opn,@Dimention,@value,@opr,@Timestamp,@Timestamp)";

				cmd = new SqlCommand(Query, conn);
				cmd.Parameters.AddWithValue("@Machine", Machine);
				cmd.Parameters.AddWithValue("@Comp", Comp);
				cmd.Parameters.AddWithValue("@Opn", OPN);
				cmd.Parameters.AddWithValue("@Dimention", ParameterID);
				cmd.Parameters.AddWithValue("@value", value.ToString("##.####"));
				cmd.Parameters.AddWithValue("@Opr", "1");
				cmd.Parameters.AddWithValue("@Timestamp", CycleStart.ToString("yyyy-MM-dd HH:mm:ss"));

				cmd.CommandType = CommandType.Text;
				RecordEffected = cmd.ExecuteNonQuery();

				if (RecordEffected > 0)
					Logger.WriteDebugLog("Record inserted into SPCAutoData successfully.");
				else
					Logger.WriteDebugLog("Could not insert data into SPCAutoData.");
			}
			catch (Exception ex)
			{
				Logger.WriteErrorLog(ex.ToString());
			}
			finally
			{
				if (conn != null) conn.Close();
			}
		}

		internal static void InsertToRawdata_MPI(string partNubmer, string partSerialNumber, string heatCode, string supplierCode, string manualInsResult, string cameraResult, string cameraPicLink, string deMagLevel, DateTime insStartDateTime, DateTime insEndDateTime, string groupId, string remarks, string visualInsp)
		{
			SqlConnection conn = ConnectionManager.GetConnection();
			string Query = @"insert into rawdata_MPI(PartNumber, PartSerialNumber, HeatCode, SupplierCode, ManualInsResult, CameraInsResult, CameraPicLink, DeMagLevel, InspectionStartDateTime, InspectionEndDateTime, GroupID, Remarks, VisualInsResult) Values(@PartNumber, @PartSerialNumber, @HeatCode, @SupplierCode, @ManualInsResult, @CameraInsResult, @CameraPicLink, @DeMagLevel, @InspectionStartDateTime, @InspectionEndDateTime, @GroupID, @Remarks,@VisualInsResult)";
			try
			{
				SqlCommand cmd = new SqlCommand(Query, conn);
				cmd.Parameters.AddWithValue("@PartNumber", partNubmer);
				cmd.Parameters.AddWithValue("@PartSerialNumber", partSerialNumber);
				cmd.Parameters.AddWithValue("@HeatCode", heatCode);
				cmd.Parameters.AddWithValue("@SupplierCode", supplierCode);
				cmd.Parameters.AddWithValue("@ManualInsResult", manualInsResult);
				cmd.Parameters.AddWithValue("@CameraInsResult", cameraResult);
				cmd.Parameters.AddWithValue("@CameraPicLink", cameraPicLink);
				cmd.Parameters.AddWithValue("@DeMagLevel", deMagLevel);
				cmd.Parameters.AddWithValue("@InspectionStartDateTime", insStartDateTime.ToString("yyyy-MM-dd HH:mm:ss"));
				cmd.Parameters.AddWithValue("@InspectionEndDateTime", insEndDateTime.ToString("yyyy-MM-dd HH:mm:ss"));
				cmd.Parameters.AddWithValue("@GroupID", groupId);
				cmd.Parameters.AddWithValue("@Remarks", remarks);
				cmd.Parameters.AddWithValue("@VisualInsResult", visualInsp);
				cmd.CommandType = CommandType.Text;
				int RecordEffected = cmd.ExecuteNonQuery();
				if (RecordEffected > 0)
					Logger.WriteDebugLog("Record inserted into RawData_MPI successfully.");
				else
					Logger.WriteDebugLog("Could not insert data into RawData_MPI.");
			}
			catch (Exception ex)
			{
				Logger.WriteErrorLog(ex.ToString());
			}
			finally
			{
				if (conn != null) conn.Close();
			}
		}
		internal static void InsertToPhantomData(int machineId, int operationID, string component, string part_Sl_No, string heat_Code_No, string supplier_Code, int supervisorID, DateTime loadDateTime)
		{
			SqlConnection conn = ConnectionManager.GetConnection();
			SqlCommand cmd = null;
			string query = @"If not exists(select * from BarcodeScanningDetails_Phantom where MachineID=@machineid)
								begin
									Insert into BarcodeScanningDetails_Phantom (MachineID,OperationID,SupervisorID,ComponentID,PartSlNo,SupplierCode,HeatCode,LoadTS) 
									  values (@machineid,@operationid,@supervisorid,@component,@partSlNo,@suppliercode,@heatCodeNo,@loadTS)
								end
							else
								begin
									Update BarcodeScanningDetails_Phantom set OperationID=@operationid, SupervisorID=@supervisorid, ComponentID=@component, PartSlNo=@partSlNo, SupplierCode=@suppliercode, HeatCode=@heatCodeNo, LoadTS=@loadTS where MachineID=@machineid
								end";
			try
			{
				cmd = new SqlCommand(query, conn);
				cmd.CommandType = CommandType.Text;
				cmd.Parameters.AddWithValue("@machineid", machineId);
				cmd.Parameters.AddWithValue("@operationid", operationID);
				cmd.Parameters.AddWithValue("@component", component);
				cmd.Parameters.AddWithValue("@partSlNo", part_Sl_No);
				cmd.Parameters.AddWithValue("@heatCodeNo", heat_Code_No);
				cmd.Parameters.AddWithValue("@suppliercode", supplier_Code);
				cmd.Parameters.AddWithValue("@supervisorid", supervisorID);
				cmd.Parameters.AddWithValue("@loadTS", loadDateTime.ToString("yyyy-MM-dd HH:mm:ss"));

				int success = cmd.ExecuteNonQuery();

				if (success >= 0)
					Logger.WriteDebugLog("Data Inserted/Updated Successfully");
				else
					Logger.WriteErrorLog("Data Insertion Failed");
			}
			catch (Exception ex)
			{
				Logger.WriteErrorLog("Exception while Inserting Data : " + ex.Message);
			}
			finally
			{
				if (conn != null) conn.Close();
			}
		}

		internal static void UpdatePhantomMachineStatus(string machineId, int machineStatus, DateTime machineStatusTS)
		{
			SqlConnection conn = ConnectionManager.GetConnection();
			SqlCommand cmd = null;
			string query = @"if exists (select * from BarcodeScanningDetails_Phantom where MachineID=@machineid)
								begin
									Update BarcodeScanningDetails_Phantom set MachineStatus=@machinestatus, MachineStatusTS=@machinestatusTS where MachineID=@machineid
								end
							else
								begin
									Insert into BarcodeScanningDetails_Phantom (MachineID,MachineStatus,MachineStatusTS) values (@machineid,@machinestatus,@machinestatusTS)
								end";

			try
			{
				cmd = new SqlCommand(query, conn);
				cmd.Parameters.AddWithValue("@machineid",machineId);
				cmd.Parameters.AddWithValue("@machinestatus", machineStatus);
				cmd.Parameters.AddWithValue("@machinestatusTS", machineStatusTS.ToString("yyyy-MM-dd HH:mm:ss"));
				int success = cmd.ExecuteNonQuery();
				//if (success >= 0)
    //                Logger.WriteDebugLog(string.Format("Machine Status Updated Successfully For Machine ID : {0} with Status : {1} at {2}", machineId, machineStatus == 1 ? "OK" : "NOT OK", machineStatusTS.ToString("dd-MMM-yyyy HH:mm:ss")));
				//else
				//	Logger.WriteDebugLog(string.Format("Machine Status Not Updated For Machine ID : {0} with Status : {1} at {2}", machineId, machineStatus == 1 ? "OK" : "NOT OK", machineStatusTS.ToString("dd-MMM-yyyy HH:mm:ss")));
			}
            catch (Exception ex)
			{
				Logger.WriteErrorLog("Exception while Updating Machine Status : " + ex.Message);
			}
			finally
			{
				if (conn != null) conn.Close();
			}
		}

        internal static void GetDefaultValues(out int oPR, out int sUP)
        {
			oPR = sUP = 1;
			SqlConnection conn = ConnectionManager.GetConnection();
			SqlCommand cmd = null;
			SqlDataReader reader = null;
			string queries = @"Select interfaceid from employeeinformation where  Company_default=1 and EmployeeRole='Operator';Select interfaceid from employeeinformation where  Company_default=1 and EmployeeRole='Supervisor';";

			try
            {
				cmd = new SqlCommand(queries, conn);
				cmd.CommandType = CommandType.Text;
				reader = cmd.ExecuteReader();
                if (reader.Read())
                {
					int opr = 0;

					if (int.TryParse(reader["interfaceid"].ToString(), out opr))
						oPR = opr;
					else
						oPR = 1;					
				}
				reader.NextResult();
				if (reader.Read())
				{
					int sup = 0;

					if (int.TryParse(reader["interfaceid"].ToString(), out sup))
						sUP = sup;
					else
						sUP = 1;
				}
			}
			catch(Exception ex)
            {
				oPR = sUP = 1;
				Logger.WriteErrorLog("Exception in GetDefaultValues() : " + ex.Message);
            }
            finally
            {
				if (reader != null) reader.Close();
				if (conn != null) conn.Close();
            }
        }

        internal static void UpdateRawData(string machineID, string comp, int opn, int result, string remarks)
        {
			SqlConnection conn = ConnectionManager.GetConnection();
			SqlCommand cmd = null;
			string query = @"Update RawData set Result=@result, Remarks=@remarks where Mc=@machine and Comp=@comp and Opn=@opn";
            try
            {
				cmd = new SqlCommand(query, conn);
				cmd.Parameters.AddWithValue("@result", result);
				cmd.Parameters.AddWithValue("@remarks", remarks);
				cmd.Parameters.AddWithValue("@machine", machineID);
				cmd.Parameters.AddWithValue("@comp", comp);
				cmd.Parameters.AddWithValue("@opn", opn);

				int res = cmd.ExecuteNonQuery();
				if (res >= 0)
					Logger.WriteDebugLog("Raw Data Updated Successfully");
				else
					Logger.WriteDebugLog("Raw Data Updation Failed");
            }
			catch(Exception ex)
            {
				Logger.WriteErrorLog(ex.Message);
            }
            finally
            {
				if (conn != null) conn.Close();
            }
        }
		public static void GetCurrentShiftDetails(out DateTime Startdate, out string ShiftID)
		{
			Startdate = DateTime.MinValue;
			ShiftID = "0";
			SqlConnection Con = ConnectionManager.GetConnection();
			SqlCommand cmd = new SqlCommand("s_GetCurrentShiftTime", Con);  /* returns only current shift Start-End Time */
			cmd.CommandType = CommandType.StoredProcedure;
			cmd.Parameters.Add("@StartDate", SqlDbType.DateTime).Value = DateTime.Now;
			cmd.Parameters.Add("@Param", SqlDbType.NVarChar).Value = "";
			SqlDataReader DR = null;
			try
			{
				DR = cmd.ExecuteReader();
                if (DR.Read())
                {
					Startdate= Convert.ToDateTime(DR["Startdate"].ToString());
					ShiftID = DR["shiftid"].ToString();
				}
			}
			catch (Exception ex)
			{
				Logger.WriteErrorLog(ex.Message);
			}
            finally
            {
				if (DR != null) DR.Close();
				if (Con != null) Con.Close();
            }
		}
		internal static DataTable GetAlarmMasterInfo()
        {
			DataTable dtAlarm = new DataTable();
			SqlConnection conn = ConnectionManager.GetConnection();
			SqlCommand cmd = null;
			SqlDataReader reader = null;
			string query = @"select AlarmNo,Description,AlarmAddress from Focas_AlarmMaster";
            try
            {
				cmd = new SqlCommand(query, conn);
				reader = cmd.ExecuteReader();
                if (reader.HasRows)
                {
					dtAlarm.Load(reader);
                }
            }
			catch(Exception ex)
            {
				Logger.WriteErrorLog(ex.Message);
            }
            finally
            {
				if (reader != null) reader.Close();
				if (conn != null) conn.Close();
            }
			return dtAlarm;
        }

        internal static void UpdateAlarmRaisedToDB(string machineID,DataRow raised_Alarm)
        {
			SqlConnection conn = ConnectionManager.GetConnection();
			SqlCommand cmd = null;
			string query = @"insert into [Focas_AlarmHistory] ([AlarmNo], [AlarmMPos], [AlarmTime], [EndTime], [MachineID]) values(@alarmNo, @alarmMPos, @alarmTime, @endTime, @mc)";

            try
            {
				cmd = new SqlCommand(query, conn);
				cmd.Parameters.AddWithValue("@alarmNo", raised_Alarm["AlarmNo"].ToString());
				cmd.Parameters.AddWithValue("@alarmMPos", raised_Alarm["AlarmAddress"].ToString());
				cmd.Parameters.AddWithValue("@alarmTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
				cmd.Parameters.AddWithValue("@endTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
				cmd.Parameters.AddWithValue("@mc", machineID);

				int result = cmd.ExecuteNonQuery();
                if (result >= 0)
                {
					Logger.WriteDebugLog("Alarm Raised status updated successfully");
                }
			}
			catch(Exception ex)
            {
				Logger.WriteErrorLog(ex.Message);
            }
            finally
            {
				if (conn != null) conn.Close();
            }
		}
    }
}
