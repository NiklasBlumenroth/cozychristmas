using System.Collections.Generic;

namespace CozySanta.Core.Carry
{
    /// <summary>
    /// Reiner LIFO-Tragstapel mit Gewichtskapazität. Einzige Autorität über Reihenfolge und Gewicht;
    /// keine Unity-Abhängigkeit, vollständig unit-testbar (Prinzip IX).
    /// </summary>
    public sealed class CarryStack
    {
        private readonly List<CarryItem> _items = new List<CarryItem>();

        public CarryStack(float capacity)
        {
            Capacity = capacity;
        }

        /// <summary>Maximale Traglast (kg); setzbar für spätere Skill-Upgrades (F6).</summary>
        public float Capacity { get; set; }

        /// <summary>Aktuell getragene Einträge in Aufnahmereihenfolge: Index 0 = ältestes, letztes = zuletzt aufgenommen.</summary>
        public IReadOnlyList<CarryItem> Items => _items;

        public int Count => _items.Count;

        /// <summary>Summe der getragenen Gewichte.</summary>
        public float TotalWeight { get; private set; }

        /// <summary>True, wenn ein Objekt mit <paramref name="weight"/> noch in die Traglast passt (Grenze inklusive).</summary>
        public bool CanPickUp(float weight)
        {
            return TotalWeight + weight <= Capacity;
        }

        /// <summary>Legt das Objekt oben auf, wenn die Traglast es zulässt.</summary>
        public bool TryPush(CarryItem item)
        {
            if (!CanPickUp(item.Weight))
            {
                return false;
            }

            _items.Add(item);
            TotalWeight += item.Weight;
            return true;
        }

        /// <summary>Entnimmt das zuletzt aufgenommene Objekt (LIFO).</summary>
        public bool TryPop(out CarryItem item)
        {
            if (_items.Count == 0)
            {
                item = default;
                return false;
            }

            var lastIndex = _items.Count - 1;
            item = _items[lastIndex];
            _items.RemoveAt(lastIndex);
            TotalWeight -= item.Weight;
            return true;
        }

        /// <summary>Liest das oberste (zuletzt aufgenommene) Objekt ohne Entnahme.</summary>
        public bool TryPeek(out CarryItem item)
        {
            if (_items.Count == 0)
            {
                item = default;
                return false;
            }

            item = _items[_items.Count - 1];
            return true;
        }
    }
}
