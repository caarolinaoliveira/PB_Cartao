using PB.Cartao.Application.Services;
using PB.Cartao.Application.Response;
using PB.Cartao.Application.Events;
using PB.Cartao.Application.Interfaces;
using System.Net;
using Microsoft.AspNetCore.Mvc;


namespace PB.Cartao.Presentation.Controllers
{
    [ApiController]
    [Route("api/v1/Cartoes")]
    public class CartaoController : ControllerBase
    {
        private readonly ICartaoService _cartaoService;

        public CartaoController(ICartaoService cartaoService)
        {
            _cartaoService = cartaoService;
            
        }

        [HttpGet("{idCliente}")]
        [ProducesResponseType(typeof(CartaoResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> ObterCartaoPorIdCliente (Guid id)
        {
            var cartoes = await _cartaoService.BuscarCartaoPorIdCliente(id);

            if (cartoes == null || !cartoes.Any())
               return NotFound("Nenhum cartão encontrado para o cliente.");

            return Ok(cartoes);
        }

    }
    
}