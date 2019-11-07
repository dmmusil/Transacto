using System.Text.Json;
using EventStore.Grpc;
using Transacto.Application;
using Transacto.Framework;
using Transacto.Infrastructure;
using Transacto.Messages;

namespace Transacto.Modules {
    public class ChartOfAccountsModule : CommandHandlerModule {
        public ChartOfAccountsModule(EventStoreGrpcClient eventStore, JsonSerializerOptions serializerOptions) {
            Build<DefineAccount>()
                .Log()
                .UnitOfWork(eventStore, serializerOptions)
                .Handle((_, ct) => {
                    var (unitOfWork, command) = _;
                    var handlers =
                        new ChartOfAccountsHandlers(new ChartOfAccountsEventStoreRepository(eventStore, unitOfWork));

                    return handlers.Handle(command, ct);
                });

            Build<DeactivateAccount>()
                .Log()
                .UnitOfWork(eventStore, serializerOptions)
                .Handle((_, ct) => {
                    var (unitOfWork, command) = _;
                    var handlers =
                        new ChartOfAccountsHandlers(new ChartOfAccountsEventStoreRepository(eventStore, unitOfWork));

                    return handlers.Handle(command, ct);
                });

            Build<ReactivateAccount>()
                .Log()
                .UnitOfWork(eventStore, serializerOptions)
                .Handle((_, ct) => {
                    var (unitOfWork, command) = _;
                    var handlers =
                        new ChartOfAccountsHandlers(new ChartOfAccountsEventStoreRepository(eventStore, unitOfWork));

                    return handlers.Handle(command, ct);
                });

            Build<RenameAccount>()
                .Log()
                .UnitOfWork(eventStore, serializerOptions)
                .Handle((_, ct) => {
                    var (unitOfWork, command) = _;
                    var handlers =
                        new ChartOfAccountsHandlers(new ChartOfAccountsEventStoreRepository(eventStore, unitOfWork));

                    return handlers.Handle(command, ct);
                });
        }
    }
}
