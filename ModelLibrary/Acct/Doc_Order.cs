﻿/********************************************************
 * Project Name   : VAdvantage
 * Class Name     : Doc_Order
 * Purpose        : Post Order Documents.
                    <pre>
                    Table:              VAB_Order (259)
                    Document Types:     SOO, POO
 *                  </pre>
 * Class Used     : Doc
 * Chronological    Development
 * Raghunandan      19-Jan-2010
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
    public class Doc_Order : Doc
    {

        #region
        //Contained Optional Tax Lines   
        private DocTax[] _taxes = null;
        // Requisitions				
        private DocLine[] _requisitions = null;
        // Order Currency Precision		
        private int _precision = -1;
        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="ass">accounting schemata</param>
        /// <param name="idr">record</param>
        /// <param name="trxName">trx</param>
        public Doc_Order(MVABAccountBook[] ass, IDataReader idr, Trx trxName)
            : base(ass, typeof(MVABOrder), idr, null, trxName)
        {

        }
        public Doc_Order(MVABAccountBook[] ass, DataRow dr, Trx trxName)
            : base(ass, typeof(MVABOrder), dr, null, trxName)
        {

        }

        /// <summary>
        /// Load Specific Document Details
        /// </summary>
        /// <returns>error message or null</returns>
        public override String LoadDocumentDetails()
        {
            MVABOrder order = (MVABOrder)GetPO();
            SetDateDoc(order.GetDateOrdered());
            SetIsTaxIncluded(order.IsTaxIncluded());
            //	Amounts
            SetAmount(AMTTYPE_Gross, order.GetGrandTotal());
            SetAmount(AMTTYPE_Net, order.GetTotalLines());
            SetAmount(AMTTYPE_Charge, order.GetChargeAmt());

            //	Contained Objects
            _taxes = LoadTaxes();
            _lines = LoadLines(order);
            log.Fine("Lines=" + _lines.Length + ", Taxes=" + _taxes.Length);
            return null;
        }


        /// <summary>
        /// Load Invoice Line
        /// </summary>
        /// <param name="order">order</param>
        /// <returns>DocLine Array</returns>
        private DocLine[] LoadLines(MVABOrder order)
        {
            List<DocLine> list = new List<DocLine>();
            MVABOrderLine[] lines = order.GetLines();
            for (int i = 0; i < lines.Length; i++)
            {
                MVABOrderLine line = lines[i];
                DocLine docLine = new DocLine(line, this);
                Decimal Qty = line.GetQtyOrdered();
                docLine.SetQty(Qty, order.IsSOTrx());
                //
                //	Decimal PriceActual = line.getPriceActual();
                Decimal? PriceCost = null;
                if (GetDocumentType().Equals(MVABMasterDocType.DOCBASETYPE_PURCHASEORDER))	//	PO
                {
                    PriceCost = line.GetPriceCost();
                }
                Decimal? LineNetAmt = null;
                if (PriceCost != null && Env.Signum(PriceCost.Value) != 0)
                {
                    LineNetAmt = Decimal.Multiply(Qty, PriceCost.Value);
                }
                else
                {
                    LineNetAmt = line.GetLineNetAmt();
                }
                docLine.SetAmount(LineNetAmt);	//	DR
                Decimal PriceList = line.GetPriceList();
                int VAB_TaxRate_ID = docLine.GetVAB_TaxRate_ID();
                //	Correct included Tax
                if (IsTaxIncluded() && VAB_TaxRate_ID != 0)
                {
                    MVABTaxRate tax = MVABTaxRate.Get(GetCtx(), VAB_TaxRate_ID);
                    if (!tax.IsZeroTax())
                    {
                        Decimal LineNetAmtTax = tax.CalculateTax(LineNetAmt.Value, true, GetStdPrecision());
                        log.Fine("LineNetAmt=" + LineNetAmt + " - Tax=" + LineNetAmtTax);
                        LineNetAmt = Decimal.Subtract(LineNetAmt.Value, LineNetAmtTax);
                        for (int t = 0; t < _taxes.Length; t++)
                        {
                            if (_taxes[t].GetVAB_TaxRate_ID() == VAB_TaxRate_ID)
                            {
                                _taxes[t].AddIncludedTax(LineNetAmtTax);
                                break;
                            }
                        }
                        Decimal PriceListTax = tax.CalculateTax(PriceList, true, GetStdPrecision());
                        PriceList = Decimal.Subtract(PriceList, PriceListTax);
                    }
                }	//	correct included Tax

                docLine.SetAmount(LineNetAmt, PriceList, Qty);
                list.Add(docLine);
            }

            //	Return Array
            DocLine[] dl = new DocLine[list.Count];
            dl = list.ToArray();
            return dl;
        }

        /// <summary>
        /// Load Requisitions
        /// </summary>
        /// <returns>requisition lines of Order</returns>
        private DocLine[] LoadRequisitions()
        {
            MVABOrder order = (MVABOrder)GetPO();
            MVABOrderLine[] oLines = order.GetLines();
            Dictionary<int, Decimal> qtys = new Dictionary<int, Decimal>();
            for (int i = 0; i < oLines.Length; i++)
            {
                MVABOrderLine line = oLines[i];
                qtys.Add(Utility.Util.GetValueOfInt(line.GetVAB_OrderLine_ID()), line.GetQtyOrdered());
            }
            //
            List<DocLine> list = new List<DocLine>();
            String sql = "SELECT * FROM VAM_RequisitionLine rl "
                + "WHERE EXISTS (SELECT * FROM VAB_Order o "
                    + " INNER JOIN VAB_OrderLine ol ON (o.VAB_Order_ID=ol.VAB_Order_ID) "
                    + "WHERE ol.VAB_OrderLine_ID=rl.VAB_OrderLine_ID"
                    + " AND o.VAB_Order_ID=" + order.GetVAB_Order_ID() + ") "
                + "ORDER BY rl.VAB_OrderLine_ID";
            IDataReader idr = null;
            try
            {
                idr = DataBase.DB.ExecuteReader(sql, null, null);
                while (idr.Read())
                {
                    MVAMRequisitionLine line = new MVAMRequisitionLine(GetCtx(), idr, null);
                    DocLine docLine = new DocLine(line, this);
                    //	Quantity - not more then OrderLine
                    //	Issue: Split of Requisition to multiple POs & different price
                    int key = line.GetVAB_OrderLine_ID();
                    Decimal maxQty = qtys[key];
                    Decimal Qty = Math.Max(line.GetQty(), maxQty);
                    if (Env.Signum(Qty) == 0)
                    {
                        continue;
                    }
                    docLine.SetQty(Qty, false);
                    if (qtys.ContainsKey(key))
                    {
                        qtys.Remove(key);
                    }
                    qtys.Add(key, Decimal.Subtract(maxQty, Qty));
                    //
                    Decimal PriceActual = line.GetPriceActual();
                    Decimal LineNetAmt = line.GetLineNetAmt();
                    if (line.GetQty().CompareTo(Qty) != 0)
                    {
                        LineNetAmt = Decimal.Multiply(PriceActual, Qty);
                    }
                    docLine.SetAmount(LineNetAmt);	 // DR
                    list.Add(docLine);
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

            // Return Array
            DocLine[] dls = new DocLine[list.Count];
            dls = list.ToArray();
            return dls;
        }


        /// <summary>
        /// Get Currency Precision
        /// </summary>
        /// <returns>precision</returns>
        private int GetStdPrecision()
        {
            if (_precision == -1)
            {
                _precision = MVABCurrency.GetStdPrecision(GetCtx(), GetVAB_Currency_ID());
            }
            return _precision;
        }

        /// <summary>
        /// Load Invoice Taxes
        /// </summary>
        /// <returns>DocTax Array</returns>
        private DocTax[] LoadTaxes()
        {
            List<DocTax> list = new List<DocTax>();
            String sql = "SELECT it.VAB_TaxRate_ID, t.Name, t.Rate, it.TaxBaseAmt, it.TaxAmt, t.IsSalesTax "
                + "FROM VAB_TaxRate t, VAB_OrderTax it "
                + "WHERE t.VAB_TaxRate_ID=it.VAB_TaxRate_ID AND it.VAB_Order_ID=" + Get_ID();
            IDataReader idr = null;
            try
            {
                idr = DataBase.DB.ExecuteReader(sql, null, GetTrx());
                while (idr.Read())
                {
                    int VAB_TaxRate_ID = Utility.Util.GetValueOfInt(idr[0]);//.getInt(1);
                    String name = Utility.Util.GetValueOfString(idr[1]);//.getString(2);
                    Decimal rate = Utility.Util.GetValueOfDecimal(idr[2]);//.getBigDecimal(3);
                    Decimal taxBaseAmt = Utility.Util.GetValueOfDecimal(idr[3]);//.getBigDecimal(4);
                    Decimal amount = Utility.Util.GetValueOfDecimal(idr[4]);//.getBigDecimal(5);
                    bool salesTax = "Y".Equals(Utility.Util.GetValueOfString(idr[5]));//.getString(6));
                    //
                    DocTax taxLine = new DocTax(VAB_TaxRate_ID, name, rate, taxBaseAmt, amount, salesTax);
                    list.Add(taxLine);
                }
                //
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

            //	Return Array
            DocTax[] tl = new DocTax[list.Count];
            tl = list.ToArray();
            return tl;
        }

        /// <summary>
        /// Get Source Currency Balance - subtracts line and tax amounts from total - no rounding
        /// </summary>
        /// <returns>positive amount, if total invoice is bigger than lines</returns>
        public override Decimal GetBalance()
        {
            Decimal retValue = new Decimal(0.0);
            StringBuilder sb = new StringBuilder(" [");
            //  Total
            retValue = Decimal.Add(retValue, GetAmount(Doc.AMTTYPE_Gross).Value);
            sb.Append(GetAmount(Doc.AMTTYPE_Gross));
            //  - Header Charge
            retValue = Decimal.Subtract(retValue, GetAmount(Doc.AMTTYPE_Charge).Value);
            sb.Append("-").Append(GetAmount(Doc.AMTTYPE_Charge));
            //  - Tax
            if (_taxes != null)
            {
                for (int i = 0; i < _taxes.Length; i++)
                {
                    retValue = Decimal.Subtract(retValue, _taxes[i].GetAmount());
                    sb.Append("-").Append(_taxes[i].GetAmount());
                }
            }
            //  - Lines
            if (_lines != null)
            {
                for (int i = 0; i < _lines.Length; i++)
                {
                    retValue = Decimal.Subtract(retValue, _lines[i].GetAmtSource());
                    sb.Append("-").Append(_lines[i].GetAmtSource());
                }
                sb.Append("]");
            }
            //
            if (Env.Signum(retValue) != 0		//	Sum of Cost(vs. Price) in lines may not add up 
                && GetDocumentType().Equals(MVABMasterDocType.DOCBASETYPE_PURCHASEORDER))	//	PO
            {
                log.Fine(ToString() + " Balance=" + retValue + sb.ToString() + " (ignored)");
                retValue = Env.ZERO;
            }
            else
            {
                log.Fine(ToString() + " Balance=" + retValue + sb.ToString());
            }
            return retValue;
        }

        /// <summary>
        /// Create Facts (the accounting logic) for
        /// SOO, POO.
        /// <pre>
        /// Reservation (release)
        /// Expense			DR
        /// Offset					CR
        /// Commitment
        /// (to be released by Invoice Matching)
        /// Expense					CR
        /// Offset			DR
        /// </pre>
        /// </summary>
        /// <param name="as1">accounting schema</param>
        /// <returns>Fact</returns>
        public override List<Fact> CreateFacts(MVABAccountBook as1)
        {
            List<Fact> facts = new List<Fact>();
            //  Purchase Order
            if (GetDocumentType().Equals(MVABMasterDocType.DOCBASETYPE_PURCHASEORDER))
            {
                UpdateProductPO(as1);

                //	Decimal grossAmt = getAmount(Doc.AMTTYPE_Gross);

                //  Commitment
                if (as1.IsCreateCommitment())
                {
                    Fact fact = new Fact(this, as1, Fact.POST_Commitment);
                    Decimal total = Env.ZERO;
                    for (int i = 0; i < _lines.Length; i++)
                    {
                        DocLine line = _lines[i];
                        Decimal cost = line.GetAmtSource();
                        total = Decimal.Add(total, cost);

                        //	Account
                        MVABAccount expense = line.GetAccount(ProductCost.ACCTTYPE_P_Expense, as1);
                        fact.CreateLine(line, expense, GetVAB_Currency_ID(), cost, null);
                    }
                    //	Offset
                    MVABAccount offset = GetAccount(ACCTTYPE_CommitmentOffset, as1);
                    if (offset == null)
                    {
                        _error = "@NotFound@ @CommitmentOffset_Acct@";
                        log.Log(Level.SEVERE, _error);
                        return null;
                    }
                    fact.CreateLine(null, offset, GetVAB_Currency_ID(), null, total);
                    //
                    facts.Add(fact);
                }

                //  Reverse Reservation
                if (as1.IsCreateReservation())
                {
                    Fact fact = new Fact(this, as1, Fact.POST_Reservation);
                    Decimal total = Env.ZERO;
                    if (_requisitions == null)
                    {
                        _requisitions = LoadRequisitions();
                    }
                    for (int i = 0; i < _requisitions.Length; i++)
                    {
                        DocLine line = _requisitions[i];
                        Decimal cost = line.GetAmtSource();
                        total = Decimal.Add(total, cost);

                        //	Account
                        MVABAccount expense = line.GetAccount(ProductCost.ACCTTYPE_P_Expense, as1);
                        fact.CreateLine(line, expense, GetVAB_Currency_ID(), null, cost);
                    }
                    //	Offset
                    MVABAccount offset = GetAccount(ACCTTYPE_CommitmentOffset, as1);
                    if (offset == null)
                    {
                        _error = "@NotFound@ @CommitmentOffset_Acct@";
                        log.Log(Level.SEVERE, _error);
                        return null;
                    }
                    fact.CreateLine(null, offset, GetVAB_Currency_ID(), total, null);
                    //
                    facts.Add(fact);
                }	//	reservations
            }
            //	SO
            return facts;
        }


        /// <summary>
        /// Update ProductPO PriceLastPO
        /// </summary>
        /// <param name="as1">accounting schema</param>
        private void UpdateProductPO(MVABAccountBook as1)
        {
            MVAFClientDetail ci = MVAFClientDetail.Get(GetCtx(), as1.GetVAF_Client_ID());
            if (ci.GetVAB_AccountBook1_ID() != as1.GetVAB_AccountBook_ID())
            {
                return;
            }

            StringBuilder sql = new StringBuilder(
                "UPDATE VAM_Product_PO po "
                + "SET PriceLastPO = (SELECT currencyConvert(ol.PriceActual,ol.VAB_Currency_ID,po.VAB_Currency_ID,o.DateOrdered,o.VAB_CurrencyType_ID,o.VAF_Client_ID,o.VAF_Org_ID) "
                + "FROM VAB_Order o, VAB_OrderLine ol "
                + "WHERE o.VAB_Order_ID=ol.VAB_Order_ID"
                + " AND po.VAM_Product_ID=ol.VAM_Product_ID AND po.VAB_BusinessPartner_ID=o.VAB_BusinessPartner_ID");
            //	AND ROWNUM=1 AND o.VAB_Order_ID=").Append(get_ID()).Append(") ")
            if (DataBase.DB.IsOracle()) //jz
            {
                sql.Append(" AND ROWNUM=1 ");
            }
            else
            {
                sql.Append(" AND o.UPDATED IN (SELECT MAX(o1.UPDATED) "
                        + "FROM VAB_Order o1, VAB_OrderLine ol1 "
                        + "WHERE o1.VAB_Order_ID=ol1.VAB_Order_ID"
                        + " AND po.VAM_Product_ID=ol1.VAM_Product_ID AND po.VAB_BusinessPartner_ID=o1.VAB_BusinessPartner_ID")
                        .Append("  AND o1.VAB_Order_ID=").Append(Get_ID()).Append(") ");
            }

            sql.Append(" AND o.VAB_Order_ID=").Append(Get_ID()).Append(") ")
            .Append("WHERE EXISTS (SELECT * "
            + "FROM VAB_Order o, VAB_OrderLine ol "
            + "WHERE o.VAB_Order_ID=ol.VAB_Order_ID"
            + " AND po.VAM_Product_ID=ol.VAM_Product_ID AND po.VAB_BusinessPartner_ID=o.VAB_BusinessPartner_ID"
            + " AND o.VAB_Order_ID=").Append(Get_ID()).Append(")");
            int no = DataBase.DB.ExecuteQuery(sql.ToString(), null, GetTrx());
            log.Fine("Updated=" + no);
        }


        /// <summary>
        /// Get Commitments
        /// </summary>
        /// <param name="doc">document</param>
        /// <param name="maxQty">Qty invoiced/matched</param>
        /// <param name="VAB_InvoiceLine_ID">invoice line</param>
        /// <returns>commitments (order lines)</returns>
        protected static DocLine[] GetCommitments(Doc doc, Decimal maxQty, int VAB_InvoiceLine_ID)
        {
            int precision = -1;
            //
            List<DocLine> list = new List<DocLine>();
            String sql = "SELECT * FROM VAB_OrderLine ol "
                + "WHERE EXISTS "
                    + "(SELECT * FROM VAB_InvoiceLine il "
                    + "WHERE il.VAB_OrderLine_ID=ol.VAB_OrderLine_ID"
                    + " AND il.VAB_InvoiceLine_ID=" + VAB_InvoiceLine_ID + ")"
                + " OR EXISTS "
                    + "(SELECT * FROM VAM_MatchPO po "
                    + "WHERE po.VAB_OrderLine_ID=ol.VAB_OrderLine_ID"
                    + " AND po.VAB_InvoiceLine_ID=" + VAB_InvoiceLine_ID + ")";
            IDataReader idr = null;
            try
            {
                idr = DataBase.DB.ExecuteReader(sql, null, null);
                while (idr.Read())
                {
                    if (Env.Signum(maxQty) == 0)
                    {
                        continue;
                    }
                    MVABOrderLine line = new MVABOrderLine(doc.GetCtx(), idr, null);
                    DocLine docLine = new DocLine(line, doc);
                    //	Currency
                    if (precision == -1)
                    {
                        doc.SetVAB_Currency_ID(docLine.GetVAB_Currency_ID());
                        precision = MVABCurrency.GetStdPrecision(doc.GetCtx(), docLine.GetVAB_Currency_ID());
                    }
                    //	Qty
                    Decimal Qty = Math.Max(line.GetQtyOrdered(), maxQty);
                    docLine.SetQty(Qty, false);
                    //
                    Decimal PriceActual = line.GetPriceActual();
                    Decimal PriceCost = line.GetPriceCost();
                    Decimal? LineNetAmt = null;
                    if ( Env.Signum(PriceCost) != 0)
                    {
                        LineNetAmt = Decimal.Multiply(Qty, PriceCost);
                    }
                    else if (Qty.Equals(maxQty))
                    {
                        LineNetAmt = line.GetLineNetAmt();
                    }
                    else
                    {
                        LineNetAmt = Decimal.Multiply(Qty, PriceActual);
                    }
                    maxQty = Decimal.Subtract(maxQty, Qty);

                    docLine.SetAmount(LineNetAmt);	//	DR
                    Decimal PriceList = line.GetPriceList();
                    int VAB_TaxRate_ID = docLine.GetVAB_TaxRate_ID();
                    //	Correct included Tax
                    if (VAB_TaxRate_ID != 0 && line.GetParent().IsTaxIncluded())
                    {
                        MVABTaxRate tax = MVABTaxRate.Get(doc.GetCtx(), VAB_TaxRate_ID);
                        if (!tax.IsZeroTax())
                        {
                            Decimal LineNetAmtTax = tax.CalculateTax(LineNetAmt.Value, true, precision);
                            _log.Fine("LineNetAmt=" + LineNetAmt + " - Tax=" + LineNetAmtTax);
                            LineNetAmt = Decimal.Subtract(LineNetAmt.Value, LineNetAmtTax);
                            Decimal PriceListTax = tax.CalculateTax(PriceList, true, precision);
                            PriceList = Decimal.Subtract(PriceList, PriceListTax);
                        }
                    }	//	correct included Tax

                    docLine.SetAmount(LineNetAmt, PriceList, Qty);
                    list.Add(docLine);
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
                _log.Log(Level.SEVERE, sql, e);
            }


            //	Return Array
            DocLine[] dl = new DocLine[list.Count];
            dl = list.ToArray();
            return dl;
        }

        /// <summary>
        /// Get Commitment Release.
        /// Called from MatchInv for accrual and Allocation for Cash Based
        /// </summary>
        /// <param name="as1">accounting schema</param>
        /// <param name="doc">doc</param>
        /// <param name="Qty">qty invoiced/matched</param>
        /// <param name="VAB_InvoiceLine_ID">line</param>
        /// <param name="multiplier">1 for accrual</param>
        /// <returns>Fact</returns>
        public static Fact GetCommitmentRelease(MVABAccountBook as1, Doc doc,
            Decimal Qty, int VAB_InvoiceLine_ID, Decimal multiplier)
        {
            Fact fact = new Fact(doc, as1, Fact.POST_Commitment);
            DocLine[] commitments = Doc_Order.GetCommitments(doc, Qty, VAB_InvoiceLine_ID);
            Decimal total = Env.ZERO;
            int VAB_Currency_ID = -1;
            for (int i = 0; i < commitments.Length; i++)
            {
                DocLine line = commitments[i];
                if (VAB_Currency_ID == -1)
                {
                    VAB_Currency_ID = line.GetVAB_Currency_ID();
                }
                else if (VAB_Currency_ID != line.GetVAB_Currency_ID())
                {
                    doc._error = "Different Currencies of Order Lines";
                    _log.Log(Level.SEVERE, doc._error);
                    return null;
                }
                Decimal cost = Decimal.Multiply(line.GetAmtSource(), multiplier);
                total = Decimal.Add(total, cost);

                //	Account
                MVABAccount expense = line.GetAccount(ProductCost.ACCTTYPE_P_Expense, as1);
                fact.CreateLine(line, expense, VAB_Currency_ID, null, cost);
            }
            //	Offset
            MVABAccount offset = doc.GetAccount(ACCTTYPE_CommitmentOffset, as1);
            if (offset == null)
            {
                doc._error = "@NotFound@ @CommitmentOffset_Acct@";
                _log.Log(Level.SEVERE, doc._error);
                return null;
            }
            fact.CreateLine(null, offset, VAB_Currency_ID, total, null);
            return fact;
        }

    }
}
