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
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace SplendidCRM
{
	public class RuleExpressionCondition
	{
		public string code { get; set; }

		public RuleExpressionCondition(string code)
		{
			this.code = code;
		}

		public bool Validate(RuleValidation validation)
		{
			bool valid = validation.ValidateConditionExpression(code);
			return valid;
		}

		public bool Evaluate(RuleExecution exec)
		{
			string sConditionCode = code.Replace("this.", "THIS.").Replace("this[", "THIS[");
			bool bCondition = CSharpScript.EvaluateAsync<bool>(sConditionCode, exec.ScriptOptions, exec.Globals).Result;
			return bCondition;
		}
	}
}
