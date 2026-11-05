(function (root, factory) {
    var api = factory();
    if (typeof module === "object" && module.exports) {
        module.exports = api;
    }

    if (typeof document === "undefined") {
        return;
    }

    function bindAll() {
        document.querySelectorAll("[data-team-phone]").forEach(api.bindTeamMemberPhone);
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", bindAll);
    } else {
        bindAll();
    }

    root.limitTeamMemberPhone = api.limitTeamMemberPhone;
})(typeof globalThis !== "undefined" ? globalThis : this, function () {
    var maxDigits = 12;

    function limitTeamMemberPhone(value) {
        var text = value == null ? "" : String(value);
        var plus = text.charAt(0) === "+";
        var digits = "";
        for (var i = 0; i < text.length && digits.length < maxDigits; i++) {
            var ch = text.charAt(i);
            if (ch >= "0" && ch <= "9") {
                digits += ch;
            }
        }

        return (plus ? "+" : "") + digits;
    }

    function removedBeforeCaret(value, caret) {
        var digits = 0;
        var removed = 0;
        var end = Math.max(0, Math.min(caret, value.length));
        for (var i = 0; i < end; i++) {
            var ch = value.charAt(i);
            if (i === 0 && ch === "+") {
                continue;
            }

            if (ch >= "0" && ch <= "9" && digits < maxDigits) {
                digits++;
                continue;
            }

            removed++;
        }

        return removed;
    }

    function validationSpan(input) {
        var holder = input.parentElement;
        if (!holder) {
            return null;
        }

        return holder.querySelector("[data-valmsg-for='Phone']");
    }

    function bindTeamMemberPhone(input) {
        if (!input || input.getAttribute("data-team-phone-bound") === "true") {
            return;
        }

        input.setAttribute("data-team-phone-bound", "true");
        input.addEventListener("input", function () {
            var raw = input.value;
            var next = limitTeamMemberPhone(raw);
            if (next !== raw) {
                var caret = input.selectionStart == null ? raw.length : input.selectionStart;
                var place = Math.max(0, Math.min(next.length, caret - removedBeforeCaret(raw, caret)));
                input.value = next;
                if (typeof input.setSelectionRange === "function") {
                    input.setSelectionRange(place, place);
                }
            }

            var span = validationSpan(input);
            if (span) {
                span.textContent = "";
            }
        });
        input.addEventListener("invalid", function () {
            var span = validationSpan(input);
            if (span) {
                span.textContent = input.title || input.validationMessage;
            }
        });
    }

    return {
        limitTeamMemberPhone: limitTeamMemberPhone,
        bindTeamMemberPhone: bindTeamMemberPhone
    };
});
