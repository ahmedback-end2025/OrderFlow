using MediatR;
using Microsoft.AspNetCore.Mvc;
using OrderFlow.Application.Features.Orders.Commands.CreateOrder;

namespace OrderFlow.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly ISender _sender;

    public OrdersController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderCommand command, CancellationToken cancellationToken)
    {
        var orderId = await _sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(CreateOrder), new { id = orderId }, new { OrderId = orderId });
    }
}