// Modal de confirmação do painel + avisos rápidos (toast).
//
// Qualquer botão com data-confirmar abre o modal (veja o comentário em Views/Shared/_ModalConfirmacao.cshtml):
//   data-url    para onde o POST é enviado ao confirmar
//   data-titulo título do modal
//   data-texto  texto explicando o que vai acontecer
//   data-botao  texto do botão de confirmar (opcional)
(function () {
    "use strict";

    /* ============ Modal de confirmação ============ */
    var modal = document.getElementById("admin-modal-confirmacao");

    if (modal) {
        var formulario = document.getElementById("modal-confirmacao-form");
        var titulo = document.getElementById("modal-confirmacao-titulo");
        var texto = document.getElementById("modal-confirmacao-texto");
        var botaoOk = document.getElementById("modal-confirmacao-ok");
        var textoPadraoOk = botaoOk.textContent;
        var gatilho = null; // botão que abriu o modal (o foco volta para ele ao fechar)

        document.addEventListener("click", function (evento) {
            var botao = evento.target.closest("[data-confirmar]");
            if (!botao) return;

            evento.preventDefault();
            gatilho = botao;

            formulario.setAttribute("action", botao.getAttribute("data-url"));
            titulo.textContent = botao.getAttribute("data-titulo") || "Confirmar";
            texto.textContent = botao.getAttribute("data-texto") || "";
            botaoOk.textContent = botao.getAttribute("data-botao") || textoPadraoOk;
            botaoOk.disabled = false;

            document.documentElement.classList.add("modal-aberto");
            modal.showModal();
            // O foco começa no "Cancelar": confirmar sem querer (Enter) é o erro mais caro aqui
            modal.querySelector("[data-modal-cancelar]").focus();
        });

        modal.querySelector("[data-modal-cancelar]").addEventListener("click", function () {
            modal.close();
        });

        // Clicar no fundo escurecido (fora da caixa) fecha
        modal.addEventListener("click", function (evento) {
            if (evento.target === modal) modal.close();
        });

        // Vale para Cancelar, Esc e clique no fundo
        modal.addEventListener("close", function () {
            document.documentElement.classList.remove("modal-aberto");
            if (gatilho) gatilho.focus();
        });

        // Evita enviar duas vezes se a pessoa clicar de novo
        formulario.addEventListener("submit", function () {
            botaoOk.disabled = true;
            botaoOk.textContent = "Aguarde...";
        });

        // Voltar pelo histórico (cache do navegador) não pode deixar o botão travado
        window.addEventListener("pageshow", function () {
            botaoOk.disabled = false;
            botaoOk.textContent = textoPadraoOk;
        });
    }

    /* ============ Avisos rápidos (toast) ============ */
    function fecharAviso(aviso) {
        aviso.classList.add("is-saindo");
        setTimeout(function () { aviso.remove(); }, 300);
    }

    document.querySelectorAll("[data-toast]").forEach(function (aviso) {
        aviso.querySelector("[data-toast-fechar]").addEventListener("click", function () {
            fecharAviso(aviso);
        });

        // Some sozinho depois de 6 segundos
        setTimeout(function () {
            if (document.body.contains(aviso)) fecharAviso(aviso);
        }, 6000);
    });
})();
