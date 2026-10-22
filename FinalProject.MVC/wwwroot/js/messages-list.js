(function () {
    const list = document.getElementById("conversation-list");
    const empty = document.getElementById("conversation-empty");
    const error = document.getElementById("conversation-list-error");
    const antiforgeryForm = document.getElementById("conversation-list-antiforgery");
    if (!list || !antiforgeryForm) {
        return;
    }

    const currentUserId = list.dataset.currentUserId || "";
    let hiding = false;

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
        const remaining = list.querySelectorAll("[data-conversation-id]").length;
        list.hidden = remaining === 0;
        if (empty) {
            empty.hidden = remaining !== 0;
        }
    }

    function formatTime(value) {
        const date = new Date(value);
        if (Number.isNaN(date.getTime())) {
            return "";
        }

        return date.toLocaleString(undefined, { dateStyle: "short", timeStyle: "short" });
    }

    function upsertConversation(payload) {
        if (!payload || payload.id == null) {
            return;
        }

        const id = String(payload.id);
        let row = list.querySelector('[data-conversation-id="' + id + '"]');
        const otherName = payload.buyerId === currentUserId
            ? (payload.sellerDisplayName || "Dealer")
            : (payload.buyerDisplayName || "Buyer");
        const lastMessage = payload.lastMessage || "";
        const updatedAt = payload.updatedAt || new Date().toISOString();
        const unreadForReceiver = payload.receiverId === currentUserId ? 1 : 0;

        if (!row) {
            row = document.createElement("div");
            row.className = "conversation-row" + (unreadForReceiver ? " unread" : "");
            row.setAttribute("data-conversation-id", id);
            row.innerHTML =
                '<a class="conversation-row__link" href="/Account/Messages/' + id + '">' +
                    (payload.carImageUrl
                        ? '<img src="' + payload.carImageUrl + '" alt="">'
                        : "") +
                    '<div class="conversation-content">' +
                        '<div class="d-flex justify-content-between gap-3">' +
                            "<h5></h5><time></time>" +
                        "</div>" +
                        '<p><strong>With:</strong> <span data-other-name></span></p>' +
                        '<p class="last-message" data-last-message></p>' +
                    "</div>" +
                    (unreadForReceiver ? '<span class="unread-count" data-unread>1</span>' : "") +
                "</a>" +
                '<button type="button" class="conversation-delete" data-hide-url="/Account/Messages/' + id + '/Hide" data-conversation-id="' + id + '" title="Remove from your messages" aria-label="Delete conversation from your list">' +
                    '<i class="fa-regular fa-trash-can" aria-hidden="true"></i>' +
                "</button>";
            list.prepend(row);
        } else {
            list.prepend(row);
            if (unreadForReceiver) {
                row.classList.add("unread");
                let badge = row.querySelector("[data-unread]");
                if (!badge) {
                    badge = document.createElement("span");
                    badge.className = "unread-count";
                    badge.setAttribute("data-unread", "");
                    row.querySelector(".conversation-row__link")?.appendChild(badge);
                }
                badge.textContent = String(Number(badge.textContent || "0") + 1);
            }
        }

        const title = row.querySelector("h5");
        if (title) {
            title.textContent = payload.carName || "Conversation";
        }
        const other = row.querySelector("[data-other-name]");
        if (other) {
            other.textContent = otherName;
        }
        const preview = row.querySelector("[data-last-message]");
        if (preview) {
            preview.textContent = lastMessage;
        }
        const time = row.querySelector("time");
        if (time) {
            time.dateTime = updatedAt;
            time.textContent = formatTime(updatedAt);
        }
        if (payload.carImageUrl) {
            let image = row.querySelector("img");
            if (!image) {
                image = document.createElement("img");
                row.querySelector(".conversation-row__link")?.prepend(image);
            }
            image.src = payload.carImageUrl;
            image.alt = payload.carName || "";
        }

        list.hidden = false;
        if (empty) {
            empty.hidden = true;
        }
    }

    list.addEventListener("click", async function (event) {
        const button = event.target.closest(".conversation-delete");
        if (!button) {
            return;
        }

        event.preventDefault();
        event.stopPropagation();
        if (hiding || button.disabled) {
            return;
        }

        if (!window.confirm("Remove this conversation from your Messages list? The other person will still see it, and it will return if they send a new message.")) {
            return;
        }

        hiding = true;
        button.disabled = true;
        showError("");
        try {
            const response = await fetch(button.dataset.hideUrl, {
                method: "POST",
                headers: {
                    "Content-Type": "application/x-www-form-urlencoded",
                    "Accept": "application/json",
                    "X-Requested-With": "XMLHttpRequest"
                },
                body: new URLSearchParams({
                    __RequestVerificationToken: token()
                }),
                credentials: "same-origin"
            });
            const payload = await response.json().catch(function () { return null; });
            if (!response.ok) {
                throw new Error((payload && payload.message) || "The conversation could not be removed.");
            }

            // Prefer the card row, not the button (which also has data-conversation-id).
            const conversationId = button.dataset.conversationId
                || (payload && payload.id != null ? String(payload.id) : "");
            const row = button.closest(".conversation-row")
                || (conversationId
                    ? list.querySelector('.conversation-row[data-conversation-id="' + conversationId + '"]')
                    : null);
            if (row) {
                row.remove();
            }
            syncEmptyState();
        } catch (exception) {
            showError(exception.message || "The conversation could not be removed.");
            button.disabled = false;
        } finally {
            hiding = false;
        }
    });

    if (!window.signalR) {
        return;
    }

    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/hubs/conversations")
        .withAutomaticReconnect()
        .build();

    connection.on("conversationRestored", upsertConversation);

    connection.start()
        .then(function () { return connection.invoke("JoinInbox"); })
        .catch(function () {
            // List hide still works without live restore.
        });
})();
