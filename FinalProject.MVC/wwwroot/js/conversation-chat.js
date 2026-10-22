(function () {
    const form = document.getElementById("conversation-reply-form");
    const thread = document.getElementById("message-thread");
    const input = document.getElementById("conversation-reply-input");
    const error = document.getElementById("conversation-send-error");
    if (!form || !thread || !input) {
        return;
    }

    const button = form.querySelector("[data-send-reply]");
    const conversationId = Number(form.dataset.conversationId);
    const currentUserId = thread.dataset.currentUserId || "";
    const seen = new Set(
        Array.from(thread.querySelectorAll("[data-message-id]"), function (element) {
            return element.getAttribute("data-message-id");
        })
    );
    let sending = false;

    function showError(text) {
        if (!error) {
            return;
        }

        error.hidden = !text;
        error.textContent = text || "";
    }

    function formatTime(value) {
        const date = new Date(value);
        if (Number.isNaN(date.getTime())) {
            return "";
        }

        return date.toLocaleString(undefined, { dateStyle: "short", timeStyle: "short" });
    }

    function scrollThreadToBottom() {
        thread.scrollTop = thread.scrollHeight;
    }

    function appendMessage(message) {
        if (!message || message.id == null) {
            return;
        }

        const id = String(message.id);
        if (seen.has(id)) {
            return;
        }

        seen.add(id);
        const bubble = document.createElement("div");
        bubble.className = "message-bubble " + (message.senderId === currentUserId ? "mine" : "theirs");
        bubble.setAttribute("data-message-id", id);

        const name = document.createElement("strong");
        name.textContent = message.senderDisplayName || "User";

        const body = document.createElement("p");
        body.textContent = message.content || "";

        const time = document.createElement("time");
        if (message.createdAt) {
            time.dateTime = message.createdAt;
        }
        time.textContent = formatTime(message.createdAt);

        bubble.append(name, body, time);
        thread.appendChild(bubble);
        scrollThreadToBottom();
    }

    scrollThreadToBottom();
    window.requestAnimationFrame(scrollThreadToBottom);

    input.addEventListener("keydown", function (event) {
        if (event.key === "Enter" && !event.shiftKey) {
            event.preventDefault();
            form.requestSubmit();
        }
    });

    form.addEventListener("submit", async function (event) {
        event.preventDefault();
        if (sending) {
            return;
        }

        const content = input.value;
        if (!content.trim()) {
            showError("Enter a message.");
            return;
        }

        if (content.trim().length > 4000) {
            showError("A message cannot exceed 4000 characters.");
            return;
        }

        const token = form.querySelector('input[name="__RequestVerificationToken"]');
        sending = true;
        if (button) {
            button.disabled = true;
        }
        showError("");

        try {
            const response = await fetch(form.action, {
                method: "POST",
                headers: {
                    "Content-Type": "application/x-www-form-urlencoded",
                    "Accept": "application/json",
                    "X-Requested-With": "XMLHttpRequest"
                },
                body: new URLSearchParams({
                    content: content,
                    __RequestVerificationToken: token ? token.value : ""
                }),
                credentials: "same-origin"
            });
            const payload = await response.json().catch(function () { return null; });
            if (!response.ok) {
                showError((payload && payload.message) || "The message could not be sent.");
                return;
            }

            appendMessage(payload);
            input.value = "";
        } catch (exception) {
            showError("The message could not be sent. Your text is still in the box.");
        } finally {
            sending = false;
            if (button) {
                button.disabled = false;
            }
            input.focus();
        }
    });

    if (!window.signalR || !conversationId) {
        showError("Live updates could not start. Refresh the page and try again.");
        return;
    }

    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/hubs/conversations")
        .withAutomaticReconnect()
        .build();

    connection.on("messageReceived", appendMessage);

    async function joinConversation() {
        await connection.invoke("JoinConversation", conversationId);
    }

    connection.onreconnected(function () {
        joinConversation().catch(function () {
            showError("Live updates disconnected. Refresh the page to keep receiving replies.");
        });
    });

    connection.start()
        .then(joinConversation)
        .catch(function () {
            showError("Live updates could not connect. You can still send a message, then refresh to see replies.");
        });
})();
