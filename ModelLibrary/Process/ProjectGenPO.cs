﻿using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
using VAdvantage.Process;
using VAdvantage.ProcessEngine;
//using VAdvantage.Model;
using VAdvantage.Utility;
using VAdvantage.Logging;
using System.Globalization;
using System.Data.SqlClient;
using System.Data;
using VAdvantage.DataBase;
using VAdvantage.Classes;

using VAdvantage.Model;

namespace VAdvantage.Process
{
    public class ProjectGenPO : SvrProcess
    {
        /** Project Parameter			*/
        private int m_VAB_Project_ID = 0;
        /** Opt Project Line Parameter	*/
        private int m_VAB_ProjectLine_ID = 0;
        /** Consolidate Document		*/
        //private boolean m_ConsolidateDocument = true;
        private bool m_ConsolidateDocument = true;
        /** List of POs for Consolidation	*/
        //private ArrayList<MOrder> m_pos = new ArrayList<MOrder>();
        private List<MVABOrder> m_pos = new List<MVABOrder>();

        /**
         *  Prepare - e.g., get Parameters.
         */

        protected override void Prepare()
        {
            ProcessInfoParameter[] para = GetParameter();
            for (int i = 0; i < para.Length; i++)
            {
                String name = para[i].GetParameterName();
                if (para[i].GetParameter() == null)
                {
                    continue;
                }

                else if (name.Equals("VAB_Project_ID"))
                {
                    //m_VAB_Project_ID = ((BigDecimal)element.getParameter()).intValue();
                    m_VAB_Project_ID = VAdvantage.Utility.Util.GetValueOfInt(VAdvantage.Utility.Util.GetValueOfDecimal(para[i].GetParameter()));
                }
                else if (name.Equals("VAB_ProjectLine_ID"))
                {
                    //m_VAB_ProjectLine_ID = ((BigDecimal)element.getParameter()).intValue();
                    m_VAB_ProjectLine_ID = VAdvantage.Utility.Util.GetValueOfInt(VAdvantage.Utility.Util.GetValueOfDecimal(para[i].GetParameter()));
                }
                else if (name.Equals("ConsolidateDocument"))
                {
                    //m_ConsolidateDocument = "Y".equals(element.getParameter());
                    m_ConsolidateDocument = "Y".Equals(para[i].GetParameter()); ;
                }
                else
                {
                    log.Log(Level.SEVERE, "prepare - Unknown Parameter: " + name);
                }
            }
        }

        protected override String DoIt()
        {
            log.Info("doIt - VAB_Project_ID=" + m_VAB_Project_ID + " - VAB_ProjectLine_ID=" + m_VAB_ProjectLine_ID + " - Consolidate=" + m_ConsolidateDocument);
            if (m_VAB_ProjectLine_ID != 0)
            {
                MVABProjectLine projectLine = new MVABProjectLine(GetCtx(), m_VAB_ProjectLine_ID, Get_TrxName());
                MVABProject project = new MVABProject(GetCtx(), projectLine.GetVAB_Project_ID(), Get_TrxName());
                CreatePO(project, projectLine);
            }
            else
            {
                MVABProject project = new MVABProject(GetCtx(), m_VAB_Project_ID, Get_TrxName());
                MVABProjectLine[] lines = project.GetLines();
                //for (MProjectLine element : lines)
                for (int i = 0; i < lines.Length; i++)
                {
                    MVABProjectLine element = lines[i];
                    CreatePO(project, element);
                }
            }
            return "";
        }

        /**
	 * 	Create PO from Planned Amt/Qty
	 * 	@param projectLine project line
	 */
        private void CreatePO(MVABProject project, MVABProjectLine projectLine)
        {
            if (projectLine.GetVAM_Product_ID() == 0)
            {
                AddLog(projectLine.GetLine(), null, null, "Line has no Product");
                return;
            }
            if (projectLine.GetVAB_OrderPO_ID() != 0)
            {
                AddLog(projectLine.GetLine(), null, null, "Line was ordered previously");
                return;
            }

            //	PO Record
            MVAMProductPO[] pos = MVAMProductPO.GetOfProduct(GetCtx(), projectLine.GetVAM_Product_ID(), Get_TrxName());
            if (pos == null || pos.Length == 0)
            {
                AddLog(projectLine.GetLine(), null, null, "Product has no PO record");
                return;
            }

            //	Create to Order
            MVABOrder order = null;
            //	try to find PO to VAB_BusinessPartner
            for (int i = 0; i < m_pos.Count; i++)
            {
                MVABOrder test = m_pos[i];
                if (test.GetVAB_BusinessPartner_ID() == pos[0].GetVAB_BusinessPartner_ID())
                {
                    order = test;
                    break;
                }
            }
            if (order == null)	//	create new Order
            {
                //	Vendor
                MVABBusinessPartner bp = new MVABBusinessPartner(GetCtx(), pos[0].GetVAB_BusinessPartner_ID(), Get_TrxName());
                //	New Order
                order = new MVABOrder(project, false, null);
                int VAF_Org_ID = projectLine.GetVAF_Org_ID();
                if (VAF_Org_ID == 0)
                {
                    log.Warning("createPOfromProjectLine - VAF_Org_ID=0");
                    VAF_Org_ID = GetCtx().GetVAF_Org_ID();
                    if (VAF_Org_ID != 0)
                        projectLine.SetVAF_Org_ID(VAF_Org_ID);
                }
                order.SetClientOrg(projectLine.GetVAF_Client_ID(), VAF_Org_ID);
                order.SetBPartner(bp);
                order.SetVAB_Project_ID(project.Get_ID());
                order.Save();
                //	optionally save for consolidation
                if (m_ConsolidateDocument)
                    m_pos.Add(order);
            }

            //	Create Line
            MVABOrderLine orderLine = new MVABOrderLine(order);
            orderLine.SetVAM_Product_ID(projectLine.GetVAM_Product_ID(), true);
            orderLine.SetQty(projectLine.GetPlannedQty());
            orderLine.SetDescription(projectLine.GetDescription());

            //	(Vendor) PriceList Price
            orderLine.SetPrice();
            if (Env.Signum(orderLine.GetPriceActual()) == 0)
            {
                //	Try to find purchase price
                Decimal poPrice = pos[0].GetPricePO();
                int VAB_Currency_ID = pos[0].GetVAB_Currency_ID();
                // 
                if ( Env.Signum(poPrice) == 0)
                    poPrice = pos[0].GetPriceLastPO();
                if ( Env.Signum(poPrice) == 0)
                    poPrice = pos[0].GetPriceList();
                //	We have a price
                if ( Env.Signum(poPrice) != 0)
                {
                    if (order.GetVAB_Currency_ID() != VAB_Currency_ID)
                        poPrice = VAdvantage.Model.MVABExchangeRate.Convert(GetCtx(), poPrice,
                            VAB_Currency_ID, order.GetVAB_Currency_ID(),
                            order.GetDateAcct(), order.GetVAB_CurrencyType_ID(),
                            order.GetVAF_Client_ID(), order.GetVAF_Org_ID());
                    orderLine.SetPrice(poPrice);
                }
            }

            orderLine.SetTax();
            orderLine.Save();

            // touch order to recalculate tax and totals
            order.SetIsActive(order.IsActive());
            order.Save();

            //	update ProjectLine
            projectLine.SetVAB_OrderPO_ID(order.GetVAB_Order_ID());
            projectLine.Save();
            AddLog(projectLine.GetLine(), null, projectLine.GetPlannedQty(), order.GetDocumentNo());
        }


    }  //	ProjectGenPO
}
