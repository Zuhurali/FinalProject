(function () {
    var form = document.getElementById("heroSearchForm");
    if (!form) {
        return;
    }

    var makeModels = bindMakeModels(form);

    var input = form.querySelector('input[name="search"]');
    var endpoint = form.getAttribute("data-suggestions-url");
    if (!input || !endpoint) {
        return;
    }

    var list = document.createElement("ul");
    list.id = "heroSuggestions";
    list.className = "hero-suggestions";
    list.setAttribute("role", "listbox");
    list.hidden = true;
    document.body.appendChild(list);

    var items = [];
    var activeIndex = -1;
    var timer = 0;
    var request = null;

    input.setAttribute("role", "combobox");
    input.setAttribute("aria-autocomplete", "list");
    input.setAttribute("aria-expanded", "false");
    input.setAttribute("aria-controls", "heroSuggestions");

    function position() {
        var rect = input.getBoundingClientRect();
        list.style.left = rect.left + "px";
        list.style.top = (rect.bottom + 8) + "px";
        list.style.width = rect.width + "px";
    }

    function close() {
        list.hidden = true;
        list.innerHTML = "";
        items = [];
        activeIndex = -1;
        input.setAttribute("aria-expanded", "false");
    }

    function uniqueSuggestions(entries) {
        var seen = {};
        return (entries || []).filter(function (item) {
            var text = (item && item.text ? item.text : "").trim();
            if (!text) {
                return false;
            }

            var key = (item.type || "") + "\n" + text.toLowerCase();
            if (seen[key]) {
                return false;
            }

            seen[key] = true;
            item.text = text;
            return true;
        });
    }

    function render(entries, emptyMessage) {
        list.innerHTML = "";
        items = uniqueSuggestions(entries);
        activeIndex = -1;

        if (!items.length) {
            var empty = document.createElement("li");
            empty.className = "hero-suggestion-empty";
            empty.textContent = emptyMessage || "No matches found";
            list.appendChild(empty);
        } else {
            items.forEach(function (item, index) {
                var row = document.createElement("li");
                row.setAttribute("role", "option");
                var button = document.createElement("button");
                button.type = "button";
                var title = document.createElement("span");
                title.className = "suggestion-text";
                title.textContent = item.text || "";
                var type = document.createElement("span");
                type.className = "suggestion-type";
                type.textContent = item.type === "brand" ? "Make" : "Car";
                button.appendChild(title);
                button.appendChild(type);
                button.addEventListener("mousedown", function (event) {
                    event.preventDefault();
                    choose(index);
                });
                row.appendChild(button);
                list.appendChild(row);
            });
        }

        list.hidden = false;
        input.setAttribute("aria-expanded", "true");
        position();
    }

    function choose(index) {
        var item = items[index];
        if (!item) {
            return;
        }

        window.clearTimeout(timer);
        if (request) {
            request.abort();
        }

        requestId += 1;
        input.value = item.text;
        close();

        if (item.type === "brand" && makeModels) {
            makeModels.selectMake(item.text);
            return;
        }

        if (item.type === "car" && makeModels) {
            makeModels.navigateFromCar(item.text);
            return;
        }

        input.focus();
    }

    function setActive(index) {
        var buttons = list.querySelectorAll("button");
        if (!buttons.length) {
            return;
        }

        if (index < 0) {
            index = buttons.length - 1;
        }

        if (index >= buttons.length) {
            index = 0;
        }

        activeIndex = index;
        buttons.forEach(function (button, buttonIndex) {
            button.parentElement.classList.toggle("active", buttonIndex === activeIndex);
        });
        buttons[activeIndex].scrollIntoView({ block: "nearest" });
    }

    var requestId = 0;

    function load(value) {
        if (request) {
            request.abort();
        }

        var controller = new AbortController();
        request = controller;
        var currentId = ++requestId;
        var separator = endpoint.indexOf("?") >= 0 ? "&" : "?";
        var url = endpoint + separator + "query=" + encodeURIComponent(value) + "&limit=8";

        function requestSuggestions(attempt) {
            fetch(url, { signal: controller.signal, headers: { "Accept": "application/json" } })
                .then(function (response) {
                    if (currentId !== requestId || input.value.trim() !== value) {
                        return null;
                    }

                    if (!response.ok) {
                        if (attempt < 2) {
                            return requestSuggestions(attempt + 1);
                        }

                        close();
                        return null;
                    }

                    return response.json();
                })
                .then(function (data) {
                    if (!data || currentId !== requestId || input.value.trim() !== value) {
                        return;
                    }

                    render(data, "No matches found");
                })
                .catch(function (error) {
                    if (error.name === "AbortError" || currentId !== requestId) {
                        return;
                    }

                    if (attempt < 2) {
                        requestSuggestions(attempt + 1);
                        return;
                    }

                    close();
                });
        }

        requestSuggestions(1);
    }

    input.addEventListener("input", function () {
        var value = input.value.trim();
        window.clearTimeout(timer);
        if (value.length < 2) {
            if (request) {
                request.abort();
            }

            close();
            return;
        }

        timer = window.setTimeout(function () {
            load(value);
        }, 250);
    });

    input.addEventListener("keydown", function (event) {
        if (event.key === "Escape") {
            close();
            return;
        }

        if (event.key === "Enter") {
            if (!list.hidden && activeIndex >= 0 && items[activeIndex]) {
                input.value = items[activeIndex].text;
            }

            close();
            event.preventDefault();
            if (typeof form.requestSubmit === "function") {
                form.requestSubmit();
            } else {
                form.submit();
            }
            return;
        }

        if (list.hidden) {
            return;
        }

        if (event.key === "ArrowDown") {
            event.preventDefault();
            setActive(activeIndex + 1);
        } else if (event.key === "ArrowUp") {
            event.preventDefault();
            setActive(activeIndex - 1);
        }
    });

    document.addEventListener("click", function (event) {
        if (event.target === input || list.contains(event.target)) {
            return;
        }

        close();
    });

    window.addEventListener("resize", function () {
        if (!list.hidden) {
            position();
        }
    });

    window.addEventListener("scroll", function () {
        if (!list.hidden) {
            position();
        }
    }, true);
})();

function bindMakeModels(form) {
    var makeSelect = form.querySelector('select[name="make"]');
    var modelSelect = form.querySelector('select[name="model"]');
    var dataElement = document.getElementById("heroModelsByMake");
    if (!makeSelect || !modelSelect || !dataElement) {
        return null;
    }

    var modelsByMake = {};
    try {
        modelsByMake = JSON.parse(dataElement.textContent || "{}") || {};
    } catch (error) {
        modelsByMake = {};
    }

    function modelsFor(make) {
        if (!make) {
            return [];
        }

        if (Object.prototype.hasOwnProperty.call(modelsByMake, make)) {
            return modelsByMake[make] || [];
        }

        var match = Object.keys(modelsByMake).find(function (name) {
            return name.toLowerCase() === make.toLowerCase();
        });
        return match ? modelsByMake[match] || [] : [];
    }

    var suppressModelSubmit = false;

    function refreshNiceSelect(select) {
        if (window.jQuery && window.jQuery.fn && window.jQuery.fn.niceSelect) {
            window.jQuery(select).niceSelect("update");
        }
    }

    function apply(make) {
        suppressModelSubmit = true;
        var models = modelsFor(make);
        modelSelect.innerHTML = "";
        var placeholder = document.createElement("option");
        placeholder.value = "";
        placeholder.textContent = make ? "Select Car Model" : "Select Car Make First";
        modelSelect.appendChild(placeholder);
        models.forEach(function (model) {
            var option = document.createElement("option");
            option.value = model;
            option.textContent = model;
            modelSelect.appendChild(option);
        });
        modelSelect.value = "";
        modelSelect.disabled = !make;
        refreshNiceSelect(modelSelect);

        var dropdown = modelSelect.nextElementSibling;
        if (dropdown && dropdown.classList.contains("nice-select")) {
            dropdown.classList.toggle("disabled", modelSelect.disabled);
        }

        suppressModelSubmit = false;
    }

    function canonicalMake(name) {
        var wanted = (name || "").trim().toLowerCase();
        if (!wanted) {
            return "";
        }

        var option = Array.prototype.find.call(makeSelect.options, function (item) {
            return item.value && item.value.toLowerCase() === wanted;
        });
        if (option) {
            return option.value;
        }

        return Object.keys(modelsByMake).find(function (item) {
            return item.toLowerCase() === wanted;
        }) || "";
    }

    function matchModelInText(text) {
        var haystack = (text || "").toLowerCase();
        var best = null;
        Object.keys(modelsByMake).forEach(function (make) {
            (modelsByMake[make] || []).forEach(function (model) {
                var needle = (model || "").toLowerCase();
                if (!needle || haystack.indexOf(needle) < 0) {
                    return;
                }

                var score = needle.length + (haystack.indexOf(make.toLowerCase()) >= 0 ? 1000 : 0);
                if (!best || score > best.score) {
                    best = { make: make, model: model, score: score };
                }
            });
        });
        return best;
    }

    function submitForm() {
        if (typeof form.requestSubmit === "function") {
            form.requestSubmit();
        } else {
            form.submit();
        }
    }

    function submitSelection() {
        var search = form.querySelector('input[name="search"]');
        if (search) {
            search.value = "";
        }

        submitForm();
    }

    function openModelDropdown() {
        window.setTimeout(function () {
            var dropdown = modelSelect.nextElementSibling;
            if (!dropdown || !dropdown.classList.contains("nice-select") || modelSelect.disabled) {
                return;
            }

            document.querySelectorAll(".nice-select.open").forEach(function (open) {
                if (open !== dropdown) {
                    open.classList.remove("open");
                }
            });
            dropdown.classList.add("open");
        }, 0);
    }

    function selectMake(name) {
        var make = canonicalMake(name);
        if (!make) {
            return;
        }

        makeSelect.value = make;
        refreshNiceSelect(makeSelect);
        apply(make);
        openModelDropdown();
    }

    function navigateFromCar(text) {
        var match = matchModelInText(text);
        if (!match) {
            var search = form.querySelector('input[name="search"]');
            if (search) {
                search.value = text;
            }

            submitForm();
            return;
        }

        makeSelect.value = match.make;
        apply(match.make);
        modelSelect.value = match.model;
        submitSelection();
    }

    function onModelChange() {
        if (suppressModelSubmit || !modelSelect.value) {
            return;
        }

        suppressModelSubmit = true;
        submitSelection();
    }

    function start() {
        var onMakeChange = function () {
            apply(makeSelect.value);
        };

        if (window.jQuery) {
            window.jQuery(makeSelect).on("change", onMakeChange);
            window.jQuery(modelSelect).on("change", onModelChange);
        } else {
            makeSelect.addEventListener("change", onMakeChange);
            modelSelect.addEventListener("change", onModelChange);
        }

        apply(makeSelect.value);
    }

    if (window.jQuery) {
        window.jQuery(start);
    } else {
        start();
    }

    return {
        selectMake: selectMake,
        navigateFromCar: navigateFromCar
    };
}
