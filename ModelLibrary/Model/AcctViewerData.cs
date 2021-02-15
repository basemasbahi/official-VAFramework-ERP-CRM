﻿/********************************************************
 * Project Name   : VAdvantage
 * Class Name     : AcctViewerData
 * Purpose        : Account Viewer State - maintaines State information for the Account Viewer
 * Class Used     : None
 * Chronological    Development
 * Raghunandan     18-Dec-2009
  ******************************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VAdvantage.Classes;
using VAdvantage.Common;
using VAdvantage.Process;
//////using System.Windows.Forms;
using VAdvantage.Model;
using VAdvantage.DataBase;
using VAdvantage.SqlExec;
using VAdvantage.Utility;
using System.Data;
using VAdvantage.Logging;
using VAdvantage.Login;

namespace VAdvantage.Model
{
    public class AcctViewerData
    {
        #region Private PrivateVariable
        //Window              
        public int windowNo;
        //Client				
        public int VAF_Client_ID;
        //All Acct Schema		
        public MVABAccountBook[] ASchemas = null;
        //This Acct Schema	
        public MVABAccountBook ASchema = null;

        //  Selection Info
        //Document Query		
        public bool documentQuery = false;
        // Acct Schema			
        public int VAB_AccountBook_ID = 0;
        //Posting Type		
        public String PostingType = "";
        // Organization		
        public int VAF_Org_ID = 0;
        // Date From		
        public DateTime? DateFrom = null;
        // Date To			
        public DateTime? DateTo = null;

        //  Dodument Table Selection Info
        // Table ID			
        public int VAF_TableView_ID;
        // Record			
        public int Record_ID;

        //Containing Column and Query
        //public HashMap<String,String>	whereInfo = new HashMap<String,String>();
        public Dictionary<String, String> whereInfo = new Dictionary<String, String>();
        //Containing TableName and VAF_TableView_ID    
        //public HashMap<String,Integer>	tableInfo = new HashMap<String,Integer>();
        public Dictionary<String, int> tableInfo = new Dictionary<String, int>();

        //  Display Info
        //Display Qty			
        public bool displayQty = false;
        //Display Source Surrency
        public bool displaySourceAmt = false;
        //Display Document info	
        public bool displayDocumentInfo = false;
        //
        public String sortBy1 = "";
        public String sortBy2 = "";
        public String sortBy3 = "";
        public String sortBy4 = "";
        //
        public bool group1 = false;
        public bool group2 = false;
        public bool group3 = false;
        public bool group4 = false;

        // Leasing Columns		
        private int _leadingColumns = 0;
        //UserElement1 Reference	
        private String _ref1 = null;
        //UserElement2 Reference	
        private String _ref2 = null;
        private Ctx _ctx = null;
        //Logger	
        private static VLogger log = VLogger.GetVLogger(typeof(AcctViewerData).FullName);
        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="windowNo"></param>
        /// <param name="vaf_client_ID"></param>
        /// <param name="vaf_tableview_ID"></param>
        public AcctViewerData(Ctx ctx, int windowNo1, int vaf_client_ID, int vaf_tableview_ID)
        {
            windowNo = windowNo1;
            VAF_Client_ID = vaf_client_ID;
            if (VAF_Client_ID == 0)
            {
                VAF_Client_ID = ctx.GetContextAsInt(windowNo, "VAF_Client_ID");
            }
            if (VAF_Client_ID == 0)
            {
                VAF_Client_ID = ctx.GetContextAsInt("VAF_Client_ID");
            }
            VAF_TableView_ID = vaf_tableview_ID;
            //
            ASchemas = MVABAccountBook.GetClientAcctSchema(ctx, VAF_Client_ID);
            ASchema = ASchemas[0];
            _ctx = ctx;
        }


        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            ASchemas = null;
            ASchema = null;
            //
            whereInfo.Clear();
            whereInfo = null;
            //
            Env.ClearWinContext(windowNo);
        }

        /// <summary>
        /// Get Accounting Schema
        /// </summary>
        /// <returns>the accounting schema</returns>
        public ListBoxVO GetAcctSchema()
        {
            List<NamePair> options = new List<NamePair>();
            for (int i = 0; i < ASchemas.Length; i++)
            {
                options.Add(new KeyNamePair(ASchemas[i].GetVAB_AccountBook_ID(), ASchemas[i].GetName()));
            }
            //return new ListBoxVO(options, null);
            return new ListBoxVO(options, null);
        }

        /// <summary>
        /// Get Posting Type
        /// </summary>
        /// <returns></returns>
        public ListBoxVO GetPostingType()
        {
            int VAF_Control_Ref_ID = 125;
            List<NamePair> options = new List<NamePair>(MVAFCtrlRefList.GetList(VAF_Control_Ref_ID, true));
            return new ListBoxVO(options, null);
        }


        /// <summary>
        /// Get Table with ValueNamePair (TableName, translatedKeyColumnName) and
        /// tableInfo with (TableName, VAF_TableView_ID) and select the entry for VAF_TableView_ID
        /// </summary>
        /// <returns></returns>
        public ListBoxVO GetTable()
        {
            List<NamePair> options = new List<NamePair>();
            String defaultKey = null;
            //
            String sql = "SELECT VAF_TableView_ID, TableName FROM VAF_TableView t "
                + "WHERE EXISTS (SELECT * FROM VAF_Column c"
                + " WHERE t.VAF_TableView_ID=c.VAF_TableView_ID AND c.ColumnName='Posted')"
                + " AND IsView='N'";
            IDataReader idr = null;
            try
            {
                idr = DataBase.DB.ExecuteReader(sql, null, null);
                while (idr.Read())
                {
                    int id = Utility.Util.GetValueOfInt(idr[0]);//.getInt(1);
                    String tableName = Utility.Util.GetValueOfString(idr[1]);//.getString(2);
                    String name = Msg.Translate(_ctx, tableName + "_ID");
                    ValueNamePair pp = new ValueNamePair(tableName, name);
                    options.Add(pp);
                    //tableInfo.put(tableName, new Integer(id));
                    tableInfo.Add(tableName.ToString(), id);
                    if (id == VAF_TableView_ID)
                    {
                        defaultKey = pp.GetValue();
                    }
                }
                idr.Close();
            }
            catch (Exception e)
            {
                if (idr != null)
                {
                    idr.Close();
                    idr = null;
                }
                log.Log(Level.SEVERE, sql, e);
            }

            return new ListBoxVO(options, defaultKey);
        }

        /// <summary>
        /// Get Org
        ///cb JComboBox to be filled
        /// </summary>
        /// <returns></returns>
        public ListBoxVO GetOrg()
        {
            List<NamePair> options = new List<NamePair>();
            KeyNamePair pp = new KeyNamePair(0, "");
            options.Add(pp);
            String sql = "SELECT VAF_Org_ID, Name FROM VAF_Org WHERE VAF_Client_ID=" + VAF_Client_ID + " ORDER BY Value";
            IDataReader idr = null;
            try
            {
                idr = DataBase.DB.ExecuteReader(sql, null, null);
                while (idr.Read())
                {
                    options.Add(new KeyNamePair(Utility.Util.GetValueOfInt(idr[0]), Utility.Util.GetValueOfString(idr[1])));
                }
                idr.Close();
            }
            catch (Exception e)
            {
                log.Log(Level.SEVERE, sql, e);
            }
            return new ListBoxVO(options, null);
        }

        /// <summary>
        /// Get Button Text
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="columnName"></param>
        /// <param name="selectSQL"></param>
        /// <returns>Text on button</returns>
        public String GetButtonText(String tableName, String columnName, String selectSQL)
        {
            //  SELECT (<embedded>) FROM tableName avd WHERE avd.<selectSQL>
            StringBuilder sql = new StringBuilder("SELECT (");
            Language language = Env.GetLanguage(_ctx);
            sql.Append(VLookUpFactory.GetLookup_TableDirEmbed(language, columnName, "avd"))
                .Append(") FROM ").Append(tableName).Append(" avd WHERE avd.").Append(selectSQL);
            String retValue = "<" + selectSQL + ">";
            IDataReader idr = null;
            try
            {
                idr = DataBase.DB.ExecuteReader(sql.ToString(), null, null);
                if (idr.Read())
                {
                    retValue = Utility.Util.GetValueOfString(idr[0]);
                }
                idr.Close();
            }
            catch (Exception e)
            {
                log.Log(Level.SEVERE, sql.ToString(), e);
            }
            return retValue;
        }

        /// <summary>
        /// Create Query and submit
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns>Report Model</returns>
        public RModel Query(Ctx ctx)
        {
            //  Set Where Clause
            StringBuilder whereClause = new StringBuilder();
            //  Add Organization
            if (VAB_AccountBook_ID != 0)
            {
                whereClause.Append(RModel.TABLE_ALIAS)
                    .Append(".VAB_AccountBook_ID=").Append(VAB_AccountBook_ID);
            }

            //	Posting Type Selected
            if (PostingType != null && PostingType.Length > 0)
            {
                if (whereClause.Length > 0)
                {
                    whereClause.Append(" AND ");
                }
                whereClause.Append(RModel.TABLE_ALIAS)
                    .Append(".PostingType='").Append(PostingType).Append("'");
            }

            //
            if (documentQuery)
            {
                if (whereClause.Length > 0)
                {
                    whereClause.Append(" AND ");
                }
                whereClause.Append(RModel.TABLE_ALIAS).Append(".VAF_TableView_ID=").Append(VAF_TableView_ID)
                    .Append(" AND ").Append(RModel.TABLE_ALIAS).Append(".Record_ID=").Append(Record_ID);
            }
            else
            {
                //  get values (Queries)
                //Iterator<String> it = whereInfo.values().iterator();
                IEnumerator<String> it = whereInfo.Values.GetEnumerator();

                while (it.MoveNext())
                {
                    String where = (String)it.Current;
                    if (where != null && where.Length > 0)    //  add only if not empty
                    {
                        if (whereClause.Length > 0)
                        {
                            whereClause.Append(" AND ");
                        }
                        whereClause.Append(RModel.TABLE_ALIAS).Append(".").Append(where);
                    }
                }
                if (DateFrom != null || DateTo != null)
                {
                    if (whereClause.Length > 0)
                    {
                        whereClause.Append(" AND ");
                    }
                    if (DateFrom != null && DateTo != null)
                    {
                        whereClause.Append("TRUNC(").Append(RModel.TABLE_ALIAS).Append(".DateAcct,'DD') BETWEEN ")
                            .Append(DataBase.DB.TO_DATE(DateFrom)).Append(" AND ").Append(DataBase.DB.TO_DATE(DateTo));
                    }
                    else if (DateFrom != null)
                    {
                        whereClause.Append("TRUNC(").Append(RModel.TABLE_ALIAS).Append(".DateAcct,'DD') >= ")
                            .Append(DataBase.DB.TO_DATE(DateFrom));
                    }
                    else    //  DateTo != null
                    {
                        whereClause.Append("TRUNC(").Append(RModel.TABLE_ALIAS).Append(".DateAcct,'DD') <= ")
                            .Append(DataBase.DB.TO_DATE(DateTo));
                    }
                }
                //  Add Organization
                if (VAF_Org_ID != 0)
                {
                    if (whereClause.Length > 0)
                    {
                        whereClause.Append(" AND ");
                    }
                    whereClause.Append(RModel.TABLE_ALIAS).Append(".VAF_Org_ID=").Append(VAF_Org_ID);
                }
            }

            //  Set Order By Clause
            StringBuilder orderClause = new StringBuilder();
            if (sortBy1.Length > 0)
            {
                orderClause.Append(RModel.TABLE_ALIAS).Append(".").Append(sortBy1);
            }
            if (sortBy2.Length > 0)
            {
                if (orderClause.Length > 0)
                {
                    orderClause.Append(",");
                }
                orderClause.Append(RModel.TABLE_ALIAS).Append(".").Append(sortBy2);
            }
            if (sortBy3.Length > 0)
            {
                if (orderClause.Length > 0)
                {
                    orderClause.Append(",");
                }
                orderClause.Append(RModel.TABLE_ALIAS).Append(".").Append(sortBy3);
            }
            if (sortBy4.Length > 0)
            {
                if (orderClause.Length > 0)
                {
                    orderClause.Append(",");
                }
                orderClause.Append(RModel.TABLE_ALIAS).Append(".").Append(sortBy4);
            }
            if (orderClause.Length == 0)
            {
                orderClause.Append(RModel.TABLE_ALIAS).Append(".Actual_Acct_Detail_ID");
            }
            //get grid view columns
            RModel rm = GetRModel(ctx);

            //  Groups
            if (group1 && sortBy1.Length > 0)
            {
                rm.SetGroup(sortBy1);
            }
            if (group2 && sortBy2.Length > 0)
            {
                rm.SetGroup(sortBy2);
            }
            if (group3 && sortBy3.Length > 0)
            {
                rm.SetGroup(sortBy3);
            }
            if (group4 && sortBy4.Length > 0)
            {
                rm.SetGroup(sortBy4);
            }

            //  Totals
            rm.SetFunction("AmtAcctDr", RModel.FUNCTION_SUM);
            rm.SetFunction("AmtAcctCr", RModel.FUNCTION_SUM);

            rm.Query(ctx, whereClause.ToString(), orderClause.ToString());

            return rm;
        }

        /// <summary>
        /// Create Report Model (Columns)
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns>Report Model</returns>
        public RModel GetRModel(Ctx ctx)
        {
            RModel rm = new RModel("Actual_Acct_Detail");
            //  Add Key (Lookups)
            List<String> keys = CreateKeyColumns();
            int max = _leadingColumns;
            if (max == 0)
            {
                max = keys.Count;
            }
            for (int i = 0; i < max; i++)
            {
                String column = (String)keys[i];
                if (column != null && column.StartsWith("Date"))
                {
                    rm.AddColumn(new RColumn(ctx, column, DisplayType.Date));
                }
                else if (column != null && column.EndsWith("_ID"))
                {
                    rm.AddColumn(new RColumn(ctx, column, DisplayType.TableDir));
                }
            }
            //  Main Info
            rm.AddColumn(new RColumn(ctx, "AmtAcctDr", DisplayType.Amount));
            rm.AddColumn(new RColumn(ctx, "AmtAcctCr", DisplayType.Amount));
            if (displaySourceAmt)
            {
                if (!keys.Contains("DateTrx"))
                {
                    rm.AddColumn(new RColumn(ctx, "DateTrx", DisplayType.Date));
                }
                rm.AddColumn(new RColumn(ctx, "VAB_Currency_ID", DisplayType.TableDir));
                rm.AddColumn(new RColumn(ctx, "AmtSourceDr", DisplayType.Amount));
                rm.AddColumn(new RColumn(ctx, "AmtSourceCr", DisplayType.Amount));
                rm.AddColumn(new RColumn(ctx, "Rate", DisplayType.Amount,
                    "CASE WHEN (AmtSourceDr + AmtSourceCr) = 0 THEN 0"
                    + " ELSE (AmtAcctDr + AmtAcctCr) / (AmtSourceDr + AmtSourceCr) END"));
            }
            //	Remaining Keys
            for (int i = max; i < keys.Count; i++)
            {
                String column = (String)keys[i];
                if (column != null && column.StartsWith("Date"))
                {
                    rm.AddColumn(new RColumn(ctx, column, DisplayType.Date));
                }
                else if (column.StartsWith("UserElement"))
                {
                    if (column.IndexOf("1") != -1)
                    {
                        rm.AddColumn(new RColumn(ctx, column, DisplayType.TableDir, null, 0, _ref1));
                    }
                    else
                    {
                        rm.AddColumn(new RColumn(ctx, column, DisplayType.TableDir, null, 0, _ref2));
                    }
                }
                else if (column != null && column.EndsWith("_ID"))
                {
                    rm.AddColumn(new RColumn(ctx, column, DisplayType.TableDir));
                }
            }
            //	Info
            if (!keys.Contains("DateAcct"))
            {
                rm.AddColumn(new RColumn(ctx, "DateAcct", DisplayType.Date));
            }
            if (!keys.Contains("VAB_YearPeriod_ID"))
            {
                rm.AddColumn(new RColumn(ctx, "VAB_YearPeriod_ID", DisplayType.TableDir));
            }
            if (displayQty)
            {
                rm.AddColumn(new RColumn(ctx, "VAB_UOM_ID", DisplayType.TableDir));
                rm.AddColumn(new RColumn(ctx, "Qty", DisplayType.Quantity));
            }
            if (displayDocumentInfo)
            {
                rm.AddColumn(new RColumn(ctx, "VAF_TableView_ID", DisplayType.TableDir));
                rm.AddColumn(new RColumn(ctx, "Record_ID", DisplayType.ID));
                rm.AddColumn(new RColumn(ctx, "Description", DisplayType.String));
            }
            if (PostingType == null || PostingType.Length == 0)
            {
                rm.AddColumn(new RColumn(ctx, "PostingType", DisplayType.List,
                    MActualAcctDetail.POSTINGTYPE_VAF_Control_Ref_ID));
            }
            return rm;
        }

        /// <summary>
        /// Create the key columns in sequence
        /// </summary>
        /// <returns>List of Key Columns</returns>
        private List<String> CreateKeyColumns()
        {
            List<String> columns = new List<String>();
            _leadingColumns = 0;
            //  Sorting Fields
            columns.Add(sortBy1);               //  may add ""
            if (!columns.Contains(sortBy2))
            {
                columns.Add(sortBy2);
            }
            if (!columns.Contains(sortBy3))
            {
                columns.Add(sortBy3);
            }
            if (!columns.Contains(sortBy4))
            {
                columns.Add(sortBy4);
            }

            //  Add Account Segments
            MVABAccountBookElement[] elements = ASchema.GetAcctSchemaElements();
            for (int i = 0; i < elements.Length; i++)
            {
                if (_leadingColumns == 0 && columns.Contains("VAF_Org_ID") && columns.Contains("Account_ID"))
                {
                    _leadingColumns = columns.Count;
                }
                //
                MVABAccountBookElement ase = elements[i];
                String columnName = ase.GetColumnName();
                if (columnName.StartsWith("UserElement"))
                {
                    if (columnName.IndexOf("1") != -1)
                    {
                        _ref1 = ase.GetDisplayColumnName();
                    }
                    else
                    {
                        _ref2 = ase.GetDisplayColumnName();
                    }
                }
                if (!columns.Contains(columnName))
                {
                    columns.Add(columnName);

                }
            }
            if (_leadingColumns == 0 && columns.Contains("VAF_Org_ID") && columns.Contains("Account_ID"))
            {
                _leadingColumns = columns.Count;
            }
            return columns;
        }
    }
}
