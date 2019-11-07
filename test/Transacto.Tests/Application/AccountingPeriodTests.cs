using System.Threading.Tasks;
using Transacto.Domain;
using Transacto.Messages;
using Transacto.Testing;
using Xunit;

namespace Transacto.Application {
    public class AccountingPeriodTests {
        private readonly AccountingPeriodHandlers _handler;
        private readonly IFactRecorder _facts;

        public AccountingPeriodTests() {
            _facts = new FactRecorder();
            _handler = new AccountingPeriodHandlers(new AccountingPeriodTestRepository(_facts));
        }

        [Theory, AutoTransactoData]
        public Task opening_the_period(PeriodIdentifier period) =>
            new Scenario()
                .GivenNone()
                .When(new OpenAccountingPeriod {
                    Period = period.ToDto()
                })
                .Then(period.ToString(), new AccountingPeriodOpened {
                    Period = period.ToDto()
                })
                .Assert(_handler, _facts);

        [Theory, AutoTransactoData]
        public Task closing_an_open_period(PeriodIdentifier period) =>
            new Scenario()
                .Given(period.ToString(), new AccountingPeriodOpened {
                    Period = period.ToDto()
                })
                .When(new CloseAccountingPeriod {
                    Period = period.ToDto()
                })
                .Then(period.ToString(), new AccountingPeriodClosed {
                    Period = period.ToDto()
                })
                .Assert(_handler, _facts);

        [Theory, AutoTransactoData]
        public Task closing_a_closed_period(PeriodIdentifier period) =>
            new Scenario()
                .Given(period.ToString(), new AccountingPeriodOpened {
                    Period = period.ToDto()
                }, new AccountingPeriodClosed {
                    Period = period.ToDto()
                })
                .When(new CloseAccountingPeriod {
                    Period = period.ToDto()
                })
                .ThenNone()
                .Assert(_handler, _facts);
    }
}