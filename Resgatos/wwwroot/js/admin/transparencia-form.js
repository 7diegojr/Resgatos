// Formulário de criar/editar gasto (Areas/Admin/Views/Transparencia/_Formulario.cshtml).
//   1. Campo de valor: aceita só números, vírgula e ponto, e formata ao sair ("189,9" vira "189,90").
//   2. Foto e comprovante: prévia, trocar, remover antes de enviar, arrastar e soltar.
//   3. Confere os campos obrigatórios antes de enviar, com as mensagens no estilo do painel.
// O servidor confere tudo de novo (TransparenciaController): aqui é só para a pessoa ver o erro sem perder o que preencheu.
(function () {
    "use strict";

    var form = document.getElementById("transparencia-form");
    if (!form) return;

    var VALOR_MAXIMO = 99999999.99;

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

    // Qualquer mudança no campo apaga o erro dele (os campos de arquivo são tratados à parte)
    function aoMudar(evento) {
        if (evento.target.type === "file") return;
        var bloco = evento.target.closest && evento.target.closest(".form-field");
        if (bloco) limparErro(bloco);
    }
    form.addEventListener("input", aoMudar);
    form.addEventListener("change", aoMudar);

    /* ============ 1. Valor ============ */
    var campoValor = document.getElementById("Valor");

    // Mesma regra do servidor (TransparenciaController.TentarLerValor). Devolve { numero, casas } ou null se não é um valor.
    function lerValor(texto) {
        var t = texto.replace(/R\$/gi, "").replace(/\s/g, "");
        if (t === "") return null;

        if (t.indexOf(",") !== -1) {
            t = t.replace(/\./g, "").replace(",", ".");          // "1.234,50" -> "1234.50"
        } else if (/^\d{1,3}(\.\d{3})+$/.test(t)) {
            t = t.replace(/\./g, "");                            // "1.234" -> "1234"
        }

        if (!/^\d+(\.\d+)?$/.test(t)) return null;

        var partes = t.split(".");
        return { numero: parseFloat(t), casas: partes[1] ? partes[1].length : 0 };
    }

    // Só deixa digitar números, vírgula e ponto
    campoValor.addEventListener("input", function () {
        var limpo = campoValor.value.replace(/[^\d.,]/g, "");
        if (limpo !== campoValor.value) campoValor.value = limpo;
    });

    // Ao sair do campo: "189,9" vira "189,90" e "1234,5" vira "1.234,50"
    campoValor.addEventListener("blur", function () {
        var valor = lerValor(campoValor.value);
        if (valor && valor.casas <= 2 && valor.numero >= 0.01 && valor.numero <= VALOR_MAXIMO) {
            campoValor.value = valor.numero.toLocaleString("pt-BR", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
        }
    });

    /* ============ 2. Foto e comprovante ============ */
    function formatarMB(bytes) {
        return Math.round(bytes / (1024 * 1024));
    }

    function tipoValido(arquivo, aceitaPdf) {
        var tipos = ["image/jpeg", "image/png", "image/webp"];
        if (aceitaPdf) tipos.push("application/pdf");
        if (tipos.indexOf(arquivo.type) !== -1) return true;

        // Alguns navegadores não informam o tipo: confere pela extensão
        var extensoes = aceitaPdf ? /\.(jpe?g|png|webp|pdf)$/i : /\.(jpe?g|png|webp)$/i;
        return arquivo.type === "" && extensoes.test(arquivo.name);
    }

    function ehPdf(arquivo) {
        return arquivo.type === "application/pdf" || /\.pdf$/i.test(arquivo.name);
    }

    // Cada campo de arquivo (foto e comprovante) funciona sozinho. Um arquivo novo sempre substitui o anterior.
    function configurarCampoArquivo(bloco) {
        var input = bloco.querySelector('input[type="file"]');
        var zonaDeSoltar = bloco.querySelector(".foto-dropzone");
        var previa = bloco.querySelector("[data-arquivo-previa]");
        var mensagemErro = bloco.querySelector(".field-error");
        var atual = bloco.querySelector("[data-arquivo-atual] .foto-tile");
        var caixaRemover = atual ? atual.querySelector('input[type="checkbox"]') : null;

        var aceitaPdf = bloco.getAttribute("data-aceita-pdf") === "true";
        var maxBytes = parseInt(bloco.getAttribute("data-max-bytes"), 10) || 5 * 1024 * 1024;
        var urlPrevia = null;

        function mostrarErroArquivo(mensagem) {
            if (mensagem) {
                mostrarErro(bloco, mensagem);
            } else {
                limparErro(bloco);
            }
        }

        // Quando há arquivo novo, o atual fica apagado ("será substituído"); sem ele, vale o "Remover" marcado
        function atualizarAtual(haNovo) {
            if (!atual) return;
            atual.classList.toggle("is-removida", haNovo || caixaRemover.checked);
            caixaRemover.disabled = haNovo;
        }

        function limparPrevia() {
            if (urlPrevia) URL.revokeObjectURL(urlPrevia);
            urlPrevia = null;
            previa.innerHTML = "";
            atualizarAtual(false);
        }

        function desenharPrevia(arquivo) {
            limparPrevia();

            var item = document.createElement("li");
            item.className = "foto-tile foto-tile-nova";

            if (ehPdf(arquivo)) {
                var pdf = document.createElement("span");
                pdf.className = "arquivo-pdf";
                pdf.innerHTML = '<i class="fa-solid fa-file-pdf" aria-hidden="true"></i>';
                var nome = document.createElement("span");
                nome.textContent = arquivo.name;
                pdf.appendChild(nome);
                item.appendChild(pdf);
            } else {
                urlPrevia = URL.createObjectURL(arquivo);
                var imagem = document.createElement("img");
                imagem.src = urlPrevia;
                imagem.alt = "Prévia de " + arquivo.name;
                item.appendChild(imagem);
            }

            var selo = document.createElement("span");
            selo.className = "foto-selo-nova";
            selo.textContent = atual ? "Nova (substitui)" : "Novo";

            // Remover (ainda não foi enviado: só tira da escolha)
            var rodape = document.createElement("div");
            rodape.className = "foto-tile-footer";
            var botaoRemover = document.createElement("button");
            botaoRemover.type = "button";
            botaoRemover.className = "foto-opcao foto-opcao-remover";
            botaoRemover.setAttribute("aria-label", "Remover o arquivo " + arquivo.name);
            botaoRemover.innerHTML = '<span><i class="fa-solid fa-trash-can" aria-hidden="true"></i> Remover</span>';
            botaoRemover.addEventListener("click", function () {
                input.value = "";
                limparPrevia();
                mostrarErroArquivo("");
            });
            rodape.appendChild(botaoRemover);

            item.appendChild(selo);
            item.appendChild(rodape);
            previa.appendChild(item);
            atualizarAtual(true);
        }

        // Confere o arquivo escolhido. Se não serve, limpa a escolha e mostra o motivo.
        function aceitarArquivo(arquivo) {
            if (!arquivo) {
                limparPrevia();
                return;
            }
            if (!tipoValido(arquivo, aceitaPdf)) {
                input.value = "";
                limparPrevia();
                mostrarErroArquivo('"' + arquivo.name + '": precisa ser ' +
                    (aceitaPdf ? "uma imagem JPG, PNG ou WebP, ou um PDF." : "uma imagem JPG, PNG ou WebP."));
                return;
            }
            if (arquivo.size > maxBytes) {
                input.value = "";
                limparPrevia();
                mostrarErroArquivo('"' + arquivo.name + '": passa de ' + formatarMB(maxBytes) + " MB.");
                return;
            }

            mostrarErroArquivo("");
            desenharPrevia(arquivo);
        }

        input.addEventListener("change", function () {
            aceitarArquivo(input.files && input.files[0]);
        });

        // "Remover" do arquivo que o gasto já tem: apaga só ao salvar
        if (caixaRemover) {
            caixaRemover.addEventListener("change", function () {
                atual.classList.toggle("is-removida", caixaRemover.checked);
            });
        }

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
            var soltos = evento.dataTransfer && evento.dataTransfer.files;
            if (!soltos || soltos.length === 0) return;

            // Só um arquivo por campo: vale o primeiro
            try {
                var transferencia = new DataTransfer();
                transferencia.items.add(soltos[0]);
                input.files = transferencia.files;
                aceitarArquivo(soltos[0]);
            } catch (erro) {
                // Navegador sem suporte a montar a lista de arquivos por código: a pessoa usa o clique
                mostrarErroArquivo("Não foi possível arrastar o arquivo aqui. Clique na área para escolhê-lo.");
            }
        });

        return {
            // Arquivo escolhido que está em desacordo (para a conferência antes de enviar)
            temErro: function () { return bloco.classList.contains("has-error"); },
            bloco: bloco,
            zona: zonaDeSoltar
        };
    }

    var camposArquivo = Array.prototype.map.call(
        form.querySelectorAll("[data-campo-arquivo]"),
        configurarCampoArquivo
    );

    /* ============ 3. Conferir antes de enviar ============ */
    function hojeIso() {
        var d = new Date();
        function dois(n) { return (n < 10 ? "0" : "") + n; }
        return d.getFullYear() + "-" + dois(d.getMonth() + 1) + "-" + dois(d.getDate());
    }

    // Devolve o primeiro campo com problema (ou null se está tudo certo) e mostra a mensagem de cada um
    function validar() {
        var primeiro = null;

        function falha(campo, mensagem) {
            var bloco = campo.closest(".form-field");
            if (bloco) mostrarErro(bloco, mensagem);
            if (!primeiro) primeiro = campo;
        }

        form.querySelectorAll("[data-required]").forEach(function (campo) {
            if (campo.closest("[hidden]")) return;
            if (campo.value.trim() === "") {
                falha(campo, campo.getAttribute("data-msg") || "Preencha este campo.");
            }
        });

        // Título: no mínimo 3 caracteres
        var titulo = document.getElementById("Titulo");
        if (titulo.value.trim() !== "" && titulo.value.trim().length < 3) {
            falha(titulo, "O título deve ter pelo menos 3 caracteres.");
        }

        // Valor: número positivo, até 2 casas, dentro do limite
        if (campoValor.value.trim() !== "") {
            var valor = lerValor(campoValor.value);
            if (!valor) {
                falha(campoValor, "Informe um valor válido (ex.: 189,90).");
            } else if (valor.casas > 2) {
                falha(campoValor, "O valor pode ter no máximo 2 casas depois da vírgula.");
            } else if (valor.numero < 0.01) {
                falha(campoValor, "O valor deve ser maior que R$ 0,00.");
            } else if (valor.numero > VALOR_MAXIMO) {
                falha(campoValor, "O valor é alto demais. Confira o que foi digitado.");
            }
        }

        // Data: não pode ser no futuro
        var data = document.getElementById("Data");
        if (data.value !== "" && data.value > hojeIso()) {
            falha(data, "A data do gasto não pode ser no futuro.");
        }

        // Arquivos que a própria tela já recusou (tipo ou tamanho)
        camposArquivo.forEach(function (campo) {
            if (campo.temErro() && !primeiro) primeiro = campo.zona;
        });

        return primeiro;
    }

    function focar(campo) {
        var alvo = campo;
        if (campo.tagName === "SELECT") {
            // O <select> real está escondido: o foco vai para a caixa que a pessoa enxerga
            alvo = campo.parentNode.querySelector(".form-select-trigger") || campo;
        }
        alvo.scrollIntoView({ behavior: "smooth", block: "center" });
        if (alvo.focus) alvo.focus({ preventScroll: true });
    }

    var botaoSalvar = document.getElementById("transparencia-form-salvar");
    var textoBotao = botaoSalvar.querySelector("span");
    var textoOriginalBotao = textoBotao.textContent;

    form.addEventListener("submit", function (evento) {
        var primeiro = validar();
        if (primeiro) {
            evento.preventDefault();
            focar(primeiro);
            return;
        }

        // Evita enviar duas vezes (o envio com arquivos pode demorar um pouco)
        botaoSalvar.disabled = true;
        textoBotao.textContent = "Salvando...";
    });

    // Voltar pelo histórico (cache do navegador) não pode deixar o botão travado
    window.addEventListener("pageshow", function () {
        botaoSalvar.disabled = false;
        textoBotao.textContent = textoOriginalBotao;
    });
})();
