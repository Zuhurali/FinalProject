(function () {
    const list = document.getElementById("activity-list");
    const empty = document.getElementById("activity-empty");
    const clearButton = document.getElementById("clear-activity-btn");
    const error = document.getElementById("activity-error");
    const antiforgeryForm = document.getElementById("activity-antiforgery");
    if (!list || !antiforgeryForm) {
        return;
    }

    function token() {
        const input = antiforgeryForm.querySelector('input[name="__RequestVerificationToken"]');
        return input ? input.value : "";
    }

    function showError(text) {
        if (!error) {
            return;
        }

        error.hidden = !text;
        error.textContent = text || "";
    }

    function syncEmptyState() {
        const remaining = list.querySelectorAll("li[data-activity-key]").length;
        if (remaining === 0) {
            list.hidden = true;
            if (empty) {
                empty.hidden = false;
            }
            if (clearButton) {
                clearButton.hidden = true;
            }
        }
    }

    async function post(url, body) {
        const response = await fetch(url, {
            method: "POST",
            headers: {
                "Content-Type": "application/x-www-form-urlencoded",
                "Accept": "application/json",
                "X-Requested-With": "XMLHttpRequest"
            },
            body: body,
            credentials: "same-origin"
        });
        const payload = await response.json().catch(function () { return null; });
        if (!response.ok) {
            throw new Error((payload && payload.message) || "The request failed.");
        }

        return payload;
    }

    if (clearButton) {
        clearButton.addEventListener("click", async function () {
            if (clearButton.disabled) {
                return;
            }

            clearButton.disabled = true;
            showError("");
            try {
                await post(clearButton.dataset.clearUrl, new URLSearchParams({
                    __RequestVerificationToken: token()
                }));
                list.querySelectorAll("li[data-activity-key]").forEach(function (item) {
                    item.remove();
                });
                syncEmptyState();
            } catch (exception) {
                showError(exception.message || "Activity could not be cleared.");
            } finally {
                clearButton.disabled = false;
            }
        });
    }

    list.addEventListener("click", async function (event) {
        const button = event.target.closest("[data-hide-url]");
        if (!button) {
            return;
        }

        event.preventDefault();
        if (button.disabled) {
            return;
        }

        const key = button.dataset.activityKey;
        const row = button.closest("li[data-activity-key]");
        button.disabled = true;
        showError("");
        try {
            await post(button.dataset.hideUrl, new URLSearchParams({
                key: key,
                __RequestVerificationToken: token()
            }));
            if (row) {
                row.remove();
            }
            syncEmptyState();
        } catch (exception) {
            showError(exception.message || "That activity entry could not be removed.");
            button.disabled = false;
        }
    });
})();
