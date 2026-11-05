using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Resgatos.Helpers;

public static class EnumExtensions
{
    // Devolve o [Display(Name = "...")] do valor do enum (ex.: SexoAnimal.Femea -> "Fêmea").
    // Se o valor não tiver [Display], devolve o próprio nome (ex.: Macho -> "Macho").
    public static string GetDisplayName(this Enum valor)
    {
        var membro = valor.GetType().GetMember(valor.ToString()).FirstOrDefault();
        return membro?.GetCustomAttribute<DisplayAttribute>()?.GetName() ?? valor.ToString();
    }
}
