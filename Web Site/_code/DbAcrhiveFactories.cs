/**********************************************************************************************************************
 * SplendidCRM is a Customer Relationship Management program created by SplendidCRM Software, Inc. 
 * Copyright (C) 2005-2022 SplendidCRM Software, Inc. All rights reserved.
 * 
 * This program is free software: you can redistribute it and/or modify it under the terms of the 
 * GNU Affero General Public License as published by the Free Software Foundation, either version 3 
 * of the License, or (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; 
 * without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. 
 * See the GNU Affero General Public License for more details.
 * 
 * You should have received a copy of the GNU Affero General Public License along with this program. 
 * If not, see <http://www.gnu.org/licenses/>. 
 * 
 * You can contact SplendidCRM Software, Inc. at email address support@splendidcrm.com. 
 * 
 * In accordance with Section 7(b) of the GNU Affero General Public License version 3, 
 * the Appropriate Legal Notices must display the following words on all interactive user interfaces: 
 * "Copyright (C) 2005-2011 SplendidCRM Software, Inc. All rights reserved."
 *********************************************************************************************************************/
using System;
using System.Data;
using Microsoft.Win32;

namespace SplendidCRM
{
	/// <summary>
	/// Summary description for DbAcrhiveFactories.
	/// </summary>
	public class DbAcrhiveFactories
	{
		private SplendidCRM.DbProviderFactories  DbProviderFactories = new SplendidCRM.DbProviderFactories();
		private HttpApplicationState Application = new HttpApplicationState();
		private AppSettings          AppSettings = new AppSettings();

		public DbAcrhiveFactories()
		{
		}

		// 12/22/2007 Paul.  Inside the timer event, there is no current context, so we need to pass the application. 
		public DbProviderFactory GetFactory()
		{
			// 11/14/2005 Paul.  Cache the connection string in the application as config and registry access is expected to be slower. 
			string sSplendidProvider = Sql.ToString(Application["SplendidProvider"]);
			string sConnectionString = Sql.ToString(Application["ArchiveConnectionString"]);
#if DEBUG
//			sSplendidProvider = String.Empty;
#endif
			if ( Sql.IsEmptyString(sSplendidProvider) || Sql.IsEmptyString(sConnectionString) )
			{
				sSplendidProvider = AppSettings["SplendidProvider"];
				switch ( sSplendidProvider )
				{
					case "System.Data.SqlClient":
						sConnectionString = AppSettings["ArchiveSQLServer"];
						break;
					case "System.Data.OracleClient":
						sConnectionString = AppSettings["ArchiveSystemOracle"];
						break;
					case "Oracle.DataAccess.Client":
						sConnectionString = AppSettings["ArchiveOracle"];
						break;
					case "MySql.Data":
						sConnectionString = AppSettings["ArchiveMySql"];
						break;
					case "IBM.Data.DB2":
						sConnectionString = AppSettings["ArchiveDB2"];
						break;
					case "Sybase.Data.AseClient":
						sConnectionString = AppSettings["ArchiveSybase"];
						break;
					case "iAnywhere.Data.AsaClient":
						sConnectionString = AppSettings["ArchiveSQLAnywhere"];
						break;
					case "Npgsql":
						sConnectionString = AppSettings["ArchiveNpgsql"];
						break;
					case "Registry":
					{
						string sSplendidRegistry = AppSettings["SplendidRegistry"];
						if ( Sql.IsEmptyString(sSplendidRegistry) )
						{
							// 11/14/2005 Paul.  If registry key is not provided, then compute it using the server and the application path. 
							// This will allow a single installation to support multiple databases. 
							// 12/22/2007 Paul.  We can no longer rely upon the Request object being valid as we might be inside the timer event. 
							string sServerName      = Sql.ToString(Application["ServerName"     ]);
							string sApplicationPath = Sql.ToString(Application["ApplicationPath"]);
							// 09/24/2010 Paul.  Remove trailing . so that it will be easier to debug http://localhost./SplendidCRM using Fiddler2. 
							if ( sServerName.EndsWith(".") )
								sServerName = sServerName.Substring(0, sServerName.Length - 1);
							sSplendidRegistry  = "SOFTWARE\\SplendidCRM Software\\" ;
							sSplendidRegistry += sServerName;
							if ( sApplicationPath != "/" )
								sSplendidRegistry += sApplicationPath.Replace("/", "\\");
						}
#pragma warning disable CA1416
						using (RegistryKey keySplendidCRM = Registry.LocalMachine.OpenSubKey(sSplendidRegistry))
						{
							if ( keySplendidCRM != null )
							{
								sSplendidProvider = Sql.ToString(keySplendidCRM.GetValue("SplendidProvider"));
								sConnectionString = Sql.ToString(keySplendidCRM.GetValue("ArchiveConnectionString"));
								// 01/17/2008 Paul.  99.999% percent of the time, we will be hosting on SQL Server. 
								// If the provider is not specified, then just assume SQL Server. 
								if ( Sql.IsEmptyString(sSplendidProvider) )
									sSplendidProvider = "System.Data.SqlClient";
							}
							else
							{
								throw(new Exception("Database connection information was not found in the registry " + sSplendidRegistry));
							}
						}
#pragma warning restore CA1416
						break;
					}
					case "HostingDatabase":
					{
						// 09/27/2006 Paul.  Allow a Hosting Database to contain connection strings. 
						/*
						<appSettings>
							<add key="SplendidProvider"          value="HostingDatabase" />
							<add key="SplendidHostingProvider"   value="System.Data.SqlClient" />
							<add key="SplendidHostingConnection" value="data source=(local)\SplendidCRM;initial catalog=SplendidCRM;user id=sa;password=" />
						</appSettings>
						*/
						string sSplendidHostingProvider   = AppSettings["SplendidHostingProvider"  ];
						string sSplendidHostingConnection = AppSettings["SplendidHostingConnection"];
						if ( Sql.IsEmptyString(sSplendidHostingProvider) || Sql.IsEmptyString(sSplendidHostingConnection) )
						{
							throw(new Exception("SplendidHostingProvider and SplendidHostingConnection are both required in order to pull the connection from a hosting server. "));
						}
						else
						{
							// 12/22/2007 Paul.  We can no longer rely upon the Request object being valid as we might be inside the timer event. 
							string sSplendidHostingSite = Sql.ToString(Application["ServerName"     ]);
							string sApplicationPath     = Sql.ToString(Application["ApplicationPath"]);
							if ( sApplicationPath != "/" )
								sSplendidHostingSite += sApplicationPath;
							
							DbProviderFactory dbf = DbProviderFactories.GetFactory(sSplendidHostingProvider, sSplendidHostingConnection);
							using ( IDbConnection con = dbf.CreateConnection() )
							{
								con.Open();
								string sSQL ;
								sSQL = "select SPLENDID_PROVIDER           " + ControlChars.CrLf
								     + "     , CONNECTION_STRING           " + ControlChars.CrLf
								     + "     , EXPIRATION_DATE             " + ControlChars.CrLf
								     + "  from vwSPLENDID_HOSTING_SITES    " + ControlChars.CrLf
								     + " where HOSTING_SITE = @HOSTING_SITE" + ControlChars.CrLf;
								using ( IDbCommand cmd = con.CreateCommand() )
								{
									cmd.CommandText = sSQL;
									Sql.AddParameter(cmd, "@HOSTING_SITE", sSplendidHostingSite);
									using ( IDataReader rdr = cmd.ExecuteReader(CommandBehavior.SingleRow) )
									{
										if ( rdr.Read() )
										{
											sSplendidProvider = Sql.ToString(rdr["SPLENDID_PROVIDER"]);
											sConnectionString = Sql.ToString(rdr["CONNECTION_STRING"]);
											// 01/17/2008 Paul.  99.999% percent of the time, we will be hosting on SQL Server. 
											// If the provider is not specified, then just assume SQL Server. 
											if ( Sql.IsEmptyString(sSplendidProvider) )
												sSplendidProvider = "System.Data.SqlClient";
											if ( rdr["EXPIRATION_DATE"] != DBNull.Value )
											{
												DateTime dtEXPIRATION_DATE = Sql.ToDateTime(rdr["EXPIRATION_DATE"]);
												if ( dtEXPIRATION_DATE < DateTime.Today )
													throw(new Exception("The hosting site " + sSplendidHostingSite + " expired on " + dtEXPIRATION_DATE.ToShortDateString()));
											}
											if ( Sql.IsEmptyString(sSplendidProvider) || Sql.IsEmptyString(sSplendidProvider) )
												throw(new Exception("Incomplete database connection information was found on the hosting server for site " + sSplendidHostingSite));
										}
										else
										{
											throw(new Exception("Database connection information was not found on the hosting server for site " + sSplendidHostingSite));
										}
									}
								}
							}
						}
						break;
					}
				}
				Application["SplendidProvider"] = sSplendidProvider;
				Application["ArchiveConnectionString"] = sConnectionString;
			}
			return DbProviderFactories.GetFactory(sSplendidProvider, sConnectionString);
		}
	}
}
