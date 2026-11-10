(function (root, factory) {
    var api = factory();
    if (typeof module === "object" && module.exports) {
        module.exports = api;
    }

    if (typeof document === "undefined") {
        return;
    }

    function bindAll() {
        document.querySelectorAll("[data-request-phone]").forEach(api.bindRequestPhone);
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", bindAll);
    } else {
        bindAll();
    }

    // Theme nice-select may wrap selects after our first bind; re-sync when it updates value.
    if (typeof window !== "undefined" && window.jQuery) {
        window.jQuery(function ($) {
            $(document).on("change", "select[data-request-phone-country]", function () {
                var field = this.closest("[data-request-phone]");
                if (field) {
                    api.applyCountry(field);
                }
            });
        });
    }
})(typeof globalThis !== "undefined" ? globalThis : this, function () {
    function selectedOption(select) {
        return select.options[select.selectedIndex] || null;
    }

    function maxDigitsFor(option) {
        if (!option) {
            return 9;
        }

        var max = Number(option.getAttribute("data-national") || option.getAttribute("data-max-digits") || "9");
        return Number.isFinite(max) && max > 0 ? max : 9;
    }

    function dialCodeFor(option) {
        if (!option) {
            return "994";
        }

        return option.getAttribute("data-dial") || "";
    }

    function limitNationalDigits(value, maxDigits, stripLeadingZero) {
        var text = value == null ? "" : String(value);
        var digits = "";
        for (var i = 0; i < text.length; i++) {
            var ch = text.charAt(i);
            if (ch >= "0" && ch <= "9") {
                digits += ch;
            }
        }

        if (stripLeadingZero) {
            while (digits.charAt(0) === "0") {
                digits = digits.slice(1);
            }
        }

        if (digits.length > maxDigits) {
            digits = digits.slice(0, maxDigits);
        }

        return digits;
    }

    function updateChrome(root, option) {
        var field = root.closest(".av-phone-field") || root.parentElement;
        var hint = field ? field.querySelector("[data-request-phone-hint]") : null;
        var prefix = root.querySelector("[data-request-phone-prefix]");
        if (hint && option) {
            hint.textContent = option.getAttribute("data-hint") || "";
        }

        if (prefix && option) {
            var dial = dialCodeFor(option);
            if (option.value === "OTHER") {
                prefix.hidden = true;
                prefix.textContent = "+";
            } else {
                prefix.hidden = false;
                prefix.textContent = dial ? "+" + dial : "+";
            }
        }
    }

    function applyCountry(root) {
        var select = root.querySelector("[data-request-phone-country]");
        var input = root.querySelector("[data-request-phone-input]");
        if (!select || !input) {
            return;
        }

        var option = selectedOption(select);
        var max = maxDigitsFor(option);
        var stripZero = option && option.value !== "OTHER";
        input.setAttribute("maxlength", String(max));
        input.value = limitNationalDigits(input.value, max, !!stripZero);
        if (option && option.value === "OTHER") {
            input.placeholder = "include country code";
        } else {
            input.placeholder = max + " digits";
        }
        updateChrome(root, option);
    }

    function bindRequestPhone(root) {
        if (!root || root.getAttribute("data-request-phone-bound") === "true") {
            return;
        }

        root.setAttribute("data-request-phone-bound", "true");
        var select = root.querySelector("[data-request-phone-country]");
        var input = root.querySelector("[data-request-phone-input]");
        if (!select || !input) {
            return;
        }

        // Never let theme nice-select wrap this control.
        select.classList.add("av-native-select");

        applyCountry(root);

        select.addEventListener("change", function () {
            applyCountry(root);
        });
        select.addEventListener("input", function () {
            applyCountry(root);
        });

        input.addEventListener("input", function () {
            var option = selectedOption(select);
            var max = maxDigitsFor(option);
            var stripZero = option && option.value !== "OTHER";
            var next = limitNationalDigits(input.value, max, !!stripZero);
            if (next !== input.value) {
                input.value = next;
            }
        });

        var form = root.closest("form");
        if (form) {
            form.addEventListener("submit", function () {
                applyCountry(root);
            });
        }
    }

    return {
        bindRequestPhone: bindRequestPhone,
        applyCountry: applyCountry,
        limitNationalDigits: limitNationalDigits,
        limitDigits: limitNationalDigits
    };
});
