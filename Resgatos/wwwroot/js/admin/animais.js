// Lista de animais do admin.
//   1. Filtros rápidos da barra (status, espécie e ordenação): trocar um já filtra.
//   2. Modal de filtros: o botão "Filtrar" abre; "Aplicar filtros" envia o formulário (GET) e a página recarrega já filtrada.
// Os filtros rápidos e o modal são os mesmos campos (a página recarrega com o valor aplicado nos dois).
// A busca por nome continua sendo enviada com Enter (ou na lupa), mantendo os filtros aplicados.
(function () {
    "use strict";

    /* ============ 1. Filtros rápidos ============ */
    var barra = document.getElementById("animais-busca");
    if (barra) {
        // O custom-select dispara "change" no <select> escondido
        barra.querySelectorAll("select").forEach(function (campo) {
            campo.addEventListener("change", function () { barra.submit(); });
        });
    }

    /* ============ 2. Modal de filtros ============ */
    var modal = document.getElementById("modal-filtros");
    if (!modal) return;

    var formulario = document.getElementById("form-filtros");
    var gatilho = null; // botão que abriu o modal (o foco volta para ele ao fechar)

    document.addEventListener("click", function (evento) {
        var botao = evento.target.closest("[data-abrir-filtros]");
        if (!botao) return;

        gatilho = botao;
        document.documentElement.classList.add("modal-aberto");
        modal.showModal();

        // O foco começa no primeiro filtro (o "x" de fechar é só um atalho)
        var primeiro = modal.querySelector(".form-select-trigger");
        if (primeiro) primeiro.focus();
    });

    modal.querySelectorAll("[data-fechar-filtros]").forEach(function (botao) {
        botao.addEventListener("click", function () { modal.close(); });
    });

    // Clicar no fundo escurecido (fora da caixa) fecha
    modal.addEventListener("click", function (evento) {
        if (evento.target === modal) modal.close();
    });

    // Vale para o "x", o Esc e o clique no fundo: o que foi mexido e não aplicado é descartado,
    // e o modal reabre sempre mostrando os filtros que realmente estão valendo
    modal.addEventListener("close", function () {
        document.documentElement.classList.remove("modal-aberto");
        formulario.reset();
        if (gatilho) gatilho.focus();
    });
})();
