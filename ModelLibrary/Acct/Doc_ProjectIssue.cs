﻿/********************************************************
 * Project Name   : VAdvantage
 * Class Name     : Doc_ProjectIssue
 * Purpose        : Project Issue.
 *	                Note:
 *		            Will load the default GL Category. 
 *		            Set up a document type to set the GL Category. 
 * Class Used     : Doc
 * * Chronological    Development
 * Raghunandan      21-Jan-2010
  ******************************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VAdvantage.Classes;
using VAdvantage.Common;
using VAdvantage.Process;
////using System.Windows.Forms;
using VAdvantage.Model;
using VAdvantage.DataBase;
using VAdvantage.SqlExec;
using VAdvantage.Utility;
using System.Data;
using VAdvantage.Logging;
using System.Data.SqlClient;
using VAdvantage.Acct;

namespace VAdvantage.Acct
{
    public class Doc_ProjectIssue : Doc
    {
        //	Pseudo Line							
        private DocLine _line = null;
        // Issue									
        private MVABProjectSupply _issue = null;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="ass"></param>
        /// <param name="idr"></param>
        /// <param name="trxName"></param>
        public Doc_ProjectIssue(MVABAccountBook[] ass, IDataReader idr, Trx trxName)
            : base(ass, typeof(MVABProjectSupply), idr, MVABMasterDocType.DOCBASETYPE_PROJECTISSUE, trxName)
        {

        }
        public Doc_ProjectIssue(MVABAccountBook[] ass,DataRow dr, Trx trxName)
            : base(ass, typeof(MVABProjectSupply), dr, MVABMasterDocType.DOCBASETYPE_PROJECTISSUE, trxName)
        {

        }

        /// <summary>
        /// Load Document Details
        /// </summary>
        /// <returns>error message or null</returns>
        public override String LoadDocumentDetails()
        {
            SetVAB_Currency_ID(NO_CURRENCY);
            _issue = (MVABProjectSupply)GetPO();
            SetDateDoc(_issue.GetMovementDate());
            SetDateAcct(_issue.GetMovementDate());

            //	Pseudo Line
            _line = new DocLine(_issue, this);
            _line.SetQty(_issue.GetMovementQty(), true);    //  sets Trx and Storage Qty

            //	Pseudo Line Check
            if (_line.GetVAM_Product_ID() == 0)
            {
                log.Warning(_line.ToString() + " - No Product");
            }
            log.Fine(_line.ToString());
            return null;
        }

        /// <summary>
        /// Get DocumentNo
        /// </summary>
        /// <returns>document no</returns>
        public new String GetDocumentNo()
        {
            MVABProject p = _issue.GetParent();
            if (p != null)
            {
                return p.GetValue() + " #" + _issue.GetLine();
            }
            return "(" + _issue.Get_ID() + ")";
        }

        /// <summary>
        ///  Get Balance
        /// </summary>
        /// <returns>Zero (always balanced)</returns>
        public override Decimal GetBalance()
        {
            Decimal retValue = Env.ZERO;
            return retValue;
        }

        /// <summary>
        /// Create Facts (the accounting logic) for
        ///  PJI
        ///  <pre>
        ///  Issue
        ///      ProjectWIP      DR
        ///      Inventory               CR
        ///  </pre>
        ///  Project Account is either Asset or WIP depending on Project Type
        /// </summary>
        /// <param name="?"></param>
        /// <returns>fact</returns>
        public override List<Fact> CreateFacts(MVABAccountBook as1)
        {
            //  create Fact Header
            Fact fact = new Fact(this, as1, Fact.POST_Actual);
            SetVAB_Currency_ID(as1.GetVAB_Currency_ID());

            MVABProject project = new MVABProject(GetCtx(), _issue.GetVAB_Project_ID(), null);
            String ProjectCategory = project.GetProjectCategory();
            MVAMProduct product = MVAMProduct.Get(GetCtx(), _issue.GetVAM_Product_ID());

            //  Line pointers
            FactLine dr = null;
            FactLine cr = null;

            //  Issue Cost
            Decimal? cost = null;
            if (_issue.GetVAM_Inv_InOutLine_ID() != 0)
            {
                cost = GetPOCost(as1);
            }
            else if (_issue.GetVAS_ExpenseReportLine_ID() != 0)
            {
                cost = GetLaborCost(as1);
            }
            if (cost == null)	//	standard Product Costs
                cost = _line.GetProductCosts(as1, GetVAF_Org_ID(), false);

            //  Project         DR
            int acctType = ACCTTYPE_ProjectWIP;
            if (MVABProject.PROJECTCATEGORY_AssetProject.Equals(ProjectCategory))
            {
                acctType = ACCTTYPE_ProjectAsset;
            }
            dr = fact.CreateLine(_line,
                GetAccount(acctType, as1), as1.GetVAB_Currency_ID(), cost, null);
            dr.SetQty((Decimal?)Decimal.Negate(Utility.Util.GetValueOfDecimal(_line.GetQty())));

            //  Inventory               CR
            acctType = ProductCost.ACCTTYPE_P_Asset;
            if (product.IsService())
            {
                acctType = ProductCost.ACCTTYPE_P_Expense;
            }
            cr = fact.CreateLine(_line,
                _line.GetAccount(acctType, as1),
                as1.GetVAB_Currency_ID(), null, cost);
            cr.SetVAM_Locator_ID(_line.GetVAM_Locator_ID());
            cr.SetLocationFroMVAMLocator(_line.GetVAM_Locator_ID(), true);	// from Loc
            //
            List<Fact> facts = new List<Fact>();
            facts.Add(fact);
            return facts;
        }

        /// <summary>
        /// Get PO Costs in Currency of AcctSchema
        /// </summary>
        /// <param name="as1"></param>
        /// <returns>Unit PO Cost</returns>
        private Decimal? GetPOCost(MVABAccountBook as1)
        {
            Decimal? retValue = null;
            //	Uses PO Date
            String sql = "SELECT currencyConvert(ol.PriceActual, o.VAB_Currency_ID, @param1, o.DateOrdered, o.VAB_CurrencyType_ID, @param2, @param3) "
                + "FROM VAB_OrderLine ol"
                + " INNER JOIN VAM_Inv_InOutLine iol ON (iol.VAB_OrderLine_ID=ol.VAB_OrderLine_ID)"
                + " INNER JOIN VAB_Order o ON (o.VAB_Order_ID=ol.VAB_Order_ID) "
                + "WHERE iol.VAM_Inv_InOutLine_ID=@param4";
            IDataReader idr = null;
            try
            {
                SqlParameter[] param = new SqlParameter[4];
                param[0] = new SqlParameter("@param1", as1.GetVAB_Currency_ID());
                param[1] = new SqlParameter("@param2", GetVAF_Client_ID());
                param[2] = new SqlParameter("@param3", GetVAF_Org_ID());
                param[3] = new SqlParameter("@param4", _issue.GetVAM_Inv_InOutLine_ID());

                idr = DataBase.DB.ExecuteReader(sql, param, null);

                if (idr.Read())
                {
                    retValue = Utility.Util.GetValueOfDecimal(idr[0]);///.getBigDecimal(1);
                    log.Fine("POCost = " + retValue);
                }
                else
                {
                    log.Warning("Not found for VAM_Inv_InOutLine_ID=" + _issue.GetVAM_Inv_InOutLine_ID());
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

            return retValue;
        }

        /// <summary>
        /// Get Labor Cost from Expense Report
        /// </summary>
        /// <param name="as1"></param>
        /// <returns>Unit Labor Cost</returns>
        private Decimal? GetLaborCost(MVABAccountBook as1)
        {
            Decimal? retValue = null;
            /** TODO Labor Cost	*/
            return retValue;
        }
    }
}
