// Formulário de contato: seletor de assunto, validação e envio para o WhatsApp
document.addEventListener("DOMContentLoaded", function () {
    var form = document.getElementById("contact-form");
    if (!form) return;

    var nome = document.getElementById("contact-nome");
    var mensagem = document.getElementById("contact-mensagem");

    // ---------- Seletor de assunto (lista própria) ----------
    var select = form.querySelector(".form-select-wrapper");
    var trigger = document.getElementById("contact-assunto");
    var list = document.getElementById("contact-assunto-list");
    var valueLabel = document.getElementById("contact-assunto-value");
    var options = Array.prototype.slice.call(list.querySelectorAll('[role="option"]'));
    var selectedOption = null;
    var activeIndex = -1;

    function isOpen() {
        return select.classList.contains("is-open");
    }

    function setActive(index) {
        options.forEach(function (option) { option.classList.remove("is-active"); });
        activeIndex = index;

        if (index < 0) {
            trigger.removeAttribute("aria-activedescendant");
            return;
        }

        options[index].classList.add("is-active");
        trigger.setAttribute("aria-activedescendant", options[index].id);
    }

    function openList() {
        select.classList.add("is-open");
        trigger.setAttribute("aria-expanded", "true");
        setActive(selectedOption ? options.indexOf(selectedOption) : 0);
    }

    function closeList() {
        select.classList.remove("is-open");
        trigger.setAttribute("aria-expanded", "false");
        setActive(-1);
    }

    function chooseOption(option) {
        selectedOption = option;
        options.forEach(function (item) {
            item.setAttribute("aria-selected", item === option ? "true" : "false");
        });

        valueLabel.textContent = option.querySelector("span").textContent.trim();
        valueLabel.classList.remove("is-placeholder");

        clearError(trigger);
        closeList();
    }

    trigger.addEventListener("click", function () {
        if (isOpen()) closeList(); else openList();
    });

    trigger.addEventListener("keydown", function (event) {
        switch (event.key) {
            case "ArrowDown":
                event.preventDefault();
                if (!isOpen()) openList();
                else setActive(Math.min(activeIndex + 1, options.length - 1));
                break;
            case "ArrowUp":
                event.preventDefault();
                if (!isOpen()) openList();
                else setActive(Math.max(activeIndex - 1, 0));
                break;
            case "Home":
                if (isOpen()) { event.preventDefault(); setActive(0); }
                break;
            case "End":
                if (isOpen()) { event.preventDefault(); setActive(options.length - 1); }
                break;
            case "Enter":
            case " ":
                event.preventDefault();
                if (!isOpen()) openList();
                else if (activeIndex >= 0) chooseOption(options[activeIndex]);
                break;
            case "Escape":
                if (isOpen()) { event.preventDefault(); closeList(); }
                break;
            case "Tab":
                closeList();
                break;
        }
    });

    // Clicar numa opção escolhe; o mousedown não deixa o foco sair do seletor
    list.addEventListener("mousedown", function (event) { event.preventDefault(); });
    list.addEventListener("click", function (event) {
        var option = event.target.closest('[role="option"]');
        if (option) chooseOption(option);
    });

    // Clicar fora fecha a lista
    document.addEventListener("click", function (event) {
        if (!select.contains(event.target)) closeList();
    });

    // ---------- Erros (a animação de altura é feita no CSS) ----------
    function setError(field, text) {
        var wrapper = field.closest(".form-field");
        wrapper.querySelector(".form-error span").textContent = text;
        wrapper.classList.add("has-error");
        field.setAttribute("aria-invalid", "true");
    }

    // O texto fica no lugar enquanto a mensagem recolhe, para não sumir antes da animação
    function clearError(field) {
        field.closest(".form-field").classList.remove("has-error");
        field.removeAttribute("aria-invalid");
    }

    nome.addEventListener("input", function () { clearError(nome); });

    // Nome e assunto são obrigatórios; a mensagem é opcional
    function validate() {
        var firstInvalid = null;

        if (nome.value.trim().length < 2) {
            setError(nome, "Digite seu nome.");
            firstInvalid = nome;
        }

        if (!selectedOption) {
            setError(trigger, "Escolha um assunto.");
            if (!firstInvalid) firstInvalid = trigger;
        }

        if (firstInvalid) firstInvalid.focus();
        return !firstInvalid;
    }

    // ---------- Mensagem do WhatsApp ----------
    //   Olá, ResGatos! Vim pelo site e gostaria de conversar com vocês.
    //
    //   Meu nome é [nome] e [frase do assunto].
    //
    //   [mensagem, se preenchida]
    function buildMessage() {
        var nomeTexto = nome.value.trim().replace(/\s+/g, " ");
        var frase = selectedOption.getAttribute("data-frase");
        var texto = mensagem.value.trim().replace(/\r\n/g, "\n");

        var partes = [
            "Olá, ResGatos! Vim pelo site e gostaria de conversar com vocês.",
            "Meu nome é " + nomeTexto + " e " + frase + "."
        ];

        if (texto) partes.push(texto);

        return partes.join("\n\n");
    }

    form.addEventListener("submit", function (event) {
        event.preventDefault();
        if (!validate()) return;

        var url = "https://wa.me/" + form.getAttribute("data-whatsapp") +
            "?text=" + encodeURIComponent(buildMessage());

        window.open(url, "_blank", "noopener");
    });
});