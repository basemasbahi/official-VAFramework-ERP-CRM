﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Newtonsoft.Json;
using VAdvantage.Model;
using VAdvantage.Utility;
using VIS.Models;

namespace VIS.Controllers
{
    public class MVAMProductionLineController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public JsonResult GetChargeAmt(string fields)
        {
            Ctx ct = Session["ctx"] as Ctx;
            MVAMProductionLineModel productionLine = new MVAMProductionLineModel();
            return Json(JsonConvert.SerializeObject(productionLine.GetChargeAmount(fields)), JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// when we select BOM on production Plan, if attribute defined on BOM then need to set the same Attributesetinstance
        /// </summary>
        /// <param name="fields">VAM_BOM_ID</param>
        /// <returns>VAM_PFeature_SetInstance_ID</returns>
        public JsonResult GetAttributeSetInstance(string fields)
        {
            Ctx ct = Session["ctx"] as Ctx;
            MVAMProductionLineModel productionLine = new MVAMProductionLineModel();
            return Json(JsonConvert.SerializeObject(productionLine.GetAttributeSetInstance(ct, fields)), JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        ///  when we select either Product or AttibutesetInstance on production Plan, pick respective BOM based on respective input
        /// </summary>
        /// <param name="fields">VAM_Product_ID , VAM_PFeature_SetInstance_ID</param>
        /// <returns>VAM_BOM_ID</returns>
        public JsonResult GetBOM(string fields)
        {
            Ctx ct = Session["ctx"] as Ctx;
            MVAMProductionLineModel productionLine = new MVAMProductionLineModel();
            return Json(JsonConvert.SerializeObject(productionLine.GetBOM(ct, fields)), JsonRequestBehavior.AllowGet);
        }
    }
}