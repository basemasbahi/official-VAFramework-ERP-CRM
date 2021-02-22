﻿/********************************************************
 * Module Name    : 
 * Purpose        : 
 * Class Used     : X_VAM_BOMVAMProduct
 * Chronological Development
 * Veena Pandey     16-Sept-2009
 ******************************************************/

using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using VAdvantage.Classes;
using VAdvantage.Utility;
using VAdvantage.DataBase;
using VAdvantage.Common;
using VAdvantage.Logging;

namespace VAdvantage.Model
{
    /// <summary>
    /// BOM Product/Component Model
    /// </summary>
    public class MVAMBOMVAMProduct : X_VAM_BOMVAMProduct
    {

        #region Private Variables
        //BOM Parent
        private MVAMBOM _bom = null;

        //Component BOM				
        private MVAMBOM _componentBOM = null;

        //Included Component 
        private MVAMProduct _component = null;
        //Logger
        private static VLogger _log = VLogger.GetVLogger(typeof(MVAMBOMVAMProduct).FullName);

        #endregion

        /// <summary>
        /// Standard Constructor
        /// </summary>
        /// <param name="ctx">context</param>
        /// <param name="VAM_BOMVAMProduct_ID">id</param>
        /// <param name="trxName">transaction</param>
        public MVAMBOMVAMProduct(Ctx ctx, int VAM_BOMVAMProduct_ID, Trx trxName)
            : base(ctx, VAM_BOMVAMProduct_ID, trxName)
        {
            if (VAM_BOMVAMProduct_ID == 0)
            {
                //	SetVAM_BOM_ID (0);
                SetBOMVAMProductType(BOMVAMProductTYPE_StandardProduct);	// S
                SetBOMQty(Env.ONE);
                SetIsPhantom(false);
                SetLeadTimeOffset(0);
                //	SetLine (0);	// @SQL=SELECT NVL(MAX(Line),0)+10 AS DefaultValue FROM VAM_BOMVAMProduct WHERE VAM_BOM_ID=@VAM_BOM_ID@
            }
        }

        /// <summary>
        /// Load Constructor
        /// </summary>
        /// <param name="ctx">context</param>
        /// <param name="dr">data row</param>
        /// <param name="trxName">transaction</param>
        public MVAMBOMVAMProduct(Ctx ctx, DataRow dr, Trx trxName)
            : base(ctx, dr, trxName)
        {
        }

        /// <summary>
        /// Parent Constructor
        /// </summary>
        /// <param name="bom">product</param>
        public MVAMBOMVAMProduct(MVAMBOM bom)
            : this(bom.GetCtx(), 0, bom.Get_Trx())
        {
            _bom = bom;
        }

        /// <summary>
        /// Get Products of BOM
        /// </summary>
        /// <param name="bom">bom</param>
        /// <returns>array of BOM Products</returns>
        public static MVAMBOMVAMProduct[] GetOfBOM(MVAMBOM bom)
        {
            List<MVAMBOMVAMProduct> list = new List<MVAMBOMVAMProduct>();
            String sql = "SELECT * FROM VAM_BOMVAMProduct WHERE IsActive = 'Y' AND VAM_BOM_ID=@bomid ORDER BY SeqNo";
            try
            {
                SqlParameter[] param = new SqlParameter[1];
                param[0] = new SqlParameter("@bomid", bom.GetVAM_BOM_ID());
                DataSet ds = DataBase.DB.ExecuteDataset(sql, param, bom.Get_Trx());
                if (ds.Tables.Count > 0)
                {
                    foreach (DataRow dr in ds.Tables[0].Rows)
                    {
                        list.Add(new MVAMBOMVAMProduct(bom.GetCtx(), dr, bom.Get_Trx()));
                    }
                }
            }
            catch (Exception e)
            {
                _log.Log(Level.SEVERE, sql, e);
            }

            MVAMBOMVAMProduct[] retValue = new MVAMBOMVAMProduct[list.Count];
            retValue = list.ToArray();
            return retValue;
        }

        /// <summary>
        /// Get Parent
        /// </summary>
        /// <returns>parent</returns>
        public MVAMBOM GetBOM()
        {
            if (_bom == null && GetVAM_BOM_ID() != 0)
                _bom = MVAMBOM.Get(GetCtx(), GetVAM_BOM_ID());
            return _bom;
        }

        /// <summary>
        /// Before Save
        /// </summary>
        /// <param name="newRecord">new</param>
        /// <returns>true/false</returns>
        protected override Boolean BeforeSave(Boolean newRecord)
        {
            //	Product
            if (GetBOMVAMProductType().Equals(BOMVAMProductTYPE_OutsideProcessing))
            {
                if (GetVAM_ProductBOM_ID() != 0)
                    SetVAM_ProductBOM_ID(0);
            }
            else if (GetVAM_ProductBOM_ID() == 0)
            {
                log.SaveError("Error", Msg.ParseTranslation(GetCtx(), "@NotFound@ @VAM_ProductBOM_ID@"));
                return false;
            }
            //	Operation
            if (GetVAM_ProductOperation_ID() == 0)
            {
                if (GetSeqNo() != 0)
                    SetSeqNo(0);
            }
            else if (GetSeqNo() == 0)
            {
                log.SaveError("Error", Msg.ParseTranslation(GetCtx(), "@NotFound@ @SeqNo@"));
                return false;
            }
            //	Product Attribute Instance
            // Commented by Bharat on 30 June 2018 as issue given by Pradeep
            //if (GetVAM_PFeature_SetInstance_ID() != 0)
            //{
            //    GetBOM();
            //    if (_bom != null
            //        && MVAMBOM.BOMTYPE_Make_To_Order.Equals(_bom.GetBOMType()))
            //    {
            //        ;
            //    }
            //    else
            //    {
            //        log.SaveError("Error", Msg.ParseTranslation(GetCtx(),
            //            "ReSet @VAM_PFeature_SetInstance_ID@: Not Make-to-Order"));
            //        SetVAM_PFeature_SetInstance_ID(0);
            //        return false;
            //    }
            //}
            //	Alternate
            if ((GetBOMVAMProductType().Equals(BOMVAMProductTYPE_Alternative)
                || GetBOMVAMProductType().Equals(BOMVAMProductTYPE_AlternativeDefault))
                && GetVAM_BOMSubsitutue_ID() == 0)
            {
                log.SaveError("Error", Msg.ParseTranslation(GetCtx(), "@NotFound@ @VAM_BOMSubsitutue_ID@"));
                return false;
            }
            //	Operation
            if (GetVAM_ProductOperation_ID() != 0)
            {
                if (GetSeqNo() == 0)
                {
                    log.SaveError("Error", Msg.ParseTranslation(GetCtx(), "@NotFound@ @SeqNo@"));
                    return false;
                }
            }
            else	//	no op
            {
                if (GetSeqNo() != 0)
                    SetSeqNo(0);
                if (GetLeadTimeOffset() != 0)
                    SetLeadTimeOffset(0);
            }

            //	Set Line Number
            if (GetLine() == 0)
            {
                String sql = "SELECT NVL(MAX(Line),0)+10 FROM VAM_BOMVAMProduct WHERE VAM_BOM_ID=" + GetVAM_BOM_ID();
                int ii = DataBase.DB.GetSQLValue(Get_Trx(), sql);
                SetLine(ii);
            }

            return true;
        }

        //*****Manfacturing
        /// <summary>
        /// After Delete
        /// </summary>
        /// <param name="success">success</param>
        /// <returns>true</returns>
        /// <writer>raghu</writer>
        /// <date>08-march-2011</date>
        protected override Boolean AfterDelete(Boolean success)
        {
            //MVAMBOMVAMProduct[] lines = MVAMBOMVAMProduct.GetBOMLines(GetBOM());
            //if (lines == null || 0 == lines.Length || (1 == lines.Length && lines[0].GetVAM_BOMVAMProduct_ID() == GetVAM_BOMVAMProduct_ID()))
            //{
            // when we delete any record, then make isverfied as false on product
            MVAMBOM _bom = new MVAMBOM(GetCtx(), GetVAM_BOM_ID(), Get_Trx());
            MVAMProduct product = MVAMProduct.Get(GetCtx(), _bom.GetVAM_Product_ID());
            product.SetIsVerified(false);
            if (!product.Save(Get_Trx()))
            {
                return false;
            }
            //}
            return true;
        }

        /// <summary>
        /// Get BOM Lines for Product. Default to Current Active, Master BOM
        /// </summary>
        /// <param name="product"></param>
        /// <param name="bomType"></param>
        /// <param name="bomUse"></param>
        /// <returns>array of BOMs</returns>
        /// <writer>raghu</writer>
        /// <date>08-march-2011</date>
        public static MVAMBOMVAMProduct[] GetBOMLines(MVAMProduct product, String bomType, String bomUse)
        {
            // return lines for Current Active, Master BOM
            String sql = "SELECT VAM_BOM_ID FROM VAM_BOM WHERE VAM_Product_ID= " + product.GetVAM_Product_ID() +
            "AND BOMType ='" + bomType + "'  AND BOMUse ='" + bomUse + "'  AND IsActive = 'Y' ";
            Trx trx = product.Get_Trx();
            int bomID = 0;
            IDataReader idr = null;
            try
            {
                idr = DB.ExecuteReader(sql, null, trx);
                if (idr.Read())
                {
                    bomID = Util.GetValueOfInt(idr[0]);
                }
                idr.Close();
            }
            catch (Exception e)
            {
                _log.Log(Level.SEVERE, sql, e);
            }
            finally
            {
                if (idr != null)
                {
                    idr.Close();
                    idr = null;
                }
            }
            return GetBOMLines(MVAMBOM.Get(product.GetCtx(), bomID));
        }

        // New function added by vivek on 12/12/2017 
        /// <summary>
        /// Get BOM Lines for Product. Default to Current Active, Master BOM
        /// </summary>
        /// <param name="product"></param>
        /// <param name="bomType"></param>
        /// <param name="bomUse"></param>
        /// <returns>array of BOMs</returns>
        /// <writer>raghu</writer>
        /// <date>08-march-2011</date>
        public static MVAMBOMVAMProduct[] GetBOMLines(MVAMProduct product, String bomType, int VAM_PFeature_SetInstance_ID)
        {
            // return lines for Current Active, Master BOM
            String sql = "SELECT VAM_BOM_ID FROM VAM_BOM WHERE VAM_Product_ID= " + product.GetVAM_Product_ID() +
            " AND BOMType ='" + bomType + "'   AND IsActive = 'Y' AND NVL(VAM_PFeature_SetInstance_ID,0)=" + VAM_PFeature_SetInstance_ID;

            Trx trx = product.Get_Trx();
            int bomID = 0;
            IDataReader idr = null;
            try
            {
                idr = DB.ExecuteReader(sql, null, trx);
                if (idr.Read())
                {
                    bomID = Util.GetValueOfInt(idr[0]);
                }
                idr.Close();
            }
            catch (Exception e)
            {
                _log.Log(Level.SEVERE, sql, e);
            }
            finally
            {
                if (idr != null)
                {
                    idr.Close();
                    idr = null;
                }
            }
            return GetBOMLines(MVAMBOM.Get(product.GetCtx(), bomID));
        }

        /// <summary>
        /// Get BOM Lines for Product. Default to Current Active, Master BOM
        /// </summary>
        /// <param name="product"></param>
        /// <returns>array of BOMs</returns>
        /// <writer>raghu</writer>
        /// <date>08-march-2011</date>
        public static MVAMBOMVAMProduct[] GetBOMLines(MVAMProduct product)
        {
            // return lines for Current Active, Master BOM
            return GetBOMLines(product, X_VAM_BOM.BOMTYPE_CurrentActive, X_VAM_BOM.BOMUSE_Master);
        }

        // New function added by vivek on 12/12/2017 
        /// <summary>
        /// Get BOM Lines for Product. Default to Current Active, Master BOM
        /// </summary>
        /// <param name="product"></param>
        /// <returns>array of BOMs</returns>
        /// <writer>raghu</writer>
        /// <date>08-march-2011</date>
        public static MVAMBOMVAMProduct[] GetBOMLines(MVAMProduct product, int VAM_PFeature_SetInstance_ID)
        {
            // return lines for Current Active, Master BOM
            return GetBOMLines(product, X_VAM_BOM.BOMTYPE_CurrentActive, VAM_PFeature_SetInstance_ID);
        }


        /// <summary>
        /// Get BOM Lines for Product given a specific BOM
        /// </summary>
        /// <param name="bom">BOM</param>
        /// <returns>array of BOMVAMProducts.</returns>
        /// <writer>raghu</writer>
        /// <date>08-march-2011</date>
        public static MVAMBOMVAMProduct[] GetBOMLines(MVAMBOM bom)
        {
            String sql = "SELECT * FROM VAM_BOMVAMProduct WHERE VAM_BOM_ID=" + bom.GetVAM_BOM_ID() + " AND IsActive='Y' ORDER BY Line";
            List<MVAMBOMVAMProduct> list = new List<MVAMBOMVAMProduct>();
            IDataReader idr = null;
            try
            {
                DataTable dt = new DataTable();
                idr = DB.ExecuteReader(sql, null, bom.Get_Trx());
                dt.Load(idr);
                idr.Close();
                foreach (DataRow dr in dt.Rows)
                {
                    list.Add(new MVAMBOMVAMProduct(bom.GetCtx(), dr, bom.Get_Trx()));
                }
            }
            catch (Exception e)
            {
                _log.Log(Level.SEVERE, sql, e);
            }
            finally
            {
                if (idr != null)
                {
                    idr.Close();
                    idr = null;
                }
            }
            //
            MVAMBOMVAMProduct[] retValue = new MVAMBOMVAMProduct[list.Count];
            retValue = list.ToArray();
            return retValue;
        }

        /// <summary>
        /// Info
        /// </summary>
        /// <returns>info</returns>
        /// <writer>raghu</writer>
        /// <date>08-march-2011</date>
        public override String ToString()
        {
            //StringBuilder sb = new StringBuilder("MVAMBOMVAMProduct[").Append(Get_ID()).Append(",ComponentProduct=").
            //Append(GetComponent().GetName()).Append("]");

            StringBuilder sb = new StringBuilder("MVAMBOMVAMProduct[").Append(Get_ID()).Append(",ComponentProduct=")
                .Append(GetComponent() != null ? GetComponent().GetName() : "").Append("]");
            return sb.ToString();
        }

        /// <summary>
        /// Get BOM Lines for Product. Default to Current Active, Master BOM
        /// BOM Lines are Ordered By Ascending Order of Product Names.
        /// </summary>
        /// <param name="product">product</param>
        /// <param name="bomType">bomtype</param>
        /// <param name="bomUse">bomuse</param>
        /// <param name="isAscending">true if ascending, false if descending</param>
        /// <returns>array of BOMs</returns>
        /// <writer>raghu</writer>
        /// <date>08-march-2011</date>
        public static MVAMBOMVAMProduct[] GetBOMLinesOrderByProductName(MVAMProduct product, String bomType, String bomUse, Boolean isAscending)
        {
            // return lines for Current Active, Master BOM
            String sql = "SELECT VAM_BOM_ID FROM VAM_BOM WHERE VAM_Product_ID= " + product.GetVAM_Product_ID() +
            "AND BOMType ='" + bomType + "' AND BOMUse ='" + bomUse + "' AND IsActive = 'Y'";
            Trx trx = product.Get_Trx();
            int bomID = 0;
            IDataReader idr = null;
            try
            {
                idr = DB.ExecuteReader(sql, null, trx);
                if (idr.Read())
                {
                    bomID = Util.GetValueOfInt(idr[0]);
                }
                idr.Close();
            }
            catch (Exception e)
            {
                _log.Log(Level.SEVERE, sql, e);
            }
            finally
            {
                if (idr != null)
                {
                    idr.Close();
                    idr = null;
                }
            }
            return GetBOMLinesOrderByProductName(MVAMBOM.Get(product.GetCtx(), bomID), isAscending);
        }

        /// <summary>
        /// Get BOM Lines for Product given a specific BOM
        /// The result is Ordered By Product Name.
        /// </summary>
        /// <param name="bom">Bom</param>
        /// <param name="isAscending">true is ascending, false if descending</param>
        /// <returns>array of BOMVAMProducts.</returns>
        ///  /// <writer>raghu</writer>
        /// <date>08-march-2011</date>
        public static MVAMBOMVAMProduct[] GetBOMLinesOrderByProductName(MVAMBOM bom, Boolean isAscending)
        {
            StringBuilder sql = new StringBuilder("SELECT * FROM VAM_BOMVAMProduct WHERE VAM_BOM_ID=" + bom.GetVAM_BOM_ID() + " AND IsActive='Y'");
            if (isAscending)
            {
                sql.Append(" ORDER BY getProductName(VAM_ProductBOM_ID)");
            }
            else
            {
                sql.Append(" ORDER BY getProductName(VAM_ProductBOM_ID) DESC");
            }
            List<MVAMBOMVAMProduct> list = new List<MVAMBOMVAMProduct>();
            IDataReader idr = null;
            try
            {
                idr = DB.ExecuteReader(sql.ToString(), null, bom.Get_Trx());
                DataTable dt = new DataTable();
                dt.Load(idr);
                idr.Close();
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    list.Add(new MVAMBOMVAMProduct(bom.GetCtx(), dt.Rows[i], bom.Get_Trx()));
                }
            }
            catch (Exception e)
            {
                _log.Log(Level.SEVERE, sql.ToString(), e);
            }
            finally
            {
                if (idr != null)
                {
                    idr.Close();
                    idr = null;
                }

            }
            //
            MVAMBOMVAMProduct[] retValue = new MVAMBOMVAMProduct[list.Count];
            retValue = list.ToArray();
            return retValue;
        }

        /// <summary>
        /// Set component
        /// </summary>
        /// <param name="VAM_ProductBOM_ID">product ID</param>
        /// /// <writer>raghu</writer>
        /// <date>08-march-2011</date>
        public new void SetVAM_ProductBOM_ID(int VAM_ProductBOM_ID)
        {
            base.SetVAM_ProductBOM_ID(VAM_ProductBOM_ID);
            _component = null;
        }

        /// <summary>
        /// Set component BOM
        /// </summary>
        /// <param name="VAM_ProductBOMVersion_ID">product ID</param>
        /// <writer>raghu</writer>
        /// <date>08-march-2011</date>
        public new void SetVAM_ProductBOMVersion_ID(int VAM_ProductBOMVersion_ID)
        {
            base.SetVAM_ProductBOMVersion_ID(VAM_ProductBOMVersion_ID);
            _componentBOM = null;
        }

        /// <summary>
        /// After Save
        /// </summary>
        /// <param name="newRecord"></param>
        /// <param name="success"></param>
        /// <returns>success</returns>
        /// <writer>raghu</writer>
        /// <date>08-march-2011</date>
        protected override Boolean AfterSave(Boolean newRecord, Boolean success)
        {
            //	BOM Component Line was changed
            if (newRecord || Is_ValueChanged("VAM_ProductBOM_ID") || Is_ValueChanged("VAM_ProductBOMVersion_ID") || Is_ValueChanged("IsActive")
                || Is_ValueChanged("BOMQty") || Is_ValueChanged("SupplyType"))
            {
                MVAMBOM MVAMBOM = new MVAMBOM(GetCtx(), GetVAM_BOM_ID(), Get_Trx());
                //	Invalidate BOM
                MVAMProduct product = new MVAMProduct(GetCtx(), MVAMBOM.GetVAM_Product_ID(), Get_Trx());
                if (Get_Trx() != null)
                {
                    product.Load(Get_Trx());
                }
                if (product.IsVerified())
                {
                    product.SetIsVerified(false);
                    product.Save(Get_Trx());
                }
                //	Invalidate Products where BOM is used
            }
            return success;
        }

        /// <summary>
        /// Get included component
        /// </summary>
        /// <returns>product</returns>
        /// <writer>raghu</writer>
        /// <date>08-march-2011</date>
        public MVAMProduct GetComponent()
        {
            if (_component == null && GetVAM_ProductBOM_ID() != 0)
                _component = MVAMProduct.Get(GetCtx(), GetVAM_ProductBOM_ID());
            return _component;
        }
        /// <summary>
        /// Get Component BOM
        /// </summary>
        /// <returns>MVAMBOM</returns>
        /// <writer>raghu</writer>
        /// <date>08-march-2011</date>
        public MVAMBOM GetComponentBOM()
        {
            if (_componentBOM == null && GetVAM_ProductBOMVersion_ID() != 0)
                _componentBOM = MVAMBOM.Get(GetCtx(), GetVAM_ProductBOMVersion_ID());
            return _componentBOM;
        }

    }
}
