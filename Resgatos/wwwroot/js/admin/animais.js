// Lista de animais do admin: os seletores (espécie e ordem) filtram assim que são trocados,
// sem precisar clicar em "Filtrar". A busca por nome continua sendo enviada com Enter ou no botão.
// Sem JavaScript tudo continua funcionando: o botão "Filtrar" envia o formulário.
(function () {
    "use strict";

    var formulario = document.getElementById("animais-filtros");
    if (!formulario) return;

    formulario.querySelectorAll("select[data-auto-submit]").forEach(function (campo) {
        campo.addEventListener("change", function () {
            formulario.submit();
        });
    });
})();
