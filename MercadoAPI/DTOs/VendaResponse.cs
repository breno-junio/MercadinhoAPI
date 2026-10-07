namespace MercadoAPI.DTOs;

public record VendaResponse(
    int Id,
    DateTime DataVenda,
    decimal ValorTotal,
    List<ItemVendaResponse> Itens);
