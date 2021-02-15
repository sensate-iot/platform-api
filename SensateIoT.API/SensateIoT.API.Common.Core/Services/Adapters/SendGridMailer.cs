﻿/*
 * Email sender service using SendGrind.com.
 * 
 * @author Michel Megens
 * @email  michel.megens@sonatolabs.com
 */

using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;
using SensateIoT.API.Common.Config.Settings;
using SensateIoT.API.Common.Data.Dto.Generic;

namespace SensateIoT.API.Common.Core.Services.Adapters
{
	public class SendGridMailer : IEmailSender
	{
		private SendGridAuthOptions _options;

		public SendGridMailer(IOptions<SendGridAuthOptions> opts)
		{
			this._options = opts.Value;
		}

		public async Task SendEmailAsync(string recip, string subj, string body)
		{
			await this.Execute(this._options.Key, recip, subj, body);
		}

		public async Task SendEmailAsync(string recip, string subj, EmailBody body)
		{
			await this.Execute(recip, subj, body);
		}

		private Task<Response> Execute(string key, string recip, string subj, string body)
		{
			SendGridMessage msg;
			var client = new SendGridClient(key);

			msg = new SendGridMessage() {
				From = new EmailAddress(this._options.From, this._options.FromName),
				Subject = subj,
				PlainTextContent = body,
				HtmlContent = body,
			};

			msg.AddTo(recip);
			return client.SendEmailAsync(msg);
		}

		private Task<Response> Execute(string recip, string subj, EmailBody body)
		{
			SendGridMessage msg;
			var client = new SendGridClient(this._options.Key);

			body.AddRecip(recip);
			body.FromEmail = this._options.From;
			body.FromName = this._options.FromName;
			body.Subject = subj;

			msg = body.BuildSendgridMessage();

			return client.SendEmailAsync(msg);

		}
	}
}
