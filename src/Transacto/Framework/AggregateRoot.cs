using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Transacto.Framework {
    public abstract class AggregateRoot {
        private readonly IDictionary<Type, Action<object>> _router;
        private readonly IList<object> _history;

        protected AggregateRoot() {
            _history = new List<object>();
            _router = new Dictionary<Type, Action<object>>();
        }

        public async ValueTask LoadFromHistory(IAsyncEnumerable<object> events) {
            await foreach (var e in events) {
                Apply(e);
            }

            MarkChangesAsCommitted();
        }

        public void LoadFromHistory(IEnumerable<object> events) {
            foreach (var e in events) {
                Apply(e);
            }

            MarkChangesAsCommitted();
        }

        public void MarkChangesAsCommitted() => _history.Clear();
        public IEnumerable<object> GetChanges() => _history.AsEnumerable();
        protected void Register<T>(Action<T> apply) => _router.Add(typeof(T), e => apply((T)e));
        public bool HasChanges => _history.Count > 0;

        protected void Apply(object e) {
            if (_router.TryGetValue(e.GetType(), out var handle)) {
                handle(e);
            }
            _history.Add(e);
        }
    }
}