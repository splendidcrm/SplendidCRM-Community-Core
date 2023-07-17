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
using System.Text;
using System.Runtime.Serialization;

using Microsoft.AspNetCore.Http;

namespace SplendidCRM
{
	[DataContract]
	public class Office365AccessToken
	{
		[DataMember] public string token_type    { get; set; }
		[DataMember] public string scope         { get; set; }
		[DataMember] public string expires_in    { get; set; }
		[DataMember] public string expires_on    { get; set; }
		[DataMember] public string access_token  { get; set; }
		[DataMember] public string refresh_token { get; set; }

		public string AccessToken
		{
			get { return access_token;  }
			set { access_token = value; }
		}
		public string RefreshToken
		{
			get { return refresh_token;  }
			set { refresh_token = value; }
		}
		public Int64 ExpiresInSeconds
		{
			get { return Sql.ToInt64(expires_in);  }
			set { expires_in = Sql.ToString(value); }
		}
		public string TokenType
		{
			get { return token_type;  }
			set { token_type = value; }
		}
	}

	// https://graph.microsoft.io/en-us/docs
	[DataContract]
	public class MicrosoftGraphProfile
	{
		[DataMember] public string id                { get; set; }
		[DataMember] public string userPrincipalName { get; set; }
		[DataMember] public string displayName       { get; set; }
		[DataMember] public string givenName         { get; set; }
		[DataMember] public string surname           { get; set; }
		[DataMember] public string jobTitle          { get; set; }
		[DataMember] public string mail              { get; set; }
		[DataMember] public string officeLocation    { get; set; }
		[DataMember] public string preferredLanguage { get; set; }
		[DataMember] public string mobilePhone       { get; set; }
		[DataMember] public string[] businessPhones  { get; set; }

		public string Name
		{
			get { return displayName; }
			set { displayName = value; }
		}
		public string FirstName
		{
			get { return givenName; }
			set { givenName = value; }
		}
		public string LastName
		{
			get { return surname; }
			set { surname = value; }
		}
		public string UserName
		{
			get { return userPrincipalName; }
			set { userPrincipalName = value; }
		}
		public string EmailAddress
		{
			get { return mail; }
			set { mail = value; }
		}
	}

	public class ActiveDirectory
	{
		private SplendidCRM.DbProviderFactories  DbProviderFactories = new SplendidCRM.DbProviderFactories();
		private HttpApplicationState Application        = new HttpApplicationState();
		private HttpContext          Context            ;
		private HttpSessionState     Session            ;
		private SplendidError        SplendidError      ;
		private SplendidInit         SplendidInit       ;

		public ActiveDirectory(IHttpContextAccessor httpContextAccessor, HttpSessionState Session, SplendidError SplendidError, SplendidInit SplendidInit)
		{
			this.Context             = httpContextAccessor.HttpContext;
			this.Session             = Session            ;
			this.SplendidError       = SplendidError      ;
			this.SplendidInit        = SplendidInit       ;
		}

		public string AzureLogin()
		{
			throw(new Exception("Azure Single-Sign-On is not supported."));
		}

		// 12/25/2018 Paul.  Logout should perform Azure or ADFS logout. 
		public string AzureLogout()
		{
			throw(new Exception("Azure Single-Sign-On is not supported."));
		}

		public Guid AzureValidate(string sToken, ref string sError)
		{
			throw(new Exception("Azure Single-Sign-On is not supported."));
		}

		public Guid AzureValidateJwt(string sToken, bool bMobileClient, ref string sError)
		{
			throw(new Exception("Azure Single-Sign-On is not supported."));
		}

		public string FederationServicesLogin()
		{
			throw(new Exception("ADFS Single-Sign-On is not supported."));
		}

		// 12/25/2018 Paul.  Logout should perform Azure or ADFS logout. 
		public string FederationServicesLogout()
		{
			throw(new Exception("ADFS Single-Sign-On is not supported."));
		}

		public Guid FederationServicesValidate(string sToken, ref string sError)
		{
			throw(new Exception("ADFS Single-Sign-On is not supported."));
		}

		public Guid FederationServicesValidate(string sUSER_NAME, string sPASSWORD, ref string sError)
		{
			throw(new Exception("ADFS Single-Sign-On is not supported."));
		}

		public Guid FederationServicesValidateJwt(string sToken, bool bMobileClient, ref string sError)
		{
			throw(new Exception("ADFS Single-Sign-On is not supported."));
		}

		// 07/08/2023 Paul.  Move Office365AcquireAccessToken, Office365RefreshAccessToken and Office365TestAccessToken to Office365Sync to prevent circular references. 		// 11/09/2019 Paul.  Pass the RedirectURL so that we can call from the React client. 

		public MicrosoftGraphProfile GetProfile(string sToken)
		{
			throw(new Exception("Office 365 integration is not supported."));
		}
	}
}
