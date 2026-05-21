using CatalogApi.Models;
using CatalogApi.Services;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Services
// ---------------------------------------------------------------------------
builder.Services.AddApplicationInsightsTelemetry();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<CosmosDbService>();
builder.Services.AddSingleton<OrderValidationService>();
builder.Services.AddSingleton<CatalogSearchService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Catalog API v1");
    c.RoutePrefix = "swagger";
    c.DocumentTitle = "Catalog API - Swagger UI";
});

// ---------------------------------------------------------------------------
// Cosmos DB initialization (non-blocking — app starts even if DB is unreachable)
// ---------------------------------------------------------------------------
app.Lifetime.ApplicationStarted.Register(() =>
{
    _ = Task.Run(async () =>
    {
        try
        {
            var cosmosDb = app.Services.GetRequiredService<CosmosDbService>();
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            await cosmosDb.InitializeAsync().WaitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            app.Logger.LogWarning(
                "Cosmos DB initialization timed out during background startup. The app remains online and will retry on request path.");
        }
        catch (Exception ex)
        {
            app.Logger.LogWarning(ex,
                "Cosmos DB initialization failed in background. The app remains online, but database requests may return errors.");
        }
    });
});

// ---------------------------------------------------------------------------
// Endpoints
// ---------------------------------------------------------------------------

// Home page - quick status and navigation links for key API endpoints
app.MapGet("/", () =>
{
        const string html = """
<!doctype html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Catalog API</title>
    <style>
        body { font-family: Segoe UI, Arial, sans-serif; margin: 2rem; line-height: 1.5; }
        h1 { margin-bottom: .25rem; }
        p { color: #333; }
        ul { padding-left: 1.25rem; }
        code { background: #f2f2f2; padding: .15rem .35rem; border-radius: 4px; }
        .card { max-width: 760px; border: 1px solid #ddd; border-radius: 10px; padding: 1rem 1.25rem; }
    </style>
</head>
<body>
    <div class="card">
        <h1>Catalog API is running</h1>
        <p>Use the links below to test core endpoints.</p>
        <ul>
            <li><a href="/health">GET /health</a> - service and Cosmos connectivity check</li>
            <li><a href="/products">GET /products</a> - list products (add <code>?hideOutOfStock=true</code> to filter)</li>
            <li><a href="/search">Product Search</a> - browse products with out-of-stock indicators</li>
            <li><a href="/orders">GET /orders</a> - list orders</li>
            <li><a href="/swagger">Swagger UI</a> - interactive API explorer</li>
            <li><a href="/swagger/v1/swagger.json">OpenAPI JSON</a> - API schema</li>
        </ul>
        <p>Write endpoints:</p>
        <ul>
            <li><code>POST /products</code></li>
            <li><code>POST /orders</code></li>
        </ul>
    </div>
</body>
</html>
""";

        return Results.Content(html, "text/html");
});

// Health check — validates Cosmos DB connectivity
app.MapGet("/health", async (CosmosDbService db) =>
{
    var healthy = await db.CheckHealthAsync();
    return healthy
        ? Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow })
        : Results.Json(new { status = "unhealthy", timestamp = DateTime.UtcNow }, statusCode: 503);
});

// GET /products — list all products, out-of-stock items sorted to bottom; supports ?hideOutOfStock=true
app.MapGet("/products", async (CosmosDbService db, CatalogSearchService search, bool hideOutOfStock = false) =>
{
    var products = await db.GetProductsAsync();
    var result = search.FilterAndSort(products, hideOutOfStock);
    return Results.Ok(result);
});

// GET /products/{id} — get a single product by id
app.MapGet("/products/{id}", async (string id, CosmosDbService db) =>
{
    var product = await db.GetProductByIdAsync(id);
    return product is not null ? Results.Ok(product) : Results.NotFound();
});

// POST /products — create a new product
app.MapPost("/products", async (Product product, CosmosDbService db) =>
{
    var created = await db.CreateProductAsync(product);
    return Results.Created($"/products/{created.Id}", created);
});

// GET /orders — list all orders
app.MapGet("/orders", async (CosmosDbService db) =>
{
    var orders = await db.GetOrdersAsync();
    return Results.Ok(orders);
});

// GET /orders/{id} — get a single order by id
app.MapGet("/orders/{id}", async (string id, CosmosDbService db) =>
{
    var order = await db.GetOrderByIdAsync(id);
    return order is not null ? Results.Ok(order) : Results.NotFound();
});

// POST /orders — create a new order (can trigger CPU spike in strict mode)
app.MapPost("/orders", async (Order order, CosmosDbService db, OrderValidationService validator) =>
{
    var validation = validator.ValidateOrder(order);
    if (!validation.IsValid)
        return Results.BadRequest(new { error = validation.Error });

    var created = await db.CreateOrderAsync(order);
    return Results.Created($"/orders/{created.Id}", created);
});

// GET /search — product search page with out-of-stock badge and filter
app.MapGet("/search", async (CosmosDbService db, CatalogSearchService search, bool hideOutOfStock = false) =>
{
    var products = await db.GetProductsAsync();
    var sorted = search.FilterAndSort(products, hideOutOfStock).ToList();

    var checkedAttr = hideOutOfStock ? " checked" : "";

    var cards = new System.Text.StringBuilder();
    foreach (var p in sorted)
    {
        var badge = p.AvailableStock <= 0
            ? """<span class="badge bg-secondary ms-2">Out of Stock</span>"""
            : string.Empty;
        var stockText = p.AvailableStock > 0
            ? $"In stock: {p.AvailableStock}"
            : "Unavailable";
        cards.Append($"""
            <div class="col-sm-6 col-lg-4 mb-4">
              <div class="card h-100">
                <div class="card-body">
                  <h5 class="card-title">{System.Net.WebUtility.HtmlEncode(p.Name)}{badge}</h5>
                  <h6 class="card-subtitle mb-2 text-muted">{System.Net.WebUtility.HtmlEncode(p.Category)}</h6>
                  <p class="card-text">{System.Net.WebUtility.HtmlEncode(p.Description)}</p>
                  <p class="card-text"><strong>${p.Price:F2}</strong></p>
                  <p class="card-text"><small class="text-muted">{stockText}</small></p>
                </div>
              </div>
            </div>
            """);
    }

    var html = $"""
        <!doctype html>
        <html lang="en">
        <head>
            <meta charset="utf-8" />
            <meta name="viewport" content="width=device-width, initial-scale=1" />
            <title>Product Search - Catalog API</title>
            <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css" rel="stylesheet" />
        </head>
        <body class="p-4">
            <div class="container">
                <h1 class="mb-3">Product Search</h1>
                <form method="get" action="/search" class="mb-4">
                    <div class="form-check mb-2">
                        <input class="form-check-input" type="checkbox" name="hideOutOfStock" value="true" id="hideOutOfStock"{checkedAttr} />
                        <label class="form-check-label" for="hideOutOfStock">Hide out-of-stock items</label>
                    </div>
                    <button type="submit" class="btn btn-primary">Apply Filter</button>
                    <a href="/search" class="btn btn-outline-secondary ms-2">Reset</a>
                </form>
                <div class="row">
                    {cards}
                </div>
            </div>
        </body>
        </html>
        """;

    return Results.Content(html, "text/html");
});

app.Run();
