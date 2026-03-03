using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Map
{
    /// <summary>
    /// Supported combat status types.
    /// </summary>
    public enum StatusType
    {
        Strength,
        Weak,
        Vulnerable
    }

    /// <summary>
    /// Unified status container for battle actors.
    /// </summary>
    [Serializable]
    public class StatusSet
    {
        private readonly Dictionary<StatusType, int> _values = new Dictionary<StatusType, int>();

        public int Get(StatusType type)
        {
            return _values.TryGetValue(type, out var value) ? value : 0;
        }

        public void Add(StatusType type, int amount)
        {
            if (amount == 0)
            {
                return;
            }

            int next = Get(type) + amount;
            if (next <= 0)
            {
                _values.Remove(type);
            }
            else
            {
                _values[type] = next;
            }
        }

        public void Decrease(StatusType type, int amount)
        {
            Add(type, -Math.Abs(amount));
        }

        public void TickOwnerTurnEnd()
        {
            Decrease(StatusType.Weak, 1);
            Decrease(StatusType.Vulnerable, 1);
        }

        public string ToDisplayString()
        {
            var items = Enum.GetValues(typeof(StatusType))
                .Cast<StatusType>()
                .Select(t => $"{t}:{Get(t)}")
                .Where(s => !s.EndsWith(":0"))
                .ToList();

            return items.Count == 0 ? "None" : string.Join("  ", items);
        }
    }
}
