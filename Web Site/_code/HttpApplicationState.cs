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
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace SplendidCRM
{
	public class HttpApplicationState
	{
		private static Dictionary<string, object> Application = null;

		public HttpApplicationState()
		{
			if ( Application == null )
			{
				Application = new Dictionary<string, object>();
			}
		}
		public HttpApplicationState(IConfiguration Configuration)
		{
			if ( Application == null )
			{
				Application = new Dictionary<string, object>();
				// 06/17/2022 Paul.  Protect against parallel access. 
				// Operations that change non-concurrent collections must have exclusive access. A concurrent update was performed on this collection and corrupted its state. The collection's state is no longer correct.
				lock ( Application )
				{
					this["CONFIG.Azure.SingleSignOn.Enabled"           ] = Configuration["Azure.SingleSignOn:Enabled"           ];
					this["CONFIG.Azure.SingleSignOn.AadTenantDomain"   ] = Configuration["Azure.SingleSignOn:AadTenantDomain"   ];
					this["CONFIG.Azure.SingleSignOn.ValidIssuer"       ] = Configuration["Azure.SingleSignOn:ValidIssuer"       ];
					this["CONFIG.Azure.SingleSignOn.AadTenantId"       ] = Configuration["Azure.SingleSignOn:AadTenantId"       ];
					this["CONFIG.Azure.SingleSignOn.AadClientId"       ] = Configuration["Azure.SingleSignOn:AadClientId"       ];
					this["CONFIG.Azure.SingleSignOn.AadSecretId"       ] = Configuration["Azure.SingleSignOn:AadSecretId"       ];
					this["CONFIG.Azure.SingleSignOn.MobileClientId"    ] = Configuration["Azure.SingleSignOn:MobileClientId"    ];
					this["CONFIG.Azure.SingleSignOn.MobileRedirectUrl" ] = Configuration["Azure.SingleSignOn:MobileRedirectUrl" ];
					this["CONFIG.Azure.SingleSignOn.Realm"             ] = Configuration["Azure.SingleSignOn:Realm"             ];
					this["CONFIG.Azure.SingleSignOn.FederationMetadata"] = Configuration["Azure.SingleSignOn:FederationMetadata"];

					this["CONFIG.ADFS.SingleSignOn.Enabled"            ] = Configuration["ADFS.SingleSignOn:Enabled"            ];
					this["CONFIG.ADFS.SingleSignOn.Authority"          ] = Configuration["ADFS.SingleSignOn:Authority"          ];
					this["CONFIG.ADFS.SingleSignOn.ClientId"           ] = Configuration["ADFS.SingleSignOn:ClientId"           ];
					this["CONFIG.ADFS.SingleSignOn.MobileClientId"     ] = Configuration["ADFS.SingleSignOn:MobileClientId"     ];
					this["CONFIG.ADFS.SingleSignOn.MobileRedirectUrl"  ] = Configuration["ADFS.SingleSignOn:MobileRedirectUrl"  ];
					this["CONFIG.ADFS.SingleSignOn.Realm"              ] = Configuration["ADFS.SingleSignOn:Realm"              ];
					this["CONFIG.ADFS.SingleSignOn.Thumbprint"         ] = Configuration["ADFS.SingleSignOn:Thumbprint"         ];
				}
			}
		}

		public object this[string key]
		{
			get
			{
				if ( Application.ContainsKey(key) )
					return Application[key];
				return null;
			}
			set
			{
				lock ( Application )
				{
					Application[key] = value;
				}
			}
		}

		public int Count
		{
			get
			{
				return Application.Count;
			}
		}

		public Dictionary<string, object>.KeyCollection Keys
		{
			get
			{
				return Application.Keys;
			}
		}

		public void Remove(string key)
		{
			lock ( Application )
			{
				Application.Remove(key);
			}
		}
	}
}
