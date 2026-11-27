// Admin > Adotados > modal de detalhes da adoção (Areas/Admin/Views/Adotados/_ModalAdotado.cshtml).
//   1. Clicar em um card (o botão com o nome do animal) abre o modal, preenchido com o JSON do card (data-detalhes).
//   2. Dois modos: "ver" (só leitura, com Editar e Excluir adoção) e "editar" (campos, Salvar e Cancelar).
//      Os botões "Adicionar observações" e "Adicionar termo" do modo ver abrem a edição já no campo certo.
//   3. Salvar envia por fetch para Adotados/Editar; os erros do servidor aparecem nos campos e, no sucesso, a página recarrega.
//   4. Excluir adoção usa o modal de confirmação do painel (confirmacao.js): este arquivo só preenche o botão com o texto certo.
// As regras dos campos (telefone, valor, parcelas, termo) são as mesmas do adotar-animal.js e do AdotadosController.Validar.
(function () {
    "use strict";

    var modal = document.getElementById("modal-adotado");
    if (!modal) return;

    var form = document.getElementById("form-adotado");
    var dados = null;                 // o JSON do card aberto
    var gatilho = null;               // botão que abriu o modal (o foco volta para ele ao fechar)

    function $(id) { return document.getElementById(id); }

    var raiz = modal.getAttribute("data-raiz") || "/";
    var urlTermo = modal.getAttribute("data-url-termo");
    var urlExcluir = modal.getAttribute("data-url-excluir");
    var urlAnimalVer = modal.getAttribute("data-url-animal-ver");
    var urlAnimalEditar = modal.getAttribute("data-url-animal-editar");

    var campoTelefone = $("ado-Telefone");
    var campoValor = $("ado-Valor");
    var caixaParcelada = $("ado-TaxaParcelada");
    var blocoParcelas = $("ado-campo-parcelas");
    var campoParcelas = $("ado-QtdParcelas");
    var campoData = $("ado-DataAdocao");
    var campoObs = $("ado-Observacoes");
    var campoTermo = $("ado-Termo");
    var alerta = $("ado-alerta");
    var botaoSalvar = $("ado-salvar");
    var textoBotao = botaoSalvar.querySelector("span");
    var textoOriginalBotao = textoBotao.textContent;

    var VALOR_MAXIMO = 99999999.99;
    var MAX_PARCELAS = 24;
    var TAMANHO_MAXIMO = 5 * 1024 * 1024;

    // Estado do termo na edição
    var arquivoNovo = null;           // arquivo escolhido agora (substitui o atual ao salvar)
    var removerTermo = false;         // o termo atual vai sair ao salvar

    /* ============ 1. Abrir, preencher e fechar ============ */
    function texto(id, valor) { $(id).textContent = valor == null ? "" : valor; }

    function urlComId(modelo, id) { return modelo.replace("__ID__", id); }

    function preencherAnimal(d) {
        texto("ado-titulo", d.titulo);
        texto("ado-sub-ver", "Registrada em " + d.registrada + ".");

        var a = d.animal;
        texto("ado-animal-nome", a.nome);
        texto("ado-especie", a.especie);
        texto("ado-sexo", a.sexo);
        texto("ado-idade", a.idade);
        texto("ado-porte", a.porte);
        texto("ado-cor", a.cor);
        texto("ado-tempo", a.tempo);
        texto("ado-castrado", a.castrado);
        texto("ado-saude", a.saude);
        $("ado-saude-item").classList.toggle("tem-pendencia", !!a.saudePendente);

        // Ver e editar o animal: as telas de Animais, que voltam para este modal (a página Adotados abre com ?abrir=)
        var ver = $("ado-animal-ver");
        var editar = $("ado-animal-editar");
        ver.setAttribute("href", urlComId(urlAnimalVer, d.idAnimal));
        ver.setAttribute("aria-label", "Ver detalhes de " + a.nome);
        editar.setAttribute("href", urlComId(urlAnimalEditar, d.idAnimal));
        editar.setAttribute("aria-label", "Editar " + a.nome);

        var img = $("ado-foto-img");
        var vazia = $("ado-foto-vazia");
        if (d.foto) {
            img.src = raiz + d.foto;
            img.alt = "Foto de " + a.nome;
            img.hidden = false;
            vazia.hidden = true;
        } else {
            img.removeAttribute("src");
            img.hidden = true;
            vazia.hidden = false;
        }
    }

    // Modo ver: tudo em texto
    function preencherVisualizacao(d) {
        texto("v-nome", d.adotante.nome);
        texto("v-telefone", d.adotante.telefone);
        texto("v-email", d.adotante.email);
        texto("v-endereco", d.adotante.endereco);

        texto("v-data", d.adocao.data);
        texto("v-valor", d.adocao.valor);
        texto("v-pagamento", d.adocao.pagamento);

        var temObs = !!(d.adocao.observacoes && d.adocao.observacoes.trim());
        texto("v-obs", temObs ? d.adocao.observacoes : "");
        $("v-obs").hidden = !temObs;
        $("v-obs-vazio").hidden = temObs;

        $("v-termo-ok").hidden = !d.adocao.termo;
        $("v-termo-vazio").hidden = !!d.adocao.termo;
        $("v-termo-abrir").setAttribute("href", urlComId(urlTermo, d.id));
    }

    // Modo editar: campos com os valores atuais (também é o que volta quando a pessoa cancela)
    function preencherFormulario(d) {
        form.reset();
        form.querySelectorAll(".form-field").forEach(limparErro);
        alerta.hidden = true;

        $("ado-id").value = d.id;
        $("ado-NomeAdotante").value = d.adotante.nome;
        campoTelefone.value = d.adotante.telefone;
        $("ado-Email").value = d.adotante.email;
        $("ado-Endereco").value = d.adotante.endereco;

        campoData.max = hojeIso();
        campoData.value = d.adocao.dataIso;
        campoValor.value = d.adocao.valorCampo;

        caixaParcelada.checked = !!d.adocao.parcelada;
        atualizarParcelas();
        if (d.adocao.parcelada && d.adocao.parcelas) campoParcelas.value = d.adocao.parcelas;

        campoObs.value = d.adocao.observacoes || "";

        campoTermo.value = "";
        arquivoNovo = null;
        removerTermo = false;
        atualizarTermo();

        $("ado-termo-abrir").setAttribute("href", urlComId(urlTermo, d.id));

        liberarBotao();
    }

    // Botão "Excluir adoção": o confirmacao.js lê estes atributos quando o botão é clicado
    function prepararExclusao(d) {
        var botao = $("ado-excluir");
        botao.setAttribute("data-url", urlComId(urlExcluir, d.id));
        botao.setAttribute("data-titulo", "Tem certeza que deseja\nexcluir esta adoção?");
        botao.setAttribute("data-texto", d.excluirTexto);
    }

    function hojeIso() {
        var d = new Date();
        function dois(n) { return (n < 10 ? "0" : "") + n; }
        return d.getFullYear() + "-" + dois(d.getMonth() + 1) + "-" + dois(d.getDate());
    }

    function definirModo(modo) {
        modal.setAttribute("data-modo", modo);
    }

    function abrir(botao) {
        var card = botao.closest(".adotado-card");
        if (!card) return;

        try {
            dados = JSON.parse(card.getAttribute("data-detalhes"));
        } catch (erro) {
            return;     // sem dados não há o que mostrar
        }

        gatilho = botao;
        preencherAnimal(dados);
        preencherVisualizacao(dados);
        preencherFormulario(dados);
        prepararExclusao(dados);
        definirModo("ver");
        form.scrollTop = 0;

        document.documentElement.classList.add("modal-aberto");
        modal.showModal();

        // Sem nenhum campo "selecionado" ao abrir (mesmo cuidado dos outros modais do painel)
        modal.setAttribute("tabindex", "-1");
        modal.style.outline = "none";
        modal.focus({ preventScroll: true });
    }

    document.addEventListener("click", function (evento) {
        var botao = evento.target.closest(".adotado-abrir");
        if (botao) abrir(botao);
    });

    // Foto quadrada: ela ocupa a lateral esquerda do quadro do animal, então a largura dela é a altura das informações.
    // O JS mede essa altura e grava em --foto-lado (o CSS usa como largura). Mede de novo sempre que a altura muda
    // (o modal abrindo, o texto sendo preenchido, a tela redimensionada).
    var quadroAnimal = modal.querySelector(".adotar-animal");
    var infoAnimal = modal.querySelector(".adotar-animal-info");
    if (quadroAnimal && infoAnimal && typeof ResizeObserver !== "undefined") {
        new ResizeObserver(function () {
            window.requestAnimationFrame(function () {
                var altura = infoAnimal.offsetHeight;
                if (altura > 0) quadroAnimal.style.setProperty("--foto-lado", altura + "px");
            });
        }).observe(infoAnimal);
    }

    // Veio de "Voltar para a adoção" (detalhes ou edição do animal): a página chega com data-abrir e o modal abre sozinho
    var grade = document.querySelector(".adotados-grid[data-abrir]");
    var idAbrir = grade && grade.getAttribute("data-abrir");
    if (idAbrir) {
        var cardAbrir = grade.querySelector('.adotado-card[data-id="' + idAbrir + '"]');
        var botaoAbrir = cardAbrir && cardAbrir.querySelector(".adotado-abrir");
        if (botaoAbrir) abrir(botaoAbrir);

        // Tira o ?abrir= do endereço: atualizar a página não pode reabrir o modal
        try {
            var url = new URL(window.location.href);
            url.searchParams.delete("abrir");
            window.history.replaceState(null, "", url.pathname + url.search + url.hash);
        } catch (erro) { /* endereço sem alteração: não atrapalha */ }
    }

    modal.querySelectorAll("[data-fechar-adotado]").forEach(function (botao) {
        botao.addEventListener("click", function () { modal.close(); });
    });

    // Clicar no fundo escurecido fecha, mas só no modo ver: na edição um clique sem querer apagaria o que foi digitado
    modal.addEventListener("click", function (evento) {
        if (evento.target === modal && modal.getAttribute("data-modo") === "ver") modal.close();
    });

    modal.addEventListener("close", function () {
        document.documentElement.classList.remove("modal-aberto");
        if (gatilho) gatilho.focus();
    });

    // O modal de confirmação (excluir) tira o travamento da rolagem da página quando fecha.
    // Se ele foi cancelado, este modal continua aberto e a rolagem precisa continuar travada.
    var confirmacao = document.getElementById("admin-modal-confirmacao");
    if (confirmacao) {
        confirmacao.addEventListener("close", function () {
            if (modal.open) document.documentElement.classList.add("modal-aberto");
        });
    }

    /* ============ 2. Modo ver <-> editar ============ */
    function entrarEdicao(foco) {
        definirModo("editar");
        form.scrollTop = 0;

        var alvo = null;
        if (foco === "obs") alvo = campoObs;
        else if (foco === "termo") alvo = campoTermo;
        else alvo = $("ado-NomeAdotante");

        if (alvo) {
            alvo.focus({ preventScroll: true });
            // O termo e as observações ficam no fim do modal: leva a tela até eles
            if (foco) (foco === "termo" ? $("ado-termo-zona") : alvo).scrollIntoView({ behavior: "smooth", block: "center" });
        }
    }

    $("ado-editar").addEventListener("click", function () { entrarEdicao(null); });

    modal.querySelectorAll("[data-editar-foco]").forEach(function (botao) {
        botao.addEventListener("click", function () { entrarEdicao(botao.getAttribute("data-editar-foco")); });
    });

    // Cancelar descarta o que foi mexido e volta para a visualização
    $("ado-cancelar").addEventListener("click", function () {
        preencherFormulario(dados);
        definirModo("ver");
        form.scrollTop = 0;
        $("ado-editar").focus({ preventScroll: true });
    });

    /* ============ Erros por campo ============ */
    function mostrarErro(bloco, mensagem) {
        bloco.classList.add("has-error");
        var campoErro = bloco.querySelector(".field-error");
        if (campoErro) campoErro.textContent = mensagem;
    }

    function limparErro(bloco) {
        bloco.classList.remove("has-error");
        var campoErro = bloco.querySelector(".field-error");
        if (campoErro) campoErro.textContent = "";
    }

    function aoMudar(evento) {
        // O arquivo do termo cuida do próprio erro (aceitarTermo). Sem esta saída, o "change" sobe até o formulário
        // logo depois e apagaria na hora a mensagem de arquivo inválido.
        if (evento.target === campoTermo) return;

        var bloco = evento.target.closest && evento.target.closest(".form-field");
        if (bloco) limparErro(bloco);
        alerta.hidden = true;
    }
    form.addEventListener("input", aoMudar);
    form.addEventListener("change", aoMudar);

    /* ============ 3. Campos ============ */
    // Telefone: (41) 99999-9999 (aceita fixo, com 10 dígitos)
    campoTelefone.addEventListener("input", function () {
        var d = campoTelefone.value.replace(/\D/g, "").slice(0, 11);
        var t = d;
        if (d.length > 6) {
            var meio = d.length === 11 ? 7 : 6;
            t = "(" + d.slice(0, 2) + ") " + d.slice(2, meio) + "-" + d.slice(meio);
        } else if (d.length > 2) {
            t = "(" + d.slice(0, 2) + ") " + d.slice(2);
        } else if (d.length > 0) {
            t = "(" + d;
        }
        campoTelefone.value = t;
    });

    // Valor: só números, vírgula e ponto; ao sair, "150" vira "150,00"
    function lerValor(textoCampo) {
        var t = textoCampo.replace(/R\$/gi, "").replace(/\s/g, "");
        if (t === "") return null;

        if (t.indexOf(",") !== -1) {
            t = t.replace(/\./g, "").replace(",", ".");
        } else if (/^\d{1,3}(\.\d{3})+$/.test(t)) {
            t = t.replace(/\./g, "");
        }

        if (!/^\d+(\.\d+)?$/.test(t)) return null;
        var partes = t.split(".");
        return { numero: parseFloat(t), casas: partes[1] ? partes[1].length : 0 };
    }

    campoValor.addEventListener("input", function () {
        var limpo = campoValor.value.replace(/[^\d.,]/g, "");
        if (limpo !== campoValor.value) campoValor.value = limpo;
    });

    campoValor.addEventListener("blur", function () {
        var v = lerValor(campoValor.value);
        if (v && v.casas <= 2 && v.numero >= 0.01 && v.numero <= VALOR_MAXIMO) {
            campoValor.value = v.numero.toLocaleString("pt-BR", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
        }
    });

    // Parcelas: só números; o campo só vale quando "parcelada" está marcada
    campoParcelas.addEventListener("input", function () {
        campoParcelas.value = campoParcelas.value.replace(/\D/g, "");
    });

    function atualizarParcelas() {
        var ativo = caixaParcelada.checked;
        campoParcelas.disabled = !ativo;
        if (ativo) {
            blocoParcelas.removeAttribute("data-oculto");
        } else {
            blocoParcelas.setAttribute("data-oculto", "");
            campoParcelas.value = "";
            limparErro(blocoParcelas);
        }
    }
    caixaParcelada.addEventListener("change", function () {
        atualizarParcelas();
        if (caixaParcelada.checked) campoParcelas.focus();
    });

    /* ============ Termo (opcional) ============ */
    function tipoTermoValido(arquivo) {
        if (["image/jpeg", "image/png", "image/webp", "application/pdf"].indexOf(arquivo.type) !== -1) return true;
        // Alguns navegadores não informam o tipo: confere pela extensão
        return arquivo.type === "" && /\.(jpe?g|png|webp|pdf)$/i.test(arquivo.name);
    }

    function formatarTamanho(bytes) {
        return bytes < 1024 * 1024
            ? Math.max(1, Math.round(bytes / 1024)) + " KB"
            : (bytes / (1024 * 1024)).toLocaleString("pt-BR", { maximumFractionDigits: 1 }) + " MB";
    }

    // Mostra só o que vale agora: termo atual, arquivo novo, aviso de remoção ou a área de escolher
    function atualizarTermo() {
        var temAtual = !!(dados && dados.adocao.termo);
        var mostrandoAtual = temAtual && !removerTermo && !arquivoNovo;
        var saindo = temAtual && removerTermo && !arquivoNovo;

        $("ado-termo-atual").hidden = !mostrandoAtual;
        $("ado-termo-novo").hidden = !arquivoNovo;
        $("ado-termo-aviso").hidden = !saindo;
        $("ado-termo-zona").hidden = mostrandoAtual || !!arquivoNovo;
        $("ado-remover-termo").value = saindo ? "true" : "false";

        if (arquivoNovo) {
            texto("ado-termo-nome", arquivoNovo.name);
            texto("ado-termo-tamanho", formatarTamanho(arquivoNovo.size) + (temAtual ? " · vai substituir o termo atual" : ""));
        }
    }

    function aceitarTermo(arquivo) {
        var bloco = $("ado-campo-termo");
        limparErro(bloco);

        if (!arquivo) {
            arquivoNovo = null;
            atualizarTermo();
            return;
        }
        if (!tipoTermoValido(arquivo)) {
            campoTermo.value = "";
            arquivoNovo = null;
            atualizarTermo();
            mostrarErro(bloco, '"' + arquivo.name + '": precisa ser uma imagem JPG, PNG ou WebP, ou um PDF.');
            return;
        }
        if (arquivo.size > TAMANHO_MAXIMO) {
            campoTermo.value = "";
            arquivoNovo = null;
            atualizarTermo();
            mostrarErro(bloco, '"' + arquivo.name + '": passa de 5 MB.');
            return;
        }
        arquivoNovo = arquivo;
        atualizarTermo();
    }

    campoTermo.addEventListener("change", function () {
        aceitarTermo(campoTermo.files && campoTermo.files[0]);
    });

    $("ado-termo-trocar").addEventListener("click", function () { campoTermo.click(); });

    $("ado-termo-tirar").addEventListener("click", function () {
        removerTermo = true;
        atualizarTermo();
        $("ado-termo-manter").focus();
    });

    $("ado-termo-manter").addEventListener("click", function () {
        removerTermo = false;
        atualizarTermo();
        $("ado-termo-tirar").focus();
    });

    $("ado-termo-descartar").addEventListener("click", function () {
        campoTermo.value = "";
        arquivoNovo = null;
        limparErro($("ado-campo-termo"));
        atualizarTermo();
    });

    // Arrastar e soltar
    var zonaTermo = $("ado-termo-zona");
    ["dragenter", "dragover"].forEach(function (nome) {
        zonaTermo.addEventListener(nome, function (evento) {
            evento.preventDefault();
            zonaTermo.classList.add("is-arrastando");
        });
    });
    ["dragleave", "dragend", "drop"].forEach(function (nome) {
        zonaTermo.addEventListener(nome, function (evento) {
            evento.preventDefault();
            zonaTermo.classList.remove("is-arrastando");
        });
    });
    zonaTermo.addEventListener("drop", function (evento) {
        var soltos = evento.dataTransfer && evento.dataTransfer.files;
        if (!soltos || soltos.length === 0) return;
        try {
            var transferencia = new DataTransfer();
            transferencia.items.add(soltos[0]);
            campoTermo.files = transferencia.files;
            aceitarTermo(soltos[0]);
        } catch (erro) {
            mostrarErro($("ado-campo-termo"), "Não foi possível arrastar o arquivo aqui. Clique na área para escolhê-lo.");
        }
    });

    /* ============ 4. Conferir e enviar ============ */
    function validar() {
        var primeiro = null;

        function falha(campo, mensagem) {
            var bloco = campo.closest(".form-field");
            if (bloco) mostrarErro(bloco, mensagem);
            if (!primeiro) primeiro = campo;
        }

        form.querySelectorAll("[data-required]").forEach(function (campo) {
            if (campo.value.trim() === "") falha(campo, campo.getAttribute("data-msg") || "Preencha este campo.");
        });

        var nome = $("ado-NomeAdotante");
        if (nome.value.trim() !== "" && nome.value.trim().length < 3) falha(nome, "Informe o nome completo (mínimo 3 caracteres).");

        var digitos = campoTelefone.value.replace(/\D/g, "");
        if (campoTelefone.value.trim() !== "" && digitos.length !== 10 && digitos.length !== 11) {
            falha(campoTelefone, "Informe um telefone com DDD, ex.: (41) 99999-9999.");
        }

        var email = $("ado-Email");
        if (email.value.trim() !== "" && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email.value.trim())) {
            falha(email, "Informe um e-mail válido.");
        }

        var endereco = $("ado-Endereco");
        if (endereco.value.trim() !== "" && endereco.value.trim().length < 8) {
            falha(endereco, "Informe o endereço completo (rua, número e bairro).");
        }

        if (campoData.value !== "" && campoData.value > hojeIso()) falha(campoData, "A data da adoção não pode ser no futuro.");

        if (campoValor.value.trim() !== "") {
            var v = lerValor(campoValor.value);
            if (!v) falha(campoValor, "Informe um valor válido (ex.: 150,00).");
            else if (v.casas > 2) falha(campoValor, "O valor pode ter no máximo 2 casas depois da vírgula.");
            else if (v.numero < 0.01) falha(campoValor, "O valor deve ser maior que R$ 0,00.");
            else if (v.numero > VALOR_MAXIMO) falha(campoValor, "O valor é alto demais. Confira o que foi digitado.");
        }

        if (campoObs.value.trim().length > 500) falha(campoObs, "As observações podem ter no máximo 500 caracteres.");

        // Termo que a própria tela já recusou (tipo ou tamanho)
        if ($("ado-campo-termo").classList.contains("has-error") && !primeiro) primeiro = campoTermo;

        if (caixaParcelada.checked) {
            var n = parseInt(campoParcelas.value, 10);
            if (isNaN(n) || n < 2 || n > MAX_PARCELAS) falha(campoParcelas, "Informe de 2 a " + MAX_PARCELAS + " parcelas.");
        }

        return primeiro;
    }

    function focar(campo) {
        var alvo = campo === campoTermo ? zonaTermo : campo;
        alvo.scrollIntoView({ behavior: "smooth", block: "center" });
        campo.focus({ preventScroll: true });
    }

    function mostrarAlerta(mensagem) {
        alerta.querySelector("span").textContent = mensagem;
        alerta.hidden = false;
    }

    function liberarBotao() {
        botaoSalvar.disabled = false;
        textoBotao.textContent = textoOriginalBotao;
    }

    form.addEventListener("submit", function (evento) {
        evento.preventDefault();
        if (modal.getAttribute("data-modo") !== "editar") return;   // Enter no modo ver não salva nada

        var primeiro = validar();
        if (primeiro) {
            focar(primeiro);
            return;
        }

        botaoSalvar.disabled = true;           // evita enviar duas vezes
        textoBotao.textContent = "Salvando...";

        fetch(form.getAttribute("action"), {
            method: "POST",
            body: new FormData(form),          // já leva o token antifalsificação e o arquivo novo, se houver
            headers: { "Accept": "application/json" },
            credentials: "same-origin"
        })
            .then(function (resposta) {
                if (!resposta.ok) throw new Error("http");
                return resposta.json();
            })
            .then(function (resultado) {
                if (resultado.ok) {
                    // Recarrega a página pedindo para abrir o modal desta adoção (?abrir=): ele volta no modo ver, já com os dados novos,
                    // e a página mostra o aviso de sucesso. Os filtros e a busca da URL continuam valendo.
                    var destino = new URL(window.location.href);
                    destino.searchParams.set("abrir", dados.id);
                    window.location.href = destino.toString();
                    return;
                }

                liberarBotao();

                // Erros do servidor: cada um aparece no seu campo
                var primeiroComErro = null;
                Object.keys(resultado.erros || {}).forEach(function (nome) {
                    var campo = form.elements[nome];
                    var bloco = campo && campo.closest ? campo.closest(".form-field") : null;
                    if (bloco) {
                        mostrarErro(bloco, resultado.erros[nome]);
                        if (!primeiroComErro) primeiroComErro = campo;
                    }
                });

                if (resultado.mensagem) mostrarAlerta(resultado.mensagem);
                if (primeiroComErro) focar(primeiroComErro);
            })
            .catch(function () {
                liberarBotao();
                mostrarAlerta("Não foi possível salvar agora. Confira a conexão e tente de novo (se a sessão expirou, entre novamente).");
            });
    });

    // Voltar pelo histórico (cache do navegador) não pode deixar o botão travado
    window.addEventListener("pageshow", liberarBotao);
})();
