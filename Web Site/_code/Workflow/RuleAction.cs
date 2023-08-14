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
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace SplendidCRM
{
	public class RuleAction
	{
		public string code { get; set; }

		public RuleAction(string code)
		{
			this.code = code;
		}

		public bool Validate(RuleValidation validator)
		{
			// 08/12/2023 Paul.  Rosyln expects a semi-colon terminator. 
			if ( !code.Trim().EndsWith(";") )
				code += ";";
			if ( !String.IsNullOrEmpty(code) )
			{
				SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
				IEnumerable<Diagnostic> diags = tree.GetDiagnostics();
				foreach (Diagnostic diag in diags)
				{
					if ( diag.Severity == DiagnosticSeverity.Error )
					{
						ValidationError error = new ValidationError(diag.GetMessage());
						validator.Errors.Add(error);
					}
				}
			}
			return validator.Errors.Count == 0;
		}

		public void Execute(RuleExecution exec)
		{
			if ( !Sql.IsEmptyString(code) )
			{
				string sActionCode = code.Replace("this.", "THIS.").Replace("this[", "THIS[");
				ScriptState<object> scriptState = CSharpScript.RunAsync(sActionCode, exec.ScriptOptions, exec.Globals).Result;
				//scriptState.ContinueWithAsync(code).Result;
			}
		}
	}
}
