// Formulário de criar/editar animal (Areas/Admin/Views/Animais/_Formulario.cshtml).
//   1. Mostra/esconde os campos que dependem de outros (FIV/FeLV só para gatos, data da castração...).
//   2. Fotos novas: prévia, escolha da capa, remover antes de enviar, arrastar e soltar.
//   3. Confere os campos obrigatórios antes de enviar, com as mensagens no estilo do painel.
// O servidor confere tudo de novo (AnimaisController): aqui é só para a pessoa ver o erro sem perder o que preencheu.
(function () {
    "use strict";

    var form = document.getElementById("animal-form");
    if (!form) return;

    var cartaoFotos = document.getElementById("fotos-card");
    // Mesmos limites do UploadService (vêm da página para não ficarem escritos em dois lugares)
    var MAX_FOTOS = parseInt(cartaoFotos.getAttribute("data-max-fotos"), 10) || 10;
    var MAX_BYTES = parseInt(cartaoFotos.getAttribute("data-max-bytes"), 10) || 5 * 1024 * 1024;
    var MAX_MB = Math.round(MAX_BYTES / (1024 * 1024));

    /* ============ Erros por campo ============ */
    function mostrarErro(bloco, mensagem) {
        bloco.classList.add("has-error");
        var texto = bloco.querySelector(".field-error");
        if (texto) texto.textContent = mensagem;
    }

    function limparErro(bloco) {
        bloco.classList.remove("has-error");
        var texto = bloco.querySelector(".field-error");
        if (texto) texto.textContent = "";
    }

    // Qualquer mudança no campo apaga o erro dele (as fotos são tratadas à parte)
    function aoMudar(evento) {
        if (evento.target.id === "NovasFotos") return;
        var bloco = evento.target.closest && evento.target.closest(".form-field");
        if (bloco) limparErro(bloco);
    }
    form.addEventListener("input", aoMudar);
    form.addEventListener("change", aoMudar);

    /* ============ 1. Campos que dependem de outros ============ */
    var especie = document.getElementById("Especie");
    var blocoGatos = document.getElementById("bloco-gatos");

    function atualizarCamposDependentes() {
        // FIV e FeLV só existem para gatos (o valor do select é o nome do enum)
        blocoGatos.hidden = especie.value !== "Gato";

        // data-depende-de="Id": o campo só aparece com aquela caixa marcada
        form.querySelectorAll("[data-depende-de]").forEach(function (alvo) {
            var controle = document.getElementById(alvo.getAttribute("data-depende-de"));
            alvo.hidden = !(controle && controle.checked);
        });
    }

    // O custom-select dispara "change" no <select> escondido, então isto pega tudo
    form.addEventListener("change", atualizarCamposDependentes);
    atualizarCamposDependentes();

    /* ============ Contador de caracteres (ex.: história, "45/200") ============ */
    form.querySelectorAll("[data-contador-de]").forEach(function (contador) {
        var campo = document.getElementById(contador.getAttribute("data-contador-de"));
        var maximo = parseInt(campo.getAttribute("maxlength"), 10);

        function atualizar() {
            contador.textContent = campo.value.length + "/" + maximo;
            contador.classList.toggle("is-no-limite", campo.value.length >= maximo);
        }

        campo.addEventListener("input", atualizar);
        atualizar();
    });

    /* ============ 2. Fotos ============ */
    var input = document.getElementById("NovasFotos");
    var lista = document.getElementById("novas-fotos-lista");
    var erroFotos = document.getElementById("erro-novas-fotos");
    var blocoFotos = document.getElementById("campo-fotos");
    var zonaDeSoltar = document.getElementById("foto-dropzone");
    var campoStatus = document.getElementById("Status");

    // Sem DataTransfer não dá para montar a lista de arquivos por código: fica o seletor normal do navegador
    var gerenciaFotos = typeof DataTransfer !== "undefined";
    var arquivos = [];   // fotos novas escolhidas (na ordem em que serão enviadas)
    var urlsPrevia = [];

    function fotosAtuaisMantidas() {
        return form.querySelectorAll('#fotos-existentes input[name="FotosRemover"]:not(:checked)').length;
    }

    function totalFotos() {
        return fotosAtuaisMantidas() + (gerenciaFotos ? arquivos.length : input.files.length);
    }

    function tipoValido(arquivo) {
        if (["image/jpeg", "image/png", "image/webp"].indexOf(arquivo.type) !== -1) return true;
        return arquivo.type === "" && /\.(jpe?g|png|webp)$/i.test(arquivo.name);
    }

    // O input de arquivo envia o que está em input.files: mantemos ele igual à nossa lista
    function sincronizarInput() {
        var transferencia = new DataTransfer();
        arquivos.forEach(function (arquivo) { transferencia.items.add(arquivo); });
        input.files = transferencia.files;
    }

    function mostrarErroFotos(mensagens) {
        erroFotos.textContent = mensagens.join("\n");
        blocoFotos.classList.toggle("has-error", mensagens.length > 0);
    }

    // Foto marcada como capa (entre as novas), para não perder a escolha quando a lista é redesenhada
    function arquivoCapaNova() {
        var marcada = form.querySelector('input[name="CapaSelecionada"]:checked');
        if (marcada && marcada.value.indexOf("nova-") === 0) {
            return arquivos[parseInt(marcada.value.slice(5), 10)] || null;
        }
        return null;
    }

    // Sempre tem de haver uma capa marcada (entre as fotos que não estão sendo removidas)
    function garantirCapa() {
        var radios = Array.prototype.filter.call(
            form.querySelectorAll('input[name="CapaSelecionada"]'),
            function (radio) { return !radio.disabled; }
        );
        if (radios.length === 0) return;

        var algumMarcado = radios.some(function (radio) { return radio.checked; });
        if (!algumMarcado) radios[0].checked = true;
    }

    function criarMiniatura(arquivo, indice, url) {
        var item = document.createElement("li");
        item.className = "foto-tile foto-tile-nova";

        var imagem = document.createElement("img");
        imagem.src = url;
        imagem.alt = "Prévia de " + arquivo.name;

        var selo = document.createElement("span");
        selo.className = "foto-selo-nova";
        selo.textContent = "Nova";

        var rodape = document.createElement("div");
        rodape.className = "foto-tile-footer";

        // Capa
        var rotuloCapa = document.createElement("label");
        rotuloCapa.className = "foto-opcao foto-opcao-capa";
        var radio = document.createElement("input");
        radio.type = "radio";
        radio.name = "CapaSelecionada";
        radio.value = "nova-" + indice;
        var textoCapa = document.createElement("span");
        textoCapa.innerHTML = '<i class="fa-solid fa-star" aria-hidden="true"></i> Capa';
        rotuloCapa.appendChild(radio);
        rotuloCapa.appendChild(textoCapa);

        // Remover (ainda não foi enviada: só tira da lista)
        var botaoRemover = document.createElement("button");
        botaoRemover.type = "button";
        botaoRemover.className = "foto-opcao foto-opcao-remover";
        botaoRemover.setAttribute("aria-label", "Remover a foto " + arquivo.name);
        botaoRemover.innerHTML = '<span><i class="fa-solid fa-trash-can" aria-hidden="true"></i> Remover</span>';
        botaoRemover.addEventListener("click", function () {
            var capa = arquivoCapaNova();
            arquivos.splice(indice, 1);
            sincronizarInput();
            mostrarErroFotos([]);
            desenharNovas(capa === arquivo ? null : capa);
        });

        rodape.appendChild(rotuloCapa);
        rodape.appendChild(botaoRemover);
        item.appendChild(imagem);
        item.appendChild(selo);
        item.appendChild(rodape);
        return item;
    }

    function desenharNovas(capaPreservada) {
        urlsPrevia.forEach(function (url) { URL.revokeObjectURL(url); });
        urlsPrevia = [];
        lista.innerHTML = "";

        arquivos.forEach(function (arquivo, indice) {
            var url = URL.createObjectURL(arquivo);
            urlsPrevia.push(url);
            lista.appendChild(criarMiniatura(arquivo, indice, url));
        });

        if (capaPreservada) {
            var radio = form.querySelector('input[name="CapaSelecionada"][value="nova-' + arquivos.indexOf(capaPreservada) + '"]');
            if (radio) radio.checked = true;
        }
        garantirCapa();

        // Se o erro "precisa de uma foto" estava no Status, adicionar/remover foto já o atualiza
        var blocoStatus = campoStatus && campoStatus.closest(".form-field");
        if (blocoStatus) limparErro(blocoStatus);
    }

    function adicionarArquivos(escolhidos) {
        var capa = arquivoCapaNova();
        var erros = [];

        Array.prototype.forEach.call(escolhidos, function (arquivo) {
            if (!tipoValido(arquivo)) {
                erros.push('"' + arquivo.name + '": precisa ser uma imagem JPG, PNG ou WebP.');
                return;
            }
            if (arquivo.size > MAX_BYTES) {
                erros.push('"' + arquivo.name + '": passa de ' + MAX_MB + ' MB.');
                return;
            }
            var repetida = arquivos.some(function (a) {
                return a.name === arquivo.name && a.size === arquivo.size && a.lastModified === arquivo.lastModified;
            });
            if (repetida) return;

            if (totalFotos() >= MAX_FOTOS) {
                var limite = "Cada animal pode ter no máximo " + MAX_FOTOS + " fotos.";
                if (erros.indexOf(limite) === -1) erros.push(limite);
                return;
            }
            arquivos.push(arquivo);
        });

        sincronizarInput();
        mostrarErroFotos(erros);
        desenharNovas(capa);
    }

    if (gerenciaFotos) {
        input.addEventListener("change", function () {
            // Neste momento input.files tem só o que a pessoa acabou de escolher; juntamos com o que já estava na lista
            adicionarArquivos(Array.prototype.slice.call(input.files));
        });

        // Arrastar e soltar
        ["dragenter", "dragover"].forEach(function (nome) {
            zonaDeSoltar.addEventListener(nome, function (evento) {
                evento.preventDefault();
                zonaDeSoltar.classList.add("is-arrastando");
            });
        });
        ["dragleave", "dragend", "drop"].forEach(function (nome) {
            zonaDeSoltar.addEventListener(nome, function (evento) {
                evento.preventDefault();
                zonaDeSoltar.classList.remove("is-arrastando");
            });
        });
        zonaDeSoltar.addEventListener("drop", function (evento) {
            if (evento.dataTransfer && evento.dataTransfer.files.length > 0) {
                adicionarArquivos(Array.prototype.slice.call(evento.dataTransfer.files));
            }
        });
    }

    // Fotos que o animal já tem: "Remover" apaga a foto (só ao salvar) e tira ela da escolha de capa
    form.addEventListener("change", function (evento) {
        var caixa = evento.target;
        if (caixa.name !== "FotosRemover") return;

        var foto = caixa.closest(".foto-tile");
        var radio = foto.querySelector('input[type="radio"]');
        foto.classList.toggle("is-removida", caixa.checked);
        radio.disabled = caixa.checked;
        if (caixa.checked) radio.checked = false;
        garantirCapa();
    });

    garantirCapa();

    /* ============ 3. Conferir antes de enviar ============ */
    // Devolve o primeiro campo com problema (ou null se está tudo certo) e mostra a mensagem de cada um
    function validar() {
        var primeiro = null;

        function falha(campo, mensagem) {
            var bloco = campo.closest(".form-field");
            if (bloco) mostrarErro(bloco, mensagem);
            if (!primeiro) primeiro = campo;
        }

        form.querySelectorAll("[data-required]").forEach(function (campo) {
            if (campo.closest("[hidden]")) return; // campo escondido não conta (ex.: FIV de cachorro)
            if (campo.value.trim() === "") {
                falha(campo, campo.getAttribute("data-msg") || "Preencha este campo.");
            }
        });

        // Caixa de texto maior que o limite (acontece quando o texto já vinha salvo com mais caracteres)
        form.querySelectorAll("textarea[maxlength]").forEach(function (campo) {
            if (campo.value.length > parseInt(campo.getAttribute("maxlength"), 10)) {
                falha(campo, campo.getAttribute("data-msg-max") || "O texto passou do limite de caracteres.");
            }
        });

        // Data de chegada: mês e ano juntos (ou nenhum), e não pode ser depois do mês atual
        var mesChegada = document.getElementById("MesChegada");
        var anoChegada = document.getElementById("AnoChegada");
        if ((mesChegada.value === "") !== (anoChegada.value === "")) {
            falha(mesChegada.value === "" ? mesChegada : anoChegada, "Informe o mês e o ano da chegada.");
        } else if (mesChegada.value !== "") {
            var hoje = new Date();
            var valorChegada = parseInt(anoChegada.value, 10) * 12 + parseInt(mesChegada.value, 10);
            if (valorChegada > hoje.getFullYear() * 12 + hoje.getMonth() + 1) {
                falha(mesChegada, "A data de chegada não pode ser no futuro.");
            }
        }

        // Só aparece no site quem está disponível, e o card do site precisa de foto
        if (campoStatus.value === "Disponivel" && totalFotos() === 0) {
            falha(campoStatus, "Para ficar disponível, o animal precisa de pelo menos uma foto.");
        }

        if (totalFotos() > MAX_FOTOS) {
            mostrarErroFotos(["Cada animal pode ter no máximo " + MAX_FOTOS + " fotos."]);
            if (!primeiro) primeiro = input;
        }

        return primeiro;
    }

    function focar(campo) {
        var alvo = campo;
        if (campo === input) {
            alvo = zonaDeSoltar;
        } else if (campo.tagName === "SELECT") {
            // O <select> real está escondido: o foco vai para a caixa que a pessoa enxerga
            alvo = campo.parentNode.querySelector(".form-select-trigger") || campo;
        }
        alvo.scrollIntoView({ behavior: "smooth", block: "center" });
        if (alvo.focus) alvo.focus({ preventScroll: true });
    }

    var botaoSalvar = document.getElementById("animal-form-salvar");
    var textoBotao = botaoSalvar.querySelector("span");
    var textoOriginalBotao = textoBotao.textContent;

    form.addEventListener("submit", function (evento) {
        var primeiro = validar();
        if (primeiro) {
            evento.preventDefault();
            focar(primeiro);
            return;
        }

        // Evita enviar duas vezes (o envio com fotos pode demorar um pouco)
        botaoSalvar.disabled = true;
        textoBotao.textContent = "Salvando...";
    });

    // Voltar pelo histórico (cache do navegador) não pode deixar o botão travado
    window.addEventListener("pageshow", function () {
        botaoSalvar.disabled = false;
        textoBotao.textContent = textoOriginalBotao;
    });
})();
