using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Resgatos.Data;
using Resgatos.Helpers;
using Resgatos.Models;
using Resgatos.ViewModels;

namespace Resgatos.Controllers;

public class HomeController : Controller
{
    private readonly AppDbContext _context;

    public HomeController(AppDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Sobre()
    {
        return View();
    }

    public async Task<IActionResult> Adocoes(AnimalFiltroViewModel filtro)
    {
        // Só aparecem no site os animais disponíveis
        var consulta = _context.Animais
            .AsNoTracking()
            .Where(a => a.Status == StatusAnimal.Disponivel);

        // Cada grupo só filtra se tiver algo marcado (nada marcado = "todos")
        if (filtro.Especies.Count > 0)
            consulta = consulta.Where(a => filtro.Especies.Contains(a.Especie));

        if (filtro.Sexos.Count > 0)
            consulta = consulta.Where(a => filtro.Sexos.Contains(a.Sexo));

        if (filtro.FaixasEtarias.Count > 0)
            consulta = consulta.Where(a => filtro.FaixasEtarias.Contains(a.FaixaEtaria));

        if (filtro.Portes.Count > 0)
            consulta = consulta.Where(a => filtro.Portes.Contains(a.Porte));

        var animais = await consulta
            .OrderBy(a => a.Nome)
            .Select(a => new AnimalCardViewModel
            {
                IdAnimal = a.IdAnimal,
                Nome = a.Nome,
                Especie = a.Especie,
                Sexo = a.Sexo,
                Porte = a.Porte,
                IdadeEstimada = a.IdadeEstimada,
                // Foto de capa; se nenhuma foi marcada como capa, usa a primeira
                FotoCapa = a.Fotos
                    .OrderByDescending(f => f.Capa)
                    .ThenBy(f => f.IdAnimalFoto)
                    .Select(f => f.Foto)
                    .FirstOrDefault()
            })
            .ToListAsync();

        // O link do WhatsApp é montado em C# (não dá para fazer isso dentro da consulta SQL)
        foreach (var animal in animais)
            animal.LinkWhatsApp = WhatsAppHelper.LinkConhecerAnimal(animal);

        return View(new AdocoesViewModel { Filtro = filtro, Animais = animais });
    }

    public IActionResult Doacoes()
    {
        return View();
    }

    public IActionResult Contatos()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
