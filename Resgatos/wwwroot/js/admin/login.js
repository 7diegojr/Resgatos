// Botão de mostrar/esconder a senha na tela de login.
(function () {
    "use strict";

    var campo = document.getElementById("Senha");
    var botao = document.getElementById("login-toggle-senha");

    if (!campo || !botao) return;

    var icone = botao.querySelector("i");

    botao.addEventListener("click", function () {
        var mostrar = campo.type === "password";

        campo.type = mostrar ? "text" : "password";
        icone.className = mostrar ? "fa-solid fa-eye-slash" : "fa-solid fa-eye";
        botao.setAttribute("aria-pressed", mostrar ? "true" : "false");
        botao.setAttribute("aria-label", mostrar ? "Ocultar senha" : "Mostrar senha");

        // Devolve o foco ao campo, para quem está digitando continuar de onde parou
        campo.focus();
    });
})();
