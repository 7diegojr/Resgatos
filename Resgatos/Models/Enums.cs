using System.ComponentModel.DataAnnotations;

namespace Resgatos.Models;

public enum EspecieAnimal
{
    Gato = 1,
    Cachorro = 2
}

public enum SexoAnimal
{
    Macho = 1,
    [Display(Name = "Fêmea")]
    Femea = 2
}

public enum PorteAnimal
{
    Pequeno = 1,
    [Display(Name = "Médio")]
    Medio = 2,
    Grande = 3
}

public enum StatusAnimal
{
    [Display(Name = "Em avaliação")]
    EmAvaliacao = 1,
    [Display(Name = "Em tratamento")]
    EmTratamento = 2,
    [Display(Name = "Disponível")]
    Disponivel = 3,
    Reservado = 4,
    Adotado = 5
}

public enum ResultadoFiv
{
    Positivo = 1,
    Negativo = 2,
    Inconclusivo = 3
}

public enum ResultadoFelv
{
    Positivo = 1,
    Negativo = 2,
    Inconclusivo = 3
}

public enum CategoriaTransparencia
{
    [Display(Name = "Alimentação")]
    Alimentacao = 1,
    [Display(Name = "Castração")]
    Castracao = 2,
    Medicamentos = 3,
    Estrutura = 4,
    Outros = 5
}