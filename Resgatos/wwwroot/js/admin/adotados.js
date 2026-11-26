// Admin > Adotados: os filtros rápidos (Espécie e Termo) aplicam assim que a escolha muda, sem botão.
// A busca por nome continua sendo enviada com Enter (ou na lupa), mantendo os filtros aplicados.
(function () {
    "use strict";

    var form = document.getElementById("adotados-busca");
    if (!form) return;

    // O custom-select dispara "change" no <select> escondido
    form.querySelectorAll("select").forEach(function (campo) {
        campo.addEventListener("change", function () { form.submit(); });
    });
})();
