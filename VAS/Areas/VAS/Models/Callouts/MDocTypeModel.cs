﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using VAdvantage.Model;
using VAdvantage.Utility;
using VIS.DBase;

namespace VIS.Models
{
    public class MVABDocTypesModel
    {
        /// <summary>
        /// Get DocType Detail
        /// </summary>
        /// <param name="ctx">Context</param>
        /// <param name="fields">Parameter</param>
        /// <returns>List of Data</returns>
        public Dictionary<string, string> GetDocType(Ctx ctx, string fields)
        {
            string[] paramValue = fields.Split(',');
            int VAB_DocTypes_ID;
            //Assign parameter value
            VAB_DocTypes_ID = Util.GetValueOfInt(paramValue[0].ToString());
            //End Assign parameter
            MVABDocTypes dt = MVABDocTypes.Get(ctx, VAB_DocTypes_ID);
            Dictionary<string, string> result = new Dictionary<string, string>();
            result["IsSOTrx"] = dt.IsSOTrx().ToString();
            result["IsReturnTrx"] = dt.IsReturnTrx().ToString();
            return result;

        }

        /// <summary>
        /// Is Used to get detail of Document Type
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="fields"></param>
        /// <returns></returns>
        public Dictionary<string, object> GetDocTypeData(Ctx ctx, string fields)
        {
            int VAB_DocTypes_ID = 0;
            VAB_DocTypes_ID = Util.GetValueOfInt(fields);
            Dictionary<string, object> result = null;
            string sql = "SELECT d.DocSubTypeSO,d.HasCharges,'N',d.IsDocNoControlled,"
            + "s.CurrentNext, d.DocBaseType, s.CurrentNextSys, "
            + "s.VAF_Record_Seq_ID,d.IsSOTrx, d.IsReturnTrx, d.value, d.IsBlanketTrx, d.TreatAsDiscount "
            + "FROM VAB_DocTypes d "
            + "LEFT OUTER JOIN VAF_Record_Seq s ON (d.DocNoSequence_ID=s.VAF_Record_Seq_ID) "
            + "WHERE VAB_DocTypes_ID=" + VAB_DocTypes_ID;		//	1
            DataSet ds = DB.ExecuteDataset(sql, null, null);
            if (ds != null && ds.Tables[0].Rows.Count > 0)
            {
                result = new Dictionary<string, object>();
                result["DocSubTypeSO"] = Util.GetValueOfString(ds.Tables[0].Rows[0]["DocSubTypeSO"]);
                result["HasCharges"] = Util.GetValueOfString(ds.Tables[0].Rows[0]["HasCharges"]);
                result["IsDocNoControlled"] = Util.GetValueOfString(ds.Tables[0].Rows[0]["IsDocNoControlled"]);
                result["CurrentNext"] = Util.GetValueOfString(ds.Tables[0].Rows[0]["CurrentNext"]);
                result["CurrentNextSys"] = Util.GetValueOfString(ds.Tables[0].Rows[0]["CurrentNextSys"]);
                result["DocBaseType"] = Util.GetValueOfString(ds.Tables[0].Rows[0]["DocBaseType"]);
                result["VAF_Record_Seq_ID"] = Util.GetValueOfInt(ds.Tables[0].Rows[0]["VAF_Record_Seq_ID"]);
                result["IsSOTrx"] = Util.GetValueOfString(ds.Tables[0].Rows[0]["IsSOTrx"]);
                result["IsReturnTrx"] = Util.GetValueOfString(ds.Tables[0].Rows[0]["IsReturnTrx"]);
                result["Value"] = Util.GetValueOfString(ds.Tables[0].Rows[0]["value"]);
                result["IsBlanketTrx"] = Util.GetValueOfString(ds.Tables[0].Rows[0]["IsReturnTrx"]);
                // JID_0244
                result["TreatAsDiscount"] = Util.GetValueOfString(ds.Tables[0].Rows[0]["TreatAsDiscount"]);
            }
            return result;

        }
    }
}