// Adoções: aplica os filtros sem recarregar a página.
// A filtragem em si é feita no servidor (HomeController.Adocoes); o JS só busca a página
// já filtrada e troca a lista de cards. Sem JavaScript, o formulário continua funcionando.
document.addEventListener("DOMContentLoaded", function () {
    var form = document.getElementById("filtros-form");
    var resultado = document.getElementById("animais-resultado");
    var botaoLimpar = document.getElementById("filtros-limpar");
    if (!form || !resultado) return;

    var controle = null; // permite cancelar a busca anterior se a pessoa clicar rápido

    function atualizarBotaoLimpar() {
        botaoLimpar.hidden = !form.querySelector("input:checked");
    }

    function buscar() {
        // FormData só inclui as caixinhas marcadas: ?especies=Gato&sexos=Femea...
        var query = new URLSearchParams(new FormData(form)).toString();
        var url = form.getAttribute("action") + (query ? "?" + query : "");

        if (controle) controle.abort();
        controle = new AbortController();

        fetch(url, { signal: controle.signal })
            .then(function (resposta) {
                if (!resposta.ok) throw new Error("Resposta " + resposta.status);
                return resposta.text();
            })
            .then(function (html) {
                var pagina = new DOMParser().parseFromString(html, "text/html");
                var novoResultado = pagina.getElementById("animais-resultado");
                if (!novoResultado) throw new Error("Resultado não encontrado");

                resultado.innerHTML = novoResultado.innerHTML;

                // O "X animais disponíveis" fica fora da lista, então atualizamos à parte
                var contagem = document.getElementById("animais-contagem");
                var novaContagem = pagina.getElementById("animais-contagem");
                if (contagem && novaContagem) contagem.textContent = novaContagem.textContent;

                history.replaceState(null, "", url); // a URL filtrada pode ser copiada e compartilhada
            })
            .catch(function (erro) {
                if (erro.name === "AbortError") return; // foi só uma busca substituída por outra
                window.location.href = url;             // plano B: navegação normal
            });
    }

    function limpar() {
        Array.prototype.forEach.call(form.querySelectorAll("input:checked"), function (input) {
            input.checked = false;
        });
        atualizarBotaoLimpar();
        buscar();
    }

    form.addEventListener("change", function () {
        atualizarBotaoLimpar();
        buscar();
    });

    form.addEventListener("submit", function (event) {
        event.preventDefault();
        buscar();
    });

    // Delegação de evento: o botão "Limpar filtros" do estado vazio é recriado a cada busca
    document.addEventListener("click", function (event) {
        var alvo = event.target.closest("[data-limpar-filtros]");
        if (!alvo) return;
        event.preventDefault();
        limpar();
    });
});
