/*
 * This program is free software: you can redistribute it and/or modify it under the terms of the 
 * GNU Affero General Public License as published by the Free Software Foundation, either version 3 
 * of the License, or (at your option) any later version.
 * 
 * In accordance with Section 7(b) of the GNU Affero General Public License version 3, 
 * the Appropriate Legal Notices must display the following words on all interactive user interfaces: 
 * "Copyright (C) 2005-2022 SplendidCRM Software, Inc. All rights reserved."
 */

// 1. React and fabric. 
import React from 'react';
import qs from 'query-string';
import { RouteComponentProps, withRouter }                             from '../Router5'                         ;
import { observer }                                                    from 'mobx-react'                               ;
import { FontAwesomeIcon }                                             from '@fortawesome/react-fontawesome'           ;
// 2. Store and Types. 
import { EditComponent }                                               from '../../../types/EditComponent'             ;
import { HeaderButtons }                                               from '../../../types/HeaderButtons'             ;
import EDITVIEWS_FIELD                                                 from '../../../types/EDITVIEWS_FIELD'           ;
// 3. Scripts. 
import L10n                                                            from '../../../scripts/L10n'                    ;
import Sql                                                             from '../../../scripts/Sql'                     ;
import Credentials                                                     from '../../../scripts/Credentials'             ;
import SplendidCache                                                   from '../../../scripts/SplendidCache'           ;
import { Admin_GetReactState }                                         from '../../../scripts/Application'             ;
import { AuthenticatedMethod, LoginRedirect }                          from '../../../scripts/Login'                   ;
import SplendidDynamic_EditView                                        from '../../../scripts/SplendidDynamic_EditView';
import { EditView_LoadLayout, EditView_FindField, EditView_HideField } from '../../../scripts/EditView'                ;
import { ListView_LoadTable }                                          from '../../../scripts/ListView'                ;
import { UpdateAdminConfig }                                           from '../../../scripts/ModuleUpdate'            ;
import { Crm_Config }                                                  from '../../../scripts/Crm'                     ;
import { uuidFast }                                                    from '../../../scripts/utility'                 ;
import { CreateSplendidRequest, GetSplendidResult }                    from '../../../scripts/SplendidRequest'         ;
// 4. Components and Views. 
import ErrorComponent                                                  from '../../../components/ErrorComponent'       ;
import HeaderButtonsFactory                                            from '../../../ThemeComponents/HeaderButtonsFactory';

const MODULE_NAME   : string = 'EmailMan';
const sDangerousTags: string = "html|meta|body|base|form|style|applet|object|script|embed|xml|frameset|iframe|frame|blink|link|ilayer|layer|import|xmp|bgsound";
const sOutlookTags  : string = "base|form|style|applet|object|script|embed|frameset|iframe|frame|link|ilayer|layer|import|xmp";

interface IExchangeConfigViewProps extends RouteComponentProps<any>
{
	callback?         : Function;
	rowDefaultSearch? : any;
	onLayoutLoaded?   : Function;
	onSubmit?         : Function;
}

interface IExchangeConfigViewState
{
	item                      : any;
	layout                    : any;
	EDIT_NAME                 : string;
	BUTTON_NAME               : string;
	MODULE_TITLE              : string;
	DUPLICATE                 : boolean;
	LAST_DATE_MODIFIED        : Date;
	editedItem                : any;
	dependents                : Record<string, Array<any>>;
	error?                    : any;
	email_xss                 : object;
	SECURITY_TOGGLE_ALL       : boolean;
	SECURITY_OUTLOOK_DEFAULTS : boolean;
	EMAIL_INBOUND_SAVE_RAW    : boolean;

	lblSmtpAuthorizedStatus?  : string;
	lblGoogleAuthorizedStatus?: string;
	lblOfficeAuthorizedStatus?: string;
}

@observer
export default class EmailManConfigView extends React.Component<IExchangeConfigViewProps, IExchangeConfigViewState>
{
	private _isMounted = false;
	private refMap: Record<string, React.RefObject<EditComponent<any, any>>>;
	private dynamicButtonsTop    = React.createRef<HeaderButtons>();

	public get data (): any
	{
		let row: any = {};
		// 08/27/2019 Paul.  Move build code to shared object. 
		let nInvalidFields: number = SplendidDynamic_EditView.BuildDataRow(row, this.refMap);
		if ( nInvalidFields == 0 )
		{
		}
		return row;
	}

	public validate(): boolean
	{
		// 08/27/2019 Paul.  Move build code to shared object. 
		let nInvalidFields: number = SplendidDynamic_EditView.Validate(this.refMap);
		return (nInvalidFields == 0);
	}

	public clear(): void
	{
		// 08/27/2019 Paul.  Move build code to shared object. 
		SplendidDynamic_EditView.Clear(this.refMap);
		if ( this._isMounted )
		{
			this.setState({ editedItem: {} });
		}
	}

	constructor(props: IExchangeConfigViewProps)
	{
		super(props);
		//console.log((new Date()).toISOString() + ' ' + this.constructor.name + '.componentDidCatch');
		let EDIT_NAME   : string = MODULE_NAME + '.ConfigView';
		let BUTTON_NAME : string = MODULE_NAME + '.ConfigView';
		let MODULE_TITLE: string = L10n.Term(MODULE_NAME + '.LBL_CAMPAIGN_EMAIL_SETTINGS');
		Credentials.SetViewMode('AdministrationView');

		this.state =
		{
			item                     : (props.rowDefaultSearch ? props.rowDefaultSearch : null),
			layout                   : null,
			EDIT_NAME                ,
			BUTTON_NAME              ,
			MODULE_TITLE             ,
			DUPLICATE                : false,
			LAST_DATE_MODIFIED       : null,
			editedItem               :   null,
			dependents               : {},
			error                    : null,
			email_xss                : {},
			SECURITY_TOGGLE_ALL      : false,
			SECURITY_OUTLOOK_DEFAULTS: false,
			EMAIL_INBOUND_SAVE_RAW   : false,
		};
	}

	componentDidCatch(error, info)
	{
		console.error((new Date()).toISOString() + ' ' + this.constructor.name + '.componentDidCatch', error, info);
	}

	async componentDidMount()
	{
		this._isMounted = true;
		try
		{
			let status = await AuthenticatedMethod(this.props, this.constructor.name + '.componentDidMount');
			if ( status == 1 )
			{
				// 10/27/2019 Paul.  In case of single page refresh, we need to make sure that the AdminMenu has been loaded. 
				if ( SplendidCache.AdminMenu == null )
				{
					await Admin_GetReactState(this.constructor.name + '.componentDidMount');
				}
				if ( !Credentials.ADMIN_MODE )
				{
					Credentials.SetADMIN_MODE(true);
				}
				await this.load();
			}
			else
			{
				LoginRedirect(this.props.history, this.constructor.name + '.componentDidMount');
			}
		}
		catch(error)
		{
			console.error((new Date()).toISOString() + ' ' + this.constructor.name + '.componentDidMount', error);
			this.setState({ error });
		}
	}

	async componentDidUpdate(prevProps: IExchangeConfigViewProps)
	{
		// 04/28/2019 Paul.  Include pathname in filter to prevent double-bounce when state changes. 
		if ( this.props.location.pathname != prevProps.location.pathname )
		{
			// 04/26/2019 Paul.  Bounce through ResetView so that layout gets completely reloaded. 
			// 11/20/2019 Paul.  Include search parameters. 
			this.props.history.push('/Reset' + this.props.location.pathname + this.props.location.search);
		}
	}

	componentWillUnmount()
	{
		this._isMounted = false;
	}
	
	private load = async () =>
	{
		const { rowDefaultSearch } = this.props;
		const { EDIT_NAME } = this.state;
		try
		{
			let layout = EditView_LoadLayout(EDIT_NAME);
			let mail_sendtype: string = Crm_Config.ToString('mail_sendtype');
			if ( Sql.IsEmptyString(mail_sendtype) )
				mail_sendtype = 'smtp';
			this.MAIL_SENDTYPE_Changed(layout, mail_sendtype);
			let layMAIL_SMTPPASS: EDITVIEWS_FIELD = EditView_FindField(layout, 'MAIL_SMTPPASS');
			if ( layMAIL_SMTPPASS != null )
			{
				layMAIL_SMTPPASS.DATA_REQUIRED = false;
				layMAIL_SMTPPASS.UI_REQUIRED = false;
			}
			if ( this._isMounted )
			{
				// 06/19/2018 Paul.  Always clear the item when setting the layout. 
				this.setState(
				{
					layout: layout,
					item: (rowDefaultSearch ? rowDefaultSearch : null),
					editedItem: null
				}, () =>
				{
					if ( this.props.onLayoutLoaded )
					{
						this.props.onLayoutLoaded();
					}
				});
				await this.LoadItem(layout);
			}
		}
		catch(error)
		{
			console.error((new Date()).toISOString() + ' ' + this.constructor.name + '.load', error);
			this.setState({ error });
		}
	}

	private EditViewFields = (layout: any) =>
	{
		let arrSelectFields = new Array();
		if ( layout.length > 0 )
		{
			for ( let nLayoutIndex = 0; nLayoutIndex < layout.length; nLayoutIndex++ )
			{
				let lay = layout[nLayoutIndex];
				let DATA_FIELD = lay.DATA_FIELD;
				arrSelectFields.push('\'' + DATA_FIELD + '\'');
			}
		}
		// 03/14/2021 Paul.  email_xss is handled manually.
		arrSelectFields.push("\'email_xss\'");
		arrSelectFields.push("\'email_inbound_save_raw\'");
		return arrSelectFields.join(',');
	}

	private LoadItem = async (layout: any) =>
	{
		try
		{
			let sFILTER = 'NAME in (' + this.EditViewFields(layout) + ')';
			const rows = await ListView_LoadTable('CONFIG', 'NAME', 'asc', 'NAME,VALUE', sFILTER, null, true);
			let item                  : any = {};
			let email_xss             : any = {};
			let EMAIL_INBOUND_SAVE_RAW: boolean = false;
			let lblGoogleAuthorizedStatus: string = '';
			let lblOfficeAuthorizedStatus: string = '';
			if ( rows.results )
			{
				for ( let i = 0; i < rows.results.length; i++ )
				{
					let row = rows.results[i];
					item[row.NAME] = row.VALUE;
					if ( row.NAME == 'email_xss' )
					{
						let arr: string = Sql.ToString(item[row.NAME]).split('|');
						for ( let i: number = 0; i < arr.length; i++ )
						{
							email_xss[arr[i]] = true;
						}
					}
					else if ( row.NAME == 'email_inbound_save_raw' )
					{
						EMAIL_INBOUND_SAVE_RAW = Sql.ToBoolean(row.VALUE);
					}
				}
				// 07/13/2020 Paul.  Process the GoogleApps OAuth code after loading the item. 
				let queryParams : any = qs.parse(location.search);
				if ( !Sql.IsEmptyString(queryParams['oauth_host']) )
				{
					let oauth_host: string = Sql.ToString(queryParams['oauth_host']);
					let error     : string = Sql.ToString(queryParams['error'     ]);
					let code      : string = Sql.ToString(queryParams['code'      ]);
					if ( oauth_host == 'GoogleApps' )
					{
						item['mail_sendtype'] = 'GoogleApps';
						if ( !Sql.IsEmptyString(error) )
						{
							lblGoogleAuthorizedStatus = error;
						}
						else if ( !Sql.IsEmptyString(code) )
						{
							try
							{
								this.setState(
								{
									lblSmtpAuthorizedStatus  : '',
									lblGoogleAuthorizedStatus: L10n.Term('OAuth.LBL_AUTHORIZING'),
									lblOfficeAuthorizedStatus: '',
								});
								if ( this._isMounted )
								{
									let redirect_url: string  = window.location.origin;
									redirect_url += window.location.pathname.replace(this.props.location.pathname, '');
									redirect_url += '/GoogleOAuth';

									let obj: any =
									{
										code        ,
										redirect_url,
									};
									//console.log((new Date()).toISOString() + ' ' + this.constructor.name + '.LoadItem', redirect_url);
										
									let sBody: string = JSON.stringify(obj);
									let res  = await CreateSplendidRequest('Administration/EmailMan/Rest.svc/GoogleApps_Authorize', 'POST', 'application/octet-stream', sBody);
									let json = await GetSplendidResult(res);
									item['GOOGLEAPPS_OAUTH_ENABLED'] = true;
									//console.log((new Date()).toISOString() + ' ' + this.constructor.name + '.load', json);
									lblGoogleAuthorizedStatus = L10n.Term('OAuth.LBL_TEST_SUCCESSFUL');
									this.setState({ lblGoogleAuthorizedStatus });
									this.props.history.replace('ConfigView');
								}
							}
							catch(error)
							{
								console.error((new Date()).toISOString() + ' ' + this.constructor.name + '.LoadItem', error);
								lblGoogleAuthorizedStatus = error.message;
								this.setState({ lblGoogleAuthorizedStatus });
								this.props.history.replace('ConfigView');
							}
						}
					}
					else if ( oauth_host == 'Office365' )
					{
						item['mail_sendtype'] = 'Office365';
						if ( !Sql.IsEmptyString(error) )
						{
							lblGoogleAuthorizedStatus = error;
						}
						else if ( !Sql.IsEmptyString(code) )
						{
							try
							{
								this.setState(
								{
									lblSmtpAuthorizedStatus  : '',
									lblGoogleAuthorizedStatus: '',
									lblOfficeAuthorizedStatus: L10n.Term('OAuth.LBL_AUTHORIZING'),
								});
								if ( this._isMounted )
								{
									//console.log((new Date()).toISOString() + ' ' + this.constructor.name + '.LoadItem code', AUTHORIZATION_CODE);
									let redirect_url      : string = window.location.origin;
									redirect_url += window.location.pathname.replace(this.props.location.pathname, '');
									redirect_url += '/Office365OAuth';
									let obj: any =
									{
										code        ,
										redirect_url,
									};
									// 11/09/2019 Paul.  We cannot use ADAL because we are using the response_type=code style of authentication (confidential) that ADAL does not support. 
									let sBody: string = JSON.stringify(obj);
									let res  = await CreateSplendidRequest('Administration/EmailMan/Rest.svc/Office365_Authorize', 'POST', 'application/octet-stream', sBody);
									let json = await GetSplendidResult(res);
									item['OFFICE365_OAUTH_ENABLED'] = true;
									//console.log((new Date()).toISOString() + ' ' + this.constructor.name + '.load', json);
									lblOfficeAuthorizedStatus = L10n.Term('OAuth.LBL_TEST_SUCCESSFUL');
									this.setState({ lblOfficeAuthorizedStatus });
									this.props.history.replace('ConfigView');
								}
							}
							catch(error)
							{
								console.error((new Date()).toISOString() + ' ' + this.constructor.name + '.LoadItem', error);
								lblOfficeAuthorizedStatus = error.message;
								this.setState({ lblOfficeAuthorizedStatus });
								this.props.history.replace('ConfigView');
							}
						}
					}
				}
				else
				{
					let res  = await CreateSplendidRequest('Administration/EmailMan/Rest.svc/GetStatus', 'GET');
					let json = await GetSplendidResult(res);
					item['GOOGLEAPPS_OAUTH_ENABLED'] = Sql.ToBoolean(json.d.GOOGLEAPPS_OAUTH_ENABLED);
					item['OFFICE365_OAUTH_ENABLED' ] = Sql.ToBoolean(json.d.OFFICE365_OAUTH_ENABLED );
				}
			}
			if ( this._isMounted )
			{
				this.setState(
				{
					layout                   ,
					item                     ,
					editedItem               : item,
					email_xss                ,
					EMAIL_INBOUND_SAVE_RAW   ,
					lblGoogleAuthorizedStatus,
					lblOfficeAuthorizedStatus,
				});
			}
		}
		catch(error)
		{
			console.error((new Date()).toISOString() + ' ' + this.constructor.name + '.LoadItem', error);
			this.setState({ error });
		}
	}

	private _onChange = (DATA_FIELD: string, DATA_VALUE: any, DISPLAY_FIELD?: string, DISPLAY_VALUE?: any): void =>
	{
		//console.log((new Date()).toISOString() + ' ' + this.constructor.name + '._onChange()' + DATA_FIELD, DATA_VALUE);
		let { layout } = this.state;
		let item = this.state.editedItem;
		if ( item == null )
			item = {};
		item[DATA_FIELD] = DATA_VALUE;
		this.setState({ editedItem: item });
	}

	private _createDependency = (DATA_FIELD: string, PARENT_FIELD: string, PROPERTY_NAME?: string): void =>
	{
		let { dependents } = this.state;
		if ( dependents[PARENT_FIELD] )
		{
			dependents[PARENT_FIELD].push( {DATA_FIELD, PROPERTY_NAME} );
		}
		else
		{
			dependents[PARENT_FIELD] = [ {DATA_FIELD, PROPERTY_NAME} ]
		}
		if ( this._isMounted )
		{
			this.setState({ dependents: dependents });
		}
	}

	private _onUpdate = (PARENT_FIELD: string, DATA_VALUE: any, item?: any): void =>
	{
		//console.log((new Date()).toISOString() + ' ' + this.constructor.name + '._onUpdate()' + PARENT_FIELD, DATA_VALUE);
		let { dependents } = this.state;
		if ( dependents[PARENT_FIELD] )
		{
			let dependentIds = dependents[PARENT_FIELD];
			for ( let i = 0; i < dependentIds.length; i++ )
			{
				let ref = this.refMap[dependentIds[i].DATA_FIELD];
				if ( ref )
				{
					ref.current.updateDependancy(PARENT_FIELD, DATA_VALUE, dependentIds[i].PROPERTY_NAME, item);
				}
			}
		}
		else if ( PARENT_FIELD == 'mail_sendtype' )
		{
			this.MAIL_SENDTYPE_Changed(this.state.layout, DATA_VALUE);
			if ( DATA_VALUE == 'GoogleApps' )
			{
				let ref = this.refMap['smtpserver'];
				if ( ref )
				{
					ref.current.updateDependancy(PARENT_FIELD, 'smtp.gmail.com', 'value', null);
				}
				ref = this.refMap['smtpport'];
				if ( ref )
				{
					ref.current.updateDependancy(PARENT_FIELD, '587', 'value', null);
				}
				ref = this.refMap['smtpauth_req'];
				if ( ref )
				{
					ref.current.updateDependancy(PARENT_FIELD, true, 'value', null);
				}
				ref = this.refMap['smtpssl'];
				if ( ref )
				{
					ref.current.updateDependancy(PARENT_FIELD, true, 'value', null);
				}
			}
		}
	}

	// 06/15/2018 Paul.  The SearchView will register for the onSubmit event. 
	private _onSubmit = (): void =>
	{
		try
		{
			if ( this.props.onSubmit )
			{
				//console.log((new Date()).toISOString() + ' ' + this.constructor.name + '._onSubmit');
				this.props.onSubmit();
			}
		}
		catch(error)
		{
			console.error((new Date()).toISOString() + ' ' + this.constructor.name + '._onSubmit', error);
			this.setState({ error });
		}
	}

	private Save = async() =>
	{
		const { email_xss, EMAIL_INBOUND_SAVE_RAW } = this.state;
		let row: any = {};
		let arrTags = sDangerousTags.split('|');
		let arrEmailXss = [];
		for ( let i: number = 0; i < arrTags.length; i++ )
		{
			let sTagName: string = arrTags[i];
			if ( email_xss[sTagName] )
			{
				arrEmailXss.push(sTagName);
			}
		}
		row['email_xss'             ] = arrEmailXss.join('|');
		row['email_inbound_save_raw'] = EMAIL_INBOUND_SAVE_RAW;

		// 08/27/2019 Paul.  Keep the old to handle password issues. 
		//let nInvalidFields: number = SplendidDynamic_EditView.BuildDataRow(row, this.refMap);
		Object.keys(this.refMap).map((key) =>
		{
			let ref = this.refMap[key];
			let data = ref.current.data;
			// 07/05/2019 Paul.  Data may be an array of key/value pairs.  This is true of TeamSelect and UserSelect. 
			if ( data )
			{
				if ( Array.isArray(data) )
				{
					for ( let i = 0; i < data.length; i++  )
					{
						if ( data[i].key )
						{
							row[data[i].key] = data[i].value;
						}
					}
				}
				else if ( data.key )
				{
					// 04/08/2019 Paul.  Password fields that have not been modified will not be sent. 
					if ( data.value != Sql.sEMPTY_PASSWORD )
					{
						row[data.key] = data.value;
					}
					if ( data.files && Array.isArray(data.files) )
					{
						if ( row.Files === undefined )
						{
							row.Files = new Array();
						}
						for ( let i = 0; i < data.files.length; i++ )
						{
							row.Files.push(data.files[i]);
						}
					}
				}
			}
		});
		try
		{
			if ( this.dynamicButtonsTop.current != null )
			{
				this.dynamicButtonsTop.current.EnableButton('Save', false);
			}
			await UpdateAdminConfig(row);
			return true;
		}
		catch(error)
		{
			console.error((new Date()).toISOString() + ' ' + this.constructor.name + '.Save', error);
			if ( this.dynamicButtonsTop.current != null )
			{
				this.dynamicButtonsTop.current.EnableButton('Save', true);
			}
			if ( this._isMounted )
			{
				this.setState({ error });
			}
			return false;
		}
	}

	// 05/14/2018 Chase. This function will be passed to DynamicButtons to be called as Page_Command
	// Add additional params if you need access to the onClick event params.
	private Page_Command = async (sCommandName, sCommandArguments) =>
	{
		const { history } = this.props;
		//console.log((new Date()).toISOString() + ' ' + this.constructor.name + '.Page_Command ' + sCommandName, sCommandArguments, this.refMap)
		// This sets the local state, which is then passed to DynamicButtons
		try
		{
			let row;
			switch (sCommandName)
			{
				case 'Save':
				{
					this.Save();
					history.push('/Administration');
					break;
				}
				case 'Cancel':
				{
					history.push(`/Reset/Administration`);
					break;
				}
				case 'Test':
				{
					await this._onSmtpTest();
					break;
				}
				default:
					break;
			}
		}
		catch(error)
		{
			console.error((new Date()).toISOString() + ' ' + this.constructor.name + '.Page_Command ' + sCommandName, error);
			this.setState({ error });
		}
	}

	private _onFieldDidMount = (DATA_FIELD: string, component: any): void =>
	{
		const { item } = this.state;
		//console.log((new Date()).toISOString() + ' ' + this.constructor.name + '._onFieldDidMount', DATA_FIELD);
		try
		{
			if ( item )
			{
				/*
				if ( !Sql.IsEmptyString(DATA_FIELD) && DATA_FIELD.indexOf('Users.LBL_') >= 0 )
				{
					component.updateDependancy(null, 'dataField', 'class', null);
				}
				*/
			}
		}
		catch(error)
		{
			console.error((new Date()).toISOString() + ' ' + this.constructor.name + '._onFieldDidMount ' + DATA_FIELD, error.message);
			this.setState({ error });
		}
	}

	private MAIL_SENDTYPE_Changed = (layout: EDITVIEWS_FIELD[], mail_sendtype: string) =>
	{
		let bSmtp    : boolean = (mail_sendtype == 'smtp');
		let bExchange: boolean = (mail_sendtype == 'Exchange-Password');
		EditView_HideField(layout, 'smtpserver'  , !bSmtp);
		EditView_HideField(layout, 'smtpport'    , !bSmtp);
		EditView_HideField(layout, 'smtpauth_req', !bSmtp);
		EditView_HideField(layout, 'smtpssl'     , !bSmtp);
		EditView_HideField(layout, 'smtpuser'    , !(bSmtp || bExchange));
		EditView_HideField(layout, 'smtppass'    , !(bSmtp || bExchange));
		this._onButtonsLoaded();
	}

	private _onSmtpTest = async () =>
	{
		//console.log((new Date()).toISOString() + ' ' + this.constructor.name + '._onSmtpTest');
		try
		{
			this.setState(
			{
				lblSmtpAuthorizedStatus : L10n.Term('OAuth.LBL_TESTING'),
			});
			if ( this._isMounted )
			{
				const currentItem = Object.assign({}, this.state.item, this.state.editedItem);
				let obj: any = {};
				obj.from_addr         = currentItem['fromaddress'  ];
				obj.from_name         = currentItem['fromname'     ];
				obj.mail_sendtype     = currentItem['mail_sendtype'];
				obj.mail_smtpuser     = currentItem['smtpuser'     ];
				obj.mail_smtppass     = currentItem['smtppass'     ];
				obj.mail_smtpserver   = currentItem['smtpserver'   ];
				obj.mail_smtpport     = currentItem['smtpport'     ];
				obj.mail_smtpauth_req = currentItem['smtpauth_req' ];
				obj.mail_smtpssl      = currentItem['smtpssl'      ];
				let sBody: string = JSON.stringify(obj);
				let res  = await CreateSplendidRequest('Administration/EmailMan/Rest.svc/SendTestMessage', 'POST', 'application/octet-stream', sBody);
				let json = await GetSplendidResult(res);
				//console.log((new Date()).toISOString() + ' ' + this.constructor.name + '._onSmtpTest', json);
				if ( this._isMounted )
				{
					this.setState(
					{
						lblSmtpAuthorizedStatus : json.d,
					});
				}
			}
		}
		catch(error)
		{
			console.error((new Date()).toISOString() + ' ' + this.constructor.name + '._onSmtpTest', error);
			this.setState(
			{
				lblSmtpAuthorizedStatus : error.message,
			});
		}
	}

	private _onGoogleAppsAuthorize = async () =>
	{
		const { editedItem } = this.state;
		//console.log((new Date()).toISOString() + ' ' + this.constructor.name + '._onGoogleAppsAuthorize');
		// 03/13/2021 Paul.  We need to update the send type if it changed as the OAuth redirect will lose this information. 
		if ( !Sql.IsEmptyString(editedItem['mail_sendtype']) )
		{
			let row: any = { mail_sendtype: editedItem['mail_sendtype'] };
			await UpdateAdminConfig(row);
		}

		let client_id      : string = Crm_Config.ToString('GoogleApps.ClientID');
		let access_type    : string = 'offline';
		let approval_prompt: string = 'force';
		let response_type  : string = 'code';
		let scope          : string = 'profile';
		// 07/13/2020 Paul.  The redirect_url cannot include parameters.  Use state variable instead. 
		let state          : string = 'EmailMan';
		let redirect_url   : string = window.location.origin;
		redirect_url += window.location.pathname.replace(this.props.location.pathname, '');
		redirect_url += '/GoogleOAuth';
		scope              += escape(' https://www.googleapis.com/auth/calendar');
		scope              += escape(' https://www.googleapis.com/auth/tasks'   );
		scope              += escape(' https://mail.google.com/'                );
		scope              += escape(' https://www.google.com/m8/feeds'         );
		let authenticateUrl = 'https://accounts.google.com/o/oauth2/auth'
		                    + '?client_id='       + client_id
		                    + '&access_type='     + access_type
		                    + '&approval_prompt=' + approval_prompt
		                    + '&response_type='   + response_type
		                    + '&redirect_uri='    + encodeURIComponent(redirect_url)
		                    + '&scope='           + scope
		                    + '&state='           + encodeURIComponent(state)
		                    ;
		//console.log((new Date()).toISOString() + ' ' + this.constructor.name + '._onGoogleAppsAuthorize', redirect_url);
		window.location.href = authenticateUrl;
	}

	private _onGoogleAppsDelete = async () =>
	{
		//console.log((new Date()).toISOString() + ' ' + this.constructor.name + '._onGoogleAppsDelete');
		try
		{
			const currentItem = Object.assign({}, this.state.item, this.state.editedItem);
			let obj: any = {};
			let sBody: string = JSON.stringify(obj);
			let res  = await CreateSplendidRequest('Administration/EmailMan/Rest.svc/GoogleApps_Delete', 'POST', 'application/octet-stream', sBody);
			let json = await GetSplendidResult(res);
			currentItem['GOOGLEAPPS_OAUTH_ENABLED'] = false;
			this.setState({ editedItem: currentItem });
		}
		catch(error)
		{
			console.error((new Date()).toISOString() + ' ' + this.constructor.name + '._onGoogleAppsDelete', error);
			this.setState({ lblGoogleAuthorizedStatus: error.message });
		}
	}

	private _onGoogleAppsTest = async () =>
	{
		//console.log((new Date()).toISOString() + ' ' + this.constructor.name + '._onGoogleAppsTest');
		try
		{
			const currentItem = Object.assign({}, this.state.item, this.state.editedItem);
			let obj: any = {};
			let sBody: string = JSON.stringify(obj);
			let res  = await CreateSplendidRequest('Administration/EmailMan/Rest.svc/GoogleApps_Test', 'POST', 'application/octet-stream', sBody);
			let json = await GetSplendidResult(res);
			this.setState({ lblGoogleAuthorizedStatus: json.d });
		}
		catch(error)
		{
			console.error((new Date()).toISOString() + ' ' + this.constructor.name + '._onGoogleAppsTest', error);
			this.setState({ lblGoogleAuthorizedStatus: error.message });
		}
	}

	private _onGoogleAppsRefreshToken = async () =>
	{
		//console.log((new Date()).toISOString() + ' ' + this.constructor.name + '._onGoogleAppsRefreshToken');
		const currentItem = Object.assign({}, this.state.item, this.state.editedItem);
		try
		{
			let obj: any = {};
			let sBody: string = JSON.stringify(obj);
			let res  = await CreateSplendidRequest('Administration/EmailMan/Rest.svc/GoogleApps_RefreshToken', 'POST', 'application/octet-stream', sBody);
			let json = await GetSplendidResult(res);
			currentItem['GOOGLEAPPS_OAUTH_ENABLED'] = true;
			this.setState(
			{
				editedItem               : currentItem,
				lblGoogleAuthorizedStatus: L10n.Term('OAuth.LBL_TEST_SUCCESSFUL'),
			});
		}
		catch(error)
		{
			console.error((new Date()).toISOString() + ' ' + this.constructor.name + '._onGoogleAppsRefreshToken', error);
			currentItem['GOOGLEAPPS_OAUTH_ENABLED'] = false;
			this.setState(
			{
				editedItem               : currentItem,
				lblGoogleAuthorizedStatus: error.message
			});
		}
	}

	private _onOffice365Authorize = async () =>
	{
		const { editedItem } = this.state;
		//console.log((new Date()).toISOString() + ' ' + this.constructor.name + '._onOffice365Authorize');
		// 11/29/2020 Paul.  We need to update the send type if it changed as the OAuth redirect will lose this information. 
		if ( !Sql.IsEmptyString(editedItem['mail_sendtype']) )
		{
			let row: any = { mail_sendtype: editedItem['mail_sendtype'] };
			await UpdateAdminConfig(row);
		}

		// 11/28/2020 Paul.  Outlook REST API has been deprecated.  Use Microsoft Graph instead. https://docs.microsoft.com/en-us/outlook/rest/compare-graph
		let client_id      : string = Crm_Config.ToString('Exchange.ClientID');
		let response_type  : string = 'code';
		// 12/29/2020 Paul.  Update scope to allow sync of contacts, calendars and mailbox. 
		let scope          : string = "openid offline_access Mail.ReadWrite Mail.Send Contacts.ReadWrite Calendars.ReadWrite MailboxSettings.ReadWrite User.Read";
		// 07/13/2020 Paul.  The redirect_url cannot include parameters.  Use state variable instead. 
		let state          : string = 'EmailMan';
		let redirect_url   : string = window.location.origin;
		redirect_url += window.location.pathname.replace(this.props.location.pathname, '');
		redirect_url += '/Office365OAuth';
		// 02/04/2023 Paul.  Directory Tenant is now required for single tenant app registrations. 
		let tenant         : string = Crm_Config.ToString('Exchange.DirectoryTenantID');
		if ( tenant == '' )
			tenant = 'common';
		let authenticateUrl: string = 'https://login.microsoftonline.com/'+ tenant + '/oauth2/v2.0/authorize'
		                   + '?response_type=' + response_type
		                   + '&client_id='     + client_id
		                   + '&redirect_uri='  + encodeURIComponent(redirect_url)
		                   + '&scope='         + escape(scope)
		                   + '&state='         + state
		                   + '&response_mode=query';
		//console.log((new Date()).toISOString() + ' ' + this.constructor.name + '._onOffice365Authorize', redirect_url);
		window.location.href = authenticateUrl;
	}

	private _onOffice365Delete = async () =>
	{
		//console.log((new Date()).toISOString() + ' ' + this.constructor.name + '._onOffice365Delete');
		try
		{
			const currentItem = Object.assign({}, this.state.item, this.state.editedItem);
			let obj: any = {};
			let sBody: string = JSON.stringify(obj);
			let res  = await CreateSplendidRequest('Administration/EmailMan/Rest.svc/Office365_Delete', 'POST', 'application/octet-stream', sBody);
			let json = await GetSplendidResult(res);
			currentItem['OFFICE365_OAUTH_ENABLED'] = false;
			this.setState({ editedItem: currentItem });
		}
		catch(error)
		{
			console.error((new Date()).toISOString() + ' ' + this.constructor.name + '._onOffice365Delete', error);
			this.setState({ lblOfficeAuthorizedStatus: error.message });
		}
	}

	private _onOffice365Test = async () =>
	{
		//console.log((new Date()).toISOString() + ' ' + this.constructor.name + '._onOffice365Test');
		try
		{
			const currentItem = Object.assign({}, this.state.item, this.state.editedItem);
			let obj: any = {};
			let sBody: string = JSON.stringify(obj);
			let res  = await CreateSplendidRequest('Administration/EmailMan/Rest.svc/Office365_Test', 'POST', 'application/octet-stream', sBody);
			let json = await GetSplendidResult(res);
			this.setState({ lblOfficeAuthorizedStatus: json.d });
		}
		catch(error)
		{
			console.error((new Date()).toISOString() + ' ' + this.constructor.name + '._onOffice365Test', error);
			this.setState({ lblOfficeAuthorizedStatus: error.message });
		}
	}

	private _onOffice365RefreshToken = async () =>
	{
		//console.log((new Date()).toISOString() + ' ' + this.constructor.name + '._onOffice365RefreshToken');
		const currentItem = Object.assign({}, this.state.item, this.state.editedItem);
		try
		{
			let obj: any = {};
			let sBody: string = JSON.stringify(obj);
			let res  = await CreateSplendidRequest('Administration/EmailMan/Rest.svc/Office365_RefreshToken', 'POST', 'application/octet-stream', sBody);
			let json = await GetSplendidResult(res);
			currentItem['OFFICE365_OAUTH_ENABLED'] = true;
			this.setState(
			{
				editedItem               : currentItem,
				lblOfficeAuthorizedStatus: L10n.Term('OAuth.LBL_TEST_SUCCESSFUL'),
			});
		}
		catch(error)
		{
			console.error((new Date()).toISOString() + ' ' + this.constructor.name + '._onOffice365RefreshToken', error);
			currentItem['OFFICE365_OAUTH_ENABLED'] = false;
			this.setState(
			{
				editedItem               : currentItem,
				lblOfficeAuthorizedStatus: error.message
			});
		}
	}

	private _onButtonsLoaded = () =>
	{
		const currentItem = Object.assign({}, this.state.item, this.state.editedItem);
		// 08/12/2019 Paul.  Here is where we can disable buttons immediately after they were loaded. 
		if ( this.dynamicButtonsTop.current != null )
		{
			this.dynamicButtonsTop.current.ShowButton('Test', (Sql.IsEmptyString(currentItem['mail_sendtype']) || currentItem['mail_sendtype'] == 'smtp' || currentItem['mail_sendtype'] == 'Exchange-Password'));
		}
	}

	private toggleEMAIL_INBOUND_SAVE_RAW = (ev: React.ChangeEvent<HTMLInputElement>) =>
	{
		let { EMAIL_INBOUND_SAVE_RAW } = this.state;
		EMAIL_INBOUND_SAVE_RAW = ev.target.checked;
		this.setState({ EMAIL_INBOUND_SAVE_RAW });
	}

	private toggleAllSecurityOptions = (ev: React.ChangeEvent<HTMLInputElement>) =>
	{
		let { SECURITY_TOGGLE_ALL, email_xss } = this.state;
		SECURITY_TOGGLE_ALL = ev.target.checked;
		let arrTags = sDangerousTags.split('|');
		for ( let i: number = 0; i < arrTags.length; i++ )
		{
			let sTagName: string = arrTags[i];
			email_xss[sTagName] = SECURITY_TOGGLE_ALL;
		}
		this.setState({ SECURITY_TOGGLE_ALL, email_xss });
	}

	private setOutlookDefaults = (ev: React.ChangeEvent<HTMLInputElement>) =>
	{
		let { SECURITY_OUTLOOK_DEFAULTS, email_xss } = this.state;
		SECURITY_OUTLOOK_DEFAULTS = ev.target.checked;
		let arrTags = sOutlookTags.split('|');
		for ( let i: number = 0; i < arrTags.length; i++ )
		{
			let sTagName: string = arrTags[i];
			email_xss[sTagName] = true;
		}
		this.setState({ SECURITY_OUTLOOK_DEFAULTS, email_xss });
	}

	private toggleEmailXss = (name: string, ev: React.ChangeEvent<HTMLInputElement>) =>
	{
		let { email_xss } = this.state;
		email_xss[name] = ev.target.checked;
		this.setState({ email_xss });
	}

	private BuildSecurityTable = () =>
	{
		const { email_xss } = this.state;
		let tblChildren = [];
		let tblSECURITY_TAGS = React.createElement('table', { style: {border: 'none', borderCollapse: 'collapse', cellPadding: '4px', cellSpacing: '0px' } }, tblChildren);
		let tr = null;
		let trChildren = [];
		let arrTags = sDangerousTags.split('|');
		for ( let i: number = 0; i < arrTags.length; i++ )
		{
			let sTagName: string = arrTags[i];
			if ( tr == null ||  (i % 2 == 0) )
			{
				trChildren = [];
				tr = React.createElement('tr', {}, trChildren);
				tblChildren.push(tr);
			}
			let tdChildren = [];
			let td = React.createElement('td', { style: {valign: 'bottom'} }, tdChildren);
			trChildren.push(td);
			let chkProps: any =  { id: 'SECURITY_' + sTagName, type: 'checkbox', className: 'checkbox', checked: email_xss[sTagName] };
			chkProps.onChange = (ev: React.ChangeEvent<HTMLInputElement>) => 
			{
				this.toggleEmailXss(sTagName, ev);
			};
			let chk = React.createElement('input', chkProps);
			tdChildren.push(chk);

			let lbl = React.createElement('label', { htmlFor: 'SECURITY_' + sTagName, dangerouslySetInnerHTML: { __html: ('&lt;' + sTagName + '&gt')}, style: { margin: '4px'} }, );
			tdChildren.push(lbl);

			td = React.createElement('td', { style: {valign: 'bottom'} }, L10n.Term('EmailMan.LBL_SECURITY_' + sTagName.toUpperCase()) );
			trChildren.push(td);
		}
		return tblSECURITY_TAGS;
	}

	public render()
	{
		const { callback } = this.props;
		const { item, layout, BUTTON_NAME, MODULE_TITLE, error, SECURITY_TOGGLE_ALL, SECURITY_OUTLOOK_DEFAULTS, EMAIL_INBOUND_SAVE_RAW } = this.state;
		const { lblSmtpAuthorizedStatus, lblGoogleAuthorizedStatus, lblOfficeAuthorizedStatus } = this.state;
		// 05/04/2019 Paul.  Reference obserable IsInitialized so that terminology update will cause refresh. 
		//console.log((new Date()).toISOString() + ' ' + this.constructor.name + '.render: ' + EDIT_NAME + ' ' + BUTTON_NAME, layout, item);
		if ( layout == null || item == null )
		{
			return null;
		}
		this.refMap = {};
		let onSubmit = (this.props.onSubmit ? this._onSubmit : null);
		if ( SplendidCache.IsInitialized && SplendidCache.AdminMenu && layout && BUTTON_NAME )
		{
			const currentItem = Object.assign({}, this.state.item, this.state.editedItem);
			// 12/04/2019 Paul.  After authentication, we need to make sure that the app gets updated. 
			Credentials.sUSER_THEME;
			let headerButtons = HeaderButtonsFactory(SplendidCache.UserTheme);
			return (
			<div>
				<ErrorComponent error={error} />
				{ !callback && headerButtons
				? React.createElement(headerButtons, { MODULE_NAME, MODULE_TITLE, ButtonStyle: 'EditHeader', VIEW_NAME: BUTTON_NAME, row: item, Page_Command: this.Page_Command, showButtons: true, onLayoutLoaded: this._onButtonsLoaded, history: this.props.history, location: this.props.location, match: this.props.match, ref: this.dynamicButtonsTop })
				: null
				}
				<div id={!!callback ? null : "content"}>
					{ SplendidDynamic_EditView.AppendEditViewFields(item, layout, this.refMap, callback, this._createDependency, null, this._onChange, this._onUpdate, onSubmit, 'tabForm', this.Page_Command) }
					{ (currentItem['mail_sendtype'] == 'smtp' || currentItem['mail_sendtype'] == 'Exchange-Password')
					? <div>
						<div className='tabForm'>
							<span id='lblSmtpAuthorizedStatus' className='error'>{ lblSmtpAuthorizedStatus }</span>
						</div>
					</div>
					: null
					}
					{ currentItem['mail_sendtype'] == 'Office365'
					? <div>
						<h4 style={ {marginTop: '6px'} }>{ L10n.Term('Users.LBL_OFFICE365_OPTIONS_TITLE') }</h4>
						<div className='tabForm'>
							<div style={ {margin: '4px'} }>
								{ Sql.ToBoolean(currentItem['OFFICE365_OAUTH_ENABLED'])
								? <span id='lblOffice365Authorized' style={ {marginRight: '10px'} }>{ L10n.Term('OAuth.LBL_AUTHORIZED') }</span>
								: null
								}
								{ !Sql.ToBoolean(currentItem['OFFICE365_OAUTH_ENABLED'])
								? <button className='button' onClick={ this._onOffice365Authorize    } style={ {marginRight: '10px'} }>  { L10n.Term('OAuth.LBL_AUTHORIZE_BUTTON_LABEL') }  </button>
								: null
								}
								{ Sql.ToBoolean(currentItem['OFFICE365_OAUTH_ENABLED'])
								? <button className='button' onClick={ this._onOffice365Delete       } style={ {marginRight: '10px'} }>  { L10n.Term('OAuth.LBL_DELETE_BUTTON_LABEL') }  </button>
								: null
								}
								{ Sql.ToBoolean(currentItem['OFFICE365_OAUTH_ENABLED'])
								? <button className='button' onClick={ this._onOffice365Test         } style={ {marginRight: '10px'} }>  { L10n.Term('OAuth.LBL_TEST_BUTTON_LABEL') }  </button>
								: null
								}
								{ Sql.ToBoolean(currentItem['OFFICE365_OAUTH_ENABLED'])
								? <button className='button' onClick={ this._onOffice365RefreshToken } style={ {marginRight: '10px'} }>  { L10n.Term('OAuth.LBL_REFRESH_TOKEN_LABEL') }  </button>
								: null
								}
								<span id='lblOfficeAuthorizedStatus' className='error'>{ lblOfficeAuthorizedStatus }</span>
							</div>
						</div>
					</div>
					: null
					}
					{ currentItem['mail_sendtype'] == 'GoogleApps'
					? <div>
						<h4 style={ {marginTop: '6px'} }>{ L10n.Term('Users.LBL_GOOGLEAPPS_OPTIONS_TITLE') }</h4>
						<div className='tabForm'>
							<div style={ {margin: '4px'} }>
								{ Sql.ToBoolean(currentItem['GOOGLEAPPS_OAUTH_ENABLED'])
								? <span id='lblGoogleAppsAuthorized' style={ {marginRight: '10px'} }>{ L10n.Term('OAuth.LBL_AUTHORIZED') }</span>
								: null
								}
								{ !Sql.ToBoolean(currentItem['GOOGLEAPPS_OAUTH_ENABLED'])
								? <button className='button' onClick={ this._onGoogleAppsAuthorize    } style={ {marginRight: '10px'} }>  { L10n.Term('OAuth.LBL_AUTHORIZE_BUTTON_LABEL') }  </button>
								: null
								}
								{ Sql.ToBoolean(currentItem['GOOGLEAPPS_OAUTH_ENABLED'])
								? <button className='button' onClick={ this._onGoogleAppsDelete       } style={ {marginRight: '10px'} }>  { L10n.Term('OAuth.LBL_DELETE_BUTTON_LABEL') }  </button>
								: null
								}
								{ Sql.ToBoolean(currentItem['GOOGLEAPPS_OAUTH_ENABLED'])
								? <button className='button' onClick={ this._onGoogleAppsTest         } style={ {marginRight: '10px'} }>  { L10n.Term('OAuth.LBL_TEST_BUTTON_LABEL') }  </button>
								: null
								}
								{ Sql.ToBoolean(currentItem['GOOGLEAPPS_OAUTH_ENABLED'])
								? <button className='button' onClick={ this._onGoogleAppsRefreshToken } style={ {marginRight: '10px'} }>  { L10n.Term('OAuth.LBL_REFRESH_TOKEN_LABEL') }  </button>
								: null
								}
								<span id='lblGoogleAuthorizedStatus' className='error'>{ lblGoogleAuthorizedStatus }</span>
							</div>
						</div>
					</div>
					: null
					}
					<div style={ {marginLeft: '10px'} }>
						<table className='tabEditView' style={ {border: 'none', borderCollapse: 'collapse', padding: '4px'} }>
							<tr>
								<th colSpan={ 4 }>
									<h4><span>{ L10n.Term('EmailMan.LBL_SECURITY_TITLE') }</span></h4>
								</th>
							</tr>
							<tr>
								<td colSpan={ 4 }><span>{ L10n.Term('EmailMan.LBL_SECURITY_DESC') }</span></td>
							</tr>
							<tr>
								<td colSpan={ 2 }>&nbsp;</td>
							</tr>
							<tr>
								<td valign='top' style={ {width: '1%'} }>
									<span className='checkbox'>
										<input id='EMAIL_INBOUND_SAVE_RAW' type='checkbox' checked={ EMAIL_INBOUND_SAVE_RAW } onChange={ this.toggleEMAIL_INBOUND_SAVE_RAW } />
									</span>
								</td>
								<td>
									<span>{ L10n.Term('EmailMan.LBL_SECURITY_PRESERVE_RAW') }</span><br />
								</td>
							</tr>
							<tr>
								<td colSpan={ 2 }>&nbsp;</td>
							</tr>
							<tr>
								<td valign='top'>
									<span className='checkbox'>
										<input id='SECURITY_TOGGLE_ALL' type='checkbox' checked={ SECURITY_TOGGLE_ALL } onChange={ this.toggleAllSecurityOptions } />
									</span>
								</td>
								<td>
									<span>{ L10n.Term('EmailMan.LBL_SECURITY_TOGGLE_ALL') }</span><br />
								</td>
							</tr>
							<tr>
								<td colSpan={ 2 }>&nbsp;</td>
							</tr>
							<tr>
								<td valign='top'>
									<span className='checkbox'>
										<input id='SECURITY_OUTLOOK_DEFAULTS' type='checkbox' checked={ SECURITY_OUTLOOK_DEFAULTS } onChange={ this.setOutlookDefaults } />
									</span>
								</td>
								<td>
									<span>{ L10n.Term('EmailMan.LBL_SECURITY_OUTLOOK_DEFAULTS') }</span><br />
								</td>
							</tr>
							<tr>
								<td colSpan={ 2 }>&nbsp;</td>
							</tr>
							<tr>
								<td colSpan={ 2 }>
									{ this.BuildSecurityTable() }
								</td>
							</tr>
						</table>
					</div>
				</div>
			</div>
			);
		}
		else
		{
			return (
			<div id={ this.constructor.name + '_spinner' } style={ {textAlign: 'center'} }>
				<FontAwesomeIcon icon="spinner" spin={ true } size="5x" />
			</div>);
		}
	}
}

