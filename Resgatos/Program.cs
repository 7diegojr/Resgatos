using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Resgatos.Data;
using Resgatos.Services;

var builder = WebApplication.CreateBuilder(args);

// Configuração do Entity Framework Core com MySQL
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySQL(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Serviço de login do admin (validação de senha e geração de hash)
builder.Services.AddScoped<AuthService>();

// Upload e exclusão das fotos dos animais (wwwroot/uploads/animais)
builder.Services.AddScoped<UploadService>();

// Autenticação por cookie (só o admin tem login; não há cadastro de usuários)
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/admin/conta/login";       // para onde manda quem não está logado
        options.Cookie.Name = "Resgatos.Admin";
        options.Cookie.HttpOnly = true;                  // JavaScript não consegue ler o cookie
        options.Cookie.SameSite = SameSiteMode.Lax;
        // Em produção o cookie só trafega por HTTPS; no desenvolvimento (http://localhost) aceita os dois
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);  // sessão expira após 8h sem uso
        options.SlidingExpiration = true;                // e é renovada enquanto a pessoa usa o painel
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication(); // descobre QUEM é a pessoa (lê o cookie); precisa vir antes da autorização
app.UseAuthorization();  // decide se essa pessoa PODE acessar (o [Authorize])

// Rota da Area Admin: "/admin" cai em Areas/Admin/Controllers/DashboardController.
// Precisa vir ANTES da rota padrão.
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Criação automática do banco de dados e das tabelas
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await context.Database.EnsureCreatedAsync();

    // O EnsureCreated só cria o banco quando ele não existe: não adiciona colunas novas em um banco que já existe.
    // Este ajuste (idempotente) cria as colunas que foram adicionadas depois, como a da lixeira.
    await SchemaAjustes.GarantirColunasAsync(context);

    // Seeds: rodam sempre que a aplicação inicia (dotnet watch run). Todas são idempotentes:
    // não duplicam nada se os dados já existirem.

    // Administrador padrão (junin@gmail.com): só é criado se ainda não existir.
    await AdminSeed.CriarAsync(context, scope.ServiceProvider.GetRequiredService<AuthService>());

    // Dados de teste dos animais (TEMPORÁRIO: remover quando o CRUD de animais da Sprint 13 existir).
    // Não faz nada se já houver animais.
    // Para apagar os animais e recriar a seed:  dotnet run --refazer-seed-teste
    if (args.Contains("--refazer-seed-teste"))
    {
        await DadosDeTeste.RefazerAnimaisAsync(context);
    }
    else
    {
        await DadosDeTeste.PopularAnimaisAsync(context);
    }

    // Dados de teste da Transparência (TEMPORÁRIO: remover quando o CRUD da Sprint 15 existir).
    // Não faz nada se já houver lançamentos.
    // Para apagar os lançamentos e recriar a seed:  dotnet run --refazer-seed-transparencia
    if (args.Contains("--refazer-seed-transparencia"))
    {
        await DadosDeTeste.RefazerTransparenciaAsync(context);
    }
    else
    {
        await DadosDeTeste.PopularTransparenciaAsync(context);
    }
}

app.Run();