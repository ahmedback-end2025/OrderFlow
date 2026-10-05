using MediatR;

namespace OrderFlow.Application.Common.GetOrderById;

public record OrderItemDetailsDto(string ProductName, int Quantity, decimal UnitPrice);

public record OrderDetailsDto(
    Guid Id,
    string CustomerName,
    decimal TotalAmount,
    string Status,
    DateTime CreatedAtUtc,
    List<OrderItemDetailsDto> Items);

public record GetOrderByIdQuery(Guid Id) : IRequest<OrderDetailsDto?>;