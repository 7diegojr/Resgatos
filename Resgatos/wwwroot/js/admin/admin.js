// Menu lateral do admin: no celular ele fica escondido e abre pelo botão da barra de topo.
(function () {
    "use strict";

    var body = document.body;
    var botaoAbrir = document.getElementById("admin-menu-toggle");
    var botaoFechar = document.getElementById("admin-menu-close");
    var fundo = document.getElementById("admin-overlay");
    var sidebar = document.getElementById("admin-sidebar");

    if (!botaoAbrir || !sidebar) return;

    function abrir() {
        body.classList.add("sidebar-open");
        botaoAbrir.setAttribute("aria-expanded", "true");
    }

    function fechar() {
        body.classList.remove("sidebar-open");
        botaoAbrir.setAttribute("aria-expanded", "false");
    }

    botaoAbrir.addEventListener("click", abrir);
    if (botaoFechar) botaoFechar.addEventListener("click", fechar);
    if (fundo) fundo.addEventListener("click", fechar);

    // Esc fecha o menu
    document.addEventListener("keydown", function (e) {
        if (e.key === "Escape") fechar();
    });

    // Ao escolher uma página (link na mesma aba), fecha o menu
    sidebar.querySelectorAll("a:not([target])").forEach(function (link) {
        link.addEventListener("click", fechar);
    });
})();
