using System;
using System.Collections.Generic;
using System.Linq;
using Transacto.Domain;
using Transacto.Framework;

namespace SomeCompany.PurchaseOrders {
	public partial class PurchaseOrder : IBusinessTransaction {
		public GeneralLedgerEntry GetGeneralLedgerEntry(PeriodIdentifier period, DateTimeOffset createdOn) {
			var entry = GeneralLedgerEntry.Create(
				new GeneralLedgerEntryIdentifier(PurchaseOrderId),
				new GeneralLedgerEntryNumber($"purchaseorder-{PurchaseOrderNumber}"), period, createdOn);

			var (accountsPayable, inventoryInTransit) =
				PurchaseOrderItems.Aggregate((new Credit(new AccountNumber(2150)), new Debit(new AccountNumber(1400))),
					Accumulate);

			entry.ApplyCredit(accountsPayable);
			entry.ApplyDebit(inventoryInTransit);
			entry.ApplyTransaction(this);

			return entry;
		}

		private static (Credit, Debit) Accumulate((Credit, Debit) _, PurchaseOrderItem item) {
			var (accountsPayable, inventoryInTransit) = _;
			return (accountsPayable + item.Total, inventoryInTransit + item.Total);
		}

		public IEnumerable<object> GetAdditionalChanges() {
			yield return this;
		}

		public int? Version { get; set; }
	}
}
