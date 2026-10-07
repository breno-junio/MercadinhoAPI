namespace MercadoAPI.DTOs;

public record ItemVendaResponse(
    int ProdutoId, string NomeProduto, decimal PrecoUnitario, int Quantidade, decimal Subtotal);
