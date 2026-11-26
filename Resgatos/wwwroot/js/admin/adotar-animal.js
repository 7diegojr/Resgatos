// Modal "Marcar como adotado" da lista de Animais (Areas/Admin/Views/Animais/_ModalAdotar.cshtml).
//   1. Abre pelo botão com data-adotar e preenche o painel do animal com os data-* dele.
//   2. Campos: telefone com máscara, valor formatado (vírgula), parcelas só quando "parcelada" está marcada.
//   3. Confere os campos antes de enviar (mesmas regras do AdotadosController.Validar) e envia por fetch;
//      erros do servidor aparecem nos campos, e o sucesso leva para a página Adotados.
(function () {
    "use strict";

    var modal = document.getElementById("modal-adotar");
    if (!modal) return;

    var form = document.getElementById("form-adotar");
    var gatilho = null;                      // botão que abriu o modal (o foco volta para ele ao fechar)

    var campoTelefone = document.getElementById("Telefone");
    var campoValor = document.getElementById("Valor");
    var caixaParcelada = document.getElementById("TaxaParcelada");
    var blocoParcelas = document.getElementById("campo-parcelas");
    var campoParcelas = document.getElementById("QtdParcelas");
    var alerta = document.getElementById("adotar-alerta");
    var botaoSalvar = document.getElementById("adotar-salvar");
    var campoTermo = document.getElementById("Termo");
    var zonaTermo = document.getElementById("termo-zona");
    var termoEscolhido = document.getElementById("termo-escolhido");
    var TAMANHO_MAXIMO = 5 * 1024 * 1024;
    var textoBotao = botaoSalvar.querySelector("span");
    var textoOriginalBotao = textoBotao.textContent;

    var VALOR_MAXIMO = 99999999.99;
    var MAX_PARCELAS = 24;

    function $(id) { return document.getElementById(id); }

    /* ============ 1. Abrir, preencher e fechar ============ */
    function preencherAnimal(b) {
        function dado(nome) { return b.getAttribute("data-" + nome) || ""; }

        $("adotar-id").value = dado("id");
        $("adotar-animal-nome").textContent = dado("nome");
        $("adotar-especie").textContent = dado("especie");
        $("adotar-sexo").textContent = dado("sexo");
        $("adotar-idade").textContent = dado("idade") + (dado("faixa") ? " (" + dado("faixa") + ")" : "");
        $("adotar-porte").textContent = dado("porte");
        $("adotar-cor").textContent = dado("cor");
        $("adotar-tempo").textContent = dado("tempo") || "Não informado";
        $("adotar-castrado").textContent = dado("castrado");

        var img = $("adotar-foto-img");
        var vazia = $("adotar-foto-vazia");
        if (dado("foto")) {
            img.src = dado("foto");
            img.alt = "Foto de " + dado("nome");
            img.hidden = false;
            vazia.hidden = true;
        } else {
            img.removeAttribute("src");
            img.hidden = true;
            vazia.hidden = false;
        }

        // Saúde: "Completa" ou "2 pendências" (em âmbar; não bloqueia a adoção). Só a quantidade, para o layout não quebrar.
        var pendencias = dado("pendencias");
        var qtdPendencias = pendencias ? pendencias.split(", ").length : 0;
        $("adotar-saude").textContent = qtdPendencias === 0 ? "Completa"
            : qtdPendencias === 1 ? "1 pendência" : qtdPendencias + " pendências";
        $("adotar-saude-item").classList.toggle("tem-pendencia", pendencias !== "");
    }

    function hojeIso() {
        var d = new Date();
        function dois(n) { return (n < 10 ? "0" : "") + n; }
        return d.getFullYear() + "-" + dois(d.getMonth() + 1) + "-" + dois(d.getDate());
    }

    function limparFormulario() {
        form.reset();
        form.querySelectorAll(".form-field").forEach(limparErro);
        alerta.hidden = true;
        atualizarParcelas();
        mostrarTermo(null);
        botaoSalvar.disabled = false;
        textoBotao.textContent = textoOriginalBotao;
    }

    document.addEventListener("click", function (evento) {
        var botao = evento.target.closest("[data-adotar]");
        if (!botao) return;

        gatilho = botao;
        limparFormulario();
        preencherAnimal(botao);
        campoDataPadrao();

        document.documentElement.classList.add("modal-aberto");
        modal.showModal();

        // Sem nenhum campo "selecionado" ao abrir (mesmo cuidado do modal de filtros)
        modal.setAttribute("tabindex", "-1");
        modal.style.outline = "none";
        modal.focus({ preventScroll: true });
    });

    function campoDataPadrao() {
        $("DataAdocao").value = hojeIso();   // o mais comum é registrar no próprio dia; dá para trocar
    }

    modal.querySelectorAll("[data-fechar-adotar]").forEach(function (botao) {
        botao.addEventListener("click", function () { modal.close(); });
    });

    // Clicar no fundo escurecido fecha (como nos outros modais do painel)
    modal.addEventListener("click", function (evento) {
        if (evento.target === modal) modal.close();
    });

    modal.addEventListener("close", function () {
        document.documentElement.classList.remove("modal-aberto");
        if (gatilho) gatilho.focus();
    });

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

    function aoMudar(evento) {
        var bloco = evento.target.closest && evento.target.closest(".form-field");
        if (bloco) limparErro(bloco);
        alerta.hidden = true;
    }
    form.addEventListener("input", aoMudar);
    form.addEventListener("change", aoMudar);

    /* ============ 2. Campos ============ */
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

    // Valor: só números, vírgula e ponto; ao sair, "150" vira "150,00" (mesma regra do campo da Transparência)
    function lerValor(texto) {
        var t = texto.replace(/R\$/gi, "").replace(/\s/g, "");
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

    // Mostra o arquivo escolhido (ou volta para a área de escolher quando é null)
    function mostrarTermo(arquivo) {
        zonaTermo.hidden = !!arquivo;
        termoEscolhido.hidden = !arquivo;
        if (arquivo) {
            $("termo-nome").textContent = arquivo.name;
            $("termo-tamanho").textContent = formatarTamanho(arquivo.size);
        }
    }

    function aceitarTermo(arquivo) {
        var bloco = $("campo-termo");
        limparErro(bloco);

        if (!arquivo) {
            mostrarTermo(null);
            return;
        }
        if (!tipoTermoValido(arquivo)) {
            campoTermo.value = "";
            mostrarTermo(null);
            mostrarErro(bloco, '"' + arquivo.name + '": precisa ser uma imagem JPG, PNG ou WebP, ou um PDF.');
            return;
        }
        if (arquivo.size > TAMANHO_MAXIMO) {
            campoTermo.value = "";
            mostrarTermo(null);
            mostrarErro(bloco, '"' + arquivo.name + '": passa de 5 MB.');
            return;
        }
        mostrarTermo(arquivo);
    }

    campoTermo.addEventListener("change", function () {
        aceitarTermo(campoTermo.files && campoTermo.files[0]);
    });

    $("termo-remover").addEventListener("click", function () {
        campoTermo.value = "";
        mostrarTermo(null);
        zonaTermo.focus();
    });

    // Arrastar e soltar
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
            mostrarErro($("campo-termo"), "Não foi possível arrastar o arquivo aqui. Clique na área para escolhê-lo.");
        }
    });

    /* ============ 3. Conferir e enviar ============ */
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

        var nome = $("NomeAdotante");
        if (nome.value.trim() !== "" && nome.value.trim().length < 3) falha(nome, "Informe o nome completo (mínimo 3 caracteres).");

        var digitos = campoTelefone.value.replace(/\D/g, "");
        if (campoTelefone.value.trim() !== "" && digitos.length !== 10 && digitos.length !== 11) {
            falha(campoTelefone, "Informe um telefone com DDD, ex.: (41) 99999-9999.");
        }

        var email = $("Email");
        if (email.value.trim() !== "" && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email.value.trim())) {
            falha(email, "Informe um e-mail válido.");
        }

        var endereco = $("Endereco");
        if (endereco.value.trim() !== "" && endereco.value.trim().length < 8) {
            falha(endereco, "Informe o endereço completo (rua, número e bairro).");
        }

        var data = $("DataAdocao");
        if (data.value !== "" && data.value > hojeIso()) falha(data, "A data da adoção não pode ser no futuro.");

        if (campoValor.value.trim() !== "") {
            var v = lerValor(campoValor.value);
            if (!v) falha(campoValor, "Informe um valor válido (ex.: 150,00).");
            else if (v.casas > 2) falha(campoValor, "O valor pode ter no máximo 2 casas depois da vírgula.");
            else if (v.numero < 0.01) falha(campoValor, "O valor deve ser maior que R$ 0,00.");
            else if (v.numero > VALOR_MAXIMO) falha(campoValor, "O valor é alto demais. Confira o que foi digitado.");
        }

        // Termo que a própria tela já recusou (tipo ou tamanho)
        if ($("campo-termo").classList.contains("has-error") && !primeiro) primeiro = zonaTermo;

        if (caixaParcelada.checked) {
            var n = parseInt(campoParcelas.value, 10);
            if (isNaN(n) || n < 2 || n > MAX_PARCELAS) falha(campoParcelas, "Informe de 2 a " + MAX_PARCELAS + " parcelas.");
        }

        return primeiro;
    }

    function focar(campo) {
        campo.scrollIntoView({ behavior: "smooth", block: "center" });
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

        var primeiro = validar();
        if (primeiro) {
            focar(primeiro);
            return;
        }

        botaoSalvar.disabled = true;           // evita enviar duas vezes
        textoBotao.textContent = "Salvando...";

        fetch(form.getAttribute("action"), {
            method: "POST",
            body: new FormData(form),          // já leva o token antifalsificação
            headers: { "Accept": "application/json" },
            credentials: "same-origin"
        })
            .then(function (resposta) {
                if (!resposta.ok) throw new Error("http");
                return resposta.json();
            })
            .then(function (dados) {
                if (dados.ok) {
                    window.location.href = dados.redirect;   // a página Adotados mostra o aviso de sucesso
                    return;
                }

                liberarBotao();

                // Erros do servidor: cada um aparece no seu campo
                var primeiroComErro = null;
                Object.keys(dados.erros || {}).forEach(function (nome) {
                    var campo = form.elements[nome];
                    var bloco = campo && campo.closest ? campo.closest(".form-field") : null;
                    if (bloco) {
                        mostrarErro(bloco, dados.erros[nome]);
                        if (!primeiroComErro) primeiroComErro = nome === "Termo" ? zonaTermo : campo;
                    }
                });

                if (dados.mensagem) mostrarAlerta(dados.mensagem);
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
