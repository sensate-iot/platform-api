﻿// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "<Pending>", Scope = "member", Target = "~M:SensateService.ApiCore.Attributes.AdminApiKeyAttribute.OnActionExecuting(Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext)")]
[assembly: SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "<Pending>", Scope = "member", Target = "~M:SensateService.ApiCore.Attributes.ValidateModelAttribute.OnActionExecuting(Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext)")]
[assembly: SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "<Pending>", Scope = "member", Target = "~F:SensateService.ApiCore.Controllers.AbstractDataController.m_links")]
[assembly: SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "<Pending>", Scope = "member", Target = "~F:SensateService.ApiCore.Controllers.AbstractDataController.m_sensors")]
[assembly: SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "<Pending>", Scope = "member", Target = "~F:SensateService.ApiCore.Controllers.AbstractDataController.m_keys")]
[assembly: SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "<Pending>", Scope = "member", Target = "~F:SensateService.ApiCore.Controllers.AbstractController._users")]
[assembly: SuppressMessage("Design", "CA1054:Uri parameters should not be strings", Justification = "<Pending>", Scope = "member", Target = "~M:SensateService.ApiCore.Middleware.ApiKeyValidationMiddleware.IsSwagger(System.String)~System.Boolean")]
[assembly: SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "<Pending>", Scope = "member", Target = "~M:SensateService.ApiCore.Middleware.ApiKeyValidationMiddleware.IsBanned(SensateService.Common.IdentityData.Models.SensateUser,SensateService.Common.IdentityData.Models.SensateRole)~System.Boolean")]
[assembly: SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "<Pending>", Scope = "member", Target = "~M:SensateService.ApiCore.Middleware.ApiKeyValidationMiddleware.IsSwagger(System.String)~System.Boolean")]
[assembly: SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "<Pending>", Scope = "member", Target = "~M:SensateService.ApiCore.Middleware.ApiKeyValidationMiddleware.RespondErrorAsync(Microsoft.AspNetCore.Http.HttpContext,SensateService.Common.Data.Enums.ReplyCode,System.String,System.Int32)~System.Threading.Tasks.Task")]
[assembly: SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "<Pending>", Scope = "member", Target = "~M:SensateService.ApiCore.Middleware.ApiKeyValidationMiddleware.Invoke(Microsoft.AspNetCore.Http.HttpContext)~System.Threading.Tasks.Task")]
[assembly: SuppressMessage("Globalization", "CA1304:Specify CultureInfo", Justification = "<Pending>", Scope = "member", Target = "~M:SensateService.ApiCore.Middleware.RequestLoggingMiddleware.ToRequestMethod(System.String)~SensateService.Common.Data.Enums.RequestMethod")]
[assembly: SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "<Pending>", Scope = "member", Target = "~M:SensateService.ApiCore.Middleware.RequestLoggingMiddleware.RespondErrorAsync(Microsoft.AspNetCore.Http.HttpContext,SensateService.Common.Data.Enums.ReplyCode,System.String,System.Int32)~System.Threading.Tasks.Task")]
[assembly: SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "<Pending>", Scope = "member", Target = "~M:SensateService.ApiCore.Middleware.RequestLoggingMiddleware.Invoke(Microsoft.AspNetCore.Http.HttpContext)~System.Threading.Tasks.Task")]
[assembly: SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "<Pending>", Scope = "member", Target = "~M:SensateService.ApiCore.Middleware.RequestLoggingMiddleware.Invoke(Microsoft.AspNetCore.Http.HttpContext)~System.Threading.Tasks.Task")]
[assembly: SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "<Pending>", Scope = "member", Target = "~M:SensateService.ApiCore.Middleware.UserFetchMiddleware.Invoke(Microsoft.AspNetCore.Http.HttpContext)~System.Threading.Tasks.Task")]
[assembly: SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "<Pending>", Scope = "member", Target = "~M:SensateService.ApiCore.Middleware.UserFetchMiddleware.Invoke(Microsoft.AspNetCore.Http.HttpContext)~System.Threading.Tasks.Task")]
[assembly: SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "<Pending>", Scope = "member", Target = "~M:SensateService.ApiCore.Middleware.WebSocketService.Invoke(Microsoft.AspNetCore.Http.HttpContext)~System.Threading.Tasks.Task")]
[assembly: SuppressMessage("Usage", "CA2208:Instantiate argument exceptions correctly", Justification = "<Pending>", Scope = "member", Target = "~M:SensateService.ApiCore.Middleware.WebSocketService.Invoke(Microsoft.AspNetCore.Http.HttpContext)~System.Threading.Tasks.Task")]
[assembly: SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "<Pending>", Scope = "member", Target = "~M:SensateService.ApiCore.Middleware.WebSocketService.Invoke(Microsoft.AspNetCore.Http.HttpContext)~System.Threading.Tasks.Task")]
[assembly: SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "<Pending>", Scope = "member")]
