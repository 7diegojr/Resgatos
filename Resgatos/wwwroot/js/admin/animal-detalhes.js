// Detalhes do animal: clicar numa miniatura troca a foto grande.
(function () {
    "use strict";

    var principal = document.getElementById("det-foto-principal");
    var seloCapa = document.getElementById("det-foto-capa");
    var miniaturas = document.querySelectorAll(".det-miniatura");
    if (!principal || miniaturas.length === 0) return;

    miniaturas.forEach(function (botao) {
        botao.addEventListener("click", function () {
            principal.src = botao.getAttribute("data-foto");

            miniaturas.forEach(function (outro) {
                var ativa = outro === botao;
                outro.classList.toggle("is-ativa", ativa);
                outro.setAttribute("aria-pressed", ativa ? "true" : "false");
            });

            // O selo "Capa" só aparece quando a foto exibida é a capa
            if (seloCapa) seloCapa.hidden = botao.getAttribute("data-capa") !== "true";
        });
    });
})();
