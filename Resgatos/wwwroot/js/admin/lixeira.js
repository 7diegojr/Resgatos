// Lixeira do admin: modo "Selecionar vários".
//   - "Selecionar vários" mostra uma caixa em cada linha e troca os botões da barra por: Marcar todos, Restaurar, Excluir e Cancelar.
//   - "Restaurar" envia o formulário (POST) com os ids marcados: as caixas das linhas apontam para ele com form="form-selecao".
//   - "Excluir" usa o modal de confirmação (confirmacao.js). A quantidade só é conhecida na hora do clique, então o texto do modal
//     e a URL (com os ids) são montados aqui, antes de o confirmacao.js ler o botão.
// A seleção vale só para os itens da página atual.
(function () {
    "use strict";

    var barra = document.getElementById("form-selecao");
    var tabela = document.querySelector("[data-lixeira-tabela]");
    if (!barra) return;

    var grupoNormal = barra.querySelector("[data-selecao-normal]");
    var grupoAtivo = barra.querySelector("[data-selecao-ativa]");
    var botaoEntrar = barra.querySelector("[data-selecao-entrar]");
    var botaoSair = barra.querySelector("[data-selecao-sair]");
    var botaoTodos = barra.querySelector("[data-selecao-todos]");
    var textoMarcar = barra.querySelector("[data-todos-marcar]");
    var textoDesmarcar = barra.querySelector("[data-todos-desmarcar]");
    var botaoRestaurar = barra.querySelector("[data-selecao-restaurar]");
    var botaoExcluir = barra.querySelector("[data-selecao-excluir]");
    var contagem = barra.querySelector("[data-selecao-contagem]");

    // Sem o botão "Selecionar vários" (nenhum item na lista) não há o que fazer aqui
    if (!botaoEntrar || !tabela) return;

    var itens = Array.prototype.slice.call(document.querySelectorAll("[data-lixeira-item]"));

    function marcados() {
        return itens.filter(function (item) { return item.checked; });
    }

    /* ============ Estado da barra e das linhas ============ */
    function atualizar() {
        var qtd = marcados().length;

        contagem.textContent = qtd === 0
            ? "Nenhum selecionado"
            : qtd === 1 ? "1 selecionado" : qtd + " selecionados";

        botaoRestaurar.disabled = qtd === 0;
        botaoExcluir.disabled = qtd === 0;

        // Os dois textos ocupam o mesmo espaço (o botão não muda de largura); só um deles fica visível
        var todos = qtd === itens.length;
        textoMarcar.classList.toggle("is-oculto", todos);
        textoDesmarcar.classList.toggle("is-oculto", !todos);

        itens.forEach(function (item) {
            item.closest("tr").classList.toggle("is-selecionada", item.checked);
        });
    }

    function entrar() {
        tabela.classList.add("is-selecionando");
        grupoNormal.hidden = true;
        grupoAtivo.hidden = false;
        atualizar();
        botaoTodos.focus();
    }

    function sair(devolverFoco) {
        itens.forEach(function (item) { item.checked = false; });
        tabela.classList.remove("is-selecionando");
        grupoAtivo.hidden = true;
        grupoNormal.hidden = false;
        atualizar();
        if (devolverFoco) botaoEntrar.focus();
    }

    botaoEntrar.addEventListener("click", entrar);
    botaoSair.addEventListener("click", function () { sair(true); });

    botaoTodos.addEventListener("click", function () {
        var marcarTodos = marcados().length !== itens.length;
        itens.forEach(function (item) { item.checked = marcarTodos; });
        atualizar();
    });

    itens.forEach(function (item) {
        item.addEventListener("change", atualizar);
    });

    // No modo de seleção, clicar em qualquer parte da linha marca/desmarca (menos em links e botões)
    tabela.addEventListener("click", function (evento) {
        if (!tabela.classList.contains("is-selecionando")) return;
        if (evento.target.closest("a, button, label, input")) return;

        var linha = evento.target.closest("tr");
        var item = linha && linha.querySelector("[data-lixeira-item]");
        if (!item) return;

        item.checked = !item.checked;
        atualizar();
    });

    // Esc sai do modo de seleção (quando não há modal aberto: nele o Esc só fecha o modal)
    document.addEventListener("keydown", function (evento) {
        if (evento.key !== "Escape") return;
        if (!tabela.classList.contains("is-selecionando")) return;
        if (document.querySelector("dialog[open]")) return;
        sair(true);
    });

    /* ============ Restaurar os selecionados ============ */
    barra.addEventListener("submit", function () {
        // Evita enviar duas vezes se a pessoa clicar de novo
        setTimeout(function () { botaoRestaurar.disabled = true; botaoExcluir.disabled = true; }, 0);
    });

    /* ============ Excluir os selecionados ============ */
    // Fase de captura: roda ANTES do listener do confirmacao.js (que lê o data-url e os textos do botão ao abrir o modal)
    document.addEventListener("click", function (evento) {
        var botao = evento.target.closest("[data-selecao-excluir]");
        if (!botao) return;

        var ids = marcados().map(function (item) { return item.value; });

        if (ids.length === 0) {
            evento.preventDefault();
            evento.stopPropagation();
            return;
        }

        var url = botao.getAttribute("data-url").split("?")[0];
        botao.setAttribute("data-url", url + "?" + ids.map(function (id) {
            return "ids=" + encodeURIComponent(id);
        }).join("&"));

        var sufixo = ids.length === 1 ? "um" : "varios";
        botao.setAttribute("data-titulo", botao.getAttribute("data-titulo-" + sufixo).replace("{n}", ids.length));
        botao.setAttribute("data-texto", botao.getAttribute("data-texto-" + sufixo).replace("{n}", ids.length));
    }, true);

    // Voltar pelo histórico (cache do navegador) não pode deixar o modo de seleção ou caixas marcadas para trás
    window.addEventListener("pageshow", function (evento) {
        if (evento.persisted) sair(false);
    });
})();
