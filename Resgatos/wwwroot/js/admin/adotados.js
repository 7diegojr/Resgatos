// Admin > Adotados.
//   1. Filtros rápidos (Espécie e Termo) aplicam assim que a escolha muda, sem botão.
//      A busca por nome continua sendo enviada com Enter (ou na lupa), mantendo os filtros aplicados.
//   2. Foto do card quadrada: a altura do card vem do conteúdo, então o JS mede o corpo do card e grava em
//      --foto-lado; o CSS usa esse valor como largura da foto (largura = altura = quadrada).
(function () {
    "use strict";

    /* ============ 1. Filtros rápidos ============ */
    var form = document.getElementById("adotados-busca");
    if (form) {
        // O custom-select dispara "change" no <select> escondido
        form.querySelectorAll("select").forEach(function (campo) {
            campo.addEventListener("change", function () { form.submit(); });
        });
    }

    /* ============ 2. Foto quadrada ============ */
    var cards = document.querySelectorAll(".adotado-card");
    if (cards.length === 0) return;

    function quadrar(card) {
        var corpo = card.querySelector(".adotado-corpo");
        if (corpo) card.style.setProperty("--foto-lado", corpo.offsetHeight + "px");
    }

    // Acompanha mudanças de altura (tela redimensionada, fonte carregando, colunas que quebram de linha)
    if (typeof ResizeObserver !== "undefined") {
        var observador = new ResizeObserver(function (entradas) {
            window.requestAnimationFrame(function () {
                entradas.forEach(function (entrada) {
                    var card = entrada.target.closest(".adotado-card");
                    if (card) quadrar(card);
                });
            });
        });

        cards.forEach(function (card) {
            var corpo = card.querySelector(".adotado-corpo");
            if (!corpo) return;
            quadrar(card);
            observador.observe(corpo);
        });
    } else {
        cards.forEach(quadrar);
        window.addEventListener("resize", function () { cards.forEach(quadrar); });
    }
})();
