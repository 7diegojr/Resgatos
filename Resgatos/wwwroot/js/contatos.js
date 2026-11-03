// Formulário de contato: valida os campos e abre o WhatsApp com a mensagem formatada
document.addEventListener("DOMContentLoaded", function () {
    var form = document.getElementById("contact-form");
    if (!form) return;

    var nome = document.getElementById("contact-nome");
    var assunto = document.getElementById("contact-assunto");
    var mensagem = document.getElementById("contact-mensagem");

    function setError(field, text) {
        var wrapper = field.closest(".form-field");
        wrapper.classList.add("has-error");
        wrapper.querySelector(".form-error").textContent = text;
        field.setAttribute("aria-invalid", "true");
    }

    function clearError(field) {
        var wrapper = field.closest(".form-field");
        wrapper.classList.remove("has-error");
        wrapper.querySelector(".form-error").textContent = "";
        field.removeAttribute("aria-invalid");
    }

    // Some o erro assim que a pessoa corrige o campo
    [nome, assunto].forEach(function (field) {
        field.addEventListener("input", function () { clearError(field); });
        field.addEventListener("change", function () { clearError(field); });
    });

    // Nome e assunto são obrigatórios; a mensagem é opcional
    function validate() {
        var firstInvalid = null;

        if (nome.value.trim().length < 2) {
            setError(nome, "Digite seu nome.");
            firstInvalid = nome;
        }

        if (!assunto.value) {
            setError(assunto, "Escolha um assunto.");
            if (!firstInvalid) firstInvalid = assunto;
        }

        if (firstInvalid) firstInvalid.focus();
        return !firstInvalid;
    }

    // Monta a mensagem:
    //   Olá, ResGatos! Vim pelo site e gostaria de conversar com vocês.
    //
    //   Meu nome é [nome] e [frase do assunto].
    //
    //   [mensagem, se preenchida]
    function buildMessage() {
        var nomeTexto = nome.value.trim().replace(/\s+/g, " ");
        var frase = assunto.options[assunto.selectedIndex].getAttribute("data-frase");
        var texto = mensagem.value.trim().replace(/\r\n/g, "\n");

        var partes = [
            "Olá, Resgatos! Vim pelo site e gostaria de conversar com vocês.",
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
