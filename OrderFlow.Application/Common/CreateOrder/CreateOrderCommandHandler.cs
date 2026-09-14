using MediatR;
using OrderFlow.Application.Common.Interfaces;
using OrderFlow.Domain;
using OrderFlow.Domain.Entities;

namespace OrderFlow.Application.Features.Orders.Commands.CreateOrder;

public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, Guid>
{
    private readonly IApplicationDbContext _context;

    public CreateOrderCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
               if (request.Items == null || !request.Items.Any())
        {
            throw new ArgumentException("Order must contain at least one item.");
        }


        var order = new Order(request.CustomerName);

        
        foreach (var item in request.Items)
        {
            order.AddItem(item.ProductName, item.Quantity, item.UnitPrice);
        }

       
        _context.Orders.Add(order);
        await _context.SaveChangesAsync(cancellationToken);

        
        return order.Id;
    }
}