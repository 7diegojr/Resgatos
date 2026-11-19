// Caixa de seleção própria do painel (mesmo visual e teclado do seletor de assunto de Contatos).
// Todo <select> do admin é trocado automaticamente por ela: não precisa mudar nada no HTML.
// O <select> original continua na página, escondido: é ele que vai no formulário e que dispara o "change".
// Para manter o seletor nativo em algum caso, use <select data-native>.
(function () {
    "use strict";

    var contador = 0;

    function melhorar(select) {
        if (select.hasAttribute("data-native") || select.multiple || select.closest(".form-select-wrapper")) return;

        contador++;
        var base = "cs" + contador;

        var wrapper = document.createElement("div");
        wrapper.className = "form-select-wrapper";

        var trigger = document.createElement("div");
        trigger.className = "form-select-trigger";
        trigger.tabIndex = 0;
        trigger.setAttribute("role", "combobox");
        trigger.setAttribute("aria-haspopup", "listbox");
        trigger.setAttribute("aria-expanded", "false");
        trigger.setAttribute("aria-controls", base + "-list");

        var valor = document.createElement("span");
        valor.className = "form-select-value";
        valor.id = base + "-value";

        var seta = document.createElement("i");
        seta.className = "fa-solid fa-chevron-down";
        seta.setAttribute("aria-hidden", "true");

        trigger.appendChild(valor);
        trigger.appendChild(seta);

        var lista = document.createElement("ul");
        lista.className = "form-select-list";
        lista.id = base + "-list";
        lista.setAttribute("role", "listbox");

        // Rótulo (<label for="...">): clicar nele foca a caixa nova, não o select escondido
        var rotulo = select.id ? document.querySelector('label[for="' + select.id + '"]') : null;
        if (rotulo) {
            if (!rotulo.id) rotulo.id = base + "-label";
            trigger.setAttribute("aria-labelledby", rotulo.id + " " + valor.id);
            lista.setAttribute("aria-labelledby", rotulo.id);
            rotulo.addEventListener("click", function (evento) {
                evento.preventDefault();
                trigger.focus();
            });
        } else if (select.getAttribute("aria-label")) {
            trigger.setAttribute("aria-label", select.getAttribute("aria-label"));
        }

        if (select.disabled) trigger.setAttribute("aria-disabled", "true");

        // Uma <li> para cada <option>
        var opcoes = Array.prototype.map.call(select.options, function (option, indice) {
            var item = document.createElement("li");
            item.id = base + "-opt" + indice;
            item.setAttribute("role", "option");
            item.setAttribute("aria-selected", "false");
            if (option.disabled) item.setAttribute("aria-disabled", "true");

            var texto = document.createElement("span");
            texto.textContent = option.textContent.trim();
            var marca = document.createElement("i");
            marca.className = "fa-solid fa-check";
            marca.setAttribute("aria-hidden", "true");

            item.appendChild(texto);
            item.appendChild(marca);
            lista.appendChild(item);
            return item;
        });

        // Troca o select pelo conjunto novo; o select vai para dentro, escondido
        select.parentNode.insertBefore(wrapper, select);
        wrapper.appendChild(trigger);
        wrapper.appendChild(lista);
        wrapper.appendChild(select);
        select.classList.add("visually-hidden");
        select.tabIndex = -1;
        select.setAttribute("aria-hidden", "true");

        var ativo = -1;

        function aberta() {
            return wrapper.classList.contains("is-open");
        }

        function marcarAtivo(indice) {
            opcoes.forEach(function (item) { item.classList.remove("is-active"); });
            ativo = indice;
            if (indice < 0) {
                trigger.removeAttribute("aria-activedescendant");
                return;
            }
            opcoes[indice].classList.add("is-active");
            trigger.setAttribute("aria-activedescendant", opcoes[indice].id);
        }

        // Mostra no gatilho a opção que está escolhida no select
        function sincronizar() {
            opcoes.forEach(function (item, indice) {
                item.setAttribute("aria-selected", indice === select.selectedIndex ? "true" : "false");
            });
            var escolhida = select.options[select.selectedIndex];
            valor.textContent = escolhida ? escolhida.textContent.trim() : "";
            // Opção sem valor ("Selecione") aparece com a cor de placeholder; nos filtros, "Todas" é uma opção sem valor mas é resposta de verdade (data-sem-placeholder)
            valor.classList.toggle("is-placeholder", !!escolhida && escolhida.value === "" && !select.hasAttribute("data-sem-placeholder"));
        }

        function abrir() {
            if (select.disabled) return;
            wrapper.classList.add("is-open");
            trigger.setAttribute("aria-expanded", "true");
            marcarAtivo(Math.max(select.selectedIndex, 0));
        }

        function fechar() {
            wrapper.classList.remove("is-open");
            trigger.setAttribute("aria-expanded", "false");
            marcarAtivo(-1);
        }

        function escolher(indice) {
            if (select.options[indice].disabled) return;
            var mudou = select.selectedIndex !== indice;
            select.selectedIndex = indice;
            sincronizar();
            fechar();
            if (mudou) select.dispatchEvent(new Event("change", { bubbles: true }));
        }

        trigger.addEventListener("click", function () {
            if (aberta()) fechar(); else abrir();
        });

        trigger.addEventListener("keydown", function (evento) {
            switch (evento.key) {
                case "ArrowDown":
                    evento.preventDefault();
                    if (!aberta()) abrir();
                    else marcarAtivo(Math.min(ativo + 1, opcoes.length - 1));
                    break;
                case "ArrowUp":
                    evento.preventDefault();
                    if (!aberta()) abrir();
                    else marcarAtivo(Math.max(ativo - 1, 0));
                    break;
                case "Home":
                    if (aberta()) { evento.preventDefault(); marcarAtivo(0); }
                    break;
                case "End":
                    if (aberta()) { evento.preventDefault(); marcarAtivo(opcoes.length - 1); }
                    break;
                case "Enter":
                case " ":
                    evento.preventDefault();
                    if (!aberta()) abrir();
                    else if (ativo >= 0) escolher(ativo);
                    break;
                case "Escape":
                    if (aberta()) { evento.preventDefault(); fechar(); }
                    break;
                case "Tab":
                    fechar();
                    break;
            }
        });

        // O mousedown não deixa o foco sair da caixa ao clicar numa opção
        lista.addEventListener("mousedown", function (evento) { evento.preventDefault(); });
        lista.addEventListener("click", function (evento) {
            var item = evento.target.closest('[role="option"]');
            if (item) escolher(opcoes.indexOf(item));
        });

        // Clicar fora fecha a lista
        document.addEventListener("click", function (evento) {
            if (!wrapper.contains(evento.target)) fechar();
        });

        // Se o select mudar por fora (ex.: formulário resetado), o gatilho acompanha
        select.addEventListener("change", sincronizar);
        if (select.form) select.form.addEventListener("reset", function () { setTimeout(sincronizar, 0); });

        sincronizar();
    }

    Array.prototype.forEach.call(document.querySelectorAll("select"), melhorar);
})();
