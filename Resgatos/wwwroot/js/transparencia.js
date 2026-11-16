// Transparência: filtros (categoria, período e ordenação) sem recarregar a página + foto ampliada.
// A filtragem em si é feita no servidor (HomeController.Transparencia); o JS só busca a página
// já filtrada e troca a lista de cards. Sem JavaScript, o formulário continua funcionando.
document.addEventListener("DOMContentLoaded", function () {
    // ---------- Filtro ----------
    var form = document.getElementById("gastos-filtros");
    var resultado = document.getElementById("gastos-resultado");
    var botaoLimpar = document.getElementById("gastos-limpar");

    if (form && resultado) {
        var controle = null; // permite cancelar a busca anterior se a pessoa clicar rápido

        var selects = form.querySelectorAll("select");

        // O "Limpar" aparece quando algo está diferente do padrão (categoria marcada,
        // período ou ordenação trocados). O valor padrão de cada select vem do data-padrao.
        function atualizarBotaoLimpar() {
            var algoMarcado = !!form.querySelector("input:checked");
            var algoTrocado = Array.prototype.some.call(selects, function (select) {
                return select.value !== select.getAttribute("data-padrao");
            });
            botaoLimpar.hidden = !(algoMarcado || algoTrocado);
        }

        function buscar() {
            // FormData inclui as caixinhas marcadas e os selects:
            // ?categorias=Alimentacao&categorias=Castracao&periodo=Mes&ordem=MaiorValor
            var parametros = new URLSearchParams(new FormData(form));

            // Select no valor padrão não precisa ir para a URL (deixa o endereço mais limpo)
            Array.prototype.forEach.call(selects, function (select) {
                if (select.value === select.getAttribute("data-padrao")) parametros.delete(select.name);
            });

            var query = parametros.toString();
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
                    var novoResultado = pagina.getElementById("gastos-resultado");
                    if (!novoResultado) throw new Error("Resultado não encontrado");

                    resultado.innerHTML = novoResultado.innerHTML;

                    // "X gastos · R$ ..." fica fora da lista, então atualizamos à parte
                    var contagem = document.getElementById("gastos-contagem");
                    var novaContagem = pagina.getElementById("gastos-contagem");
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
            Array.prototype.forEach.call(selects, function (select) {
                select.value = select.getAttribute("data-padrao");
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

        // Delegação de evento: o botão "Limpar filtro" do estado vazio é recriado a cada busca
        document.addEventListener("click", function (event) {
            var alvo = event.target.closest("[data-limpar-filtros]");
            if (!alvo) return;
            event.preventDefault();
            limpar();
        });
    }

    // ---------- Foto ampliada ----------
    // O <dialog> aberto com showModal() já cuida do fundo inerte, do foco preso e do Esc.
    var modal = document.getElementById("foto-modal");
    var imagem = document.getElementById("foto-modal-imagem");
    if (!modal || !imagem || typeof modal.showModal !== "function") return;

    var raiz = document.documentElement;

    // Delegação de evento: os cards são recriados a cada filtro
    document.addEventListener("click", function (evento) {
        var botao = evento.target.closest("[data-imagem-url]");
        if (!botao) return;

        imagem.src = botao.getAttribute("data-imagem-url");
        imagem.alt = botao.getAttribute("data-imagem-alt") || "";
        modal.showModal();
        raiz.classList.add("foto-aberta"); // trava a rolagem da página
    });

    // O evento "close" dispara de qualquer jeito que o modal fecha (Esc, botão X, clique fora)
    modal.addEventListener("close", function () {
        raiz.classList.remove("foto-aberta");
        imagem.removeAttribute("src");
    });

    // Clique no fundo escurecido: o clique "cai" no próprio <dialog>.
    // Conferimos também onde o clique COMEÇOU, para não fechar ao soltar o mouse do lado de fora.
    var cliqueComecouNoFundo = false;

    modal.addEventListener("mousedown", function (evento) {
        cliqueComecouNoFundo = evento.target === modal;
    });

    modal.addEventListener("click", function (evento) {
        if (evento.target === modal && cliqueComecouNoFundo) {
            modal.close();
            return;
        }
        if (evento.target.closest("[data-fechar-foto]")) modal.close();
    });
});
