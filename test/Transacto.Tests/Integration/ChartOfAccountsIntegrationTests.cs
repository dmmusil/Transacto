using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Transacto.Domain;
using Transacto.Framework;
using Transacto.Infrastructure;
using Transacto.Messages;
using Xunit;

namespace Transacto.Integration {
	public class ChartOfAccountsIntegrationTests : IDisposable {
		private readonly TestServer _testServer;
		private readonly HttpClient _httpClient;

		public ChartOfAccountsIntegrationTests() {
			_testServer = new TestServer(new WebHostBuilder()
				.Configure(app => app.UseTransacto())
				.ConfigureServices(s => s
					.AddEventStoreClient(settings => {
						settings.OperationOptions.ThrowOnAppendFailure = true;
						settings.CreateHttpMessageHandler = () => new SocketsHttpHandler {
							SslOptions = {
								RemoteCertificateValidationCallback = delegate {
									return true;
								}
							}
						};
					})
					.AddTransacto(MessageTypeMapper.Create())));
			_httpClient = _testServer.CreateClient();
		}

		[Fact]
		public async Task Somewthing() {
			var accounts = GetChartOfAccounts();

			foreach (var (accountNumber, accountName) in accounts.OrderBy(_ => Guid.NewGuid())) {
				await _httpClient.SendCommand("/chart-of-accounts", new DefineAccount {
					AccountName = accountName.ToString(),
					AccountNumber = accountNumber.ToInt32()
				}, TransactoSerializerOptions.CommandSerializerOptions());
			}

			await Task.Delay(500);

			using var response = await _httpClient.GetAsync("/chart-of-accounts");

			var chartOfAccounts = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());

			using var resultEnumerator = chartOfAccounts.RootElement.EnumerateObject();
			using var expectEnumerator = accounts.OrderBy(x => x.Item1.ToInt32()).GetEnumerator();

			while (expectEnumerator.MoveNext() && resultEnumerator.MoveNext()) {
				Assert.Equal(expectEnumerator.Current.Item1.ToString(), resultEnumerator.Current.Name);
				Assert.Equal(expectEnumerator.Current.Item2.ToString(), resultEnumerator.Current.Value.ToString());
			}

			Assert.False(expectEnumerator.MoveNext());
			Assert.False(resultEnumerator.MoveNext());

			Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		}

		private static IEnumerable<(AccountNumber, AccountName)> GetChartOfAccounts() {
			yield return (new AccountNumber(1000), new AccountName("Bank Checking Account"));
			yield return (new AccountNumber(1050), new AccountName("Bank Savings Account"));
			yield return (new AccountNumber(1200), new AccountName("Accounts Receivable"));
			yield return (new AccountNumber(2000), new AccountName("Accounts Payable"));
			yield return (new AccountNumber(3000), new AccountName("Opening Balance Equity"));
			yield return (new AccountNumber(3900), new AccountName("Retained Earnings"));
			yield return (new AccountNumber(4000), new AccountName("Sales Income"));
			yield return (new AccountNumber(5000), new AccountName("Cost of Goods Sold"));
		}

		public void Dispose() {
			_httpClient?.Dispose();
			_testServer?.Dispose();
		}
	}

	[DataContract]
	internal class BusinessTransaction : IBusinessTransaction {
		[DataMember(Name = "transactionId")] public Guid TransactionId { get; set; }
		[DataMember(Name = "referenceNumber")] public int ReferenceNumber { get; set; }

		public GeneralLedgerEntry GetGeneralLedgerEntry(PeriodIdentifier period, DateTimeOffset createdOn) {
			var entry = GeneralLedgerEntry.Create(new GeneralLedgerEntryIdentifier(TransactionId),
				new GeneralLedgerEntryNumber($"t-{ReferenceNumber}"),
				period, createdOn);

			entry.ApplyDebit(new Debit(new AccountNumber(1000), new Money(5m)));
			entry.ApplyCredit(new Credit(new AccountNumber(3000), new Money(5m)));
			entry.ApplyTransaction(this);

			return entry;
		}

		public IEnumerable<object> GetAdditionalChanges() {
			yield return this;
		}

		public int? Version { get; set; }
	}

	public class BusinessTransactionIntegrationTests : IDisposable {
		private readonly TestServer _testServer;
		private readonly HttpClient _httpClient;

		public BusinessTransactionIntegrationTests() {
			_testServer = new TestServer(new WebHostBuilder()
				.Configure(app => app.UseTransacto().Map("/transactions", inner => inner.UseRouting().UseEndpoints(
					e => e.MapBusinessTransaction<BusinessTransaction>(string.Empty))))
				.ConfigureServices(s => s
					.AddEventStoreClient(settings => {
						settings.OperationOptions.ThrowOnAppendFailure = true;
						settings.CreateHttpMessageHandler = () => new SocketsHttpHandler {
							SslOptions = {
								RemoteCertificateValidationCallback = delegate {
									return true;
								}
							}
						};
					})
					.AddTransacto(
						MessageTypeMapper.Create(
							new MessageTypeMapper(new[] {typeof(BusinessTransaction)})))));
			_httpClient = _testServer.CreateClient();
		}

		[Fact]
		public async Task Somewthing() {
			var now = DateTimeOffset.UtcNow;
			var period = new PeriodIdentifier(now.Month, now.Year);
			await _httpClient.SendCommand("/transactions", new PostGeneralLedgerEntry {
				BusinessTransaction = new BusinessTransaction {
					TransactionId = Guid.NewGuid(),
					ReferenceNumber = 1,
					Version = 1
				},
				Period = period.ToDto(),
				CreatedOn = now
			}, TransactoSerializerOptions.CommandSerializerOptions(typeof(BusinessTransaction)));
		}

		public void Dispose() {
			_httpClient?.Dispose();
			_testServer?.Dispose();
		}
	}
}