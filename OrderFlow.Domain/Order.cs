using OrderFlow.Domain.Entities;
using OrderFlow.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderFlow.Domain
{
    public class Order
    {
        private readonly List<OrderItem> _items = new();

        public Guid Id { get; private set; }
        public string CustomerName { get; private set; } = string.Empty;
        public decimal TotalAmount { get; private set; }
        public OrderStatus Status { get; private set; }
        public DateTime CreatedAtUtc { get; private set; }

        public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

        private Order() { }

        public Order(string customerName)
        {
            if (string.IsNullOrWhiteSpace(customerName))
                throw new ArgumentException("Customer name cannot be empty.", nameof(customerName));

            Id = Guid.NewGuid();
            CustomerName = customerName;
            Status = OrderStatus.Pending;
            CreatedAtUtc = DateTime.UtcNow;
        }

        
        public void AddItem(string productName, int quantity, decimal unitPrice)
        {
            var item = new OrderItem(productName, quantity, unitPrice);
            _items.Add(item);

            TotalAmount += item.Quantity * item.UnitPrice;
        }

        public void MarkAsCompleted()
        {
            Status = OrderStatus.Completed;
        }
    }
}
