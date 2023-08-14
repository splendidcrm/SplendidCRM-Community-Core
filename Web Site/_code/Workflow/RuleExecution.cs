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
using Microsoft.CodeAnalysis.Scripting;

namespace SplendidCRM
{
	public class RuleExecution
	{
		public class SplendidControlThisGlobals
		{
			public SplendidControlThis THIS;
		}

		public class SplendidWizardThisGlobals
		{
			public SplendidWizardThis THIS;
		}

		public class SplendidImportThisGlobals
		{
			public SplendidImportThis THIS;
		}

		public class SplendidReportThisGlobals
		{
			public SplendidReportThis THIS;
		}

		public bool                     Halted                   { get; set; }
		public object                   ThisObject               { get; set; }
		public RuleValidation           Validation               { get; set; }
		public ActivityExecutionContext ActivityExecutionContext { get; set; }
		public RuleLiteralResult        ThisLiteralResult        { get; set; }
		public ScriptOptions            ScriptOptions            { get; set; }
		public object                   Globals                  { get; set; }

		public RuleExecution(RuleValidation validation, object swThis)
		{
			this.Validation = validation;
			this.ThisObject = swThis    ;
			this.ScriptOptions = ScriptOptions.Default.AddReferences("SplendidCRM");
			this.ScriptOptions.AddImports("System");

			if      ( this.ThisObject is SplendidControlThis ) this.Globals = new SplendidControlThisGlobals { THIS = (SplendidControlThis) this.ThisObject };
			else if ( this.ThisObject is SplendidWizardThis  ) this.Globals = new SplendidWizardThisGlobals  { THIS = (SplendidWizardThis ) this.ThisObject };
			else if ( this.ThisObject is SplendidImportThis  ) this.Globals = new SplendidImportThisGlobals  { THIS = (SplendidImportThis ) this.ThisObject };
			else if ( this.ThisObject is SplendidReportThis  ) this.Globals = new SplendidReportThisGlobals  { THIS = (SplendidReportThis ) this.ThisObject };
		}
	}
}
