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

namespace SplendidCRM
{
	public class RuleValidation
	{
		public Type                                           ThisType            { get; set; }
		public ITypeProvider                                  TypeProvider        { get; set; }
		public List<ValidationError>                          Errors              { get; set; }
		public Dictionary<string, Type>                       TypesUsed           { get; set; }
		public Dictionary<string, Type>                       TypesUsedAuthorized { get; set; }
		//public Stack<CodeExpression>                          ActiveParentNodes   { get; set; }
		//public Dictionary<CodeExpression, RuleExpressionInfo> ExpressionInfoMap   { get; set; }
		//public Dictionary<CodeTypeReference, Type>            TypeRefMap          { get; set; }
		public bool                                           CheckStaticType     { get; set; }
		public IList<AuthorizedType>                          AuthorizedTypes     { get; set; }
 
		public RuleValidation(Type thisType, SplendidRulesTypeProvider typeProvider)
		{
			this.Errors = new List<ValidationError>();
		}

		public bool ValidateConditionExpression(string expression)
		{
			if ( String.IsNullOrEmpty(expression) )
				throw(new ArgumentNullException("expression"));

			// 08/12/2023 Paul.  Rosyln expects a condition. 
			string code = "if (" + expression + ") {}";
			SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
			IEnumerable<Diagnostic> diags = tree.GetDiagnostics();
			foreach (Diagnostic diag in diags)
			{
				if ( diag.Severity == DiagnosticSeverity.Error )
				{
					ValidationError error = new ValidationError(diag.GetMessage());
					this.Errors.Add(error);
				}
			}
			return Errors.Count == 0;
		}
	}
}
