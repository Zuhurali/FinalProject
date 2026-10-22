(function () {
    "use strict";

    var EVENT_COUNTS = "autovault:notification-counts";
    var navBadge = document.getElementById("nav-notification-badge");
    var counts = {
        count: 0,
        messages: 0,
        favorites: 0,
        listings: 0,
        other: 0
    };

    function formatCount(value) {
        var n = Math.max(0, Number(value) || 0);
        return n > 99 ? "99+" : String(n);
    }

    function setBadgeElement(el, value, labelSingular, labelPlural) {
        if (!el) {
            return;
        }

        var n = Math.max(0, Number(value) || 0);
        if (n <= 0) {
            el.hidden = true;
            el.textContent = "0";
            if (el.hasAttribute("aria-label")) {
                el.setAttribute("aria-label", "No unread notifications");
            }
            return;
        }

        el.hidden = false;
        el.textContent = formatCount(n);
        if (el.hasAttribute("aria-label")) {
            el.setAttribute(
                "aria-label",
                n === 1 ? labelSingular : labelPlural.replace("{n}", String(n)));
        }
    }

    function applyCounts(next) {
        counts = {
            count: Math.max(0, Number(next && next.count) || 0),
            messages: Math.max(0, Number(next && next.messages) || 0),
            favorites: Math.max(0, Number(next && next.favorites) || 0),
            listings: Math.max(0, Number(next && next.listings) || 0),
            other: Math.max(0, Number(next && next.other) || 0)
        };

        setBadgeElement(
            navBadge,
            counts.count,
            "1 unread notification",
            "{n} unread notifications");

        document.querySelectorAll("[data-notification-badge]").forEach(function (el) {
            var key = el.getAttribute("data-notification-badge");
            if (key === "messages") {
                setBadgeElement(el, counts.messages);
            } else if (key === "favorites") {
                setBadgeElement(el, counts.favorites);
            } else if (key === "listings") {
                setBadgeElement(el, counts.listings);
            } else if (key === "total") {
                setBadgeElement(el, counts.count);
            }
        });

        window.dispatchEvent(new CustomEvent(EVENT_COUNTS, { detail: counts }));
    }

    function categoryForType(type) {
        if (type === "message") {
            return "messages";
        }
        if (type === "price_drop") {
            return "favorites";
        }
        if (type === "listing") {
            return "listings";
        }
        return "other";
    }

    function incrementFromNotification(notification) {
        var next = {
            count: counts.count + 1,
            messages: counts.messages,
            favorites: counts.favorites,
            listings: counts.listings,
            other: counts.other
        };
        var category = categoryForType(notification && notification.type);
        if (category === "messages") {
            next.messages += 1;
        } else if (category === "favorites") {
            next.favorites += 1;
        } else if (category === "listings") {
            next.listings += 1;
        } else {
            next.other += 1;
        }
        applyCounts(next);
    }

    function getAntiForgeryToken() {
        var input = document.querySelector("#notification-antiforgery input[name='__RequestVerificationToken']")
            || document.querySelector("input[name='__RequestVerificationToken']");
        return input ? input.value : "";
    }

    function fetchCounts() {
        return fetch("/Account/Notifications/Count", {
            headers: { Accept: "application/json" },
            credentials: "same-origin"
        })
            .then(function (response) {
                if (!response.ok) {
                    return { count: 0, messages: 0, favorites: 0, listings: 0, other: 0 };
                }
                return response.json();
            })
            .then(function (data) {
                applyCounts(data || {});
                return counts;
            })
            .catch(function () {
                applyCounts({ count: 0, messages: 0, favorites: 0, listings: 0, other: 0 });
                return counts;
            });
    }

    function postJson(url) {
        var token = getAntiForgeryToken();
        var body = new URLSearchParams();
        if (token) {
            body.set("__RequestVerificationToken", token);
        }

        return fetch(url, {
            method: "POST",
            headers: {
                Accept: "application/json",
                "Content-Type": "application/x-www-form-urlencoded",
                RequestVerificationToken: token,
                "X-Requested-With": "XMLHttpRequest"
            },
            credentials: "same-origin",
            body: body.toString()
        }).then(function (response) {
            if (!response.ok) {
                throw new Error("Request failed");
            }
            return response.json();
        });
    }

    function markRowRead(row) {
        if (!row) {
            return;
        }
        row.classList.remove("is-unread");
        row.classList.add("is-read");
        var dot = row.querySelector(".notification-row__dot");
        if (dot) {
            dot.remove();
        }
    }

    function bindNotificationPage() {
        var list = document.getElementById("notification-list");
        if (!list) {
            return;
        }

        if (list.getAttribute("data-marked-read") === "true") {
            applyCounts({ count: 0, messages: 0, favorites: 0, listings: 0, other: 0 });
            list.querySelectorAll(".notification-row").forEach(markRowRead);
            var markAllWrap = document.getElementById("mark-all-notifications-wrap");
            if (markAllWrap) {
                markAllWrap.hidden = true;
            }
        }

        list.querySelectorAll("form.notification-row").forEach(function (form) {
            form.addEventListener("submit", function (event) {
                event.preventDefault();
                var id = form.getAttribute("data-notification-id");
                var returnUrl = form.querySelector("input[name='returnUrl']");
                var destination = returnUrl ? returnUrl.value : "/Account/Notifications";

                postJson("/Account/Notifications/" + id + "/Read")
                    .then(function (payload) {
                        if (payload && payload.counts) {
                            applyCounts(payload.counts);
                        } else {
                            applyCounts({ count: 0, messages: 0, favorites: 0, listings: 0, other: 0 });
                        }
                        markRowRead(form);
                        if (destination && destination.charAt(0) === "/") {
                            window.location.assign(destination);
                        }
                    })
                    .catch(function () {
                        form.submit();
                    });
            });
        });

        var markAllForm = document.getElementById("mark-all-notifications-form");
        if (markAllForm) {
            markAllForm.addEventListener("submit", function (event) {
                event.preventDefault();
                postJson("/Account/Notifications/ReadAll")
                    .then(function (payload) {
                        applyCounts((payload && payload.counts) || {
                            count: 0,
                            messages: 0,
                            favorites: 0,
                            listings: 0,
                            other: 0
                        });
                        list.querySelectorAll(".notification-row").forEach(markRowRead);
                        var wrap = document.getElementById("mark-all-notifications-wrap");
                        if (wrap) {
                            wrap.hidden = true;
                        }
                    })
                    .catch(function () {
                        markAllForm.submit();
                    });
            });
        }
    }

    function connectHub() {
        if (!window.signalR || !navBadge) {
            return;
        }

        var connection = new signalR.HubConnectionBuilder()
            .withUrl("/hubs/conversations")
            .withAutomaticReconnect()
            .build();

        connection.on("notificationReceived", function (notification) {
            incrementFromNotification(notification || {});
        });

        connection.start()
            .then(function () {
                return connection.invoke("JoinInbox");
            })
            .catch(function () {
                // Counts still load from the API.
            });
    }

    window.AutoVaultNotifications = {
        applyCounts: applyCounts,
        fetchCounts: fetchCounts,
        countsEvent: EVENT_COUNTS
    };

    bindNotificationPage();
    fetchCounts().then(connectHub);
})();
