namespace VAdvantage.Model
{

/** Generated Model - DO NOT CHANGE */
using System;
using System.Text;
using VAdvantage.DataBase;
using VAdvantage.Common;
using VAdvantage.Classes;
using VAdvantage.Process;
using VAdvantage.Model;
using VAdvantage.Utility;
using System.Data;
/** Generated Model for CM_AccessMedia
 *  @author Jagmohan Bhatt (generated) 
 *  @version Vienna Framework 1.1.1 - $Id$ */
public class X_CM_AccessMedia : PO
{
public X_CM_AccessMedia (Context ctx, int CM_AccessMedia_ID, Trx trxName) : base (ctx, CM_AccessMedia_ID, trxName)
{
/** if (CM_AccessMedia_ID == 0)
{
SetCM_AccessProfile_ID (0);
SetCM_Media_ID (0);
}
 */
}
public X_CM_AccessMedia (Ctx ctx, int CM_AccessMedia_ID, Trx trxName) : base (ctx, CM_AccessMedia_ID, trxName)
{
/** if (CM_AccessMedia_ID == 0)
{
SetCM_AccessProfile_ID (0);
SetCM_Media_ID (0);
}
 */
}
/** Load Constructor 
@param ctx context
@param rs result set 
@param trxName transaction
*/
public X_CM_AccessMedia (Context ctx, DataRow rs, Trx trxName) : base(ctx, rs, trxName)
{
}
/** Load Constructor 
@param ctx context
@param rs result set 
@param trxName transaction
*/
public X_CM_AccessMedia (Ctx ctx, DataRow rs, Trx trxName) : base(ctx, rs, trxName)
{
}
/** Load Constructor 
@param ctx context
@param rs result set 
@param trxName transaction
*/
public X_CM_AccessMedia (Ctx ctx, IDataReader dr, Trx trxName) : base(ctx, dr, trxName)
{
}
/** Static Constructor 
 Set Table ID By Table Name
 added by ->Harwinder */
static X_CM_AccessMedia()
{
 Table_ID = Get_Table_ID(Table_Name);
 model = new KeyNamePair(Table_ID,Table_Name);
}
/** Serial Version No */
//static long serialVersionUID 27562514368034L;
/** Last Updated Timestamp 7/29/2010 1:07:31 PM */
public static long updatedMS = 1280389051245L;
/** AD_Table_ID=890 */
public static int Table_ID;
 // =890;

/** TableName=CM_AccessMedia */
public static String Table_Name="CM_AccessMedia";

protected static KeyNamePair model;
protected Decimal accessLevel = new Decimal(6);
/** AccessLevel
@return 6 - System - Client 
*/
protected override int Get_AccessLevel()
{
return Convert.ToInt32(accessLevel.ToString());
}
/** Load Meta Data
@param ctx context
@return PO Info
*/
protected override POInfo InitPO (Ctx ctx)
{
POInfo poi = POInfo.GetPOInfo (ctx, Table_ID);
return poi;
}
/** Load Meta Data
@param ctx context
@return PO Info
*/
protected override POInfo InitPO(Context ctx)
{
POInfo poi = POInfo.GetPOInfo (ctx, Table_ID);
return poi;
}
/** Info
@return info
*/
public override String ToString()
{
StringBuilder sb = new StringBuilder ("X_CM_AccessMedia[").Append(Get_ID()).Append("]");
return sb.ToString();
}
/** Set Web Access Profile.
@param CM_AccessProfile_ID Web Access Profile */
public void SetCM_AccessProfile_ID (int CM_AccessProfile_ID)
{
if (CM_AccessProfile_ID < 1) throw new ArgumentException ("CM_AccessProfile_ID is mandatory.");
Set_ValueNoCheck ("CM_AccessProfile_ID", CM_AccessProfile_ID);
}
/** Get Web Access Profile.
@return Web Access Profile */
public int GetCM_AccessProfile_ID() 
{
Object ii = Get_Value("CM_AccessProfile_ID");
if (ii == null) return 0;
return Convert.ToInt32(ii);
}
/** Get Record ID/ColumnName
@return ID/ColumnName pair */
public KeyNamePair GetKeyNamePair() 
{
return new KeyNamePair(Get_ID(), GetCM_AccessProfile_ID().ToString());
}
/** Set Media Item.
@param CM_Media_ID Contains media content like images, flash movies etc. */
public void SetCM_Media_ID (int CM_Media_ID)
{
if (CM_Media_ID < 1) throw new ArgumentException ("CM_Media_ID is mandatory.");
Set_ValueNoCheck ("CM_Media_ID", CM_Media_ID);
}
/** Get Media Item.
@return Contains media content like images, flash movies etc. */
public int GetCM_Media_ID() 
{
Object ii = Get_Value("CM_Media_ID");
if (ii == null) return 0;
return Convert.ToInt32(ii);
}
}

}
