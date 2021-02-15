﻿using System;
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
using System.Data.SqlClient;
using VAdvantage.Acct;

namespace ModelLibrary.Acct
{
    class Doc_IncomeTax: Doc
    {

        //private int VAB_AccountBook = 0;
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="ass"></param>
        /// <param name="idr"></param>
        /// <param name="trxName"></param>
        public Doc_IncomeTax(MVABAccountBook[] ass, IDataReader idr, Trx trxName)
            : base(ass, typeof(MIncomeTax), idr, null, trxName)
        {

        }
        public Doc_IncomeTax(MVABAccountBook[] ass, DataRow dr, Trx trxName)
            : base(ass, typeof(MIncomeTax), dr, null, trxName)
        {

        }

        /// <summary>
        /// Load Specific Document Details
        /// </summary>
        /// <returns>error message or null</returns>
        public override String LoadDocumentDetails()
        {
            MIncomeTax tax = (MIncomeTax)GetPO();
            SetDateDoc(tax.GetDateTrx());            
            _lines = LoadLines(tax);
            log.Fine("Lines=" + _lines.Length);
            return null;
        }

        private DocLine[] LoadLines(MIncomeTax tax)
        {
            List<DocLine> list = new List<DocLine>();
            MIncomeTaxLines[] lines = tax.GetLines(false);            
            for (int i = 0; i < lines.Length; i++)
            {
                MIncomeTaxLines line = lines[i];
                DocLine docLine = new DocLine(line, this);
                //docLine.SetAmount(line.GetIncomeTaxAmount());           

                // set primary key value 
                docLine.SetPrimaryKeyValue(line.GetVAB_IncomeTaxLines_ID());
                // set GL journal line table ID
                docLine.SetLineTable_ID(line.Get_Table_ID());
                //
                list.Add(docLine);
            }

            //	Return Array
            DocLine[] dls = new DocLine[list.Count];
            dls = list.ToArray();
            return dls;
        }

        /// <summary>
        /// Get Source Currency Balance - subtracts line amounts from total - no rounding
        /// </summary>
        /// <returns>positive amount, if total invoice is bigger than lines</returns>
        public override Decimal GetBalance()
        {
            Decimal retValue = Env.ZERO;
            StringBuilder sb = new StringBuilder(" [");
            //  Total
            retValue = Decimal.Add(retValue, GetAmount(Doc.AMTTYPE_Gross).Value);
            sb.Append(GetAmount(Doc.AMTTYPE_Gross));
            //  - Lines
            for (int i = 0; i < _lines.Length; i++)
            {
                retValue = Decimal.Subtract(retValue, _lines[i].GetAmtSource());
                sb.Append("-").Append(_lines[i].GetAmtSource());
            }
            sb.Append("]");
            //
            log.Fine(ToString() + " Balance=" + retValue + sb.ToString());
            //	return retValue;
            return Env.ZERO;    //  Lines are balanced
        }
        /// <summary>
        /// Create Facts (the accounting logic) for
        /// </summary>
        /// <param name="?"></param>
        /// <returns>Fact</returns>
        public override List<Fact> CreateFacts(MVABAccountBook as1)
        {
            //  create Fact Header
            List<Fact> facts = new List<Fact>();
            if (GetDocumentType().Equals(MVABMasterDocType.DOCBASETYPE_INCOMETAX))
            {

                //	Decimal grossAmt = getAmount(Doc.AMTTYPE_Gross);                
                //  Commitment
                Fact fact = new Fact(this, as1,Fact.POST_Actual);
                Decimal total = Env.ZERO;
                Decimal amount = Env.ZERO;

                for (int i = 0; i < _lines.Length; i++)
                {
                    DocLine dline = _lines[i];
                    MIncomeTaxLines line = new MIncomeTaxLines(GetCtx(), dline.Get_ID(), null);
                    amount = Util.GetValueOfDecimal(line.GetIncomeTaxAmount());
                    if (amount != Env.ZERO)
                    {
                        total = Decimal.Add(total, amount);
                    }

                    //	Account
                    MVABAccount expense = MVABAccount.Get(GetCtx(), line.GetVAB_IncomeTax_Acct());
                    fact.CreateLine(dline, expense, GetVAB_Currency_ID(), amount);
                }

                if (total != Env.ZERO)
                {
                    int validComID = Util.GetValueOfInt(DB.ExecuteScalar(@"SELECT T_Due_Acct FROM VAB_Tax_Acct WHERE VAB_AccountBook_ID=" + as1.GetVAB_AccountBook_ID() + " AND VAF_Client_ID = " + GetVAF_Client_ID()));
                    MVABAccount acct = MVABAccount.Get(GetCtx(), validComID);
                    fact.CreateLine(null, acct,GetVAB_Currency_ID(), Decimal.Negate(total));
                }

                facts.Add(fact);
            }
            return facts;
        }
    }
}
