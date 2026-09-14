using MediatR;

namespace OrderFlow.Application.Features.Orders.Commands.CreateOrder;

public record OrderItemDto(string ProductName, int Quantity, decimal UnitPrice);

public record CreateOrderCommand(string CustomerName, List<OrderItemDto> Items) : IRequest<Guid>;