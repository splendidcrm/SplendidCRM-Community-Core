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
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
//using Microsoft.OpenApi.Models;
using Microsoft.Extensions.FileProviders;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Diagnostics;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SpaServices.ReactDevelopmentServer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Server.IISIntegration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;

namespace SplendidCRM
{
	public class Startup
	{
		public Startup(IConfiguration configuration)
		{
			//Console.WriteLine("Startup.constructor");
			Configuration = configuration;
		}

		public IConfiguration Configuration
		{
			get;
		}

		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices(IServiceCollection services)
		{
			//Console.WriteLine("Startup.ConfigureServices");
			HttpApplicationState Application = new HttpApplicationState(Configuration);
			// https://docs.microsoft.com/en-us/aspnet/core/security/authentication/windowsauth?view=aspnetcore-5.0&tabs=visual-studio
			services.AddAuthentication(IISDefaults.AuthenticationScheme);
			/*
			if ( Sql.ToBoolean(Configuration["Azure.SingleSignOn:Enabled"]) )
			{
				services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
					//.AddMicrosoftIdentityWebApp(Configuration.GetSection("AzureAd"));
					// https://docs.microsoft.com/en-us/answers/questions/567445/azure-ad-openidconnect-federation-using-addmicroso.html
					.AddMicrosoftIdentityWebApp(options =>
					{
						options.Instance = "https://login.microsoftonline.com/";
						options.Domain   = Configuration["Azure.SingleSignOn:AadTenantDomain"];
						options.TenantId = Configuration["Azure.SingleSignOn:AadTenantId"];
						options.ClientId = Configuration["Azure.SingleSignOn:AadClientId"];
						options.CallbackPath = "/signin-oidc";
						options.SignedOutCallbackPath = "/signout-callback-oidc";
						options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
					});
			}
			else
			*/
			{
				services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie();
				services.AddAuthentication(options =>
				{
					options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
				});
			}

			// 01/25/2022 Paul.  We are now using the Identity to take advantage of [Authorize] attribute. 
			// https://blog.johnnyreilly.com/2020/12/21/how-to-make-azure-ad-403/
			// https://docs.microsoft.com/en-us/aspnet/core/security/authentication/azure-ad-b2c?view=aspnetcore-3.1#configure-the-underlying-openidconnectoptionsjwtbearercookie-options
			services.Configure<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme, options =>
			{
				options.Events.OnRedirectToAccessDenied = new Func<RedirectContext<CookieAuthenticationOptions>, Task>(context =>
				{
					L10N L10n = new L10N(context.HttpContext);
					ExceptionDetail detail = new ExceptionDetail(L10n.Term("ACL.LBL_INSUFFICIENT_ACCESS"));
					Dictionary<string, object> error = new Dictionary<string, object>();
					error.Add("ExceptionDetail", detail);
					context.Response.StatusCode = StatusCodes.Status401Unauthorized;
					context.Response.WriteAsync(JsonSerializer.Serialize(error));
					return context.Response.CompleteAsync();
				});
				options.Events.OnRedirectToLogin = context =>
				{
					L10N L10n = new L10N(context.HttpContext);
					ExceptionDetail detail = new ExceptionDetail(L10n.Term("ACL.LBL_INSUFFICIENT_ACCESS"));
					Dictionary<string, object> error = new Dictionary<string, object>();
					error.Add("ExceptionDetail", detail);
					context.Response.StatusCode = StatusCodes.Status401Unauthorized;
					context.Response.WriteAsync(JsonSerializer.Serialize(error));
					return context.Response.CompleteAsync();
				};
			});
			// https://docs.microsoft.com/en-us/aspnet/core/fundamentals/app-state?view=aspnetcore-5.0
			services.AddHttpContextAccessor();
			services.AddMemoryCache();
			services.AddDistributedMemoryCache();
			services.AddSession(options =>
			{
				options.IdleTimeout        = TimeSpan.FromMinutes(HttpSessionState.Timeout);
				options.Cookie.HttpOnly    = true;
				options.Cookie.IsEssential = true;
			});
			// https://learn.microsoft.com/en-us/aspnet/core/signalr/hubs?view=aspnetcore-5.0#strongly-typed-hubs
			services.AddScoped<HttpSessionState>();
			services.AddScoped<Security>();
			services.AddScoped<Sql>();
			services.AddScoped<SqlProcs>();
			services.AddScoped<SplendidError>();
			services.AddScoped<XmlUtil>();
			services.AddScoped<Utils>();
			services.AddScoped<SplendidInit>();
			services.AddScoped<SplendidCache>();
			// 04/27/2024 Paul.  RestUtil uses ChatManager. 
			services.AddScoped<ChatManager>();
			// 05/25/2024 Paul.  Move credit card class above RestUtil. 
			services.AddScoped<PayPalCache>();
			services.AddScoped<PayPalRest>();
			services.AddScoped<PayPalUtils>();
			services.AddScoped<PayTraceUtils>();
			services.AddScoped<AuthorizeNetUtils>();
			services.AddScoped<RestUtil>();
			services.AddScoped<SplendidDynamic>();
			services.AddScoped<ModuleUtils.Notes>();
			services.AddScoped<ModuleUtils.Audit>();
			services.AddScoped<ModuleUtils.AuditPersonalInfo>();
			services.AddScoped<ModuleUtils.EditCustomFields>();
			services.AddScoped<ModuleUtils.Activities>();
			services.AddScoped<SplendidCRM.Crm.Users>();
			services.AddScoped<SplendidCRM.Crm.Modules>();
			services.AddScoped<SplendidCRM.Crm.Emails>();
			services.AddScoped<SplendidCRM.Crm.Images>();
			services.AddScoped<SplendidCRM.Crm.EmailImages>();
			services.AddScoped<SplendidCRM.Crm.NoteAttachments>();
			services.AddScoped<SplendidCRM.Crm.BugAttachments>();
			services.AddScoped<SplendidCRM.Crm.DocumentRevisions>();
			services.AddScoped<ActiveDirectory>();
			services.AddScoped<QueryBuilder>();

			services.AddScoped<Utils>();
			services.AddScoped<CurrencyUtils>();
			services.AddScoped<CampaignUtils>();
			services.AddScoped<MimeUtils>();
			services.AddScoped<ImapUtils>();
			services.AddScoped<PopUtils>();
			services.AddScoped<ExchangeUtils>();
			services.AddScoped<SplendidControl>();
			services.AddScoped<ImportUtils>();
			services.AddScoped<LanguagePackImport>();
			services.AddScoped<SyncError>();
			services.AddScoped<ArchiveExternalDB>();
			services.AddScoped<CurrencyUtils>();
			services.AddScoped<SplendidExport>();
			services.AddScoped<SplendidImport>();
			services.AddScoped<ArchiveUtils>();
			// 05/14/2023 Paul.  These don't support injection as constructors take parameters. 
			//services.AddScoped<CampaignUtils.SendMail>();
			//services.AddScoped<CampaignUtils.GenerateCalls>();
			//services.AddScoped<ModuleUtils.UndeleteModule>();
			// 06/18/2023 Paul.  SqlBuild requires SplendidInit, so it cannot be dependency injected. 
			//services.AddScoped<SqlBuild>();
			services.AddScoped<TwilioManager>();
			// 06/30/2023 Paul.  Add support for reporting. 
			services.AddScoped<ExchangeSecurity>();
			services.AddScoped<RdlUtil>();
			// 07/04/2023 Paul.  Add support for Sync services. 
			services.AddScoped<SyncError>();
			services.AddScoped<iCloudSync>();
			services.AddScoped<GoogleApps>();
			services.AddScoped<GoogleSync>();
			services.AddScoped<ExchangeUtils>();
			services.AddScoped<Spring.Social.Office365.Office365Sync>();
			// 07/05/2023 Paul.  ExchangeSync injects Office365Sync. 
			services.AddScoped<ExchangeSync>();
			// 07/05/2023 Paul.  EmailUtils injects ExchangeUtils, Office365Sync and GoogleApps. 
			services.AddScoped<EmailUtils>();

			// 07/04/2023 Paul.  SchedulerUtils requires ExchangeSync, iCloudSync. 
			services.AddScoped<SchedulerUtils>();
			// 07/04/2023 Paul.  ModuleUtils.Login injects GoogleApps. 
			services.AddScoped<ModuleUtils.Login>();

			services.AddControllersWithViews();
			// http://www.binaryintellect.net/articles/a1e0e49e-d4d0-4b7c-b758-84234f14047b.aspx
			// 12/23/2021 Paul.  We need to prevent UserProfile from getting converted to camel case. 
			services.AddControllers().AddJsonOptions(options =>
			{
				options.JsonSerializerOptions.PropertyNamingPolicy = null;
				// 12/31/2021 Paul.  We need to make sure that DBNull is treated as null instead of an empty object. 
				// https://github.com/dotnet/runtime/issues/418
				options.JsonSerializerOptions.Converters.Add(new DBNullConverter());
			});
			services.AddControllers(options =>
			{
				options.InputFormatters.Insert(0, new RawRequestBodyFormatter());
			});
			// In production, the React files will be served from this directory
			services.AddSpaStaticFiles(configuration =>
			{
				configuration.RootPath = "React/dist"; // "ClientApp/build";
			});
			services.AddRazorPages();
			services.AddSignalR();

			services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
			services.AddHostedService<QueuedBackgroundService>();
			services.AddHostedService<SchedulerHostedService>();
			services.AddHostedService<EmailHostedService>();
			services.AddHostedService<ArchiveHostedService>();
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
		{
			Console.WriteLine("Startup.Configure");
			Debug.WriteLine("Startup.Configure");
			app.UseExceptionHandler(appError =>
			{
				appError.Run(async context =>
				{
					context.Response.StatusCode = (int) HttpStatusCode.InternalServerError;
					context.Response.ContentType = "application/json";
					IExceptionHandlerFeature contextFeature = context.Features.Get<IExceptionHandlerFeature>();
					if ( contextFeature != null )
					{
						ExceptionDetail detail = new ExceptionDetail(contextFeature.Error);
						Dictionary<string, object> error = new Dictionary<string, object>();
						error.Add("ExceptionDetail", detail);
						await context.Response.WriteAsync(JsonSerializer.Serialize(error));
					}
					else
					{
						ExceptionDetail detail = new ExceptionDetail("Startup.UseExceptionHandler contextFeature is null");
						Dictionary<string, object> error = new Dictionary<string, object>();
						error.Add("ExceptionDetail", detail);
						await context.Response.WriteAsync(JsonSerializer.Serialize(error));
					}
				});
			});
			app.UseDefaultFiles(new DefaultFilesOptions
			{
				DefaultFileNames = new List<string>
				{
					"index.cshtml"
				}
			});
			// 12/30/2021 Paul.  We must rewrite the URL very early, otherwise it is ignored. 
			app.Use(async (context, next) =>
			{
				string sRequestPath = context.Request.Path.Value;
				Console.WriteLine("Request: " + sRequestPath);
				Debug.WriteLine("Request: " + sRequestPath);
#if false
				IHttpConnectionFeature connectionFeature = context.Features.Get<IHttpConnectionFeature>();
				string sLocalIpAddress = connectionFeature.LocalIpAddress?.ToString();
				Console.WriteLine("LocalIpAddress: " + sLocalIpAddress);
				Debug.WriteLine("LocalIpAddress: " + sLocalIpAddress);
				var hostName = System.Net.Dns.GetHostName();
				Console.WriteLine("hostName: " + hostName);
				Debug.WriteLine("hostName: " + hostName);
				var ips = await System.Net.Dns.GetHostAddressesAsync(hostName);
				foreach (var sIpAddress in ips)
				{
					Console.WriteLine("IpAddress: " + sIpAddress);
					Debug.WriteLine("IpAddress: " + sIpAddress);
				}
#endif
				if ( !sRequestPath.Contains(".") )
				{
					// 12/29/2021 Paul.  SpaDefaultPageMiddleware.cs: Rewrite all requests to the default page
					// This is not working.  Still get page not found for /Home
					// https://weblog.west-wind.com/posts/2020/Mar/13/Back-to-Basics-Rewriting-a-URL-in-ASPNET-Core
					// 07/31/2022 Paul.  Enable Angular UI. 
					if ( sRequestPath.Contains("/Angular") )
					{
						context.Request.Path = "/Angular";
					}
					// 06/20/2023 Paul.  Must not chagne SignalR requests. 
					else if ( !sRequestPath.StartsWith("/signalr_") )
					{
						context.Request.Path = "/";
					}
				}
				await next.Invoke();
				//return next();
			});
			//app.UseHttpsRedirection();
			// https://docs.microsoft.com/en-us/aspnet/core/fundamentals/static-files?view=aspnetcore-5.0
			app.UseStaticFiles();
			app.UseStaticFiles(new StaticFileOptions
			{
				FileProvider = new PhysicalFileProvider(Path.Combine(env.ContentRootPath, "Include")),
				RequestPath  = "/Include"
			});  // Include
			app.UseStaticFiles(new StaticFileOptions
			{
				FileProvider = new PhysicalFileProvider(Path.Combine(env.ContentRootPath, "App_Themes")),
				RequestPath  = "/App_Themes"
			});  // App_Themes
			// 06/10/2023 Paul.  Not working on Angular at this time. 
			/*
			app.UseStaticFiles(new StaticFileOptions
			{
				FileProvider = new PhysicalFileProvider(Path.Combine(env.ContentRootPath, "Angular/dist")),
				RequestPath  = "/Angular/dist"
			});
			*/
			/*
			app.UseStaticFiles(new StaticFileOptions
			{
				FileProvider = new PhysicalFileProvider(Path.Combine(env.ContentRootPath, "React")),
				RequestPath  = "/React"
			});  // React
			*/
			app.UseSpaStaticFiles();
			app.UseRouting();
			app.UseAuthentication();
			app.UseAuthorization();
			// https://docs.microsoft.com/en-us/aspnet/core/fundamentals/app-state?view=aspnetcore-5.0
			app.UseSession();
			// https://docs.microsoft.com/en-us/aspnet/core/fundamentals/middleware/?view=aspnetcore-5.0
			// Context is null at this point.  Must go inside Use() to get valid context. 
			app.Use(async (context, next) =>
			{
				string sRequestPath = context.Request.Path.Value;
				//Console.WriteLine("Request: " + sRequestPath);
				//Debug.WriteLine("Request: " + sRequestPath);
				if ( context.User != null )
				{
					//Console.WriteLine("User: " + context.User.Identity.Name);
					//Debug.WriteLine("User: " + context.User.Identity.Name);
				}
				if ( sRequestPath.Contains(".svc") )
				{
					HttpApplicationState Application = new HttpApplicationState();
					if ( !Sql.ToBoolean(Application["SplendidInit.InitApp"]) && !MaintenanceMiddleware.MaintenanceMode )
					{
						// https://www.thecodebuzz.com/cannot-resolve-scoped-service-from-root-provider-asp-net-core/
						IServiceScope scope = app.ApplicationServices.CreateScope();
						SplendidInit SplendidInit = scope.ServiceProvider.GetService<SplendidInit>();
						await SplendidInit.InitDatabase();
					}
					ISession Session = context.Session;
					if ( Session != null && !MaintenanceMiddleware.MaintenanceMode )
					{
						//Console.WriteLine("Session: " + Session.Id);
						//Debug.WriteLine("Session: " + Session.Id);
						//Console.WriteLine("Session Items: " + Session.Keys.Count<string>().ToString());
						//Debug.WriteLine("Session Items: " + Session.Keys.Count<string>().ToString());
						if ( Session.Keys.Count<string>() == 0 )
						{
							IServiceScope scope = app.ApplicationServices.CreateScope();
							SplendidInit SplendidInit = scope.ServiceProvider.GetService<SplendidInit>();
							SplendidInit.InitSession();
						}
					}
				}
				await next.Invoke();
			});
			app.UseMiddleware<MaintenanceMiddleware>();

			app.UseEndpoints(endpoints =>
			{
				endpoints.MapControllerRoute(name: "default", pattern: "{controller}/{action=Index}/{id?}");
				endpoints.MapRazorPages();
				// 06/18/2023 Paul.  UseEndpoints must follow InitDatabase above, otehrwise init will get skipped. 
				// https://learn.microsoft.com/en-us/aspnet/core/signalr/hubs?view=aspnetcore-5.0#strongly-typed-hubs
				// https://learn.microsoft.com/en-us/aspnet/core/tutorials/signalr?WT.mc_id=dotnet-35129-website&view=aspnetcore-5.0&tabs=visual-studio#add-the-signalr-client-library
				endpoints.MapHub<TwilioManagerHub>("/signalr_twiliohub");
				// 04/27/2024 Paul.  Adding support for ChatManager. 
				endpoints.MapHub<ChatManagerHub>("/signalr_chathub");
			});
			// 12/30/2021 Paul.  Using the standard html file does not allow for dynamic base href. 
			/*
			app.UseSpa(spa =>
			{
				spa.Options.SourcePath = "React/dist";  // "ClientApp";
				if ( env.IsDevelopment() )
				{
					//spa.UseReactDevelopmentServer(npmScript: "start");
				}
			});
			*/
		}
	}
}
