﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VAdvantage.Classes;
using VAdvantage.SqlExec;
using System.Data;
using System.Data.SqlClient;
using VAdvantage.Logging;
using VAdvantage.Utility;
using VAdvantage.ProcessEngine;
//using Microsoft.Office;
using System.Data.OleDb;
//using Microsoft.Office.Interop;
using System.IO;
using VAdvantage.Model;
//using Excel;
using System.ComponentModel;
//using System.Windows.Forms;
using VAdvantage.ImpExp;
using VAdvantage.DataBase;
//using System.Web.Hosting;
using ViennaAdvantage.Model;
using System.Web.Hosting;

namespace ViennaAdvantage.Process
{
    public class ImportCOA : SvrProcess
    {
        private string _message = "";
        public string msg { get { return _message; } set { _message = value; } }
        DataTable dt = new DataTable();
        string filename = "AccountingUS1.xlsx";
        int C_Elememt_ID = 0;
        string sql = "";

        protected override void Prepare()
        {
            ProcessInfoParameter[] para = GetParameter();
            for (int i = 0; i < para.Length; i++)
            {
                string name = para[i].GetParameterName();
                if (para[i].GetParameter() == null)
                {
                    ;
                }
                else if (name.Equals("VAB_Element_ID"))
                {
                    C_Elememt_ID = para[i].GetParameterAsInt();
                }
                else if (name.Equals("FileType"))
                {
                    filename = para[i].GetInfo();
                }
                else
                {
                    log.Log(Level.SEVERE, "Unknown Parameter: " + name);
                }
            }
        }

        public string CreateName(string prefix)
        {
            if (prefix == null || prefix.Length == 0)
            {
                prefix = "AcctWiz";
            }

            prefix += "_" + CommonFunctions.CurrentTimeMillis();

            return prefix;
        }

        protected override string DoIt()
        {
            string extension = filename;
            string path = HostingEnvironment.ApplicationPhysicalPath;
            if (filename.Contains("_FileCtrl"))
            {
                path = path + "TempDownload//" + filename;
                if (Directory.Exists(path))
                {
                    string[] files = Directory.GetFiles(path);
                    if (files != null && files.Length > 0)
                    {
                        filename = "//" + Path.GetFileName(files[0]);
                    }
                }
                else
                {
                    _message = Msg.GetMsg(GetCtx(), "PathNotExist");
                    return _message;
                }
            }

            int ind = filename.LastIndexOf(".");
            extension = filename.Substring(ind, filename.Length - ind);

            int client = Util.GetValueOfInt(GetVAF_Client_ID());
            int user = GetVAF_UserContact_ID();

            sql = "select VAF_TreeInfo_id from VAB_Element where VAB_Element_id = " + C_Elememt_ID + " and vaf_client_id = " + client;
            int VAF_TreeInfo_id = 0;
            MVAFTreeInfo tree = null;

            try
            {
                VAF_TreeInfo_id = Util.GetValueOfInt(DB.ExecuteScalar(sql));
                tree = new MVAFTreeInfo(GetCtx(), VAF_TreeInfo_id, null);
            }
            catch
            {
                VAF_TreeInfo_id = 0;
            }
            if (VAF_TreeInfo_id == 0)
            {
                _message = Msg.GetMsg(GetCtx(), "TreeNotBind");
                return _message;
            }

            //  if (extension.ToUpper() == ".XLSX" || extension.ToUpper() == ".XLS" || extension.ToUpper() == ".CSV")
            if (extension.ToUpper() == ".XLSX" || extension.ToUpper() == ".CSV")
            {
                try
                {
                    DataSet ds = ImportExcelXLS(path + filename, false);

                    System.Data.DataTable dt = null;
                    if (ds != null)
                    {
                        dt = ds.Tables[0];
                    }

                    if (dt != null && dt.Rows.Count > 0)
                    {

                        //if (VAF_TreeInfo_id == 0)
                        //{
                        //    int tableID = Convert.ToInt32(DB.ExecuteScalar("select vaf_tableview_id from vaf_tableview where lower(tablename)='VAB_Acct_Element'"));

                        //    tree = new MTree(GetCtx(), 0, null);

                        //    tree.SetName(CreateName("AcctWiz"));


                        //    tree.SetVAF_TableView_ID(tableID);
                        //    //tree.SetTreeType("EV");
                        //    tree.Save();
                        //    VAF_TreeInfo_id = tree.Get_ID();
                        //}
                        MVABAcctElement eleValue = null;
                        string key = "";
                        int result = 0;
                        for (int i = 0; i < dt.Rows.Count; i++)
                        {
                            key = Util.GetValueOfString(dt.Rows[i]["(Account_Value)"]);
                            if (key != "")
                            {
                                sql = " SELECT VAB_Acct_Element_id FROM VAB_Acct_Element WHERE IsActive='Y' AND VAB_ELEMENT_ID=" + C_Elememt_ID + " AND value = '" + key + "' AND vaf_client_id = " + client;
                                int VAB_Acct_Element_ID = 0;
                                try
                                {
                                    VAB_Acct_Element_ID = Util.GetValueOfInt(DB.ExecuteScalar(sql));
                                }
                                catch
                                {
                                    VAB_Acct_Element_ID = 0;
                                }

                                eleValue = new MVABAcctElement(GetCtx(), VAB_Acct_Element_ID, null);

                                string parent_ID = Util.GetValueOfString(dt.Rows[i]["(Account_Parent)"]);
                                sql = "SELECT VAB_Acct_Element_id FROM VAB_Acct_Element WHERE IsActive='Y' AND VAB_Element_ID=" + C_Elememt_ID + " AND value = '" + parent_ID + "' AND vaf_client_id = " + client;
                                int VAB_Acct_Element_ID_Parent = Util.GetValueOfInt(DB.ExecuteScalar(sql));
                                try
                                {
                                    VAB_Acct_Element_ID_Parent = Util.GetValueOfInt(DB.ExecuteScalar(sql));
                                }
                                catch
                                {
                                    VAB_Acct_Element_ID_Parent = 0;
                                }
                                //eleValue = new MVABAcctElement(GetCtx(), 0, null);
                                //int VAB_Acct_Element_ID = DB.GetNextID(GetVAF_Client_ID(), "VAB_Acct_Element", null);
                                string accSign = Util.GetValueOfString(dt.Rows[i]["(Account_Sign)"]);
                                if (accSign == "")
                                {
                                    eleValue.SetAccountSign("N");
                                }
                                else
                                {
                                    eleValue.SetAccountSign(accSign);
                                }
                                eleValue.SetVAB_Element_ID(C_Elememt_ID);
                                // eleValue.SetVAB_Acct_Element_ID(VAB_Acct_Element_ID);
                                eleValue.SetValue(Util.GetValueOfString(dt.Rows[i]["(Account_Value)"]));
                                eleValue.SetName(Util.GetValueOfString(dt.Rows[i]["(Account_Name)"]));
                                eleValue.SetDescription(Util.GetValueOfString(dt.Rows[i]["(Account_Description)"]));
                                eleValue.SetIsActive(true);
                                // For Summary
                                if (dt.Rows[i]["(Account_Summary)"].ToString().ToUpper() == "YES")
                                {
                                    eleValue.SetIsSummary(true);
                                }
                                else
                                {
                                    eleValue.SetIsSummary(false);
                                }
                                //For DefaultAccount
                                if (dt.Rows[i]["(Account_Document)"].ToString().ToUpper() == "YES")
                                {
                                    ///******************** Commented
                                    eleValue.SetIsDefault(true);
                                }
                                else
                                {
                                    ///******************** Commented
                                    eleValue.SetIsDefault(false);
                                }
                                //for MasterType
                                if (!string.IsNullOrEmpty(Util.GetValueOfString(dt.Rows[i]["(Master_Type)"])))
                                {
                                    eleValue.SetMasterAccountType(dt.Rows[i]["(Master_Type)"].ToString());
                                }

                                //For Primary Group
                                string primaryGroup = dt.Rows[i]["(Primary_Group)"].ToString();
                                if (!string.IsNullOrEmpty(primaryGroup))
                                {
                                    int primaryGroupID = Util.GetValueOfInt(DB.ExecuteScalar("select VAB_AccountGroup_id from VAB_AccountGroup where value='" + primaryGroup + "' AND vaf_client_ID=" + GetCtx().GetVAF_Client_ID()));
                                    if (primaryGroupID > 0)
                                    {
                                        eleValue.SetVAB_AccountGroup_ID(primaryGroupID);
                                    }

                                    //try
                                    //{
                                    //    eleValue.SetRef_VAB_AccountGroup_ID(Util.GetValueOfInt(primaryGroup));
                                    //}
                                    //catch { }
                                }
                                //For PrimarySub Group
                                string primarysubGroup = dt.Rows[i]["(Primary_Sub_Group)"].ToString();
                                if (!string.IsNullOrEmpty(primarysubGroup))
                                {
                                    int primarysubGroupID = Util.GetValueOfInt(DB.ExecuteScalar("select VAB_AccountSubGroup_id from VAB_AccountSubGroup where value='" + primarysubGroup + "' AND vaf_client_ID=" + GetCtx().GetVAF_Client_ID()));
                                    if (primarysubGroupID > 0)
                                    {
                                        eleValue.SetVAB_AccountSubGroup_ID(primarysubGroupID);
                                    }

                                    //eleValue.SetRef_VAB_AccountSubGroup_ID(Util.GetValueOfInt(primarysubGroup));
                                    //try
                                    //{
                                    //    eleValue.SetRef_VAB_AccountSubGroup_ID(Util.GetValueOfInt(primarysubGroup));
                                    //}
                                    //catch { }

                                }


                                // For Account Type                   
                                if (Util.GetValueOfString(dt.Rows[i]["(Account_Type)"]).ToUpper() == "ASSET")
                                {
                                    eleValue.SetAccountType("A");
                                }
                                else if (Util.GetValueOfString(dt.Rows[i]["(Account_Type)"]).ToUpper() == "LIABILITY")
                                {
                                    eleValue.SetAccountType("L");
                                }
                                else if (Util.GetValueOfString(dt.Rows[i]["(Account_Type)"]).ToUpper() == "OWNER'S EQUITY")
                                {
                                    eleValue.SetAccountType("O");
                                }
                                else if (Util.GetValueOfString(dt.Rows[i]["(Account_Type)"]).ToUpper() == "REVENUE")
                                {
                                    eleValue.SetAccountType("R");
                                }
                                else if (Util.GetValueOfString(dt.Rows[i]["(Account_Type)"]).ToUpper() == "EXPENSE")
                                {
                                    eleValue.SetAccountType("E");
                                }
                                else
                                {
                                    eleValue.SetAccountType("M");
                                }

                                // Document controlled
                                if (dt.Columns.Contains("(Document_Controlled)") && dt.Rows[i]["(Document_Controlled)"] != DBNull.Value)
                                {
                                    if (dt.Rows[i]["(Document_Controlled)"].ToString().ToUpper() == "YES")
                                    {
                                        eleValue.SetIsDocControlled(true);
                                    }
                                    else
                                    {
                                        eleValue.SetIsDocControlled(false);
                                    }
                                }

                                // Bank Account
                                if (dt.Columns.Contains("(Bank_Account)") && dt.Rows[i]["(Bank_Account)"] != DBNull.Value)
                                {
                                    if (dt.Rows[i]["(Bank_Account)"].ToString().ToUpper() == "YES")
                                    {
                                        eleValue.SetIsBankAccount(true);
                                    }
                                    else
                                    {
                                        eleValue.SetIsBankAccount(false);
                                    }
                                }

                                // when bank account true, then only set Foreign Currency Account
                                if (eleValue.IsBankAccount() && dt.Columns.Contains("(Foreign_Currency_Account)") && dt.Rows[i]["(Foreign_Currency_Account)"] != DBNull.Value)
                                {
                                    if (dt.Rows[i]["(Foreign_Currency_Account)"].ToString().ToUpper() == "YES")
                                    {
                                        eleValue.SetIsForeignCurrency(true);
                                    }
                                    else
                                    {
                                        eleValue.SetIsForeignCurrency(false);
                                    }
                                }

                                // when Foreign Currency true, then only set Currency
                                if (eleValue.IsForeignCurrency() && eleValue.Get_ColumnIndex("VAB_Currency_ID") >= 0)
                                {
                                    sql = "SELECT VAB_Currency_ID FROM VAB_Currency WHERE IsActive='Y' AND ISO_CODE='"
                                        + Util.GetValueOfString(dt.Rows[i]["(Currency)"]) + "'";
                                    result = Util.GetValueOfInt(DB.ExecuteScalar(sql));
                                    if (result > 0)
                                    {
                                        eleValue.Set_Value("VAB_Currency_ID", result);
                                    }
                                }

                                // Intermediate_Code
                                if (eleValue.Get_ColumnIndex("IsInterMediateCode") >= 0 &&
                                    dt.Columns.Contains("(Intermediate_Code)") && dt.Rows[i]["(Intermediate_Code)"] != DBNull.Value)
                                {
                                    if (dt.Rows[i]["(Intermediate_Code)"].ToString().ToUpper() == "YES")
                                    {
                                        eleValue.Set_Value("IsInterMediateCode", true);
                                    }
                                    else
                                    {
                                        eleValue.Set_Value("IsInterMediateCode", false);
                                    }
                                }

                                // Allocation_Related
                                if (eleValue.Get_ColumnIndex("IsAllocationRelated") >= 0 &&
                                    dt.Columns.Contains("(Allocation_Related)") && dt.Rows[i]["(Allocation_Related)"] != DBNull.Value)
                                {
                                    if (dt.Rows[i]["(Allocation_Related)"].ToString().ToUpper() == "YES")
                                    {
                                        eleValue.Set_Value("IsAllocationRelated", true);
                                    }
                                    else
                                    {
                                        eleValue.Set_Value("IsAllocationRelated", false);
                                    }
                                }

                                // Has Group
                                if (eleValue.Get_ColumnIndex("HasGroup") >= 0 &&
                                    dt.Columns.Contains("(Has_Group)") && dt.Rows[i]["(Has_Group)"] != DBNull.Value)
                                {
                                    if (dt.Rows[i]["(Has_Group)"].ToString().ToUpper() == "YES")
                                    {
                                        eleValue.Set_Value("HasGroup", true);
                                    }
                                    else
                                    {
                                        eleValue.Set_Value("HasGroup", false);
                                    }
                                }

                                // Conversion Type
                                if (eleValue.Get_ColumnIndex("VAB_CurrencyType_ID") >= 0)
                                {
                                    sql = "SELECT VAB_CurrencyType_ID FROM VAB_CurrencyType WHERE IsActive='Y' AND Value='"
                                        + Util.GetValueOfString(dt.Rows[i]["(Currency_Type)"]) + "'";
                                    result = Util.GetValueOfInt(DB.ExecuteScalar(sql));
                                    if (result > 0)
                                    {
                                        eleValue.Set_Value("VAB_CurrencyType_ID", result);
                                    }
                                }

                                //string memo = dt.Rows[i]["(Memo_Ledger)"].ToString();
                                //if (dt.Rows[i]["(Memo_Ledger)"] != null && dt.Rows[i]["(Memo_Ledger)"] != DBNull.Value)
                                //{
                                //    try
                                //    {
                                //        eleValue.SetRef_VAB_Acct_Element_ID(Util.GetValueOfInt(memo));
                                //    }
                                //    catch { }
                                //}
                                //eleValue.SetParent_ID(VAB_Acct_Element_ID_Parent);
                                if (!string.IsNullOrEmpty(parent_ID))
                                {
                                    ///******************** Commented
                                    //eleValue.SetParentSerachKey(parent_ID.ToString());
                                }



                                if (!eleValue.Save())
                                {
                                    log.SaveError("NotSaved", "");

                                    //return msg;
                                }
                                VAdvantage.Model.MVAFTreeInfo obj = new VAdvantage.Model.MVAFTreeInfo(GetCtx(), VAF_TreeInfo_id, null);
                                //VAB_Acct_Element_ID = VAB_Acct_Element_ID + 1;                                   
                                VAdvantage.Model.MVAFTreeInfoChild mNode = VAdvantage.Model.MVAFTreeInfoChild.Get(obj, eleValue.Get_ID());
                                if (mNode == null)
                                {
                                    mNode = new VAdvantage.Model.MVAFTreeInfoChild(tree, eleValue.Get_ID());
                                }
                                mNode.SetParent_ID(VAB_Acct_Element_ID_Parent);
                                // ((PO)mNode).Set_Value("Parent_ID", VAB_Acct_Element_ID_Parent);
                                if (!mNode.Save())
                                {
                                    log.SaveError("NodeNotSaved", "");
                                    return msg;
                                }

                            }
                            else
                            {
                            }
                        }
                        /////////Set Memo Ledger
                        //int tempElementID = 0;
                        //for (int i = 0; i < dt.Rows.Count; i++)
                        //{
                        //    if (dt.Rows[i]["(Memo_Ledger)"] != null && dt.Rows[i]["(Memo_Ledger)"] != DBNull.Value)
                        //    {
                        //        if (!(string.IsNullOrEmpty(dt.Rows[i]["(Memo_Ledger)"].ToString())))
                        //        {
                        //            refElementValID = Util.GetValueOfInt(DB.ExecuteScalar("Select VAB_Acct_Element_ID from VAB_Acct_Element WHERE Value='" + dt.Rows[i]["(Memo_Ledger)"] + "'"));
                        //            if (refElementValID > 0)
                        //            {
                        //                tempElementID = Util.GetValueOfInt(DB.ExecuteScalar("Select VAB_Acct_Element_ID from VAB_Acct_Element WHERE Value='" + dt.Rows[i]["(Account_Value)"] + "'"));
                        //                eleValue = new MVABAcctElement(GetCtx(), tempElementID, null);
                        //                eleValue.SetRef_VAB_Acct_Element_ID(refElementValID);
                        //                eleValue.Save();
                        //            }
                        //        }
                        //    }
                        //}

                        //******************** Commented
                        //if (tree != null)
                        //{
                        //    sql = "Update VAB_Element SET TreeID=" + tree.Get_ID() + " WHERE VAB_Element_ID=" + C_Elememt_ID;
                        //    DB.ExecuteQuery(sql);
                        //}

                        if (path.Contains("_FileCtrl"))
                        {
                            Directory.Delete(path, true);
                        }
                    }

                    msg = Msg.GetMsg(GetCtx(), "ImportedSuccessfully");
                    return msg;
                }
                catch
                {
                    if (_message != "")
                    {
                        msg = _message;
                    }
                    else
                    {
                        msg = Msg.GetMsg(GetCtx(), "ExcelSheetNotInProperFormat");
                    }
                    return msg;
                }
            }
            else
            {
                msg = Msg.GetMsg(GetCtx(), "UseDefaultExcelSheet");
                return msg;
            }
        }

        public DataSet ImportExcelXLS(string FileName, bool hasHeaders)
        {

            string HDR = hasHeaders ? "Yes" : "No";
            string strConn;
            var connString = "";

            if (FileName.Substring(FileName.LastIndexOf('.')).ToLower() == ".xlsx")
            {
                strConn = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + FileName + ";Extended Properties=\"Excel 12.0;HDR=" + HDR + ";IMEX=0\"";
            }
            else
            {
                connString = string.Format(
    @"Provider=Microsoft.Jet.OleDb.4.0; Data Source={0};Extended Properties=""Text;HDR=YES;FMT=Delimited""",
    Path.GetDirectoryName(FileName));
                strConn = "";
            }

            DataSet output = new DataSet();

            try
            {
                if (FileName.Substring(FileName.LastIndexOf('.')).ToLower() == ".xlsx")
                {
                    using (OleDbConnection conn = new OleDbConnection(strConn))
                    {
                        conn.Open();

                        DataTable schemaTable = conn.GetOleDbSchemaTable(
                            OleDbSchemaGuid.Tables, new object[] { null, null, null, "TABLE" });

                        foreach (DataRow schemaRow in schemaTable.Rows)
                        {
                            string sheet = schemaRow["TABLE_NAME"].ToString();

                            if (!sheet.EndsWith("_"))
                            {
                                try
                                {
                                    OleDbCommand cmd = new OleDbCommand("SELECT * FROM [" + sheet + "]", conn);
                                    cmd.CommandType = CommandType.Text;

                                    DataTable outputTable = new DataTable(sheet);
                                    output.Tables.Add(outputTable);
                                    new OleDbDataAdapter(cmd).Fill(outputTable);
                                }
                                catch
                                {
                                    return null;
                                }
                            }
                        }
                    }
                }
                else
                {
                    using (OleDbConnection conn = new OleDbConnection(connString))
                    {
                        conn.Open();
                        DataTable schemaTable = conn.GetOleDbSchemaTable(
                            OleDbSchemaGuid.Tables, new object[] { null, null, null, "TABLE" });

                        foreach (DataRow schemaRow in schemaTable.Rows)
                        {
                            string sheet = schemaRow["TABLE_NAME"].ToString();

                            if (!sheet.EndsWith("_"))
                            {
                                try
                                {
                                    OleDbCommand cmd = new OleDbCommand("SELECT * FROM [" + sheet + "]", conn);
                                    cmd.CommandType = CommandType.Text;
                                    DataTable outputTable = new DataTable(sheet);
                                    output.Tables.Add(outputTable);
                                    new OleDbDataAdapter(cmd).Fill(outputTable);
                                }
                                catch
                                {

                                    return null;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _message = ex.Message;
                return output;
            }
            return output;
        }
    }
}
