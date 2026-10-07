using MercadoAPI.DTOs;
using MercadoAPI.Modelos;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();




var produtos = new List<Produto>
{
    new Produto(1, "Arroz", 28.90m),
    new Produto(2, "Feijão", 7.50m),
    new Produto(3, "Óleo", 6.80m),
    new Produto(4, "Leite", 4.99m),
    new Produto(5, "Café", 16.50m)
};

var estoque = new Dictionary<int, int>
{
    { 1, 50 },  
    { 2, 30 },  
    { 3, 100 }, 
    { 4, 12 },  
    { 5, 0 }
};
var vendas = new List<VendaResponse>
{
    new VendaResponse(
        Id: 1,
        DataVenda: DateTime.Now.AddDays(-1),
        Itens: new List<ItemVendaResponse>
        {
            new ItemVendaResponse(ProdutoId: 1, NomeProduto: "Arroz 5kg", PrecoUnitario: 28.90m, Quantidade: 2, Subtotal: 57.80m),
            new ItemVendaResponse(ProdutoId: 2, NomeProduto: "Feijão Carioca 1kg", PrecoUnitario: 7.50m, Quantidade: 1, Subtotal: 7.50m)
        },
        ValorTotal: 65.30m
    )
}; 

#region Endpoints de Produtos

// Listar produtos
app.MapGet("/produtos", () => Results.Ok(produtos));

// Adicionar produto
app.MapPost("/produtos", (Produto produto) =>
{
    if (produtos.Any(p => p.Id == produto.Id))
        return Results.BadRequest("Produto com este ID já existe.");

    produtos.Add(produto);
    estoque[produto.Id] = 0;
    return Results.Created($"/produtos/{produto.Id}", produto);
});

// Deletar produto
app.MapDelete("/produtos/{id:int}", (int id) =>
{
    var produto = produtos.FirstOrDefault(p => p.Id == id);
    if (produto is null) return Results.NotFound("Produto não encontrado.");

    produtos.Remove(produto);
    estoque.Remove(id);
    return Results.NoContent();
});

// Agrupar e quantidade total por produto
app.MapGet("/produtos/agrupados", () =>
{
    var resultado = produtos.Select(p => new
    {
        p.Id,
        p.Nome,
        p.Preco,
        QuantidadeEmEstoque = estoque.GetValueOrDefault(p.Id, 0)
    });

    return Results.Ok(resultado);
});

#endregion

#region Controle de Estoque

// Consultar estoque atual
app.MapGet("/estoque", () => 
{
    var armazem = estoque.Select(e => new
    {
        ProdutoId = e.Key,
        Nome = produtos.FirstOrDefault(p => p.Id == e.Key)?.Nome ?? "Desconhecido",
        Quantidade = e.Value
    });

    return Results.Ok(armazem);
});

// Entrada/Atualização de estoque
app.MapPost("/estoque/adicionar", ([FromBody] EntradaEstoqueRequest dto) =>
{
    if (!produtos.Any(p => p.Id == dto.ProdutoId))
        return Results.NotFound("Produto não cadastrado.");

    if (dto.Quantidade <= 0)
        return Results.BadRequest("A quantidade deve ser maior que zero.");

    estoque[dto.ProdutoId] = estoque.GetValueOrDefault(dto.ProdutoId, 0) + dto.Quantidade;

    return Results.Ok(new
    {
        Mensagem = "Estoque atualizado com sucesso.",
        ProdutoId = dto.ProdutoId,
        NovoEstoque = estoque[dto.ProdutoId]
    });
});

#endregion

#region Registro e Visualização de Vendas

// Registrar uma nova venda
app.MapPost("/vendas", ([FromBody] NovaVendaRequest dto) =>
{       
    var itensVenda = new List<ItemVendaResponse>();
    decimal valorTotal = 0;

    foreach (var item in dto.Itens)
    {
        var produto = produtos.First(p => p.Id == item.ProdutoId);
        estoque[item.ProdutoId] -= item.Quantidade;

        var subtotal = produto.Preco * item.Quantidade;
        valorTotal += subtotal;

        itensVenda.Add(new ItemVendaResponse(produto.Id, produto.Nome, produto.Preco, item.Quantidade, subtotal));
    }

    var venda = new VendaResponse(vendas.Count + 1, DateTime.Now, valorTotal, itensVenda);
    vendas.Add(venda);

    return Results.Created($"/vendas/{venda.Id}", venda);
});

// Visualizar todas as vendas
app.MapGet("/vendas", () => Results.Ok(vendas));

// Visualizar venda por ID
app.MapGet("/vendas/{id:int}", (int id) =>
{
    var venda = vendas.FirstOrDefault(v => v.Id == id);
    return venda is not null ? Results.Ok(venda) : Results.NotFound("Venda não encontrada.");
});

#endregion

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.Run();