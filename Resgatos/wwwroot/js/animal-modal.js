// Adoções: modal de detalhes do animal.
// Clicar num card busca o miolo do modal no servidor (HomeController.DetalhesAnimal) e o coloca
// dentro do <dialog>. O <dialog> aberto com showModal() cuida do fundo travado, do foco preso
// e do Esc; aqui só fazemos o que ele não faz: travar a rolagem, buscar o conteúdo e a galeria.
document.addEventListener("DOMContentLoaded", function () {
    var modal = document.getElementById("animal-modal");
    var conteudo = document.getElementById("animal-modal-conteudo");
    if (!modal || !conteudo || typeof modal.showModal !== "function") return;

    var raiz = document.documentElement;
    var controle = null; // permite cancelar a busca se o modal for fechado antes de ela terminar

    // ---------- Estados do modal (carregando / erro) ----------
    function mostrarCarregando() {
        modal.setAttribute("aria-busy", "true");
        conteudo.innerHTML =
            '<div class="animal-modal-status">' +
            '<i class="fa-solid fa-paw fa-beat" aria-hidden="true"></i>' +
            '<p role="status">Carregando os detalhes…</p>' +
            "</div>";
    }

    function mostrarErro(titulo, texto) {
        modal.removeAttribute("aria-busy");
        conteudo.innerHTML =
            '<div class="animal-modal-status">' +
            '<i class="fa-solid fa-paw" aria-hidden="true"></i>' +
            "<h2>" + titulo + "</h2>" +
            "<p>" + texto + "</p>" +
            '<button type="button" class="animal-card-button" data-fechar-modal>' +
            '<i class="fa-solid fa-xmark" aria-hidden="true"></i><span>Fechar</span></button>' +
            "</div>";
    }

    // ---------- Abrir ----------
    function abrir(url) {
        if (controle) controle.abort();
        controle = new AbortController();

        mostrarCarregando();

        if (!modal.open) {
            modal.showModal();
            raiz.classList.add("modal-aberto"); // trava a rolagem da página
        }

        fetch(url, { signal: controle.signal })
            .then(function (resposta) {
                if (resposta.status === 404) throw new Error("nao-encontrado");
                if (!resposta.ok) throw new Error("Resposta " + resposta.status);
                return resposta.text();
            })
            .then(function (html) {
                conteudo.innerHTML = html;
                conteudo.scrollTop = 0;
                modal.removeAttribute("aria-busy");
            })
            .catch(function (erro) {
                if (erro.name === "AbortError") return; // foi só uma busca cancelada

                if (erro.message === "nao-encontrado") {
                    // Pode acontecer se o animal foi adotado enquanto a pessoa olhava a página
                    mostrarErro("Esse bichinho não está mais aqui",
                        "Ele pode já ter encontrado um lar! Veja os outros animais disponíveis.");
                } else {
                    mostrarErro("Não conseguimos carregar",
                        "Algo deu errado ao buscar os detalhes. Tente novamente em instantes.");
                }
            });
    }

    // ---------- Fechar ----------
    // O evento "close" dispara de qualquer jeito que o modal fecha (Esc, botão X, clique fora)
    modal.addEventListener("close", function () {
        raiz.classList.remove("modal-aberto"); // destrava a rolagem
        if (controle) controle.abort();
        conteudo.innerHTML = "";
        modal.removeAttribute("aria-busy");
    });

    // Clique no fundo escurecido: o clique "cai" no próprio <dialog> (o conteúdo ocupa toda a caixa,
    // então clicar dentro dela atinge sempre um elemento filho). Conferimos também onde o clique
    // COMEÇOU, para não fechar quando a pessoa seleciona um texto e solta o mouse do lado de fora.
    var cliqueComecouNoFundo = false;

    modal.addEventListener("mousedown", function (evento) {
        cliqueComecouNoFundo = evento.target === modal;
    });

    modal.addEventListener("click", function (evento) {
        if (evento.target === modal && cliqueComecouNoFundo) {
            modal.close();
            return;
        }
        if (evento.target.closest("[data-fechar-modal]")) modal.close();
    });

    // ---------- Abrir pelo card ----------
    // Delegação de evento: os cards são recriados a cada filtro, então o ouvinte fica no document
    document.addEventListener("click", function (evento) {
        var botao = evento.target.closest("[data-detalhes-url]");
        if (!botao) return;
        abrir(botao.getAttribute("data-detalhes-url"));
    });

    // ---------- Galeria de fotos ----------
    function irParaFoto(galeria, indice) {
        var miniaturas = galeria.querySelectorAll("[data-foto-indice]");
        var total = miniaturas.length;
        if (total < 2) return;

        indice = (indice + total) % total; // dá a volta: depois da última vem a primeira
        var alvo = miniaturas[indice];

        var imagem = galeria.querySelector("[data-galeria-imagem]");
        imagem.src = alvo.getAttribute("data-foto");
        imagem.alt = alvo.getAttribute("data-alt");

        Array.prototype.forEach.call(miniaturas, function (m) {
            m.setAttribute("aria-current", m === alvo ? "true" : "false");
        });

        var contador = galeria.querySelector("[data-galeria-contador]");
        if (contador) contador.textContent = indice + 1 + " / " + total;

        galeria.setAttribute("data-indice", indice);
    }

    function indiceAtual(galeria) {
        return parseInt(galeria.getAttribute("data-indice"), 10) || 0;
    }

    conteudo.addEventListener("click", function (evento) {
        var galeria = evento.target.closest("[data-galeria]");
        if (!galeria) return;

        var miniatura = evento.target.closest("[data-foto-indice]");
        if (miniatura) {
            irParaFoto(galeria, parseInt(miniatura.getAttribute("data-foto-indice"), 10));
        } else if (evento.target.closest("[data-foto-anterior]")) {
            irParaFoto(galeria, indiceAtual(galeria) - 1);
        } else if (evento.target.closest("[data-foto-proxima]")) {
            irParaFoto(galeria, indiceAtual(galeria) + 1);
        }
    });

    // Setas do teclado também passam as fotos
    modal.addEventListener("keydown", function (evento) {
        if (evento.key !== "ArrowLeft" && evento.key !== "ArrowRight") return;

        var galeria = conteudo.querySelector("[data-galeria]");
        if (!galeria) return;

        irParaFoto(galeria, indiceAtual(galeria) + (evento.key === "ArrowRight" ? 1 : -1));
    });
});
