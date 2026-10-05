using MediatR;
using Microsoft.AspNetCore.Mvc;
using OrderFlow.Application.Common.GetOrderById;
using OrderFlow.Application.Features.Orders.Commands.CreateOrder;
using OrderFlow.Application.Common.GetOrderById;
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


    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetOrderById(Guid id, CancellationToken cancellationToken)
    {
        var query = new GetOrderByIdQuery(id);
        var order = await _sender.Send(query, cancellationToken);

        if (order == null)
            return NotFound(new { Message = $"Order with ID {id} not found." });

        return Ok(order);
    }

}